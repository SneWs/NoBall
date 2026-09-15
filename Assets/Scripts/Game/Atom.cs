using UnityEngine;

namespace NoBall
{
    public sealed class Atom : MonoBehaviour
    {
        Rigidbody2D _body;
        float _speed;
        float _signX = 1f;
        float _signY = 1f;
        bool _paused;

        public System.Action Bounced;

        public Vector2 Position => _body != null ? _body.position : (Vector2)transform.position;
        public float Radius { get; private set; }

        public void Build(Vector2 position, float radius, float speed, PhysicsMaterial2D material, Vector2 direction)
        {
            Radius = radius;
            _speed = speed;
            transform.position = position;

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Atom;
            renderer.sharedMaterial = SpriteFactory.SpriteMaterial;
            renderer.sortingOrder = 10;
            float diameter = radius * 2f;
            transform.localScale = Vector3.one * diameter;

            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
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
            ApplyVelocity();
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

        void FixedUpdate()
        {
            if (_paused || _body == null)
                return;

            var velocity = _body.linearVelocity;
            bool bounced = false;
            if (Mathf.Abs(velocity.x) > 0.01f)
            {
                float sign = Mathf.Sign(velocity.x);
                if (sign != _signX)
                    bounced = true;
                _signX = sign;
            }

            if (Mathf.Abs(velocity.y) > 0.01f)
            {
                float sign = Mathf.Sign(velocity.y);
                if (sign != _signY)
                    bounced = true;
                _signY = sign;
            }

            if (bounced)
                Bounced?.Invoke();
            ApplyVelocity();
        }

        void ApplyVelocity()
        {
            _body.linearVelocity = new Vector2(_signX, _signY).normalized * _speed;
        }
    }
}
