#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using SpeedStars.Themes;
namespace SpeedStars.Editor {
 public static class RomanArtComposition {
  static RomanCompositionSlot S(string name,RomanAssetLayer layer,float z,float x,float scale,string role,params string[] alternatives){return new RomanCompositionSlot{asset=name,layer=layer,distance=z,x=x,scale=scale,role=role,focal=role.StartsWith("Focus"),alternatives=alternatives};}
  static RomanPavedCourt C(string name,float z,float length,float x,float width){return new RomanPavedCourt{name=name,distance=z,length=length,x=x,width=width};}
  public static void Enrich(RomanCompositionPlan plan,LoopThemeDefinition library){
   if(!library.landmarks.Any(p=>p&&p.name=="Art Ochre Arcade Insula"))return;
   const RomanAssetLayer B=RomanAssetLayer.Building,P=RomanAssetLayer.StreetProp,T=RomanAssetLayer.Plant,H=RomanAssetLayer.Skyline,R=RomanAssetLayer.Countryside;
   var old=plan.districts;
   var street=new RomanDistrict{name="Via dei Mercanti - Living Roman Street",length=69,courts=new[]{C("Merchant Street Stone Apron",32,67,-26,25)},slots=new[]{
    S("Art Ochre Arcade Insula",B,7,-26,.68f,"Ochre Corner House"),S("Art Red Plaster Tabernae",B,23,-27,.76f,"Wine Merchants"),S("Art Baker Oven Courtyard",B,43,-27,.85f,"Baker Courtyard"),S("Art Terraced Merchant Loggia",B,61,-35,.66f,"Merchant Loggia"),
    S("Art Wine Amphora Stack",P,13,-21,.85f,"Wine Delivery"),S("Art Fruit Merchant Stall",P,30,-21,.92f,"Street Fruit Market"),S("Art Senator Marble Bust",P,53,-23,.85f,"Loggia Patron"),S("Art Oleander Urn",P,3,-21,.8f,"Shop Entrance"),S("Art Oleander Urn",P,10,-21,.8f,"Shop Entrance"),S("Art Lion Wall Fountain",P,36,-23,.9f,"Communal Drinking Fountain"),
    S("Art Umbrella Pine",T,53,-34,.92f,"Loggia Pine Frame"),S("Art Oleander Flower Bed",T,18,-22,.65f,"Street Garden"),
    S("Horizon Gabled Merchant Quarter",H,10,-61,1.15f,"Layered Roofline"),S("Horizon Stepped Hill Town",H,44,-72,1.2f,"Layered Roofline")}};
   var senate=new RomanDistrict{name="Curia and Civic Procession",length=73,courts=new[]{C("Civic Travertine Square",34,65,-30,35)},slots=new[]{
    S("Art Imperial Gatehouse",B,9,-42,.75f,"City Gate"),S("Art Senate Curia",B,38,-37,.76f,"Focus Senate Curia"),S("Statue and Mosaic Plaza",B,64,-28,.65f,"Civic Sculpture Court"),
    S("Art Bronze Horse Monument",P,24,-25,1,"Imperial Procession"),S("Art Senator Marble Bust",P,52,-22,1,"Senate Sculpture"),S("Roman Red Standard",P,32,-24,.72f,"Senate Standards"),S("Roman Red Standard",P,44,-24,.72f,"Senate Standards"),S("Carved Marble Bench",P,58,-19,1,"Civic Seating"),
    S("Art Tall Cypress Pair",T,19,-31,.85f,"Gate Garden"),S("Art Umbrella Pine",T,58,-37,.9f,"Civic Pine"),
    S("Horizon Terraced Imperial Palace",H,20,-75,1.1f,"Capitoline Backdrop"),S("Horizon Round Tower City Gate",H,61,-85,1.05f,"Capitoline Backdrop")}};
   var gardens=new RomanDistrict{name="Water Gardens and Peristyle",length=67,courts=new[]{C("Water Garden Walk",32,62,-27,29)},slots=new[]{
    S("Art Marble Nymphaeum",B,15,-31,.95f,"Focus Nymphaeum"),S("Art Garden Peristyle",B,46,-31,.95f,"Open Peristyle"),
    S("Art Senator Marble Bust",P,31,-23,.95f,"Garden Statue"),S("Art Oleander Urn",P,5,-23,1,"Water Garden Flowers"),S("Art Oleander Urn",P,25,-23,1,"Water Garden Flowers"),S("Art Lion Wall Fountain",P,59,-23,1,"Wayside Fountain"),S("Carved Marble Bench",P,34,-20,1,"Garden Seating"),
    S("Art Umbrella Pine",T,34,-40,1.1f,"Garden Pine Canopy"),S("Art Oleander Flower Bed",T,60,-27,1,"Garden Border"),S("Art Tall Cypress Pair",T,2,-34,.8f,"Water Garden Frame"),
    S("Horizon Garden Wall Pavilions",H,12,-68,1.1f,"Garden Backdrop"),S("Horizon Hillside Monastery",H,52,-80,1.15f,"Garden Backdrop")}};
   var ruins=new RomanDistrict{name="Ancient Sanctuary and Olive Grove",length=76,courts=new[]{C("Archaeological Entrance",13,19,-24,20)},slots=new[]{
    S("Art Broken Sanctuary",B,25,-34,.95f,"Focus Ancient Sanctuary"),S("Art Round Brick Mausoleum",B,59,-39,.81f,"Focus Round Mausoleum"),
    S("Art Travertine Milestone",P,6,-20,1,"Ancient Road Marker"),S("Excavated Column Fragments",P,43,-28,1.1f,"Excavation Stones"),S("Art Wine Amphora Stack",P,69,-24,.7f,"Archaeological Finds"),
    S("Art Old Olive Grove",T,9,-40,1.2f,"Ancient Olive Grove"),S("Art Umbrella Pine",T,44,-44,1.2f,"Sanctuary Canopy"),S("Art Oleander Flower Bed",T,70,-29,.9f,"Mausoleum Garden"),
    S("Horizon Ancient Terraced Ruins",H,21,-72,1.25f,"Archaeological Backdrop"),S("Horizon Citadel Ridge",H,60,-83,1.15f,"Archaeological Backdrop")}};
   var estate=new RomanDistrict{name="Olive Estate and Vineyard",length=75,courts=new[]{C("Estate Forecourt",18,31,-26,24)},slots=new[]{
    S("Art Olive Press Estate",B,17,-30,.85f,"Olive Estate"),S("Art Senator Garden Villa",B,53,-35,.78f,"Senator Estate"),
    S("Art Wine Amphora Stack",P,29,-23,1,"Estate Harvest"),S("Roman Produce Cart",P,35,-24,1,"Harvest Deliveries"),S("Art Travertine Milestone",P,69,-20,1,"Country Road Marker"),
    S("Art Old Olive Grove",T,36,-43,1.25f,"Estate Grove"),S("Art Oleander Urn",P,44,-25,1,"Villa Gate"),S("Art Oleander Urn",P,62,-25,1,"Villa Gate"),
    S("Countryside Terraced Vineyard",R,21,-65,1.2f,"Vineyard Landscape","Countryside Open Olive Meadow"),S("Horizon Hillside Monastery",H,56,-90,1.1f,"Estate Horizon")}};
   // Keep the full original civic catalogue; replace the repetitive residential opening.
   plan.districts=new[]{street,old[1],gardens,old[2],senate,old[3],old[5],estate,ruins,old[7],old[4],old[6]};
   foreach(var district in plan.districts){
    var dress=district.slots.ToList();
    bool garden=district.name.Contains("Garden")||district.name.Contains("Estate")||district.name.Contains("Villas");
    bool civic=district.name.Contains("Civic")||district.name.Contains("Forum")||district.name.Contains("Arena")||district.name.Contains("Basilica");
    if(garden){dress.Add(S("Art Ivy Garden Wall",P,3,-40,1,"Garden Boundary"));dress.Add(S("Art Ivy Garden Wall",P,district.length-6,-43,1.15f,"Garden Boundary"));}
    if(district==senate||district.name=="Forum Square")dress.Add(S("Art Civic Colonnade",P,district.length*.70f,-48,1,"Rear Civic Gallery"));
    if(garden||civic)dress.Add(S("Art Mosaic Ground Medallion",P,district.length*.52f,-24,1.25f,"Court Mosaic"));
    if(district==street){dress.Add(S("Art Ivy Garden Wall",P,34,-39,1.2f,"Street Courtyard Boundary"));dress.Add(S("Art Mosaic Ground Medallion",P,52,-25,.9f,"Loggia Mosaic"));}
    district.slots=dress.ToArray();
    foreach(var slot in district.slots){
     if(slot.asset=="Statue and Mosaic Plaza")slot.alternatives=new[]{"Art Garden Peristyle","Tiered Fountain Garden"};
     if(slot.asset=="Art Senator Garden Villa"||slot.asset=="Art Olive Press Estate")slot.alternatives=new[]{"Art Terraced Merchant Loggia","Courtyard Roman Villa"};
     if(slot.layer==B&&slot.role=="Focus Forum Monument")slot.alternatives=new string[0];
     if(slot.asset=="Olive Tree")slot.asset="Art Umbrella Pine";
     if(slot.asset=="Cypress Tree")slot.asset="Art Tall Cypress Pair";
     if(slot.asset=="Terracotta Garden Planter")slot.asset="Art Oleander Urn";
     if(slot.asset=="Roman Amphora")slot.asset="Art Wine Amphora Stack";
     if(slot.asset=="Courtyard Roman Villa"&&slot.role=="Village Home"){slot.alternatives=new[]{"Art Olive Press Estate","Art Senator Garden Villa"};}
     if(slot.role=="Rural Craft Yard")slot.alternatives=new[]{"Art Baker Oven Courtyard","Art Red Plaster Tabernae"};
     if(slot.role=="Square Exit")slot.alternatives=new[]{"Art Terraced Merchant Loggia"};
     if(slot.role=="Shop Cluster"||slot.role=="Residential Edge")slot.alternatives=new[]{"Art Ochre Arcade Insula","Art Red Plaster Tabernae","Art Baker Oven Courtyard","Art Senator Garden Villa"};
    }
   }
  }
 }
}
#endif
