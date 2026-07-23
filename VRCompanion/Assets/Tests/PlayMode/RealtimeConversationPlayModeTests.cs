using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion.Speech;

namespace VRCompanion.Tests
{
    /// <summary>
    /// Exercises OpenAiRealtimeService's event parsing/playback logic with synthetic
    /// server messages — no live network call. The account behind the configured key
    /// has no billing/quota, so the actual WebSocket round-trip is unverified; this
    /// only proves the client-side handling of the documented event shapes is correct.
    /// </summary>
    public class RealtimeConversationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SyntheticEvents_PlayAudioAndFireTranscript()
        {
            var go = new GameObject("RealtimeService");
            var svc = go.AddComponent<OpenAiRealtimeService>();
            yield return null; // let Awake add the AudioSource

            string receivedTranscript = null;
            svc.ReplyTranscriptReceived += t => receivedTranscript = t;

            // ~0.1s of silent PCM16 mono audio (2400 samples @ 24kHz), base64-encoded,
            // matching response.audio.delta's documented shape.
            byte[] silentPcm = new byte[2400 * 2];
            string audioB64 = Convert.ToBase64String(silentPcm);

            svc.HandleServerEvent("{\"type\":\"response.audio.delta\",\"delta\":\"" + audioB64 + "\"}");
            svc.HandleServerEvent("{\"type\":\"response.audio_transcript.delta\",\"delta\":\"Hello there\"}");
            svc.HandleServerEvent("{\"type\":\"response.done\"}");

            yield return null;

            Assert.AreEqual("Hello there", receivedTranscript);

            var audioSource = go.GetComponent<AudioSource>();
            Assert.IsNotNull(audioSource, "Expected Awake() to have added an AudioSource.");
            Assert.IsTrue(audioSource.isPlaying, "Expected the synthesized reply clip to be playing after response.done.");

            UnityEngine.Object.Destroy(go);
        }

        [Test]
        public void ExtractStringField_ParsesTopLevelAndEscapedStrings()
        {
            string json = "{\"type\":\"error\",\"error\":{\"message\":\"Say \\\"hi\\\" now\"}}";

            Assert.AreEqual("error", OpenAiRealtimeService.ExtractStringField(json, "type"));
            Assert.AreEqual("Say \"hi\" now", OpenAiRealtimeService.ExtractStringField(json, "message"));
            Assert.IsNull(OpenAiRealtimeService.ExtractStringField(json, "missing"));
        }

        [Test]
        public void HandleServerEvent_TranscriptionCompleted_StoresUserTranscript()
        {
            var go = new GameObject("RealtimeService2");
            var svc = go.AddComponent<OpenAiRealtimeService>();

            // Should not throw even with no pending turn (RunTurnAsync never called) —
            // FinishTurn's TaskCompletionSource use is null-conditional.
            Assert.DoesNotThrow(() =>
                svc.HandleServerEvent("{\"type\":\"conversation.item.input_audio_transcription.completed\",\"transcript\":\"testing\"}"));
            Assert.DoesNotThrow(() => svc.HandleServerEvent("{\"type\":\"response.done\"}"));

            UnityEngine.Object.Destroy(go);
        }
    }
}
