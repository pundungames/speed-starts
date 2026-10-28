#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static partial class ThemeKitBuilder {
  // Hand-shaped horizontal cross sections: width, depth and centre vary independently.
  static void SculptureVolume(Transform t,string name,Vector3 centre,float[] heights,float[] widths,float[] depths,float[] offsets,Material material,int sides=16){
   var v=new List<Vector3>();var tr=new List<int>();for(int j=0;j<heights.Length;j++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(centre+new Vector3(Mathf.Sin(a)*widths[j],heights[j],offsets[j]+Mathf.Cos(a)*depths[j]));}
   for(int j=0;j<heights.Length-1;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;tr.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});}
   for(int end=0;end<2;end++){int row=end==0?0:heights.Length-1,k=v.Count;v.Add(centre+new Vector3(0,heights[row],offsets[row]));for(int i=0;i<sides;i++){int a=row*sides+i,b=row*sides+(i+1)%sides;tr.AddRange(end==0?new[]{k,a,b}:new[]{k,b,a});}}
   for(int i=0;i<tr.Count;i+=3){int swap=tr[i+1];tr[i+1]=tr[i+2];tr[i+2]=swap;}MeshObject(t,name,v,tr,material);
  }
  static void SculptureRibbon(Transform t,string name,Vector3[] left,Vector3[] right,Material material){var v=new List<Vector3>();var tr=new List<int>();for(int i=0;i<left.Length;i++){v.Add(left[i]);v.Add(right[i]);if(i<left.Length-1){int k=i*2;tr.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3,k+1,k+2,k,k+3,k+2,k+1});}}MeshObject(t,name,v,tr,material);}
  static void DetailedDesertStatue(Transform t,bool jackal){
   var stone=Paint("Sculpture Honey Limestone",new Color(.86f,.72f,.48f),.25f);var trim=Paint("Sculpture Carved Edges",new Color(.96f,.83f,.59f),.28f);var carved=Paint("Sculpture Warm Recess",new Color(.59f,.46f,.28f),.3f);var ink=Paint("Sculpture Weathered Indigo",new Color(.29f,.37f,.36f),.35f);
   Box(t,"Chamfered Monument Footing",new Vector3(0,.12f,0),new Vector3(2.55f,.24f,2.7f),stone);Box(t,"Plinth Upper Lip",new Vector3(0,.29f,0),new Vector3(2.35f,.10f,2.5f),trim);
   Box(t,"Throne Back",new Vector3(0,1.8f,.60f),new Vector3(1.55f,3.0f,.42f),stone);Box(t,"Throne Seat",new Vector3(0,1.45f,.10f),new Vector3(1.5f,.25f,1.45f),stone);
   foreach(float s in new[]{-1f,1f}){Box(t,"Throne Side Panel",new Vector3(s*.72f,.95f,.10f),new Vector3(.16f,1.25f,1.5f),stone);for(int i=0;i<4;i++){float z=-.4f+i*.33f;Box(t,"Throne Recessed Reed",new Vector3(s*.81f,.96f,z),new Vector3(.025f,.7f,.055f),carved);}Box(t,"Throne Side Cornice",new Vector3(s*.72f,1.6f,.1f),new Vector3(.25f,.13f,1.55f),trim);}
   SculptureVolume(t,"Anatomical Seated Torso",Vector3.zero,new[]{1.45f,1.7f,2.05f,2.4f,2.7f,2.91f,3.02f},new[]{.47f,.43f,.36f,.44f,.59f,.54f,.22f},new[]{.36f,.33f,.27f,.31f,.33f,.25f,.19f},new[]{.04f,.02f,.03f,.02f,0f,.03f,.04f},stone,20);
   SculptureVolume(t,"Connected Carved Neck",new Vector3(0,2.94f,0),new[]{0f,.25f,.42f},new[]{.22f,.19f,.24f},new[]{.20f,.18f,.20f},new[]{0f,0f,0f},stone);
   foreach(float s in new[]{-1f,1f}){
    SculptureVolume(t,"Sculpted Shin and Ankle",new Vector3(s*.32f,0,-.69f),new[]{.38f,.55f,.78f,1.12f,1.45f,1.58f},new[]{.18f,.14f,.17f,.20f,.23f,.23f},new[]{.33f,.17f,.19f,.21f,.23f,.25f},new[]{-.15f,0f,0f,0f,0f,.02f},stone);
    SculptureVolume(t,"Seated Thigh",new Vector3(s*.3f,0,-.25f),new[]{1.36f,1.48f,1.67f,1.77f},new[]{.23f,.28f,.27f,.20f},new[]{.52f,.55f,.49f,.32f},new[]{-.02f,-.02f,0f,.1f},stone);
    SculptureVolume(t,"Shoulder Upper Arm and Elbow",new Vector3(s*.59f,0,-.02f),new[]{1.64f,1.87f,2.13f,2.48f,2.76f,2.88f},new[]{.16f,.16f,.15f,.18f,.22f,.13f},new[]{.20f,.21f,.18f,.20f,.22f,.13f},new[]{-.30f,-.21f,-.08f,0f,0f,0f},stone);
    SculptureVolume(t,"Resting Forearm and Hand",new Vector3(s*.57f,0,-.44f),new[]{1.55f,1.64f,1.77f,1.88f},new[]{.15f,.18f,.16f,.14f},new[]{.27f,.35f,.29f,.13f},new[]{-.1f,-.04f,.03f,.16f},stone);
    for(int finger=0;finger<4;finger++)Beam(t,"Incised Hand Finger",new Vector3(s*.57f+(finger-1.5f)*.067f,1.65f,-.79f),new Vector3(s*.57f+(finger-1.5f)*.067f,1.56f,-.77f),.018f,carved);
    for(int toe=0;toe<4;toe++)Beam(t,"Carved Toes",new Vector3(s*.32f+(toe-1.5f)*.075f,.42f,-1.12f),new Vector3(s*.32f+(toe-1.5f)*.075f,.48f,-1.09f),.018f,carved);
   }
   SculptureRibbon(t,"Pleated Royal Kilt",new[]{new Vector3(-.42f,2.05f,-.23f),new Vector3(-.5f,1.73f,-.4f),new Vector3(-.48f,1.35f,-.89f)},new[]{new Vector3(.42f,2.05f,-.23f),new Vector3(.5f,1.73f,-.4f),new Vector3(.48f,1.35f,-.89f)},trim);
   for(int i=-5;i<=5;i++)Tube(t,"Kilt Engraved Pleat",new List<Vector3>{new Vector3(i*.067f,2.02f,-.25f),new Vector3(i*.083f,1.73f,-.42f),new Vector3(i*.08f,1.37f,-.90f)},.012f,carved,5,.012f);
   Box(t,"Royal Belt",new Vector3(0,2.06f,-.24f),new Vector3(.87f,.13f,.1f),ink);Box(t,"Belt Cartouche",new Vector3(0,2.02f,-.31f),new Vector3(.18f,.25f,.055f),trim);
   for(int band=0;band<3;band++){var pts=new List<Vector3>();for(int i=0;i<=12;i++){float a=Mathf.PI*i/12;pts.Add(new Vector3(Mathf.Cos(a)*(.46f+band*.035f),2.87f-Mathf.Sin(a)*(.22f+band*.055f),-.26f-Mathf.Sin(a)*.09f));}Tube(t,"Broad Usekh Collar",pts,.022f,band%2==0?ink:trim,6,.022f);}
   if(!jackal){
    SculptureVolume(t,"Sculpted Pharaoh Face",new Vector3(0,3.2f,-.04f),new[]{0f,.13f,.28f,.45f,.63f,.79f,.92f},new[]{.16f,.25f,.32f,.36f,.35f,.30f,.12f},new[]{.20f,.27f,.30f,.31f,.28f,.22f,.12f},new[]{0f,-.02f,-.02f,0f,.02f,.05f,.07f},stone,20);
    SculptureVolume(t,"Carved Nose Bridge",new Vector3(0,3.47f,-.30f),new[]{0f,.07f,.19f,.34f},new[]{.10f,.09f,.055f,.045f},new[]{.075f,.12f,.08f,.025f},new[]{-.025f,-.04f,0f,.02f},trim,8);
    foreach(float s in new[]{-1f,1f}){Tube(t,"Almond Eye Recess",new List<Vector3>{new Vector3(s*.07f,3.73f,-.30f),new Vector3(s*.19f,3.77f,-.31f),new Vector3(s*.29f,3.73f,-.26f)},.019f,carved,6,.012f);Tube(t,"Carved Brow Ridge",new List<Vector3>{new Vector3(s*.06f,3.84f,-.29f),new Vector3(s*.19f,3.87f,-.30f),new Vector3(s*.31f,3.8f,-.24f)},.027f,trim,6,.018f);}
    Tube(t,"Sculpted Mouth",new List<Vector3>{new Vector3(-.14f,3.40f,-.29f),new Vector3(0,3.38f,-.33f),new Vector3(.14f,3.40f,-.29f)},.014f,carved,6,.014f);
    SculptureVolume(t,"Crown Nemes Skull",new Vector3(0,3.72f,.1f),new[]{0f,.25f,.49f,.62f},new[]{.39f,.43f,.35f,.1f},new[]{.32f,.34f,.29f,.1f},new[]{0f,0f,.02f,.06f},ink,16);
    foreach(float s in new[]{-1f,1f}){var l=new[]{new Vector3(s*.31f,4.03f,-.09f),new Vector3(s*.46f,3.76f,-.11f),new Vector3(s*.61f,3.29f,-.08f),new Vector3(s*.48f,3.08f,-.24f)};var r=new[]{new Vector3(s*.42f,4.03f,.15f),new Vector3(s*.64f,3.76f,.15f),new Vector3(s*.74f,3.29f,.12f),new Vector3(s*.61f,3.08f,-.03f)};SculptureRibbon(t,"Sculpted Nemes Wing",l,r,ink);for(int i=0;i<9;i++){float u=i/8f;Beam(t,"Nemes Carved Stripe",new Vector3(s*(.45f+.15f*u),3.78f-u*.62f,-.12f),new Vector3(s*(.64f+.08f*u),3.78f-u*.62f,.14f-u*.10f),.027f,trim);}}
    SculptureVolume(t,"Tapered Ceremonial Beard",new Vector3(0,3.02f,-.32f),new[]{0f,.22f,.40f},new[]{.06f,.075f,.10f},new[]{.07f,.08f,.10f},new[]{-.07f,-.05f,0f},ink,8);
   }else{
    SculptureVolume(t,"Anubis Anatomical Skull",new Vector3(0,3.2f,0),new[]{0f,.15f,.39f,.62f,.82f},new[]{.17f,.29f,.34f,.29f,.14f},new[]{.23f,.31f,.34f,.26f,.12f},new[]{0f,0f,.02f,.04f,.06f},ink,16);
    SculptureVolume(t,"Anubis Tapered Muzzle",new Vector3(0,3.43f,-.26f),new[]{0f,.12f,.24f,.34f},new[]{.10f,.18f,.21f,.20f},new[]{.13f,.36f,.40f,.23f},new[]{-.17f,-.14f,-.10f,-.02f},ink,12);
    foreach(float s in new[]{-1f,1f}){SculptureVolume(t,"Carved Jackal Ear",new Vector3(s*.23f,3.87f,.03f),new[]{0f,.25f,.54f,.85f},new[]{.15f,.13f,.075f,.006f},new[]{.12f,.10f,.06f,.006f},new[]{0f,.02f,.05f,.08f},ink,10);SculptureRibbon(t,"Jackal Inner Ear Relief",new[]{new Vector3(s*.16f,3.98f,-.09f),new Vector3(s*.205f,4.35f,.01f),new Vector3(s*.23f,4.58f,.07f)},new[]{new Vector3(s*.30f,3.98f,-.09f),new Vector3(s*.255f,4.35f,.01f),new Vector3(s*.23f,4.58f,.07f)},carved);Beam(t,"Jackal Almond Eye",new Vector3(s*.17f,3.78f,-.27f),new Vector3(s*.29f,3.75f,-.22f),.025f,trim);}
    Beam(t,"Jackal Muzzle Mouth",new Vector3(-.17f,3.56f,-.53f),new Vector3(.17f,3.56f,-.53f),.018f,carved);
   }
   for(int row=0;row<3;row++)for(int col=0;col<5;col++){float x=(col-2)*.23f;Beam(t,"Plinth Incised Glyph",new Vector3(x,.16f,-1.36f),new Vector3(x+.07f,.21f,-1.36f),.018f,carved);}
  }
  public static string UpgradeEgyptSculptures(){
   themeFolder=Base+"/Egypt";Palette();DesertPalette();egyptAudit.Clear();DesertModel("Desert Art Royal Pharaoh Statue",t=>DetailedDesertStatue(t,false));DesertModel("Desert Art Jackal Guardian Statue",t=>DetailedDesertStatue(t,true));
   var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(themeFolder+"/Definition.asset");BuildEgyptHorizon(d);
   var shader=Shader.Find("Speed Stars/Desert Pixel Paint");int count=0;foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{themeFolder+"/Materials"})){var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(mat.shader.name!="Speed Stars/Pastel Street Prop"&&mat.shader!=shader)continue;mat.shader=shader;mat.SetFloat("_PixelSize",.12f);mat.SetFloat("_Grain",mat.name.Contains("Horizon")?.012f:mat.name.Contains("Ground")||mat.name.Contains("Paving")?.012f:.025f);EditorUtility.SetDirty(mat);count++;}
   EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();System.IO.File.WriteAllText("Docs/EgyptSculptureAudit.md",string.Join("\n",egyptAudit.ToArray()));return "Two custom sculpted statues; "+count+" outline-free pixel-painted materials";
  }
 }
}
#endif
