Shader "Speed Stars/Desert Dust" {
 Properties {_Color("Dust Tint",Color)=(1,.73,.36,1)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
  Pass {CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   fixed4 frag(vary i):SV_Target {float d=saturate(1-length(i.uv*2-1));return fixed4(i.color.rgb,i.color.a*d*d);}
   ENDCG}
 }
}
