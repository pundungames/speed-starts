// The backdrop a theme's weather draws behind the world: a two-colour gradient with an optional sun. An overcast
// theme sets Sun Size to 0 and keeps a faint glow where the light comes from; a clear one gives the disk its colour.
Shader "Speed Stars/Theme Sky" {
 Properties {
  _Horizon("Horizon",Color)=(.46,.52,.58,1)
  _Zenith("Upper Sky",Color)=(.17,.22,.30,1)
  _Break("Gradient Break",Range(0,1))=.12
  _SunColor("Sun Disk",Color)=(1,.91,.55,1)
  _GlowColor("Sun Glow",Color)=(1,.81,.39,1)
  _SunPos("Sun Position (uv)",Vector)=(.79,.86,0,0)
  _SunSize("Sun Size",Range(0,.3))=0
  _GlowStrength("Glow Strength",Range(0,1))=.12
  _GlowSpread("Glow Spread",Range(2,120))=45
 }
 SubShader {Tags {"Queue"="Background" "RenderType"="Opaque"} Cull Off ZWrite Off
  Pass {CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _Horizon,_Zenith,_SunColor,_GlowColor,_SunPos;
   float _Break,_SunSize,_GlowStrength,_GlowSpread;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
   vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   fixed4 frag(vary i):SV_Target {
    float3 color=lerp(_Horizon.rgb,_Zenith.rgb,smoothstep(_Break,1,i.uv.y));
    float2 p=i.uv-_SunPos.xy;p.x*=_ScreenParams.x/_ScreenParams.y;
    float r=length(p);
    color=lerp(color,_GlowColor.rgb,exp(-r*r*_GlowSpread)*_GlowStrength);
    if(_SunSize>0)color=lerp(color,_SunColor.rgb,1-smoothstep(_SunSize*.85,_SunSize,r));
    return float4(color,1);
   }
   ENDCG}
 }
}
