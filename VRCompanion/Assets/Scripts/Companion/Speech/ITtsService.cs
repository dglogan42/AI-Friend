using System;
using System.Threading;
using System.Threading.Tasks;

namespace VRCompanion.Speech
{
    /// <summary>
    /// Text-to-speech. Swap StubTtsService for a local neural TTS later.
    /// </summary>
    public interface ITtsService
    {
        bool IsSpeaking { get; }
        event Action Started;
        event Action Finished;

        Task SpeakAsync(string text, CancellationToken ct = default);
        void Stop();
    }
}
