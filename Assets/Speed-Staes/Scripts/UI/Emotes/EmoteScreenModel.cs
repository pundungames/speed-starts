using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Emotes
{
    /// <summary>What the main action under the pedestal does for the emote that is selected.</summary>
    public enum EmoteAction
    {
        /// <summary>Nothing selected.</summary>
        None,
        /// <summary>Owned, not equipped: equip it.</summary>
        Equip,
        /// <summary>Owned and equipped already.</summary>
        Equipped,
        /// <summary>Not owned: buy it.</summary>
        Buy
    }

    /// <summary>
    /// State of the emote screen without any Unity object: which emote is selected (and so previewed), what the action
    /// button does, and the two actions. Selecting never saves or spends anything; the service does that.
    /// </summary>
    public sealed class EmoteScreenModel
    {
        private readonly IEmoteService _service;

        public EmoteScreenModel(IEmoteService service)
        {
            _service = service;
            Begin();
        }

        public string SelectedId { get; private set; } = string.Empty;

        /// <summary>Opening the screen: the equipped emote is the selected one.</summary>
        public void Begin() => SelectedId = _service.EquippedId;

        /// <summary>Selects an emote (also a locked one, to preview it). False for an unknown id.</summary>
        public bool Select(string id)
        {
            if (!_service.TryGet(id, out _)) return false;
            SelectedId = id;
            return true;
        }

        public EmoteAction Action
        {
            get
            {
                if (!_service.TryGet(SelectedId, out _)) return EmoteAction.None;
                if (!_service.IsOwned(SelectedId)) return EmoteAction.Buy;
                return string.Equals(SelectedId, _service.EquippedId, System.StringComparison.Ordinal) ? EmoteAction.Equipped : EmoteAction.Equip;
            }
        }

        /// <summary>Price of the selected emote; 0 when nothing is selected.</summary>
        public int SelectedPrice => _service.TryGet(SelectedId, out EmoteInfo info) ? info.CashPrice : 0;

        /// <summary>Equips the selected emote when it is owned.</summary>
        public bool Equip() => Action == EmoteAction.Equip && _service.Equip(SelectedId);

        /// <summary>Buys the selected emote. <c>AlreadyOwned</c> is returned for an owned one (nothing is charged).</summary>
        public EmotePurchaseResult Purchase() => _service.TryPurchase(SelectedId);
    }
}
