using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VRCompanion.Content;

namespace VRCompanion.Speech
{
    /// <summary>
    /// OpenAI Realtime API (WebSocket) conversational voice service: mic in,
    /// spoken reply out, server-side VAD handles turn-taking.
    ///
    /// UNVERIFIED AGAINST A LIVE SESSION: the account behind the configured key has
    /// no billing/quota, so this has only been checked for (a) correct WebSocket
    /// auth/handshake shape and (b) correct parsing of the documented event schema
    /// via synthetic events (see FaceTrackingPlayModeTests-style tests). Re-verify
    /// event field names against platform.openai.com/docs/api-reference/realtime
    /// once billing is enabled — the API has changed shape before.
    /// </summary>
    public sealed class OpenAiRealtimeService : MonoBehaviour, IRealtimeConversationService
    {
        [SerializeField] string model = "gpt-realtime-2";
        [SerializeField] string micDevice; // null/empty = system default
        [SerializeField] int sampleRate = 24000;
        [SerializeField] string voice = "alloy";

        ClientWebSocket _ws;
        AudioSource _audioSource;
        AudioClip _micClip;
        int _micReadPos;
        bool _streamingMic;
        int _sending; // 0/1 guard so we never call SendAsync concurrently

        TaskCompletionSource<string> _pendingTurn;
        readonly List<float> _replySamples = new List<float>();
        readonly StringBuilder _replyTranscript = new StringBuilder();
        string _userTranscript;

        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;
        public event Action<string> ReplyTranscriptReceived;

        void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        public async Task ConnectAsync(CancellationToken ct)
        {
            if (IsConnected)
                return;

            if (!OpenAiSecrets.TryGetApiKey(out string apiKey))
                throw new InvalidOperationException($"OpenAI API key not found at {OpenAiSecrets.ApiKeyPath}");

            _ws = new ClientWebSocket();
            _ws.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            var uri = new Uri($"wss://api.openai.com/v1/realtime?model={Uri.EscapeDataString(model)}");
            await _ws.ConnectAsync(uri, ct);

            await SendJsonAsync(BuildSessionUpdateJson(), ct);
            _ = ReceiveLoop(ct);
        }

        string BuildSessionUpdateJson()
        {
            var content = CompanionContentSettings.Resolve(gameObject);
            string instructions = content != null
                ? content.BuildLlmSystemInstructions()
                : CompanionContentSettings.DefaultLlmSystemInstructions();
            // Escape for JSON string embedding.
            instructions = instructions
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");

            // Hand-built (not JsonUtility) since the payload is nested/optional-heavy.
            // instructions allow intimate/NSFW when CompanionContentSettings permits it.
            return "{\"type\":\"session.update\",\"session\":{" +
                   "\"modalities\":[\"text\",\"audio\"]," +
                   $"\"voice\":\"{voice}\"," +
                   $"\"instructions\":\"{instructions}\"," +
                   "\"input_audio_format\":\"pcm16\"," +
                   "\"output_audio_format\":\"pcm16\"," +
                   "\"input_audio_transcription\":{\"model\":\"whisper-1\"}," +
                   "\"turn_detection\":{\"type\":\"server_vad\"}" +
                   "}}";
        }

        public async Task<string> RunTurnAsync(CancellationToken ct)
        {
            if (!IsConnected)
                await ConnectAsync(ct);

            _pendingTurn = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _replySamples.Clear();
            _replyTranscript.Clear();
            _userTranscript = null;

            StartMicStreaming();
            using (ct.Register(() => _pendingTurn?.TrySetCanceled()))
            {
                try
                {
                    return await _pendingTurn.Task;
                }
                finally
                {
                    StopMicStreaming();
                }
            }
        }

        void StartMicStreaming()
        {
            string device = string.IsNullOrEmpty(micDevice) ? null : micDevice;
            _micClip = Microphone.Start(device, true, 30, sampleRate);
            _micReadPos = 0;
            _streamingMic = true;
        }

        void StopMicStreaming()
        {
            _streamingMic = false;
            string device = string.IsNullOrEmpty(micDevice) ? null : micDevice;
            if (Microphone.IsRecording(device))
                Microphone.End(device);
        }

        void Update()
        {
            if (!_streamingMic || _micClip == null || !IsConnected)
                return;

            int pos = Microphone.GetPosition(string.IsNullOrEmpty(micDevice) ? null : micDevice);
            int available = pos - _micReadPos;
            if (available < 0)
                available += _micClip.samples; // wrapped
            if (available < sampleRate / 10) // wait for >=100ms of audio
                return;

            var floatBuf = new float[available];
            _micClip.GetData(floatBuf, _micReadPos);
            _micReadPos = (_micReadPos + available) % _micClip.samples;

            byte[] pcm16 = FloatsToPcm16(floatBuf);
            string b64 = Convert.ToBase64String(pcm16);
            string json = "{\"type\":\"input_audio_buffer.append\",\"audio\":\"" + b64 + "\"}";
            _ = SendJsonAsync(json, CancellationToken.None);
        }

        static byte[] FloatsToPcm16(float[] samples)
        {
            var bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short s = (short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
                bytes[i * 2] = (byte)(s & 0xFF);
                bytes[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }
            return bytes;
        }

        static float[] Pcm16ToFloats(byte[] bytes)
        {
            var floats = new float[bytes.Length / 2];
            for (int i = 0; i < floats.Length; i++)
            {
                short s = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
                floats[i] = s / (float)short.MaxValue;
            }
            return floats;
        }

        async Task SendJsonAsync(string json, CancellationToken ct)
        {
            if (_ws == null || _ws.State != WebSocketState.Open)
                return;

            while (Interlocked.CompareExchange(ref _sending, 1, 0) != 0)
                await Task.Yield();
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
            }
            finally
            {
                Interlocked.Exchange(ref _sending, 0);
            }
        }

        async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[1 << 16];
            var messageBuffer = new StringBuilder();

            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (!result.EndOfMessage)
                        continue;

                    string json = messageBuffer.ToString();
                    messageBuffer.Clear();
                    HandleServerEvent(json);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenAiRealtimeService] Receive loop ended: {ex.Message}");
                _pendingTurn?.TrySetException(ex);
            }
        }

        /// <summary>
        /// Pure(ish) event dispatcher — deliberately separated from the network loop
        /// so it can be exercised with synthetic JSON in tests without a live socket.
        /// </summary>
        public void HandleServerEvent(string json)
        {
            string type = ExtractStringField(json, "type");
            if (string.IsNullOrEmpty(type))
                return;

            switch (type)
            {
                case "conversation.item.input_audio_transcription.completed":
                    _userTranscript = ExtractStringField(json, "transcript");
                    break;

                case "response.audio.delta":
                    string audioB64 = ExtractStringField(json, "delta");
                    if (!string.IsNullOrEmpty(audioB64))
                        _replySamples.AddRange(Pcm16ToFloats(Convert.FromBase64String(audioB64)));
                    break;

                case "response.audio_transcript.delta":
                    _replyTranscript.Append(ExtractStringField(json, "delta"));
                    break;

                case "response.done":
                    FinishTurn();
                    break;

                case "error":
                    string message = ExtractStringField(json, "message");
                    _pendingTurn?.TrySetException(new InvalidOperationException($"Realtime API error: {message}"));
                    break;
            }
        }

        void FinishTurn()
        {
            if (_replySamples.Count > 0)
            {
                var clip = AudioClip.Create("RealtimeReply", _replySamples.Count, 1, sampleRate, false);
                clip.SetData(_replySamples.ToArray(), 0);
                _audioSource.PlayOneShot(clip);
            }

            string reply = _replyTranscript.ToString();
            if (!string.IsNullOrEmpty(reply))
                ReplyTranscriptReceived?.Invoke(reply);

            _pendingTurn?.TrySetResult(_userTranscript ?? string.Empty);
        }

        /// <summary>Minimal hand-rolled extraction for a top-level or one-level-nested
        /// string field — avoids pulling in a full JSON library for a handful of fields.
        /// Not a general JSON parser; relies on OpenAI's event shapes being flat enough.</summary>
        public static string ExtractStringField(string json, string field)
        {
            string needle = $"\"{field}\":\"";
            int idx = json.IndexOf(needle, StringComparison.Ordinal);
            if (idx < 0)
                return null;
            int start = idx + needle.Length;
            int end = start;
            while (end < json.Length && json[end] != '"')
            {
                if (json[end] == '\\')
                    end++; // skip escaped char
                end++;
            }
            if (end > json.Length)
                return null;
            string raw = json.Substring(start, end - start);
            return raw.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\\\", "\\");
        }

        void OnDestroy()
        {
            StopMicStreaming();
            _ws?.Dispose();
        }
    }
}
