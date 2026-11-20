// Rain and anything else a weather layer draws stretched along its own velocity: a soft line that thins out at
// both tips, so a stretched billboard reads as a falling drop instead of a smeared blob.
Shader "Speed Stars/Weather Streak" {
 Properties {_Color("Streak Tint",Color)=(.80,.88,.96,1) _Softness("Edge Softness",Range(0.05,1))=.45}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
  Pass {CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;float _Softness;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   fixed4 frag(vary i):SV_Target {
    // uv.x runs across the drop, uv.y along it (stretched billboards map the quad this way).
    float across=1-abs(i.uv.x*2-1);
    float body=saturate(across/_Softness);
    float along=sin(i.uv.y*3.14159265);
    return fixed4(i.color.rgb,i.color.a*body*body*along);
   }
   ENDCG}
 }
}
