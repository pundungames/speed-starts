// Opt-in per-renderer aerial perspective. Zero strength preserves existing rendering.
float4 _AerialSettings;float4 _AerialColor;
float3 ThemeAerial(float3 color,float3 world){float depth=distance(_WorldSpaceCameraPos,world);float strength=saturate(_AerialSettings.x)*smoothstep(_AerialSettings.y,_AerialSettings.z,depth);float luminance=dot(color,float3(.299,.587,.114));float3 quiet=lerp(color,luminance.xxx,strength*.48);quiet=lerp(quiet,_AerialColor.rgb,strength*.72);return saturate(quiet);}
