using System;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Costume
{
    /// <summary>What tapping an item card did to the outfit.</summary>
    public enum ItemTapOutcome
    {
        /// <summary>Nothing changed (unknown item, or an item of another category).</summary>
        None,
        /// <summary>The item is now worn (a locked premium item is worn as a preview only).</summary>
        Worn,
        /// <summary>It was the worn item, so the slot is bare again.</summary>
        Removed
    }

    /// <summary>
    /// The state of one costume editor session: the preset being edited, the outfit being tried on, whether it differs
    /// from the saved one, the selected category, and what Save / Random / Purchase may do. Pure C# over
    /// <see cref="ICosmeticService"/> (no Unity, DOTween or Zenject types), so it is tested in EditMode. The screen view
    /// only draws what this says and forwards taps.
    /// Rules: a locked (unowned) item may be worn to look at it, but then Save is off; Random keeps the unsaved outfit
    /// apart from the saved one; tapping the worn item takes it off; a new item keeps the colours of the category. The
    /// colour picker edits one colour channel of the worn item at a time (<see cref="ChannelIndex"/>); items have 1 to 3.
    /// </summary>
    public sealed class CostumeEditorModel
    {
        private readonly ICosmeticService _service;
        private CosmeticOutfit _saved = CosmeticOutfit.Empty;
        private int _channel;

        public CostumeEditorModel(ICosmeticService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            Outfit = CosmeticOutfit.Empty;
            Category = CosmeticOutfit.Categories.Length > 0 ? CosmeticOutfit.Categories[0] : default;
        }

        /// <summary>Preset index the session edits.</summary>
        public int PresetIndex { get; private set; }

        /// <summary>The outfit being tried on (what the menu character shows).</summary>
        public CosmeticOutfit Outfit { get; private set; }

        /// <summary>The category whose items the grid lists and whose colour the picker changes.</summary>
        public CosmeticCategory Category { get; private set; }

        public bool IsDirty => !Outfit.Equals(_saved);

        /// <summary>Any worn item that the player does not own.</summary>
        public bool HasLockedItem
        {
            get
            {
                foreach (CosmeticCategory category in CosmeticOutfit.Categories)
                {
                    CosmeticSlot slot = Outfit.Get(category);
                    if (!slot.IsEmpty && _service.IsLocked(slot.CosmeticId)) return true;
                }
                return false;
            }
        }

        /// <summary>Save writes the outfit to the preset; refused while a locked item is worn.</summary>
        public bool CanSave => !HasLockedItem;

        /// <summary>The worn slot of the selected category.</summary>
        public CosmeticSlot SelectedSlot => Outfit.Get(Category);

        /// <summary>The colour picker needs an item to recolour.</summary>
        public bool CanColor => !SelectedSlot.IsEmpty;

        /// <summary>How many colour channels the worn item of the category has (the colour boxes shown); 0 while the slot is bare.</summary>
        public int ChannelCount
        {
            get
            {
                CosmeticSlot slot = SelectedSlot;
                if (slot.IsEmpty) return 0;
                return _service.TryGetItem(slot.CosmeticId, out CosmeticItemInfo item) ? item.ChannelCount : 1;
            }
        }

        /// <summary>The channel the picker edits; always inside the worn item's channels.</summary>
        public int ChannelIndex => Math.Min(_channel, Math.Max(0, ChannelCount - 1));

        /// <summary>Makes the picker edit another channel. False for a channel the worn item does not have.</summary>
        public bool SelectChannel(int index)
        {
            if (index < 0 || index >= ChannelCount) return false;
            _channel = index;
            return true;
        }

        /// <summary>
        /// The colour a channel box shows: the colour the player set, else the colour its material has by default (from the
        /// catalog); empty only when neither is known.
        /// </summary>
        public string ChannelHex(int index)
        {
            CosmeticSlot slot = SelectedSlot;
            if (slot.IsEmpty) return string.Empty;
            string set = slot.ColorAt(index);
            if (!string.IsNullOrEmpty(set)) return set;
            return _service.TryGetItem(slot.CosmeticId, out CosmeticItemInfo item) ? item.DefaultColorAt(index) : string.Empty;
        }

        /// <summary>Starts a session on the selected preset: the working outfit is the saved one.</summary>
        public void Begin()
        {
            PresetIndex = _service.SelectedPreset;
            Revert();
        }

        /// <summary>Picks another preset (saved at once by the service) and shows its saved outfit. False for a bad index.</summary>
        public bool SelectPreset(int index)
        {
            if (!_service.SelectPreset(index)) return false;
            PresetIndex = index;
            Revert();
            return true;
        }

        /// <summary>Drops every unsaved change: the working outfit is the saved one again.</summary>
        public void Revert()
        {
            _saved = _service.GetOutfit(PresetIndex);
            Outfit = _saved;
            _channel = 0;
        }

        public void SelectCategory(CosmeticCategory category)
        {
            Category = category;
            _channel = 0;
        }

        public ItemTapOutcome TapItem(string cosmeticId)
        {
            if (!_service.TryGetItem(cosmeticId, out CosmeticItemInfo item) || item.Category != Category) return ItemTapOutcome.None;

            CosmeticSlot current = Outfit.Get(Category);
            if (string.Equals(current.CosmeticId, cosmeticId, StringComparison.Ordinal))
            {
                Outfit = Outfit.Without(Category);
                _channel = 0;
                return ItemTapOutcome.Removed;
            }

            Outfit = Outfit.WithSlot(Category, new CosmeticSlot(cosmeticId, current.ColorAt(0), current.ColorAt(1), current.ColorAt(2)));
            _channel = 0;
            return ItemTapOutcome.Worn;
        }

        /// <summary>Recolours the selected channel of the worn item of the selected category. False when the slot is bare.</summary>
        public bool SetColor(string colorHex)
        {
            if (!CanColor) return false;
            Outfit = Outfit.WithColor(Category, ChannelIndex, colorHex);
            return true;
        }

        /// <summary>Owned items and palette colours in every category; not saved.</summary>
        public void Randomize(System.Random random)
        {
            Outfit = _service.CreateRandomOutfit(random) ?? CosmeticOutfit.Empty;
            _channel = 0;
        }

        /// <summary>
        /// The worn item of the selected category when it is a premium item the player can buy (locked and priced); the
        /// purchase button shows only then.
        /// </summary>
        public bool TryGetBuyableItem(out CosmeticItemInfo item)
        {
            item = default;
            CosmeticSlot slot = SelectedSlot;
            if (slot.IsEmpty || !_service.TryGetItem(slot.CosmeticId, out item)) return false;
            return item.IsPremium && item.IsLocked && item.IsForSale;
        }

        /// <summary>Buys the buyable item of the selected category. <see cref="CosmeticPurchaseResult.UnknownItem"/> when there is none.</summary>
        public CosmeticPurchaseResult Purchase()
        {
            if (!TryGetBuyableItem(out CosmeticItemInfo item)) return CosmeticPurchaseResult.UnknownItem;
            return _service.TryPurchase(item.Id);
        }

        /// <summary>Writes the outfit to the preset. A refused save changes nothing.</summary>
        public CosmeticSaveResult Save()
        {
            if (HasLockedItem) return CosmeticSaveResult.LockedItem;

            CosmeticSaveResult result = _service.SaveOutfit(PresetIndex, Outfit);
            // NotPersisted: kept in memory only, but it is still the saved outfit of this run.
            if (result == CosmeticSaveResult.Saved || result == CosmeticSaveResult.NotPersisted) _saved = Outfit;
            return result;
        }
    }

    /// <summary>The swing of a shake over its length, as a pure curve (the view feeds it to a tween).</summary>
    public static class ShakeCurve
    {
        private const float TwoPi = 6.2831853f;

        /// <summary>
        /// Offset at <paramref name="progress"/> (0 = start, 1 = end): swings <paramref name="cycles"/> times to each side and
        /// dies down, so it starts and ends at 0 (the button returns to where the designer put it).
        /// </summary>
        public static float Offset(float progress, float strength, int cycles)
        {
            if (progress <= 0f || progress >= 1f) return 0f;
            double swing = Math.Sin(progress * TwoPi * Math.Max(1, cycles));
            return (float)(swing * (1f - progress)) * strength;
        }
    }
}
