#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static class RomanCompositionSetup {
  static RomanCompositionSlot S(string asset,RomanAssetLayer layer,float d,float x,float scale,string role,params string[] alternatives){return new RomanCompositionSlot{asset=asset,layer=layer,distance=d,x=x,scale=scale,role=role,focal=role.StartsWith("Focus"),alternatives=alternatives};}
  static RomanPavedCourt C(string name,float d,float length,float x,float width){return new RomanPavedCourt{name=name,distance=d,length=length,x=x,width=width};}
  public static string Build(){
   const string folder="Assets/Speed-Staes/Prefabs/Themes/RomeByzantium";string path=folder+"/RomanComposition.asset";
   var plan=AssetDatabase.LoadAssetAtPath<RomanCompositionPlan>(path);if(!plan){plan=ScriptableObject.CreateInstance<RomanCompositionPlan>();AssetDatabase.CreateAsset(plan,path);}
   const RomanAssetLayer B=RomanAssetLayer.Building,P=RomanAssetLayer.StreetProp,T=RomanAssetLayer.Plant,H=RomanAssetLayer.Skyline,R=RomanAssetLayer.Countryside;
   plan.districts=new[]{
    new RomanDistrict{name="Market Neighborhood",length=58,courts=new[]{C("Market Forecourt",20,33,-23,19),C("Well Court Opening",40,13,-23,18)},slots=new[]{
     S("Roman Insula Shops",B,7,-24.5f,.62f,"Shop Cluster"),
     S("Striped Roman Market",B,18,-24.5f,.79f,"Shop Cluster","Ceramic Kiln Workshop"),
     S("Ceramic Kiln Workshop",B,30,-25,.72f,"Craft Cluster","Striped Roman Market"),
     S("Courtyard Roman Villa",B,51,-26,.72f,"Residential Edge"),
     S("Stone Well With Winch",P,40,-19,.88f,"Well Court"),S("Carved Marble Bench",P,43,-17,.82f,"Well Court"),
     S("Roman Produce Cart",P,21,-17,.82f,"Market Goods"),S("Roman Amphora",P,14,-18,.78f,"Market Goods"),S("Roman Amphora",P,16,-18.4f,.62f,"Market Goods"),
     S("Terracotta Garden Planter",P,4,-17,.85f,"Shop Entry"),S("Terracotta Garden Planter",P,10,-17,.85f,"Shop Entry"),S("Excavated Column Fragments",P,45,-22,.66f,"Well Court"),
     S("Olive Tree",T,35,-22,.74f,"Court Frame"),S("Cypress Tree",T,55,-24,.63f,"Residential Frame"),
     S("Horizon Gabled Merchant Quarter",H,10,-64,.9f,"Town Backdrop"),S("Horizon Stepped Hill Town",H,37,-68,.92f,"Town Backdrop")}},
    new RomanDistrict{name="Forum Square",length=64,courts=new[]{C("Forum Entrance Paving",9,18,-26,26),C("Main Forum Square",33,34,-28,32)},slots=new[]{
     S("Triumphal Arch",B,7,-41,.57f,"Forum Gateway"),
     S("Marble Forum Temple",B,31,-43,.75f,"Focus Forum Monument","Turquoise Dome Basilica","Circular Copper Roof Shrine"),
     S("Tiered Fountain Garden",B,32,-23.8f,.62f,"Low Fountain Court"),
     S("Roman Insula Shops",B,57,-27,.60f,"Square Exit","Courtyard Roman Villa"),
     S("Cypress Tree",T,16,-27,.68f,"Left Monument Frame"),S("Cypress Tree",T,47,-27,.73f,"Right Monument Frame"),
     S("Roman Red Standard",P,4,-18,.68f,"Gateway Markers"),S("Roman Red Standard",P,11,-18,.68f,"Gateway Markers"),
     S("Carved Marble Bench",P,23,-18,.9f,"Square Seating"),S("Carved Marble Bench",P,41,-18,.9f,"Square Seating"),
     S("Marble Sundial",P,49,-21,.85f,"Square Exit Detail"),S("Terracotta Garden Planter",P,20,-20,.8f,"Fountain Approach"),S("Terracotta Garden Planter",P,44,-20,.8f,"Fountain Approach"),
     S("Horizon Terraced Imperial Palace",H,13,-72,.95f,"Civic Backdrop"),S("Horizon Abandoned Roofless Hall",H,51,-68,.94f,"Civic Backdrop")}},
    new RomanDistrict{name="Villas and City Edge",length=55,courts=new[]{C("Villa Garden Apron",10,19,-23,20),C("Workshop Yard",28,16,-24,22)},slots=new[]{
     S("Courtyard Roman Villa",B,6,-25,.76f,"Villa Garden","Roman Insula Shops"),
     S("Ceramic Kiln Workshop",B,21,-26,.74f,"Rural Craft Yard"),
     S("Tiered Fountain Garden",B,43,-29,.7f,"Roadside Garden","Courtyard Roman Villa"),
     S("Olive Tree",T,15,-21,.82f,"Garden Frame"),S("Olive Tree",T,50,-25,.78f,"Garden Frame"),
     S("Roman Produce Cart",P,28,-18,.9f,"Workshop Deliveries"),S("Stone Well With Winch",P,37,-18,.9f,"Roadside Water"),
     S("Terracotta Garden Planter",P,2,-17,.9f,"Villa Entry"),S("Terracotta Garden Planter",P,10,-17,.9f,"Villa Entry"),
     S("Roman Amphora",P,24,-19,.8f,"Workshop Goods"),S("Roman Amphora",P,25.5f,-19.2f,.65f,"Workshop Goods"),
     S("Countryside Terraced Vineyard",R,30,-54,.9f,"Edge Farmland","Countryside Fenced Farmstead")}},
    new RomanDistrict{name="Pasture and Village",length=65,courts=new[]{C("Village Entrance Apron",6,15,-23,18),C("Wayside Well Apron",34,12,-21,16)},slots=new[]{
     S("Courtyard Roman Villa",B,5,-27,.7f,"Village Home"),
     S("Striped Roman Market",B,57,-28,.64f,"Farm Produce Stall","Ceramic Kiln Workshop"),
     S("Countryside Shepherd Flock",R,23,-46,.94f,"Open Pasture View","Countryside Open Olive Meadow","Countryside Mountain Pasture"),
     S("Countryside Village Chapel",R,49,-65,.76f,"Village Horizon","Countryside Fenced Farmstead","Countryside Terraced Vineyard"),
     S("Olive Tree",T,12,-24,.8f,"Pasture Left Frame"),S("Olive Tree",T,44,-25,.72f,"Village Frame"),
     S("Stone Well With Winch",P,34,-18,.9f,"Wayside Water"),S("Carved Marble Bench",P,37,-17,.9f,"Wayside Rest"),
     S("Roman Produce Cart",P,53,-18,.82f,"Farm Produce"),S("Terracotta Garden Planter",P,60,-18,.9f,"Farm Produce")}}
   };
   // Civic landmarks need their own spaces and roles; a housing-only pool cannot express Rome.
   var originals=plan.districts;
   var arena=new RomanDistrict{name="Arena and Processional Walk",length=66,courts=new[]{C("Arena Arrival Terrace",29,42,-30,30)},slots=new[]{
    S("Byzantine Brick Watchtower",B,7,-33,.55f,"Arena Approach"),
    S("Oval Roman Amphitheatre",B,29,-37,.85f,"Focus Amphitheatre"),
    S("Statue and Mosaic Plaza",B,56,-27,.72f,"Sculpture Terrace"),
    S("Marble Emperor Statue",P,17,-23,.85f,"Processional Statue"),S("Roman Red Standard",P,23,-21,.7f,"Arena Standards"),S("Roman Red Standard",P,36,-21,.7f,"Arena Standards"),
    S("Carved Marble Bench",P,47,-18,.9f,"Arena Rest"),S("Framed Mosaic Panel",P,60,-20,.8f,"Gallery Mosaic"),
    S("Cypress Tree",T,43,-29,.7f,"Arena Tree Frame"),
    S("Horizon Ancient Terraced Ruins",H,17,-68,.9f,"Arena Backdrop"),S("Horizon Round Tower City Gate",H,51,-73,.85f,"Arena Backdrop")}};
   var baths=new RomanDistrict{name="Baths and Sculpture Gardens",length=62,courts=new[]{C("Baths Garden Terrace",24,36,-28,28)},slots=new[]{
    S("Terracotta Vault Baths",B,23,-32,.82f,"Focus Public Baths"),
    S("Ceramic Kiln Workshop",B,53,-26,.68f,"Baths Service Lane","Striped Roman Market"),
    S("Marble Emperor Statue",P,10,-24,.85f,"Baths Sculpture"),S("Marble Sundial",P,39,-22,.9f,"Garden Sundial"),S("Framed Mosaic Panel",P,45,-22,.9f,"Baths Mosaic"),
    S("Olive Tree",T,4,-26,.8f,"Baths Garden Frame"),S("Cypress Tree",T,41,-27,.7f,"Baths Garden Frame"),
    S("Carved Marble Bench",P,34,-18,.9f,"Baths Seating"),S("Terracotta Garden Planter",P,31,-23,.9f,"Baths Garden"),
    S("Horizon Garden Wall Pavilions",H,17,-65,.95f,"Garden Backdrop"),S("Horizon Long Roof Warehouses",H,47,-73,.85f,"Garden Backdrop")}};
   var sacred=new RomanDistrict{name="Basilica and Bell Courtyard",length=70,courts=new[]{C("Basilica Forecourt",26,35,-29,30),C("Bell Courtyard Apron",59,17,-27,24)},slots=new[]{
    S("Turquoise Dome Basilica",B,25,-38,.46f,"Focus Basilica"),
    S("Ivory Belfry Courtyard",B,58,-33,.43f,"Bell Courtyard"),
    S("Marble Emperor Statue",P,9,-24,.8f,"Sacred Entry Sculpture"),S("Framed Mosaic Panel",P,42,-22,.85f,"Sacred Mosaic"),
    S("Cypress Tree",T,10,-28,.7f,"Basilica Frame"),S("Cypress Tree",T,41,-28,.75f,"Basilica Frame"),S("Olive Tree",T,66,-25,.65f,"Belfry Garden"),
    S("Carved Marble Bench",P,46,-18,.9f,"Pilgrim Rest"),S("Terracotta Garden Planter",P,52,-21,.85f,"Belfry Entry"),
    S("Horizon Hillside Monastery",H,19,-72,.95f,"Sacred Backdrop"),S("Horizon Twin Slender Spires",H,53,-78,.9f,"Sacred Backdrop")}};
   var gallery=new RomanDistrict{name="Shrine and Ancient Waterworks",length=67,courts=new[]{C("Round Shrine Court",19,26,-28,27)},slots=new[]{
    S("Circular Copper Roof Shrine",B,18,-33,.7f,"Focus Round Shrine"),
    S("Aqueduct Arcade",B,48,-43,.78f,"Ancient Waterworks"),
    S("Marble Emperor Statue",P,34,-24,.85f,"Waterworks Sculpture"),S("Excavated Column Fragments",P,56,-25,.85f,"Archaeological Garden"),
    S("Roman Amphora",P,6,-20,.9f,"Shrine Offering"),S("Framed Mosaic Panel",P,28,-22,.8f,"Shrine Mosaic"),S("Stone Well With Winch",P,61,-19,.85f,"Aqueduct Water"),
    S("Olive Tree",T,31,-28,.7f,"Shrine Frame"),S("Cypress Tree",T,64,-28,.65f,"Waterworks Frame"),
    S("Horizon Abandoned Roofless Hall",H,14,-72,.9f,"Ancient Backdrop"),S("Horizon Citadel Ridge",H,52,-78,.9f,"Ancient Backdrop")}};
   plan.districts=new[]{originals[0],arena,originals[1],baths,originals[2],sacred,originals[3],gallery};
   var library=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(folder+"/Definition.asset");
   foreach(var district in plan.districts)foreach(var slot in district.slots){
    if(slot.layer==RomanAssetLayer.Skyline){var alternatives=new System.Collections.Generic.List<string>();foreach(var asset in library.background)if(asset.name!=slot.asset)alternatives.Add(asset.name);slot.alternatives=alternatives.ToArray();}
    if(district.name=="Market Neighborhood"&&slot.layer==RomanAssetLayer.Building)slot.alternatives=new[]{"Roman Insula Shops","Striped Roman Market","Ceramic Kiln Workshop","Courtyard Roman Villa"};
    if(slot.role=="Rural Craft Yard")slot.alternatives=new[]{"Striped Roman Market","Roman Insula Shops"};
    if(slot.role=="Village Home")slot.alternatives=new[]{"Ceramic Kiln Workshop"};
    if(slot.role=="Roadside Garden")slot.alternatives=new[]{"Courtyard Roman Villa","Statue and Mosaic Plaza"};
   }
   RomanArtComposition.Enrich(plan,library);
   EditorUtility.SetDirty(plan);var d=AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(folder+"/Definition.asset");d.romanComposition=plan;EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();return path;
  }
 }
}
#endif



