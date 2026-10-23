using UnityEngine;

namespace SpeedStars.Runner
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class PastelBuildingSurface : MonoBehaviour
    {
        public Color frontColor, sideColor, windowColor;
        public Vector4 dimensions, pattern;
        void OnEnable() { Apply(); }
        void OnValidate() { Apply(); }
        public void Apply()
        {
            var renderer = GetComponent<Renderer>();
            if (!renderer) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_FrontColor", frontColor);
            block.SetColor("_SideColor", sideColor);
            block.SetColor("_WindowColor", windowColor);
            block.SetVector("_BuildingSize", dimensions);
            block.SetVector("_Pattern", pattern);
            renderer.SetPropertyBlock(block);
        }
    }
}
