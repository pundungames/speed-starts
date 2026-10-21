Shader "Speed Stars/Cloud Volume 2D"
{
 Properties {
 _Seed("Shape Variant",Float)=0
 _LightColor("Sunlit Cloud",Color)=(0.96,1,1,1)
 _ShadeColor("Cloud Shadow",Color)=(0.47,0.79,0.90,1)
 _PixelGrid("Pixel Columns",Float)=180
 }
 SubShader {
 Tags { "Queue"="Transparent-100" "RenderType"="Transparent" "IgnoreProjector"="True" }
 Blend One OneMinusSrcAlpha
 Cull Off ZWrite Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 float _Seed,_PixelGrid; float4 _LightColor,_ShadeColor;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
 float hash(float n){return frac(sin(n)*43758.5453);}
 fixed4 frag(v2f i):SV_Target {
 float2 uv=(floor(i.uv*_PixelGrid)+0.5)/_PixelGrid;
 float2 p=(uv-.5)*float2(3.1,1.8);
 float edge=-10, shade=.4; float shadeSum=0,weightSum=0;
 [unroll] for(int j=0;j<11;j++) {
 float t=(j-5)/5.0;
 float n=hash(j*127.1+_Seed*31.7);
 float n2=hash(j*63.9+_Seed*57.3);
 float env=pow(saturate(1-abs(t)),.65);
 float peak=exp(-pow((t-.16-sin(_Seed*2)*.22)*2.0,2));
 float2 r=float2(.18+env*(.12+n*.065),.07+env*(.18+peak*.12+n2*.22));
 float2 c=float2(t*1.17+(n-.5)*.07,-.25+r.y*.72);
 float2 q=(p-c)/r;
 float d=(1-length(q))*min(r.x,r.y);
 edge=max(edge,d); float w=exp(d*18); float localShade=saturate(.57+q.y*.25-q.x*.13+sqrt(saturate(1-dot(q,q)))*.20); shadeSum+=w*localShade;weightSum+=w;
 }
 shade=shadeSum/max(weightSum,.00001); // Broad stepped paint regions, with a narrow soft pixel edge.
 float2 cell=floor(uv*float2(45,29));
 float paint=hash(dot(cell,float2(17.13,91.7))+floor(_Seed)*19);
 float mottling=sin(p.x*12+p.y*7+_Seed)*sin(p.y*16-p.x*5)*.055;
 shade=saturate(shade+mottling+(paint-.5)*.035);
 shade=lerp(shade,floor(shade*9+.5)/9,.25);
 float alpha=smoothstep(-.024,.025,edge);
 float3 col=lerp(_ShadeColor.rgb,_LightColor.rgb,shade);
 return float4(col*alpha,alpha);
 }
 ENDCG
 }
 }
}

