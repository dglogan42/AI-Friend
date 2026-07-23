using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion.Dialogue;

namespace VRCompanion.Tests
{
    public class DialogueAndExpressionTests
    {
        [UnityTest]
        public IEnumerator ReplyAsync_UnknownInput_ProducesVariedResponsesOverManyCalls()
        {
            var go = new GameObject("DialogueTest");
            var dialogue = go.AddComponent<DialogueService>();
            var seenTexts = new HashSet<string>();

            for (int i = 0; i < 20; i++)
            {
                var task = dialogue.ReplyAsync("zzz nonsense query zzz", "hub");
                while (!task.IsCompleted)
                    yield return null;
                seenTexts.Add(task.Result.Text);
            }

            Assert.Greater(seenTexts.Count, 1,
                "Expected more than one distinct fallback response across 20 calls with the same input — dialogue should vary, not repeat a single canned line.");

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator ReplyAsync_CafeKeyword_RoutesToCafeScene()
        {
            var go = new GameObject("DialogueTest2");
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("can we go to the cafe", "hub");
            while (!task.IsCompleted)
                yield return null;

            Assert.AreEqual(CompanionSceneId.Cafe, task.Result.SwitchToScene);
            Assert.IsNotEmpty(task.Result.Text);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator ReplyAsync_ThanksKeyword_DoesNotSwitchScene()
        {
            var go = new GameObject("DialogueTest3");
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("thank you so much", "hub");
            while (!task.IsCompleted)
                yield return null;

            Assert.IsNull(task.Result.SwitchToScene);
            Assert.IsNotEmpty(task.Result.Text);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator ExpressionController_AllExpressionIds_ApplyWithoutError()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var controller = cube.AddComponent<ExpressionController>();
            yield return null;

            foreach (ExpressionId id in System.Enum.GetValues(typeof(ExpressionId)))
            {
                controller.SetExpression(id, 1f);
                yield return null;
                yield return null; // let the blend settle enough to hit ApplyVisual with the new id
            }

            Object.Destroy(cube);
        }
    }
}
