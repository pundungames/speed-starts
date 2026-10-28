Shader "Speed Stars/Painted Pyramid Stone"
{
 Properties { _Color("Limestone",Color)=(.84,.69,.46,1) }
 SubShader {
 Tags { "RenderType"="Opaque" }
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 float4 _Color;
 struct v2f { float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;float3 worldNormal:TEXCOORD2; };
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=v.normal;o.worldNormal=UnityObjectToWorldNormal(v.normal);return o;}
 fixed4 frag(v2f i):SV_Target {
  float row=floor(i.local.y/.8);
  float along=abs(i.normal.z)>abs(i.normal.x)?i.local.x:i.local.z;
  float2 grid=float2(along/2.1+fmod(row,2)*.5,i.local.y/.8);
  float2 cell=floor(grid);float2 f=frac(grid);
  float variation=frac(sin(dot(cell,float2(17.13,71.7)))*43758.5453);
  float3 stone=_Color.rgb*(.95+variation*.09);
  float2 edge=min(f,1-f);float2 aa=max(fwidth(grid),.0001);
  float joints=min(smoothstep(.009-aa.x,.009+aa.x,edge.x),smoothstep(.018-aa.y,.018+aa.y,edge.y));
  // Broad block tones remain at any distance; only subpixel mortar lines soften.
  float3 color=lerp(_Color.rgb*.84,stone,joints);
  float d=dot(normalize(i.worldNormal),normalize(float3(.5,.8,-.3)));
  float shade=d>.5?1.08:(d>0?.96:.82);
  return float4(color*shade,1);
 }
 ENDCG
 }
 }
}
