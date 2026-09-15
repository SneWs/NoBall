using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NoBall
{
    public sealed class WallFx : MonoBehaviour
    {
        static WallFx _instance;
        static Mesh _cubeMesh;
        static Material _debrisMaterial;
        static Material _sparkMaterial;
        static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

        ParticleSystem _grow;

        public static void GrowTip(Vector2 world, Vector2 direction)
        {
            Ensure().EmitGrow(world, direction);
        }

        public static void Shatter(Playfield playfield, IReadOnlyList<Vector2Int> cells, Vector2 crash)
        {
            Shatter(playfield, cells, crash, GameColors.Building);
        }

        public static void Shatter(Playfield playfield, IReadOnlyList<Vector2Int> cells, Vector2 crash, Color color)
        {
            if (playfield == null || cells == null || cells.Count == 0)
                return;
            Ensure().SpawnDebris(playfield, cells, crash, color);
        }

        static WallFx Ensure()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject("WallFx");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<WallFx>();
            _instance.Build();
            return _instance;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void EmitGrow(Vector2 world, Vector2 direction)
        {
            var origin = new Vector3(world.x, world.y, -0.05f);
            var dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
            var tangent = new Vector2(-dir.y, dir.x);

            for (int i = 0; i < 10; i++)
            {
                var emit = new ParticleSystem.EmitParams();
                var spray = dir * Random.Range(0.4f, 1.15f) + tangent * Random.Range(-0.55f, 0.55f);
                emit.position = origin;
                emit.velocity = (Vector3)(spray.normalized * Random.Range(1.6f, 3.8f));
                emit.startSize = Random.Range(0.05f, 0.13f);
                emit.startLifetime = Random.Range(0.12f, 0.26f);
                emit.startColor = Color.Lerp(
                    new Color(1f, 0.95f, 0.55f, 1f),
                    GameColors.Building,
                    Random.value);
                _grow.Emit(emit, 1);
            }

            for (int i = 0; i < 2; i++)
            {
                var emit = new ParticleSystem.EmitParams();
                emit.position = origin;
                emit.velocity = (Vector3)(dir * Random.Range(0.2f, 0.8f));
                emit.startSize = Random.Range(0.16f, 0.28f);
                emit.startLifetime = Random.Range(0.08f, 0.14f);
                emit.startColor = new Color(1f, 0.9f, 0.45f, 0.9f);
                _grow.Emit(emit, 1);
            }
        }

        void SpawnDebris(Playfield playfield, IReadOnlyList<Vector2Int> cells, Vector2 crash, Color color)
        {
            float cell = playfield.CellSize;
            int piecesPerCell = cells.Count <= 64 ? 4 : 1;
            int skip = 1;
            if (cells.Count * piecesPerCell > 140)
            {
                piecesPerCell = 1;
                skip = Mathf.Max(1, Mathf.CeilToInt(cells.Count / 100f));
            }

            float shard = cell * (skip > 1 ? Mathf.Clamp(skip * 0.55f, 0.7f, 1.6f) : (piecesPerCell == 1 ? 0.82f : 0.42f));
            var crash3 = new Vector3(crash.x, crash.y, 0f);

            for (int c = 0; c < cells.Count; c += skip)
            {
                var rect = playfield.CellWorldRect(cells[c]);
                int div = piecesPerCell == 4 ? 2 : 1;
                float step = cell / div;
                for (int ix = 0; ix < div; ix++)
                {
                    for (int iy = 0; iy < div; iy++)
                    {
                        float x = rect.xMin + (ix + 0.5f) * step;
                        float y = rect.yMin + (iy + 0.5f) * step;
                        var pos = new Vector3(x, y, -0.08f);
                        var away = pos - crash3;
                        if (away.sqrMagnitude < 0.0001f)
                            away = Random.insideUnitSphere;
                        away.z = Random.Range(-0.4f, 0.15f);
                        var velocity = away.normalized * Random.Range(2.0f, 5.2f);
                        velocity.y -= Random.Range(1.6f, 3.4f);
                        SpawnShard(pos, Vector3.one * (shard * Random.Range(0.75f, 1.05f)), velocity, color);
                    }
                }
            }
        }

        void SpawnShard(Vector3 position, Vector3 scale, Vector3 velocity, Color color)
        {
            var go = new GameObject("WallShard");
            go.transform.SetPositionAndRotation(position, Random.rotation);
            go.transform.localScale = scale;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = CubeMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = DebrisMaterial();
            renderer.SetPropertyBlock(ColorBlock(color));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.sortingOrder = 14;

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.15f;
            body.linearDamping = 0.35f;
            body.angularDamping = 0.2f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * Random.Range(8f, 18f);

            var debris = go.AddComponent<WallDebris>();
            debris.Begin(Random.Range(1.15f, 1.9f), scale);
        }

        static MaterialPropertyBlock ColorBlock(Color color)
        {
            PropertyBlock.Clear();
            PropertyBlock.SetColor("_Color", color);
            PropertyBlock.SetColor("_BaseColor", color);
            return PropertyBlock;
        }

        void Build()
        {
            var go = new GameObject("GrowSparks");
            go.transform.SetParent(transform, false);
            _grow = go.AddComponent<ParticleSystem>();
            _grow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _grow.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.gravityModifier = 0.2f;
            main.maxParticles = 256;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = _grow.emission;
            emission.enabled = false;
            var shape = _grow.shape;
            shape.enabled = false;

            var size = _grow.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var color = _grow.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(GameColors.Building, 0.45f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.75f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = SparkMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 13;
        }

        static Material DebrisMaterial()
        {
            if (_debrisMaterial != null)
                return _debrisMaterial;
            var shader = Resources.Load<Shader>("Shaders/Debris") ?? Shader.Find("NoBall/Debris");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _debrisMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _debrisMaterial.SetColor("_Color", GameColors.Building);
            _debrisMaterial.SetColor("_BaseColor", GameColors.Building);
            _debrisMaterial.renderQueue = 3010;
            return _debrisMaterial;
        }

        static Material SparkMaterial()
        {
            if (_sparkMaterial != null)
                return _sparkMaterial;
            var shader = Resources.Load<Shader>("Shaders/Spark") ?? Shader.Find("NoBall/Spark");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            _sparkMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _sparkMaterial.SetColor("_Color", Color.white);
            _sparkMaterial.renderQueue = 3120;
            return _sparkMaterial;
        }

        static Mesh CubeMesh()
        {
            if (_cubeMesh != null)
                return _cubeMesh;
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.SetActive(false);
            _cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
            return _cubeMesh;
        }
    }

    public sealed class WallDebris : MonoBehaviour
    {
        float _life;
        float _age;
        Vector3 _startScale;

        public void Begin(float life, Vector3 startScale)
        {
            _life = life;
            _startScale = startScale;
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Mathf.Max(_life, 0.01f));
            if (t > 0.55f)
            {
                float fade = 1f - (t - 0.55f) / 0.45f;
                transform.localScale = _startScale * Mathf.Max(0.01f, fade);
            }

            if (_age >= _life)
                Destroy(gameObject);
        }
    }
}
