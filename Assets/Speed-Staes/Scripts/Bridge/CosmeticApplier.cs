using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Cosmetics;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Puts an outfit on one runner instance. Which parts are on and which channel gets which colour is decided by the
    /// engine-free <c>OutfitPlan</c>; this component only carries it out: it switches the objects under
    /// <c>Accessories/&lt;Category&gt;/</c>, writes the primary colour into <c>_Color</c> with a MaterialPropertyBlock
    /// per material slot (the shared toon materials are never touched), and hides the old base-outfit objects at the
    /// root (names come from the catalog). Added to the runner instance by a builder or by the menu/race bridges; the
    /// runner prefab is not edited.
    /// </summary>
    public sealed class CosmeticApplier : MonoBehaviour
    {
        [SerializeField, Tooltip("The runner root (HumanCharacterDummy_F instance). Empty = this object.")]
        Transform runner;

        [SerializeField, Tooltip("Name of the wardrobe object under the runner root; its children are the category slots.")]
        string accessoriesName = "Accessories";

        static readonly int ColorId = Shader.PropertyToID("_Color");

        MaterialPropertyBlock _block;
        Transform _root;
        Transform _accessories;
        bool _warned;

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        /// <summary>Idempotent (also callable where AddComponent skips Awake).</summary>
        public void Initialize()
        {
            if (_root) return;
            _root = runner ? runner : transform;
            _block = new MaterialPropertyBlock();
        }

        /// <summary>Applies the outfit; safe to call again with another one.</summary>
        public void Apply(CosmeticOutfit outfit, CosmeticCatalog catalog)
        {
            if (catalog == null) { Debug.LogWarning("[CosmeticApplier] No cosmetic catalog; outfit not applied.", this); return; }
            Apply(OutfitPlan.Build(outfit, catalog));
        }

        public void Apply(OutfitPlan plan)
        {
            Initialize();
            HideLegacy(plan);
            _accessories = _root.Find(accessoriesName);
            if (_accessories == null)
            {
                if (!_warned) Debug.LogWarning($"[CosmeticApplier] '{accessoriesName}' not found under '{_root.name}'; outfit not applied.", this);
                _warned = true;
                return;
            }
            foreach (SlotPlan slot in plan.Slots) ApplySlot(slot);
        }

        void ApplySlot(SlotPlan slot)
        {
            Transform slotRoot = _accessories.Find(slot.Category.ToString());
            if (slotRoot == null) return;
            foreach (Transform part in slotRoot)
            {
                bool on = slot.IsActive(part.name);
                if (part.gameObject.activeSelf != on) part.gameObject.SetActive(on);
                if (on) Recolor(part, slot);
            }
        }

        /// <summary>Colours only the material slots of the primary channel; the other slots return to their material's own colour.</summary>
        void Recolor(Transform part, SlotPlan slot)
        {
            Color color = Color.white;
            bool colored = slot.HasColor && CosmeticColor.TryParse(slot.ColorHex, out color);
            foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    bool primary = colored && materials[i] != null && materials[i].name.StartsWith(slot.ColorMaterial);
                    if (primary)
                    {
                        _block.Clear();
                        _block.SetColor(ColorId, color);
                        renderer.SetPropertyBlock(_block, i);
                    }
                    else
                    {
                        renderer.SetPropertyBlock(null, i);
                    }
                }
            }
        }

        /// <summary>
        /// The old base outfit sits directly under the runner root with names that collide with category slots
        /// (Shoes, Leggings), so only direct children are looked at, never the wardrobe below.
        /// </summary>
        void HideLegacy(OutfitPlan plan)
        {
            foreach (string name in plan.LegacyObjects)
            {
                Transform child = _root.Find(name);
                if (child != null && child.gameObject.activeSelf) child.gameObject.SetActive(false);
            }
        }
    }
}
