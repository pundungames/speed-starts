using System;
using System.Collections.Generic;
using UnityEngine;
using SpeedStars.Themes;
namespace SpeedStars.Editor
{
    public static partial class ThemeKitBuilder
    {
        static void EgyptianTemple(Transform t)
        {
            Box(t,"Raised Temple Terrace",new Vector3(0,.18f,0),new Vector3(17,.36f,7.5f),sandLight);
            foreach(float x in new[]{-5.5f,5.5f})
            {
                var p=Lathe(t,"Tapered Pylon",new Vector3(x,.35f,.5f),new[]{0f,8f},new[]{3.3f,2.65f},sand,4);p.transform.localRotation=Quaternion.Euler(0,45,0);p.transform.localScale=new Vector3(1,1,.72f);
                Box(t,"Pylon Cornice",new Vector3(x,8.5f,.5f),new Vector3(5.5f,.45f,4.0f),sandLight);
                for(int row=0;row<4;row++){Box(t,"Recessed Glyph Panel",new Vector3(x,1.7f+row*1.3f,-1.87f),new Vector3(2.2f,.7f,.055f),row%2==0?teal:wood);Cylinder(t,"Sun Disc Relief",new Vector3(x,6.9f,-1.95f),.45f,.06f,gold,12).transform.localRotation=Quaternion.Euler(90,0,0);}
            }
            Box(t,"Gateway Lintel",new Vector3(0,5.4f,.3f),new Vector3(6.2f,.8f,3.6f),sandLight);Box(t,"Dark Recessed Door",new Vector3(0,2.5f,1.5f),new Vector3(4.2f,4.6f,.2f),woodDark);
            for(int step=0;step<3;step++)Box(t,"Approach Stair",new Vector3(0,.13f+step*.12f,-3.3f+step*.43f),new Vector3(6.2f,.24f,1.4f),sandLight);
            Cylinder(t,"Winged Sun",new Vector3(0,5.4f,-1.58f),.45f,.07f,gold,12).transform.localRotation=Quaternion.Euler(90,0,0);
            foreach(float sign in new[]{-1f,1f}){var wing=Box(t,"Winged Sun Feathers",new Vector3(sign*1.0f,5.45f,-1.58f),new Vector3(1.4f,.20f,.10f),teal);wing.transform.localRotation=Quaternion.Euler(0,0,sign*8);}
        }
        static void Obelisk(Transform t,float height=10)
        {Box(t,"Obelisk Base",new Vector3(0,.25f,0),new Vector3(2.4f,.5f,2.4f),sandLight);var shaft=Lathe(t,"Tapered Stone Obelisk",new Vector3(0,.5f,0),new[]{0f,height-1.2f},new[]{1.15f,.75f},sand,4);shaft.transform.localRotation=Quaternion.Euler(0,45,0);Pyramid(t,"Golden Pyramidion",new Vector3(0,height-.7f,0),1.08f,1.2f,gold);for(int i=0;i<5;i++){Box(t,"Vertical Glyph Relief",new Vector3(0,1.6f+i*1.4f,-.59f),new Vector3(.15f,.45f,.04f),teal);Box(t,"Glyph Crossbar",new Vector3(0,1.85f+i*1.4f,-.6f),new Vector3(.45f,.10f,.04f),teal);}}
        static void Sphinx(Transform t)
        {
            Box(t,"Sphinx Plinth",new Vector3(0,.2f,0),new Vector3(10,.4f,4.8f),sandLight);
            Ball(t,"Lion Body",new Vector3(-1,1.6f,.2f),new Vector3(6,2.8f,3),sand);
            foreach(float z in new[]{-1.1f,1.1f})Box(t,"Long Lion Paw",new Vector3(2.0f,.7f,z),new Vector3(4.0f,.95f,.95f),sandLight);
            var head=Ball(t,"Pharaoh Face",new Vector3(1.9f,3.5f,0),new Vector3(1.8f,2.1f,1.6f),sandLight);
            Box(t,"Nemes Headdress",new Vector3(1.75f,3.9f,.10f),new Vector3(2.8f,1.8f,1.95f),sand);
            for(int i=0;i<4;i++)Box(t,"Headdress Teal Stripe",new Vector3(1.75f,3.35f+i*.32f,-.90f),new Vector3(2.7f,.10f,.05f),teal);
            Box(t,"Nose",new Vector3(2.65f,3.55f,-.05f),new Vector3(.5f,.65f,.40f),sandLight);Box(t,"Royal Chin Beard",new Vector3(2.35f,2.6f,-.1f),new Vector3(.35f,1.0f,.35f),gold);
        }
        static void Urn(Transform t,Material mat,float scale=1)
        {var g=Lathe(t,"Amphora Vessel",Vector3.zero,new[]{0f,.12f,.65f,1.15f,1.45f,1.55f},new[]{.28f,.4f,.58f,.45f,.22f,.30f},mat,10);g.transform.localScale=Vector3.one*scale;foreach(float sign in new[]{-1f,1f})Arc(t,"Amphora Handle",new Vector3(sign*.39f,1.08f,0)*scale,.24f*scale,.07f*scale,.09f*scale,mat,8,sign>0?-90:90,sign>0?90:270);}
        static void Egypt(LoopThemeDefinition d)
        {
            var pyramid=Model("Desert Pyramid",PolishedPyramid);
            var temple=Model("Nile Pylon Temple",EgyptianTemple);
            var sphinx=Model("Sphinx Courtyard",t=>{Sphinx(t);Palm(t,new Vector3(-5,0,1.5f),6);});
            var oasis=Model("Palm Oasis Pavilion",t=>{Cylinder(t,"Oasis Stone Rim",Vector3.zero,4,.25f,sandLight,18);Cylinder(t,"Still Oasis Water",new Vector3(0,.28f,0),3.55f,.04f,water,18);Palm(t,new Vector3(-4,0,1),7);Palm(t,new Vector3(3,0,2.8f),5.5f);Box(t,"Pavilion Canopy",new Vector3(0,3.6f,2),new Vector3(5.0f,.20f,2.6f),cream);foreach(float x in new[]{-2.2f,2.2f})Cylinder(t,"Pavilion Column",new Vector3(x,0,2),.15f,3.5f,sandLight);});
            var market=Model("Desert Merchant Stall",t=>{foreach(float x in new[]{-3f,3f})foreach(float z in new[]{-1.4f,1.4f})Cylinder(t,"Awning Post",new Vector3(x,0,z),.09f,3.7f,wood,8);Box(t,"Ochre Fabric Roof",new Vector3(0,3.7f,0),new Vector3(6.6f,.15f,3.5f),red);Box(t,"Merchant Table",new Vector3(0,1.2f,0),new Vector3(5.8f,.18f,1.8f),wood);for(int i=0;i<4;i++){var root=new GameObject("Market Jar");root.transform.SetParent(t,false);root.transform.localPosition=new Vector3(-2+i*1.3f,1.3f,0);Urn(root.transform,i%2==0?sand:teal,.7f);}});
            d.landmarks=new[]{pyramid,temple,sphinx,oasis,market};
            var dune=Model("Wind Sculpted Dune",t=>{Ball(t,"Broad Sand Dune",new Vector3(0,-2.0f,0),new Vector3(28,9,16),sandLight);Ball(t,"Dune Shoulder",new Vector3(8,-2,2),new Vector3(19,7,13),sand);});
            d.background=new[]{Background(pyramid,"Distant Great Pyramid"),Background(temple,"Distant Temple"),Background(dune,"Distant Dunes")};
            var obelisk=Model("Hieroglyph Obelisk",t=>Obelisk(t,7));var pots=Model("Painted Amphora Cluster",t=>{Urn(t,red);var r=new GameObject("Small Teal Jar");r.transform.SetParent(t,false);r.transform.localPosition=new Vector3(.9f,0,.2f);Urn(r.transform,teal,.7f);});
            var chest=Model("Pharaoh Treasure Coffer",t=>{Box(t,"Coffer",new Vector3(0,.55f,0),new Vector3(1.7f,1.1f,.90f),teal);Box(t,"Gold Rim",new Vector3(0,1.13f,0),new Vector3(1.82f,.12f,1.02f),gold);foreach(float x in new[]{-.6f,.6f})Box(t,"Gold Band",new Vector3(x,.6f,-.47f),new Vector3(.12f,1,.06f),gold);});
            var stool=Model("Sandstone Seat",t=>{Box(t,"Stone Seat",new Vector3(0,.6f,0),new Vector3(2.5f,.23f,.9f),sandLight);foreach(float x in new[]{-.8f,.8f})Box(t,"Seat Leg",new Vector3(x,.25f,0),new Vector3(.3f,.5f,.7f),sand);});
            var tablet=Model("Carved Egyptian Stele",t=>{Box(t,"Tablet Base",new Vector3(0,.15f,0),new Vector3(1.7f,.3f,1),sand);Box(t,"Carved Tablet",new Vector3(0,1.4f,0),new Vector3(1.3f,2.4f,.3f),sandLight);for(int i=0;i<5;i++)Box(t,"Teal Inscription",new Vector3(0,.65f+i*.35f,-.17f),new Vector3(.78f,.10f,.035f),teal);});
            var palm=Model("Date Palm",t=>Palm(t,Vector3.zero,5));var reeds=Model("Papyrus Reeds",t=>{for(int i=0;i<5;i++){float x=(i-2)*.35f;Beam(t,"Papyrus Stem",new Vector3(x,0,0),new Vector3(x,1.6f+(i%2)*.4f,.1f),.055f,green);Ball(t,"Papyrus Crown",new Vector3(x,1.7f+(i%2)*.4f,.1f),new Vector3(.55f,.25f,.45f),leaf);}});
            var torch=Model("Bronze Temple Brazier",t=>{Cylinder(t,"Brazier Base",Vector3.zero,.34f,.18f,sandLight);Cylinder(t,"Bronze Stem",Vector3.up*.18f,.10f,2.8f,wood);Lathe(t,"Brazier Bowl",Vector3.up*2.9f,new[]{0f,.35f},new[]{.20f,.48f},gold,10);Pyramid(t,"Stylized Flame",Vector3.up*3.25f,.55f,.85f,gold);});
            d.props=new[]{pots,chest,stool,tablet,obelisk};d.vegetation=new[]{palm,reeds};d.lamps=new[]{torch};d.accent=new[]{obelisk,oasis};
        }
        static void Column(Transform t,Vector3 p,float height)
        {Box(t,"Column Plinth",p+Vector3.up*.18f,new Vector3(1.25f,.36f,1.25f),stone);Cylinder(t,"Fluted Column Shaft",p+Vector3.up*.35f,.36f,height-.7f,stone,12);Box(t,"Column Capital",p+Vector3.up*(height-.20f),new Vector3(1.25f,.4f,1.1f),cream);}
        static void ClassicalTemple(Transform t)
        {
            for(int i=0;i<3;i++)Box(t,"Temple Steps",new Vector3(0,.16f+i*.22f,0),new Vector3(15-i*.7f,.32f,7.5f-i*.4f),stone);
            foreach(float x in new[]{-5.3f,-3.2f,-1.1f,1.1f,3.2f,5.3f})Column(t,new Vector3(x,.7f,-2.0f),5.7f);
            foreach(float x in new[]{-5.3f,5.3f})Column(t,new Vector3(x,.7f,2.0f),5.7f);
            Box(t,"Temple Back Sanctuary",new Vector3(0,3.1f,2.5f),new Vector3(12,4.8f,.6f),stoneDark);
            Box(t,"Classical Entablature",new Vector3(0,6.5f,0),new Vector3(13.5f,.65f,6.3f),cream);
            MeshObject(t,"Triangular Pediment",new List<Vector3>{new Vector3(-7,6.8f,-3.3f),new Vector3(7,6.8f,-3.3f),new Vector3(0,9.1f,-3.3f),new Vector3(-7,6.8f,3.3f),new Vector3(7,6.8f,3.3f),new Vector3(0,9.1f,3.3f)},new List<int>{0,2,1,3,4,5,0,3,5,0,5,2,2,5,4,2,4,1},red);
            Box(t,"Pediment Lower Band",new Vector3(0,6.91f,-3.38f),new Vector3(14,.20f,.18f),cream);
            Cylinder(t,"Pediment Medallion",new Vector3(0,7.65f,-3.38f),.50f,.10f,gold,12).transform.localRotation=Quaternion.Euler(90,0,0);
        }
        static void Dome(Transform t,Vector3 p,float radius,Material mat)
        {var heights=new float[8];var radii=new float[8];for(int i=0;i<8;i++){float angle=i*Mathf.PI*.5f/7;heights[i]=Mathf.Sin(angle)*radius;radii[i]=Mathf.Cos(angle)*radius;}Lathe(t,"Faceted Byzantine Dome",p,heights,radii,mat,20);}
        static void Basilica(Transform t)
        {
            Box(t,"Basilica Marble Base",new Vector3(0,.2f,0),new Vector3(15,.4f,8),stone);Box(t,"Central Nave",new Vector3(0,3.3f,.4f),new Vector3(8,6.2f,6),cream);
            foreach(float x in new[]{-5.6f,5.6f}){Box(t,"Basilica Side Aisle",new Vector3(x,2.3f,.4f),new Vector3(3.2f,4.2f,6),stone);Dome(t,new Vector3(x,4.4f,.4f),1.9f,teal);}
            Cylinder(t,"Windowed Dome Drum",new Vector3(0,6.2f,.4f),3.6f,1.4f,stone,16);Dome(t,new Vector3(0,7.6f,.4f),3.8f,teal);Beam(t,"Dome Finial",new Vector3(0,11.2f,.4f),new Vector3(0,12.5f,.4f),.12f,gold);Beam(t,"Finial Crossarm",new Vector3(-.45f,12,.4f),new Vector3(.45f,12,.4f),.12f,gold);
            for(int i=0;i<5;i++){float x=(i-2)*2.4f;Box(t,"Recessed Arched Window",new Vector3(x,3.6f,-2.64f),new Vector3(.8f,1.35f,.06f),blue);Arc(t,"Pale Window Arch",new Vector3(x,4.25f,-2.72f),.4f,.13f,.09f,cream,8);}
            Box(t,"Grand Door",new Vector3(0,1.5f,-2.7f),new Vector3(2.3f,2.5f,.10f),woodDark);Arc(t,"Entrance Arch",new Vector3(0,2.6f,-2.8f),1.16f,.35f,.25f,stone,12);
            foreach(float x in new[]{-3.0f,3.0f}){Column(t,new Vector3(x,0,-3.4f),4.9f);}
            Box(t,"Mosaic Frieze",new Vector3(0,5.35f,-2.66f),new Vector3(12,.45f,.08f),gold);for(int i=0;i<9;i++){var tile=Box(t,"Teal Mosaic Diamond",new Vector3(-5+i*1.25f,5.35f,-2.72f),new Vector3(.28f,.28f,.04f),teal);tile.transform.localRotation=Quaternion.Euler(0,0,45);}
        }
        static void Statue(Transform t)
        {
            Box(t,"Sculpture Pedestal",new Vector3(0,.6f,0),new Vector3(2.2f,1.2f,2.0f),stoneDark);Box(t,"Pedestal Cornice",new Vector3(0,1.28f,0),new Vector3(2.5f,.22f,2.2f),cream);
            Lathe(t,"Draped Marble Robe",new Vector3(0,1.4f,0),new[]{0f,1.6f,2.4f},new[]{.7f,.45f,.65f},stone,10);Ball(t,"Marble Head",new Vector3(0,4.25f,0),new Vector3(.75f,.92f,.70f),cream);Ball(t,"Curled Hair",new Vector3(0,4.55f,.08f),new Vector3(.85f,.6f,.76f),stoneDark);
            Beam(t,"Raised Arm",new Vector3(-.5f,3.5f,0),new Vector3(-1.2f,4.9f,-.1f),.26f,cream);Beam(t,"Resting Arm",new Vector3(.5f,3.5f,0),new Vector3(.75f,2.9f,-.4f),.28f,cream);Cylinder(t,"Ceremonial Staff",new Vector3(-1.25f,1.4f,-.1f),.055f,4.0f,gold,8);
        }
        static void Rome(LoopThemeDefinition d)
        {
            var temple=Model("Marble Forum Temple",ClassicalTemple);var church=Model("Turquoise Dome Basilica",Basilica);
            var aqueduct=Model("Aqueduct Arcade",t=>{for(int i=0;i<4;i++){float x=-7.5f+i*5;Box(t,"Aqueduct Pier",new Vector3(x,2.5f,0),new Vector3(1.1f,5,2),stone);if(i<3)Arc(t,"Roman Arch",new Vector3(x+2.5f,4.2f,0),1.95f,.55f,1.6f,cream,12);}Box(t,"Aqueduct Water Channel",new Vector3(0,7,0),new Vector3(17,1.2f,2.3f),stone);Box(t,"Aqueduct Cornice",new Vector3(0,7.7f,0),new Vector3(17.4f,.25f,2.65f),cream);});
            var triumph=Model("Triumphal Arch",t=>{foreach(float x in new[]{-3.6f,3.6f})Box(t,"Monument Pier",new Vector3(x,3.4f,0),new Vector3(2.2f,6.8f,3.6f),stone);Arc(t,"Great Vault",new Vector3(0,4.5f,0),2.5f,.9f,3.4f,cream,16);Box(t,"Inscribed Attic",new Vector3(0,8.3f,0),new Vector3(10,2,4),stone);for(int i=0;i<5;i++)Box(t,"Attic Inscription",new Vector3(-2.4f+i*1.2f,8.4f,-2.05f),new Vector3(.65f,.10f,.06f),gold);Flag(t,new Vector3(0,9.4f,0),red,3);});
            var plaza=Model("Statue and Mosaic Plaza",t=>{Box(t,"Mosaic Plaza Base",new Vector3(0,.12f,0),new Vector3(13,.24f,7),stone);Statue(t);Tree(t,new Vector3(-4,0,1),5,true);Tree(t,new Vector3(4,0,1),4.5f,true);for(int i=0;i<7;i++)Box(t,"Mosaic Path Tile",new Vector3(-4.5f+i*1.5f,.26f,-2),new Vector3(.8f,.035f,.8f),i%2==0?teal:red);});
            d.landmarks=new[]{temple,plaza,church,aqueduct,triumph};d.background=new[]{Background(church,"Distant Domed City"),Background(aqueduct,"Distant Aqueduct"),Background(temple,"Distant Forum"),Background(triumph,"Distant Triumphal Gate"),Background(plaza,"Distant Mosaic Courtyard")};
            var statue=Model("Marble Emperor Statue",Statue);var urn=Model("Roman Amphora",t=>Urn(t,red));var cypress=Model("Cypress Tree",t=>Tree(t,Vector3.zero,5.7f,true));var olive=Model("Olive Tree",t=>Tree(t,Vector3.zero,4.5f));
            var bench=Model("Carved Marble Bench",t=>{Box(t,"Seat Slab",new Vector3(0,.65f,0),new Vector3(3,.25f,1),cream);foreach(float x in new[]{-.9f,.9f})Box(t,"Decorative Bench Foot",new Vector3(x,.3f,0),new Vector3(.45f,.6f,.8f),stone);});
            var mosaic=Model("Framed Mosaic Panel",t=>{Box(t,"Mosaic Stone Frame",new Vector3(0,1.4f,0),new Vector3(2,2.8f,.35f),stone);Box(t,"Ochre Mosaic Ground",new Vector3(0,1.4f,-.2f),new Vector3(1.65f,2.4f,.06f),gold);for(int y=0;y<5;y++)for(int x=0;x<3;x++){var tile=Box(t,"Broad Mosaic Tile",new Vector3((x-1)*.45f,.5f+y*.45f,-.245f),new Vector3(.3f,.3f,.03f),(x+y)%2==0?teal:red);tile.transform.localRotation=Quaternion.Euler(0,0,45);}});
            var banner=Model("Roman Red Standard",t=>{Cylinder(t,"Standard Plinth",Vector3.zero,.35f,.25f,stone);Flag(t,Vector3.up*.25f,red,4);});
            var lantern=Model("Bronze Forum Lantern",t=>{Cylinder(t,"Lantern Pedestal",Vector3.zero,.30f,.2f,stone);Cylinder(t,"Bronze Post",Vector3.up*.2f,.10f,3.3f,woodDark);Box(t,"Lantern Ivory Light",new Vector3(0,3.8f,0),new Vector3(.60f,.75f,.60f),cream);foreach(float x in new[]{-.32f,.32f})foreach(float z in new[]{-.32f,.32f})Beam(t,"Lantern Frame",new Vector3(x,3.4f,z),new Vector3(x,4.2f,z),.055f,gold);Pyramid(t,"Lantern Roof",new Vector3(0,4.2f,0),1,.45f,teal);});
            d.props=new[]{urn,bench,mosaic,banner,statue};d.vegetation=new[]{cypress,olive};d.lamps=new[]{lantern};d.accent=new[]{plaza,aqueduct};AddRomanDistricts(d);
        }
    }
}




