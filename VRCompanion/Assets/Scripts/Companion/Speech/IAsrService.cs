using System;
using System.Threading;
using System.Threading.Tasks;

namespace VRCompanion.Speech
{
    /// <summary>
    /// Automatic speech recognition. Swap StubAsrService for a local Whisper / on-device model later.
    /// </summary>
    public interface IAsrService
    {
        bool IsListening { get; }
        event Action<string> PartialTranscript;
        event Action<string> FinalTranscript;

        Task StartListeningAsync(CancellationToken ct = default);
        Task StopListeningAsync();
    }
}
