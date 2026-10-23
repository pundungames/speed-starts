Shader "Speed Stars/Runner Toon"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Base Texture", 2D) = "white" {}
        _ShadowStrength ("Shadow Darkness", Range(0,1)) = 0.48
        _ShadowThreshold ("Hard Shadow Threshold", Range(-1,1)) = 0.15
        _NoiseStrength ("Shadow Grain Strength", Range(0,0.4)) = 0.08
        _NoiseScale ("Shadow Grain Density", Float) = 100
        _WorldGrainScale ("World Grain Density (0 uses UV)", Float) = 0
        _AmbientStrength ("Scene Ambient Strength", Range(0,2)) = 0.35
        _OutlineColor ("Outline Color", Color) = (0.025,0.025,0.03,1)
        _OutlineWidth ("Outline Width (reference pixels at 1284h)", Range(0,5)) = 1.4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            Offset 1, 1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth;
            fixed4 _OutlineColor;
            float4 vert(appdata_base v) : SV_POSITION
            {
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, UnityObjectToWorldNormal(v.normal));
                float2 direction = mul((float2x2)UNITY_MATRIX_P, viewNormal.xy);
                // Fade expansion on faces pointing at the camera, avoiding thick inner rims.
                direction /= max(length(direction), 0.35);
                // Preserve the approved 2778x1284 Simulator appearance. Scale pixel
                // width with render height, keeping thickness/height constant on
                // phones, Game View, Recorder and reduced-resolution targets.
                float scaledWidth = _OutlineWidth * (_ScreenParams.y / 1284.0);
                clip.xy += direction * (2.0 * scaledWidth / _ScreenParams.xy) * clip.w;
                return clip;
            }
            fixed4 frag() : SV_Target { return _OutlineColor; }
            ENDCG
        }
        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows addshadow noforwardadd noambient novertexlights nolightmap nodynlightmap exclude_path:deferred
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _ShadowStrength, _ShadowThreshold, _NoiseStrength;
        half _AmbientStrength;
        float _NoiseScale, _WorldGrainScale;
        struct Input { float2 uv_MainTex; float3 worldPos; float3 worldNormal; };
        struct SurfaceOutputToon
        {
            fixed3 Albedo;
            fixed3 Normal;
            fixed3 Emission;
            half Specular;
            fixed Gloss;
            fixed Alpha;
            half Grain;
            half3 SceneAmbient;
        };
        half4 LightingToon(SurfaceOutputToon s, half3 lightDir, half atten)
        {
            half lit = step(_ShadowThreshold, dot(normalize(s.Normal), lightDir)) * step(0.5, atten);
            half shade = saturate(1.0 - _ShadowStrength + s.Grain * _NoiseStrength);
            // The main light supplies its real color and intensity. Environment
            // lighting comes from Unity's sky/light-probe SH, never a fixed tint.
            half3 direct = _LightColor0.rgb * lerp(shade * 0.35, 1.0, lit);
            half3 environment = s.SceneAmbient * _AmbientStrength * lerp(1.0, 0.35, lit);
            return half4(s.Albedo * (direct + environment), s.Alpha);
        }
        void surf(Input IN, inout SurfaceOutputToon o)
        {
            fixed4 base = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = base.rgb;
            o.Alpha = 1;
            o.SceneAmbient = max(half3(0,0,0), ShadeSH9(half4(normalize(IN.worldNormal), 1)));
            float2 grainUV = IN.uv_MainTex * _NoiseScale;
            if (_WorldGrainScale > 0)
                grainUV = IN.worldPos.xz * _WorldGrainScale;
            float2 cell = floor(grainUV);
            half grain = frac(sin(dot(cell, float2(127.1,311.7))) * 43758.5453) * 2.0 - 1.0;
            // Suppress subpixel grain rather than allowing it to sparkle during motion.
            float footprint = max(length(ddx(grainUV)), length(ddy(grainUV)));
            o.Grain = grain * (1.0 - smoothstep(0.65, 1.5, footprint));
        }
        ENDCG
    }
    Fallback "Diffuse"
}
