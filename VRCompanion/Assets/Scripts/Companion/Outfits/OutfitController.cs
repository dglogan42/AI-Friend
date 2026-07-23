using System;
using System.Collections.Generic;
using UnityEngine;
using VRCompanion.Content;

namespace VRCompanion.Outfits
{
    /// <summary>
    /// Suggestive / explicit outfit changes by retinting and hiding CLOTH materials
    /// on the VRM character (Tops/Shoes/etc.). Works on multi-material body meshes
    /// without separate clothing prefabs.
    /// </summary>
    public sealed class OutfitController : MonoBehaviour
    {
        [SerializeField] Transform characterRoot;
        [SerializeField] OutfitId current = OutfitId.Default;
        [SerializeField] bool requireNsfwForNude = true;

        readonly List<ClothSlot> _slots = new List<ClothSlot>();
        bool _cached;

        public OutfitId Current => current;
        public event Action<OutfitId> OutfitChanged;

        struct ClothSlot
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public Material RuntimeMat;
            public Color OriginalColor;
            public string Name;
            public bool IsTops;
            public bool IsShoes;
            public bool IsAccessory;
        }

        void Awake()
        {
            if (characterRoot == null)
                characterRoot = transform;
            CacheSlots();
        }

        public void SetCharacterRoot(Transform root)
        {
            characterRoot = root;
            _cached = false;
            _slots.Clear();
            CacheSlots();
            Apply(current, force: true);
        }

        void CacheSlots()
        {
            if (_cached || characterRoot == null)
                return;

            foreach (var r in characterRoot.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials; // instance materials
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null)
                        continue;
                    string n = mat.name;
                    if (n.IndexOf("CLOTH", StringComparison.OrdinalIgnoreCase) < 0
                        && n.IndexOf("Tops", StringComparison.OrdinalIgnoreCase) < 0
                        && n.IndexOf("Shoes", StringComparison.OrdinalIgnoreCase) < 0
                        && n.IndexOf("Skirt", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    _slots.Add(new ClothSlot
                    {
                        Renderer = r,
                        MaterialIndex = i,
                        RuntimeMat = mat,
                        OriginalColor = ReadColor(mat),
                        Name = n,
                        IsTops = n.IndexOf("Tops", StringComparison.OrdinalIgnoreCase) >= 0
                                 || n.IndexOf("Top", StringComparison.OrdinalIgnoreCase) >= 0,
                        IsShoes = n.IndexOf("Shoes", StringComparison.OrdinalIgnoreCase) >= 0
                                  || n.IndexOf("Shoe", StringComparison.OrdinalIgnoreCase) >= 0,
                        IsAccessory = n.IndexOf("Accessory", StringComparison.OrdinalIgnoreCase) >= 0
                                      || n.IndexOf("CatEar", StringComparison.OrdinalIgnoreCase) >= 0,
                    });
                }
            }

            _cached = true;
            Debug.Log($"[OutfitController] Cached {_slots.Count} cloth slot(s) under {characterRoot.name}");
        }

        public bool TrySetOutfit(OutfitId id)
        {
            var content = CompanionContentSettings.Resolve(gameObject);
            if (content != null)
            {
                if (id >= OutfitId.Suggestive && !content.AllowIntimate)
                    return false;
                if (requireNsfwForNude && id >= OutfitId.Lingerie && !content.AllowNsfw)
                    return false;
            }

            Apply(id, force: false);
            return true;
        }

        public void Apply(OutfitId id, bool force = false)
        {
            CacheSlots();
            if (!force && current == id)
                return;
            current = id;

            foreach (var slot in _slots)
            {
                if (slot.RuntimeMat == null)
                    continue;

                // Keep cat ears / accessories unless fully nude optional — keep ears always.
                if (slot.IsAccessory)
                {
                    WriteColor(slot.RuntimeMat, slot.OriginalColor);
                    SetVisible(slot, true);
                    continue;
                }

                switch (id)
                {
                    case OutfitId.Default:
                        WriteColor(slot.RuntimeMat, slot.OriginalColor);
                        SetVisible(slot, true);
                        SetAlpha(slot.RuntimeMat, 1f);
                        break;

                    case OutfitId.Casual:
                        WriteColor(slot.RuntimeMat, Color.Lerp(slot.OriginalColor, new Color(0.55f, 0.65f, 0.95f), 0.35f));
                        SetVisible(slot, true);
                        SetAlpha(slot.RuntimeMat, 1f);
                        break;

                    case OutfitId.Suggestive:
                        // Tighter/darker look; hide shoes.
                        WriteColor(slot.RuntimeMat, Color.Lerp(slot.OriginalColor, new Color(0.25f, 0.08f, 0.12f), 0.55f));
                        SetVisible(slot, !slot.IsShoes);
                        SetAlpha(slot.RuntimeMat, slot.IsShoes ? 0f : 1f);
                        break;

                    case OutfitId.Lingerie:
                        WriteColor(slot.RuntimeMat, new Color(0.15f, 0.05f, 0.08f));
                        if (slot.IsTops)
                            WriteColor(slot.RuntimeMat, new Color(0.75f, 0.25f, 0.4f));
                        SetVisible(slot, !slot.IsShoes);
                        SetAlpha(slot.RuntimeMat, slot.IsShoes ? 0f : 0.92f);
                        break;

                    case OutfitId.Micro:
                        // Barely-there: heavy alpha on tops, hide shoes.
                        WriteColor(slot.RuntimeMat, new Color(0.9f, 0.35f, 0.5f));
                        SetVisible(slot, !slot.IsShoes);
                        SetAlpha(slot.RuntimeMat, slot.IsTops ? 0.35f : (slot.IsShoes ? 0f : 0.55f));
                        break;

                    case OutfitId.Nude:
                        SetVisible(slot, false);
                        SetAlpha(slot.RuntimeMat, 0f);
                        break;
                }
            }

            OutfitChanged?.Invoke(current);
            Debug.Log($"[OutfitController] Outfit → {current}");
        }

        public static string DisplayName(OutfitId id) => id switch
        {
            OutfitId.Default => "default clothes",
            OutfitId.Casual => "casual look",
            OutfitId.Suggestive => "something tighter and more revealing",
            OutfitId.Lingerie => "lingerie",
            OutfitId.Micro => "barely anything",
            OutfitId.Nude => "nothing at all",
            _ => id.ToString()
        };

        public static bool TryParseOutfitCommand(string text, out OutfitId id)
        {
            id = OutfitId.Default;
            if (string.IsNullOrEmpty(text))
                return false;
            text = text.ToLowerInvariant();

            if (text.Contains("nude") || text.Contains("naked") || text.Contains("strip fully") || text.Contains("take everything off") || text.Contains("undress completely"))
            {
                id = OutfitId.Nude;
                return true;
            }
            if (text.Contains("micro") || text.Contains("sling") || text.Contains("barely") || text.Contains("tiny outfit"))
            {
                id = OutfitId.Micro;
                return true;
            }
            if (text.Contains("lingerie") || text.Contains("underwear") || text.Contains("bra") || text.Contains("panties"))
            {
                id = OutfitId.Lingerie;
                return true;
            }
            if (text.Contains("suggestive") || text.Contains("sexy outfit") || text.Contains("revealing") || text.Contains("dress sexy") || text.Contains("slutty"))
            {
                id = OutfitId.Suggestive;
                return true;
            }
            if (text.Contains("casual") || text.Contains("normal clothes") || text.Contains("everyday"))
            {
                id = OutfitId.Casual;
                return true;
            }
            if (text.Contains("get dressed") || text.Contains("put clothes") || text.Contains("default outfit") || text.Contains("cover up") || text.Contains("dress normal"))
            {
                id = OutfitId.Default;
                return true;
            }
            if (text.Contains("strip") || text.Contains("undress") || text.Contains("take off"))
            {
                // Partial strip → micro if already suggestive, else lingerie
                id = OutfitId.Lingerie;
                return true;
            }
            if (text.Contains("change outfit") || text.Contains("change clothes") || text.Contains("wear something"))
            {
                id = OutfitId.Suggestive;
                return true;
            }
            return false;
        }

        static Color ReadColor(Material mat)
        {
            if (mat.HasProperty("_Color"))
                return mat.GetColor("_Color");
            if (mat.HasProperty("_BaseColor"))
                return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color0"))
                return mat.GetColor("_Color0");
            return Color.white;
        }

        static void WriteColor(Material mat, Color c)
        {
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color0"))
                mat.SetColor("_Color0", c);
            if (mat.HasProperty("_ShadeColor"))
                mat.SetColor("_ShadeColor", c * 0.7f);
        }

        static void SetAlpha(Material mat, float a)
        {
            a = Mathf.Clamp01(a);
            if (mat.HasProperty("_Color"))
            {
                var c = mat.GetColor("_Color");
                c.a = a;
                mat.SetColor("_Color", c);
            }
            if (mat.HasProperty("_BaseColor"))
            {
                var c = mat.GetColor("_BaseColor");
                c.a = a;
                mat.SetColor("_BaseColor", c);
            }
            // MToon transparency-ish
            if (mat.HasProperty("_Cutoff") && a < 0.99f)
                mat.SetFloat("_Cutoff", Mathf.Lerp(0.5f, 0.05f, 1f - a));
        }

        static void SetVisible(ClothSlot slot, bool visible)
        {
            // Per-material hide is limited; we zero alpha and also skip drawing via material keyword when possible.
            if (!visible)
                SetAlpha(slot.RuntimeMat, 0f);
        }
    }
}
