using SpeedStars.Core.Race;
using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>What a runner does with its body when no emote plays at the finish.</summary>
    public enum RunnerFinishReaction
    {
        None,
        /// <summary>Arms up, pumping: the race is won.</summary>
        Cheer,
        /// <summary>Bent over, breathing hard: it is over.</summary>
        Winded
    }

    /// <summary>
    /// A runner after the line: no slide on still legs and no crouch at the end. It takes a few more steps, further and further apart,
    /// through its own physics (RunnerInput.Inject, as the bots run), and as it slows down its waiting stance turns from the crouch into
    /// an upright one (the pose driver's public crouch and ready values, read every frame, so its springs do the standing up). A runner
    /// diving through the line first gets up. A reaction moves the same stance for a while, so it flows in and out the same way. A reset
    /// puts everything back. Malik's code and assets are untouched: the stance is a pose of this component's own. Runs after
    /// RunnerPressGate (-150), so its presses never meet that gate, and before RunnerBody (-100).
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public sealed class RunnerCooldown : MonoBehaviour
    {
        const int NoLeg = -1;

        RunnerBody _body;
        RunnerPoseDriver _driver;
        RaceFinishConfig _config;
        RunnerPose _stance;
        RunnerPose _ready;
        RunnerPoseDriver.LegAngles _crouch;
        Behaviour[] _stepFeedback = new Behaviour[0];
        bool _active;
        bool _stepping;
        bool _keysUp;
        int _stepsLeft;
        float _gap;
        double _nextPress;
        double _release;
        int _heldLeg = NoLeg;
        float _stand;
        RunnerFinishReaction _reaction;
        float _reactionLeft;
        float _reactionWeight;

        /// <summary>Starts easing this runner off (again from the start if it already was).</summary>
        public static RunnerCooldown Begin(RunnerBody body, RaceFinishConfig config)
        {
            RunnerCooldown cooldown = body.GetComponent<RunnerCooldown>();
            if (!cooldown) cooldown = body.gameObject.AddComponent<RunnerCooldown>();
            cooldown.Play(body, config);
            return cooldown;
        }

        /// <summary>The body's own reaction, for the config's seconds; it flows back to standing after that.</summary>
        public void React(RunnerFinishReaction reaction)
        {
            if (!_active) return;
            _reaction = reaction;
            _reactionLeft = _config.ReactionSeconds;
        }

        void Play(RunnerBody body, RaceFinishConfig config)
        {
            Restore();
            _body = body;
            _config = config;
            _driver = body.GetComponentInChildren<RunnerPoseDriver>();
            if (_driver)
            {
                _ready = _driver.ready;
                _crouch = _driver.crouch;
                if (!_stance) _stance = ScriptableObject.CreateInstance<RunnerPose>();
                _driver.ready = _stance;
            }
            _body.RunReset -= Restore;
            _body.RunReset += Restore;
            // The steps here are not the player's: no perfect / early / late on them.
            _stepFeedback = new Behaviour[]
            {
                body.GetComponent<PerfectStepFeedback>(), body.GetComponent<EarlyStepFeedback>(), body.GetComponent<LateStepFeedback>(),
            };
            for (int i = 0; i < _stepFeedback.Length; i++)
            {
                if (_stepFeedback[i] && _stepFeedback[i].enabled) _stepFeedback[i].enabled = false;
                else _stepFeedback[i] = null; // off by itself (no material): not ours to turn on
            }

            _body.acceptInput = true; // presses are how it steps; the player's own are stopped by the gate
            _stepsLeft = config.StepCount;
            _gap = config.FirstStepGapSeconds;
            _nextPress = Time.timeAsDouble + _gap;
            _heldLeg = NoLeg;
            _stand = 0f;
            _keysUp = false;
            _reaction = RunnerFinishReaction.None;
            _reactionLeft = 0f;
            _reactionWeight = 0f;
            _active = true;
            _stepping = true;
            ApplyStance();
        }

        void OnDestroy()
        {
            if (_body) _body.RunReset -= Restore;
            Restore();
            if (_stance) Destroy(_stance);
        }

        void Update()
        {
            if (!_active) return;
            RunnerSim sim = _body.Sim;
            if (sim == null) return;

            if (_stepping) Steps(sim);

            float dt = Time.deltaTime;
            float wanted = Mathf.InverseLerp(_config.StandFromSpeed, _config.StandFullSpeed, sim.Speed);
            _stand = Mathf.MoveTowards(_stand, wanted, dt / _config.StandSeconds);
            _reactionLeft -= dt;
            _reactionWeight = Mathf.MoveTowards(_reactionWeight, _reactionLeft > 0f ? 1f : 0f, dt / _config.ReactionBlendSeconds);
            ApplyStance();
        }

        void Steps(RunnerSim sim)
        {
            double now = Time.timeAsDouble;
            RunnerInput input = _body.input;
            if (!_keysUp)
            {
                // A finger may still be down from the race; its own release is the gate's to drop now.
                _keysUp = true;
                if (input)
                {
                    input.Inject(0, false, now);
                    input.Inject(1, false, now);
                }
            }

            if (_heldLeg != NoLeg && now >= _release)
            {
                if (input) input.Inject(_heldLeg, false, now);
                _heldLeg = NoLeg;
            }
            if (_stepsLeft > 0 && _heldLeg == NoLeg && now >= _nextPress)
            {
                if (sim.Speed < _config.MinStepSpeed) _stepsLeft = 0;
                else if (sim.HeadAngle <= _config.StepHeadLimit) Step(sim, input, now);
            }

            if (_stepsLeft > 0 || _heldLeg != NoLeg || sim.Speed > 0f) return;
            _body.acceptInput = false; // stopped: nothing more to press
            _stepping = false;
        }

        void Step(RunnerSim sim, RunnerInput input, double now)
        {
            int leg = sim.LastLeg == 0 ? 1 : 0;
            if (input) input.Inject(leg, true, now);
            _heldLeg = leg;
            _release = now + _config.StepPressSeconds;
            _stepsLeft--;
            _gap *= _config.StepGapGrowth;
            _nextPress = now + _gap;
        }

        /// <summary>The stance in between: the runner's own crouch and ready pose at 0, the config's upright one at 1, the reaction over it, eased.</summary>
        void ApplyStance()
        {
            if (!_driver) return;
            float w = Mathf.SmoothStep(0f, 1f, _stand);
            var legs = RunnerPoseDriver.LegAngles.Lerp(_crouch, new RunnerPoseDriver.LegAngles(_config.StandThigh, _config.StandKnee), w);

            PoseData own = _ready ? _ready.pose : RunnerPoseDefaults.Ready;
            PoseData upright = own;
            upright.pelvisPitch = _config.StandPelvisPitch;
            upright.width = _config.StandWidth;
            upright.leftArm = upright.rightArm = new ArmPose { swing = _config.StandArmSwing, elbow = _config.StandElbow };
            PoseData pose = PoseData.Lerp(own, upright, w);

            float r = Mathf.SmoothStep(0f, 1f, _reactionWeight);
            if (r > 0f && _reaction == RunnerFinishReaction.Cheer)
            {
                float pump = _config.CheerPumpDegrees * Mathf.Sin(Time.time * _config.CheerPumpRate * Mathf.PI * 2f);
                PoseData up = pose;
                up.leftArm = up.rightArm = new ArmPose { swing = _config.CheerArmSwing + pump, elbow = _config.CheerElbow - pump };
                pose = PoseData.Lerp(pose, up, r);
            }
            else if (r > 0f && _reaction == RunnerFinishReaction.Winded)
            {
                float breath = _config.WindedBreathDegrees * Mathf.Sin(Time.time * _config.WindedBreathRate * Mathf.PI * 2f);
                PoseData bent = pose;
                bent.pelvisPitch = _config.WindedPelvisPitch + breath;
                bent.leftArm = bent.rightArm = new ArmPose { swing = _config.WindedArmSwing, elbow = _config.WindedElbow };
                pose = PoseData.Lerp(pose, bent, r);
                legs = RunnerPoseDriver.LegAngles.Lerp(legs, new RunnerPoseDriver.LegAngles(_config.WindedThigh, _config.WindedKnee), r);
            }

            _driver.crouch = legs;
            _stance.pose = pose;
        }

        /// <summary>Everything as it was: a new race starts from the runner's own crouch. Also on a reset of the runner.</summary>
        void Restore()
        {
            _active = false;
            _stepping = false;
            if (_driver && _stance && _driver.ready == _stance)
            {
                _driver.ready = _ready;
                _driver.crouch = _crouch;
            }
            foreach (Behaviour feedback in _stepFeedback)
                if (feedback) feedback.enabled = true;
            _stepFeedback = new Behaviour[0];
        }
    }
}
