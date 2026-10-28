Shader "Speed Stars/Runner Contact Shadow"
{
    Properties
    {
        _Color ("Shadow Color and Opacity", Color) = (0.055,0.075,0.11,0.60)
        _EdgeSoftness ("Edge Softness", Range(0.01,0.8)) = 0.14
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _EdgeSoftness;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float radius = length((i.uv - 0.5) * 2.0);
                float edge = max(_EdgeSoftness, fwidth(radius));
                return fixed4(_Color.rgb, _Color.a * (1.0 - smoothstep(1.0-edge, 1.0, radius)));
            }
            ENDCG
        }
    }
}
