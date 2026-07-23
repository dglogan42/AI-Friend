using System;
using System.Threading;
using System.Threading.Tasks;

namespace VRCompanion.Speech
{
    /// <summary>
    /// A conversational voice service that owns listen+think+speak as one integrated
    /// turn (e.g. OpenAI Realtime), as opposed to the decoupled IAsrService -&gt;
    /// DialogueService -&gt; ITtsService stub pipeline. CompanionController prefers this
    /// when present and connected.
    /// </summary>
    public interface IRealtimeConversationService
    {
        bool IsConnected { get; }
        Task ConnectAsync(CancellationToken ct);

        /// <summary>Records the user, waits for the model's full spoken reply, and plays it.
        /// Returns the user's transcribed text (for logging/UI) once the turn completes.</summary>
        Task<string> RunTurnAsync(CancellationToken ct);
    }
}
