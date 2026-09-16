using UnityEngine;
using UnityEngine.EventSystems;

namespace NoBall
{
    public sealed class DeviceUiInputModule : PointerInputModule
    {
        public override bool IsModuleSupported() => true;

        public override bool ShouldActivateModule() => enabled && gameObject.activeInHierarchy;

        public override void Process()
        {
            if (!eventSystem.isActiveAndEnabled)
                return;

            ProcessPointer(
                GamePointer.Position,
                GamePointer.LeftPressedThisFrame,
                GamePointer.LeftReleasedThisFrame);
        }

        void ProcessPointer(Vector2 position, bool pressed, bool released)
        {
            GetPointerData(kMouseLeftId, out var data, true);
            data.Reset();
            data.delta = position - data.position;
            data.position = position;
            data.button = PointerEventData.InputButton.Left;

            eventSystem.RaycastAll(data, m_RaycastResultCache);
            data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();

            ProcessMove(data);
            ProcessPress(data, pressed, released);
            if (!released)
                ProcessDrag(data);
        }

        void ProcessPress(PointerEventData data, bool pressed, bool released)
        {
            var currentOver = data.pointerCurrentRaycast.gameObject;

            if (pressed)
            {
                data.eligibleForClick = true;
                data.delta = Vector2.zero;
                data.dragging = false;
                data.useDragThreshold = true;
                data.pressPosition = data.position;
                data.pointerPressRaycast = data.pointerCurrentRaycast;
                DeselectIfSelectionChanged(currentOver, data);

                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOver, data, ExecuteEvents.pointerDownHandler);
                var newClick = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOver);
                if (newPressed == null)
                    newPressed = newClick;

                data.pointerPress = newPressed;
                data.rawPointerPress = currentOver;
                data.pointerClick = newClick;
                data.clickTime = Time.unscaledTime;
                data.clickCount = 1;
                data.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOver);
                if (data.pointerDrag != null)
                    ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.initializePotentialDrag);
            }

            if (!released)
                return;

            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);
            var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOver);
            if (data.pointerClick == clickHandler && data.eligibleForClick)
                ExecuteEvents.Execute(data.pointerClick, data, ExecuteEvents.pointerClickHandler);

            if (data.pointerDrag != null && data.dragging)
                ExecuteEvents.ExecuteHierarchy(currentOver, data, ExecuteEvents.dropHandler);

            data.eligibleForClick = false;
            data.pointerPress = null;
            data.rawPointerPress = null;
            data.pointerClick = null;
            if (data.pointerDrag != null && data.dragging)
                ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.endDragHandler);
            data.dragging = false;
            data.pointerDrag = null;
        }
    }
}
