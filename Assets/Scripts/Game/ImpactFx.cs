using UnityEngine;

namespace NoBall
{
    public sealed class ImpactFx : MonoBehaviour
    {
        static ImpactFx _instance;
        static Material _sparkMaterial;
        static Material _smokeMaterial;

        ParticleSystem _explosion;
        ParticleSystem _smoke;

        public static void WallCrash(Vector2 world)
        {
            Ensure().Burst(world);
        }

        static ImpactFx Ensure()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject("ImpactFx");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ImpactFx>();
            _instance.Build();
            return _instance;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Burst(Vector2 world)
        {
            var origin = new Vector3(world.x, world.y, -0.06f);
            EmitExplosion(origin);
            EmitSmoke(origin);
        }

        void EmitExplosion(Vector3 origin)
        {
            for (int i = 0; i < 36; i++)
            {
                var emit = new ParticleSystem.EmitParams();
                var dir = (Vector3)Random.insideUnitCircle.normalized;
                dir.z = Random.Range(-0.2f, 0.2f);
                emit.position = origin;
                emit.velocity = dir * Random.Range(4.5f, 11f);
                emit.startSize = Random.Range(0.12f, 0.34f);
                emit.startLifetime = Random.Range(0.18f, 0.38f);
                emit.startColor = Color.Lerp(
                    new Color(1f, 0.95f, 0.55f, 1f),
                    new Color(1f, 0.28f, 0.06f, 1f),
                    Random.value);
                _explosion.Emit(emit, 1);
            }

            for (int i = 0; i < 8; i++)
            {
                var emit = new ParticleSystem.EmitParams();
                emit.position = origin + (Vector3)(Random.insideUnitCircle * 0.08f);
                emit.velocity = (Vector3)(Random.insideUnitCircle * 1.6f);
                emit.startSize = Random.Range(0.5f, 0.95f);
                emit.startLifetime = Random.Range(0.14f, 0.26f);
                emit.startColor = new Color(1f, 0.72f, 0.22f, 0.95f);
                _explosion.Emit(emit, 1);
            }
        }

        void EmitSmoke(Vector3 origin)
        {
            for (int i = 0; i < 24; i++)
            {
                var emit = new ParticleSystem.EmitParams();
                var dir = (Vector3)Random.insideUnitCircle.normalized;
                dir.y += Random.Range(0.6f, 1.4f);
                emit.position = origin + (Vector3)(Random.insideUnitCircle * 0.16f);
                emit.velocity = dir.normalized * Random.Range(0.8f, 2.4f);
                emit.startSize = Random.Range(0.5f, 1.15f);
                emit.startLifetime = Random.Range(0.85f, 1.8f);
                emit.rotation = Random.Range(0f, 360f);
                float shade = Random.Range(0.18f, 0.42f);
                emit.startColor = new Color(shade, shade * 0.95f, shade * 0.9f, Random.Range(0.5f, 0.78f));
                _smoke.Emit(emit, 1);
            }
        }

        void Build()
        {
            _explosion = CreateSystem("Explosion", SparkMaterial(), 12, true);
            var ex = _explosion.main;
            ex.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
            ex.startSpeed = new ParticleSystem.MinMaxCurve(4f, 11f);
            ex.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.4f);
            ex.gravityModifier = 0.15f;
            ex.maxParticles = 160;

            _smoke = CreateSystem("Smoke", SmokeMaterial(), 11, false);
            var sm = _smoke.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.8f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            sm.gravityModifier = -0.45f;
            sm.maxParticles = 128;
            sm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var smokeSize = _smoke.sizeOverLifetime;
            smokeSize.enabled = true;
            smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 2.8f));
        }

        ParticleSystem CreateSystem(string name, Material material, int sorting, bool additiveColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.duration = 0.3f;

            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            if (additiveColor)
            {
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(1f, 0.55f, 0.12f), 0.4f),
                        new GradientColorKey(new Color(0.45f, 0.08f, 0.02f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.8f, 0.3f),
                        new GradientAlphaKey(0f, 1f)
                    });
            }
            else
            {
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.45f, 0.42f, 0.4f), 0f),
                        new GradientColorKey(new Color(0.28f, 0.27f, 0.26f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.55f, 0f),
                        new GradientAlphaKey(0.35f, 0.4f),
                        new GradientAlphaKey(0f, 1f)
                    });
            }

            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sorting;
            return ps;
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

        static Material SmokeMaterial()
        {
            if (_smokeMaterial != null)
                return _smokeMaterial;
            var shader = Resources.Load<Shader>("Shaders/Smoke") ?? Shader.Find("NoBall/Smoke");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            _smokeMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _smokeMaterial.SetColor("_Color", Color.white);
            _smokeMaterial.renderQueue = 3110;
            return _smokeMaterial;
        }
    }
}
