#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  static void DetailRomanEquestrian(Transform t,Transform portrait){
   var light=Paint("Equestrian Bronze Raised Detail",new Color(.40f,.49f,.40f),.28f);
   var shade=Paint("Equestrian Bronze Recess",new Color(.26f,.35f,.29f),.32f);
   // All cast-metal parts share the bronze family; only the stone pedestal stays pale.
   foreach(var renderer in t.GetComponentsInChildren<Renderer>()){
    if(renderer.name.StartsWith("Equestrian Grounded Pedestal")||renderer.name.StartsWith("Equestrian Pedestal Crown"))continue;
    var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=rBronze;renderer.sharedMaterials=materials;
   }
   foreach(float side in new[]{-1f,1f}){
    // Sculpted shoulder / haunch contours sit on the barrel instead of separate floating balls.
    SculptureRibbon(t,"Horse Shoulder Muscle Plane",new[]{new Vector3(.50f,2.58f,side*.29f),new Vector3(.86f,2.36f,side*.44f),new Vector3(.86f,2.12f,side*.38f)},new[]{new Vector3(.83f,2.62f,side*.23f),new Vector3(1.02f,2.36f,side*.32f),new Vector3(.96f,2.12f,side*.29f)},rBronze);
    Tube(t,"Shoulder Tendon Carving",new List<Vector3>{new Vector3(.48f,2.57f,side*.31f),new Vector3(.73f,2.37f,side*.44f),new Vector3(.82f,2.12f,side*.37f)},.017f,shade,6,.012f);
    Tube(t,"Haunch Muscle Carving",new List<Vector3>{new Vector3(-.74f,2.65f,side*.27f),new Vector3(-1.03f,2.39f,side*.32f),new Vector3(-.92f,2.03f,side*.3f)},.018f,shade,6,.013f);
    Tube(t,"Horse Lower Leg Tendon",new List<Vector3>{new Vector3(.83f,1.94f,side*.40f),new Vector3(.75f,1.4f,side*.45f),new Vector3(.83f,.91f,side*.41f)},.015f,shade,6,.012f);
    // Raised eyelids, nostrils and mouth belong to the same bronze casting.
    Tube(t,"Horse Eyelid Relief",new List<Vector3>{new Vector3(1.63f,3.49f,side*.19f),new Vector3(1.71f,3.53f,side*.20f),new Vector3(1.79f,3.48f,side*.18f)},.022f,light,8,.016f);
    Tube(t,"Carved Horse Nostril",new List<Vector3>{new Vector3(1.83f,3.36f,side*.13f),new Vector3(1.89f,3.33f,side*.12f),new Vector3(1.82f,3.3f,side*.15f)},.023f,shade,8,.018f);
    Tube(t,"Horse Muzzle Lip",new List<Vector3>{new Vector3(1.58f,3.23f,side*.17f),new Vector3(1.80f,3.23f,side*.14f),new Vector3(1.87f,3.27f,side*.11f)},.016f,shade,6,.012f);
    Tube(t,"Embossed Bridle Cheek Strap",new List<Vector3>{new Vector3(1.3f,3.61f,side*.14f),new Vector3(1.49f,3.46f,side*.22f),new Vector3(1.68f,3.30f,side*.21f)},.036f,light,8,.03f);
    var bit=new GameObject("Attached Cast Bridle Ring");bit.transform.SetParent(t,false);bit.transform.localPosition=new Vector3(1.72f,3.31f,side*.20f);Arc(bit.transform,"Closed Bronze Bit Ring",Vector3.zero,.065f,.018f,.025f,light,12,0,360);
    // Contoured saddle cloth, edging and a girth pass across the horse's body.
    SculptureRibbon(t,"Cast Saddle Cloth",new[]{new Vector3(-.67f,2.74f,side*.2f),new Vector3(-.73f,2.56f,side*.39f),new Vector3(-.68f,2.22f,side*.43f)},new[]{new Vector3(.31f,2.74f,side*.2f),new Vector3(.38f,2.56f,side*.39f),new Vector3(.33f,2.22f,side*.43f)},shade);
    Tube(t,"Saddle Cloth Raised Border",new List<Vector3>{new Vector3(-.73f,2.60f,side*.40f),new Vector3(-.68f,2.22f,side*.45f),new Vector3(.33f,2.22f,side*.45f),new Vector3(.38f,2.60f,side*.40f)},.025f,light,6,.025f);
    Tube(t,"Saddle Girth Strap",new List<Vector3>{new Vector3(-.28f,2.73f,side*.19f),new Vector3(-.28f,2.34f,side*.45f),new Vector3(-.28f,1.89f,side*.22f)},.035f,light,8,.03f);
    Tube(t,"Stirrup Leather",new List<Vector3>{new Vector3(-.14f,2.71f,side*.34f),new Vector3(-.10f,2.23f,side*.57f),new Vector3(-.03f,1.92f,side*.56f)},.025f,rBronze,8,.025f);
    var stirrup=new GameObject("Grounded Rider Stirrup");stirrup.transform.SetParent(t,false);stirrup.transform.localPosition=new Vector3(-.02f,1.84f,side*.55f);Arc(stirrup.transform,"Closed Bronze Stirrup",Vector3.zero,.11f,.025f,.045f,rBronze,12,0,360);
    SculptureVolume(t,"Sculpted Rider Riding Boot",new Vector3(-.025f,1.8f,side*.52f),new[]{0f,.10f,.24f},new[]{.20f,.18f,.08f},new[]{.10f,.11f,.07f},new[]{0f,0f,0f},rBronze,12);
    SculptureVolume(t,"Defined Rider Gauntlet",new Vector3(.67f,3.08f,side*.20f),new[]{0f,.08f,.17f},new[]{.075f,.105f,.07f},new[]{.08f,.09f,.065f},new[]{0f,0f,0f},rBronze,12);
    for(int i=0;i<4;i++)Tube(t,"Rein Holding Finger",new List<Vector3>{new Vector3(.68f+(i-1.5f)*.035f,3.19f,side*.25f),new Vector3(.73f+(i-1.5f)*.035f,3.14f,side*.25f),new Vector3(.7f+(i-1.5f)*.035f,3.1f,side*.23f)},.012f,light,5,.01f);
   }
   SculptureVolume(t,"Contoured Saddle Seat",new Vector3(-.18f,2.54f,0),new[]{0f,.14f,.24f},new[]{.48f,.43f,.35f},new[]{.34f,.32f,.28f},new[]{0f,0f,0f},shade,20);
   for(int i=0;i<12;i++){
    float u=i/11f;Tube(t,"Sculpted Flowing Mane Strand",new List<Vector3>{new Vector3(1.25f-u*.41f,3.45f-u*.77f,0),new Vector3(1.14f-u*.43f,3.35f-u*.77f,-.16f),new Vector3(1.1f-u*.42f,3.2f-u*.74f,-.20f)},.05f,i%3==0?light:rBronze,8,.025f);
   }
   for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Tube(t,"Incised Horse Tail Hair",new List<Vector3>{new Vector3(-1.22f,2.4f,Mathf.Sin(a)*.1f),new Vector3(-1.5f,2.06f,.06f+Mathf.Sin(a)*.10f),new Vector3(-1.62f,1.48f,.08f+Mathf.Sin(a)*.055f)},.013f,i%2==0?light:shade,5,.01f);}
   SculptureVolume(t,"Anatomical Bronze Cuirass",new Vector3(-.18f,3.0f,-.02f),new[]{0f,.16f,.32f,.48f,.55f},new[]{.28f,.30f,.38f,.36f,.19f},new[]{.23f,.27f,.27f,.23f,.14f},new[]{0f,0f,0f,0f,0f},rBronze,24);
   foreach(float side in new[]{-1f,1f}){
    Tube(t,"Cuirass Pectoral Relief",new List<Vector3>{new Vector3(-.18f,3.35f,-.30f),new Vector3(-.18f+side*.14f,3.39f,-.29f),new Vector3(-.18f+side*.26f,3.32f,-.25f)},.025f,light,8,.02f);
    Tube(t,"Cuirass Lower Relief",new List<Vector3>{new Vector3(-.18f+side*.24f,3.17f,-.24f),new Vector3(-.18f+side*.14f,3.11f,-.29f),new Vector3(-.18f,3.1f,-.29f)},.02f,shade,6,.015f);
   }
   for(int i=0;i<7;i++){float x=-.48f+i*.10f;SculptureRibbon(t,"Individual Armour Skirt Tab",new[]{new Vector3(x,3.0f,-.24f),new Vector3(x-.02f,2.75f,-.31f)},new[]{new Vector3(x+.065f,3.0f,-.24f),new Vector3(x+.07f,2.75f,-.31f)},i%2==0?light:rBronze);}
   // The cloak remains connected to both shoulders and rests against the rider / saddle.
   SculptureRibbon(t,"Imperial Shoulder Cloak",new[]{new Vector3(-.55f,3.46f,.12f),new Vector3(-.65f,3.12f,.29f),new Vector3(-.73f,2.76f,.31f),new Vector3(-.82f,2.5f,.25f)},new[]{new Vector3(.06f,3.46f,.12f),new Vector3(.14f,3.12f,.29f),new Vector3(.13f,2.76f,.31f),new Vector3(.03f,2.5f,.25f)},shade);
   for(int i=0;i<5;i++)Tube(t,"Cloak Drapery Fold",new List<Vector3>{new Vector3(-.5f+i*.12f,3.42f,.14f),new Vector3(-.59f+i*.14f,3.07f,.31f),new Vector3(-.7f+i*.15f,2.67f,.30f)},.024f,rBronze,6,.017f);
   for(int row=0;row<2;row++)for(int i=0;i<11;i++)Box(t,"Equestrian Pedestal Dedication",new Vector3((i-5)*.20f,.26f+row*.13f,-1.015f),new Vector3(.09f,.065f,.02f),rLime);
  }
  public static string UpgradeRomanEquestrianDetail(){
   themeFolder=Base+"/RomeByzantium";Palette();RomanArtPalette();egyptAudit.Clear();var prefab=RomanQualityModel("Art Bronze Horse Monument",RomanEquestrianSculpture,true);ApplyRomanPixelPaint();AssetDatabase.SaveAssets();AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(prefab),ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);System.IO.File.WriteAllText("Docs/RomanEquestrianAudit.md",string.Join("\n",egyptAudit.ToArray()));return "Bronze rider portrait, anatomical horse, tack, armour and cloak rebuilt";
  }
 }
}
#endif
