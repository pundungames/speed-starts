Shader "Speed Stars/Painted Canal"
{
 Properties { _Color("Water Blue",Color)=(0.10,0.43,0.90,1) }
 SubShader { Tags {"RenderType"="Opaque"} Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color;
 struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 fixed4 frag(v2f i):SV_Target{
 float2 p=i.world.xz;float wide=hash(floor(p*float2(1.1,0.12)));float small=hash(floor(p*float2(3,0.6)));
 float light=step(0.75,wide)*0.06+(small-0.5)*0.025;
 return float4(saturate(_Color.rgb+light*float3(0.5,0.7,1)),1);
 }
 ENDCG
 } }
}
