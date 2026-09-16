using UnityEngine;
using UnityEngine.InputSystem;

namespace NoBall
{
    public static class GamePointer
    {
        static int _legacy = 0;

        public static Vector2 Position
        {
            get
            {
                if (LegacyAvailable)
                    return Input.mousePosition;
                if (Mouse.current != null)
                    return Mouse.current.position.ReadValue();
                if (Pointer.current != null)
                    return Pointer.current.position.ReadValue();
                return default;
            }
        }

        public static bool LeftPressedThisFrame =>
            (LegacyAvailable && Input.GetMouseButtonDown(0))
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Mouse.current == null && Pointer.current != null && Pointer.current.press.wasPressedThisFrame);

        public static bool LeftReleasedThisFrame =>
            (LegacyAvailable && Input.GetMouseButtonUp(0))
            || (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            || (Mouse.current == null && Pointer.current != null && Pointer.current.press.wasReleasedThisFrame);

        public static bool RightPressedThisFrame =>
            (LegacyAvailable && Input.GetMouseButtonDown(1))
            || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);

        static bool LegacyAvailable
        {
            get
            {
                if (_legacy != 0)
                    return _legacy > 0;
                try
                {
                    _ = Input.mousePosition;
                    _legacy = 1;
                    return true;
                }
                catch (System.InvalidOperationException)
                {
                    _legacy = -1;
                    return false;
                }
            }
        }
    }
}
