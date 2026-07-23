using UnityEngine;
using VRCompanion.Dialogue;
using VRCompanion.Scenes;
using VRCompanion.Speech;
using VRCompanion.Singing;
using VRCompanion.Body;

namespace VRCompanion
{
    /// <summary>
    /// Builds a playable stub scene at runtime if the hierarchy was empty:
    /// floor, three location props, companion capsule, XR camera, services.
    /// </summary>
    public sealed class CompanionBootstrap : MonoBehaviour
    {
        [SerializeField] bool buildIfEmpty = true;

        void Awake()
        {
            if (!buildIfEmpty)
                return;

            if (FindFirstObjectByType<CompanionController>() != null)
                return;

            Build();
        }

        [ContextMenu("Build Stub Scene")]
        public void Build()
        {
            EnsureCamera();
            EnsureLight();

            var world = new GameObject("World");
            var floor = CreatePrimitive(PrimitiveType.Plane, "Floor", world.transform, Vector3.zero, new Vector3(2f, 1f, 2f), new Color(0.25f, 0.27f, 0.3f));

            var locationsRoot = new GameObject("Locations");
            locationsRoot.transform.SetParent(world.transform, false);

            var hub = CreateLocation("Hub", locationsRoot.transform, new Color(0.35f, 0.4f, 0.5f), new Vector3(0f, 0.5f, 2.5f));
            var cafe = CreateLocation("Cafe", locationsRoot.transform, new Color(0.55f, 0.35f, 0.25f), new Vector3(-2.5f, 0.5f, 2.5f));
            var shop = CreateLocation("Shop", locationsRoot.transform, new Color(0.3f, 0.5f, 0.35f), new Vector3(2.5f, 0.5f, 2.5f));

            // Simple props for flavour
            CreatePrimitive(PrimitiveType.Cylinder, "CafeTable", cafe.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.4f, 0.8f), new Color(0.4f, 0.25f, 0.15f));
            CreatePrimitive(PrimitiveType.Cube, "ShopCounter", shop.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 0.5f), new Color(0.45f, 0.4f, 0.3f));
            CreatePrimitive(PrimitiveType.Cube, "HubSign", hub.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.2f, 0.3f, 0.1f), new Color(0.7f, 0.75f, 0.9f));

            var companionGo = new GameObject("Companion");
            companionGo.transform.position = new Vector3(0f, 0f, 1.5f);

            var body = CreatePrimitive(PrimitiveType.Capsule, "Body", companionGo.transform, new Vector3(0f, 1f, 0f), Vector3.one, new Color(0.9f, 0.75f, 0.7f));
            var earL = CreatePrimitive(PrimitiveType.Cube, "EarL", companionGo.transform, new Vector3(-0.25f, 1.85f, 0f), new Vector3(0.18f, 0.28f, 0.1f), new Color(0.95f, 0.7f, 0.75f));
            var earR = CreatePrimitive(PrimitiveType.Cube, "EarR", companionGo.transform, new Vector3(0.25f, 1.85f, 0f), new Vector3(0.18f, 0.28f, 0.1f), new Color(0.95f, 0.7f, 0.75f));
            earL.transform.rotation = Quaternion.Euler(0f, 0f, 15f);
            earR.transform.rotation = Quaternion.Euler(0f, 0f, -15f);

            var expression = body.AddComponent<ExpressionController>();
            var dialogue = companionGo.AddComponent<DialogueService>();
            var asr = companionGo.AddComponent<StubAsrService>();
            var tts = companionGo.AddComponent<StubTtsService>();
            var controller = companionGo.AddComponent<CompanionController>();

            var webcamFace = companionGo.AddComponent<WebcamFaceTrackingSource>();
            var viveFace = companionGo.AddComponent<ViveFaceTrackingSource>();
            var faceBridge = companionGo.AddComponent<FaceTrackingBridge>();
            faceBridge.Configure(expression, webcamFace, viveFace);

            companionGo.AddComponent<SingingRaterService>();
            companionGo.AddComponent<KinectBodyTrackingSource>();

            var switcherGo = new GameObject("SceneSwitcher");
            var switcher = switcherGo.AddComponent<SceneSwitcher>();
            var light = FindFirstObjectByType<Light>();
            switcher.SetEnvironmentLight(light);
            switcher.SetLocations(new[]
            {
                new SceneSwitcher.Location { Id = CompanionSceneId.Hub, DisplayName = "Hub", Root = hub, AmbientTint = new Color(0.85f, 0.9f, 1f) },
                new SceneSwitcher.Location { Id = CompanionSceneId.Cafe, DisplayName = "Café", Root = cafe, AmbientTint = new Color(1f, 0.85f, 0.7f) },
                new SceneSwitcher.Location { Id = CompanionSceneId.Shop, DisplayName = "Shop", Root = shop, AmbientTint = new Color(0.75f, 1f, 0.85f) },
            });

            // Wire via reflection-free public flow: CompanionController fills missing refs in Awake.
            // Force re-awake wiring by disabling/enabling.
            controller.enabled = false;
            controller.enabled = true;

            Debug.Log("[CompanionBootstrap] Stub scene ready. Press Space in Play Mode to talk.");
        }

        static void EnsureCamera()
        {
            if (Camera.main != null)
                return;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 1.4f, 0f);
            camGo.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        }

        static void EnsureLight()
        {
            if (FindFirstObjectByType<Light>() != null)
                return;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static GameObject CreateLocation(string name, Transform parent, Color color, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            CreatePrimitive(PrimitiveType.Cube, name + "Marker", go.transform, Vector3.zero, new Vector3(0.6f, 0.1f, 0.6f), color);
            return go;
        }

        static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                // URP/Built-in both accept property blocks later; set shared material color when possible.
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
                renderer.sharedMaterial = mat;
            }
            return go;
        }
    }
}
