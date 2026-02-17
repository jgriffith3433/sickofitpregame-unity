using UnityEngine;
using System.Collections.Generic;

namespace SickOfIt.VideoFrameExtractor.Runtime
{
    /// <summary>
    /// ScriptableObject that defines a sprite frame sequence and its chunks for progressive loading
    /// </summary>
    [CreateAssetMenu(fileName = "SpriteFramesData", menuName = "Video/Sprite Frames Data")]
    public class SpriteFramesData : ScriptableObject
    {
        [Header("Video Info")]
        [Tooltip("Name of this video (e.g., 'Video3')")]
        public string videoName = "";

        [Tooltip("Target playback FPS")]
        public float playbackFPS = 30f;

        [Tooltip("If true, video will loop")]
        public bool loop = false;

        [Header("Audio")]
        [Tooltip("Addressables address for audio clip (optional)")]
        public string audioAddress = "";

        [Header("Sprite Sheet Configuration")]
        [Tooltip("Original source frame width in pixels (e.g., 1280)")]
        public int sourceFrameWidth = 1280;

        [Tooltip("Original source frame height in pixels (e.g., 720)")]
        public int sourceFrameHeight = 720;

        [Tooltip("Maximum sprite sheet texture size (e.g., 2048)")]
        public int maxSheetSize = 2048;

        [Tooltip("Number of frames packed per sprite sheet (auto-calculated)")]
        public int framesPerSheet = 6;

        [Tooltip("Columns in sprite sheet grid (auto-calculated)")]
        public int sheetColumns = 2;

        [Tooltip("Rows in sprite sheet grid (auto-calculated)")]
        public int sheetRows = 3;

        [Tooltip("Width of each frame cell in the sheet (auto-calculated, aspect-ratio preserved)")]
        public int frameWidth = 1024;

        [Tooltip("Height of each frame cell in the sheet (auto-calculated, aspect-ratio preserved)")]
        public int frameHeight = 576;

        [Tooltip("Actual sprite sheet texture width (power of 2, auto-calculated)")]
        public int sheetTextureWidth = 2048;

        [Tooltip("Actual sprite sheet texture height (power of 2, auto-calculated)")]
        public int sheetTextureHeight = 2048;

        [Tooltip("Pixels per unit for created sprites")]
        public float spritePixelsPerUnit = 100f;

        [Header("Chunk Configuration")]
        [Tooltip("List of frame chunks - each chunk has its own addressables label")]
        public List<FrameChunk> chunks = new List<FrameChunk>();

        [Header("Info (Read-only)")]
        [SerializeField] private int totalFrames = 0;
        [SerializeField] private int totalSpriteSheets = 0;

        /// <summary>
        /// Single chunk of frames
        /// </summary>
        [System.Serializable]
        public class FrameChunk
        {
            [Tooltip("Name of this chunk (e.g., 'Part1')")]
            public string chunkName = "";

            [Tooltip("Addressables label for this chunk's frames")]
            public string addressableLabel = "";

            [Tooltip("Starting frame index (inclusive)")]
            public int startFrame = 0;

            [Tooltip("Ending frame index (inclusive)")]
            public int endFrame = 0;

            [Tooltip("Read-only: Number of frames in this chunk")]
            public int frameCount => endFrame - startFrame + 1;

            public override string ToString()
            {
                return $"{chunkName}: frames {startFrame}-{endFrame} ({frameCount} frames) - Label: {addressableLabel}";
            }
        }

        /// <summary>
        /// Get total number of frames across all chunks
        /// </summary>
        public int GetTotalFrames()
        {
            int total = 0;
            foreach (var chunk in chunks)
            {
                total += chunk.frameCount;
            }
            totalFrames = total;
            return total;
        }

        /// <summary>
        /// Set total frame count (called by editor when packing sprite sheets)
        /// </summary>
        public void SetTotalFrameCount(int count)
        {
            totalFrames = count;
        }

        /// <summary>
        /// Set total sprite sheet count (called by editor when packing)
        /// </summary>
        public void SetTotalSpriteSheetCount(int count)
        {
            totalSpriteSheets = count;
        }

        /// <summary>
        /// Get number of sprite sheets needed for a given frame count
        /// </summary>
        public int GetSheetCountForFrames(int frameCount)
        {
            return Mathf.CeilToInt((float)frameCount / framesPerSheet);
        }

        /// <summary>
        /// Calculate optimal grid layout that preserves aspect ratio and maximizes frames per sheet
        /// while keeping cell size at least half the source resolution for quality.
        /// </summary>
        public void CalculateOptimalGrid()
        {
            if (sourceFrameWidth <= 0 || sourceFrameHeight <= 0 || maxSheetSize <= 0)
                return;

            float aspect = (float)sourceFrameWidth / sourceFrameHeight;
            int bestCols = 1, bestRows = 1, bestCellW = 0, bestCellH = 0, bestCount = 0;

            // Minimum cell size = 75% of the source resolution to preserve quality
            int minCellW = Mathf.Max(maxSheetSize / 8, sourceFrameWidth * 3 / 4);
            int minCellH = Mathf.Max(maxSheetSize / 8, sourceFrameHeight * 3 / 4);

            // Try reasonable column counts (1-8) to find the layout that maximizes frames per sheet
            for (int cols = 1; cols <= 8; cols++)
            {
                // Cell width = sheet width / columns, rounded down to multiple of 4 (for GPU compression)
                int cellW = (maxSheetSize / cols) & ~3; // multiple of 4
                if (cellW < minCellW) break;

                // Cell height from aspect ratio, rounded down to multiple of 4
                int cellH = (int)(cellW / aspect) & ~3;
                if (cellH < minCellH) continue;

                int rowsFit = maxSheetSize / cellH;
                if (rowsFit < 1) continue;

                int count = cols * rowsFit;
                if (count > bestCount)
                {
                    bestCount = count;
                    bestCols = cols;
                    bestRows = rowsFit;
                    bestCellW = cellW;
                    bestCellH = cellH;
                }
            }

            sheetColumns = bestCols;
            sheetRows = bestRows;
            frameWidth = bestCellW;
            frameHeight = bestCellH;
            framesPerSheet = bestCols * bestRows;

            // Calculate actual texture size as next power of 2
            sheetTextureWidth = Mathf.NextPowerOfTwo(bestCols * bestCellW);
            sheetTextureHeight = Mathf.NextPowerOfTwo(bestRows * bestCellH);
        }

        /// <summary>
        /// Get the chunk that contains a specific frame index
        /// </summary>
        public FrameChunk GetChunkForFrame(int frameIndex)
        {
            foreach (var chunk in chunks)
            {
                if (frameIndex >= chunk.startFrame && frameIndex <= chunk.endFrame)
                {
                    return chunk;
                }
            }
            return null;
        }

        /// <summary>
        /// Get chunk index for a specific frame
        /// </summary>
        public int GetChunkIndexForFrame(int frameIndex)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                if (frameIndex >= chunks[i].startFrame && frameIndex <= chunks[i].endFrame)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Validate that chunks are properly configured
        /// </summary>
        public bool Validate(out string error)
        {
            error = "";

            if (chunks.Count == 0)
            {
                error = "No chunks defined";
                return false;
            }

            // Check for gaps or overlaps
            for (int i = 0; i < chunks.Count - 1; i++)
            {
                if (chunks[i].endFrame + 1 != chunks[i + 1].startFrame)
                {
                    error = $"Gap or overlap between {chunks[i].chunkName} and {chunks[i + 1].chunkName}";
                    return false;
                }
            }

            // Check for empty labels
            foreach (var chunk in chunks)
            {
                if (string.IsNullOrEmpty(chunk.addressableLabel))
                {
                    error = $"Chunk {chunk.chunkName} has no addressable label";
                    return false;
                }
            }

            return true;
        }
    }
}
