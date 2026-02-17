using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


namespace MobileControllerFramework
{
    public class DebugMenu : MonoBehaviour, IPointerClickHandler
    {
        [Header("Behaviour")]
        public bool displayByDefault = false;

        [Header("References")]
        public Transform entryParent;
        public GameObject entryType_1;
        public GameObject entryType_2;
        public GameObject content;

        private readonly Dictionary<string, List<TextMeshProUGUI>> textLists = new();
        private readonly Dictionary<string, List<MonoBehaviour>> componentLists = new();


        /**/


        #region Setup

        private void Start()
        {
            content.SetActive(displayByDefault);

            // Register all component types
            RegisterComponents<Analog>("Analogs");
            RegisterComponents<Dpad>("Dpads");
            RegisterComponents<ArrowKeys>("ArrowKeys");
            RegisterComponents<UnityEngine.UI.Button>("Buttons");
            RegisterComponents<Cooldown>("Cooldowns");
            RegisterComponents<Toggle>("Toggles");
            RegisterComponents<Swap>("Swaps");
            RegisterComponents<TouchDrag>("TouchDrags");

            // Instantiate UI entries
            foreach (var list in componentLists.Keys.ToList())
            {
                InstantiateEntries(list);
            }
        }


        private void RegisterComponents<T>(string key) where T : MonoBehaviour
        {
            var list = GameObject.FindObjectsOfType<T>().Cast<MonoBehaviour>().ToList();
            componentLists[key] = list;
            textLists[key] = new List<TextMeshProUGUI>();
        }


        private void InstantiateEntries(string key)
        {
            bool isType1 = true;

            foreach (var item in componentLists[key])
            {
                GameObject tmpObj = GameObject.Instantiate(isType1 ? entryType_1 : entryType_2, entryParent);
                textLists[key].Add(tmpObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>());
                isType1 = !isType1;
            }
        }

        #endregion


        #region Display

        private void Update()
        {
            UpdateEntries();
        }


        private void UpdateEntries()
        {
            foreach (var kvp in componentLists)
            {
                string key = kvp.Key;
                var comps = kvp.Value;
                var texts = textLists[key];

                for (int i = 0; i < comps.Count; i++)
                {
                    string status = GetComponentStatus(comps[i]);
                    texts[i].text = comps[i].name + ": " + status;
                }
            }
        }


        private string GetComponentStatus(MonoBehaviour comp)
        {
            switch (comp)
            {
                case Analog a:
                    return $"{a.GetAnalogState()} / {a.GetStickPosition()}";
                case Dpad d:
                    return $"{d.GetDpadState()} / {d.GetDpadDirection()}";
                case ArrowKeys ak:
                    return $"{ak.GetState()} / {ak.GetDirection()}";
                case UnityEngine.UI.Button b:
                    return $"{b.interactable}";
                case Cooldown c:
                    return $"{c.GetState()} / {c.GetTimeRemaining():F1} cooldown";
                case Toggle t:
                    return $"{t.GetToggleState()}";
                case Swap s:
                    return $"{s.GetSwapState()} / item {s.GetCurrentItem().name} at index {s.GetCurrentIndex()}";
                case TouchDrag td:
                    return $"{td.GetTouchDragState()} / movement {td.GetTouchDragMovement()}";
                default:
                    return "Unknown";
            }
        }


        public void ToggleContent()
        {
            if (content != null)
                content.SetActive(!content.activeSelf);
        }

        #endregion


        #region Tap Handling

        public void OnPointerClick(PointerEventData eventData)
        {
            ToggleContent();
        }

        #endregion
    }
}