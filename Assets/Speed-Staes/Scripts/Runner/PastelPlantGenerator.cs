using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
namespace SpeedStars.Runner
{
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class PastelPlantGenerator:MonoBehaviour
    {
        public enum PlantShape { RoundTree, WideTree, TallTree, Bush }
        public PlantShape shape=PlantShape.RoundTree;
        [Tooltip("Reference-style joined hedge silhouette with square painted foliage.")]
        public bool referenceHedge=true;
        public int variationSeed=12;
        [Min(0.3f)]public float height=3.8f;
        [Min(0.3f)]public float width=3.4f;
        [Min(0.3f)]public float depth=2.5f;
        [InspectorName("Canopy Detail"), Tooltip("Frequency of gentle shape variation on the single rounded crown.")]
        [Range(8,90)]public int leafClusterCount=32;
        [InspectorName("Silhouette Variation"), Tooltip("Amount of gentle variation in the continuous round silhouette.")]
        [Range(0.1f,0.75f)]public float clusterSize=0.34f;
        [Range(0,1)]public float trunkFraction=0.20f;
        [HideInInspector]public float blockRotation=3;
        [Header("Palette")]
        public Color shadowGreen=new Color(0.12f,0.34f,0.12f);
        public Color leafGreen=new Color(0.32f,0.60f,0.07f);
        public Color sunlitGreen=new Color(0.65f,0.82f,0.09f);
        public Color trunkColor=new Color(0.25f,0.28f,0.12f);
        [Range(1,8)]public int patchDensity=3;
        [Range(0,0.3f)]public float patchStrength=0.10f;
        public bool automaticUpdate=true;
        public Material foliageMaterial;
        [SerializeField,HideInInspector]Transform generatedRoot;
        [SerializeField,HideInInspector]int lastBuiltSettingsHash;
        [SerializeField,HideInInspector]bool hasBuiltSettings;
        int SettingsHash()
        {
            unchecked {int h=17;foreach(var v in new[]{(float)shape,referenceHedge?1f:0f,variationSeed,height,width,depth,leafClusterCount,clusterSize,trunkFraction,blockRotation,patchDensity,patchStrength})h=h*31+v.GetHashCode();h=h*31+shadowGreen.GetHashCode();h=h*31+leafGreen.GetHashCode();h=h*31+sunlitGreen.GetHashCode();h=h*31+trunkColor.GetHashCode();return h;}
        }
        public void KeepCurrentGeometry(){lastBuiltSettingsHash=SettingsHash();hasBuiltSettings=true;}
#if UNITY_EDITOR
        bool queued;
        void OnValidate()
        {
            height=Mathf.Max(0.3f,height);width=Mathf.Max(0.3f,width);depth=Mathf.Max(0.3f,depth);leafClusterCount=Mathf.Clamp(leafClusterCount,8,90);
            if(!automaticUpdate||Application.isPlaying||queued)return;
            if(hasBuiltSettings&&lastBuiltSettingsHash==SettingsHash()&&generatedRoot&&generatedRoot.parent==transform)return;
            queued=true;EditorApplication.delayCall+=DeferredRebuild;
        }
        void DeferredRebuild()
        {
            queued=false;
            if(this&&automaticUpdate&&!Application.isPlaying&&gameObject.scene.IsValid()&&(!hasBuiltSettings||lastBuiltSettingsHash!=SettingsHash()||!generatedRoot||generatedRoot.parent!=transform))Rebuild();
        }
#endif
        [ContextMenu("Rebuild Plant")]
        public void Rebuild()
        {
            if(!foliageMaterial)return;
            KeepCurrentGeometry();
#if UNITY_EDITOR
            bool recordUndo=!Application.isPlaying&&!GetComponentInParent<LoopWayGenerator>();
            if(recordUndo)Undo.RecordObject(this,"Generate Pastel Plant");
#endif
            // A copied controller must never delete another controller's geometry.
            if(generatedRoot&&generatedRoot.parent==transform)
            {
#if UNITY_EDITOR
                if(!Application.isPlaying){if(recordUndo)Undo.DestroyObjectImmediate(generatedRoot.gameObject);else DestroyImmediate(generatedRoot.gameObject);}else { generatedRoot.gameObject.SetActive(false); Destroy(generatedRoot.gameObject); }
#else
                Destroy(generatedRoot.gameObject);
#endif
            }
            var root=new GameObject("Generated Plant");root.transform.SetParent(transform,false);generatedRoot=root.transform;

            float stem=shape==PlantShape.Bush||referenceHedge?0.03f:trunkFraction;
            float canopyHeight=height*(1-stem);
            float spread=shape==PlantShape.WideTree?1.20f:(shape==PlantShape.TallTree?0.72f:1);
            if(shape!=PlantShape.Bush&&!referenceHedge)
            {
                MakeBlock("Trunk",new Vector3(0,height*0.40f,0),new Vector3(width*0.085f,height*0.80f,depth*0.10f),Quaternion.identity,trunkColor,0,true);
                MakeBlock("Branch",new Vector3(width*0.12f,height*0.55f,0),new Vector3(width*0.07f,height*0.36f,depth*0.08f),Quaternion.Euler(0,0,-28),trunkColor,0,true);
            }
            // Single organic crown; rectangular light patches are shader detail.
            MakeBlock("Continuous Rounded Canopy",new Vector3(0,height*stem+canopyHeight*0.49f,0),
                new Vector3(width*spread*0.84f,canopyHeight*0.93f,depth*0.88f),Quaternion.identity,leafGreen,variationSeed,false);
#if UNITY_EDITOR
            if(!Application.isPlaying){if(recordUndo)Undo.RegisterCreatedObjectUndo(root,"Generate Pastel Plant");EditorUtility.SetDirty(this);EditorSceneManager.MarkSceneDirty(gameObject.scene);}
#endif
        }
        static float Rand(System.Random random,float min,float max){return Mathf.Lerp(min,max,(float)random.NextDouble());}
        void MakeBlock(string blockName,Vector3 position,Vector3 scale,Quaternion rotation,Color color,int seed,bool trunk)
        {
            var cube=GameObject.CreatePrimitive(trunk?PrimitiveType.Cube:(referenceHedge?PrimitiveType.Quad:PrimitiveType.Sphere));cube.name=blockName;cube.transform.SetParent(generatedRoot,false);
            cube.transform.localPosition=position;cube.transform.localScale=scale;cube.transform.localRotation=rotation;
            var collider=cube.GetComponent<Collider>();if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);
            var renderer=cube.GetComponent<Renderer>();renderer.sharedMaterial=foliageMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var surface=cube.AddComponent<PastelFoliageSurface>();surface.color=color;
            surface.shadowColor=shadowGreen;surface.lightColor=sunlitGreen;
            surface.canopyShape=new Vector4(clusterSize*0.40f,Mathf.Lerp(2.5f,4.5f,(leafClusterCount-8)/82f),scale.x,scale.y);
            surface.patchData=new Vector4(patchDensity,seed,patchStrength,trunk?1:(referenceHedge?2:0));surface.Apply();
        }
    }
}




