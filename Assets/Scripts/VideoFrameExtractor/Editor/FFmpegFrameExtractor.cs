#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Video;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SickOfIt.VideoFrameExtractor.Runtime;

namespace SickOfIt.VideoFrameExtractor.Editor
{
    public class FFmpegFrameExtractorWindow : EditorWindow
    {
        private VideoClip videoClip;
        private float targetFPS = 30f;
        private int frameSkip = 0;
        private string outputFolderPath = "";
        private bool isExtracting = false;

        [MenuItem("Tools/Video Frame Extractor")]
        public static void ShowWindow()
        {
            GetWindow<FFmpegFrameExtractorWindow>("Frame Extractor");
        }

        private void OnGUI()
        {
            GUILayout.Label("Video Frame Extraction", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Video selection
            videoClip = (VideoClip)EditorGUILayout.ObjectField("Video Clip", videoClip, typeof(VideoClip), false);

            // FPS setting
            targetFPS = EditorGUILayout.FloatField("Target FPS", targetFPS);
            targetFPS = Mathf.Clamp(targetFPS, 1f, 60f);

            GUILayout.Space(5);
            EditorGUILayout.HelpBox($"Higher FPS = More frames = More memory usage\n15-24 FPS recommended for long videos", MessageType.Info);

            GUILayout.Space(10);

            // Frame skip setting
            frameSkip = EditorGUILayout.IntField("Frame Skip", frameSkip);
            frameSkip = Mathf.Max(0, frameSkip);

            GUILayout.Space(5);
            if (frameSkip > 0)
            {
                EditorGUILayout.HelpBox($"Skipping {frameSkip} frame(s) - extracting every {frameSkip + 1}{GetOrdinalSuffix(frameSkip + 1)} frame\nOutput will be numbered sequentially (frame00000, frame00001, etc.)", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Frame Skip: 0 (extract all frames at target FPS)", MessageType.Info);
            }

            GUILayout.Space(10);

            // Output folder
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Folder", GUILayout.Width(100));

            if (string.IsNullOrEmpty(outputFolderPath) && videoClip != null)
            {
                outputFolderPath = GetDefaultOutputPath();
            }

            EditorGUILayout.LabelField(outputFolderPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Choose Output Folder"))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        outputFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside the Assets directory.", "OK");
                    }
                }
            }

            GUI.enabled = videoClip != null && !isExtracting;

            GUILayout.Space(10);

            if (GUILayout.Button("Extract Frames", GUILayout.Height(40)))
            {
                ExtractFrames();
            }

            GUI.enabled = true;

            if (isExtracting)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Extracting frames... This may take a while. Check the Console for progress.", MessageType.Warning);
            }

            GUILayout.Space(20);

            // Memory estimation
            if (videoClip != null)
            {
                float duration = (float)videoClip.length;
                int estimatedFrames = Mathf.CeilToInt(duration * targetFPS);

                // Adjust for frame skipping
                if (frameSkip > 0)
                {
                    estimatedFrames = estimatedFrames / (frameSkip + 1);
                }

                EditorGUILayout.HelpBox($"Estimated frames: ~{estimatedFrames}\nVideo duration: {duration:F1}s", MessageType.None);
            }
        }

        private string GetDefaultOutputPath()
        {
            if (videoClip == null) return "";

            string videoPath = AssetDatabase.GetAssetPath(videoClip);
            string directory = Path.GetDirectoryName(videoPath);
            string videoName = Path.GetFileNameWithoutExtension(videoPath);

            return $"{directory}/{videoName}_frames";
        }

        private string GetOrdinalSuffix(int number)
        {
            if (number <= 0) return "th";

            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
                return "th";

            switch (lastDigit)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        private void ExtractFrames()
        {
            if (!FFmpegHelper.IsFFmpegAvailable())
            {
                EditorUtility.DisplayDialog("FFmpeg Not Found", 
                    "FFmpeg not found on your system.\n\n" +
                    "Please install FFmpeg:\n" +
                    "1. Open PowerShell/Command Prompt as Administrator\n" +
                    "2. Run: winget install ffmpeg\n" +
                    "3. Restart Unity after installation", 
                    "OK");
                return;
            }

            if (string.IsNullOrEmpty(outputFolderPath))
            {
                outputFolderPath = GetDefaultOutputPath();
            }

            isExtracting = true;

            string videoPath = AssetDatabase.GetAssetPath(videoClip);

            // Convert Unity asset path to absolute file system path
            string fullOutputPath;
            if (outputFolderPath.StartsWith("Assets"))
            {
                // Remove "Assets" or "Assets/" or "Assets\" from the beginning
                string relativePath = outputFolderPath.Substring(6).TrimStart('/', '\\');
                fullOutputPath = Path.Combine(Application.dataPath, relativePath);
            }
            else
            {
                fullOutputPath = outputFolderPath;
            }

            FFmpegHelper.ExtractFrames(videoPath, targetFPS, frameSkip, fullOutputPath, (success, message) =>
            {
                isExtracting = false;

                if (success)
                {
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("Success", message, "OK");
                    UnityEngine.Debug.Log($"Frames extracted to: {outputFolderPath}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", message, "OK");
                }

                Repaint();
            });
        }
    }

    public static class FFmpegHelper
    {
        private static string ffmpegPath = null;

        public static bool IsFFmpegAvailable()
        {
            if (ffmpegPath != null)
                return true;

            // Check hardcoded path first
            string hardcodedPath = @"C:\ffmpeg\bin\ffmpeg.exe";
            if (File.Exists(hardcodedPath))
            {
                ffmpegPath = hardcodedPath;
                UnityEngine.Debug.Log($"Found FFmpeg at hardcoded path: {ffmpegPath}");
                return true;
            }

            // Try to find ffmpeg in PATH using 'where' command
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c where ffmpeg",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(2000);

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        string[] lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            ffmpegPath = lines[0].Trim();
                            UnityEngine.Debug.Log($"Found FFmpeg at: {ffmpegPath}");
                            return true;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"FFmpeg PATH search exception: {e.Message}");
            }

            // Check WinGet installation paths
            string[] possiblePaths = new string[]
            {
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft\\WinGet\\Links\\ffmpeg.exe"),
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
                    "ffmpeg\\bin\\ffmpeg.exe")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    ffmpegPath = path;
                    UnityEngine.Debug.Log($"Found FFmpeg at: {ffmpegPath}");
                    return true;
                }
            }

            // Search WinGet packages directory
            string wingetPackages = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Microsoft\\WinGet\\Packages");

            if (Directory.Exists(wingetPackages))
            {
                string[] ffmpegDirs = Directory.GetDirectories(wingetPackages, "Gyan.FFmpeg*");
                foreach (string dir in ffmpegDirs)
                {
                    string[] binDirs = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)
                        .Where(d => d.EndsWith("\\bin")).ToArray();

                    foreach (string binDir in binDirs)
                    {
                        string exePath = Path.Combine(binDir, "ffmpeg.exe");
                        if (File.Exists(exePath))
                        {
                            ffmpegPath = exePath;
                            UnityEngine.Debug.Log($"Found FFmpeg at: {ffmpegPath}");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void ExtractFrames(string videoPath, float fps, int frameSkip, string outputPath, System.Action<bool, string> onComplete)
        {
            if (!IsFFmpegAvailable())
            {
                onComplete?.Invoke(false, "FFmpeg not found.");
                return;
            }

            // Create output directory
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Convert to absolute paths
            string absoluteVideoPath = Path.GetFullPath(videoPath);
            string absoluteOutputPath = Path.GetFullPath(outputPath);

            // FFmpeg command - extract as JPG files with high quality
            string outputPattern = Path.Combine(absoluteOutputPath, "frame_%05d.jpg");

            // Build video filter
            string videoFilter;
            if (frameSkip > 0)
            {
                // First apply fps filter, then select every (frameSkip+1)th frame
                // This ensures we skip frames from the target fps, not the source fps
                videoFilter = $"fps={fps},select='not(mod(n\\,{frameSkip + 1}))',setpts=N/TB";
            }
            else
            {
                // No frame skipping, just use fps filter
                videoFilter = $"fps={fps}";
            }

            // Use -vsync vfr to prevent frame duplication when using select
            string vsyncParam = frameSkip > 0 ? "-vsync vfr" : "";
            string arguments = $"-i \"{absoluteVideoPath}\" {vsyncParam} -vf {videoFilter} -q:v 3 -start_number 0 \"{outputPattern}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                UnityEngine.Debug.Log($"FFmpeg command: {ffmpegPath} {arguments}");
                UnityEngine.Debug.Log($"Video path: {absoluteVideoPath}");
                UnityEngine.Debug.Log($"Output path: {absoluteOutputPath}");

                Process process = Process.Start(startInfo);
                process.WaitForExit();

                int exitCode = process.ExitCode;
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                UnityEngine.Debug.Log($"FFmpeg exit code: {exitCode}");
                if (!string.IsNullOrEmpty(output))
                    UnityEngine.Debug.Log($"FFmpeg output: {output}");
                if (!string.IsNullOrEmpty(error))
                    UnityEngine.Debug.Log($"FFmpeg stderr: {error}");

                if (exitCode == 0)
                {
                    string[] jpgFiles = Directory.GetFiles(absoluteOutputPath, "frame_*.jpg");
                    UnityEngine.Debug.Log($"Found {jpgFiles.Length} frames in {absoluteOutputPath}");

                    // Set import settings for all extracted frames
                    SetFrameImportSettings(outputPath, jpgFiles.Length);

                    onComplete?.Invoke(true, $"Successfully extracted {jpgFiles.Length} frames!");
                }
                else
                {
                    UnityEngine.Debug.LogError($"FFmpeg extraction failed with exit code {exitCode}");
                    onComplete?.Invoke(false, $"FFmpeg extraction failed. Check console for details.");
                }
            }
            catch (System.Exception e)
            {
                onComplete?.Invoke(false, $"Failed to start FFmpeg: {e.Message}");
            }
        }

        private static void SetFrameImportSettings(string outputPath, int frameCount)
        {
            try
            {
                // Convert absolute path to Unity asset path
                string unityPath;
                if (outputPath.StartsWith(Application.dataPath))
                {
                    unityPath = "Assets" + outputPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    unityPath = outputPath;
                }

                // Ensure path uses forward slashes
                unityPath = unityPath.Replace("\\", "/");

                UnityEngine.Debug.Log($"Setting import settings for frames in: {unityPath}");

                // Find all frame files and set import settings
                string[] guids = AssetDatabase.FindAssets("frame_* t:Texture2D", new[] { unityPath });

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.SaveAndReimport();
                    }
                }

                UnityEngine.Debug.Log($"Import settings updated for {guids.Length} frames");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to set import settings: {e.Message}");
            }
        }
    }
}
#endif
