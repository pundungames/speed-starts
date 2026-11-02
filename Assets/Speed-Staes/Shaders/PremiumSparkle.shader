Shader "Speed Stars/Premium Sparkle"
{
    Properties { _Shape ("Shape: Circle, Star, Square", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Shape;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 position : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = abs(i.uv * 2.0 - 1.0);
                float distance = length(p);
                if (_Shape > 1.5) distance = max(p.x, p.y) / 0.78;
                else if (_Shape > 0.5) distance = pow(sqrt(p.x) + sqrt(p.y), 2.0);
                float silhouette = 1.0 - smoothstep(0.72, 1.0, distance);
                return fixed4(i.color.rgb * 1.3, i.color.a * silhouette);
            }
            ENDCG
        }
    }
}
