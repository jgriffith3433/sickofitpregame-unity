using System;

namespace SickOfIt.Core.Flow
{
    public interface IFrameSequencePlayer
    {
        bool IsLoaded { get; }
        void Play();
        void SkipToEnd();
        event Action PlaybackCompleted;
    }
}
