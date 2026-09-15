using UnityEngine;
using UnityEngine.Rendering;

namespace NoBall
{
    public sealed class GameBackgroundView : MonoBehaviour
    {
        MeshRenderer _meshRenderer;
        Material _material;
        Camera _camera;
        bool _fullScreen;
        bool _customShader;
        int _lastWidth;
        int _lastHeight;

        public void BuildPlayfield(Vector2 worldSize)
        {
            _fullScreen = false;
            BuildRenderer();
            transform.position = new Vector3(0f, 0f, 0.25f);
            transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
            Apply(GameSettings.Background);
        }

        public void BuildFullScreen(Camera camera)
        {
            _fullScreen = true;
            _camera = camera;
            BuildRenderer();
            FitCamera();
            Apply(GameSettings.Background);
        }

        public void Apply(GameBackgroundId id)
        {
            if (_meshRenderer == null || _material == null)
                return;

            bool classic = id == GameBackgroundId.Classic;
            _meshRenderer.enabled = !classic;
            if (classic)
                return;

            var tex = TextureFor(id);
            if (tex != null)
                tex.wrapMode = TextureWrapMode.Clamp;

            if (_customShader)
            {
                _material.SetFloat("_Effect", (int)id);
                _material.SetFloat("_Darken", _fullScreen ? 0.38f : 0f);
                if (tex != null)
                {
                    _material.SetTexture("_MainTex", tex);
                    _material.SetFloat("_UseTex", 1f);
                }
                else
                {
                    _material.SetFloat("_UseTex", 0f);
                }
            }
            else if (tex != null)
            {
                _material.SetTexture("_BaseMap", tex);
                _material.SetTexture("_MainTex", tex);
                _material.color = _fullScreen ? new Color(0.62f, 0.62f, 0.62f, 1f) : Color.white;
            }

            Vector3 scale = transform.localScale;
            _material.SetFloat("_Aspect", scale.x / Mathf.Max(scale.y, 0.001f));
        }

        public static Texture2D TextureFor(GameBackgroundId id)
        {
            string path = id switch
            {
                GameBackgroundId.Waterfall => "Backgrounds/Waterfall",
                GameBackgroundId.Laboratory => "Backgrounds/Laboratory",
                GameBackgroundId.Aquarium => "Backgrounds/Aquarium",
                GameBackgroundId.Aurora => "Backgrounds/Aurora",
                _ => null
            };
            return string.IsNullOrEmpty(path) ? null : Resources.Load<Texture2D>(path);
        }

        void OnEnable()
        {
            GameSettings.BackgroundChanged += OnBackgroundChanged;
        }

        void OnDisable()
        {
            GameSettings.BackgroundChanged -= OnBackgroundChanged;
        }

        void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

        void Update()
        {
            if (_material != null)
                _material.SetFloat("_AnimTime", Time.time);

            if (!_fullScreen)
                return;
            if (_lastWidth == Screen.width && _lastHeight == Screen.height)
                return;
            FitCamera();
        }

        void OnBackgroundChanged()
        {
            Apply(GameSettings.Background);
        }

        void BuildRenderer()
        {
            var shader = Resources.Load<Shader>("Shaders/GameBackground")
                         ?? Shader.Find("NoBall/GameBackground");
            _customShader = shader != null && shader.isSupported;
            if (!_customShader)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                Debug.LogWarning("NoBall background shader missing.");
                return;
            }

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = 2900
            };
            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = QuadMesh();
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = _material;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            _meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _meshRenderer.sortingOrder = 1;
        }

        void FitCamera()
        {
            if (_camera == null || _material == null)
                return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            transform.position = new Vector3(_camera.transform.position.x, _camera.transform.position.y, 0.25f);
            transform.localScale = new Vector3(width, height, 1f);
            _material.SetFloat("_Aspect", width / Mathf.Max(height, 0.001f));
        }

        static Mesh QuadMesh()
        {
            var mesh = new Mesh
            {
                name = "BackgroundQuad",
                hideFlags = HideFlags.HideAndDontSave
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
