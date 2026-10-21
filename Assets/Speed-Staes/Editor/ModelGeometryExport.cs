using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SpeedStars.Runner;
namespace SpeedStars.Editor
{
    public static class ModelGeometryExport
    {
        [Serializable] public class Part {public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;}
        [Serializable] public class Model {public string name,source;public Part[] parts;public int excluded2D;}
        [Serializable] public class Manifest {public Model[] models;}
        static Model Extract(string name,GameObject root,string source)
        {
            var parts=new List<Part>();int excluded=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<Renderer>();var mesh=filter.sharedMesh;if(!renderer||!mesh)continue;
                bool flat=filter.GetComponentInParent<CloudVariation>()!=null||filter.GetComponentInParent<PastelPlantGenerator>()!=null;
                if(renderer.sharedMaterial&&(renderer.sharedMaterial.shader.name=="Speed Stars/Cloud Volume 2D"||(renderer.sharedMaterial.shader.name=="Speed Stars/Pastel Foliage"&&mesh.bounds.size.z<.0001f)))flat=true;
                // Painted foliage, clouds and their shader-dependent geometry are intentionally omitted.
                if(flat){excluded++;continue;}
                Matrix4x4 matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                Matrix4x4 normalMatrix=matrix.inverse.transpose;
                Vector3[] vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
                Vector3[] normals=mesh.normals.Select(n=>normalMatrix.MultiplyVector(n).normalized).ToArray();
                int[] triangles=mesh.triangles;
                if(matrix.determinant<0)for(int i=0;i<triangles.Length;i+=3){int temp=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=temp;}
                string path=AnimationUtility.CalculateTransformPath(filter.transform,root.transform);if(string.IsNullOrEmpty(path))path=root.name;
                parts.Add(new Part{name=path,vertices=vertices,normals=normals,uv=mesh.uv,triangles=triangles});
            }
            return new Model{name=name,source=source,parts=parts.ToArray(),excluded2D=excluded};
        }
        public static string Export()
        {
            var owner=GameObject.Find("LoopWay").GetComponent<LoopWayGenerator>();var models=new List<Model>();
            Action<string,GameObject> add=(name,obj)=>{if(obj)models.Add(Extract(name,obj,AssetDatabase.GetAssetPath(obj)));};
            string[] propNames={"Bench","FireHydrant","LitterBin","Dumpster","CardboardParcel","VendingMachine","ParkingMeter","StreetUtilityCabinet","BicycleRackAndBicycle","RedMailbox","NewspaperStand","RoadBollards","FlowerPlanter","TrafficCones","MaintenanceBarrier"};
            for(int i=0;i<owner.props.Length;i++)add("StreetProps/"+(i<propNames.Length?propNames[i]:owner.props[i].prefab.name),owner.props[i].prefab);
            string[] buildingNames={"SmallHorizontalBuilding","TallBlueGlazingBuilding","MintCornerStore","TerracottaBalconyApartments","CreamWorkshop"};
            for(int i=0;i<owner.buildings.Length;i++)add("Buildings/"+(i<buildingNames.Length?buildingNames[i]:owner.buildings[i].prefab.name),owner.buildings[i].prefab);
            add("Buildings/CornerShop",owner.transform.Find("Corner Shop - Balcony Awning Blank Sign").gameObject);
            add("Landmarks/BusStop",owner.busStop.prefab);add("Landmarks/PocketPark",owner.park.prefab);add("Landmarks/Hospital",owner.hospital.prefab);
            add("StreetProps/Ambulance",owner.hospital.prefab.transform.Find("Parked Ambulance").gameObject);
            add("ParkParts/PlaygroundSlide",owner.park.prefab.transform.Find("Slide Playground Tower").gameObject);
            add("ParkParts/Fountain",owner.park.prefab.transform.Find("Octagonal Fountain Pool").gameObject);
            add("Infrastructure/RoadSegment",owner.roads[0].prefab);add("Infrastructure/SidewalkSlab",owner.sidewalk.prefab);add("Infrastructure/Curb",owner.curb.prefab);add("Infrastructure/BridgeModule",owner.bridge.prefab);
            add("StreetProps/StreetLamp",owner.lamp.prefab);
            add("Signs/CityRightArrow",AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Speed-Staes/Prefabs/StreetProps/Signs/CityRightArrow.prefab"));
            add("Signs/RunFaster",AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Speed-Staes/Prefabs/StreetProps/Signs/RunFasterBoard.prefab"));
            var city=owner.transform.Find("Building Row - Count and Seed/Generated Buildings");
            if(city)for(int i=0;i<city.childCount;i++)add("BackgroundBuildings/CityBuilding_"+(i+1).ToString("00"),city.GetChild(i).gameObject);
            string folder=Path.GetFullPath("Captures/ModelExport");Directory.CreateDirectory(folder);
            string json=JsonUtility.ToJson(new Manifest{models=models.Where(m=>m.parts.Length>0).ToArray()});File.WriteAllText(Path.Combine(folder,"geometry.json"),json);
            return JsonUtility.ToJson(new Summary{models=models.Count,parts=models.Sum(m=>m.parts.Length),excluded=models.Sum(m=>m.excluded2D),json=Path.Combine(folder,"geometry.json")});
        }
        [Serializable] class Summary {public int models,parts,excluded;public string json;}
    }
}

