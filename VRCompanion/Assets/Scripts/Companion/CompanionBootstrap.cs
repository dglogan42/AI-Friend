using UnityEngine;
using VRCompanion.Dialogue;
using VRCompanion.Scenes;
using VRCompanion.Speech;
using VRCompanion.Singing;
using VRCompanion.Body;
using VRCompanion.Vision;
using VRCompanion.Diagnostics;
using VRCompanion.Content;
using VRCompanion.Outfits;
using VRCompanion.Intimacy;
using VRCompanion.Characters;

namespace VRCompanion
{
    /// <summary>
    /// Builds a playable stub scene at runtime if the hierarchy was empty:
    /// floor, location props, companion body (female CatEarsGirl or male CatEarsBoy),
    /// XR camera, services.
    /// </summary>
    public sealed class CompanionBootstrap : MonoBehaviour
    {
        [SerializeField] bool buildIfEmpty = true;

        [Header("Character")]
        [Tooltip("Default companion gender. Overridden by VRCOMPANION_GENDER env or PlayerPrefs.")]
        [SerializeField] CompanionGender defaultGender = CompanionGender.Female;

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
            CompanionPhysics.ApplyProjectDefaults();
            EnsureCamera();
            EnsureLight();

            var world = new GameObject("World");
            var floor = CreatePrimitive(PrimitiveType.Plane, "Floor", world.transform, Vector3.zero, new Vector3(2f, 1f, 2f), new Color(0.25f, 0.27f, 0.3f));
            CompanionPhysics.ConfigureCollider(floor, isStatic: true);

            var locationsRoot = new GameObject("Locations");
            locationsRoot.transform.SetParent(world.transform, false);

            var hub = CreateLocation("Hub", locationsRoot.transform, new Color(0.35f, 0.4f, 0.5f), new Vector3(0f, 0.5f, 2.5f));
            var cafe = CreateLocation("Cafe", locationsRoot.transform, new Color(0.55f, 0.35f, 0.25f), new Vector3(-2.5f, 0.5f, 2.5f));
            var shop = CreateLocation("Shop", locationsRoot.transform, new Color(0.3f, 0.5f, 0.35f), new Vector3(2.5f, 0.5f, 2.5f));
            var privateRoom = CreateLocation("Private", locationsRoot.transform, new Color(0.45f, 0.2f, 0.35f), new Vector3(0f, 0.5f, -2.5f));
            // Soft ambient prop for private space
            var bed = CreatePrimitive(PrimitiveType.Cube, "Daybed", privateRoom.transform, new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.35f, 0.9f), new Color(0.55f, 0.25f, 0.4f));
            CompanionPhysics.ConfigureCollider(bed, isStatic: true);

            // Simple props for flavour — static colliders so they stay put in VR.
            var table = CreatePrimitive(PrimitiveType.Cylinder, "CafeTable", cafe.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.4f, 0.8f), new Color(0.4f, 0.25f, 0.15f));
            CompanionPhysics.ConfigureCollider(table, isStatic: true);
            var counter = CreatePrimitive(PrimitiveType.Cube, "ShopCounter", shop.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 0.5f), new Color(0.45f, 0.4f, 0.3f));
            CompanionPhysics.ConfigureCollider(counter, isStatic: true);
            var sign = CreatePrimitive(PrimitiveType.Cube, "HubSign", hub.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.2f, 0.3f, 0.1f), new Color(0.7f, 0.75f, 0.9f));
            CompanionPhysics.ConfigureCollider(sign, isStatic: true);

            // Small dynamic prop for physics smoke testing in Play Mode.
            var ball = CreatePrimitive(PrimitiveType.Sphere, "PhysicsBall", hub.transform, new Vector3(0.4f, 1.6f, 0.2f), Vector3.one * 0.12f, new Color(0.9f, 0.3f, 0.25f));
            CompanionPhysics.ConfigureCollider(ball, isStatic: false, mass: 0.2f);

            var companionGo = new GameObject("Companion");
            companionGo.transform.position = new Vector3(0f, 0f, 1.5f);

            // Intimacy + NSFW allowed by default (Inspector can disable for SFW demos).
            companionGo.AddComponent<CompanionContentSettings>();

            var profile = companionGo.AddComponent<CompanionCharacterProfile>();
            profile.Gender = CompanionCharacterProfile.ResolveStartupGender(defaultGender);
            profile.SavePreference();

            var body = CreateCharacter(companionGo.transform, profile);
            var expression = body.AddComponent<ExpressionController>();
            var dialogue = companionGo.AddComponent<DialogueService>();
            var asr = companionGo.AddComponent<StubAsrService>();
            var tts = companionGo.AddComponent<StubTtsService>();
            var outfits = companionGo.AddComponent<OutfitController>();
            outfits.SetCharacterRoot(body.transform);
            var explicitActs = companionGo.AddComponent<ExplicitInteractionController>();
            explicitActs.Configure(body.transform, expression, outfits, tts);
            var controller = companionGo.AddComponent<CompanionController>();

            var webcamFace = companionGo.AddComponent<WebcamFaceTrackingSource>();
            var viveFace = companionGo.AddComponent<ViveFaceTrackingSource>();
            var faceBridge = companionGo.AddComponent<FaceTrackingBridge>();
            faceBridge.Configure(expression, webcamFace, viveFace);

            companionGo.AddComponent<SingingRaterService>();
            companionGo.AddComponent<KinectBodyTrackingSource>();
            companionGo.AddComponent<WebcamBodyTrackingSource>();
            companionGo.AddComponent<WebcamImageRecognitionSource>();
            companionGo.AddComponent<CompanionDiagnosticsHud>();
            CreateSingingVisualizer(companionGo.transform, profile.ApproximateHeight);

            var switcherGo = new GameObject("SceneSwitcher");
            var switcher = switcherGo.AddComponent<SceneSwitcher>();
            var light = FindFirstObjectByType<Light>();
            switcher.SetEnvironmentLight(light);
            switcher.SetLocations(new[]
            {
                new SceneSwitcher.Location { Id = CompanionSceneId.Hub, DisplayName = "Hub", Root = hub, AmbientTint = new Color(0.85f, 0.9f, 1f) },
                new SceneSwitcher.Location { Id = CompanionSceneId.Cafe, DisplayName = "Café", Root = cafe, AmbientTint = new Color(1f, 0.85f, 0.7f) },
                new SceneSwitcher.Location { Id = CompanionSceneId.Shop, DisplayName = "Shop", Root = shop, AmbientTint = new Color(0.75f, 1f, 0.85f) },
                new SceneSwitcher.Location { Id = CompanionSceneId.Private, DisplayName = "Private", Root = privateRoom, AmbientTint = new Color(1f, 0.55f, 0.7f) },
            });

            // Wire via reflection-free public flow: CompanionController fills missing refs in Awake.
            // Force re-awake wiring by disabling/enabling.
            controller.enabled = false;
            controller.enabled = true;

            Debug.Log($"[CompanionBootstrap] Stub scene ready ({profile.DisplayName}). Press Space to talk, G to switch gender, O for outfits.");
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

        /// <summary>
        /// Instantiates the selected character:
        /// female → CatEarsGirl Resources prefab;
        /// male → CatEarsBoy prefab, else runtime-loaded Yellow.vrm from disk, else yellow stand-in.
        /// </summary>
        public static GameObject CreateCharacter(Transform parent, CompanionCharacterProfile profile)
        {
            if (profile == null)
            {
                var fallback = CreatePrimitiveStandIn(parent, CompanionGender.Female);
                fallback.name = "Body";
                return fallback;
            }

            var prefab = Resources.Load<GameObject>(profile.ResourcesPath);
            if (prefab != null)
            {
                var character = Object.Instantiate(prefab, parent);
                character.name = "Body";
                character.transform.localPosition = Vector3.zero;
                character.transform.localRotation = Quaternion.identity;
                Debug.Log($"[CompanionBootstrap] Loaded prefab body from Resources/{profile.ResourcesPath}");
                return character;
            }

            // Male: load "Yellow" VRM from disk (not redistributable — user-downloaded).
            if (profile.IsMale)
            {
                var fromDisk = VrmRuntimeLoader.TryLoadMaleVrm(parent);
                if (fromDisk != null)
                {
                    Debug.Log(
                        $"[CompanionBootstrap] Male body: {CompanionCharacterProfile.MaleModelTitle} " +
                        $"by {CompanionCharacterProfile.MaleModelCreator} " +
                        $"(credit required; {CompanionCharacterProfile.MaleModelSourceUrl})");
                    return fromDisk;
                }
            }

            Debug.LogWarning(
                $"[CompanionBootstrap] No prefab/VRM for {profile.DisplayName} — " +
                "using procedural stand-in. See Assets/Resources/Characters/CatEarsBoy/README.md");
            var go = CreatePrimitiveStandIn(parent, profile.Gender);
            go.name = "Body";
            return go;
        }

        /// <summary>
        /// Gender-styled capsule stand-in: female is peach/pink cat-ears; male matches
        /// Yellow (blonde / school-vest gold) until the real VRM is placed on disk.
        /// </summary>
        public static GameObject CreatePrimitiveStandIn(Transform parent, CompanionGender gender)
        {
            bool male = gender == CompanionGender.Male;
            // Male: slightly taller + broader; female: original proportions.
            float bodyY = male ? 1.05f : 1.0f;
            float bodyScaleY = male ? 1.12f : 1.0f;
            float bodyScaleX = male ? 1.08f : 1.0f;
            float hairY = male ? 2.0f : 1.85f;
            float hairX = male ? 0.22f : 0.25f;

            var skin = male
                ? new Color(0.96f, 0.88f, 0.8f)
                : new Color(0.9f, 0.75f, 0.7f);
            // Male hair tufts = blonde (Yellow model); female = pink cat ears.
            var hair = male
                ? new Color(0.95f, 0.82f, 0.35f)
                : new Color(0.95f, 0.7f, 0.75f);
            var accent = male
                ? new Color(0.95f, 0.78f, 0.25f) // yellow tie / vest accent
                : new Color(0.95f, 0.55f, 0.65f);

            var body = CreatePrimitive(PrimitiveType.Capsule, "BodyMesh", parent,
                new Vector3(0f, bodyY, 0f),
                new Vector3(bodyScaleX, bodyScaleY, bodyScaleX),
                skin);
            var root = new GameObject("BodyRoot");
            root.transform.SetParent(parent, false);
            body.transform.SetParent(root.transform, true);

            if (male)
            {
                // Blonde hair blob (no cat ears — Yellow model is human).
                CreatePrimitive(PrimitiveType.Sphere, "Hair", root.transform,
                    new Vector3(0f, hairY, 0f), new Vector3(0.45f, 0.28f, 0.4f), hair);
            }
            else
            {
                var earL = CreatePrimitive(PrimitiveType.Cube, "EarL", root.transform,
                    new Vector3(-hairX, hairY, 0f), new Vector3(0.18f, 0.28f, 0.1f), hair);
                var earR = CreatePrimitive(PrimitiveType.Cube, "EarR", root.transform,
                    new Vector3(hairX, hairY, 0f), new Vector3(0.18f, 0.28f, 0.1f), hair);
                earL.transform.rotation = Quaternion.Euler(0f, 0f, 15f);
                earR.transform.rotation = Quaternion.Euler(0f, 0f, -15f);
            }

            CreatePrimitive(PrimitiveType.Sphere, "Accent", root.transform,
                new Vector3(0f, bodyY + 0.15f, 0.28f),
                Vector3.one * (male ? 0.07f : 0.06f),
                accent);

            // Fake cloth slots so OutfitController can find CLOTH materials on the stand-in.
            // Male tops ≈ yellow sweater vest.
            var tops = CreatePrimitive(PrimitiveType.Cube, "Tops_CLOTH", root.transform,
                new Vector3(0f, bodyY + 0.05f, 0.05f),
                new Vector3(male ? 0.52f : 0.48f, male ? 0.42f : 0.4f, 0.25f),
                male ? new Color(0.95f, 0.75f, 0.25f) : new Color(0.95f, 0.55f, 0.7f));
            RenameMaterial(tops, "N00_002_01_Tops_01_CLOTH");
            var shoes = CreatePrimitive(PrimitiveType.Cube, "Shoes_CLOTH", root.transform,
                new Vector3(0f, 0.08f, 0.05f),
                new Vector3(0.35f, 0.12f, 0.28f),
                male ? new Color(0.12f, 0.12f, 0.14f) : new Color(0.35f, 0.2f, 0.25f));
            RenameMaterial(shoes, "N00_003_01_Shoes_01_CLOTH");

            return root;
        }

        static void RenameMaterial(GameObject go, string materialName)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null || r.sharedMaterial == null)
                return;
            // Instance so we don't rename a shared material asset.
            var mat = r.material;
            mat.name = materialName;
            r.material = mat;
        }

        static void CreateSingingVisualizer(Transform companion, float characterHeight)
        {
            var go = new GameObject("SingingVisualizer");
            go.transform.SetParent(companion, false);
            go.transform.localPosition = new Vector3(0f, characterHeight + 0.25f, 0f);

            var line = go.AddComponent<LineRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            mat.color = new Color(0.6f, 0.9f, 1f);
            line.sharedMaterial = mat;
            line.startColor = line.endColor = new Color(0.6f, 0.9f, 1f);
            line.startWidth = line.endWidth = 0.02f;

            go.AddComponent<SingingVisualizer>();
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
