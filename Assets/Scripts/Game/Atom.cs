using UnityEngine;
using UnityEngine.Rendering;

namespace NoBall
{
    public sealed class Atom : MonoBehaviour
    {
        static Mesh _sphereMesh;
        static Material _sharedSparkMaterial;

        Rigidbody2D _body;
        Transform _visual;
        Material _material;
        ParticleSystem _sparks;
        float _speed;
        float _signX = 1f;
        float _signY = 1f;
        float _diameter;
        float _flash;
        float _spinSpeed;
        Vector3 _spinAxis = Vector3.up;
        bool _paused;

        public System.Action Bounced;

        public Vector2 Position => _body != null ? _body.position : (Vector2)transform.position;
        public float Radius { get; private set; }

        public void Build(Vector2 position, float radius, float speed, PhysicsMaterial2D material, Vector2 direction)
        {
            Radius = radius;
            _speed = speed;
            _diameter = radius * 2f;
            transform.position = new Vector3(position.x, position.y, 0f);
            transform.localScale = Vector3.one;

            BuildVisual();
            BuildSparks();

            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = radius;
            collider.sharedMaterial = material;

            _body = gameObject.AddComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = 0f;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.sharedMaterial = material;
            _body.position = position;

            _signX = direction.x < 0f ? -1f : 1f;
            _signY = direction.y < 0f ? -1f : 1f;
            _spinAxis = Random.onUnitSphere;
            _spinSpeed = Random.Range(40f, 90f);
            ApplyVelocity();
        }

        public void PlayWallCrash()
        {
            _flash = 1f;
            _spinSpeed = Random.Range(280f, 420f);
            _spinAxis = Random.onUnitSphere;
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            if (_body == null)
                return;
            _body.simulated = !paused;
            if (!paused)
                ApplyVelocity();
        }

        void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

        void Update()
        {
            if (_visual == null)
                return;

            float dt = Time.deltaTime;
            _visual.Rotate(_spinAxis, _spinSpeed * dt, Space.World);
            _spinSpeed = Mathf.MoveTowards(_spinSpeed, 55f, dt * 80f);
            _flash = Mathf.MoveTowards(_flash, 0f, dt * 5.5f);

            if (_material != null)
            {
                _material.SetFloat("_AnimTime", Time.time);
                _material.SetFloat("_Flash", _flash);
            }
        }

        void FixedUpdate()
        {
            if (_paused || _body == null)
                return;

            var velocity = _body.linearVelocity;
            bool bounced = false;
            float hitX = 0f;
            float hitY = 0f;
            if (Mathf.Abs(velocity.x) > 0.01f)
            {
                float sign = Mathf.Sign(velocity.x);
                if (sign != _signX)
                {
                    bounced = true;
                    hitX = sign;
                }

                _signX = sign;
            }

            if (Mathf.Abs(velocity.y) > 0.01f)
            {
                float sign = Mathf.Sign(velocity.y);
                if (sign != _signY)
                {
                    bounced = true;
                    hitY = sign;
                }

                _signY = sign;
            }

            if (bounced)
            {
                var dir = new Vector2(hitX != 0f ? hitX : _signX, hitY != 0f ? hitY : _signY);
                OnBounce(dir.normalized);
                Bounced?.Invoke();
            }

            ApplyVelocity();
        }

        void ApplyVelocity()
        {
            _body.linearVelocity = new Vector2(_signX, _signY).normalized * _speed;
        }

        void OnBounce(Vector2 direction)
        {
            _flash = 1f;
            _spinSpeed = Random.Range(220f, 380f);
            _spinAxis = Vector3.Cross(direction, Vector3.forward);
            if (_spinAxis.sqrMagnitude < 0.01f)
                _spinAxis = Random.onUnitSphere;
            else
                _spinAxis.Normalize();
            BurstSparks(direction);
        }

        void BuildVisual()
        {
            var shader = Resources.Load<Shader>("Shaders/Atom") ?? Shader.Find("NoBall/Atom");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _material.SetColor("_White", GameColors.AtomWhite);
            _material.SetColor("_Red", GameColors.AtomRed);
            _material.SetColor("_Outline", GameColors.AtomOutline);
            _material.SetFloat("_Seed", Random.Range(0f, 32f));
            _material.SetFloat("_Flash", 0f);
            _material.renderQueue = 3005;

            var light = FindAnyObjectByType<Light>();
            if (light != null)
                _material.SetVector("_LightDir", -light.transform.forward);
            else
                _material.SetVector("_LightDir", new Vector4(0.35f, 0.8f, -0.5f, 0f));

            var visual = new GameObject("Visual");
            _visual = visual.transform;
            _visual.SetParent(transform, false);
            _visual.localPosition = new Vector3(0f, 0f, -0.04f);
            _visual.localScale = Vector3.one * _diameter;
            _visual.rotation = Random.rotation;

            var filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = SphereMesh();
            var meshRenderer = visual.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.sortingOrder = 10;
        }

        void BuildSparks()
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(transform, false);
            _sparks = go.AddComponent<ParticleSystem>();
            _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.92f, 0.45f, 1f),
                new Color(1f, 0.45f, 0.18f, 1f));
            main.gravityModifier = 0.45f;
            main.maxParticles = 96;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = _sparks.emission;
            emission.enabled = false;

            var shape = _sparks.shape;
            shape.enabled = false;

            var size = _sparks.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var color = _sparks.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = SparkMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 12;
        }

        void BurstSparks(Vector2 direction)
        {
            if (_sparks == null)
                return;

            var origin = (Vector3)Position + new Vector3(0f, 0f, -0.04f);
            var tangent = new Vector2(-direction.y, direction.x);

            for (int i = 0; i < 26; i++)
            {
                var emit = new ParticleSystem.EmitParams();
                var spray = direction * Random.Range(0.65f, 1.2f)
                            + tangent * Random.Range(-0.85f, 0.85f)
                            + Random.insideUnitCircle * 0.2f;
                emit.position = origin;
                emit.velocity = (Vector3)(spray.normalized * Random.Range(3.4f, 7.8f));
                emit.startSize = Random.Range(0.08f, 0.22f);
                emit.startLifetime = Random.Range(0.2f, 0.42f);
                emit.startColor = Color.Lerp(
                    new Color(1f, 0.95f, 0.55f, 1f),
                    new Color(1f, 0.35f, 0.12f, 1f),
                    Random.value);
                _sparks.Emit(emit, 1);
            }

            for (int i = 0; i < 6; i++)
            {
                var emit = new ParticleSystem.EmitParams();
                emit.position = origin;
                emit.velocity = (Vector3)(direction * Random.Range(0.8f, 2.2f));
                emit.startSize = Random.Range(0.28f, 0.52f);
                emit.startLifetime = Random.Range(0.14f, 0.24f);
                emit.startColor = new Color(1f, 0.82f, 0.35f, 0.9f);
                _sparks.Emit(emit, 1);
            }
        }

        static Material SparkMaterial()
        {
            if (_sharedSparkMaterial != null)
                return _sharedSparkMaterial;

            var shader = Resources.Load<Shader>("Shaders/Spark") ?? Shader.Find("NoBall/Spark");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            _sharedSparkMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _sharedSparkMaterial.SetColor("_Color", Color.white);
            _sharedSparkMaterial.renderQueue = 3100;
            return _sharedSparkMaterial;
        }

        static Mesh SphereMesh()
        {
            if (_sphereMesh != null)
                return _sphereMesh;

            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.SetActive(false);
            _sphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
            return _sphereMesh;
        }
    }
}
