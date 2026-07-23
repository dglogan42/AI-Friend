using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion;
using VRCompanion.Characters;
using VRCompanion.Content;
using VRCompanion.Dialogue;
using VRCompanion.Outfits;

namespace VRCompanion.Tests
{
    public class CharacterGenderTests
    {
        [Test]
        public void CharacterProfile_PathsAndHeights_DifferByGender()
        {
            var go = new GameObject("Profile");
            var p = go.AddComponent<CompanionCharacterProfile>();

            p.Gender = CompanionGender.Female;
            Assert.AreEqual(CompanionCharacterProfile.FemaleResourcePath, p.ResourcesPath);
            Assert.IsTrue(p.IsFemale);
            Assert.Less(p.ApproximateHeight, 1.6f);

            p.Gender = CompanionGender.Male;
            Assert.AreEqual(CompanionCharacterProfile.MaleResourcePath, p.ResourcesPath);
            Assert.IsTrue(p.IsMale);
            Assert.Greater(p.ApproximateHeight, 1.5f);
            StringAssert.Contains("Yellow", p.DisplayName);
            StringAssert.Contains("hannahciel25", CompanionCharacterProfile.MaleModelCreator);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ContentSettings_MaleLlm_MentionsYellowAndMaleAnatomy()
        {
            string male = CompanionContentSettings.BuildLlmSystemInstructions(
                allowIntimate: true, allowNsfw: true, intimacyLevel: 0.8f, CompanionGender.Male);
            StringAssert.Contains("Yellow", male);
            StringAssert.Contains("male", male.ToLowerInvariant());
            StringAssert.DoesNotContain("cowgirl", male.ToLowerInvariant());

            string female = CompanionContentSettings.BuildLlmSystemInstructions(
                allowIntimate: true, allowNsfw: true, intimacyLevel: 0.8f, CompanionGender.Female);
            StringAssert.Contains("Girl", female);
            StringAssert.Contains("cowgirl", female.ToLowerInvariant());
        }

        [Test]
        public void OutfitDisplayName_Lingerie_Gendered()
        {
            Assert.AreEqual("lingerie", OutfitController.DisplayName(OutfitId.Lingerie, male: false));
            Assert.AreEqual("snug underwear", OutfitController.DisplayName(OutfitId.Lingerie, male: true));
        }

        [Test]
        public void Bootstrap_CreatePrimitiveStandIn_MaleTallerThanFemale()
        {
            var root = new GameObject("Root");
            var female = CompanionBootstrap.CreatePrimitiveStandIn(root.transform, CompanionGender.Female);
            var male = CompanionBootstrap.CreatePrimitiveStandIn(root.transform, CompanionGender.Male);

            Assert.IsNotNull(female);
            Assert.IsNotNull(male);
            // Male body mesh should sit higher / scale larger.
            var fMesh = female.transform.Find("BodyMesh");
            var mMesh = male.transform.Find("BodyMesh");
            Assert.IsNotNull(fMesh);
            Assert.IsNotNull(mMesh);
            Assert.Greater(mMesh.localScale.y, fMesh.localScale.y);
            Assert.IsNotNull(male.transform.Find("Tops_CLOTH"), "Male stand-in should expose CLOTH slots for outfits");

            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator Dialogue_Male_WhoAreYou_MentionsYellow()
        {
            var go = new GameObject("MaleDlg");
            go.AddComponent<CompanionContentSettings>();
            var profile = go.AddComponent<CompanionCharacterProfile>();
            profile.Gender = CompanionGender.Male;
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("who are you", "hub");
            while (!task.IsCompleted)
                yield return null;

            StringAssert.Contains("yellow", task.Result.Text.ToLowerInvariant());
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Dialogue_Male_Nsfw_UsesMaleLines()
        {
            var go = new GameObject("MaleNsfw");
            go.AddComponent<CompanionContentSettings>();
            var profile = go.AddComponent<CompanionCharacterProfile>();
            profile.Gender = CompanionGender.Male;
            var dialogue = go.AddComponent<DialogueService>();

            var task = dialogue.ReplyAsync("I want sex with you", "hub");
            while (!task.IsCompleted)
                yield return null;

            Assert.AreEqual(CompanionSceneId.Private, task.Result.SwitchToScene);
            // Male NSFW pool mentions hard / ride me; never "cowgirl" as the only option
            Assert.IsNotEmpty(task.Result.Text);
            Object.Destroy(go);
        }

        [Test]
        public void MaleModelCandidates_IncludeGlbPaths()
        {
            var paths = VrmRuntimeLoader.MaleModelCandidatePaths();
            Assert.IsTrue(
                System.Array.Exists(paths, p => p != null && p.IndexOf("CatEarsBoy.glb", System.StringComparison.OrdinalIgnoreCase) >= 0),
                "expected CatEarsBoy.glb candidate");
            Assert.IsTrue(
                System.Array.Exists(paths, p => p != null && p.IndexOf("Yellow.glb", System.StringComparison.OrdinalIgnoreCase) >= 0),
                "expected Yellow.glb candidate");
        }
    }
}
