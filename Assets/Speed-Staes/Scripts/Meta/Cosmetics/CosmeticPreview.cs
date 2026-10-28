using System;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Cosmetics
{
    /// <summary>
    /// The outfit on the menu character. With nothing previewed it follows the selected preset's saved outfit; the costume
    /// screen shows unsaved changes through <c>Show</c> and goes back with <c>Reset</c>. Choosing another preset drops a
    /// running preview (the preview belonged to the old preset). Plain class: the menu character listens to <c>Changed</c>.
    /// </summary>
    public sealed class CosmeticPreview : ICosmeticPreview
    {
        private readonly ICosmeticService _service;
        private CosmeticOutfit _shown;
        private int _lastPreset;

        public CosmeticPreview(ICosmeticService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _lastPreset = _service.SelectedPreset;
            _service.Changed += OnServiceChanged;
        }

        public event Action Changed;

        public bool IsPreviewing => _shown != null;

        public CosmeticOutfit Current => _shown ?? _service.SelectedOutfit;

        public void Show(CosmeticOutfit outfit)
        {
            outfit = outfit ?? CosmeticOutfit.Empty;
            if (_shown != null && _shown.Equals(outfit)) return;
            _shown = outfit;
            Changed?.Invoke();
        }

        public void Reset()
        {
            if (_shown == null) return;
            _shown = null;
            Changed?.Invoke();
        }

        private void OnServiceChanged()
        {
            int preset = _service.SelectedPreset;
            if (preset != _lastPreset)
            {
                _lastPreset = preset;
                _shown = null;
            }
            // A saved outfit or a new preset changes what the menu character wears unless a preview covers it.
            if (_shown == null) Changed?.Invoke();
        }
    }
}
