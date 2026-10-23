Shader "Speed Stars/Pastel Foliage"
{
 Properties { _BaseColor("Leaf Color",Color)=(0.30,0.57,0.08,1) _ShadowColor("Shadow Green",Color)=(0.15,0.36,0.11,1) _LightColor("Sunlit Green",Color)=(0.56,0.77,0.12,1) _PatchData("Patch Density, Seed, Strength, Trunk",Vector)=(4,1,0.12,0) }
 SubShader
 {
  Tags {"RenderType"="Opaque"}
  Cull Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   UNITY_INSTANCING_BUFFER_START(Plant)
    UNITY_DEFINE_INSTANCED_PROP(float4,_BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4,_ShadowColor)
    UNITY_DEFINE_INSTANCED_PROP(float4,_LightColor)
    UNITY_DEFINE_INSTANCED_PROP(float4,_PatchData)
    UNITY_DEFINE_INSTANCED_PROP(float4,_CanopyShape)
   UNITY_INSTANCING_BUFFER_END(Plant)
   struct v2f {float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;float3 localNormal:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
   v2f vert(appdata_base v)
   {
    v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);
    float4 patch=UNITY_ACCESS_INSTANCED_PROP(Plant,_PatchData);
    float4 shape=UNITY_ACCESS_INSTANCED_PROP(Plant,_CanopyShape);
    if(patch.w<0.5)
    {
     float3 n=normalize(v.normal);float seed=patch.y*0.71;
     float wobble=sin(n.x*shape.y+seed)*sin(n.y*shape.y*1.2-seed)*cos(n.z*shape.y+seed*0.3);
     float secondary=sin(n.x*shape.y*1.7-seed)*cos(n.y*shape.y*1.4+seed)*sin(n.z*shape.y*1.3+seed);
     v.vertex.xyz*=1+shape.x*(wobble*0.75+secondary*0.35);
    }
    o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz+0.5;o.normal=UnityObjectToWorldNormal(v.normal);o.localNormal=v.normal;return o;
   }
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float puff(float2 p,float2 center,float2 radius){return 1-length((p-center)/radius);}
   fixed4 frag(v2f i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);
    float4 color=UNITY_ACCESS_INSTANCED_PROP(Plant,_BaseColor);
    float4 patch=UNITY_ACCESS_INSTANCED_PROP(Plant,_PatchData);
    float4 shape=UNITY_ACCESS_INSTANCED_PROP(Plant,_CanopyShape);
    if(patch.w>1.5)
    {
     // The reference is a connected hedge, with a low left lobe and a taller
     // off-centre crown. Geometry stays planar; pixel cells shape its outline.
     float2 count=float2(26,19);
     float2 id=floor(i.local.xy*count);
     float2 paint=(id+0.5)/count;paint.x=1-paint.x;
     float2 outlineGrid=float2(70,48);
     float2 p=(floor(i.local.xy*outlineGrid)+0.5)/outlineGrid;
     p.x=1-p.x;
     float seed=patch.y;
     float shift=(hash(float2(seed,2))-0.5)*0.12;
     float body=puff(p,float2(0.5,0.13),float2(0.48,0.24));
     body=max(body,puff(p,float2(0.20,0.37+shift),float2(0.18,0.26)));
     body=max(body,puff(p,float2(0.39,0.32),float2(0.18,0.22)));
     body=max(body,puff(p,float2(0.62,0.57-shift),float2(0.23,0.37)));
     body=max(body,puff(p,float2(0.77,0.38),float2(0.18,0.29)));
     body=max(body,puff(p,float2(0.88,0.23),float2(0.12,0.19)));
     float small=hash(id+seed*3.7);
     clip(body+(small-0.5)*0.15-0.025);
     float mass=hash(floor(id/3)+seed*0.17);
     float shade=paint.y*0.45+0.28+(mass-0.5)*0.40+(small-0.5)*0.12;
     // Blue-green pockets under foliage; sunlit yellow-green top-left cells.
     shade+=0.18*saturate(1-paint.x*1.6)*smoothstep(0.25,0.6,paint.y);
     shade+=0.08*smoothstep(0.55,0.85,paint.y);
     float3 dark=UNITY_ACCESS_INSTANCED_PROP(Plant,_ShadowColor).rgb;
     float3 light=UNITY_ACCESS_INSTANCED_PROP(Plant,_LightColor).rgb;
     float3 rgb=shade<0.30?dark:(shade<0.46?lerp(dark,color.rgb,0.65):(shade<0.62?color.rgb:(shade<0.78?lerp(color.rgb,light,0.58):light)));
     return float4(rgb,1);
    }
    float grid=max(6,patch.x*4);
    float3 centerWorld=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
    float3 viewLocal=mul((float3x3)unity_WorldToObject,_WorldSpaceCameraPos-centerWorld);
    bool faceZ=abs(viewLocal.z)>abs(viewLocal.x);
    float2 projected=faceZ?i.local.xy:i.local.zy;
    float aspect=faceZ?shape.z/max(0.01,shape.w):length(unity_ObjectToWorld._m02_m12_m22)/max(0.01,shape.w);
    float2 pixelGrid=float2(grid*max(0.5,aspect),grid);
    float2 pixelCell=floor(projected*pixelGrid);
    float2 q=(pixelCell+0.5)/pixelGrid-0.5;
    float facing=sqrt(max(0.015,0.25-dot(q,q)));
    float3 roundedNormal=normalize(faceZ?float3(q.x,q.y,sign(viewLocal.z)*facing):float3(sign(viewLocal.x)*facing,q.y,q.x));
    float3 n=patch.w>0.5?normalize(i.normal):UnityObjectToWorldNormal(roundedNormal);
    float sunlight=dot(n,normalize(float3(0.45,0.85,-0.35)));
    float shade=sunlight>0.5?1.12:(sunlight>0?0.92:0.70);
    float3 ln=abs(i.localNormal);
    float2 uv=ln.y>0.5?i.local.xz:(ln.x>0.5?i.local.zy:i.local.xy);
    uv=clamp(uv,0.0001,0.9999);
    float cell=hash(pixelCell+patch.y);
    float broad=hash(floor(pixelCell/2)+patch.y*0.31);
    float illumination=saturate(sunlight*0.60+0.43+(cell-0.5)*patch.z*2.5+(broad-0.5)*0.22);
    illumination=illumination>0.78?1.0:(illumination>0.62?0.80:(illumination>0.43?0.53:(illumination>0.25?0.28:0.05)));
    float3 dark=UNITY_ACCESS_INSTANCED_PROP(Plant,_ShadowColor).rgb;
    float3 light=UNITY_ACCESS_INSTANCED_PROP(Plant,_LightColor).rgb;
    float3 rgb=illumination<0.5?lerp(dark,color.rgb,illumination*2):lerp(color.rgb,light,(illumination-0.5)*2);
    if(patch.w>0.5)rgb=color.rgb*shade;
    return float4(saturate(rgb),1);
   }
   ENDCG
  }
 }
}
