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


    public class ArrowKeys : MonoBehaviour, IPointerExitHandler
    {
        public enum InitialState { Normal, Disabled }
        public enum ArrowKeysState { Normal, Disabled }

        [Header("Behaviour")]
        [Tooltip("Initial state of the arrow keys.")]
        public InitialState initialState;

        [Header("References")]
        public Key upKey;
        public Key rightKey;
        public Key downKey;
        public Key leftKey;

        [Header("Events")]
        public UnityEvent UpTapEvent;
        public UnityEvent UpExitEvent;
        public UnityEvent RightTapEvent;
        public UnityEvent RightExitEvent;
        public UnityEvent DownTapEvent;
        public UnityEvent DownExitEvent;
        public UnityEvent LeftTapEvent;
        public UnityEvent LeftExitEvent;
        public UnityEvent ExitArrowKeysEvent;
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private Vector2Int tappedKeys = Vector2Int.zero;
        private ArrowKeysState arrowKeysState;


        /**/


        #region Setup

        private void Start()
        {
            arrowKeysState = initialState == InitialState.Normal ? ArrowKeysState.Normal : ArrowKeysState.Disabled;
            upKey.SetState(arrowKeysState);
            rightKey.SetState(arrowKeysState);
            downKey.SetState(arrowKeysState);
            leftKey.SetState(arrowKeysState);
        }

        #endregion


        #region Pointer Events

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearKeys();
            eventData.pointerPress = null;
        }

        #endregion


        #region Update Keys

        public void ClearKeys()
        {
            SetActiveKeys(false, false, false, false);
            ExitArrowKeysEvent.Invoke();
        }


        public void UpdateKeys()
        {
            HandleDrag(upKey.IsPressed(), rightKey.IsPressed(), downKey.IsPressed(), leftKey.IsPressed());
        }


        private void HandleDrag(bool up, bool right, bool down, bool left)
        {
            if (up)
            {
                if (left) SetActiveKeys(true, false, false, true);
                else if (right) SetActiveKeys(true, true, false, false);
                else SetActiveKeys(true, false, false, false);
            }
            else if (right)
            {
                if (down) SetActiveKeys(false, true, true, false);
                else SetActiveKeys(false, true, false, false);
            }
            else if (down)
            {
                if (left) SetActiveKeys(false, false, true, true);
                else SetActiveKeys(false, false, true, false);
            }
            else if (left) SetActiveKeys(false, false, false, true);
            else SetActiveKeys(false, false, false, false);
        }


        public void SetActiveKeys(bool upActive, bool rightActive, bool downActive, bool leftActive)
        {
            if (arrowKeysState == ArrowKeysState.Disabled) return;

            Vector2Int newTappedKeys = Vector2Int.zero;
            if (upActive) newTappedKeys.y = 1;
            else if (downActive) newTappedKeys.y = -1;

            if (rightActive) newTappedKeys.x = 1;
            else if (leftActive) newTappedKeys.x = -1;

            // invoke events only when state changes
            if (newTappedKeys.y == 1 && tappedKeys.y != 1) UpTapEvent.Invoke();
            else if (newTappedKeys.y != 1 && tappedKeys.y == 1) UpExitEvent.Invoke();

            if (newTappedKeys.x == 1 && tappedKeys.x != 1) RightTapEvent.Invoke();
            else if (newTappedKeys.x != 1 && tappedKeys.x == 1) RightExitEvent.Invoke();

            if (newTappedKeys.y == -1 && tappedKeys.y != -1) DownTapEvent.Invoke();
            else if (newTappedKeys.y != -1 && tappedKeys.y == -1) DownExitEvent.Invoke();

            if (newTappedKeys.x == -1 && tappedKeys.x != -1) LeftTapEvent.Invoke();
            else if (newTappedKeys.x != -1 && tappedKeys.x == -1) LeftExitEvent.Invoke();

            tappedKeys = newTappedKeys;

            upKey.SetKeyState(upActive);
            rightKey.SetKeyState(rightActive);
            downKey.SetKeyState(downActive);
            leftKey.SetKeyState(leftActive);
        }

        #endregion


        #region Getters / Setters

        public ArrowKeysState GetState() => arrowKeysState;
        public Vector2Int GetDirection() => tappedKeys;


        public void Enable()
        {
            EnableEvent.Invoke();
            arrowKeysState = ArrowKeysState.Normal;
            upKey.SetState(arrowKeysState);
            rightKey.SetState(arrowKeysState);
            downKey.SetState(arrowKeysState);
            leftKey.SetState(arrowKeysState);
        }


        public void Disable()
        {
            DisableEvent.Invoke();
            arrowKeysState = ArrowKeysState.Disabled;
            upKey.SetState(arrowKeysState);
            rightKey.SetState(arrowKeysState);
            downKey.SetState(arrowKeysState);
            leftKey.SetState(arrowKeysState);
        }

        #endregion
    }
}