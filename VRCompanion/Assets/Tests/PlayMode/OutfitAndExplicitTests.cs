using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion.Dialogue;
using VRCompanion.Intimacy;
using VRCompanion.Outfits;
using VRCompanion.Content;

namespace VRCompanion.Tests
{
    public class OutfitAndExplicitTests
    {
        [Test]
        public void OutfitParser_RecognizesSuggestiveAndNude()
        {
            Assert.IsTrue(OutfitController.TryParseOutfitCommand("wear something sexy", out var a));
            Assert.AreEqual(OutfitId.Suggestive, a);
            Assert.IsTrue(OutfitController.TryParseOutfitCommand("get naked", out var b));
            Assert.AreEqual(OutfitId.Nude, b);
            Assert.IsTrue(OutfitController.TryParseOutfitCommand("put on lingerie", out var c));
            Assert.AreEqual(OutfitId.Lingerie, c);
        }

        [Test]
        public void ExplicitParser_RecognizesActs()
        {
            Assert.IsTrue(ExplicitInteractionController.TryParseAct("give me a blowjob", out var oral));
            Assert.AreEqual(ExplicitAct.Oral, oral);
            Assert.IsTrue(ExplicitInteractionController.TryParseAct("doggy style", out var doggy));
            Assert.AreEqual(ExplicitAct.Doggy, doggy);
            Assert.IsTrue(ExplicitInteractionController.TryParseAct("ride me cowgirl", out var cow));
            Assert.AreEqual(ExplicitAct.Cowgirl, cow);
            Assert.IsTrue(ExplicitInteractionController.TryParseAct("make me cum", out var climax));
            Assert.AreEqual(ExplicitAct.Climax, climax);
        }

        [UnityTest]
        public IEnumerator Dialogue_LingerieCommand_SetsOutfit()
        {
            var go = new GameObject("OutfitDlg");
            go.AddComponent<CompanionContentSettings>();
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("put on lingerie", "hub");
            while (!task.IsCompleted)
                yield return null;

            Assert.AreEqual(OutfitId.Lingerie, task.Result.Outfit);
            Assert.IsNotEmpty(task.Result.Text);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Dialogue_Doggy_SetsActAndPrivateScene()
        {
            var go = new GameObject("ActDlg");
            go.AddComponent<CompanionContentSettings>();
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("fuck me doggy", "hub");
            while (!task.IsCompleted)
                yield return null;

            Assert.AreEqual(ExplicitAct.Doggy, task.Result.Act);
            Assert.AreEqual(CompanionSceneId.Private, task.Result.SwitchToScene);

            Object.Destroy(go);
        }

        [Test]
        public void OutfitDisplayNames_AreNonEmpty()
        {
            foreach (OutfitId id in System.Enum.GetValues(typeof(OutfitId)))
                Assert.IsNotEmpty(OutfitController.DisplayName(id));
        }
    }
}
