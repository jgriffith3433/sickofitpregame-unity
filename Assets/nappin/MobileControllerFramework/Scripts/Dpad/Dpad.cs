using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


namespace MobileControllerFramework
{
    /*
    * NOTE:
    * This component uses Unity's EventSystem (Pointer Events).
    * It works out of the box with both the Old Input Manager
    * and the New Input System — no additional setup required.
    */


    public class Dpad : MonoBehaviour, IPointerExitHandler
    {
        public enum InitialState { Normal, Disabled }
        public enum DpadState { Normal, Disabled }

        [Header("Behaviour")]
        [Tooltip("Initial state of the D-pad.")]
        public InitialState initialState;

        [Header("References")]
        public Direction upDirection;
        public Direction rightDirection;
        public Direction downDirection;
        public Direction leftDirection;

        [Header("Diagonal References")]
        public Direction upRightDirection;
        public Direction upLeftDirection;
        public Direction downRightDirection;
        public Direction downLeftDirection;

        [Header("Events")]
        public UnityEvent UpTapEvent;
        public UnityEvent UpExitEvent;
        public UnityEvent RightTapEvent;
        public UnityEvent RightExitEvent;
        public UnityEvent DownTapEvent;
        public UnityEvent DownExitEvent;
        public UnityEvent LeftTapEvent;
        public UnityEvent LeftExitEvent;
        public UnityEvent ExitDpadEvent;
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private Vector2Int tappedDirections = Vector2Int.zero;
        private DpadState dpadState;


        /**/


        #region Setup

        private void Start()
        {
            dpadState = initialState == InitialState.Normal ? DpadState.Normal : DpadState.Disabled;

            upDirection.SetState(dpadState);
            rightDirection.SetState(dpadState);
            downDirection.SetState(dpadState);
            leftDirection.SetState(dpadState);
        }

        #endregion


        #region Pointer Events

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearDirections();
            eventData.pointerPress = null;
        }

        #endregion


        #region Update Directions

        public void ClearDirections()
        {
            SetActiveDirections(false, false, false, false);
            ExitDpadEvent.Invoke();
        }


        public void UpdateDirections()
        {
            HandleDrag(
                upDirection.GetDragged(),
                rightDirection.GetDragged(),
                downDirection.GetDragged(),
                leftDirection.GetDragged(),
                upRightDirection.GetDragged(),
                upLeftDirection.GetDragged(),
                downRightDirection.GetDragged(),
                downLeftDirection.GetDragged()
            );
        }


        private void HandleDrag(bool up, bool right, bool down, bool left, bool upRight, bool upLeft, bool downRight, bool downLeft)
        {
            if (up) SetActiveDirections(true, false, false, false);
            else if (upRight) SetActiveDirections(true, true, false, false);
            else if (right) SetActiveDirections(false, true, false, false);
            else if (downRight) SetActiveDirections(false, true, true, false);
            else if (down) SetActiveDirections(false, false, true, false);
            else if (downLeft) SetActiveDirections(false, false, true, true);
            else if (left) SetActiveDirections(false, false, false, true);
            else if (upLeft) SetActiveDirections(true, false, false, true);
            else SetActiveDirections(false, false, false, false);
        }


        public void SetActiveDirections(bool upActive, bool rightActive, bool downActive, bool leftActive)
        {
            if (dpadState == DpadState.Disabled) return;

            Vector2Int newTappedDirections = Vector2Int.zero;

            if (upActive) newTappedDirections.y = 1;
            else if (downActive) newTappedDirections.y = -1;

            if (rightActive) newTappedDirections.x = 1;
            else if (leftActive) newTappedDirections.x = -1;

            // Invoke events only on state change
            if (newTappedDirections.y == 1 && tappedDirections.y != 1) UpTapEvent.Invoke();
            else if (newTappedDirections.y != 1 && tappedDirections.y == 1) UpExitEvent.Invoke();

            if (newTappedDirections.x == 1 && tappedDirections.x != 1) RightTapEvent.Invoke();
            else if (newTappedDirections.x != 1 && tappedDirections.x == 1) RightExitEvent.Invoke();

            if (newTappedDirections.y == -1 && tappedDirections.y != -1) DownTapEvent.Invoke();
            else if (newTappedDirections.y != -1 && tappedDirections.y == -1) DownExitEvent.Invoke();

            if (newTappedDirections.x == -1 && tappedDirections.x != -1) LeftTapEvent.Invoke();
            else if (newTappedDirections.x != -1 && tappedDirections.x == -1) LeftExitEvent.Invoke();

            tappedDirections = newTappedDirections;

            // Update direction active states
            upDirection.SetDirectionState(upActive);
            rightDirection.SetDirectionState(rightActive);
            downDirection.SetDirectionState(downActive);
            leftDirection.SetDirectionState(leftActive);
        }

        #endregion


        #region Getters / Setters

        public DpadState GetDpadState() => dpadState;
        public Vector2Int GetDpadDirection() => tappedDirections;


        public void Enable()
        {
            EnableEvent.Invoke();
            dpadState = DpadState.Normal;

            upDirection.SetState(dpadState);
            rightDirection.SetState(dpadState);
            downDirection.SetState(dpadState);
            leftDirection.SetState(dpadState);
        }


        public void Disable()
        {
            DisableEvent.Invoke();
            dpadState = DpadState.Disabled;

            upDirection.SetState(dpadState);
            rightDirection.SetState(dpadState);
            downDirection.SetState(dpadState);
            leftDirection.SetState(dpadState);
        }

        #endregion
    }
}