// A leaf (or petal) for a weather layer: a pointed oval with a lighter midrib, drawn flat like the rest of the art.
// The particle's own rotation turns it, so one quad reads as a leaf tumbling through the air.
Shader "Speed Stars/Weather Leaf" {
 Properties {
  _Color("Leaf Tint",Color)=(.85,.52,.22,1)
  _Vein("Midrib",Color)=(1,.80,.52,1)
  _VeinStrength("Midrib Strength",Range(0,1))=.45
  _Width("Leaf Width",Range(.1,1))=.55
  _Edge("Edge Softness",Range(.01,.5))=.12
 }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
  Pass {CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color,_Vein;float _VeinStrength,_Width,_Edge;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   fixed4 frag(vary i):SV_Target {
    float2 p=i.uv*2-1;
    // Half width shrinks to nothing at both ends: an oval that comes to a point, not a circle.
    float halfWidth=_Width*(1-p.y*p.y);
    float body=saturate((halfWidth-abs(p.x))/_Edge);
    float rib=(1-smoothstep(0,_Width*.22,abs(p.x)))*(1-abs(p.y));
    float3 color=lerp(i.color.rgb,_Vein.rgb,rib*_VeinStrength);
    return fixed4(color,i.color.a*body);
   }
   ENDCG}
 }
}
