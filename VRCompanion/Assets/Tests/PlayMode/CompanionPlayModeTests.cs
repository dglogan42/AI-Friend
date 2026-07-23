using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion;

namespace VRCompanion.Tests
{
    public class CompanionPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpaceTriggeredTurn_CompletesWithoutErrors()
        {
            // CompanionBootstrap.Awake() builds the stub world itself (runtime-safe GameObject
            // creation) — avoid EditorSceneManager.NewScene() here, it can't run in Play mode.
            new GameObject("CompanionBootstrap").AddComponent<CompanionBootstrap>();

            CompanionController controller = null;
            for (int i = 0; i < 60 && controller == null; i++)
            {
                controller = Object.FindFirstObjectByType<CompanionController>();
                yield return null;
            }
            Assert.IsNotNull(controller, "CompanionController was not found in the bootstrap scene.");

            // Let Start()'s auto-greet (stub TTS, ~2.2s) finish before simulating the push-to-talk key.
            yield return new WaitForSeconds(2.5f);

            var turn = controller.RunTurnAsync();
            Debug.Log("[PlayModeTest] Triggered RunTurnAsync (simulated Space press).");

            // Stub pipeline budget: ~1.2s fake listen + up to ~5s stub-speak reply, plus margin.
            float timeout = 10f;
            while (!turn.IsCompleted && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(turn.IsCompleted, "RunTurnAsync did not complete within 10 seconds.");
            Assert.IsFalse(turn.IsFaulted, $"RunTurnAsync threw: {turn.Exception}");
            Assert.IsFalse(controller.IsBusy, "Controller still reports busy after the turn completed.");

            Debug.Log("[PlayModeTest] Turn completed cleanly. PASSED.");
        }
    }
}
