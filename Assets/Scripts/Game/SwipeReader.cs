using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NoBall
{
    public sealed class SwipeReader : MonoBehaviour
    {
        Camera _camera;
        bool _dragging;
        Vector2 _startScreen;
        Vector2 _currentScreen;
        readonly List<RaycastResult> _raycastHits = new List<RaycastResult>();

        public bool IsDragging => _dragging;
        public Vector2 StartWorld { get; private set; }
        public Vector2 CurrentWorld { get; private set; }
        public Vector2 ScreenDelta => _currentScreen - _startScreen;

        public event Action<Vector2, Vector2> Swiped;

        public void Build(Camera camera)
        {
            _camera = camera;
        }

        public void Cancel()
        {
            _dragging = false;
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null)
                return;

            var position = pointer.position.ReadValue();
            if (pointer.press.wasPressedThisFrame)
            {
                if (IsOverUi(position))
                    return;
                _dragging = true;
                _startScreen = position;
                _currentScreen = position;
                StartWorld = ScreenToWorld(position);
                CurrentWorld = StartWorld;
            }
            else if (_dragging && pointer.press.isPressed)
            {
                _currentScreen = position;
                CurrentWorld = ScreenToWorld(position);
            }
            else if (_dragging && pointer.press.wasReleasedThisFrame)
            {
                _dragging = false;
                _currentScreen = position;
                CurrentWorld = ScreenToWorld(position);
                var delta = _currentScreen - _startScreen;
                if (delta.magnitude < GameConfig.SwipeMinPixels)
                    return;
                Swiped?.Invoke(StartWorld, CurrentWorld);
            }
        }

        Vector2 ScreenToWorld(Vector2 screen)
        {
            float z = Mathf.Abs(_camera.transform.position.z);
            return _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        }

        bool IsOverUi(Vector2 screen)
        {
            var es = EventSystem.current;
            if (es == null)
                return false;
            var data = new PointerEventData(es) { position = screen };
            _raycastHits.Clear();
            es.RaycastAll(data, _raycastHits);
            return _raycastHits.Count > 0;
        }
    }
}
