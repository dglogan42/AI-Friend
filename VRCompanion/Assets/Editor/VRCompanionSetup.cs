using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRCompanion;

namespace VRCompanion.EditorTools
{
    /// <summary>
    /// Menu helpers to finish XR wiring and create a playable Bootstrap scene.
    /// </summary>
    public static class VRCompanionSetup
    {
        const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        [MenuItem("VR Companion/Create Bootstrap Scene", priority = 0)]
        public static void CreateBootstrapScene()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var bootstrapGo = new GameObject("CompanionBootstrap");
            bootstrapGo.AddComponent<CompanionBootstrap>();

            // Default camera position for seated VR preview
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.4f, 0f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            }

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            AddSceneToBuildSettings(BootstrapScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[VR Companion] Created {BootstrapScenePath}. Enter Play Mode and press Space to talk.");
        }

        [MenuItem("VR Companion/Open Bootstrap Scene", priority = 1)]
        public static void OpenBootstrapScene()
        {
            if (!File.Exists(BootstrapScenePath))
                CreateBootstrapScene();
            else
                EditorSceneManager.OpenScene(BootstrapScenePath);
        }

        [MenuItem("VR Companion/Focus Project Settings XR", priority = 20)]
        public static void OpenXrSettings()
        {
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path))
                return;

            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
