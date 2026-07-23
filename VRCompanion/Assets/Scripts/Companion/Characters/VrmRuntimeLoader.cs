using System;
using System.IO;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using VRM;

namespace VRCompanion.Characters
{
    /// <summary>
    /// Loads the male companion model from disk at runtime.
    /// Accepts .vrm or .glb (Yellow is often exported as GLB with VRM extensions).
    /// File must not be redistributed — local install only.
    /// </summary>
    public static class VrmRuntimeLoader
    {
        /// <summary>
        /// Candidate paths for the male Yellow model (first existing file wins).
        /// </summary>
        public static string[] MaleModelCandidatePaths()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string env = Environment.GetEnvironmentVariable("VRCOMPANION_MALE_VRM")
                         ?? Environment.GetEnvironmentVariable("VRCOMPANION_MALE_MODEL");

            string streaming = Application.streamingAssetsPath;
            string resourcesBoy = Path.Combine(Application.dataPath, "Resources", "Characters", "CatEarsBoy");
            string models = Path.Combine(home, ".vrcompanion", "models");

            return new[]
            {
                env,
                // StreamingAssets (preferred for drop-in without Editor reimport)
                Path.Combine(streaming, "Characters", "CatEarsBoy.glb"),
                Path.Combine(streaming, "Characters", "CatEarsBoy.vrm"),
                Path.Combine(streaming, "Characters", "Yellow.glb"),
                Path.Combine(streaming, "Characters", "Yellow.vrm"),
                // Resources folder (may also be used after Editor import)
                Path.Combine(resourcesBoy, "CatEarsBoy.glb"),
                Path.Combine(resourcesBoy, "CatEarsBoy.vrm"),
                Path.Combine(resourcesBoy, "Yellow.glb"),
                Path.Combine(resourcesBoy, "Yellow.vrm"),
                // User home cache
                Path.Combine(models, "CatEarsBoy.glb"),
                Path.Combine(models, "CatEarsBoy.vrm"),
                Path.Combine(models, "Yellow.glb"),
                Path.Combine(models, "Yellow.vrm"),
                // Accidental drop at project root (dev convenience)
                Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? "", "2531419525577818593.glb"),
                Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? "", "CatEarsBoy.glb"),
                Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? "", "Yellow.glb"),
            };
        }

        // Back-compat alias
        public static string[] MaleVrmCandidatePaths() => MaleModelCandidatePaths();

        public static string FindExistingMaleVrmPath()
        {
            foreach (var p in MaleModelCandidatePaths())
            {
                if (string.IsNullOrEmpty(p))
                    continue;
                if (File.Exists(p))
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Synchronous-style load using ImmediateCaller (OK for bootstrap / editor tests).
        /// Returns the root GameObject, or null on failure.
        /// </summary>
        public static GameObject LoadFromPath(string path, Transform parent)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                RuntimeGltfInstance instance = LoadInstanceSync(path);
                if (instance == null)
                    return null;

                instance.ShowMeshes();
                var go = instance.gameObject;
                go.name = "Body";
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                Debug.Log($"[VrmRuntimeLoader] Loaded model from {path}");
                return go;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VrmRuntimeLoader] Failed to load '{path}': {e.Message}");
                return null;
            }
        }

        static RuntimeGltfInstance LoadInstanceSync(string path)
        {
            var awaiter = new ImmediateCaller();
            string ext = Path.GetExtension(path)?.ToLowerInvariant() ?? "";

            // Prefer VRM path when file is .vrm, or .glb that may embed VRM extensions
            // (Yellow from VRoid Hub re-exported as GLB still carries the VRM extension).
            if (ext == ".vrm" || ext == ".glb")
            {
                try
                {
                    var task = VrmUtility.LoadAsync(path, awaiter);
                    return task.GetAwaiter().GetResult();
                }
                catch (Exception vrmEx)
                {
                    if (ext == ".vrm")
                        throw;
                    Debug.Log($"[VrmRuntimeLoader] VRM parse failed for GLB, falling back to plain glTF: {vrmEx.Message}");
                }
            }

            // Plain glTF / GLB
            {
                var task = GltfUtility.LoadAsync(path, awaiter);
                return task.GetAwaiter().GetResult();
            }
        }

        public static GameObject TryLoadMaleVrm(Transform parent)
        {
            string path = FindExistingMaleVrmPath();
            return path == null ? null : LoadFromPath(path, parent);
        }

        public static async Task<GameObject> LoadFromPathAsync(string path, Transform parent)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                var awaiter = new RuntimeOnlyAwaitCaller();
                string ext = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
                RuntimeGltfInstance instance = null;

                if (ext == ".vrm" || ext == ".glb")
                {
                    try
                    {
                        instance = await VrmUtility.LoadAsync(path, awaiter);
                    }
                    catch (Exception)
                    {
                        if (ext == ".vrm")
                            throw;
                        instance = await GltfUtility.LoadAsync(path, awaiter);
                    }
                }
                else
                {
                    instance = await GltfUtility.LoadAsync(path, awaiter);
                }

                if (instance == null)
                    return null;
                instance.ShowMeshes();
                var go = instance.gameObject;
                go.name = "Body";
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                return go;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VrmRuntimeLoader] Async load failed '{path}': {e.Message}");
                return null;
            }
        }
    }
}
