using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Themes;
using SpeedStars.Physical;
using SpeedStars.Runner;
using SpeedStars.Themes;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Opens the streaming world of the theme picked in the main menu (<see cref="IThemeService.ActiveShowcaseIndex"/>) in the
    /// Race scene and closes the others, exactly the way Serhat's ThemeRunPlayground.SelectTheme does it: hand the runner to the
    /// world, place it relative to the runner's z, call Generate. A runner reset (restart, or the finish frame) rebuilds the world
    /// from the start. Only translates: the selection rule is <see cref="RaceThemeSlots"/>; Serhat's and Malik's code is untouched.
    /// </summary>
    public sealed class RaceThemeView : MonoBehaviour
    {
        [Tooltip("The scene's runner body: the worlds follow its transform and a RunReset rebuilds the world. Set by the Race scene builder.")]
        [SerializeField] private RunnerBody body;

        [Tooltip("Standard city (the configured LoopWay prefab instance). Slot 0 = showcase index 0.")]
        [SerializeField] private LoopWayGenerator standardWorld;

        [Tooltip("Themed worlds in showcase order: slot 1 = element 0 (Egypt), then Rome / Byzantium, Pirate Sea, Floating Sky.")]
        [SerializeField] private ThemedLoopWay[] themedWorlds;

        [Tooltip("World position of the standard city for a runner standing at Authored Runner X (Serhat's playground value).")]
        [SerializeField] private Vector3 standardOrigin = new Vector3(-5.6f, 0f, -7.2f);

        [Tooltip("The runner x the worlds were authored for (their road sits under it). The worlds are shifted by runner x minus this, so the road lies under the Race runner.")]
        [SerializeField] private float authoredRunnerX = -5.63f;

        private IThemeService _themes;
        private bool _subscribed;
        private int _builtSlot = -1;

        [Inject]
        public void Construct(IThemeService themes)
        {
            _themes = themes;
            Initialize();
        }

        private void Awake() => Initialize();

        private void OnEnable() => Initialize();

        /// <summary>Idempotent: listens to runner resets once and builds the world once. Safe before injection (waits for the service).</summary>
        public void Initialize()
        {
            if (body != null && !_subscribed)
            {
                body.RunReset += OnRunReset;
                _subscribed = true;
            }
            if (_themes == null || _builtSlot >= 0) return;
            ShowWorld(_themes.ActiveShowcaseIndex);
        }

        private void OnDisable()
        {
            if (body != null && _subscribed) body.RunReset -= OnRunReset;
            _subscribed = false;
        }

        private void OnRunReset()
        {
            if (_builtSlot >= 0) BuildSlot(_builtSlot); // same world, from the start
        }

        /// <summary>Opens the world for a showcase index (out of range = standard city), closes the rest and builds it at the runner.</summary>
        public void ShowWorld(int showcaseIndex)
        {
            int themedCount = themedWorlds == null ? 0 : themedWorlds.Length;
            int slot = RaceThemeSlots.SlotFor(showcaseIndex, RaceThemeSlots.SlotCountFor(themedCount));
            BuildSlot(slot);
        }

        private void BuildSlot(int slot)
        {
            _builtSlot = slot;
            int themedIndex = RaceThemeSlots.ThemedIndexOf(slot);

            if (standardWorld != null) standardWorld.gameObject.SetActive(themedIndex < 0);
            if (themedWorlds != null)
            {
                for (int i = 0; i < themedWorlds.Length; i++)
                    if (themedWorlds[i] != null) themedWorlds[i].gameObject.SetActive(i == themedIndex);
            }

            Transform runner = body != null ? body.transform : null;
            float z = runner != null ? runner.position.z : 0f;
            float shiftX = runner != null ? runner.position.x - authoredRunnerX : 0f;

            if (themedIndex < 0)
            {
                if (standardWorld == null) return;
                standardWorld.runner = runner;
                float offset = Mathf.Floor((z - standardOrigin.z - standardWorld.startZ) / standardWorld.segmentLength) * standardWorld.segmentLength;
                standardWorld.transform.position = new Vector3(standardOrigin.x + shiftX, standardOrigin.y, standardOrigin.z + offset);
                standardWorld.Generate();
                return;
            }

            ThemedLoopWay world = themedWorlds[themedIndex];
            if (world == null) return;
            world.runner = runner;
            float themedOffset = Mathf.Floor((z - ThemedLoopWay.StartZ) / ThemedLoopWay.SectionLength) * ThemedLoopWay.SectionLength;
            world.transform.position = new Vector3(shiftX, 0f, themedOffset);
            world.Generate();
        }
    }
}
