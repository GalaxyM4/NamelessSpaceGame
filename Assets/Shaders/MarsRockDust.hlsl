#ifndef MARS_ROCK_DUST_INCLUDED
#define MARS_ROCK_DUST_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// Identical material layout in every pass; no per-renderer scripts/material copies.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _DustTint;
half _BumpScale, _Smoothness, _Metallic;
half _DustCoverage, _DustTopStrength, _DustBaseStrength, _DustNormalRetention;
half _DustSmoothness, _DustTextureStrength, _DustVariation, _DustVeil;
float _DustMeters, _DustPatchMeters, _RockBottom, _RockHeight, _DustBaseHeight;
CBUFFER_END
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_DustMap); SAMPLER(sampler_DustMap);

struct RockAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct RockVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    half4 tangentWS : TEXCOORD2;
    float2 uv : TEXCOORD3;
    float height : TEXCOORD4;
    half fog : TEXCOORD5;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
float RockHash(float2 p)
{
    float3 q = frac(float3(p.xyx) * 0.1031);
    q += dot(q, q.yzx + 33.33);
    return frac((q.x + q.y) * q.z);
}
float RockNoise(float2 p)
{
    float2 c = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(RockHash(c), RockHash(c + float2(1,0)), f.x),
        lerp(RockHash(c + float2(0,1)), RockHash(c + 1), f.x), f.y);
}
RockVaryings RockVertex(RockAttributes input)
{
    RockVaryings output = (RockVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.positionCS = p.positionCS;
    output.positionWS = p.positionWS;
    output.normalWS = n.normalWS;
    output.tangentWS = half4(n.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    // Mesh-relative lower band follows the existing buried rock; no terrain queries/displacement.
    output.height = (input.positionOS.y - _RockBottom) / max(_RockHeight, 0.001);
    output.fog = ComputeFogFactor(p.positionCS.z);
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(n.normalWS, output.vertexSH);
    return output;
}
half RockDustMask(RockVaryings input)
{
    float2 pivot = GetObjectToWorldMatrix()._m03_m23;
    half instanceVariation = lerp(1 - _DustVariation, 1 + _DustVariation, RockHash(pivot));
    half patch = RockNoise(input.positionWS.xz / max(_DustPatchMeters, 0.01) +
        input.positionWS.y * 0.73);
    half top = smoothstep(0.12h, 0.85h, normalize(input.normalWS).y) * _DustTopStrength;
    half base = (1 - smoothstep(0.03, max(_DustBaseHeight, 0.04), input.height)) * _DustBaseStrength;
    half deposit = _DustVeil + (1 - _DustVeil) * max(top, base);
    return saturate(deposit * _DustCoverage * instanceVariation * lerp(0.55h, 1.15h, patch));
}
half3 RockNormal(RockVaryings input, half dust)
{
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
    normalTS = normalize(lerp(normalTS, half3(0,0,1), dust * (1 - _DustNormalRetention)));
    half3 n = normalize(input.normalWS);
    half3 t = normalize(input.tangentWS.xyz);
    half3 b = cross(n, t) * input.tangentWS.w;
    return NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(t, b, n)));
}
half4 RockFragment(RockVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half dust = RockDustMask(input);
    half3 original = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
    // Same source as the ground, but restrained contrast so it reads as fine deposited dust.
    half3 ground = SAMPLE_TEXTURE2D(_DustMap, sampler_DustMap,
        input.positionWS.xz / max(_DustMeters, 0.01)).rgb;
    half3 dustColor = lerp(_DustTint.rgb, ground * _DustTint.rgb, _DustTextureStrength);
    SurfaceData surface = (SurfaceData)0;
    surface.albedo = lerp(original, dustColor, dust);
    surface.metallic = _Metallic * (1 - dust);
    surface.smoothness = lerp(_Smoothness, _DustSmoothness, dust);
    surface.occlusion = 1;
    surface.alpha = 1;
    InputData data = (InputData)0;
    data.positionWS = input.positionWS;
    data.normalWS = RockNormal(input, dust);
    data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    data.shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
#else
    data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif
    data.fogCoord = input.fog;
    data.vertexLighting = VertexLighting(input.positionWS, data.normalWS);
    data.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, data.normalWS);
    data.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
    data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    half4 color = UniversalFragmentPBR(data, surface);
    return half4(MixFog(color.rgb, data.fogCoord), 1);
}
half4 RockDepthNormals(RockVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half3 normal = RockNormal(input, RockDustMask(input));
#if defined(_GBUFFER_NORMALS_OCT)
    return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(normal) * 0.5 + 0.5)), 0);
#else
    return half4(normal, 0);
#endif
}
float3 _LightDirection;
float3 _LightPosition;
RockVaryings RockShadowVertex(RockAttributes input)
{
    RockVaryings output = RockVertex(input);
    UNITY_SETUP_INSTANCE_ID(input);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirection = normalize(_LightPosition - output.positionWS);
#else
    float3 lightDirection = _LightDirection;
#endif
    output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, output.normalWS, lightDirection));
    output.positionCS = ApplyShadowClamping(output.positionCS);
    return output;
}
half4 RockDepth(RockVaryings input) : SV_Target { return input.positionCS.z; }
#endif
