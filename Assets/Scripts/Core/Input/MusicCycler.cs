using UnityEngine;
using System.Collections.Generic;
using SickOfIt.Core.Addressables;

namespace SickOfIt.Core.Input
{
    /// <summary>
    /// Cycles through audio clips loaded via Addressables.
    /// Loads music by label for optimized WebGL builds.
    /// </summary>
    public class MusicCycler : MonoBehaviour
    {
        [Header("Button")]
        [Tooltip("The button from nappin's framework that triggers song cycling")]
        public MobileControllerFramework.Button cycleButton;

        [Header("Addressables Settings")]
        [Tooltip("Label for music clips in Addressables (e.g., 'Level1_Music')")]
        [SerializeField] private string musicLabel = "Level1_Music";
        
        [Tooltip("Preload music immediately on Awake (recommended)")]
        [SerializeField] private bool preloadOnAwake = true;

        [Header("Settings")]
        [Tooltip("Auto-play the first song on start (after loading)")]
        public bool autoPlayOnStart = true;
        
        [Tooltip("Loop the current song")]
        [SerializeField] private bool loopSongs = false;
        
        [Tooltip("Volume (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        [Header("Debug Info")]
        [SerializeField] private List<AudioClip> loadedClips = new List<AudioClip>();
        [SerializeField] private int currentSongIndex = 0;
        [SerializeField] private bool isLoaded = false;
        
        private AudioSource audioSource;

        /// <summary>
        /// Public property to check if music is loaded
        /// </summary>
        public bool IsLoaded => isLoaded;

        private void Awake()
        {
            // Create an AudioSource component if we don't have one
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.playOnAwake = false;
            audioSource.loop = loopSongs;
            audioSource.volume = volume;
            
            // Preload music for instant playback
            if (preloadOnAwake && !string.IsNullOrEmpty(musicLabel))
            {
                LoadMusic();
            }
        }

        private void Start()
        {
            if (cycleButton == null)
            {
                Debug.LogWarning("MusicCycler: No button assigned!");
                return;
            }

            // Subscribe to button tap event
            cycleButton.TapEndEvent.AddListener(CycleToNextSong);

            // Auto-play first song if enabled and music is loaded
            if (autoPlayOnStart && isLoaded && loadedClips.Count > 0)
            {
                PlaySong(0);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from button events
            if (cycleButton != null)
            {
                cycleButton.TapEndEvent.RemoveListener(CycleToNextSong);
            }
        }

        /// <summary>
        /// Load all music clips from Addressables by label
        /// </summary>
        public void LoadMusic()
        {
            if (string.IsNullOrEmpty(musicLabel))
            {
                Debug.LogError("MusicCycler: musicLabel is not set!");
                return;
            }
            
            if (isLoaded)
            {
                return;
            }
            
            AddressablesAssetLoader.Instance.LoadAssetsByLabel<AudioClip>(musicLabel, (clips) =>
            {
                if (clips != null && clips.Count > 0)
                {
                    loadedClips.Clear();
                    loadedClips.AddRange(clips);
                    
                    // Sort by name for consistent order
                    loadedClips.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
                    
                    isLoaded = true;
                    
                    // Auto-play if enabled
                    if (autoPlayOnStart)
                    {
                        PlaySong(0);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load music with label: {musicLabel}. Make sure audio clips are marked as Addressable with this label.");
                }
            });
        }

        /// <summary>
        /// Cycles to the next song in the list
        /// </summary>
        public void CycleToNextSong()
        {
            if (!isLoaded)
            {
                Debug.LogWarning("MusicCycler: Music not loaded yet!");
                LoadMusic();
                return;
            }
            
            if (loadedClips.Count == 0)
            {
                Debug.LogWarning("MusicCycler: No music clips loaded!");
                return;
            }

            // Stop current song
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Move to next song (loop back to 0 if at the end)
            currentSongIndex = (currentSongIndex + 1) % loadedClips.Count;

            // Play the new song
            PlaySong(currentSongIndex);
        }

        /// <summary>
        /// Plays a specific song by index
        /// </summary>
        private void PlaySong(int index)
        {
            if (!isLoaded || loadedClips.Count == 0)
            {
                Debug.LogWarning("MusicCycler: No music loaded!");
                return;
            }
            
            if (index < 0 || index >= loadedClips.Count)
            {
                Debug.LogWarning($"MusicCycler: Song index {index} is out of range!");
                return;
            }

            AudioClip clip = loadedClips[index];
            if (clip == null)
            {
                Debug.LogWarning($"MusicCycler: Clip at index {index} is null!");
                return;
            }

            audioSource.clip = clip;
            audioSource.time = 0f;
            audioSource.Play();
        }

        /// <summary>
        /// Plays a specific song by index (can be called from UI or other scripts)
        /// </summary>
        public void PlaySongAtIndex(int index)
        {
            if (!isLoaded)
            {
                Debug.LogWarning("MusicCycler: Music not loaded yet!");
                return;
            }
            
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            currentSongIndex = index;
            PlaySong(index);
        }

        /// <summary>
        /// Stops the current song
        /// </summary>
        public void StopMusic()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Pause the current song
        /// </summary>
        public void PauseMusic()
        {
            if (audioSource != null)
            {
                audioSource.Pause();
            }
        }

        /// <summary>
        /// Resume the current song
        /// </summary>
        public void ResumeMusic()
        {
            if (audioSource != null)
            {
                audioSource.UnPause();
            }
        }

        /// <summary>
        /// Set volume (0-1)
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }

        /// <summary>
        /// Get current song name
        /// </summary>
        public string GetCurrentSongName()
        {
            if (isLoaded && currentSongIndex >= 0 && currentSongIndex < loadedClips.Count)
            {
                return loadedClips[currentSongIndex]?.name ?? "Unknown";
            }
            return "No song loaded";
        }

        /// <summary>
        /// Get total number of loaded songs
        /// </summary>
        public int GetSongCount()
        {
            return loadedClips.Count;
        }
    }
}
