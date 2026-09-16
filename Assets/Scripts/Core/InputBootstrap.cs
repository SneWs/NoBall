using UnityEngine;
using UnityEngine.InputSystem;

namespace NoBall
{
    static class InputBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            ApplyLinuxWindow();
            EnablePointers();
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterScene()
        {
            ApplyLinuxWindow();
            EnablePointers();
        }

        static void ApplyLinuxWindow()
        {
            if (Application.platform != RuntimePlatform.LinuxPlayer)
                return;
            Screen.fullScreen = false;
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }

        static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected || change == InputDeviceChange.Enabled)
                EnableIfPointer(device);
        }

        static void EnablePointers()
        {
            EnableIfPointer(Mouse.current);
            EnableIfPointer(Pen.current);
            EnableIfPointer(Touchscreen.current);
            EnableIfPointer(Pointer.current);
        }

        static void EnableIfPointer(InputDevice device)
        {
            if (device != null && !device.enabled)
                InputSystem.EnableDevice(device);
        }
    }
}
