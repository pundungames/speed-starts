Shader "Speed Stars/Road Painted"
{
    Properties
    {
        _MainTex ("Existing Palette", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Asphalt ("Asphalt Surface", Float) = 0
        _Variation ("Painted Variation", Range(0,0.2)) = 0.065
        _RepeatLength ("Road Segment Repeat Length", Float) = 20.14
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Painted vertex:vert fullforwardshadows addshadow noforwardadd noambient novertexlights
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        float _Asphalt, _Variation, _RepeatLength;
        void vert(inout appdata_full v)
        {
            if(_Asphalt<0.5)
            {
                float3 palette=tex2Dlod(_MainTex,float4(v.texcoord.xy,0,0)).rgb;
                float4 originalWorld=mul(unity_ObjectToWorld,v.vertex);
                // Widen only painted ground markings, never yellow post bands.
                if(abs(originalWorld.y)<0.025 && palette.r>0.35 && palette.r>palette.b*1.7 && palette.g>palette.b*1.5)
                {
                    float4 world=mul(unity_ObjectToWorld,v.vertex);
                    world.x=-5.63+(world.x+5.63)*1.8;
                    v.vertex=mul(unity_WorldToObject,world);
                }
            }
        }
        struct Input { float2 uv_MainTex; float3 worldPos; float3 worldNormal; };
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        half4 LightingPainted(SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl=dot(normalize(s.Normal),lightDir);
            half face=ndl>0.55 ? 1.0 : (ndl>0.05 ? 0.86 : 0.70);
            half shadow=lerp(0.72,1.0,saturate(atten));
            return half4(s.Albedo*face*shadow,s.Alpha);
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            float3 base=tex2D(_MainTex,IN.uv_MainTex).rgb;
            if(_Asphalt>0.5)
            {
                // Long, low-contrast paint patches along the road; no added texture.
                float2 p=IN.worldPos.xz;
                float phase=frac(p.y/max(_RepeatLength,0.01));
                float broad=hash(float2(floor(p.x*1.8),floor(phase*4)));
                float fine=hash(float2(floor(p.x*22),floor(phase*60)));
                float stripe=sin(p.x*12.0+sin(phase*6.2831853))*0.5+0.5;
                base=_Color.rgb+(broad-0.5)*_Variation+(fine-0.5)*0.006+(stripe-0.5)*0.006;
            }
            else
            {
                float sourceBrightness=max(base.r,max(base.g,base.b));
                float yellow=step(base.b*1.7,base.r)*step(base.b*1.5,base.g)*step(0.35,base.r);
                base=lerp(base,float3(1.0,0.71,0.24),yellow);
                float neutral=1.0-smoothstep(0.04,0.16,max(base.r,max(base.g,base.b))-min(base.r,min(base.g,base.b)));
                base=lerp(base,base*float3(1.12,1.13,1.18),neutral);
                float tile=hash(float2(floor(IN.worldPos.x*1.6),floor(frac(IN.worldPos.z/max(_RepeatLength,0.01))*17)));
                base+=(tile-0.5)*0.045*neutral;
                base*=_Color.rgb;
                float height=IN.worldPos.y;
                float top=smoothstep(0.3,0.85,IN.worldNormal.y);
                float neutralMask=neutral*step(0.15,max(base.r,max(base.g,base.b)));
                if(height>0.60)
                {
                    // Silver crown, slate underside and small repeated fastening marks.
                    float3 metal=lerp(float3(0.59,0.64,0.70),float3(0.88,0.90,0.92),top);
                    metal+=step(0.81,height)*0.07;
                    float boltZ=frac(IN.worldPos.z/max(_RepeatLength,0.01)*20);
                    float bolt=1.0-smoothstep(0.025,0.055,length(float2((boltZ-0.5)*0.7,(height-0.81)*4)));
                    metal=lerp(metal,float3(0.35,0.41,0.48),bolt*(1-top));
                    // Dark palette entries belong to posts, not silver rails.
                    float silverMask=neutralMask*smoothstep(0.40,0.58,sourceBrightness);
                    base=lerp(base,metal*1.10,silverMask);
                }
                else if(height<0.20 && (height>0.025 || IN.worldPos.x> -0.51 || IN.worldPos.x< -10.75))
                {
                    float phase=frac(IN.worldPos.z/max(_RepeatLength,0.01));
                    float slab=floor(phase*14);
                    float jag=hash(float2(slab,floor(phase*180)));
                    float edgeDistance=min(abs(IN.worldPos.x+0.50),abs(IN.worldPos.x+10.76));
                    float rim=1.0-step(0.115+jag*0.035,edgeDistance);
                    float3 concrete=lerp(float3(0.57,0.56,0.60),float3(0.80,0.79,0.81),rim*top);
                    concrete+=(hash(float2(slab,3))-0.5)*0.05;
                    float joint=min(frac(phase*14),1.0-frac(phase*14));
                    concrete*=lerp(0.76,1.0,smoothstep(0.008,0.018,joint));
                    concrete*=lerp(0.91,1.0,top);
                    base=lerp(base,concrete,neutralMask);
                }
            }
            o.Albedo=saturate(base); o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
