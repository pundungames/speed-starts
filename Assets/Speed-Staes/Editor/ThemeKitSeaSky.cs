using System;
using System.Collections.Generic;
using UnityEngine;
using SpeedStars.Themes;
namespace SpeedStars.Editor
{
    public static partial class ThemeKitBuilder
    {
        static void Chest(Transform t)
        {
            Box(t,"Treasure Chest Body",new Vector3(0,.48f,0),new Vector3(1.7f,.96f,.95f),wood);var lid=Lathe(t,"Rounded Chest Lid",Vector3.zero,new[]{0f,.15f,.4f,.55f},new[]{.68f,.83f,.68f,.06f},wood,8);lid.transform.localPosition=new Vector3(0,.9f,0);lid.transform.localScale=new Vector3(1.1f,.8f,.70f);
            foreach(float x in new[]{-.64f,.64f}){Box(t,"Chest Gold Binding",new Vector3(x,.55f,-.49f),new Vector3(.14f,.95f,.06f),gold);Box(t,"Lid Gold Binding",new Vector3(x,1.04f,-.43f),new Vector3(.14f,.32f,.10f),gold);}Box(t,"Gold Lock",new Vector3(0,.75f,-.53f),new Vector3(.25f,.32f,.10f),gold);
        }
        static void Ship(Transform t)
        {
            var v=new List<Vector3>();var tr=new List<int>();float[] xs={-10,-7,-3,3,7,10},width={.3f,2,2.6f,2.5f,1.7f,.12f};
            for(int i=0;i<xs.Length;i++)v.AddRange(new[]{new Vector3(xs[i],2.6f,-width[i]),new Vector3(xs[i],2.6f,width[i]),new Vector3(xs[i],.25f,width[i]*.5f),new Vector3(xs[i],-.35f,0),new Vector3(xs[i],.25f,-width[i]*.5f)});
            for(int i=0;i<xs.Length-1;i++)for(int j=0;j<5;j++){int a=i*5+j,b=i*5+(j+1)%5;tr.AddRange(new[]{a,a+5,b,b,a+5,b+5});}
            MeshObject(t,"Faceted Pirate Hull",v,tr,wood);Box(t,"Main Timber Deck",new Vector3(-.3f,2.55f,0),new Vector3(15.7f,.20f,4.2f),woodDark);Box(t,"Raised Stern Cabin",new Vector3(-6.7f,3.1f,0),new Vector3(4.0f,1.3f,4.2f),wood);Box(t,"Stern Quarter Deck",new Vector3(-6.7f,3.85f,0),new Vector3(4.5f,.18f,4.6f),gold);
            foreach(float z in new[]{-2.2f,2.2f})for(int i=0;i<10;i++)Beam(t,"Ship Rail Post",new Vector3(-7.5f+i*1.6f,2.6f,z),new Vector3(-7.5f+i*1.6f,3.25f,z),.075f,woodDark);
            foreach(float z in new[]{-2.2f,2.2f})Beam(t,"Timber Gunwale",new Vector3(-7.8f,3.25f,z),new Vector3(7,3.25f,z),.12f,gold);
            foreach(float x in new[]{-3.8f,-.6f,2.6f})foreach(float z in new[]{-2.2f,2.2f}){var cannon=Cylinder(t,"Black Cannon",new Vector3(x,2.9f,z),.19f,1.1f,dark,10);cannon.transform.localRotation=Quaternion.Euler(z<0?90:-90,0,0);Box(t,"Cannon Gun Port",new Vector3(x,1.9f,z*.97f),new Vector3(.7f,.6f,.10f),dark);}
            for(int mast=0;mast<3;mast++)
            {
                float x=-5.6f+mast*5.4f,height=mast==1?15.0f:12.5f;Cylinder(t,"Tall Pirate Mast",new Vector3(x,2.7f,0),.13f,height-2.7f,woodDark,10);Beam(t,"Sail Yard",new Vector3(x-3.1f,height-1.5f,0),new Vector3(x+3.1f,height-1.5f,0),.10f,wood);
                var sailVerts=new List<Vector3>{new Vector3(x-3,height-1.6f,0),new Vector3(x+3,height-1.6f,0),new Vector3(x+2.4f,height-6.2f,.1f),new Vector3(x,height-6.6f,-.9f),new Vector3(x-2.6f,height-6.0f,.1f),new Vector3(x,height-3.7f,-1.05f)};
                MeshObject(t,"Billowing Ivory Sail",sailVerts,new List<int>{0,5,1,1,5,2,2,5,3,3,5,4,4,5,0,1,5,0,2,5,1,3,5,2,4,5,3,0,5,4},cream);
                Tube(t,"Standing Rigging",new List<Vector3>{new Vector3(x-2.7f,2.8f,-2.1f),new Vector3(x,height-.5f,0),new Vector3(x+2.7f,2.8f,2.1f)},.025f,woodDark,5,.025f);
                Flag(t,new Vector3(x,height,0),mast==1?dark:red,1.5f);
            }
            Beam(t,"Bowsprit",new Vector3(7.5f,3.1f,0),new Vector3(12.0f,5.0f,0),.15f,woodDark);
            foreach(float x in new[]{-7.6f,-6.6f,-5.6f})Box(t,"Stern Cabin Window",new Vector3(x,3.15f,-2.15f),new Vector3(.60f,.55f,.05f),gold);
        }
        static void Octopus(Transform t)
        {
            Ball(t,"Giant Octopus Mantle",new Vector3(0,4.4f,0),new Vector3(6.8f,7.8f,6.0f),purple);Ball(t,"Mantle Highlight",new Vector3(-.9f,5.6f,-.9f),new Vector3(4.7f,5.1f,3.8f),Paint("Octopus Sunlit",new Color(.70f,.43f,.60f)));
            foreach(float x in new[]{-1.7f,1.7f}){Ball(t,"Large Octopus Eye",new Vector3(x,4.0f,-2.55f),new Vector3(1.25f,1.45f,.65f),cream);Ball(t,"Curious Pupil",new Vector3(x+.15f,4.05f,-2.93f),new Vector3(.45f,.65f,.18f),dark);}
            for(int tentacle=0;tentacle<8;tentacle++)
            {
                float angle=tentacle*Mathf.PI/4;var points=new List<Vector3>();
                for(int j=0;j<17;j++){float u=j/16f,reach=2+u*9;float theta=angle+u*u*.85f;float y=1.2f+Mathf.Sin(u*Mathf.PI)*3.0f+Mathf.Sin(u*Mathf.PI*2)*.6f;points.Add(new Vector3(Mathf.Cos(theta)*reach,y,Mathf.Sin(theta)*reach));}
                Tube(t,"Curling Sea Tentacle",points,.95f,purple,9,.12f);
                if(tentacle>=4)for(int j=3;j<14;j+=2)Ball(t,"Broad Suction Cup",points[j]+new Vector3(0,-.35f,-.12f),new Vector3(.37f,.14f,.32f),cream);
            }
        }
        static void Island(Transform t,bool floating)
        {
            if(floating)Lathe(t,"Tapered Floating Rock",new Vector3(0,-7,0),new[]{0f,2f,5.8f,7f},new[]{.5f,3,6.4f,7.0f},stoneDark,10);
            else Lathe(t,"Tropical Rocky Island",new Vector3(0,-.5f,0),new[]{0f,.8f,1.2f},new[]{7.8f,7.5f,6.2f},sandLight,16);
            Cylinder(t,"Island Grass Rim",new Vector3(0,floating?0:.8f,0),6.5f,.30f,green,14);Cylinder(t,"Island Sandy Patch",new Vector3(-1,floating?.32f:1.13f,-1),3.5f,.035f,sandLight,12);
            Palm(t,new Vector3(-3,floating?.30f:1.1f,1.8f),6);Palm(t,new Vector3(3.3f,floating?.30f:1.1f,2.3f),4.8f);
        }
        static void Lighthouse(Transform t)
        {
            Cylinder(t,"Lighthouse Rock Pad",Vector3.zero,3.6f,.7f,stoneDark,12);
            for(int i=0;i<6;i++)Lathe(t,"Striped Lighthouse Course",new Vector3(0,.7f+i*1.9f,0),new[]{0f,1.9f},new[]{2.0f-i*.16f,1.84f-i*.16f},i%2==0?cream:red,14);
            Cylinder(t,"Lantern Gallery",new Vector3(0,12.2f,0),1.70f,.30f,woodDark,14);Cylinder(t,"Ivory Lantern Light",new Vector3(0,12.5f,0),1.14f,1.7f,cream,12);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Beam(t,"Lantern Housing Frame",new Vector3(Mathf.Cos(a)*1.25f,12.5f,Mathf.Sin(a)*1.25f),new Vector3(Mathf.Cos(a)*1.25f,14.2f,Mathf.Sin(a)*1.25f),.08f,dark);}Lathe(t,"Lighthouse Roof",new Vector3(0,14.2f,0),new[]{0f,1.25f},new[]{1.8f,.05f},red,14);
            Box(t,"Lighthouse Door",new Vector3(0,1.7f,-2.04f),new Vector3(.95f,1.8f,.08f),woodDark);
        }
        static void Sea(LoopThemeDefinition d)
        {
            d.pathMaterial=wood;
            string oceanPath=themeFolder+"/Materials/Painted Ocean.mat";
            var ocean=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(oceanPath);
            if(!ocean){ocean=new Material(Shader.Find("Speed Stars/Painted Canal"));UnityEditor.AssetDatabase.CreateAsset(ocean,oceanPath);}
            ocean.SetColor("_Color",new Color(.12f,.50f,.72f));d.waterMaterial=ocean;UnityEditor.EditorUtility.SetDirty(ocean);
            var ship=Model("Three Mast Pirate Ship",Ship);var octopus=Model("Giant Friendly Sea Octopus",Octopus);var island=Model("Tropical Palm Island",t=>Island(t,false));var lighthouse=Model("Red White Lighthouse",Lighthouse);
            var dock=Model("Harbor Jetty and Storehouse",t=>{for(int i=0;i<12;i++)Box(t,"Pier Plank",new Vector3(-6+i*1.05f,.6f,0),new Vector3(1,.23f,4.5f),i%2==0?wood:woodDark);foreach(float x in new[]{-5f,0,5f})foreach(float z in new[]{-2f,2f})Cylinder(t,"Dock Pile",new Vector3(x,-1,z),.18f,2.8f,woodDark,10);Box(t,"Harbor Storehouse",new Vector3(-1,2.0f,1.0f),new Vector3(6,2.5f,3.3f),wood);Pyramid(t,"Storehouse Roof",new Vector3(-1,3.25f,1.0f),6.8f,1.7f,red);Box(t,"Storehouse Door",new Vector3(-1,1.6f,-.7f),new Vector3(1.6f,2,.06f),woodDark);});
            var arch=Model("Sea Rock Arch",t=>{foreach(float x in new[]{-3.7f,3.7f})Lathe(t,"Sea Stack",new Vector3(x,-.5f,0),new[]{0f,5f,7f},new[]{2.8f,2.1f,1.9f},stoneDark,7);Arc(t,"Natural Sea Arch",new Vector3(0,5.5f,0),2.3f,1.8f,3.6f,stone,12);});
            d.landmarks=new[]{dock,island,lighthouse,arch};d.background=new[]{Background(ship,"Distant Pirate Fleet"),Background(island,"Distant Tropical Island"),Background(lighthouse,"Distant Beacon")};d.backgroundHeight=-.1f;
            var chest=Model("Pirate Treasure Chest",Chest);
            var barrels=Model("Rum Barrel Cluster",t=>{Lathe(t,"Bound Rum Barrel",Vector3.zero,new[]{0f,.2f,.8f,1.4f,1.6f},new[]{.47f,.55f,.66f,.55f,.47f},wood,12);foreach(float y in new[]{.25f,1.3f})Lathe(t,"Iron Barrel Hoop",new Vector3(0,y,0),new[]{0f,.10f},new[]{.59f,.59f},dark,12);});
            var anchor=Model("Old Ship Anchor",t=>{Beam(t,"Anchor Shank",new Vector3(0,.3f,0),new Vector3(0,2.8f,0),.13f,dark);Beam(t,"Anchor Stock",new Vector3(-.75f,2.1f,0),new Vector3(.75f,2.1f,0),.12f,dark);Arc(t,"Curved Anchor Arms",new Vector3(0,1.2f,0),1.0f,.15f,.15f,dark,14,185,355);foreach(float sign in new[]{-1f,1f})Pyramid(t,"Anchor Fluke",new Vector3(sign*.9f,1.0f,0),.40f,.55f,dark);});
            var crate=Model("Roped Cargo Crate",t=>{Box(t,"Wooden Cargo Crate",new Vector3(0,.65f,0),new Vector3(1.5f,1.3f,1.2f),wood);foreach(float x in new[]{-.55f,.55f})Box(t,"Rope Binding",new Vector3(x,.7f,-.62f),new Vector3(.09f,1.3f,.06f),cream);Beam(t,"Crate Diagonal Brace",new Vector3(-.6f,.14f,-.65f),new Vector3(.6f,1.2f,-.65f),.1f,woodDark);});
            var wheel=Model("Captain Steering Wheel",t=>{Cylinder(t,"Wheel Pedestal",Vector3.zero,.23f,1.5f,woodDark);Arc(t,"Wood Wheel Ring",new Vector3(0,2.0f,0),.65f,.12f,.13f,wood,20,0,360);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Beam(t,"Ship Wheel Spoke",new Vector3(0,2,0),new Vector3(Mathf.Cos(a)*.9f,2+Mathf.Sin(a)*.9f,0),.08f,wood);} });
            var palm=Model("Shore Palm",t=>Palm(t,Vector3.zero,4.8f));var rocks=Model("Shell and Sea Grass Cluster",t=>{for(int i=0;i<4;i++)Ball(t,"Smooth Shore Rock",new Vector3((i-1.5f)*.45f,.25f,(i%2)*.3f),new Vector3(.8f,.55f,.7f),stone);for(int i=0;i<5;i++)Beam(t,"Sea Grass Blade",new Vector3(.4f,0,.3f),new Vector3(.1f+i*.14f,1.1f+(i%2)*.4f,.3f),.06f,green);});
            var lamp=Model("Dock Rope Lantern",t=>{Cylinder(t,"Dock Lantern Post",Vector3.zero,.14f,3.8f,woodDark,10);Beam(t,"Lantern Cross Arm",new Vector3(0,3.6f,0),new Vector3(1.0f,3.6f,0),.12f,wood);Beam(t,"Hanging Chain",new Vector3(.9f,3.6f,0),new Vector3(.9f,3.1f,0),.035f,dark);Box(t,"Lantern Warm Glass",new Vector3(.9f,2.8f,0),new Vector3(.50f,.65f,.50f),cream);Pyramid(t,"Lantern Hood",new Vector3(.9f,3.13f,0),.75f,.30f,dark);});
            d.props=new[]{chest,barrels,anchor,crate,wheel};d.vegetation=new[]{palm,rocks};d.lamps=new[]{lamp};d.accent=new[]{octopus,ship,island};
        }
        static void Castle(Transform t)
        {
            Island(t,true);Box(t,"Castle Keep",new Vector3(0,4,0),new Vector3(6,7.4f,4.8f),cream);Box(t,"Keep Top Cornice",new Vector3(0,7.7f,0),new Vector3(6.4f,.35f,5.2f),stone);
            foreach(float x in new[]{-4.3f,4.3f})foreach(float z in new[]{-2.7f,2.7f})
            {Cylinder(t,"Round Castle Tower",new Vector3(x,.3f,z),1.20f,7.3f,stone,12);Lathe(t,"Blue Turret Roof",new Vector3(x,7.6f,z),new[]{0f,3.0f},new[]{1.55f,.04f},blue,12);Flag(t,new Vector3(x,10.5f,z),red,1.6f);}
            for(int i=0;i<5;i++)Box(t,"Keep Battlement",new Vector3(-2.7f+i*1.35f,8.2f,-2.5f),new Vector3(.7f,.9f,.65f),cream);
            Box(t,"Castle Gate",new Vector3(0,1.8f,-2.48f),new Vector3(1.8f,2.8f,.08f),woodDark);Arc(t,"Castle Gate Arch",new Vector3(0,3.1f,-2.55f),.90f,.28f,.15f,gold,12);
            foreach(float x in new[]{-1.6f,0,1.6f})foreach(float y in new[]{4.4f,6.3f})Box(t,"Blue Keep Window",new Vector3(x,y,-2.44f),new Vector3(.55f,.9f,.08f),blue);
        }
        static void Rainbow(Transform t)
        {
            Color[] c={new Color(.91f,.37f,.38f),new Color(.96f,.62f,.29f),new Color(.96f,.83f,.41f),new Color(.40f,.73f,.47f),new Color(.29f,.64f,.85f),new Color(.45f,.46f,.76f),new Color(.70f,.46f,.74f)};
            for(int i=0;i<7;i++)Arc(t,"Rainbow Ribbon",new Vector3(0,3.0f,0),7.5f+i*.38f,.40f,.32f,Paint("Rainbow "+i,c[i],.8f),28);

        }
        static void Bird(Transform t)
        {Ball(t,"Flying Gull Body",Vector3.zero,new Vector3(.36f,.38f,1),cream);MeshObject(t,"Outstretched Bird Wings",new List<Vector3>{Vector3.zero,new Vector3(-2.1f,.7f,.1f),new Vector3(-1.4f,.85f,.65f),new Vector3(0,.1f,.45f),new Vector3(2.1f,.7f,.1f),new Vector3(1.4f,.85f,.65f)},new List<int>{0,1,2,0,2,3,0,3,5,0,5,4,2,1,0,3,2,0,5,3,0,4,5,0},cream);Pyramid(t,"Golden Beak",new Vector3(0,0,-.45f),.2f,.35f,gold).transform.localRotation=Quaternion.Euler(-90,0,0);}
        static void Sky(LoopThemeDefinition d)
        {
            d.pathMaterial=Paint("Cloudside Paving",new Color(.80f,.82f,.94f),.65f);
            var castle=Model("Floating Cloud Castle",Castle);
            var garden=Model("Floating Treasure Garden",t=>{Island(t,true);Tree(t,new Vector3(-2,0,1),4);var r=new GameObject("Garden Treasure");r.transform.SetParent(t,false);r.transform.localPosition=new Vector3(2,.4f,-1);Chest(r.transform);Arc(t,"Garden Gold Arch",new Vector3(0,.3f,1.5f),2,.20f,.25f,gold,20);});
            var rainbow=Model("Rainbow Between Clouds",Rainbow);
            var tower=Model("Sky Windmill Island",t=>{Island(t,true);Cylinder(t,"Windmill Tower",new Vector3(0,.3f,0),2.0f,6.5f,cream,10);Lathe(t,"Windmill Blue Roof",new Vector3(0,6.8f,0),new[]{0f,2.5f},new[]{2.6f,.03f},blue,12);for(int i=0;i<4;i++){float a=i*90+35;Vector3 end=new Vector3(Mathf.Cos(a*Mathf.Deg2Rad)*4,4.8f+Mathf.Sin(a*Mathf.Deg2Rad)*4,-2.2f);Beam(t,"Windmill Timber Spar",new Vector3(0,4.8f,-2.2f),end,.12f,wood);var sail=Box(t,"Windmill Cloth Blade",Vector3.Lerp(new Vector3(0,4.8f,-2.2f),end,.7f),new Vector3(.9f,2.0f,.08f),cream);sail.transform.localRotation=Quaternion.Euler(0,0,a-90);}});
            var balloon=Model("Pastel Explorer Balloon",t=>{Box(t,"Woven Balloon Basket",new Vector3(0,1.0f,0),new Vector3(2,1.3f,1.6f),wood);foreach(float x in new[]{-.9f,.9f})foreach(float z in new[]{-.7f,.7f})Beam(t,"Balloon Basket Rope",new Vector3(x,1.6f,z),new Vector3(x*1.8f,5,z*1.8f),.045f,cream);Lathe(t,"Faceted Balloon Envelope",new Vector3(0,4,0),new[]{0f,1f,3.5f,5.5f,6.7f},new[]{.7f,2.4f,3.1f,2.2f,.05f},red,16);for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Tube(t,"Balloon Pale Gore",new List<Vector3>{new Vector3(Mathf.Cos(a)*.75f,4,Mathf.Sin(a)*.75f),new Vector3(Mathf.Cos(a)*2.45f,5,Mathf.Sin(a)*2.45f),new Vector3(Mathf.Cos(a)*3.13f,7.5f,Mathf.Sin(a)*3.13f),new Vector3(Mathf.Cos(a)*2.25f,9.5f,Mathf.Sin(a)*2.25f)},.08f,cream,6,.05f);}});
            d.landmarks=new[]{garden,castle,tower,balloon};d.background=new[]{Background(castle,"Distant Floating Castle"),Background(garden,"Distant Sky Island"),Background(tower,"Distant Windmill Island")};d.backgroundHeight=2;
            var chest=Model("Sky Treasure Chest",Chest);var bird=Model("White Cloud Gull",t=>Bird(t));
            var crystal=Model("Floating Crystal Cluster",t=>{for(int i=0;i<3;i++){var r=Lathe(t,"Prismatic Crystal",new Vector3((i-1)*.5f,.1f,0),new[]{0f,1.5f+(i%2),2f+(i%2)},new[]{.30f,.30f,.01f},i==1?gold:blue,6);r.transform.localRotation=Quaternion.Euler(0,0,(i-1)*12);}});
            var star=Model("Golden Star Pedestal",t=>{Cylinder(t,"Star Plinth",Vector3.zero,.45f,.18f,stone);Cylinder(t,"Star Stem",Vector3.up*.18f,.07f,1.5f,gold);var v=new List<Vector3>{new Vector3(0,2,0)};for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad,r=i%2==0?.75f:.32f;v.Add(new Vector3(Mathf.Cos(a)*r,2+Mathf.Sin(a)*r,0));}var tr=new List<int>();for(int i=0;i<10;i++)tr.AddRange(new[]{0,i+1,(i+1)%10+1,0,(i+1)%10+1,i+1});MeshObject(t,"Golden Star",v,tr,gold);});

            var smallIsland=Model("Small Floating Meadow",t=>{Lathe(t,"Meadow Rock",new Vector3(0,-1.7f,0),new[]{0f,1.7f},new[]{.15f,1.7f},stoneDark,8);Cylinder(t,"Meadow Grass",Vector3.zero,1.75f,.18f,green,10);Tree(t,new Vector3(0,.2f,0),2.4f);});
            var flowers=Model("Sky Flower Tuft",t=>{for(int i=0;i<5;i++){float x=(i-2)*.32f;Beam(t,"Flower Stem",new Vector3(x,0,0),new Vector3(x,1.1f+(i%2)*.3f,0),.035f,green);Ball(t,"Broad Flower Petals",new Vector3(x,1.15f+(i%2)*.3f,0),Vector3.one*.38f,i%2==0?gold:red);}});
            var lamp=Model("Cloud Orb Lantern",t=>{Cylinder(t,"Lantern Base",Vector3.zero,.35f,.20f,stone);Beam(t,"Curved Orb Stem",new Vector3(0,.2f,0),new Vector3(0,3.3f,0),.10f,teal);Arc(t,"Ornate Orb Arch",new Vector3(.65f,3.3f,0),.65f,.09f,.10f,gold,14);Ball(t,"Ivory Floating Orb",new Vector3(1.3f,3.0f,0),Vector3.one*.70f,cream);});
            d.props=new[]{chest,bird,crystal,star};d.vegetation=new[]{smallIsland,flowers};d.lamps=new[]{lamp};d.accent=new[]{rainbow,balloon};
        }
    }
}


