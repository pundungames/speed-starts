Shader "Speed Stars/Desert Pixel Paint" {
 Properties {_Color("Paint",Color)=(.9,.75,.5,1) _Glow("Flat Paint",Range(0,1))=.3 _PixelSize("World Paint Cell",Float)=.18 _Grain("Paint Variation",Range(0,.15))=.008 _PaintProfile("Surface: plaster, stone, wood, foliage, metal, distant",Float)=0 _PaintShadow("Painted Shadow",Color)=(.62,.66,.68,1) _PaintLight("Painted Highlight",Color)=(1.05,1.04,1.01,1)}
 SubShader {Tags {"RenderType"="Opaque"} Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color,_PaintShadow,_PaintLight;float _Glow,_PixelSize,_Grain,_PaintProfile;
 struct v2f{float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);float3 objectScale=float3(length(mul((float3x3)unity_ObjectToWorld,float3(1,0,0))),length(mul((float3x3)unity_ObjectToWorld,float3(0,1,0))),length(mul((float3x3)unity_ObjectToWorld,float3(0,0,1))));o.local=v.vertex.xyz*objectScale;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
 float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
 fixed4 frag(v2f i):SV_Target{
  float3 n=normalize(i.normal);float sunlight=dot(n,normalize(float3(.5,.8,-.3)));
  float size=max(.05,_PixelSize);float3 axis=abs(n);float2 surface=axis.y>.6?i.local.xz:(axis.x>axis.z?i.local.zy:i.local.xy);
  float2 cell=floor(surface/size);float footprint=max(length(ddx(surface)),length(ddy(surface)));float fade=1-smoothstep(size*.65,size*1.8,footprint);
  // Broad paint patches have a purpose per material; grain never covers every surface equally.
  float paint=(hash(float3(floor(cell/3),7))-.5)*_Grain;
  if(_PaintProfile>1.5&&_PaintProfile<2.5){float2 fibres=floor(surface/float2(size*.45,size*5));paint=(hash(float3(fibres,13))-.5)*_Grain;}
  float illumination=sunlight>.5?1:(sunlight>0?.61:.18);
  if(_PaintProfile>2.5&&_PaintProfile<3.5){float mass=hash(float3(floor(cell/2),29));float fine=hash(float3(cell,19));float value=saturate(sunlight*.57+.43+(mass-.5)*.32+(fine-.5)*.13);illumination=value>.78?1:(value>.6?.8:(value>.42?.55:(value>.25?.29:.06)));paint=0;}
  float3 shadow=_Color.rgb*_PaintShadow.rgb,lit=_Color.rgb*_PaintLight.rgb;
  float3 color=illumination<.6?lerp(shadow,_Color.rgb,illumination/.6):lerp(_Color.rgb,lit,(illumination-.6)/.4);
  color=lerp(color,_Color.rgb,saturate(_Glow)*.55);
  color+=_Color.rgb*paint*fade;
  return float4(saturate(color),1);
 }
 ENDCG}}
}
