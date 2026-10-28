#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  static Material eClay,eLight,eDark,eWood,eCloth,eRed,eBlue,eGrass,eSkin,eCamel,eRope,eFruit,eDust;
  static readonly List<string> egyptAudit=new List<string>();
  static void DesertPalette(){
   eClay=Paint("Desert Adobe",new Color(.77f,.53f,.30f),.3f);eLight=Paint("Desert Limestone",new Color(.94f,.78f,.50f),.3f);eDark=Paint("Desert Deep Recess",new Color(.30f,.23f,.16f),.35f);eWood=Paint("Desert Aged Timber",new Color(.43f,.29f,.16f),.3f);
   eCloth=Paint("Desert Unbleached Linen",new Color(.91f,.82f,.61f),.4f);eRed=Paint("Desert Rust Cloth",new Color(.64f,.29f,.17f),.3f);eBlue=Paint("Desert Indigo Cloth",new Color(.25f,.35f,.40f),.35f);eGrass=Paint("Desert Dry Sage",new Color(.56f,.48f,.25f),.4f);
   eSkin=Paint("Desert Warm Skin",new Color(.49f,.31f,.19f),.3f);eCamel=Paint("Desert Camel Coat",new Color(.66f,.43f,.23f),.3f);eRope=Paint("Desert Rope Fibre",new Color(.60f,.45f,.26f),.35f);eFruit=Paint("Desert Dates and Melons",new Color(.88f,.56f,.18f),.35f);
  }
  static GameObject DesertModel(string name,Action<Transform> build){var raw=Model(name,build);AuditEgyptModel(raw);if(name.Contains("Statue"))return raw;return BatchThemeModel(raw);}
  static void AuditEgyptModel(GameObject model){
   var filters=model.GetComponentsInChildren<MeshFilter>();var bounds=new List<Bounds>();var names=new List<string>();int invalid=0;
   foreach(var filter in filters){if(!filter.sharedMesh){invalid++;continue;}var r=filter.GetComponent<Renderer>();bounds.Add(r.bounds);names.Add(filter.name);foreach(var v in filter.sharedMesh.vertices)if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.z))invalid++;}
   var seen=new bool[bounds.Count];var floating=new List<string>();int components=0;
   for(int i=0;i<bounds.Count;i++){if(seen[i])continue;components++;bool grounded=false;var queue=new Queue<int>();queue.Enqueue(i);seen[i]=true;var parts=new List<string>();while(queue.Count>0){int k=queue.Dequeue();parts.Add(names[k]);if(bounds[k].min.y<=.12f)grounded=true;var expanded=bounds[k];expanded.Expand(.14f);for(int j=0;j<bounds.Count;j++)if(!seen[j]&&expanded.Intersects(bounds[j])){seen[j]=true;queue.Enqueue(j);}}if(!grounded)floating.Add(string.Join(", ",parts.Take(5).ToArray()));}
   bool neck=!model.name.Contains("Statue")||names.Any(n=>n.Contains("Neck"));bool rope=!model.name.Contains("Well")||names.Any(n=>n=="Bucket Rope");
   egyptAudit.Add("| "+model.name+" | "+filters.Length+" | "+invalid+" | "+floating.Count+" | "+(neck&&rope?"pass":"MISSING CONNECTION")+" |"+(floating.Count>0?"\n  Floating candidates: "+string.Join("; ",floating.ToArray()):""));
   if(invalid>0||!neck||!rope)Debug.LogError("Egypt model audit failed: "+model.name);
  }
  static void EArch(Transform t,Vector3 p,float width,float height){float r=width*.5f,spring=height-r;Box(t,"Dark Door Recess",p+Vector3.up*spring*.5f,new Vector3(width,spring,.09f),eDark);var v=new List<Vector3>{p+Vector3.up*spring};var tr=new List<int>();for(int i=0;i<=12;i++){float a=i*Mathf.PI/12;v.Add(p+new Vector3(Mathf.Cos(a)*r,spring+Mathf.Sin(a)*r,0));if(i>0)tr.AddRange(new[]{0,i+1,i});}MeshObject(t,"Dark Arch Crown",v,tr,eDark);foreach(float side in new[]{-1f,1f})Box(t,"Door Stone Jamb",p+new Vector3(side*(r+.09f),spring*.5f,-.02f),new Vector3(.18f,spring,.18f),eLight);Arc(t,"Door Stone Arch",p+new Vector3(0,spring,-.02f),r,.18f,.16f,eLight,12);}
  static void EWindow(Transform t,Vector3 p,float w=.7f,float h=.9f){Box(t,"Inset Shutter Window",p,new Vector3(w,h,.08f),eDark);foreach(float side in new[]{-1f,1f})Box(t,"Window Timber Jamb",p+new Vector3(side*(w*.5f+.06f),0,-.03f),new Vector3(.12f,h+.18f,.15f),eWood);Box(t,"Window Sill",p+new Vector3(0,-h*.5f-.04f,-.06f),new Vector3(w+.35f,.15f,.22f),eLight);for(int i=0;i<3;i++)Box(t,"Window Lattice",p+new Vector3((i-1)*w*.26f,0,-.06f),new Vector3(.04f,h,.04f),eWood);}
  static void EHouse(Transform t,float width,float height,float depth,bool domed=false){
   Box(t,"Adobe Walls",new Vector3(0,height*.5f,0),new Vector3(width,height,depth),eClay);Box(t,"Stone Foundation",new Vector3(0,.18f,0),new Vector3(width+.2f,.36f,depth+.16f),eLight);
   EArch(t,new Vector3(-width*.18f,.25f,-depth*.5f-.05f),1.2f,2.4f);EWindow(t,new Vector3(width*.27f,2.45f,-depth*.5f-.05f));
   if(domed)Dome(t,new Vector3(0,height,0),width*.48f,eLight);else{Box(t,"Flat Roof Cornice",new Vector3(0,height+.08f,0),new Vector3(width+.3f,.16f,depth+.3f),eLight);foreach(float z in new[]{-depth*.5f,depth*.5f})Box(t,"Roof Parapet",new Vector3(0,height+.35f,z),new Vector3(width+.2f,.5f,.2f),eClay);for(int i=0;i<4;i++)Box(t,"Embedded Timber Roof Beam",new Vector3(-width*.38f+i*width*.25f,height-.14f,-depth*.5f-.15f),new Vector3(.15f,.16f,.55f),eWood);}
  }
  static void EAwning(Transform t,Vector3 p,float width,float depth,Material cloth){
   foreach(float x in new[]{-width*.44f,width*.44f})foreach(float z in new[]{-depth*.43f,depth*.43f})Beam(t,"Grounded Awning Post",p+new Vector3(x,0,z),p+new Vector3(x,2.65f,z),.1f,eWood);
   foreach(float z in new[]{-depth*.43f,depth*.43f})Beam(t,"Awning Cross Beam",p+new Vector3(-width*.5f,2.65f,z),p+new Vector3(width*.5f,2.65f,z),.13f,eWood);
   for(int i=0;i<8;i++)Box(t,"Woven Canopy Stripe",p+new Vector3((i-3.5f)*width/8,2.74f,0),new Vector3(width/8+.01f,.08f,depth),i%2==0?cloth:eCloth);
  }
  static void EProduce(Transform t,Vector3 p,bool spice=false){
   foreach(float x in new[]{-1.7f,1.7f})foreach(float z in new[]{-.55f,.55f})Box(t,"Counter Supporting Leg",p+new Vector3(x,.52f,z),new Vector3(.14f,1.04f,.14f),eWood);
   Box(t,"Merchant Counter",p+new Vector3(0,1.12f,0),new Vector3(4.1f,.16f,1.5f),eWood);
   for(int tray=0;tray<4;tray++){float x=-1.5f+tray;Box(t,"Produce Tray Base",p+new Vector3(x,1.25f,0),new Vector3(.85f,.1f,1.0f),eClay);foreach(float z in new[]{-.5f,.5f})Box(t,"Produce Tray Rim",p+new Vector3(x,1.38f,z),new Vector3(.88f,.25f,.08f),eWood);for(int j=0;j<6;j++)Ball(t,spice?"Spice Heap":"Dates and Melons",p+new Vector3(x+(j%2)*.3f-.15f,1.39f+(j/4)*.10f,(j/2%2)*.33f-.16f),spice?new Vector3(.3f,.18f,.32f):new Vector3(.33f,.25f,.30f),tray%2==0?eFruit:eRed);}
  }
  static void EClosedCylinder(Transform t,string name,Vector3 p,float r,float h,Material material,int sides=12){
   var v=new List<Vector3>();var tr=new List<int>();for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));v.Add(new Vector3(Mathf.Cos(a)*r,h,Mathf.Sin(a)*r));}v.Add(Vector3.zero);v.Add(Vector3.up*h);for(int i=0;i<sides;i++){int a=i*2,b=(i+1)%sides*2;tr.AddRange(new[]{a,a+1,b,b,a+1,b+1,sides*2,a,b,sides*2+1,b+1,a+1});}var g=MeshObject(t,name,v,tr,material);g.transform.localPosition=p;
  }
  static void EBedouin(Transform parent,Vector3 p){
   var g=new GameObject("Bedouin Caravan Handler");g.transform.SetParent(parent,false);g.transform.localPosition=p;var t=g.transform;
   foreach(float x in new[]{-.14f,.14f}){Box(t,"Grounded Leather Sandal",new Vector3(x,.07f,-.04f),new Vector3(.22f,.14f,.4f),eDark);Beam(t,"Handler Lower Leg",new Vector3(x,.12f,0),new Vector3(x,.65f,0),.13f,eSkin);}
   Lathe(t,"Bedouin Long Robe",new Vector3(0,.4f,0),new[]{0f,.85f,1.25f},new[]{.34f,.29f,.19f},eCloth,10);Cylinder(t,"Handler Neck",new Vector3(0,1.57f,0),.12f,.25f,eSkin,10);
   Ball(t,"Handler Head",new Vector3(0,1.96f,0),new Vector3(.42f,.52f,.42f),eSkin);Ball(t,"Wrapped Indigo Turban",new Vector3(0,2.12f,.02f),new Vector3(.56f,.33f,.53f),eBlue);Box(t,"Turban Cloth Tail",new Vector3(.21f,1.8f,.1f),new Vector3(.18f,.65f,.15f),eBlue);
   Beam(t,"Left Sleeve",new Vector3(-.18f,1.45f,0),new Vector3(-.2f,1.5f,-.05f),.18f,eCloth);Ball(t,"Rope Holding Hand",new Vector3(-.2f,1.5f,-.05f),Vector3.one*.19f,eSkin);
   Beam(t,"Right Sleeve",new Vector3(.19f,1.45f,0),new Vector3(.4f,.95f,-.05f),.16f,eCloth);Ball(t,"Right Hand",new Vector3(.4f,.95f,-.05f),Vector3.one*.17f,eSkin);
  }
  static void ECamel(Transform parent,Vector3 p,bool cargo=true){
   var root=new GameObject("Complete Dromedary Camel");root.transform.SetParent(parent,false);root.transform.localPosition=p;var t=root.transform;
   Ball(t,"Camel Body",new Vector3(0,1.85f,0),new Vector3(2.8f,1.4f,1.0f),eCamel);
   foreach(float x in new[]{-.85f,.85f})foreach(float z in new[]{-.32f,.32f}){var knee=new Vector3(x+.1f,.85f,z);Beam(t,"Camel Upper Leg",new Vector3(x,1.7f,z),knee,.18f,eCamel);Ball(t,"Camel Knee Joint",knee,Vector3.one*.23f,eCamel);Beam(t,"Camel Lower Leg",knee,new Vector3(x,.16f,z),.12f,eCamel);Box(t,"Camel Hoof",new Vector3(x,.09f,z),new Vector3(.24f,.18f,.28f),eDark);}
   Lathe(t,"Single Camel Hump",new Vector3(-.35f,2.3f,0),new[]{0f,.55f,.9f},new[]{.53f,.35f,.04f},eCamel,12);
   Tube(t,"Connected Curved Camel Neck",new List<Vector3>{new Vector3(1.0f,2.1f,0),new Vector3(1.25f,2.7f,0),new Vector3(1.55f,3.4f,0)},.25f,eCamel,10,.19f);
   Ball(t,"Camel Head and Muzzle",new Vector3(1.83f,3.46f,0),new Vector3(.95f,.48f,.42f),eCamel);foreach(float z in new[]{-.24f,.24f})Ball(t,"Attached Camel Ear",new Vector3(1.62f,3.73f,z),new Vector3(.16f,.32f,.16f),eCamel);
   foreach(float z in new[]{-.21f,.21f})Ball(t,"Camel Eye",new Vector3(1.88f,3.56f,z),Vector3.one*.07f,eDark);
   Tube(t,"Camel Tail",new List<Vector3>{new Vector3(-1.2f,2,0),new Vector3(-1.5f,1.7f,0),new Vector3(-1.6f,1.35f,0)},.09f,eCamel,7,.06f);Ball(t,"Tail Hair Tassel",new Vector3(-1.6f,1.29f,0),new Vector3(.18f,.3f,.18f),eDark);
   if(cargo){foreach(float side in new[]{-1f,1f}){Box(t,"Saddle Woven Side Rug",new Vector3(-.35f,2.12f,side*.48f),new Vector3(1.6f,.95f,.12f),eRed);Ball(t,"Attached Caravan Pack",new Vector3(-.4f,1.95f,side*.72f),new Vector3(1.1f,.8f,.62f),eCloth);Beam(t,"Pack Supporting Strap",new Vector3(-.4f,2.5f,side*.42f),new Vector3(-.4f,1.95f,side*.88f),.06f,eRope);}}
   Tube(t,"Camel Halter",new List<Vector3>{new Vector3(2.1f,3.55f,-.23f),new Vector3(2.2f,3.25f,-.1f),new Vector3(2.1f,3.5f,.23f)},.035f,eRope,6,.035f);
  }
  static void ERoyalStatue(Transform t,bool jackal=false){
   DetailedDesertStatue(t,jackal);
  }
  public static string BuildEgyptDesertArt(){
   themeFolder=Base+"/Egypt";Palette();DesertPalette();egyptAudit.Clear();var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");var models=new List<GameObject>(d.landmarks.Where(p=>p&&!p.name.StartsWith("Desert Art ")));
   var pyramid=DesertModel("Desert Pyramid",t=>EStonePyramid(t,18,10.7f,0));models.RemoveAll(p=>p.name=="Desert Pyramid");models.Insert(0,pyramid);
   var sphinx=DesertModel("Sphinx Courtyard",t=>{Sphinx(t);Palm(t,new Vector3(-5,0,1.5f),6);});models.RemoveAll(p=>p.name=="Sphinx Courtyard");models.Insert(1,sphinx);
   var temple=DesertModel("Nile Pylon Temple",EgyptianTemple);models.RemoveAll(p=>p.name=="Nile Pylon Temple");models.Insert(2,temple);
   var merchant=DesertModel("Desert Merchant Stall",t=>{EAwning(t,Vector3.zero,6.6f,3.6f,eCloth);EProduce(t,new Vector3(0,0,-.4f),true);foreach(float x in new[]{-2.6f,2.6f}){var jar=new GameObject("Grounded Merchant Jar");jar.transform.SetParent(t,false);jar.transform.localPosition=new Vector3(x,0,.5f);Urn(jar.transform,eClay,.8f);}});models.RemoveAll(p=>p.name=="Desert Merchant Stall");models.Insert(3,merchant);
   models.Add(DesertModel("Desert Art Flat Roof Village House",t=>EHouse(t,7,4.1f,4.8f)));
   models.Add(DesertModel("Desert Art Nubian Dome House",t=>{EHouse(t,4.8f,3.2f,4.5f,true);var wing=new GameObject("Lower Domed Room");wing.transform.SetParent(t,false);wing.transform.localPosition=new Vector3(4,0,1);EHouse(wing.transform,3.8f,2.7f,3.8f,true);}));
   models.Add(DesertModel("Desert Art Windcatcher House",t=>{EHouse(t,6.2f,4.2f,5);Box(t,"Connected Wind Tower",new Vector3(1.4f,5.3f,.8f),new Vector3(1.8f,2.2f,1.8f),eClay);Box(t,"Wind Tower Cap",new Vector3(1.4f,6.5f,.8f),new Vector3(2.1f,.2f,2.1f),eLight);foreach(float x in new[]{.95f,1.85f})EWindow(t,new Vector3(x,5.55f,-.13f),.4f,.7f);}));
   models.Add(DesertModel("Desert Art Terrace Stair House",t=>{EHouse(t,6.5f,3.5f,5.4f);for(int i=0;i<9;i++){float h=(i+1)*.4f;Box(t,"Grounded Exterior Stair",new Vector3(4,h*.5f,-3.2f+i*.7f),new Vector3(1.7f,h,.73f),eLight);}EAwning(t,new Vector3(-.5f,0,-3.8f),4.5f,2.8f,eBlue);}));
   models.Add(DesertModel("Desert Art Date Granary",t=>{foreach(float x in new[]{-2.0f,2.0f}){Lathe(t,"Grounded Grain Silo",new Vector3(x,0,.4f),new[]{0f,.25f,3.8f,4.2f},new[]{1.8f,1.8f,1.5f,.8f},eClay,12);EWindow(t,new Vector3(x,2.8f,-1.18f),.6f,.7f);}Box(t,"Granary Connecting Store",new Vector3(0,1.3f,1.1f),new Vector3(3,2.6f,2),eLight);for(int i=0;i<4;i++)Ball(t,"Grain Sack",new Vector3(-1.2f+i*.8f,.4f,-1.8f),new Vector3(.7f,.8f,.65f),eCloth);}));
   models.Add(DesertModel("Desert Art Produce Souk",t=>{EAwning(t,Vector3.zero,5.5f,3.5f,eRed);EProduce(t,new Vector3(0,0,-.5f));}));
   models.Add(DesertModel("Desert Art Spice and Textile Bazaar",t=>{EAwning(t,Vector3.zero,6.5f,3.5f,eBlue);EProduce(t,new Vector3(0,0,-.6f),true);foreach(float x in new[]{-2.3f,2.3f}){var jar=new GameObject("Merchant Water Jar");jar.transform.SetParent(t,false);jar.transform.localPosition=new Vector3(x,0,.5f);Urn(jar.transform,eClay);}for(int i=0;i<3;i++)Box(t,"Rolled Woven Carpet",new Vector3(-1.2f+i,1.45f,1.0f),new Vector3(.55f,.5f,1.7f),i%2==0?eRed:eCloth);}));
   models.Add(DesertModel("Desert Art Bedouin Trading Tent",t=>{foreach(float x in new[]{-3f,0,3f})Beam(t,"Tent Main Pole",new Vector3(x,0,0),new Vector3(x,3,0),.12f,eWood);foreach(float side in new[]{-1f,1f}){MeshObject(t,"Woven Tent Roof",new List<Vector3>{new Vector3(-3.5f,3,0),new Vector3(3.5f,3,0),new Vector3(3.5f,1.6f,side*2.5f),new Vector3(-3.5f,1.6f,side*2.5f)},side<0?new List<int>{0,1,2,0,2,3,2,1,0,3,2,0}:new List<int>{0,2,1,0,3,2,1,2,0,2,3,0},eCloth);foreach(float x in new[]{-3f,3f}){Beam(t,"Tent Side Support",new Vector3(x,0,side*2.5f),new Vector3(x,1.6f,side*2.5f),.09f,eWood);Beam(t,"Pegged Tent Rope",new Vector3(x,1.6f,side*2.5f),new Vector3(x,.08f,side*3.7f),.035f,eRope);Box(t,"Grounded Tent Peg",new Vector3(x,.14f,side*3.7f),new Vector3(.08f,.28f,.08f),eWood);}}EProduce(t,new Vector3(0,0,-1),true);}));
   models.Add(DesertModel("Desert Art Caravanserai",t=>{foreach(float side in new[]{-1f,1f}){Box(t,"Caravanserai Gate Wing",new Vector3(side*5,2.3f,0),new Vector3(4,4.6f,7),eClay);Box(t,"Wing Roof Cornice",new Vector3(side*5,4.7f,0),new Vector3(4.3f,.2f,7.3f),eLight);for(int i=0;i<3;i++)Box(t,"Gate Roof Merlon",new Vector3(side*5+(i-1)*1.3f,5.0f,-3.1f),new Vector3(.7f,.6f,.6f),eLight);EWindow(t,new Vector3(side*5,2.4f,-3.55f),1,1.1f);}Box(t,"Connected Gate Lintel",new Vector3(0,4.4f,0),new Vector3(6.2f,.7f,7),eLight);Arc(t,"Caravanserai Entrance Vault",new Vector3(0,1.7f,-2.6f),2.4f,.55f,1.6f,eLight,16);foreach(float side in new[]{-1f,1f})Box(t,"Gate Arch Pier",new Vector3(side*2.65f,.85f,-2.6f),new Vector3(.55f,1.7f,1.6f),eLight);Box(t,"Courtyard Rear Wall",new Vector3(0,1.6f,3.4f),new Vector3(14,3.2f,.4f),eClay);EArch(t,new Vector3(0,.2f,3.15f),1.7f,2.5f);}));
   models.Add(DesertModel("Desert Art Royal Sun Palace",t=>{Box(t,"Palace Ground Terrace",new Vector3(0,.3f,0),new Vector3(17,.6f,9),eLight);Box(t,"Palace Central Hall",new Vector3(0,3.0f,.4f),new Vector3(10,4.8f,6.5f),eClay);foreach(float side in new[]{-1f,1f}){Box(t,"Palace Flanking Tower",new Vector3(side*6.5f,3.65f,.5f),new Vector3(3.5f,6.1f,6),eLight);Pyramid(t,"Palace Gold Roof",new Vector3(side*6.5f,6.7f,.5f),3.8f,1.3f,gold);for(int i=0;i<2;i++)EWindow(t,new Vector3(side*6.5f,2.3f+i*2,-2.55f),1,1.1f);}for(int i=-2;i<=2;i++){Cylinder(t,"Palace Porch Column",new Vector3(i*2.1f,.6f,-3.8f),.25f,4.5f,eLight,12);Box(t,"Porch Column Capital",new Vector3(i*2.1f,5.0f,-3.8f),new Vector3(.9f,.3f,.9f),gold);}Box(t,"Supported Palace Porch Roof",new Vector3(0,5.35f,-3.1f),new Vector3(11,.4f,3),eLight);EArch(t,new Vector3(0,.6f,-2.9f),2.1f,3.8f);for(int i=0;i<3;i++)Box(t,"Palace Approach Step",new Vector3(0,.1f+i*.1f,-4.9f+i*.5f),new Vector3(7,.2f+i*.2f,1.1f),eLight);}));
   models.Add(DesertModel("Desert Art Camel and Bedouin",t=>{ECamel(t,Vector3.zero);EBedouin(t,new Vector3(4.15f,0,-.1f));Tube(t,"Handler Lead Rope",new List<Vector3>{new Vector3(2.15f,3.4f,-.23f),new Vector3(3,2.35f,-.2f),new Vector3(3.95f,1.5f,-.15f)},.035f,eRope,6,.035f);}));
   models.Add(DesertModel("Desert Art Loaded Camel Caravan",t=>{ECamel(t,new Vector3(-4,0,0));ECamel(t,new Vector3(2,0,.2f));EBedouin(t,new Vector3(6.15f,0,.1f));Tube(t,"Leading Camel Handler Rope",new List<Vector3>{new Vector3(4.15f,3.4f,-.03f),new Vector3(5,2.4f,0),new Vector3(5.95f,1.5f,.05f)},.035f,eRope,6,.035f);Tube(t,"Between Camel Tether",new List<Vector3>{new Vector3(-1.85f,3.4f,-.23f),new Vector3(-.7f,2.6f,-.3f),new Vector3(.7f,2.05f,-.15f)},.035f,eRope,6,.035f);}));
   models.Add(DesertModel("Desert Art Royal Pharaoh Statue",t=>ERoyalStatue(t)));
   models.Add(DesertModel("Desert Art Jackal Guardian Statue",t=>ERoyalStatue(t,true)));
   models.Add(DesertModel("Desert Art Stepped Pyramid",t=>{for(int i=0;i<8;i++){float w=17-i*1.8f;Box(t,"Mastaba Stone Tier",new Vector3(0,i*.8f+.4f,0),new Vector3(w,.8f,w),i%2==0?eLight:eClay);}EArch(t,new Vector3(0,0,-8.53f),1.5f,2.1f);}));
   models.Add(DesertModel("Desert Art Bent Pyramid",t=>EStonePyramid(t,20,11,1)));
   models.Add(DesertModel("Desert Art Twin Royal Pyramids",t=>{var left=new GameObject("Larger Royal Pyramid");left.transform.SetParent(t,false);left.transform.localPosition=new Vector3(-7,0,1);EStonePyramid(left.transform,12,7.5f,0);var right=new GameObject("Smaller Royal Pyramid");right.transform.SetParent(t,false);right.transform.localPosition=new Vector3(7,0,-1);EStonePyramid(right.transform,10,6,0);}));
   d.landmarks=models.ToArray();BuildEgyptStreetDetails(d);BuildEgyptHorizon(d);
   d.clouds=new GameObject[0];d.waterAcrossRoad=false;d.groundMaterial=Paint("Desert Warm Sand Ground",new Color(.83f,.62f,.34f),.65f);d.pathMaterial=Paint("Desert Dusty Sandstone Paving",new Color(.88f,.71f,.46f),.55f);
   EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();
   string report="# Egypt model connection audit\n\nMesh bounds expanded by 0.07 m for contact candidates. Grounded connected components are checked before material batching; this is a geometry support check, complemented by camera inspection.\n\n| Model | Mesh parts | Invalid data | Ungrounded components | Required neck / rope |\n|---|---:|---:|---:|---|\n"+string.Join("\n",egyptAudit.ToArray());
   System.IO.Directory.CreateDirectory("Docs");System.IO.File.WriteAllText("Docs/EgyptModelAudit.md",report);return d.landmarks.Length+" structures, "+d.props.Length+" street props, "+d.background.Length+" horizon models";
  }
 }
}
#endif
