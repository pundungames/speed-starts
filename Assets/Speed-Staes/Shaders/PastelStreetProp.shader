Shader "Speed Stars/Pastel Street Prop"
{
 Properties { _Color("Paint Color",Color)=(0.2,0.24,0.29,1) _Glow("Self-lit Face",Range(0,1))=0 _SceneLighting("Use Scene Lighting",Range(0,1))=0 _MinDirect("Minimum Face Lighting",Range(0,1))=0.32 }
 SubShader {
 Tags {"RenderType"="Opaque"}
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 #include "UnityLightingCommon.cginc"
 float4 _Color;float _Glow,_SceneLighting,_MinDirect;
 struct v2f {float4 pos:SV_POSITION;float3 n:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.n=UnityObjectToWorldNormal(v.normal);return o;}
 fixed4 frag(v2f i):SV_Target{
 float3 n=normalize(i.n);float d=dot(n,normalize(float3(0.5,0.8,-0.3)));
 float shade=d>0.5?1.12:(d>0?0.92:0.72);
 float3 painted=_Color.rgb*lerp(shade,1,_Glow);
 float ndl=dot(n,normalize(_WorldSpaceLightPos0.xyz));
 float3 direct=_LightColor0.rgb*lerp(_MinDirect,1.0,step(0.08,ndl));
 float3 ambient=max(float3(0,0,0),ShadeSH9(float4(n,1)))*0.25;
 return float4(lerp(painted,_Color.rgb*(direct+ambient),_SceneLighting),1);
 }
 ENDCG
 }
 }
}
