using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion.Singing;

namespace VRCompanion.Tests
{
    /// <summary>
    /// LIVE hardware check, not a deterministic assertion — runs the real
    /// SingingRaterService.RunChallengeAsync (reference tone playback + real mic
    /// recording + scoring) against whatever the actual microphone picks up. Unlike
    /// SingingRaterTests (synthetic sine waves), this exercises the real audio I/O
    /// path end to end. Logs the result; only fails on an outright exception/hang.
    /// </summary>
    public class SingingRaterLiveSmokeTest
    {
        [UnityTest]
        public IEnumerator RunChallengeAsync_AgainstRealMicrophone_CompletesWithoutError()
        {
            if (Microphone.devices.Length == 0)
            {
                Assert.Ignore("No microphone devices available in this environment.");
                yield break;
            }

            var go = new GameObject("LiveSingingRater");
            var rater = go.AddComponent<SingingRaterService>();
            yield return null;

            var cts = new System.Threading.CancellationTokenSource();
            var task = rater.RunChallengeAsync(cts.Token);

            float timeout = 15f;
            while (!task.IsCompleted && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(task.IsCompleted, "RunChallengeAsync did not complete within 15s against real hardware.");
            Assert.IsFalse(task.IsFaulted, $"RunChallengeAsync threw: {task.Exception}");

            var result = task.Result;
            Debug.Log($"[LiveSmokeTest] Score={result.Score:0.0} VoicedMatchedWindows={result.VoicedMatchedWindows}/{result.TotalWindows} AvgAbsCentsError={result.AverageAbsCentsError:0.0}");
            Debug.Log($"[LiveSmokeTest] Feedback: {SingingRaterService.FeedbackFor(result)}");
            Debug.Log("[LiveSmokeTest] PASSED — real reference-tone playback + real mic recording + scoring completed without error.");

            Object.Destroy(go);
        }
    }
}
