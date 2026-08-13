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
        /// <summary>Optional male drop-in stems (Qifrey/Yellow plus user-owned JJK-style files).</summary>
        public static readonly string[] MaleStems =
        {
            "Qifrey", "Yellow", "CatEarsBoy",
            "Gojo", "Geto", "Nanami", "Itadori", "Yuji", "Megumi", "Toji",
            "Sukuna", "Yuta", "Todo", "Choso",
        };

        public const string MaleVariantPrefsKey = "VRCompanion.MaleVariant";

        public static string[] MaleModelCandidatePaths()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string env = Environment.GetEnvironmentVariable("VRCOMPANION_MALE_VRM")
                         ?? Environment.GetEnvironmentVariable("VRCOMPANION_MALE_MODEL");
            string streaming = Path.Combine(Application.streamingAssetsPath, "Characters");
            string models = Path.Combine(home, ".vrcompanion", "models");
            string resources = Path.Combine(Application.dataPath, "Resources", "Characters");

            var list = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(env))
            {
                if (File.Exists(env))
                    list.Add(env);
                else
                    AddStemPaths(list, env, streaming, models, resources);
            }

            string preferred = PlayerPrefs.GetString(MaleVariantPrefsKey, "");
            if (!string.IsNullOrEmpty(preferred))
                AddStemPaths(list, preferred, streaming, models, resources);

            foreach (var stem in MaleStems)
                AddStemPaths(list, stem, streaming, models, resources);

            return list.ToArray();
        }

        // Back-compat alias
        /// <summary>ASCII drop-in stems for 空猫 Nilcat Hub characters (see docs/character-references/Nilcat).</summary>
        public static readonly string[] NilcatFemaleStems =
        {
            "Kuroto", "KurotoSwimsuit",
            "Seiya", "SeiyaSwimsuit",
            "Rincat",
            "HaiyuZero",
            "Komoe", "KomoeMemorial",
            "YumemiMero",
            "Eliolot",
            "Hitomi",
            "Sitari",
            "Yobipai",
            "ShiuMoku",
            "NinerNine",
            "Yukai",
            "Minne",
            "Philorty",
            "MikageTantian",
            "Eimi",
        };

        public const string FemaleVariantPrefsKey = "VRCompanion.FemaleVariant";

        public static string[] FemaleModelCandidatePaths()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string env = Environment.GetEnvironmentVariable("VRCOMPANION_FEMALE_VRM")
                         ?? Environment.GetEnvironmentVariable("VRCOMPANION_FEMALE_MODEL");
            string streaming = Path.Combine(Application.streamingAssetsPath, "Characters");
            string models = Path.Combine(home, ".vrcompanion", "models");
            string resources = Path.Combine(Application.dataPath, "Resources", "Characters");

            var list = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(env))
            {
                if (File.Exists(env))
                    list.Add(env);
                else
                    AddStemPaths(list, env, streaming, models, resources);
            }

            string preferred = PlayerPrefs.GetString(FemaleVariantPrefsKey, "");
            if (!string.IsNullOrEmpty(preferred) &&
                !preferred.Equals("builtin", StringComparison.OrdinalIgnoreCase))
                AddStemPaths(list, preferred, streaming, models, resources);

            foreach (var stem in NilcatFemaleStems)
                AddStemPaths(list, stem, streaming, models, resources);

            AppendLooseFiles(list, streaming);
            AppendLooseFiles(list, models);
            return list.ToArray();
        }

        static void AddStemPaths(
            System.Collections.Generic.List<string> list,
            string stem,
            string streaming,
            string models,
            string resources)
        {
            if (string.IsNullOrEmpty(stem))
                return;
            stem = stem.Trim().Trim('"', '\'');
            foreach (var dir in new[] { streaming, models, Path.Combine(resources, stem) })
            {
                list.Add(Path.Combine(dir, stem + ".vrm"));
                list.Add(Path.Combine(dir, stem + ".glb"));
            }
        }

        static readonly string[] MaleFileHints =
        {
            "qifrey", "yellow", "catearsboy",
            "gojo", "geto", "nanami", "itadori", "yuji", "megumi", "toji",
            "sukuna", "yuta", "todo", "choso",
        };

        static void AppendLooseFiles(System.Collections.Generic.List<string> list, string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;
            foreach (var file in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(file)?.ToLowerInvariant();
                if (ext != ".vrm" && ext != ".glb")
                    continue;
                string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                bool male = false;
                foreach (var hint in MaleFileHints)
                {
                    if (name.Contains(hint))
                    {
                        male = true;
                        break;
                    }
                }
                if (!male)
                    list.Add(file);
            }
        }

        public static string FindExistingFemaleVrmPath()
        {
            string preferred = PlayerPrefs.GetString(FemaleVariantPrefsKey, "");
            if (preferred.Equals("builtin", StringComparison.OrdinalIgnoreCase))
                return null;

            foreach (var p in FemaleModelCandidatePaths())
            {
                if (string.IsNullOrEmpty(p))
                    continue;
                if (File.Exists(p))
                    return p;
            }
            return null;
        }

        /// <summary>Installed female drop-ins plus "builtin" (Cat-ears Girl prefab).</summary>
        public static string[] ListFemaleVariantIds()
        {
            var ids = new System.Collections.Generic.List<string> { "builtin" };
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "builtin" };
            foreach (var p in FemaleModelCandidatePaths())
            {
                if (string.IsNullOrEmpty(p) || !File.Exists(p))
                    continue;
                string id = Path.GetFileNameWithoutExtension(p);
                if (seen.Add(id))
                    ids.Add(id);
            }
            return ids.ToArray();
        }

        public static string CycleFemaleVariant()
        {
            var ids = ListFemaleVariantIds();
            string cur = PlayerPrefs.GetString(FemaleVariantPrefsKey, "");
            int i = 0;
            for (int n = 0; n < ids.Length; n++)
            {
                if (ids[n].Equals(cur, StringComparison.OrdinalIgnoreCase))
                {
                    i = n;
                    break;
                }
            }
            // If a file is loaded with empty prefs, treat as first non-builtin if present.
            if (string.IsNullOrEmpty(cur) && ids.Length > 1 && FindExistingFemaleVrmPath() != null)
                i = 1;
            string next = ids[(i + 1) % ids.Length];
            PlayerPrefs.SetString(FemaleVariantPrefsKey, next);
            PlayerPrefs.Save();
            return next;
        }

        public static GameObject TryLoadFemaleVrm(Transform parent)
        {
            string path = FindExistingFemaleVrmPath();
            return path == null ? null : LoadFromPath(path, parent);
        }

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

        public static string[] ListMaleVariantIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in MaleModelCandidatePaths())
            {
                if (string.IsNullOrEmpty(p) || !File.Exists(p))
                    continue;
                string id = Path.GetFileNameWithoutExtension(p);
                if (seen.Add(id))
                    ids.Add(id);
            }
            return ids.ToArray();
        }

        public static string CycleMaleVariant()
        {
            var ids = ListMaleVariantIds();
            if (ids.Length == 0)
                return "";
            string cur = PlayerPrefs.GetString(MaleVariantPrefsKey, "");
            int i = 0;
            for (int n = 0; n < ids.Length; n++)
            {
                if (ids[n].Equals(cur, StringComparison.OrdinalIgnoreCase))
                {
                    i = n;
                    break;
                }
            }
            string next = ids[(i + 1) % ids.Length];
            PlayerPrefs.SetString(MaleVariantPrefsKey, next);
            PlayerPrefs.Save();
            return next;
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
