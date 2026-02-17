using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
    public class Cooldown : MonoBehaviour, IPointerDownHandler
    {
        public enum FillType { Radial, Horizontal, Vertical }
        public enum FillMode { Refill, Empty }
        public enum InitialState { Normal, Disabled }
        public enum CooldownState { Normal, Cooldown, Disabled }

        [Header("Behaviour")]
        [Tooltip("Initial state of the cooldown button.")]
        public InitialState initialState = InitialState.Normal;

        [Tooltip("Cooldown duration in seconds.")]
        [Min(0f)] public float cooldownTime = 2f;

        [Header("Text")]
        [Tooltip("Enable countdown text display.")]
        public bool enableText = false;
        [Min(0.01f)] public int decimalPlaces = 1;

        [Header("Fill")]
        [Tooltip("Enable image fill to visualize cooldown.")]
        public bool enableFill = true;
        public FillMode fillMode = FillMode.Refill;
        public FillType fillType = FillType.Radial;

        [Header("References")]
        public Image fillImage;
        public TextMeshProUGUI countdownText;

        [Header("Events")]
        public UnityEvent CooldownStartEvent;
        public UnityEvent CooldownEndEvent;
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private CooldownState cooldownState;
        private Animator animator;
        private float currentTime;


        /**/


        #region Setup

        private void Awake()
        {
            animator = GetComponent<Animator>();
            SetupFillImage();
            SetInitialState();
            ResetCooldown();
        }


        private void SetupFillImage()
        {
            if (fillImage == null) return;

            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = fillType switch
            {
                FillType.Radial => Image.FillMethod.Radial360,
                FillType.Horizontal => Image.FillMethod.Horizontal,
                FillType.Vertical => Image.FillMethod.Vertical,
                _ => Image.FillMethod.Radial360
            };
        }


        private void SetInitialState()
        {
            if (initialState == InitialState.Normal)
            {
                animator.SetTrigger("Normal");
                cooldownState = CooldownState.Normal;
            }
            else
            {
                animator.SetTrigger("Disable");
                cooldownState = CooldownState.Disabled;
            }
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (cooldownState == CooldownState.Cooldown || cooldownState == CooldownState.Disabled) return;

            CooldownStartEvent.Invoke();
            animator.SetTrigger("Cooldown");
            cooldownState = CooldownState.Cooldown;
        }

        #endregion


        #region Update

        private void Update()
        {
            if (cooldownState != CooldownState.Cooldown) return;

            currentTime -= Time.deltaTime;

            if (enableText) UpdateText(false);
            if (enableFill) UpdateFill(false);

            if (currentTime <= 0f)
            {
                CooldownEndEvent.Invoke();
                animator.SetTrigger("Normal");
                cooldownState = CooldownState.Normal;
                ResetCooldown();
            }
        }


        private void UpdateFill(bool reset)
        {
            if (fillImage == null) return;

            if (reset)
            {
                fillImage.fillAmount = 0f;
                return;
            }

            fillImage.fillAmount = fillMode == FillMode.Empty
                ? currentTime / cooldownTime
                : 1f - currentTime / cooldownTime;
        }


        private void UpdateText(bool reset)
        {
            if (countdownText == null) return;

            countdownText.text = reset ? " " : currentTime.ToString($"F{decimalPlaces}");
        }


        private void ResetCooldown()
        {
            currentTime = cooldownTime;
            UpdateText(true);
            UpdateFill(true);
        }

        #endregion


        #region Getters / Setters

        public CooldownState GetState() => cooldownState;
        public float GetTimeRemaining() => currentTime;


        public void ClearCooldown()
        {
            CooldownEndEvent.Invoke();
            animator.SetTrigger("Normal");
            cooldownState = CooldownState.Normal;
            ResetCooldown();
        }


        public void Enable()
        {
            EnableEvent.Invoke();
            animator.SetTrigger("Normal");
            cooldownState = CooldownState.Normal;
        }


        public void Disable()
        {
            DisableEvent.Invoke();
            animator.SetTrigger("Disable");
            cooldownState = CooldownState.Disabled;
        }

        #endregion


        public void TriggerCooldown()
        {
            if (cooldownState != CooldownState.Cooldown && cooldownState != CooldownState.Disabled)
            {
                CooldownStartEvent.Invoke();
                animator.SetTrigger("Cooldown");
                cooldownState = CooldownState.Cooldown;
            }
        }
    }
}