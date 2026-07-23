using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion;
using VRCompanion.Content;
using VRCompanion.Dialogue;

namespace VRCompanion.Tests
{
    public class ContentIntimacyTests
    {
        [Test]
        public void ContentSettings_DefaultAllowsIntimateAndNsfw()
        {
            var go = new GameObject("Content");
            var c = go.AddComponent<CompanionContentSettings>();
            Assert.IsTrue(c.AllowIntimate);
            Assert.IsTrue(c.AllowNsfw);
            StringAssert.Contains("Adult sexual content", c.BuildLlmSystemInstructions());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ContentSettings_NsfwRequiresIntimate()
        {
            var go = new GameObject("Content2");
            var c = go.AddComponent<CompanionContentSettings>();
            c.AllowIntimate = false;
            c.AllowNsfw = true;
            Assert.IsFalse(c.AllowNsfw, "NSFW should be gated behind AllowIntimate");
            StringAssert.Contains("PG-13", c.BuildLlmSystemInstructions());
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator Dialogue_Kiss_WhenIntimateAllowed_UsesAffection()
        {
            var go = new GameObject("Dlg");
            go.AddComponent<CompanionContentSettings>();
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("kiss me", "hub");
            while (!task.IsCompleted)
                yield return null;

            Assert.IsNotEmpty(task.Result.Text);
            Assert.That(
                task.Result.Expression == ExpressionId.Affectionate
                || task.Result.Expression == ExpressionId.Flirty
                || task.Result.Expression == ExpressionId.Intimate,
                "Kiss replies should use intimate affect");

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Dialogue_Nsfw_WhenAllowed_RoutesPrivate()
        {
            var go = new GameObject("DlgNsfw");
            go.AddComponent<CompanionContentSettings>();
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("I want sex with you", "hub");
            while (!task.IsCompleted)
                yield return null;

            Assert.AreEqual(CompanionSceneId.Private, task.Result.SwitchToScene);
            Assert.IsNotEmpty(task.Result.Text);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Dialogue_Nsfw_WhenDisabled_SoftDenies()
        {
            var go = new GameObject("DlgDeny");
            var content = go.AddComponent<CompanionContentSettings>();
            content.AllowNsfw = false;
            content.AllowIntimate = true;
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("let's have sex", "hub");
            while (!task.IsCompleted)
                yield return null;

            StringAssert.Contains("NSFW", task.Result.Text);

            Object.Destroy(go);
        }

        [Test]
        public void FaceExpressionShapes_IntimateIds_Map()
        {
            Assert.AreEqual(FaceExpressionShapes.Joy, FaceExpressionShapes.ShapeFor(ExpressionId.Intimate));
            Assert.AreEqual(FaceExpressionShapes.Fun, FaceExpressionShapes.ShapeFor(ExpressionId.Flirty));
            Assert.AreEqual(FaceExpressionShapes.Fun, FaceExpressionShapes.ShapeFor(ExpressionId.Seductive));
        }
    }
}
