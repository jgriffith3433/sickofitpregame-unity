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


    public class TouchDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IDragHandler
    {
        public enum InitialState { normal, disable };
        public enum TouchDragState { normal, touch, disable };

        [Header("Behaviour")]
        public InitialState initialState;

        [Header("Events")]
        public UnityEvent TouchStartEvent;
        public UnityEvent TouchEndEvent;
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private Vector2 touchStartPosition = Vector2.zero;
        private Vector2 movementFromTouch = Vector2.zero;
        private TouchDragState touchDragState;


        /**/


        #region Setup

        private void Awake()
        {
            touchDragState = initialState == InitialState.normal ? TouchDragState.normal : TouchDragState.disable;
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (touchDragState != TouchDragState.normal) return;

            touchStartPosition = eventData.position;
            movementFromTouch = Vector2.zero;

            touchDragState = TouchDragState.touch;
            TouchStartEvent.Invoke();
        }


        public void OnDrag(PointerEventData eventData)
        {
            if (touchDragState != TouchDragState.touch) return;

            movementFromTouch = eventData.position - touchStartPosition;
        }


        public void OnPointerUp(PointerEventData eventData) => PointerUp();
        public void OnPointerExit(PointerEventData eventData) => PointerUp();


        private void PointerUp()
        {
            if (touchDragState != TouchDragState.touch) return;

            touchDragState = TouchDragState.normal;
            movementFromTouch = Vector2.zero;
            touchStartPosition = Vector2.zero;

            TouchEndEvent.Invoke();
        }

        #endregion


        #region Getters / Setters

        public Vector2 GetTouchDragMovement() => movementFromTouch;
        public TouchDragState GetTouchDragState() => touchDragState;


        public void Enable()
        {
            EnableEvent.Invoke();
            touchDragState = TouchDragState.normal;
            touchStartPosition = Vector2.zero;
            movementFromTouch = Vector2.zero;
        }


        public void Disable()
        {
            DisableEvent.Invoke();
            touchDragState = TouchDragState.disable;
            touchStartPosition = Vector2.zero;
            movementFromTouch = Vector2.zero;
        }

        #endregion
    }
}