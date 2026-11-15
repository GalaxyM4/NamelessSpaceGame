#ifndef MARS_TERRAIN_DETAIL_INCLUDED
#define MARS_TERRAIN_DETAIL_INCLUDED

// Keep Unity's terrain vertex deformation, lighting and shadow machinery.
// Detail changes lighting normals only, never terrain positions or image UVs.
#define _NORMALMAP 1
#define _TERRAIN_INSTANCED_PERPIXEL_NORMAL 1
#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"

TEXTURE2D(_MarsDetailNormal);
SAMPLER(sampler_MarsDetailNormal);
float _MarsDetailTileMeters;
half _MarsDetailStrength;
float _MarsDetailFadeStart;
float _MarsDetailFadeEnd;

half3 MarsDetailNormal(Varyings IN)
{
    float distanceToCamera = distance(GetCameraPositionWS(), IN.positionWS);
    half fade = 1.0h - smoothstep(_MarsDetailFadeStart,
        max(_MarsDetailFadeEnd, _MarsDetailFadeStart + 0.01), distanceToCamera);
    // Independent world-space UVs: the registered orthoimage does not repeat.
    float2 uv = IN.positionWS.xz / max(_MarsDetailTileMeters, 0.1);
    return UnpackNormalScale(SAMPLE_TEXTURE2D(_MarsDetailNormal,
        sampler_MarsDetailNormal, uv), _MarsDetailStrength * fade);
}

void MarsTerrainFragment(Varyings IN, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
#ifdef _ALPHATEST_ON
    ClipHoles(IN.uvMainAndLM.xy);
#endif
    float2 controlUV = (IN.uvMainAndLM.xy * (_Control_TexelSize.zw - 1) + 0.5) * _Control_TexelSize.xy;
    half4 control = SAMPLE_TEXTURE2D(_Control, sampler_Control, controlUV);
    half weight;
    half4 diffuse, smoothness;
    half3 layerNormal = half3(0, 0, 1);
    SplatmapMix(IN.uvMainAndLM, IN.uvSplat01, IN.uvSplat23, control,
        weight, diffuse, smoothness, layerNormal);
    half3 normalTS = normalize(half3(layerNormal.xy + MarsDetailNormal(IN).xy, layerNormal.z));
    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);
    InitializeBakedGIData(IN, inputData);
    // This material is a matte, non-metallic HiRISE surface.
    half4 color = UniversalFragmentPBR(inputData, diffuse.rgb, 0, half3(0,0,0),
        0, 1, half3(0,0,0), 1);
    SplatmapFinalColor(color, inputData.fogCoord);
    outColor = half4(color.rgb, 1);
#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

void MarsTerrainDepthNormals(Varyings IN, out half4 outNormal : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
#ifdef _ALPHATEST_ON
    ClipHoles(IN.uvMainAndLM.xy);
#endif
    InputData inputData;
    InitializeInputData(IN, MarsDetailNormal(IN), inputData);
#ifdef _GBUFFER_NORMALS_OCT
    float2 octNormal = PackNormalOctQuadEncode(inputData.normalWS);
    outNormal = half4(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)), 0);
#else
    outNormal = half4(inputData.normalWS, 0);
#endif
#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
#endif
