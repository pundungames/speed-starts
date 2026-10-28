Shader "Speed Stars/Desert Sun Sky" {
 Properties {_Horizon("Horizon",Color)=(1,.76,.40,1) _Zenith("Upper Sky",Color)=(.91,.48,.22,1)}
 SubShader {Tags {"Queue"="Background" "RenderType"="Opaque"} Cull Off ZWrite Off
  Pass {CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _Horizon,_Zenith;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
   vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   fixed4 frag(vary i):SV_Target {float3 color=lerp(_Horizon.rgb,_Zenith.rgb,smoothstep(.12,1,i.uv.y));float2 p=i.uv-float2(.79,.86);p.x*=_ScreenParams.x/_ScreenParams.y;float r=length(p);float glow=exp(-r*r*45)*.32;float sun=1-smoothstep(.052,.063,r);color=lerp(color,float3(1,.81,.39),glow);color=lerp(color,float3(1,.91,.55),sun);return float4(color,1);}
   ENDCG}
 }
}
