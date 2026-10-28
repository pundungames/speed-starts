Shader "Speed Stars/Road Daylight Sky"
{
 SubShader {
 Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
 Cull Off ZWrite Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
 v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o; }
 fixed4 frag(v2f i):SV_Target { float y=normalize(i.dir).y;return float4(lerp(float3(0.29,0.76,0.98),float3(0.08,0.43,0.99),saturate(y*2.4+0.2)),1); }
 ENDCG
 }
 }
}
