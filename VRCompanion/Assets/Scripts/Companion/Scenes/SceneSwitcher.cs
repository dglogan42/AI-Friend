using System;
using UnityEngine;
using VRCompanion.Dialogue;

namespace VRCompanion.Scenes
{
    /// <summary>
    /// Swaps lightweight in-scene "locations" (not additive Unity scenes) for café/shop/hub.
    /// Keeps the companion + XR rig alive while changing environment visuals.
    /// </summary>
    public sealed class SceneSwitcher : MonoBehaviour
    {
        [Serializable]
        public class Location
        {
            public CompanionSceneId Id;
            public string DisplayName;
            public GameObject Root;
            public Color AmbientTint = Color.white;
        }

        [SerializeField] Location[] locations;
        [SerializeField] CompanionSceneId defaultLocation = CompanionSceneId.Hub;
        [SerializeField] Light environmentLight;

        public CompanionSceneId Current { get; private set; }
        public string CurrentDisplayName { get; private set; } = "Hub";
        public event Action<CompanionSceneId> LocationChanged;

        void Start()
        {
            if (locations == null || locations.Length == 0)
                BuildDefaultLocations();

            SwitchTo(defaultLocation, force: true);
        }

        public void SwitchTo(CompanionSceneId id, bool force = false)
        {
            if (!force && Current == id)
                return;

            Current = id;
            foreach (var loc in locations)
            {
                if (loc?.Root == null)
                    continue;
                bool active = loc.Id == id;
                loc.Root.SetActive(active);
                if (active)
                {
                    CurrentDisplayName = string.IsNullOrEmpty(loc.DisplayName) ? loc.Id.ToString() : loc.DisplayName;
                    if (environmentLight != null)
                        environmentLight.color = loc.AmbientTint;
                }
            }

            LocationChanged?.Invoke(id);
            Debug.Log($"[SceneSwitcher] Now at {CurrentDisplayName}");
        }

        void BuildDefaultLocations()
        {
            // Runtime fallback if inspector not wired (CompanionBootstrap creates these).
            locations = Array.Empty<Location>();
        }

        public void SetLocations(Location[] locs) => locations = locs;
        public void SetEnvironmentLight(Light light) => environmentLight = light;
    }
}
