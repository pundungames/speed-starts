using System;
using UnityEngine;

namespace SpeedStars.Themes
{
    /// <summary>How a weather layer's particles are drawn: round motes (dust, pollen, snow) or stretched streaks (rain).</summary>
    public enum WeatherParticleShape
    {
        Mote,
        Streak
    }

    /// <summary>
    /// One airborne layer of a theme's weather (dust, rain, haze motes). A theme can stack a few: Egypt's high golden
    /// dust plus the sand sweeping along the ground. The layer is built as a single ParticleSystem that follows the
    /// camera, so the cost is one draw call per layer whatever the world does.
    /// </summary>
    [Serializable]
    public sealed class WeatherLayer
    {
        [Tooltip("Name of the layer's object in the hierarchy; only for reading the scene while testing.")]
        public string name = "Weather Layer";

        [Tooltip("Particle material. Dust and rain use the unlit Speed Stars particle shaders; empty = the layer is skipped.")]
        public Material material;

        [Tooltip("Round motes (dust) or stretched streaks (rain).")]
        public WeatherParticleShape shape = WeatherParticleShape.Mote;

        [Tooltip("Particle colour and, in the alpha, how dense the layer reads. Small alpha values are what keep dust air and not fog.")]
        public Color tint = new Color(1f, 0.72f, 0.34f, 0.08f);

        [Tooltip("Particle size range in metres.")]
        public Vector2 size = new Vector2(1f, 3.4f);

        [Tooltip("Particle lifetime range in seconds. Long lifetimes with slow speed read as hanging air.")]
        public Vector2 lifetime = new Vector2(8f, 13f);

        [Tooltip("Particle speed range in metres per second, along Direction.")]
        public Vector2 speed = new Vector2(0.15f, 0.45f);

        [Tooltip("Direction the layer drifts in, in world axes. The world streams along +Z, so +Z drifts across the screen and -X drifts towards the camera.")]
        public Vector3 direction = new Vector3(0f, 0f, 1f);

        [Tooltip("Gravity pull (0 = none, 1 = full). Rain needs it; hanging dust does not.")]
        public float gravity;

        [Tooltip("Particles born per second.")]
        [Min(0f)] public float rate = 12f;

        [Tooltip("Upper bound on living particles: the layer's real cost on a phone.")]
        [Min(1)] public int maxParticles = 150;

        [Tooltip("Size of the box particles are born in (x = depth across the track, y = height, z = along the track).")]
        public Vector3 boxSize = new Vector3(75f, 3.4f, 125f);

        [Tooltip("Where the box sits: x and y are world values, z is measured from the camera, because the world streams past along z.")]
        public Vector3 follow = new Vector3(-30f, 1.5f, 22f);

        [Tooltip("Streak layers only: how far a particle is stretched along its own velocity.")]
        [Min(0f)] public float streakLength = 3f;

        [Tooltip("Spin range in degrees per second (min, max). Anything but zero also starts every particle at a random angle: leaves tumble, dust and rain do not need it.")]
        public Vector2 spin;

        [Tooltip("How hard the air pushes the particles off their straight path (0 = none). Leaves need it to wander; rain should fall straight.")]
        [Min(0f)] public float turbulence;
    }

    /// <summary>
    /// The air of one theme: sky, sun, ambient, the aerial haze over the whole world and the airborne layers. A theme
    /// differs from the others by this data, not by code — every theme runs the same <see cref="ThemeWeatherView"/>.
    /// Empty fields leave the scene's own value alone, so a theme can take only what it needs.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Theme Weather", fileName = "Weather")]
    public sealed class ThemeWeather : ScriptableObject
    {
        [Header("Sky")]
        [Tooltip("Full-screen backdrop drawn behind the world (gradient + sun). Empty = the scene keeps its own sky.")]
        public Material skyMaterial;

        [Tooltip("Colour the camera clears to behind the backdrop. Only used when a Sky Material is set.")]
        public Color horizonFill = new Color(0.96f, 0.65f, 0.30f);

        [Header("Sun")]
        [Tooltip("Colour of the scene's directional light while this theme is shown.")]
        public Color sunColor = Color.white;

        [Tooltip("Multiplies the light's own intensity (1 = leave it as the scene set it).")]
        [Min(0f)] public float sunIntensityScale = 1f;

        [Header("Ambient")]
        [Tooltip("Ambient light from above (sky). The three ambient colours are applied as Trilight.")]
        public Color ambientSky = new Color(0.87f, 0.70f, 0.46f);

        [Tooltip("Ambient light from the sides (horizon).")]
        public Color ambientEquator = new Color(0.64f, 0.49f, 0.31f);

        [Tooltip("Ambient light bounced off the ground.")]
        public Color ambientGround = new Color(0.36f, 0.27f, 0.18f);

        [Header("Air over the whole world")]
        [Tooltip("Colour the distance fades into. The generators already set a haze per object; this pulls all of them towards the theme's air.")]
        public Color airTint = Color.white;

        [Tooltip("How far each object's own haze colour is pulled towards Air Tint (0 = keep what the generator authored, 1 = the tint only).")]
        [Range(0f, 1f)] public float airTintBlend;

        [Tooltip("Added to every object's haze strength (0 = keep). Thicker air hides the distance and makes the near world read warmer or colder.")]
        [Range(-1f, 1f)] public float airStrengthBoost;

        [Tooltip("Multiplies where the haze starts (smaller = the air closes in sooner). 1 = keep.")]
        [Min(0.05f)] public float airStartScale = 1f;

        [Tooltip("Multiplies where the haze is full (smaller = the horizon disappears sooner). 1 = keep.")]
        [Min(0.05f)] public float airEndScale = 1f;

        [Header("The world's own colours")]
        [Tooltip("Colour the theme's objects are multiplied by, the way light would: the hour of the day on the near world, which the haze alone cannot reach because these shaders paint flat and barely answer to light. White changes nothing.")]
        public Color worldTint = Color.white;

        [Tooltip("How much of that multiplied colour the world takes (0 = the objects keep their own colours). Multiplying keeps the contrast, so a storm darkens without flattening.")]
        [Range(0f, 1f)] public float worldTintBlend;

        [Header("Airborne layers")]
        [Tooltip("Dust, rain and the like. Each layer is one camera-following ParticleSystem.")]
        public WeatherLayer[] layers = new WeatherLayer[0];

        /// <summary>The haze blend this theme pushes onto every <see cref="ThemeAtmosphere"/> of its world.</summary>
        public ThemeAtmosphere.AirBlend Air => new ThemeAtmosphere.AirBlend
        {
            tint = airTint,
            tintBlend = airTintBlend,
            strengthBoost = airStrengthBoost,
            startScale = airStartScale,
            endScale = airEndScale
        };
    }
}
