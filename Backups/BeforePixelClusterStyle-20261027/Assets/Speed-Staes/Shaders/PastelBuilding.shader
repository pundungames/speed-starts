Shader "Speed Stars/Pastel Building"
{
 Properties
 {
  _FrontColor("Sunlit Facade",Color)=(0.66,0.82,0.96,1)
  _SideColor("Shaded Facade",Color)=(0.36,0.60,0.85,1)
  _WindowColor("Painted Windows",Color)=(0.42,0.66,0.87,1)
  _BuildingSize("Building Dimensions",Vector)=(4,15,6,0)
  _Pattern("Pattern: Columns, Floors, Style, Seed",Vector)=(3,12,0,1)
 }
 SubShader
 {
  Tags {"RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _FrontColor,_SideColor,_WindowColor,_BuildingSize,_Pattern;
   struct v2f {float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;};
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz+0.5;o.normal=v.normal;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   fixed4 frag(v2f i):SV_Target
   {
    float3 n=normalize(i.normal);
    bool front=abs(n.x)>0.5;
    bool roof=n.y>0.5;
    float2 uv=float2(front?i.local.z:i.local.x,i.local.y);
    // Deliberately flat, face-dependent paint, independent of scene exposure.
    float3 color=front?_FrontColor.rgb:_SideColor.rgb;
    if(roof) return float4(lerp(_FrontColor.rgb,1,0.12),1);
    if(_Pattern.w<0) return float4(color,1);
    float columns=max(1,_Pattern.x),floors=max(1,_Pattern.y);
    float2 grid=float2(uv.x*columns,(uv.y-0.10)*floors/0.83);
    float2 cell=floor(grid),f=frac(grid);
    float style=_Pattern.z;
    if(abs(style-3)<0.1) return float4(color,1);
    bool corner=abs(style-4)<0.1;
    float width=style>1.5?0.87:(style>0.5?0.72:0.62);
    float h=style>1.5?0.48:0.40;
    if(corner){width=0.73;h=0.53;}
    if(abs(style-5)<0.1){width=0.40;h=0.73;}
    if(abs(style-6)<0.1){width=0.94;h=0.34;}
    float window=step((1-width)*0.5,f.x)*step(f.x,(1+width)*0.5);
    window*=step(0.22,f.y)*step(f.y,0.22+h)*step(0.105,uv.y)*step(uv.y,0.91);
    float randomness=hash(cell+_Pattern.w*11);
    float variation=(randomness-0.5)*0.035;
    float3 glass=_WindowColor.rgb+variation;
    if(style>6.5) window*=lerp(0.50,1,step(0.35,randomness));
    if(!front){glass=lerp(_SideColor.rgb,_WindowColor.rgb,corner?0.92:0.26);window*=corner?1:0.12;}
    color=lerp(color,glass,window);
    // Tiny painterly offsets, without outlines, reflections or black recesses.
    float grain=hash(floor(uv*float2(140,220))+_Pattern.w);
    color+=(grain-0.5)*0.006;
    return float4(color,1);
   }
   ENDCG
  }
 }
}
