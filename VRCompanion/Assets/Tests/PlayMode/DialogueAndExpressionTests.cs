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

        [Test]
        public void FaceExpressionShapes_ShapeFor_AllExpressionIds_ReturnsKnownShape()
        {
            var known = new HashSet<string>(FaceExpressionShapes.AllShapes);
            foreach (ExpressionId id in System.Enum.GetValues(typeof(ExpressionId)))
            {
                string shape = FaceExpressionShapes.ShapeFor(id);
                Assert.IsTrue(known.Contains(shape), $"{id} mapped to unrecognized shape '{shape}'.");
            }
        }

        [UnityTest]
        public IEnumerator ExpressionController_WithFaceMesh_DrivesBlendShapeWeightsInsteadOfColor()
        {
            var go = new GameObject("FaceMeshTest");
            var mesh = new Mesh();
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2 };
            foreach (var shape in FaceExpressionShapes.AllShapes)
                mesh.AddBlendShapeFrame(shape, 100f, new Vector3[3], null, null);

            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;

            var controller = go.AddComponent<ExpressionController>();
            yield return null;

            controller.SetExpression(ExpressionId.Happy, 1f);

            // Wait on simulated time, not a fixed frame count: the exponential blend's
            // convergence rate depends on Time.deltaTime, which varies in batchmode.
            float elapsed = 0f;
            while (elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            int joyIndex = mesh.GetBlendShapeIndex(FaceExpressionShapes.Joy);
            int neutralIndex = mesh.GetBlendShapeIndex(FaceExpressionShapes.Neutral);
            Assert.Greater(smr.GetBlendShapeWeight(joyIndex), 90f, "Happy should drive the Joy shape near full weight.");
            Assert.Less(smr.GetBlendShapeWeight(neutralIndex), 10f, "Happy at full intensity should leave Neutral near zero.");

            Object.Destroy(go);
        }
    }
}
