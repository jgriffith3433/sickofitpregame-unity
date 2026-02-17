#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SickOfIt.VideoFrameExtractor.Runtime;

namespace SickOfIt.VideoFrameExtractor.Editor
{
    [CustomEditor(typeof(SpriteFramePlayer))]
    public class SpriteFramePlayerEditor : UnityEditor.Editor
    {
        private SpriteFramePlayer player;
        private SerializedProperty spriteFramesDataProp;
        private SerializedProperty preloadOnAwakeProp;
        private SerializedProperty framesProp;
        private SerializedProperty isLoadedProp;
        private SerializedProperty allChunksPreloadedProp;
        private SerializedProperty loadedChunksProp;

        private void OnEnable()
        {
            player = (SpriteFramePlayer)target;
            spriteFramesDataProp = serializedObject.FindProperty("spriteFramesData");
            preloadOnAwakeProp = serializedObject.FindProperty("preloadOnAwake");
            framesProp = serializedObject.FindProperty("frames");
            isLoadedProp = serializedObject.FindProperty("isLoaded");
            allChunksPreloadedProp = serializedObject.FindProperty("allChunksPreloaded");
            loadedChunksProp = serializedObject.FindProperty("loadedChunks");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Chunked Loading Info", EditorStyles.boldLabel);

            // Show loaded frame count and chunk info
            int frameCount = framesProp.arraySize;
            bool isLoaded = isLoadedProp.boolValue;
            bool allChunksPreloaded = allChunksPreloadedProp.boolValue;
            int loadedChunksCount = loadedChunksProp.arraySize;

            string status;
            MessageType messageType;

            if (isLoaded)
            {
                status = $"✓ Ready: {frameCount} frames in current chunk\nAll chunks cached: {(allChunksPreloaded ? "Yes" : "No")}\nLoaded chunks: {loadedChunksCount}";
                messageType = MessageType.Info;
            }
            else
            {
                status = "Not loaded yet - call PreloadAllChunks()";
                messageType = MessageType.Warning;
            }

            EditorGUILayout.HelpBox(status, messageType);

            // Manual load button (if not preloading on awake)
            if (!Application.isPlaying && !preloadOnAwakeProp.boolValue)
            {
                EditorGUILayout.HelpBox("Preload On Awake is disabled. Call PreloadAllChunks() manually.", MessageType.Info);
            }

            // Runtime controls
            if (Application.isPlaying)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Playback Controls (Runtime)", EditorStyles.boldLabel);

                // Manual preload button if not loaded
                if (!isLoaded && GUILayout.Button("Preload All Chunks Now", GUILayout.Height(30)))
                {
                    player.PreloadAllChunks();
                }

                if (isLoaded)
                {
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Play"))
                    {
                        player.Play();
                    }

                    if (GUILayout.Button("Pause"))
                    {
                        player.Pause();
                    }

                    if (GUILayout.Button("Stop"))
                    {
                        player.Stop();
                    }

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    // Frame scrubber
                    int totalFrames = player.GetTotalFrames();
                    if (totalFrames > 0)
                    {
                        int currentFrame = player.GetCurrentFrame();
                        int newFrame = EditorGUILayout.IntSlider("Scrub Frame", currentFrame, 0, totalFrames - 1);

                        if (newFrame != currentFrame)
                        {
                            player.SetFrame(newFrame);
                        }

                        EditorGUILayout.LabelField($"Progress: {player.GetProgress() * 100:F1}%");
                        EditorGUILayout.LabelField($"Playing: {(player.IsPlaying() ? "Yes" : "No")}");
                    }
                }
            }

            GUILayout.Space(10);

            // Updated help box for chunked loading
            EditorGUILayout.HelpBox(
                "CHUNKED LOADING SYSTEM:\n\n" +
                "1. Create a SpriteFramesData asset (Right-click → Create → Video → Sprite Frames Data)\n" +
                "2. Open Tools → Sprite Frames Manager\n" +
                "3. Select all your frames in Project window\n" +
                "4. Auto-chunk them (e.g., 250 frames per chunk)\n" +
                "5. Apply Addressable Labels\n" +
                "6. Assign the SpriteFramesData asset above\n\n" +
                "See CHUNKED_LOADING_GUIDE.md for details!",
                MessageType.Info
            );

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
