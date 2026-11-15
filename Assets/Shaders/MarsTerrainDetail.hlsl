#ifndef MARS_TERRAIN_DETAIL_INCLUDED
#define MARS_TERRAIN_DETAIL_INCLUDED

// Keep Unity's terrain vertex deformation, lighting and shadow machinery.
// Artistic surface shading only: never displace the measured terrain.
#define _NORMALMAP 1
#define _TERRAIN_INSTANCED_PERPIXEL_NORMAL 1
#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"

TEXTURE2D(_MarsDetailNormal);
SAMPLER(sampler_MarsDetailNormal);
half _MarsDetailStrength;
float _MarsDetailFadeStart;
float _MarsDetailFadeEnd;
float _MarsMacroMeters;
half _MarsMacroStrength;

float2 MarsHash(float2 p)
{
    // Stable spatial randomness; no time dependence or runtime-generated textures.
    float3 q = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33);
    return frac((q.xx + q.yz) * q.zy);
}

void MarsTriangle(float2 uv, out float2 a, out float2 b, out float2 c, out float3 weights)
{
    // Equilateral triangular lattice: three overlapping samples, not a square tile grid.
    float2 skew = float2(uv.x - uv.y * 0.577350269, uv.y * 1.154700538);
    float2 cell = floor(skew);
    float2 f = frac(skew);
    if (f.x + f.y < 1.0)
    {
        a = cell; b = cell + float2(1, 0); c = cell + float2(0, 1);
        weights = float3(1.0 - f.x - f.y, f.x, f.y);
    }
    else
    {
        a = cell + 1; b = cell + float2(0, 1); c = cell + float2(1, 0);
        weights = float3(f.x + f.y - 1.0, 1.0 - f.x, 1.0 - f.y);
    }
    // Smooth to zero at patch boundaries; retain formations rather than muddy averaging.
    weights = weights * weights * weights;
    weights /= max(dot(weights, float3(1, 1, 1)), 0.0001);
}

void MarsSample(float2 uv, float2 dx, float2 dy, float2 id, half strength,
    out half3 color, out half3 normal)
{
    float2 random = MarsHash(id);
    float angle = random.x * 6.283185307;
    float s, c;
    sincos(angle, s, c);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 sampleUV = mul(rotation, uv) + MarsHash(id + 19.17) * 47.0;
    // Derivatives must not include the discontinuous per-patch offsets/rotations.
    float2 sampleDx = mul(rotation, dx);
    float2 sampleDy = mul(rotation, dy);
    color = SAMPLE_TEXTURE2D_GRAD(_Splat0, sampler_Splat0, sampleUV, sampleDx, sampleDy).rgb;
    normal = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_MarsDetailNormal,
        sampler_MarsDetailNormal, sampleUV, sampleDx, sampleDy), strength);
    // Inverse rotation brings the sampled normal back into the original tangent frame.
    normal.xy = mul(normal.xy, rotation);
}

half MarsNoise(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(MarsHash(cell).x, MarsHash(cell + float2(1, 0)).x, f.x),
        lerp(MarsHash(cell + float2(0, 1)).x, MarsHash(cell + 1).x, f.x), f.y);
}

void MarsSurface(Varyings IN, out half3 color, out half3 normal)
{
    float distanceToCamera = distance(GetCameraPositionWS(), IN.positionWS);
    half fade = 1.0h - smoothstep(_MarsDetailFadeStart,
        max(_MarsDetailFadeEnd, _MarsDetailFadeStart + 0.01), distanceToCamera);
    float2 uv = IN.uvSplat01.xy;
    float2 a, b, c;
    float3 weights;
    MarsTriangle(uv, a, b, c, weights);
    float2 dx = ddx(uv), dy = ddy(uv);
    half3 ca, cb, cc, na, nb, nc;
    MarsSample(uv, dx, dy, a, _MarsDetailStrength * fade, ca, na);
    MarsSample(uv, dx, dy, b, _MarsDetailStrength * fade, cb, nb);
    MarsSample(uv, dx, dy, c, _MarsDetailStrength * fade, cc, nc);
    color = ca * weights.x + cb * weights.y + cc * weights.z;
    normal = normalize(na * weights.x + nb * weights.y + nc * weights.z);
    float2 macroUV = IN.positionWS.xz / max(_MarsMacroMeters, 1.0);
    half variation = (MarsNoise(macroUV) * 0.65h +
        MarsNoise(macroUV * 0.31 + 17.3) * 0.35h) * 2.0h - 1.0h;
    // Restrained rusty/pale dust variation, not black satellite-image markings.
    color *= 1.0h + variation * _MarsMacroStrength * half3(0.65h, 0.9h, 1.15h);
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
    // Deliberately supports the one full-weight artistic rock layer configured by the baker.
    half3 diffuse, normalTS;
    MarsSurface(IN, diffuse, normalTS);
    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);
    InitializeBakedGIData(IN, inputData);
    half4 color = UniversalFragmentPBR(inputData, diffuse, 0, half3(0,0,0),
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
    half3 unusedColor, normalTS;
    MarsSurface(IN, unusedColor, normalTS);
    InitializeInputData(IN, normalTS, inputData);
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
