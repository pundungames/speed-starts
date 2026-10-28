using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Dresses the Race scene's runner in the selected preset once, when the race scene starts (the outfit cannot change
    /// during a race). Added to the runner by <c>SceneCosmetics</c> (builder), never to the prefab. Injection is optional
    /// so a scene without the cosmetic bindings still runs, with a warning.
    /// </summary>
    public sealed class RaceRunnerOutfit : MonoBehaviour
    {
        [SerializeField, Tooltip("Applier on the runner root. Empty = the one on this object (added when missing).")]
        CosmeticApplier applier;

        ICosmeticService _cosmetics;
        CosmeticCatalog _catalog;

        [Inject]
        void Construct([InjectOptional] ICosmeticService cosmetics, [InjectOptional] CosmeticCatalog catalog)
        {
            _cosmetics = cosmetics;
            _catalog = catalog;
        }

        void Start()
        {
            if (_cosmetics == null || _catalog == null)
            {
                Debug.LogWarning("[RaceRunnerOutfit] ICosmeticService / CosmeticCatalog are not bound; the runner keeps its prefab look.", this);
                return;
            }
            if (!applier) applier = GetComponent<CosmeticApplier>() ?? gameObject.AddComponent<CosmeticApplier>();
            applier.Apply(_cosmetics.SelectedOutfit, _catalog);
        }
    }
}
