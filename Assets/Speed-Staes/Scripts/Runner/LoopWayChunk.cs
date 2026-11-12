using UnityEngine;
namespace SpeedStars.Runner
{
    public sealed class LoopWayChunk : MonoBehaviour
    {
        public int index,seed,cloudCount,cityCount,buildingType,hedgeCount,propCount;
        public float start,end;
        public string frontageKind;
        // World-space renderer bounds, captured once when the chunk is finished (see LoopWayStreaming).
        public Bounds worldBounds;
    }
    /// <summary>Shared helpers for streamed chunk retirement: a chunk is only removed once the camera cannot see it.</summary>
    public static class LoopWayStreaming
    {
        static readonly Plane[] planes=new Plane[6];
        public static Bounds CaptureBounds(GameObject g)
        {
            bool found=false;var result=new Bounds();
            foreach(var renderer in g.GetComponentsInChildren<Renderer>())
            {if(!found){result=renderer.bounds;found=true;}else result.Encapsulate(renderer.bounds);}
            return result;
        }
        // Alloc-free frustum test; a null camera counts as "out of view" so the plain distance rule decides.
        public static bool OutOfView(Camera camera,Bounds bounds)
        {
            if(!camera)return true;
            GeometryUtility.CalculateFrustumPlanes(camera,planes);
            return !GeometryUtility.TestPlanesAABB(planes,bounds);
        }
    }
}
