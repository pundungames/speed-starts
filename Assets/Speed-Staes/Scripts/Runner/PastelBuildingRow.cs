using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SpeedStars.Runner
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class PastelBuildingRow : MonoBehaviour
    {
        [Range(1,100)] public int buildingCount = 5;
        public int variationSeed = 42;
        [Min(0)] public float gap = 1.4f;
        public Vector2 heightRange = new Vector2(8, 15);
        public Vector2 widthRange = new Vector2(3.5f, 6);
        public Vector2 depthRange = new Vector2(3, 5);
        [Tooltip("Unrestricted angle in degrees, including beyond +/-45.")]
        public float facadeAngle = -18;
        [Header("District Layout")]
        [Range(1,8)] public int depthLayers = 3;
        [Min(1)] public float layerSpacing = 9;
        [Min(0)] public float layerStagger = 3;
        [Min(0)] public float positionJitter = 1.5f;
        [Min(0)] public float angleVariation = 12;
        [Header("Building Variety")]
        public Vector2 sizeVariation = new Vector2(0.5f,1.5f);
        [Range(0,1)] public float darkBuildingChance = 0.4f;
        public bool automaticUpdate = true;
        public Material buildingMaterial;
        [SerializeField, HideInInspector] Transform generatedRoot;
#if UNITY_EDITOR
        bool queued;
        void OnValidate()
        {
            buildingCount = Mathf.Clamp(buildingCount,1,100);
            if (!automaticUpdate || Application.isPlaying || queued) return;
            queued = true;
            EditorApplication.delayCall += DeferredRebuild;
        }
        void DeferredRebuild()
        {
            queued = false;
            if (this && !Application.isPlaying && gameObject.scene.IsValid()) Rebuild();
        }
#endif
        [ContextMenu("Rebuild Buildings")]
        public void Rebuild()
        {
            if (!buildingMaterial) return;
#if UNITY_EDITOR
            bool recordUndo=!Application.isPlaying&&!GetComponentInParent<LoopWayGenerator>();
            if (recordUndo) Undo.RecordObject(this,"Generate Pastel Buildings");
#endif
            if (generatedRoot && generatedRoot.parent == transform)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying){if(recordUndo)Undo.DestroyObjectImmediate(generatedRoot.gameObject);else DestroyImmediate(generatedRoot.gameObject);}
                else { generatedRoot.gameObject.SetActive(false); Destroy(generatedRoot.gameObject); }
#else
                Destroy(generatedRoot.gameObject);
#endif
            }
            var root = new GameObject("Generated Buildings");
            root.transform.SetParent(transform, false);
            generatedRoot = root.transform;
            var random = new System.Random(variationSeed);
            int layers=Mathf.Clamp(depthLayers,1,buildingCount);
            var widths = new float[buildingCount];
            var totals = new float[layers];
            var cursors = new float[layers];
            for(int i=0;i<buildingCount;i++)
            {
                widths[i]=Range(random,widthRange)*Range(random,sizeVariation);
                int type=((i+variationSeed)%8+8)%8;
                if(type==0) widths[i]*=1.25f;
                if(type==1) widths[i]*=0.78f;
                if(type==3) widths[i]*=1.15f;
                totals[i%layers]+=widths[i]+gap;
            }
            for(int layer=0;layer<layers;layer++) cursors[layer]=-(totals[layer]-gap)*0.5f+(layer%2==0?-1:1)*layerStagger;
            for(int i=0;i<buildingCount;i++)
            {
                int layer=i%layers;
                int archetype=((i+variationSeed)%8+8)%8;
                float width=widths[i],height=Range(random,heightRange)*Range(random,sizeVariation),depth=Range(random,depthRange);
                height*=Mathf.Lerp(0.72f,1.1f,layers==1?0.5f:(float)layer/(layers-1));
                if(archetype==0) height=Mathf.Min(height,5.0f);
                if(archetype==1) height*=1.25f;
                if(archetype==3) height*=0.85f;
                float tone=(float)random.NextDouble();
                var front=Color.Lerp(new Color(0.53f,0.73f,0.94f),new Color(0.73f,0.86f,0.96f),tone);
                var side=Color.Lerp(new Color(0.30f,0.53f,0.80f),new Color(0.43f,0.66f,0.89f),tone);
                if(random.NextDouble()<darkBuildingChance)
                {
                    front=Color.Lerp(new Color(0.25f,0.48f,0.73f),new Color(0.38f,0.61f,0.84f),tone);
                    side=Color.Lerp(new Color(0.18f,0.36f,0.62f),new Color(0.29f,0.50f,0.75f),tone);
                }
                float haze=layers==1?0:(float)layer/(layers-1)*0.16f;
                front=Color.Lerp(front,new Color(0.56f,0.77f,0.95f),haze);
                side=Color.Lerp(side,new Color(0.47f,0.69f,0.91f),haze);
                var windows=Color.Lerp(front,new Color(0.31f,0.56f,0.82f),0.30f+0.22f*(float)random.NextDouble());
                int style=archetype==0?4:(archetype==1?0:(archetype==2?1:(archetype==3?2:(archetype==4?5:(archetype==5?6:(archetype==6?3:7))))));
                int cols=(style==2||style==6)?1:(style==4?2:(style==5?5:random.Next(2,5)));
                int floors=style==4?3:Mathf.Max(3,Mathf.RoundToInt(height/1.1f));
                if(style==4) {front=new Color(0.78f,0.87f,0.91f);side=new Color(0.64f,0.77f,0.84f);windows=new Color(0.32f,0.56f,0.72f);}
                string[] names={"White Corner Block","Narrow Tower","Office Grid","Broad Slab","Vertical Windows","Terraced Tower","Solid Monolith","Mixed Offices"};
                var body=Cube("Building "+(i+1).ToString("00")+" - "+names[archetype]+" - Layer "+(layer+1),generatedRoot,
                    new Vector3(-layer*layerSpacing+((float)random.NextDouble()-0.5f)*positionJitter,height*0.5f,cursors[layer]+width*0.5f+((float)random.NextDouble()-0.5f)*positionJitter),new Vector3(depth,height,width));
                body.transform.localRotation=Quaternion.Euler(0,facadeAngle+((float)random.NextDouble()-0.5f)*angleVariation*2,0);
                Paint(body,front,side,windows,new Vector4(cols,floors,style,random.Next(10000)));
                // A small stepped rooftop block is characteristic of the reference skyline.
                if(random.NextDouble()>0.22)
                {
                    var cap=Cube("Roof Step",body.transform,new Vector3(0,0.52f,-0.06f),new Vector3(0.55f,0.06f,0.40f));
                    Paint(cap,front,side,windows,new Vector4(1,1,0,-1));
                }
                if(archetype==5)
                {
                    var terrace=Cube("Upper Setback",body.transform,new Vector3(0,0.60f,0.1f),new Vector3(0.68f,0.28f,0.64f));
                    Paint(terrace,front,side,windows,new Vector4(2,3,1,random.Next(10000)));
                }
                if(archetype==4||archetype==7)
                {
                    var podium=Cube("Wide Podium",body.transform,new Vector3(0,-0.40f,0),new Vector3(1.16f,0.20f,1.18f));
                    Paint(podium,front,side,windows,new Vector4(3,2,4,random.Next(10000)));
                }
                cursors[layer]+=width+gap;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if(recordUndo)Undo.RegisterCreatedObjectUndo(root,"Generate Pastel Buildings");
                EditorUtility.SetDirty(this);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }
        static float Range(System.Random random,Vector2 range)
        { return Mathf.Lerp(Mathf.Max(0.5f,range.x),Mathf.Max(range.x,range.y),(float)random.NextDouble()); }
        GameObject Cube(string objectName,Transform parent,Vector3 position,Vector3 scale)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name=objectName;cube.transform.SetParent(parent,false);
            cube.transform.localPosition=position;cube.transform.localScale=scale;
            var collider=cube.GetComponent<Collider>();
            if(Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            var renderer=cube.GetComponent<Renderer>();renderer.sharedMaterial=buildingMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            return cube;
        }
        static void Paint(GameObject cube,Color front,Color side,Color windows,Vector4 pattern)
        {
            var surface=cube.AddComponent<PastelBuildingSurface>();
            surface.frontColor=front;surface.sideColor=side;surface.windowColor=windows;
            surface.dimensions=cube.transform.localScale;surface.pattern=pattern;surface.Apply();
        }
    }
}



