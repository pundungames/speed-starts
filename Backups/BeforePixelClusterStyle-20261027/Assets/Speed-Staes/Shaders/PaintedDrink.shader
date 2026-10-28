Shader "Speed Stars/Painted Drink"
{
 Properties { _Color("Drink Color",Color)=(0.2,0.5,0.7,1) _Variation("Paint Variation",Range(0,0.15))=0.045 }
 SubShader {
 Tags {"RenderType"="Opaque"}
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color;float _Variation;
 struct v2f {float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz+0.5;o.normal=v.normal;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 fixed4 frag(v2f i):SV_Target {
 float2 uv=abs(i.normal.x)>0.5?i.local.zy:i.local.xy;
 float broad=hash(floor(uv*float2(3,7)));
 float fine=hash(floor(uv*float2(10,22)));
 float variation=(broad-0.5)*_Variation+(fine-0.5)*0.008;
 return float4(saturate(_Color.rgb+variation),1);
 }
 ENDCG
 }
 }
}
