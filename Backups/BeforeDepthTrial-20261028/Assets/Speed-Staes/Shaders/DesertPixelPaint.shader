Shader "Speed Stars/Desert Pixel Paint" {
 Properties {_Color("Paint",Color)=(.9,.75,.5,1) _Glow("Flat Paint",Range(0,1))=.3 _PixelSize("World Paint Cell",Float)=.18 _Grain("Paint Variation",Range(0,.15))=.008 _PaintProfile("Surface: plaster, stone, wood, foliage, metal, distant",Float)=0 _PaintShadow("Painted Shadow",Color)=(.62,.66,.68,1) _PaintLight("Painted Highlight",Color)=(1.05,1.04,1.01,1) _ClusterStrength("Paint Cluster Strength",Range(0,1))=.15 _PixelSnap("Pixel Silhouette",Range(0,1))=0}
 SubShader {Tags {"RenderType"="Opaque"} Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color,_PaintShadow,_PaintLight;float _Glow,_PixelSize,_Grain,_PaintProfile,_ClusterStrength,_PixelSnap;
 struct v2f{float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);float2 grid=float2(360*_ScreenParams.x/max(1,_ScreenParams.y),360);float2 snapped=(floor((o.pos.xy/max(.0001,o.pos.w)*.5+.5)*grid)+.5)/grid;float2 clip=(snapped*2-1)*o.pos.w;o.pos.xy=lerp(o.pos.xy,clip,_PixelSnap);float3 objectScale=float3(length(mul((float3x3)unity_ObjectToWorld,float3(1,0,0))),length(mul((float3x3)unity_ObjectToWorld,float3(0,1,0))),length(mul((float3x3)unity_ObjectToWorld,float3(0,0,1))));o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.local=v.vertex.xyz*objectScale;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
 float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
 float cluster(float2 p){float2 id=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(float3(id,17)),hash(float3(id+float2(1,0),17)),f.x),lerp(hash(float3(id+float2(0,1),17)),hash(float3(id+1,17)),f.x),f.y);}
 fixed4 frag(v2f i):SV_Target{
  if(_PaintProfile>6.5){float2 p=float2(i.world.z/12,i.world.y/4);float2 grid=floor(p*12)/12;float value=cluster(grid+float2(i.world.y*.06,0));float slope=dot(normalize(i.normal),normalize(float3(.45,.85,-.35)));float band=value>.65?.055:(value<.32?-.065:0);float ridge=slope>.5?.035:(slope<0?-.035:0);return float4(saturate(_Color.rgb+band+ridge),1);}
if(_PaintProfile>5.5){float2 p=i.world.xz;float2 coarse=floor(p/float2(.8,.5))*float2(.8,.5);float a=cluster(coarse/float2(18,8));float b=cluster(coarse/float2(35,17)+float2(71,29));float value=a*.72+b*.28;float tonal=value>.64?1:(value<.35?-1:0);float edge=min(abs(value-.64),abs(value-.35));float soften=smoothstep(0,.035,edge);float offset=tonal*soften*.045;float shade=dot(normalize(i.normal),float3(.45,.85,-.35))>.2?1:.95;return float4(saturate(_Color.rgb*shade+offset),1);}float3 n=normalize(i.normal);float sunlight=dot(n,normalize(float3(.5,.8,-.3)));
  float size=max(.05,_PixelSize);float3 axis=abs(n);float2 surface=axis.y>.6?i.local.xz:(axis.x>axis.z?i.local.zy:i.local.xy);
  float2 cell=floor(surface/size);float footprint=max(length(ddx(surface)),length(ddy(surface)));float fade=1-smoothstep(size*.65,size*1.8,footprint);
  // Broad paint patches have a purpose per material; grain never covers every surface equally.
  float paint=(hash(float3(floor(cell/3),7))-.5)*_Grain;
  if(_PaintProfile>1.5&&_PaintProfile<2.5){float2 fibres=floor(surface/float2(size*.45,size*5));paint=(hash(float3(fibres,13))-.5)*_Grain;}
  float mass=cluster((cell+.5)/3);float patch=floor(mass*4)/3-.5;
  float illumination=sunlight>.5?1:(sunlight>0?.60:.12);
  illumination=saturate(illumination+patch*_ClusterStrength*fade);
  if(_PaintProfile>2.5&&_PaintProfile<3.5){float small=hash(float3(cell,29));float lump=hash(float3(floor(cell/3),31));float3 pixelNormal=normalize(floor(n*5+.5)/5+float3(.001,.001,.001));float light=dot(pixelNormal,normalize(float3(.45,.85,-.35)));float value=saturate(light*.55+.43+(lump-.5)*.40+(small-.5)*.13);illumination=value>.78?1:(value>.62?.80:(value>.46?.6:(value>.30?.32:.04)));paint=0;}
  else illumination=floor(illumination*4+.5)/4;
  float3 shadow=_Color.rgb*_PaintShadow.rgb,lit=_Color.rgb*_PaintLight.rgb;
  float3 color=illumination<.6?lerp(shadow,_Color.rgb,illumination/.6):lerp(_Color.rgb,lit,(illumination-.6)/.4);
  color=lerp(color,_Color.rgb,saturate(_Glow)*.55);
  color+=_Color.rgb*paint*fade;
  return float4(saturate(color),1);
 }
 ENDCG}}
}
