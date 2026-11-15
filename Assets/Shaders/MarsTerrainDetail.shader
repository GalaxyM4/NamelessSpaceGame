Shader "NamelessSpaceGame/Mars Terrain Detail"
{
    Properties
    {
        [Normal] _MarsDetailNormal("Surface Detail Normal (Artistic)", 2D) = "bump" {}
        _MarsDetailTileMeters("Detail Tile Size (Metres)", Float) = 3
        _MarsDetailStrength("Detail Strength", Range(0, 1)) = 0.3
        _MarsDetailFadeStart("Detail Fade Start (Metres)", Float) = 12
        _MarsDetailFadeEnd("Detail Fade End (Metres)", Float) = 45
        [HideInInspector] _Control("Control", 2D) = "red" {}
        [HideInInspector] _Splat0("Layer 0", 2D) = "grey" {}
        [HideInInspector] _Splat1("Layer 1", 2D) = "grey" {}
        [HideInInspector] _Splat2("Layer 2", 2D) = "grey" {}
        [HideInInspector] _Splat3("Layer 3", 2D) = "grey" {}
        [HideInInspector] _Normal0("Normal 0", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2", 2D) = "bump" {}
        [HideInInspector] _Normal3("Normal 3", 2D) = "bump" {}
        [HideInInspector] _MainTex("Base Map", 2D) = "grey" {}
        [HideInInspector] _BaseColor("Base Color", Color) = (1,1,1,1)
        [HideInInspector] _TerrainHolesTexture("Holes", 2D) = "white" {}
        [HideInInspector] _EnableInstancedPerPixelNormal("Per Pixel Terrain Normal", Float) = 1
        [HideInInspector] _NumLayersCount("Layer Count", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Geometry-100" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"
               "UniversalMaterialType"="Lit" "TerrainCompatible"="True" }
        Pass
        {
            Name "ForwardLit"
            // Also works with a deferred renderer, without an inconsistent custom GBuffer.
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SplatmapVert
            #pragma fragment MarsTerrainFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma multi_compile_fragment _ _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "MarsTerrainDetail.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SplatmapVert
            #pragma fragment MarsTerrainDepthNormals
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma multi_compile_fragment _ _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "MarsTerrainDetail.hlsl"
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Terrain/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Terrain/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Terrain/Lit/SceneSelectionPass"
        UsePass "Universal Render Pipeline/Terrain/Lit/Meta"
        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
    }
    Dependency "AddPassShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Add Pass)"
    Dependency "BaseMapShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Base Pass)"
    Dependency "BaseMapGenShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Basemap Gen)"
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
