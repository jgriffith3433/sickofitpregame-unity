// using UnityEngine;
// using TMPro;
// using UnityEngine.UI;
// using System.Collections.Generic;
// using UnityInput = UnityEngine.Input;

// namespace SickOfIt.Core.Addressables
// {
//     /// <summary>
//     /// Shows what's currently being preloaded in the background
//     /// Auto-creates UI in the corner of the screen
//     /// </summary>
//     public class PreloadStatusIndicator : MonoBehaviour
//     {
//         private static PreloadStatusIndicator _instance;
//         public static PreloadStatusIndicator Instance
//         {
//             get
//             {
//                 if (_instance == null)
//                 {
//                     GameObject obj = new GameObject("PreloadStatusIndicator");
//                     _instance = obj.AddComponent<PreloadStatusIndicator>();
//                     DontDestroyOnLoad(obj);
//                     _instance.Initialize();
//                 }
//                 return _instance;
//             }
//         }

//         [Header("Settings")]
//         [SerializeField] private bool showIndicator = true;
//         [SerializeField] private float updateInterval = 1.0f; // Update every 1 second to reduce allocations

//         private Canvas canvas;
//         private GameObject indicatorPanel;
//         private TextMeshProUGUI statusText;
//         private float lastUpdateTime;
//         private System.Text.StringBuilder statusBuilder = new System.Text.StringBuilder(500);
//         private string cachedPlatformLimit = "";
//         private List<string> cachedActiveOps = new List<string>(); // Reuse to avoid allocations

//         private void Awake()
//         {
//             if (_instance != null && _instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//             _instance = this;
//             DontDestroyOnLoad(gameObject);
//         }

//         private void Initialize()
//         {
//             // Create Canvas
//             canvas = gameObject.AddComponent<Canvas>();
//             canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//             canvas.sortingOrder = 9998; // Below debug console but above game UI

//             CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
//             scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//             scaler.referenceResolution = new Vector2(1920, 1080);

//             gameObject.AddComponent<GraphicRaycaster>();

//             // Create indicator panel (top-right corner)
//             indicatorPanel = new GameObject("IndicatorPanel");
//             indicatorPanel.transform.SetParent(transform);

//             RectTransform panelRect = indicatorPanel.AddComponent<RectTransform>();
//             panelRect.anchorMin = new Vector2(1, 1);
//             panelRect.anchorMax = new Vector2(1, 1);
//             panelRect.pivot = new Vector2(1, 1);
//             panelRect.anchoredPosition = new Vector2(-10, -10);
//             panelRect.sizeDelta = new Vector2(450, 220);

//             Image panelImage = indicatorPanel.AddComponent<Image>();
//             panelImage.color = new Color(0, 0, 0, 0.7f);

//             // Create status text
//             GameObject textObj = new GameObject("StatusText");
//             textObj.transform.SetParent(indicatorPanel.transform);

//             RectTransform textRect = textObj.AddComponent<RectTransform>();
//             textRect.anchorMin = Vector2.zero;
//             textRect.anchorMax = Vector2.one;
//             textRect.offsetMin = new Vector2(10, 10);
//             textRect.offsetMax = new Vector2(-10, -10);

//             statusText = textObj.AddComponent<TextMeshProUGUI>();
//             statusText.fontSize = 18;
//             statusText.color = Color.white;
//             statusText.alignment = TextAlignmentOptions.TopLeft;
//             statusText.enableWordWrapping = true;
//             statusText.text = "[Preload Status]\nIdle";

//             if (!showIndicator)
//             {
//                 indicatorPanel.SetActive(false);
//             }

//             // Initialized

//             // Cache platform limit string (never changes)
//             cachedPlatformLimit = GetPlatformMemoryLimit();
//         }

//         private void Update()
//         {
//             if (!showIndicator || indicatorPanel == null) return;

//             // Toggle with 'O' key (for "Operations")
//             if (UnityInput.GetKeyDown(KeyCode.O))
//             {
//                 ToggleVisibility();
//                 // Toggled visibility
//             }

//             // Update at intervals to avoid performance issues
//             if (Time.unscaledTime - lastUpdateTime > updateInterval)
//             {
//                 lastUpdateTime = Time.unscaledTime;
//                 UpdateStatus();
//             }
//         }

//         private void UpdateStatus()
//         {
//             if (!showIndicator)
//             {
//                 if (indicatorPanel != null)
//                     indicatorPanel.SetActive(false);
//                 return;
//             }

//             // Use StringBuilder to avoid List<string> and string.Join allocations
//             statusBuilder.Clear();

//             // Get memory info
//             AddressablesOperationManager.Instance.GetMemoryInfo(out long totalMB, out long usedMB, out long monoUsedMB, out long monoHeapMB);

//             // Memory section
//             statusBuilder.AppendLine("[Memory Status]");
//             statusBuilder.Append("Total Alloc: ").Append(totalMB).AppendLine("MB");
//             statusBuilder.Append("Reserved: ").Append(usedMB).AppendLine("MB");
//             statusBuilder.Append("Mono: ").Append(monoUsedMB).Append("MB / ").Append(monoHeapMB).AppendLine("MB");

//             // Platform memory limit (cached)
//             if (!string.IsNullOrEmpty(cachedPlatformLimit))
//             {
//                 statusBuilder.Append("Platform Limit: ~").AppendLine(cachedPlatformLimit);
//             }

//             statusBuilder.AppendLine();

//             // Preload operations section
//             cachedActiveOps.Clear();
//             cachedActiveOps.AddRange(AddressablesOperationManager.Instance.GetActiveOperations());

//             statusBuilder.AppendLine("[Preload Status]");

//             if (cachedActiveOps.Count == 0)
//             {
//                 statusBuilder.AppendLine("Idle");
//             }
//             else
//             {
//                 foreach (var op in cachedActiveOps)
//                 {
//                     if (op.StartsWith("Load:"))
//                     {
//                         string label = op.Substring(6);
//                         statusBuilder.Append("Preloading: ").AppendLine(label);
//                     }
//                     else if (op.StartsWith("Scene:"))
//                     {
//                         string sceneName = op.Substring(7);
//                         float progress = AddressablesOperationManager.Instance.GetCurrentSceneLoadProgress(sceneName);
//                         statusBuilder.Append("Loading: ").AppendLine(sceneName);
//                         statusBuilder.Append("  Progress: ").Append((progress * 100f).ToString("F1")).AppendLine("%");
//                     }
//                     else
//                     {
//                         statusBuilder.Append("Loading: ").AppendLine(op);
//                     }
//                 }
//             }

//             if (statusText != null)
//             {
//                 statusText.text = statusBuilder.ToString();
//             }

//             if (indicatorPanel != null)
//             {
//                 indicatorPanel.SetActive(true); // Always show when enabled
//             }
//         }

//         private string GetPlatformMemoryLimit()
//         {
//             // Estimate memory limits for different platforms
//             #if UNITY_WEBGL
//                 return "300-500MB"; // Safari iOS is very restrictive
//             #elif UNITY_IOS
//                 return "Varies by device";
//             #elif UNITY_ANDROID
//                 return "Varies by device";
//             #elif UNITY_STANDALONE
//                 return "System RAM";
//             #else
//                 return "";
//             #endif
//         }

//         public void SetVisible(bool visible)
//         {
//             showIndicator = visible;
//             if (indicatorPanel != null)
//             {
//                 // Check if there are any operations without allocating a new list
//                 bool hasOperations = AddressablesOperationManager.Instance.GetActiveOperationsCount() > 0;
//                 indicatorPanel.SetActive(visible && hasOperations);
//             }
//         }

//         public void ToggleVisibility()
//         {
//             SetVisible(!showIndicator);
//         }

//         /// <summary>
//         /// Call this to manually refresh the status display
//         /// </summary>
//         public static void RefreshStatus()
//         {
//             if (_instance != null)
//             {
//                 _instance.UpdateStatus();
//             }
//         }

//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
//         private static void AutoInitialize()
//         {
//             // Automatically create indicator on game start
//             var instance = Instance;
//         }
//     }
// }
