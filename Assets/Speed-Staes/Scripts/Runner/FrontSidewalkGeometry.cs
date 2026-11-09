using UnityEngine;

namespace SpeedStars.Runner
{
    public static class FrontSidewalkGeometry
    {
        public const string GroupName="Front Sidewalk - Continuous Paving";

        // Positive X is the camera-facing edge. Overlap the curb slightly to avoid a sky seam.
        public static void Build(Transform parent,float edge,float start,float end,LoopWayAsset paving)
        {
            if(paving==null||!paving.prefab||end<=start)return;
            var group=new GameObject(GroupName).transform;group.SetParent(parent,false);
            for(float z=start;z<end-.001f;z+=1.5f)
            {
                float length=Mathf.Min(1.5f,end-z);
                var tile=Object.Instantiate(paving.prefab,group,false);tile.name="Front Paving Slab";
                tile.transform.localRotation=paving.rotation;
                tile.transform.localScale=new Vector3(paving.scale.x,paving.scale.y,length-.018f);
                tile.transform.localPosition=new Vector3(0,paving.position.y,z+length*.5f);
                var bounds=LoopWayGenerator.LocalBounds(tile,parent);
                tile.transform.localPosition+=Vector3.right*(edge-.12f-bounds.min.x);
            }
        }

        public static void Build(Transform parent,float edge,float start,float end,Material material)
        {
            if(!material||end<=start)return;
            var group=new GameObject(GroupName).transform;group.SetParent(parent,false);
            for(float z=start;z<end-.001f;z+=1.5f)
            {
                float length=Mathf.Min(1.5f,end-z);
                var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);tile.name="Front Paving Slab";
                tile.transform.SetParent(group,false);
                tile.transform.localPosition=new Vector3(edge-.12f+8f,-.05f,z+length*.5f);
                tile.transform.localScale=new Vector3(16,.2f,length-.018f);
                tile.GetComponent<Renderer>().sharedMaterial=material;
                var collider=tile.GetComponent<Collider>();
                if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
            }
        }
    }
}
