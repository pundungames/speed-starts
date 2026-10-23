using UnityEngine;
namespace SpeedStars.Runner
{
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class PastelFoliageSurface:MonoBehaviour
    {
        public Color color=Color.green;
        public Color shadowColor=new Color(0.15f,0.36f,0.11f),lightColor=new Color(0.56f,0.77f,0.12f);
        public Vector4 patchData;
        public Vector4 canopyShape;
        void OnEnable(){Apply();}
        void OnValidate(){Apply();}
        public void Apply()
        {
            var renderer=GetComponent<Renderer>();if(!renderer)return;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_ShadowColor",shadowColor);block.SetColor("_LightColor",lightColor);block.SetVector("_PatchData",patchData);block.SetVector("_CanopyShape",canopyShape);renderer.SetPropertyBlock(block);
        }
    }
}
