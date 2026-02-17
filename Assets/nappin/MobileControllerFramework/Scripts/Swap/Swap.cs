using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace MobileControllerFramework
{
    /*
    * NOTE:
    * This component uses Unity's EventSystem (Pointer Events).
    * It works out of the box with both the Old Input Manager
    * and the New Input System — no additional setup required.
    */


    [RequireComponent(typeof(Animator))]
    public class Swap : MonoBehaviour
    {
        public enum InitialState { Normal, Disabled }
        public enum SwapState { Normal, Disabled }

        [Header("Behaviour")]
        [Tooltip("Initial state of the swap component.")]
        public InitialState initialState;

        [Tooltip("Should the swap cycle from last item to first and vice versa?")]
        public bool isCyclical = false;

        [Tooltip("List of items to swap.")]
        public List<SwapContent> items;

        [Min(0)]
        [Tooltip("Starting index in the items list.")]
        public int startIndex = 0;

        [Header("References")]
        public Swapper swapperPrev;
        public Swapper swapperNext;
        public Image iconImage;

        [Header("Events")]
        public UnityEvent SwapEvent;
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private SwapState swapState;
        private Animator anim;
        private int currentIndex = 0;


        /**/


        #region Setup

        private void Start()
        {
            anim = GetComponent<Animator>();
            currentIndex = Mathf.Clamp(startIndex, 0, items.Count - 1);

            swapState = initialState == InitialState.Normal ? SwapState.Normal : SwapState.Disabled;

            if (swapState == SwapState.Normal)
            {
                anim.SetTrigger("Normal");
                UpdateSwappers();
            }
            else
            {
                anim.SetTrigger("Disable");
                swapperPrev.SetState(false);
                swapperNext.SetState(false);
            }

            UpdateContent();
        }

        #endregion


        #region Swap Logic

        public void OnPrev()
        {
            if (swapState == SwapState.Disabled) return;

            if (currentIndex == 0 && !isCyclical) return;

            currentIndex--;
            if (currentIndex < 0 && isCyclical) currentIndex = items.Count - 1;

            SwapEvent.Invoke();
            anim.SetTrigger("Swap");

            UpdateContent();
            UpdateSwappers();
        }


        public void OnNext()
        {
            if (swapState == SwapState.Disabled) return;

            if (currentIndex == items.Count - 1 && !isCyclical) return;

            currentIndex++;
            if (currentIndex >= items.Count && isCyclical) currentIndex = 0;

            SwapEvent.Invoke();
            anim.SetTrigger("Swap");

            UpdateContent();
            UpdateSwappers();
        }

        #endregion


        #region Update Helpers

        private void UpdateContent()
        {
            if (iconImage != null && items.Count > 0)
                iconImage.sprite = items[currentIndex].contentImage;
        }


        private void UpdateSwappers()
        {
            if (!isCyclical)
            {
                swapperPrev.SetState(currentIndex != 0);
                swapperNext.SetState(currentIndex != items.Count - 1);
            }
            else
            {
                swapperPrev.SetState(true);
                swapperNext.SetState(true);
            }
        }

        #endregion


        #region Getters / Setters

        public SwapContent GetCurrentItem() => items[currentIndex];
        public int GetCurrentIndex() => currentIndex;
        public SwapState GetSwapState() => swapState;


        public void Enable()
        {
            EnableEvent.Invoke();
            swapState = SwapState.Normal;
            anim.SetTrigger("Normal");
            UpdateSwappers();
        }


        public void Disable()
        {
            DisableEvent.Invoke();
            swapState = SwapState.Disabled;
            anim.SetTrigger("Disable");
            swapperPrev.SetState(false);
            swapperNext.SetState(false);
        }


        public void SetActiveIndex(int index)
        {
            currentIndex = Mathf.Clamp(index, 0, items.Count - 1);
            anim.SetTrigger("Swap");
            UpdateContent();
            UpdateSwappers();
        }

        #endregion
    }
}