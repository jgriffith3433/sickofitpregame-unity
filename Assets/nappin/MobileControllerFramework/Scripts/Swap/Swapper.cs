using UnityEngine;
using UnityEngine.EventSystems;


namespace MobileControllerFramework
{
    /*
    * NOTE:
    * This component uses Unity's EventSystem (Pointer Events).
    * It works out of the box with both the Old Input Manager
    * and the New Input System — no additional setup required.
    */


    [RequireComponent(typeof(Animator))]
    public class Swapper : MonoBehaviour, IPointerDownHandler
    {
        public enum ScrollDirection { Prev, Next }

        [Header("References")]
        public ScrollDirection scrollDirection;
        public Swap buttonSwap;

        private Animator anim;


        /**/


        #region Setup

        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (buttonSwap == null) return;

            if (scrollDirection == ScrollDirection.Prev)
                buttonSwap.OnPrev();
            else
                buttonSwap.OnNext();
        }

        #endregion


        #region Getters / Setters

        public void SetState(bool enable)
        {
            anim.SetTrigger(enable ? "Normal" : "Disable");
        }

        #endregion
    }
}