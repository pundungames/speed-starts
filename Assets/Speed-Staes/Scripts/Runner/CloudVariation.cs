using UnityEngine;
namespace SpeedStars.Runner
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Renderer))]
    public sealed class CloudVariation : MonoBehaviour
    {
        public float shapeVariant;
        [Range(64,512)] public int pixelColumns=190;
        public Color sunlitColor=new Color(.96f,1,1,1);
        public Color shadowColor=new Color(.53f,.83f,.91f,1);
        void OnEnable(){Apply();}
        void OnValidate(){Apply();}
        public void ReadMaterial()
        {
            var m=GetComponent<Renderer>().sharedMaterial;
            if(!m)return;
            shapeVariant=m.GetFloat("_Seed");pixelColumns=Mathf.RoundToInt(m.GetFloat("_PixelGrid"));
            sunlitColor=m.GetColor("_LightColor");shadowColor=m.GetColor("_ShadeColor");Apply();
        }
        public void Apply()
        {
            var r=GetComponent<Renderer>();if(!r)return;
            var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);
            b.SetFloat("_Seed",shapeVariant);b.SetFloat("_PixelGrid",pixelColumns);
            b.SetColor("_LightColor",sunlitColor);b.SetColor("_ShadeColor",shadowColor);r.SetPropertyBlock(b);
        }
    }
}
