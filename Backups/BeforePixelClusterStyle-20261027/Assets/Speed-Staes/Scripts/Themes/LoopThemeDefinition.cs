using UnityEngine;
namespace SpeedStars.Themes
{
    public enum EnvironmentConcept { Egypt, RomeByzantium, PirateSea, FloatingSky }
    [CreateAssetMenu(menuName="Speed Stars/Theme Definition")]
    public sealed class LoopThemeDefinition:ScriptableObject
    {
        public EnvironmentConcept concept;
        public Material groundMaterial,pathMaterial,waterMaterial,asphaltMaterial,paletteMaterial;
        public GameObject road;
        public GameObject[] landmarks,background,props,vegetation,lamps,clouds,accent;
        public GameObject[] ruralScenes;
        public RomanCompositionPlan romanComposition;
        public Material[] mountainMaterials;
        public bool waterAcrossRoad,noGround;
        public float backgroundHeight;
    }
}



