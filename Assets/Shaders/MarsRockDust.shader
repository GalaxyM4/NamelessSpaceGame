Shader "Mars/Rock Dust"
{
    Properties
    {
        _BaseMap("Original rock color", 2D) = "white" {}
        _BaseColor("Rock tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Original rock normal", 2D) = "bump" {}
        _BumpScale("Rock normal strength", Range(0,2)) = 0.65
        _Smoothness("Rock smoothness", Range(0,1)) = 0.12
        _Metallic("Metallic", Range(0,1)) = 0
        _DustMap("Terrain-compatible dust texture", 2D) = "white" {}
        _DustTint("Dust tint", Color) = (0.62,0.47,0.34,1)
        _DustCoverage("Dust coverage (0 restores original)", Range(0,1)) = 0.72
        _DustVeil("Thin dust on exposed sides", Range(0,0.5)) = 0.12
        _DustTopStrength("Upward surface dust", Range(0,1)) = 0.85
        _DustBaseStrength("Lower band dust", Range(0,1)) = 0.95
        _DustBaseHeight("Lower band fraction", Range(0.04,0.7)) = 0.5
        _DustNormalRetention("Dust normal retention", Range(0,1)) = 0.3
        _DustSmoothness("Dust smoothness", Range(0,1)) = 0.02
        _DustTextureStrength("Terrain texture influence", Range(0,1)) = 0.65
        _DustVariation("Per-rock coverage variation", Range(0,0.5)) = 0.22
        _DustMeters("Dust texture meters", Float) = 0.8
        _DustPatchMeters("Patch size meters", Float) = 0.32
        [HideInInspector] _RockBottom("Mesh bottom", Float) = -0.4
        [HideInInspector] _RockHeight("Mesh height", Float) = 0.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #pragma target 3.5
        #include "MarsRockDust.hlsl"
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex RockVertex
            #pragma fragment RockFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex RockShadowVertex
            #pragma fragment RockDepth
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex RockVertex
            #pragma fragment RockDepth
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex RockVertex
            #pragma fragment RockDepthNormals
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
    FallBack Off
}
