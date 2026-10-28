#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  static Material rLime,rIvory,rShade,rBrick,rRoof,rTile,rOlive,rPine,rSoil,rBronze,rWine,rOchre,rWater,rPave;
  static void RomanArtPalette(){
   Paint("Warm Marble",new Color(.82f,.78f,.66f));Paint("Marble Shade",new Color(.62f,.60f,.50f));Paint("Meadow Green",new Color(.40f,.50f,.27f));Paint("Leaf Highlights",new Color(.57f,.63f,.34f));Paint("Painted Blue",new Color(.38f,.47f,.45f));
   rLime=Paint("Art Travertine",new Color(.83f,.77f,.63f),.24f);rIvory=Paint("Art Marble Edges",new Color(.94f,.89f,.76f),.3f);
   rShade=Paint("Art Deep Warm Recess",new Color(.27f,.30f,.27f),.35f);rBrick=Paint("Art Dusty Roman Brick",new Color(.61f,.33f,.24f),.25f);
   rRoof=Paint("Art Weathered Roof",new Color(.59f,.27f,.20f),.28f);rTile=Paint("Art Roof Sun Edges",new Color(.77f,.42f,.27f),.25f);
   rOlive=Paint("Art Olive Foliage",new Color(.40f,.49f,.25f),.35f);rPine=Paint("Art Pine Foliage",new Color(.25f,.39f,.25f),.3f);
   rSoil=Paint("Art Terracotta Pots",new Color(.65f,.40f,.27f),.3f);rBronze=Paint("Art Aged Bronze",new Color(.34f,.45f,.38f),.25f);
   rWine=Paint("Art Oxide Red Plaster",new Color(.64f,.30f,.23f),.32f);rOchre=Paint("Art Ochre Plaster",new Color(.80f,.61f,.35f),.3f);
   rWater=Paint("Art Fountain Water",new Color(.28f,.58f,.57f),.6f);rPave=Paint("Art Warm Paving",new Color(.71f,.69f,.59f),.45f);
  }
  static GameObject RomanArtModel(string name,Action<Transform> build){
   return BatchThemeModel(Model(name,build));
  }
  static GameObject BatchThemeModel(GameObject source){
   string name=source.name;var root=UnityEngine.Object.Instantiate(source);root.name=name;
   if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   var batches=new Dictionary<Material,List<CombineInstance>>();
   foreach(var f in root.GetComponentsInChildren<MeshFilter>()){var renderer=f.GetComponent<MeshRenderer>();if(!renderer||!f.sharedMesh)continue;var mats=renderer.sharedMaterials;
    for(int s=0;s<f.sharedMesh.subMeshCount;s++){var mat=mats[Mathf.Min(s,mats.Length-1)];if(!batches.ContainsKey(mat))batches[mat]=new List<CombineInstance>();batches[mat].Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=s,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});}}
   foreach(Transform child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
   int index=0;foreach(var pair in batches){var mesh=new Mesh{name=name+" material batch"};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
    string path=themeFolder+"/Meshes/"+name+"_Batched_"+(index++)+".asset";mesh.name=System.IO.Path.GetFileNameWithoutExtension(path);var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){RefreshGeneratedMesh(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
    var part=new GameObject(pair.Key.name);part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=saved;var r=part.AddComponent<MeshRenderer>();r.sharedMaterial=pair.Key;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
   }
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,themeFolder+"/Models/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
  }
  static void RRoof(Transform t,float width,float depth,float y,bool flat=false){
   if(flat){Box(t,"Roof Parapet",new Vector3(0,y,0),new Vector3(width+.45f,.27f,depth+.4f),rIvory);return;}
   float w=width*.5f+.3f,d=depth*.5f+.3f,h=1.25f;
   MeshObject(t,"Terracotta Gabled Roof",new List<Vector3>{new Vector3(-w,y,-d),new Vector3(w,y,-d),new Vector3(-w,y,d),new Vector3(w,y,d),new Vector3(0,y+h,-d),new Vector3(0,y+h,d)},new List<int>{0,2,4,2,5,4,4,5,1,5,3,1,0,4,1,2,3,5},rRoof);
   Beam(t,"Roof Ridge Cap",new Vector3(0,y+h,-d),new Vector3(0,y+h,d),.18f,rTile);
   for(float z=-d;z<=d;z+=.65f){Beam(t,"Left Tile Ribs",new Vector3(-w,y+.03f,z),new Vector3(0,y+h+.03f,z),.06f,rTile);Beam(t,"Right Tile Ribs",new Vector3(0,y+h+.03f,z),new Vector3(w,y+.03f,z),.06f,rTile);}
   Box(t,"Deep Roof Cornice",new Vector3(0,y-.12f,0),new Vector3(width+.45f,.22f,depth+.4f),rLime);
  }
  static void RArch(Transform t,Vector3 p,float width,float height,Material surround=null){
   float radius=width*.5f,spring=height-radius;var m=surround?surround:rIvory;
   Box(t,"Arch Recess",p+Vector3.up*spring*.5f,new Vector3(width,spring,.08f),rShade);
   var v=new List<Vector3>{p+Vector3.up*spring};var tr=new List<int>();for(int i=0;i<=12;i++){float a=i*Mathf.PI/12;v.Add(p+new Vector3(Mathf.Cos(a)*radius,spring+Mathf.Sin(a)*radius,0));if(i>0)tr.AddRange(new[]{0,i+1,i});}MeshObject(t,"Dark Arch Crown",v,tr,rShade);
   foreach(float side in new[]{-1f,1f})Box(t,"Arch Jamb",p+new Vector3(side*(radius+.1f),spring*.5f,-.03f),new Vector3(.2f,spring,.2f),m);
   Arc(t,"Voussoir Arch Ring",p+new Vector3(0,spring,-.03f),radius,.2f,.2f,m,12);
   for(int i=1;i<6;i++){float a=i*Mathf.PI/6;Beam(t,"Arch Wedge Joint",p+new Vector3(Mathf.Cos(a)*radius,spring+Mathf.Sin(a)*radius,-.14f),p+new Vector3(Mathf.Cos(a)*(radius+.2f),spring+Mathf.Sin(a)*(radius+.2f),-.14f),.025f,rLime);}
   Box(t,"Arch Sill",p+new Vector3(0,-.06f,-.04f),new Vector3(width+.5f,.16f,.25f),m);
  }
  static void RColumn(Transform t,Vector3 p,float h,float radius=.22f){
   Box(t,"Column Square Plinth",p+Vector3.up*.12f,new Vector3(radius*3,.24f,radius*3),rLime);
   Lathe(t,"Tapered Column Shaft",p+Vector3.up*.25f,new[]{0f,.16f,h-.65f,h-.5f},new[]{radius*1.2f,radius,radius*.85f,radius*1.2f},rIvory,12);
   Box(t,"Carved Capital",p+Vector3.up*(h-.14f),new Vector3(radius*3,.28f,radius*3),rIvory);
   for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Beam(t,"Column Fluting",p+new Vector3(Mathf.Cos(a)*radius*.99f,.6f,Mathf.Sin(a)*radius*.99f),p+new Vector3(Mathf.Cos(a)*radius*.88f,h-.65f,Mathf.Sin(a)*radius*.88f),.022f,rLime);}
  }
  static void RHouse(Transform t,float w,float h,float d,Material plaster,int floors,bool arcade=false,bool flat=false){
   Box(t,"Plastered Masonry",new Vector3(0,h*.5f,0),new Vector3(w,h,d),plaster);
   Box(t,"Stone Footing",new Vector3(0,.25f,0),new Vector3(w+.25f,.5f,d+.2f),rLime);
   for(int floor=1;floor<=floors;floor++){float y=(floor-.5f)*h/floors;int columns=Mathf.Max(2,Mathf.RoundToInt(w/2.5f));for(int i=0;i<columns;i++){float x=(i-(columns-1)*.5f)*w/columns;
     if(floor==1&&arcade)RArch(t,new Vector3(x,.4f,-d*.5f-.055f),1.5f,2.4f);
     else{Box(t,"Deep Window",new Vector3(x,y+.2f,-d*.5f-.05f),new Vector3(.85f,1.12f,.08f),rShade);Box(t,"Window Lintel",new Vector3(x,y+.85f,-d*.5f-.1f),new Vector3(1.12f,.16f,.25f),rIvory);Box(t,"Projecting Window Sill",new Vector3(x,y-.42f,-d*.5f-.2f),new Vector3(1.15f,.16f,.42f),rLime);foreach(float side in new[]{-1f,1f})Box(t,"Timber Shutter",new Vector3(x+side*.56f,y+.2f,-d*.5f-.1f),new Vector3(.18f,1.2f,.13f),wood);}
    }if(floor<floors)Box(t,"Floor String Course",new Vector3(0,floor*h/floors,-d*.5f-.12f),new Vector3(w+.15f,.16f,.3f),rIvory);}
   for(float y=.8f;y<h-.3f;y+=.6f)foreach(float side in new[]{-1f,1f})Box(t,"Corner Quoin",new Vector3(side*(w*.5f-.16f),y,-d*.5f-.07f),new Vector3(.38f,.27f,.17f),rLime);
   RRoof(t,w,d,h,flat);
  }
  static void RPlanter(Transform t,Vector3 p,float scale=1){Lathe(t,"Terracotta Urn",p,new[]{0f,.18f,.7f,.95f,1f},new[]{.24f,.4f,.48f,.36f,.4f},rSoil,10);Ball(t,"Oleander Leaves",p+Vector3.up*1.15f,new Vector3(1,.65f,.8f)*scale,rOlive);foreach(float x in new[]{-.3f,0,.3f})Ball(t,"Pink Flowers",p+new Vector3(x,1.4f,.05f),Vector3.one*.18f,rWine);}
  static void RCanopy(Transform t,Vector3 p,float w,float d,Material cloth){
   foreach(float x in new[]{-w*.5f,w*.5f})Beam(t,"Awning Post",p+new Vector3(x,0,-d),p+new Vector3(x,2.5f,-d),.09f,woodDark);
   for(int i=0;i<8;i++){float x=(i-3.5f)*w/8;var g=Box(t,"Linen Awning Stripe",p+new Vector3(x,2.65f,-d*.5f),new Vector3(w/8,.08f,d),i%2==0?cloth:rIvory);g.transform.localRotation=Quaternion.Euler(-8,0,0);Box(t,"Awning Valance",p+new Vector3(x,2.46f,-d),new Vector3(w/8,.28f,.06f),i%2==0?cloth:rIvory);}
  }
  static void RProduce(Transform t,bool bread){
   Box(t,"Market Counter",new Vector3(0,.8f,0),new Vector3(3,1.6f,1.1f),wood);for(int i=0;i<3;i++){Box(t,"Wooden Produce Tray",new Vector3((i-1)*.9f,1.68f,-.05f),new Vector3(.8f,.13f,.8f),woodDark);for(int j=0;j<4;j++)Ball(t,bread?"Round Loaf":"Fresh Fruit",new Vector3((i-1)*.9f+(j%2)*.28f-.14f,1.84f,(j/2)*.28f-.18f),bread?new Vector3(.35f,.19f,.26f):Vector3.one*.22f,bread?rOchre:i%2==0?rWine:rOlive);}
   RCanopy(t,new Vector3(0,0,.6f),3.5f,2.2f,bread?rRoof:rOlive);
  }
  static void RStatueBust(Transform t,Vector3 p,float scale){
   var go=new GameObject("Carved Senator Bust");go.transform.SetParent(t,false);go.transform.localPosition=p;go.transform.localScale=Vector3.one*scale;var s=go.transform;
   Box(s,"Bust Pedestal",new Vector3(0,.8f,0),new Vector3(.9f,1.6f,.9f),rLime);Box(s,"Pedestal Crown",new Vector3(0,1.65f,0),new Vector3(1.1f,.18f,1.1f),rIvory);
   Lathe(s,"Draped Bust Torso",new Vector3(0,1.75f,0),new[]{0f,.55f,.83f},new[]{.45f,.55f,.2f},rIvory,10);Ball(s,"Sculpted Head",new Vector3(0,2.95f,0),new Vector3(.55f,.74f,.59f),rIvory);Box(s,"Sculpted Nose",new Vector3(0,2.96f,-.32f),new Vector3(.11f,.18f,.15f),rIvory);
   for(int i=0;i<6;i++)Beam(s,"Toga Fold",new Vector3(-.37f+i*.14f,1.9f,-.26f),new Vector3(.15f,2.48f,-.35f),.035f,rLime);
  }
  static void RPine(Transform t,Vector3 p,float h){
   Tube(t,"Crooked Pine Trunk",new List<Vector3>{p,p+new Vector3(.2f,h*.4f,0),p+new Vector3(-.15f,h*.72f,.1f)},.24f,wood,9,.14f);
   for(int i=0;i<5;i++){float a=i*2.399f;Vector3 crown=p+new Vector3(Mathf.Cos(a)*h*.19f,h*.82f+Mathf.Sin(i)*.18f,Mathf.Sin(a)*h*.16f);Beam(t,"Pine Crown Branch",p+Vector3.up*h*.55f,crown,.1f,wood);Ball(t,"Umbrella Pine Canopy",crown,new Vector3(h*.66f,h*.26f,h*.59f),i%2==0?rPine:rOlive);}
  }
  public static string BuildRomanArtExpansion(){
   themeFolder=Base+"/RomeByzantium";Palette();RomanArtPalette();UpgradeRomanArena();var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");var buildings=new List<GameObject>(d.landmarks.Where(p=>p&&!p.name.StartsWith("Art ")));
   buildings.Add(RomanArtModel("Art Ochre Arcade Insula",t=>{RHouse(t,9,6.6f,5,rOchre,2,true);RCanopy(t,new Vector3(-2,0,-2.6f),3.6f,2,rWine);RPlanter(t,new Vector3(3.5f,0,-3));Box(t,"Brick Chimney",new Vector3(3,7.3f,.8f),new Vector3(.8f,2,.8f),rBrick);Box(t,"Painted Shop Fascia",new Vector3(-2,2.9f,-2.65f),new Vector3(3,.35f,.16f),rWine);}));
   buildings.Add(RomanArtModel("Art Red Plaster Tabernae",t=>{RHouse(t,11,4.8f,4.5f,rWine,2,true);RCanopy(t,new Vector3(2.5f,0,-2.3f),4,2,rOchre);RProduce(t,false);RPlanter(t,new Vector3(-4.5f,0,-3));}));
   buildings.Add(RomanArtModel("Art Baker Oven Courtyard",t=>{RHouse(t,6,4.2f,4.2f,rLime,1,false);RArch(t,new Vector3(-1,.2f,-2.18f),1.5f,2.5f);var oven=new GameObject("Brick Bread Oven");oven.transform.SetParent(t,false);oven.transform.localPosition=new Vector3(4,0,-1);Lathe(oven.transform,"Domed Oven",Vector3.up*.7f,new[]{0f,.5f,1.3f,1.6f},new[]{1.4f,1.4f,.9f,.06f},rBrick,14);Box(oven.transform,"Oven Foundation",new Vector3(0,.4f,0),new Vector3(3,.8f,2.7f),rLime);RArch(oven.transform,new Vector3(0,.8f,-1.42f),1.1f,1.1f);Cylinder(oven.transform,"Oven Flue",new Vector3(.3f,2.1f,.2f),.24f,1.2f,rBrick,10);var stall=new GameObject("Bread Table");stall.transform.SetParent(t,false);stall.transform.localPosition=new Vector3(-3,0,-3.3f);RProduce(stall.transform,true);}));
   buildings.Add(RomanArtModel("Art Terraced Merchant Loggia",t=>{RHouse(t,10,5.8f,5.6f,rLime,2,true,true);for(int i=0;i<5;i++)RColumn(t,new Vector3((i-2)*2.2f,5.9f,-2.7f),2.4f,.14f);Box(t,"Open Loggia Beam",new Vector3(0,8.3f,-2.7f),new Vector3(10.5f,.3f,.7f),rIvory);for(int i=0;i<4;i++)RPlanter(t,new Vector3(-3.8f+i*2.5f,5.95f,-1.8f));}));
   buildings.Add(RomanArtModel("Art Marble Nymphaeum",t=>{Box(t,"Fountain Back Wall",new Vector3(0,2.3f,.7f),new Vector3(11,4.6f,1.5f),rLime);for(int i=0;i<3;i++){float x=(i-1)*3.3f;RArch(t,new Vector3(x,.65f,-.12f),1.9f,3.3f);RColumn(t,new Vector3(x-1.3f,0,-.5f),4.5f,.15f);}RColumn(t,new Vector3(4.65f,0,-.5f),4.5f,.15f);Box(t,"Attic Cornice",new Vector3(0,4.7f,.4f),new Vector3(11.6f,.38f,2.4f),rIvory);Box(t,"Pool Stone Base",new Vector3(0,.22f,-2),new Vector3(11.6f,.45f,3.6f),rLime);Box(t,"Turquoise Pool Surface",new Vector3(0,.47f,-2),new Vector3(10.8f,.05f,2.8f),rWater);foreach(float z in new[]{-3.8f,-.2f})Box(t,"Pool Coping",new Vector3(0,.55f,z),new Vector3(12,.3f,.28f),rIvory);for(int i=-1;i<=1;i++)Cylinder(t,"Fountain Spout",new Vector3(i*3.3f,1.5f,-.4f),.13f,.4f,rBronze,8);}));
   buildings.Add(RomanArtModel("Art Senate Curia",t=>{RHouse(t,11,6,7,rBrick,1,false);for(int i=-2;i<=2;i++)RColumn(t,new Vector3(i*2.2f,.5f,-4.1f),4.8f,.25f);Box(t,"Portico Entablature",new Vector3(0,5.45f,-4.1f),new Vector3(11.8f,.55f,2),rIvory);MeshObject(t,"Carved Triangular Pediment",new List<Vector3>{new Vector3(-6,5.7f,-5.2f),new Vector3(6,5.7f,-5.2f),new Vector3(0,7.5f,-5.2f)},new List<int>{0,2,1},rLime);Beam(t,"Pediment Left Cornice",new Vector3(-6,5.7f,-5.25f),new Vector3(0,7.5f,-5.25f),.22f,rIvory);Beam(t,"Pediment Right Cornice",new Vector3(0,7.5f,-5.25f),new Vector3(6,5.7f,-5.25f),.22f,rIvory);for(int i=0;i<3;i++)Box(t,"Broad Curia Steps",new Vector3(0,.13f+i*.15f,-4.8f+i*.4f),new Vector3(12-i*.35f,.26f,2),rLime);RArch(t,new Vector3(0,.5f,-3.57f),2.2f,4.5f);}));
   buildings.Add(RomanArtModel("Art Garden Peristyle",t=>{Box(t,"Peristyle Court Paving",new Vector3(0,.08f,0),new Vector3(12,.16f,7),rPave);foreach(float x in new[]{-5f,5f})for(int i=0;i<4;i++)RColumn(t,new Vector3(x,.16f,-2.5f+i*1.8f),3.7f,.18f);foreach(float x in new[]{-5f,5f})Box(t,"Peristyle Colonnade Roof",new Vector3(x,4,0),new Vector3(1.8f,.36f,7),rRoof);Box(t,"Recessed Garden Pool",new Vector3(0,.2f,0),new Vector3(5.5f,.1f,3),rWater);for(int i=-1;i<=1;i++)RPlanter(t,new Vector3(i*2.8f,.1f,-2.8f));RomanMosaicStele(t,new Vector3(0,.16f,2.5f),.7f);}));
   buildings.Add(RomanArtModel("Art Broken Sanctuary",t=>{Box(t,"Ruined Sanctuary Platform",new Vector3(0,.22f,0),new Vector3(12,.44f,6.5f),rLime);for(int i=0;i<5;i++)RColumn(t,new Vector3((i-2)*2.2f,.44f,1.8f),i==1?2.1f:4.4f,.23f);Box(t,"Surviving Stone Lintel",new Vector3(2.2f,5.1f,1.8f),new Vector3(5.5f,.5f,1),rIvory);for(int i=0;i<9;i++){var g=Box(t,"Scattered Dressed Stone",new Vector3(-4+(i%5)*1.8f,.55f,-1.4f+(i/5)*1.4f),new Vector3(.9f,.4f,.7f),rLime);g.transform.localRotation=Quaternion.Euler(0,i*37,0);}var fallen=Cylinder(t,"Fallen Fluted Column",new Vector3(-2,.8f,-2),.4f,3.4f,rIvory,12);fallen.transform.localRotation=Quaternion.Euler(0,20,86);RPlanter(t,new Vector3(5,0,-2));}));
   buildings.Add(RomanArtModel("Art Imperial Gatehouse",t=>{foreach(float x in new[]{-5f,5f}){Box(t,"Gate Brick Tower",new Vector3(x,3,0),new Vector3(3.3f,6,4),rBrick);Box(t,"Tower Pale Cornice",new Vector3(x,6.05f,0),new Vector3(3.7f,.35f,4.4f),rIvory);RArch(t,new Vector3(x,2.5f,-2.08f),1.0f,1.8f);for(int i=0;i<3;i++)Box(t,"Tower Merlon",new Vector3(x+(i-1)*1.2f,6.6f,0),new Vector3(.7f,.8f,4),rLime);}Box(t,"Gate Attic",new Vector3(0,5.2f,0),new Vector3(7,.8f,3),rLime);Arc(t,"Monumental Entry Arch",new Vector3(0,2,-.3f),3.25f,.55f,2.8f,rIvory,16);foreach(float x in new[]{-3.5f,3.5f})Box(t,"Gate Arch Pier",new Vector3(x,1,-.3f),new Vector3(.5f,2,2.8f),rIvory);}));
   buildings.Add(RomanArtModel("Art Round Brick Mausoleum",t=>{Lathe(t,"Round Brick Drum",Vector3.zero,new[]{0f,.4f,4.3f,4.6f},new[]{4.4f,4.4f,4,4.2f},rBrick,24);for(int i=0;i<12;i++){float a=i*Mathf.PI/6;RColumn(t,new Vector3(Mathf.Sin(a)*4.12f,.4f,Mathf.Cos(a)*4.12f),3.9f,.14f);}Lathe(t,"Stepped Copper Dome",new Vector3(0,4.6f,0),new[]{0f,.3f,.8f,1.4f,1.8f,2},new[]{4.4f,4.4f,3.8f,2.8f,1.5f,.04f},rBronze,24);RArch(t,new Vector3(0,.2f,-4.13f),1.8f,3.1f);Box(t,"Door Projecting Cornice",new Vector3(0,3.65f,-4.3f),new Vector3(3,.25f,.65f),rIvory);}));
   buildings.Add(RomanArtModel("Art Olive Press Estate",t=>{RHouse(t,7,3.8f,5,rOchre,1,true);for(int i=0;i<4;i++)Beam(t,"Pergola Post",new Vector3(4+i*1.5f,0,-3),new Vector3(4+i*1.5f,3.4f,-3),.16f,wood);for(int i=0;i<5;i++)Box(t,"Pergola Rafters",new Vector3(6.4f,3.5f,-2+i*1.1f),new Vector3(6,.15f,.14f),wood);for(int i=0;i<4;i++)Ball(t,"Pergola Vine Mass",new Vector3(4+i*1.4f,3.6f,-.5f),new Vector3(2,.5f,4),rOlive);Cylinder(t,"Olive Mill Stone",new Vector3(5,.4f,-1),1.4f,.6f,rLime,16);Box(t,"Olive Press Beam",new Vector3(5,1.8f,-1),new Vector3(4,.35f,.4f),woodDark);}));
   buildings.Add(RomanArtModel("Art Senator Garden Villa",t=>{RHouse(t,8,4.3f,5.5f,rIvory,1,true);var wing=new GameObject("Raised Ochre Wing");wing.transform.SetParent(t,false);wing.transform.localPosition=new Vector3(5,0,1);RHouse(wing.transform,4,6.5f,4,rOchre,2,false,true);for(int i=0;i<4;i++)RColumn(t,new Vector3(-3+i*2,.1f,-4),3.3f,.15f);Box(t,"Garden Loggia Roof",new Vector3(0,3.6f,-4),new Vector3(8,.25f,2.2f),rRoof);RPlanter(t,new Vector3(-4.5f,0,-3));RPlanter(t,new Vector3(4.5f,0,-3));}));
   d.landmarks=buildings.ToArray();
   var props=new List<GameObject>(d.props.Where(p=>p&&!p.name.StartsWith("Art ")));
   props.Add(RomanArtModel("Art Bread Merchant Stall",t=>RProduce(t,true)));
   props.Add(RomanArtModel("Art Fruit Merchant Stall",t=>RProduce(t,false)));
   props.Add(RomanArtModel("Art Oleander Urn",t=>RPlanter(t,Vector3.zero)));
   props.Add(RomanArtModel("Art Travertine Milestone",t=>{Cylinder(t,"Milestone Base",Vector3.zero,.5f,.2f,rLime,12);Cylinder(t,"Rounded Road Marker",Vector3.up*.2f,.32f,1.6f,rIvory,12);for(int i=0;i<4;i++)Box(t,"Engraved Numeral",new Vector3((i-1.5f)*.1f,1.25f,-.32f),new Vector3(.035f,.27f,.025f),rShade);}));
   props.Add(RomanArtModel("Art Wine Amphora Stack",t=>{for(int i=0;i<5;i++){Vector3 p=new Vector3((i%3)*.75f-.75f,0,(i/3)*.8f);Lathe(t,"Pointed Wine Amphora",p,new[]{0f,.2f,.8f,1.15f,1.35f},new[]{.08f,.3f,.4f,.17f,.19f},i%2==0?rSoil:rOchre,12);foreach(float x in new[]{-.3f,.3f})Arc(t,"Amphora Handle",p+new Vector3(x,.97f,0),.18f,.055f,.07f,rSoil,8,0,290);}}));
   props.Add(RomanArtModel("Art Ivy Garden Wall",t=>{Box(t,"Garden Wall Core",new Vector3(0,.65f,0),new Vector3(10,1.3f,.5f),rLime);Box(t,"Garden Wall Coping",new Vector3(0,1.35f,0),new Vector3(10.3f,.18f,.7f),rIvory);foreach(float x in new[]{-4.8f,4.8f}){Box(t,"Garden Pier",new Vector3(x,.85f,0),new Vector3(.7f,1.7f,.8f),rLime);Box(t,"Pier Cap",new Vector3(x,1.75f,0),new Vector3(.9f,.16f,1),rIvory);}for(int i=0;i<7;i++){float x=-4+i*1.3f;Box(t,"Masonry Bed Joint",new Vector3(x,.64f,-.27f),new Vector3(1.1f,.025f,.025f),rShade);if(i%2==0)Ball(t,"Trailing Ivy",new Vector3(x,1.12f,-.34f),new Vector3(1.5f,.65f,.35f),rOlive);}}));
   props.Add(RomanArtModel("Art Civic Colonnade",t=>{for(int i=0;i<4;i++)RColumn(t,new Vector3((i-1.5f)*2.6f,0,0),3.8f,.2f);Box(t,"Gallery Stone Entablature",new Vector3(0,3.95f,0),new Vector3(9,.45f,1.5f),rLime);Box(t,"Gallery Overhanging Cornice",new Vector3(0,4.25f,0),new Vector3(9.4f,.18f,1.8f),rIvory);for(int i=0;i<15;i++)Box(t,"Gallery Dentil Frieze",new Vector3(-4.2f+i*.6f,4.1f,-.8f),new Vector3(.22f,.15f,.25f),rIvory);}));
   props.Add(RomanArtModel("Art Mosaic Ground Medallion",t=>{Cylinder(t,"Mosaic Outer Disc",new Vector3(0,.06f,0),2.6f,.025f,rShade,32);Cylinder(t,"Mosaic Warm Disc",new Vector3(0,.09f,0),2.4f,.02f,rIvory,32);for(int i=0;i<16;i++){float a=i*Mathf.PI/8;var g=Box(t,"Mosaic Border Tessera",new Vector3(Mathf.Sin(a)*2.1f,.12f,Mathf.Cos(a)*2.1f),new Vector3(.28f,.025f,.28f),i%2==0?rWine:rBronze);g.transform.localRotation=Quaternion.Euler(0,i*22.5f,0);}for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var g=Box(t,"Mosaic Compass Petal",new Vector3(Mathf.Sin(a)*.85f,.12f,Mathf.Cos(a)*.85f),new Vector3(.55f,.025f,1.05f),i%2==0?rWine:rBronze);g.transform.localRotation=Quaternion.Euler(0,i*45,0);}Cylinder(t,"Mosaic Gold Centre",new Vector3(0,.13f,0),.4f,.025f,rOchre,12);}));
   d.props=props.ToArray();var plants=new List<GameObject>(d.vegetation.Where(p=>p&&!p.name.StartsWith("Art ")));
   plants.Add(RomanArtModel("Art Umbrella Pine",t=>RPine(t,Vector3.zero,5.5f)));
   plants.Add(RomanArtModel("Art Old Olive Grove",t=>{RPine(t,new Vector3(-1.5f,0,0),3.5f);RPine(t,new Vector3(1.8f,0,1),4.2f);}));
   plants.Add(RomanArtModel("Art Oleander Flower Bed",t=>{Box(t,"Garden Bed Edging",new Vector3(0,.17f,0),new Vector3(4,.34f,1.2f),rLime);for(int i=0;i<4;i++)RPlanter(t,new Vector3(-1.5f+i,0,0),.85f);}));
   plants.Add(RomanArtModel("Art Tall Cypress Pair",t=>{Tree(t,new Vector3(-.8f,0,0),4.8f,true);Tree(t,new Vector3(.9f,0,.6f),3.7f,true);}));d.vegetation=plants.ToArray();
   d.pathMaterial=rPave;d.groundMaterial=Paint("Art Mediterranean Ground",new Color(.48f,.55f,.33f),.6f);
   EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();BuildRomanStreetPlantExpansion();UpgradeRomanQuality();return d.landmarks.Length+" buildings, "+d.props.Length+" props, "+d.vegetation.Length+" plants";
  }
 }
}
#endif