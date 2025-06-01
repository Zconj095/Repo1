 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\AutoLight.cginc---------------


#ifndef AUTOLIGHT_INCLUDED
#define AUTOLIGHT_INCLUDED

#include "HLSLSupport.cginc"
#include "UnityShadowLibrary.cginc"

// ----------------
//  Shadow helpers
// ----------------

// If none of the keywords are defined, assume directional?
#if !defined(POINT) && !defined(SPOT) && !defined(DIRECTIONAL) && !defined(POINT_COOKIE) && !defined(DIRECTIONAL_COOKIE)
    #define DIRECTIONAL 1
#endif

// ---- Screen space direction light shadows helpers (any version)
#if defined (SHADOWS_SCREEN)

    #if defined(UNITY_NO_SCREENSPACE_SHADOWS)
        UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
        #define TRANSFER_SHADOW(a) a._ShadowCoord = mul( unity_WorldToShadow[0], mul( unity_ObjectToWorld, v.vertex ) );
        #define TRANSFER_SHADOW_WPOS(a, wpos) a._ShadowCoord = mul( unity_WorldToShadow[0], float4(wpos.xyz, 1.0f) );
        inline fixed unitySampleShadow (unityShadowCoord4 shadowCoord)
        {
            #if defined(SHADOWS_NATIVE)
                fixed shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, shadowCoord.xyz);
                shadow = _LightShadowData.r + shadow * (1-_LightShadowData.r);
                return shadow;
            #else
                unityShadowCoord dist = SAMPLE_DEPTH_TEXTURE(_ShadowMapTexture, shadowCoord.xy);
                // tegra is confused if we use _LightShadowData.x directly
                // with "ambiguous overloaded function reference max(mediump float, float)"
                unityShadowCoord lightShadowDataX = _LightShadowData.x;
                unityShadowCoord threshold = shadowCoord.z;
                return max(dist > threshold, lightShadowDataX);
            #endif
        }

    #else // UNITY_NO_SCREENSPACE_SHADOWS
        UNITY_DECLARE_SCREENSPACE_SHADOWMAP(_ShadowMapTexture);
        #define TRANSFER_SHADOW(a) a._ShadowCoord = ComputeScreenPos(a.pos);
        #define TRANSFER_SHADOW_WPOS(a, wpos) a._ShadowCoord = ComputeScreenPos(a.pos);
        inline fixed unitySampleShadow (unityShadowCoord4 shadowCoord)
        {
            fixed shadow = UNITY_SAMPLE_SCREEN_SHADOW(_ShadowMapTexture, shadowCoord);
            return shadow;
        }

    #endif

    #define SHADOW_COORDS(idx1) unityShadowCoord4 _ShadowCoord : TEXCOORD##idx1;
    #define SHADOW_ATTENUATION(a) unitySampleShadow(a._ShadowCoord)
#endif

// -----------------------------
//  Shadow helpers (5.6+ version)
// -----------------------------
// This version depends on having worldPos available in the fragment shader and using that to compute light coordinates.
// if also supports ShadowMask (separately baked shadows for lightmapped objects)

half UnityComputeForwardShadows(float2 lightmapUV, float3 worldPos, float4 screenPos)
{
    //fade value
    float zDist = dot(_WorldSpaceCameraPos - worldPos, UNITY_MATRIX_V[2].xyz);
    float fadeDist = UnityComputeShadowFadeDistance(worldPos, zDist);
    half  realtimeToBakedShadowFade = UnityComputeShadowFade(fadeDist);

    //baked occlusion if any
    half shadowMaskAttenuation = UnitySampleBakedOcclusion(lightmapUV, worldPos);

    half realtimeShadowAttenuation = 1.0f;
    //directional realtime shadow
    #if defined (SHADOWS_SCREEN)
        #if defined(UNITY_NO_SCREENSPACE_SHADOWS) && !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
            realtimeShadowAttenuation = unitySampleShadow(mul(unity_WorldToShadow[0], unityShadowCoord4(worldPos, 1)));
        #else
            //Only reached when LIGHTMAP_ON is NOT defined (and thus we use interpolator for screenPos rather than lightmap UVs). See HANDLE_SHADOWS_BLENDING_IN_GI below.
            realtimeShadowAttenuation = unitySampleShadow(screenPos);
        #endif
    #endif

    #if defined(UNITY_FAST_COHERENT_DYNAMIC_BRANCHING) && defined(SHADOWS_SOFT) && !defined(LIGHTMAP_SHADOW_MIXING)
    //avoid expensive shadows fetches in the distance where coherency will be good
    UNITY_BRANCH
    if (realtimeToBakedShadowFade < (1.0f - 1e-2f))
    {
    #endif

        //spot realtime shadow
        #if (defined (SHADOWS_DEPTH) && defined (SPOT))
            #if !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
                unityShadowCoord4 spotShadowCoord = mul(unity_WorldToShadow[0], unityShadowCoord4(worldPos, 1));
            #else
                unityShadowCoord4 spotShadowCoord = screenPos;
            #endif
            realtimeShadowAttenuation = UnitySampleShadowmap(spotShadowCoord);
        #endif

        //point realtime shadow
        #if defined (SHADOWS_CUBE)
            realtimeShadowAttenuation = UnitySampleShadowmap(worldPos - _LightPositionRange.xyz);
        #endif

    #if defined(UNITY_FAST_COHERENT_DYNAMIC_BRANCHING) && defined(SHADOWS_SOFT) && !defined(LIGHTMAP_SHADOW_MIXING)
    }
    #endif

    return UnityMixRealtimeAndBakedShadows(realtimeShadowAttenuation, shadowMaskAttenuation, realtimeToBakedShadowFade);
}

#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
#   define UNITY_SHADOW_W(_w) _w
#else
#   define UNITY_SHADOW_W(_w) (1.0/_w)
#endif

#if !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
#    define UNITY_READ_SHADOW_COORDS(input) 0
#else
#    define UNITY_READ_SHADOW_COORDS(input) READ_SHADOW_COORDS(input)
#endif

#if defined(HANDLE_SHADOWS_BLENDING_IN_GI) // handles shadows in the depths of the GI function for performance reasons
#   define UNITY_SHADOW_COORDS(idx1) SHADOW_COORDS(idx1)
#   define UNITY_TRANSFER_SHADOW(a, coord) TRANSFER_SHADOW(a)
#   define UNITY_SHADOW_ATTENUATION(a, worldPos) SHADOW_ATTENUATION(a)
#elif defined(SHADOWS_SCREEN) && !defined(LIGHTMAP_ON) && !defined(UNITY_NO_SCREENSPACE_SHADOWS) // no lightmap uv thus store screenPos instead
    // can happen if we have two directional lights. main light gets handled in GI code, but 2nd dir light can have shadow screen and mask.
    // - Disabled on ES2 because WebGL 1.0 seems to have junk in .w (even though it shouldn't)
#   if defined(SHADOWS_SHADOWMASK) && !defined(SHADER_API_GLES)
#       define UNITY_SHADOW_COORDS(idx1) unityShadowCoord4 _ShadowCoord : TEXCOORD##idx1;
#       define UNITY_TRANSFER_SHADOW(a, coord) {a._ShadowCoord.xy = coord * unity_LightmapST.xy + unity_LightmapST.zw; a._ShadowCoord.zw = ComputeScreenPos(a.pos).xy;}
#       define UNITY_SHADOW_ATTENUATION(a, worldPos) UnityComputeForwardShadows(a._ShadowCoord.xy, worldPos, float4(a._ShadowCoord.zw, 0.0, UNITY_SHADOW_W(a.pos.w)));
#   else
#       define UNITY_SHADOW_COORDS(idx1) SHADOW_COORDS(idx1)
#       define UNITY_TRANSFER_SHADOW(a, coord) TRANSFER_SHADOW(a)
#       define UNITY_SHADOW_ATTENUATION(a, worldPos) UnityComputeForwardShadows(0, worldPos, a._ShadowCoord)
#   endif
#else
#   define UNITY_SHADOW_COORDS(idx1) unityShadowCoord4 _ShadowCoord : TEXCOORD##idx1;
#   if defined(SHADOWS_SHADOWMASK)
#       define UNITY_TRANSFER_SHADOW(a, coord) a._ShadowCoord.xy = coord.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#       if (defined(SHADOWS_DEPTH) || defined(SHADOWS_SCREEN) || defined(SHADOWS_CUBE) || UNITY_LIGHT_PROBE_PROXY_VOLUME)
#           define UNITY_SHADOW_ATTENUATION(a, worldPos) UnityComputeForwardShadows(a._ShadowCoord.xy, worldPos, UNITY_READ_SHADOW_COORDS(a))
#       else
#           define UNITY_SHADOW_ATTENUATION(a, worldPos) UnityComputeForwardShadows(a._ShadowCoord.xy, 0, 0)
#       endif
#   else
#       if !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
#           define UNITY_TRANSFER_SHADOW(a, coord)
#       else
#           define UNITY_TRANSFER_SHADOW(a, coord) TRANSFER_SHADOW(a)
#       endif
#       if (defined(SHADOWS_DEPTH) || defined(SHADOWS_SCREEN) || defined(SHADOWS_CUBE))
#           define UNITY_SHADOW_ATTENUATION(a, worldPos) UnityComputeForwardShadows(0, worldPos, UNITY_READ_SHADOW_COORDS(a))
#       else
#           if UNITY_LIGHT_PROBE_PROXY_VOLUME
#               define UNITY_SHADOW_ATTENUATION(a, worldPos) UnityComputeForwardShadows(0, worldPos, UNITY_READ_SHADOW_COORDS(a))
#           else
#               define UNITY_SHADOW_ATTENUATION(a, worldPos) UnityComputeForwardShadows(0, 0, 0)
#           endif
#       endif
#   endif
#endif

#ifdef POINT
sampler2D_float _LightTexture0;
unityShadowCoord4x4 unity_WorldToLight;
#   define UNITY_LIGHT_ATTENUATION(destName, input, worldPos) \
        unityShadowCoord3 lightCoord = mul(unity_WorldToLight, unityShadowCoord4(worldPos, 1)).xyz; \
        fixed shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
        fixed destName = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).r * shadow;
#endif

#ifdef SPOT
sampler2D_float _LightTexture0;
unityShadowCoord4x4 unity_WorldToLight;
sampler2D_float _LightTextureB0;
inline fixed UnitySpotCookie(unityShadowCoord4 LightCoord)
{
    return tex2D(_LightTexture0, LightCoord.xy / LightCoord.w + 0.5).w;
}
inline fixed UnitySpotAttenuate(unityShadowCoord3 LightCoord)
{
    return tex2D(_LightTextureB0, dot(LightCoord, LightCoord).xx).r;
}
#if !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
#define DECLARE_LIGHT_COORD(input, worldPos) unityShadowCoord4 lightCoord = mul(unity_WorldToLight, unityShadowCoord4(worldPos, 1))
#else
#define DECLARE_LIGHT_COORD(input, worldPos) unityShadowCoord4 lightCoord = input._LightCoord
#endif
#   define UNITY_LIGHT_ATTENUATION(destName, input, worldPos) \
        DECLARE_LIGHT_COORD(input, worldPos); \
        fixed shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
        fixed destName = (lightCoord.z > 0) * UnitySpotCookie(lightCoord) * UnitySpotAttenuate(lightCoord.xyz) * shadow;
#endif

#ifdef DIRECTIONAL
#   define UNITY_LIGHT_ATTENUATION(destName, input, worldPos) fixed destName = UNITY_SHADOW_ATTENUATION(input, worldPos);
#endif

#ifdef POINT_COOKIE
samplerCUBE_float _LightTexture0;
unityShadowCoord4x4 unity_WorldToLight;
sampler2D_float _LightTextureB0;
#   if !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
#       define DECLARE_LIGHT_COORD(input, worldPos) unityShadowCoord3 lightCoord = mul(unity_WorldToLight, unityShadowCoord4(worldPos, 1)).xyz
#   else
#       define DECLARE_LIGHT_COORD(input, worldPos) unityShadowCoord3 lightCoord = input._LightCoord
#   endif
#   define UNITY_LIGHT_ATTENUATION(destName, input, worldPos) \
        DECLARE_LIGHT_COORD(input, worldPos); \
        fixed shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
        fixed destName = tex2D(_LightTextureB0, dot(lightCoord, lightCoord).rr).r * texCUBE(_LightTexture0, lightCoord).w * shadow;
#endif

#ifdef DIRECTIONAL_COOKIE
sampler2D_float _LightTexture0;
unityShadowCoord4x4 unity_WorldToLight;
#   if !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
#       define DECLARE_LIGHT_COORD(input, worldPos) unityShadowCoord2 lightCoord = mul(unity_WorldToLight, unityShadowCoord4(worldPos, 1)).xy
#   else
#       define DECLARE_LIGHT_COORD(input, worldPos) unityShadowCoord2 lightCoord = input._LightCoord
#   endif
#   define UNITY_LIGHT_ATTENUATION(destName, input, worldPos) \
        DECLARE_LIGHT_COORD(input, worldPos); \
        fixed shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
        fixed destName = tex2D(_LightTexture0, lightCoord).w * shadow;
#endif


// -----------------------------
//  Light/Shadow helpers (4.x version)
// -----------------------------
// This version computes light coordinates in the vertex shader and passes them to the fragment shader.

// ---- Spot light shadows
#if defined (SHADOWS_DEPTH) && defined (SPOT)
#define SHADOW_COORDS(idx1) unityShadowCoord4 _ShadowCoord : TEXCOORD##idx1;
#define TRANSFER_SHADOW(a) a._ShadowCoord = mul (unity_WorldToShadow[0], mul(unity_ObjectToWorld,v.vertex));
#define TRANSFER_SHADOW_WPOS(a, wpos) a._ShadowCoord = mul (unity_WorldToShadow[0], float4(wpos.xyz, 1.0f));
#define SHADOW_ATTENUATION(a) UnitySampleShadowmap(a._ShadowCoord)
#endif

// ---- Point light shadows
#if defined (SHADOWS_CUBE)
#define SHADOW_COORDS(idx1) unityShadowCoord3 _ShadowCoord : TEXCOORD##idx1;
#define TRANSFER_SHADOW(a) a._ShadowCoord.xyz = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz;
#define TRANSFER_SHADOW_WPOS(a, wpos) a._ShadowCoord.xyz = wpos.xyz - _LightPositionRange.xyz;
#define SHADOW_ATTENUATION(a) UnitySampleShadowmap(a._ShadowCoord)
#define READ_SHADOW_COORDS(a) unityShadowCoord4(a._ShadowCoord.xyz, 1.0)
#endif

// ---- Shadows off
#if !defined (SHADOWS_SCREEN) && !defined (SHADOWS_DEPTH) && !defined (SHADOWS_CUBE)
#define SHADOW_COORDS(idx1)
#define TRANSFER_SHADOW(a)
#define TRANSFER_SHADOW_WPOS(a, wpos)
#define SHADOW_ATTENUATION(a) 1.0
#define READ_SHADOW_COORDS(a) 0
#else
#ifndef READ_SHADOW_COORDS
#define READ_SHADOW_COORDS(a) a._ShadowCoord
#endif
#endif

#ifdef POINT
#   define DECLARE_LIGHT_COORDS(idx) unityShadowCoord3 _LightCoord : TEXCOORD##idx;
#   define COMPUTE_LIGHT_COORDS(a) a._LightCoord = mul(unity_WorldToLight, mul(unity_ObjectToWorld, v.vertex)).xyz;
#   define LIGHT_ATTENUATION(a)    (tex2D(_LightTexture0, dot(a._LightCoord,a._LightCoord).rr).r * SHADOW_ATTENUATION(a))
#endif

#ifdef SPOT
#   define DECLARE_LIGHT_COORDS(idx) unityShadowCoord4 _LightCoord : TEXCOORD##idx;
#   define COMPUTE_LIGHT_COORDS(a) a._LightCoord = mul(unity_WorldToLight, mul(unity_ObjectToWorld, v.vertex));
#   define LIGHT_ATTENUATION(a)    ( (a._LightCoord.z > 0) * UnitySpotCookie(a._LightCoord) * UnitySpotAttenuate(a._LightCoord.xyz) * SHADOW_ATTENUATION(a) )
#endif

#ifdef DIRECTIONAL
#   define DECLARE_LIGHT_COORDS(idx)
#   define COMPUTE_LIGHT_COORDS(a)
#   define LIGHT_ATTENUATION(a) SHADOW_ATTENUATION(a)
#endif

#ifdef POINT_COOKIE
#   define DECLARE_LIGHT_COORDS(idx) unityShadowCoord3 _LightCoord : TEXCOORD##idx;
#   define COMPUTE_LIGHT_COORDS(a) a._LightCoord = mul(unity_WorldToLight, mul(unity_ObjectToWorld, v.vertex)).xyz;
#   define LIGHT_ATTENUATION(a)    (tex2D(_LightTextureB0, dot(a._LightCoord,a._LightCoord).rr).r * texCUBE(_LightTexture0, a._LightCoord).w * SHADOW_ATTENUATION(a))
#endif

#ifdef DIRECTIONAL_COOKIE
#   define DECLARE_LIGHT_COORDS(idx) unityShadowCoord2 _LightCoord : TEXCOORD##idx;
#   define COMPUTE_LIGHT_COORDS(a) a._LightCoord = mul(unity_WorldToLight, mul(unity_ObjectToWorld, v.vertex)).xy;
#   define LIGHT_ATTENUATION(a)    (tex2D(_LightTexture0, a._LightCoord).w * SHADOW_ATTENUATION(a))
#endif

#define UNITY_LIGHTING_COORDS(idx1, idx2) DECLARE_LIGHT_COORDS(idx1) UNITY_SHADOW_COORDS(idx2)
#define LIGHTING_COORDS(idx1, idx2) DECLARE_LIGHT_COORDS(idx1) SHADOW_COORDS(idx2)
#define UNITY_TRANSFER_LIGHTING(a, coord) COMPUTE_LIGHT_COORDS(a) UNITY_TRANSFER_SHADOW(a, coord)
#define TRANSFER_VERTEX_TO_FRAGMENT(a) COMPUTE_LIGHT_COORDS(a) TRANSFER_SHADOW(a)

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\AutoLight.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\GraniteShaderLib3.cginc---------------


//@IGNORE_BEGIN
/**
*   Graphine Granite Runtime Shader 3.0
*
*   Copyright (c) 2017 Graphine. All Rights Reserved
*
*   This shader library contains all shader functionality to sample
*   Granite tile sets. It should be included in the application-specific
*   shader code.
*
*   --------------
*   FUNCTION CALLS
*   --------------
*
*   To sample a layer from a tile set, first perform the lookup:
*
*       int Granite_Lookup[_UDIM](  in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord,
*                                   in GraniteTranslationTexture translationTable,
*                                   out GraniteLookupData graniteLookupData,
*                                   out gra_Float4 resolveResult);
*
*   It is now possible to sample from any layer in the tile set:
*
*       int Granite_Sample( in GraniteConstantBuffers grCB,
*                           in GraniteLookupData graniteLookupData,
*                           in uint layer,
*                           in GraniteCacheTexture cacheTexture,
*                           out gra_Float4 result);
*
*
*   Depending on you resolve strategy, you will need to do the following
*
*       - Separate Resolver:
*
*           Calculate only the resolve output in the separate resolver pass:
*
*               gra_Float4  Granite_ResolverPixel[_UDIM](   in GraniteConstantBuffers grCB,
*                                                           gra_Float2 inputTexCoord);
*
*           You can supply a dummy resolveResult parameter to the Granite_Lookup function when sampling.
*
*       - MRT Resolver:
*
*           Output the resolveResult parameter given by the Granite_Lookup function to the render target.
*
*       - RWTexture2D Resolver:
*
*           You can write the resolveResult parameter given by the Granite_Lookup function to a RWTexture as follows:
*
*           void Granite_DitherResolveOutput(   in gra_Float4 resolve,
*                                               in RWTexture2D<gra_Float4> resolveTexture,
*                                               in gra_Float2 screenPos,
*                                               in float alpha = 1.0f);
*
*           Don't forget to set GRA_RWTEXTURE2D_SCALE to the actual scale used!
*
*
*   To transform the texture coordinates when using atlassing use:
*
*   gra_Float4  Granite_Transform(  in GraniteStreamingTextureConstantBuffer grSTCB,
*                                   gra_Float2 inputTexCoord);
*
*   If you want to sample from a UDIM streaming texture use the Granite_Lookup_UDIM functions to perform the lookup.
*   If you want to sample with explicit derivatives, use the overloaded 'Lookup' and 'Resolve' functions that take additional ddX and ddY parameters.
*   If you want to sample with explicit level-of-detail, use the overloaded 'Lookup' and 'Resolve' functions that take an additional LOD parameter. Note that these take a special GraniteLodVTData parameter.
*   If you do not want to have texture wrapping of streaming textures when using atlassing, use the overloaded 'PreTransformed' Lookup functions. Call Granite_Transform (or transform the UVs at the CPU) yourself!
* If you want to sample a cube map, use the appropriate 'Lookup'' and 'Sample' functions.
* Pass in the complete texture coordinate ( NOT the fractional part of it) in order to avoid discontinuities!
*
*   ---------------------
*   INTEGRATION CHECKLIST
*   ---------------------
*
*   (1.) Define the following preprocessor directives to configure the shader:
*
*   define GRA_HLSL_3 1/0
*       Enable/disable HLSL 3 syntax
*       Default: disabled
*
*   define GRA_HLSL_4 1/0
*       Enable/disable HLSL 4 syntax
*       Default: disabled
*
*   define GRA_HLSL_5 1/0
*       Enable/disable HLSL 5 syntax
*       Default: disabled
*
*   define GRA_GLSL_120 1/0
*       Enable/disable GLSL version 1.2 syntax
*       Default: disabled
*
*   define GRA_GLSL_130 1/0
*       Enable/disable GLSL version 1.3 syntax
*       Default: disabled
*
*   define GRA_GLSL_330 1/0
*       Enable/disable GLSL version 3.2 syntax
*       Default: disabled
*
*   define GRA_VERTEX_SHADER 1/0
*       Define that we are compiling a vertex shader and limit the instruction set to valid instructions
*       Default: disabled
*
*   define GRA_PIXEL_SHADER 1/0
*       Define that we are compiling a pixel shader and limit the instruction set to valid instructions
*       Default: disabled
*
*   define GRA_HQ_CUBEMAPPING 1/0
*       Enable/disable high quality cubemapping
*       Default: disabled
*
*   define GRA_DEBUG 0
*       Enable/disable debug mode of shader. It recommended to set this to true when first integrating
*       Granite into your engine. It will catch some common mistakes with passing shader parameters etc.
*       Default: disabled
*
*   define GRA_DEBUG_TILES 1/0
*       Enable/disable visual debug output of tiles
*       Default: disabled
*
*   define GRA_BGRA 1/0
*       Enable shader output in BGRA format (else RGBA is used)
*       Default: disabled (rgba)
*
*   define GRA_ROW_MAJOR 1/0
*       Set row major or colum major order of arrays
*       Default: enabled (row major)
*
*   define GRA_64BIT_RESOLVER 1/0
*       Render the resolver pass to a 64bpp resolver instead of a 32 bit per pixel format.
*       Default: disabled
*
*   define GRA_RWTEXTURE2D_SCALE [1,16]
*       The scale we are resolving at in the RWTexture2D. Must match the resolveScale when creation the RWTexture2D resolver.
*       Default: 16
*
*   define GRA_DISABLE_TEX_LOAD 1/0
*       Prefer a texture sample over a texture load (Only has effect on shader models that support the texture load/fetch instruction)
*       Default: 0
*
*   define GRA_PACK_RESOLVE_OUTPUT 1/0
*       When enabled, pack the resolve output values. If disabled, you should pack the returned resolve value using Granite_PackTileId.
*       Use this when performing multiple VT samples and dithering the resolve output.
*       Default: 1
*
*   define GRA_TEXTURE_ARRAY_SUPPORT 1/0
*       Does the graphics API / shader model supports texture arrays ?
*       Default: 1 for shader models supporting texture arrays, else 0
*
*   (2.) Include the Shader library, "GraniteShaderLib.h"
*
*   (3.) Ensure a nearest-point sampler is passed for the translation texture,
*        including the mipmap filter (e.g., D3DTEXF_POINT for D3D9, or
*        NearestMipmapNearest for CG)
*
*/
//@IGNORE_END
// KEEP THESE IGNORE HERE (DO NOT COLLAPSE)
//@IGNORE_BEGIN
#ifndef GRA_HLSL_3
#define GRA_HLSL_3 0
#endif

#ifndef GRA_HLSL_4
#define GRA_HLSL_4 0
#endif

#ifndef GRA_HLSL_5
#define GRA_HLSL_5 0
#endif

#ifndef GRA_GLSL_120
#define GRA_GLSL_120 0
#endif

#ifndef GRA_GLSL_130
#define GRA_GLSL_130 0
#endif

#ifndef GRA_GLSL_330
#define GRA_GLSL_330 0
#endif

#ifndef GRA_VERTEX_SHADER
#define GRA_VERTEX_SHADER 0
#endif

#ifndef GRA_PIXEL_SHADER
#define GRA_PIXEL_SHADER 0
#endif

#ifndef GRA_HQ_CUBEMAPPING
#define GRA_HQ_CUBEMAPPING 0
#endif

#ifndef GRA_DEBUG_TILES
#define GRA_DEBUG_TILES 0
#endif

#ifndef GRA_BGRA
#define GRA_BGRA 0
#endif

#ifndef GRA_ROW_MAJOR
#define GRA_ROW_MAJOR 1
#endif

#ifndef GRA_DEBUG
#define GRA_DEBUG 1
#endif

#ifndef GRA_64BIT_RESOLVER
#define GRA_64BIT_RESOLVER 0
#endif

#ifndef GRA_RWTEXTURE2D_SCALE
#define GRA_RWTEXTURE2D_SCALE 16
#endif

#ifndef GRA_DISABLE_TEX_LOAD
#define GRA_DISABLE_TEX_LOAD 0
#endif

#ifndef GRA_PACK_RESOLVE_OUTPUT
#define GRA_PACK_RESOLVE_OUTPUT 1
#endif

#ifndef GRA_FORCE_SM3
#define GRA_FORCE_SM3 0
#endif

// Temp workaround for PSSL's lack of unorm. Ideally there would be a whole seperate GRA_PSSL backend.
#ifdef GRA_NO_UNORM
    #define GRA_UNORM
#else
    #define GRA_UNORM unorm
#endif

#ifndef GRA_TEXTURE_ARRAY_SUPPORT
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_330 == 1)
        #define GRA_TEXTURE_ARRAY_SUPPORT 1
    #else
        #define GRA_TEXTURE_ARRAY_SUPPORT 0
    #endif
#endif

#define GRA_HLSL_FAMILY ((GRA_HLSL_3 == 1) || (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1))
#define GRA_GLSL_FAMILY ((GRA_GLSL_120 == 1) || (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1))

#if GRA_HLSL_FAMILY
    #define gra_Float2 float2
    #define gra_Float3 float3
    #define gra_Float4 float4
    #define gra_Int3 int3
    #define gra_Float4x4 float4x4
    #define gra_Unroll [unroll]
    #define gra_Branch [branch]
#elif GRA_GLSL_FAMILY
    #if (GRA_VERTEX_SHADER == 0) && (GRA_PIXEL_SHADER ==0)
        #error GLSL requires knowledge of the shader stage! Neither GRA_VERTEX_SHADER or GRA_PIXEL_SHADER are defined!
    #else
        #define gra_Float2 vec2
        #define gra_Float3 vec3
        #define gra_Float4 vec4
        #define gra_Int3 ivec3
        #define gra_Float4x4 mat4
        #define gra_Unroll
        #define gra_Branch
        #if (GRA_VERTEX_SHADER == 1)
            #define ddx
            #define ddy
        #elif (GRA_PIXEL_SHADER == 1)
            #define ddx dFdx
            #define ddy dFdy
        #endif
        #define frac fract
        #define lerp mix
        /** This is not correct (http://stackoverflow.com/questions/7610631/glsl-mod-vs-hlsl-fmod) but it is for our case */
        #define fmod mod
    #endif
#else
    #error unknown shader architecture
#endif

#if (GRA_DISABLE_TEX_LOAD!=1)
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
        #define GRA_LOAD_INSTR 1
    #else
        #define GRA_LOAD_INSTR 0
    #endif
#else
    #define GRA_LOAD_INSTR 0
#endif

//@IGNORE_END

// These are special values that need to be replaced by the shader stripper
//! gra_TranslationTableBias
//! gra_MaxAnisotropyLog2
//! gra_CalcMiplevelDeltaScale
//! gra_CalcMiplevelDeltaScaleX
//! gra_CalcMiplevelDeltaScaleY
//! gra_LodBiasPow2
//! gra_TileToCacheScale
//! gra_TileToCacheBias
//! gra_CacheIdxToCacheScale
//! gra_CacheDeltaScale
//! gra_Level0NumTilesX
//! gra_TextureMagic
//! gra_TextureId
//! gra_TransSwiz
//! gra_NumTilesYScale
//! gra_CutoffLevel
//! gra_MetaTiles
//! gra_NumLevels
//! gra_TileContentInTiles
//! gra_RcpCacheInTiles
//! gra_BorderPixelsRcpCache
//! TranslateCoord
//! PackTileId
//! UnpackTileId
//! CalcMiplevelAnisotropic
//! DrawDebugTiles
//! TranslateCoord
//! numPagesOnLevel
//! cacheCoord
//! deltaScale
//! sampDeltaX
//! sampDeltaY
//! lenDxSqr
//! lenDySqr
//! dMaxSqr
//! dMinSqr
//! maxLevel
//! minLevel
//! anisoLog2
//! borderTemp
//! translationData
//! virtualTilesUv
//! cache
//! ddX
//! ddY
//! faceIdx
//! resultBits
//! swiz
//! level0NumTiles
//! sourceColor
//! borderColor
//! level
//! ddxTc
//! ddyTc
//! availableMips
//! maxAvailableMip
//! lod
//! smoothLevel
//! unused_resolve
//! transform
//! textureCoord
//! resolveResult
//! GranitePrivate
//! pixelLocation
//! screenPos
//! resolveTexture
//! pixelPos
//! writePos
//! dither
//! packedTile
//! cacheOffs
//! offset
//! tileY
//! tileX
//! border
//! tex
//! scale
//! output
//! temp
//! value
//! LOD
//! graniteLookupData
//! numTilesOnLevel
//! cacheX
//! cacheY
//! contTexCoord
//! derivX
//! derivY
//! dVx
//! dVy
//! majorAxis
//! GranitePrivate_CalcMiplevelAnisotropic
//! GranitePrivate_PackTileId
//! GranitePrivate_UnpackTileId
//! GranitePrivate_TranslateCoord
//! GranitePrivate_DrawDebugTiles
//! GranitePrivate_Sample
//! GranitePrivate_SampleLevel
//! GranitePrivate_SampleGrad
//! GranitePrivate_SampleArray
//! GranitePrivate_SampleLevelArray
//! GranitePrivate_SampleGradArray
//! GranitePrivate_SampleBias
//! GranitePrivate_Saturate
//! GranitePrivate_CalculateLevelOfDetail
//! GranitePrivate_Gather
//! GranitePrivate_Load
//! GranitePrivate_FloatAsUint
//! GranitePrivate_Pow2
//! GranitePrivate_CalcMiplevelLinear
//! GranitePrivate_ResolverPixel
//! GranitePrivate_DitherResolveOutput
//! GranitePrivate_CalculateCubemapCoordinates
//! GranitePrivate_MakeResolveOutput
//! exponent
//! gra_TilesetBuffer
//! gra_TilesetBufferInternal
//! gra_TilesetCacheBuffer
//! gra_StreamingTextureCB
//! gra_StreamingTextureCubeCB
//! gra_Transform
//! gra_CubeTransform
//! gra_StreamingTextureTransform
//! gra_StreamingTextureInfo
//! grCB
//! tsCB
//! grSTCB
//! tileTexCoord
//! transforms
//! GranitePrivate_RepeatUV
//! GranitePrivate_UdimUV
//! GranitePrivate_ClampUV
//! GranitePrivate_MirrorUV
//! gra_AssetWidthRcp
//! gra_AssetHeightRcp

// These are special values that need to be defined by the shader stripper
//! d floor
//! d frac
//! d fmod
//! d dot
//! d max
//! d min
//! d log2
//! d ddx
//! d ddy
//! d pow
//! d smoothstep
//! d sqrt
//! d saturate
//! d clamp
//! d float
//! d gra_Float2
//! d gra_Float3
//! d gra_Float4
//! d gra_Float4x4
//! d int
//! d int2
//! d uint2
//! d const
//! d bool
//! d if
//! d else
//! d for
//! d translationTable
//! d cacheTexture
//! d inputTexCoord
//! d tileXY
//! d resolveOutput
//! d result
//! d texCoord
//! d return
//! d paramBlock0
//! d paramBlock1
//! d class
//! d in
//! d out
//! d inout
//! d Texture
//! d TextureArray
//! d Sampler
//! d GraniteCacheTexture
//! d GraniteTranslationTexture
//! d GraniteLookupData
//! d GraniteLODLookupData
//! d RWTexture2D
//! d tex2Dlod
//! d tex2D
//! d tex2Dbias
//! d dX
//! d dY
//! d textureCoordinates
//! d translationTableData
//! d layer
//! d cacheLevel
//! d StreamingTextureConstantBuffer
//! d StreamingTextureCubeConstantBuffer
//! d TilesetConstantBuffer
//! d GraniteConstantBuffers
//! d GraniteCubeConstantBuffers

//@IGNORE_BEGIN

/**
    a cross API texture handle
*/
#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
    struct GraniteTranslationTexture
    {
        SamplerState Sampler;
        Texture2D Texture;
    };
    struct GraniteCacheTexture
    {
        SamplerState Sampler;

        #if GRA_TEXTURE_ARRAY_SUPPORT
            Texture2DArray TextureArray;
        #else
            Texture2D Texture;
        #endif
    };
#elif (GRA_HLSL_3 == 1) || (GRA_GLSL_120 == 1) || (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
    #define GraniteTranslationTexture sampler2D

    #if GRA_TEXTURE_ARRAY_SUPPORT
        #define GraniteCacheTexture sampler2DArray
    #else
        #define GraniteCacheTexture sampler2D
    #endif

#else
    #error unknow shader archtecture
#endif

/**
        Struct defining the constant buffer for each streaming texture.
        Use IStreamingTexture::GetConstantBuffer to fill this struct.
*/
struct GraniteStreamingTextureConstantBuffer
{
    #define _grStreamingTextureCBSize 2
    gra_Float4 data[_grStreamingTextureCBSize];
};

/**
        Struct defining the constant buffer for each cube streaming texture.
        Use multiple calls to IStreamingTexture::GetConstantBuffer this struct (one call for each face).
    */
struct GraniteStreamingTextureCubeConstantBuffer
{
    #define _grStreamingTextureCubeCBSize 6
    GraniteStreamingTextureConstantBuffer data[_grStreamingTextureCubeCBSize];
};

/**
        Struct defining the constant buffer for each tileset.
        Use ITileSet::GetConstantBuffer to fill this struct.
*/
struct GraniteTilesetConstantBuffer
{
    #define _grTilesetCBSize 2
    gra_Float4x4 data[_grTilesetCBSize];
};

/**
        Utility struct used by the shaderlib to wrap up all required constant buffers needed to perform a VT lookup/sample.
    */
struct GraniteConstantBuffers
{
    GraniteTilesetConstantBuffer                        tilesetBuffer;
    GraniteStreamingTextureConstantBuffer   streamingTextureBuffer;
};

/**
        Utility struct used by the shaderlib to wrap up all required constant buffers needed to perform a Cube VT lookup/sample.
    */
struct GraniteCubeConstantBuffers
{
    GraniteTilesetConstantBuffer                                tilesetBuffer;
    GraniteStreamingTextureCubeConstantBuffer   streamingTextureCubeBuffer;
};

/**
    The Granite lookup data for the different sampling functions.
*/

// Granite lookup data for automatic mip level selecting sampling
struct GraniteLookupData
{
    gra_Float4 translationTableData;
    gra_Float2 textureCoordinates;
    gra_Float2 dX;
    gra_Float2 dY;
};

// Granite lookup data for explicit level-of-detail sampling
struct GraniteLODLookupData
{
    gra_Float4 translationTableData;
    gra_Float2 textureCoordinates;
    float cacheLevel;
};
//@IGNORE_END

// public interface
//@IGNORE_BEGIN
/**
    The public interface of all Granite related shader calls
*/

/**
*   Transform the texture coordinates from [0...1]x[0...1] texture space in [0...1]x[0...1] tile set space.
*
*   @param grSTCB The Granite Shader Runtime streaming texture parameter block
*   @param textureCoord The texture coord that will be transformed
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float2 Granite_Transform(in GraniteStreamingTextureConstantBuffer grSTCB, in gra_Float2 textureCoord);


/**
    Merge two resolve output values into one.
    This can be used when you need multiple tile sets on the same geometry ( != multiple layers from one tile set).
*/
gra_Float4 Granite_MergeResolveOutputs(in gra_Float4 resolve0, in gra_Float4 resolve1, in gra_Float2 pixelLocation);


/**
    Convert the 64bit created resolver to 32bit resolver data.
    This is useful when debugging 64bit resolver resources.
    Only use in debug mode !
*/
gra_Float4 Granite_DebugPackedTileId64(in gra_Float4 PackedTile);


/**
    Convert the packed normal data to a normalized three-dimensional normal.

    @param PackedNormal The normal data sampled by Granite_Sample

    @return The normalized three-dimensional normal.
*/
gra_Float3 Granite_UnpackNormal(in gra_Float4 packedNormal);

/**
    Convert the packed normal data to a normalized three-dimensional normal.

    @param PackedNormal The normal data sampled by Granite_Sample
    @param scale scale to apply to the normal during unpacking

    @return The normalized three-dimensional normal.
*/
gra_Float3 Granite_UnpackNormal(in gra_Float4 packedNormal, float scale);

#if GRA_HLSL_FAMILY
/**
    Applies an additional resolution offset to the parameter block.
    @param INtsCB The Granite Shader Runtime tile set parameter block
    @param resolutionOffsetPow2 The additional resolution offset calculated as follows: resolutionOffsetPow2 = 2^resolutionOffset.
    @return The Granite Shader Runtime tile set parameter block transformed with the additional resolution offset.
*/
GraniteTilesetConstantBuffer Granite_ApplyResolutionOffset(in GraniteTilesetConstantBuffer INtsCB, in float resolutionOffsetPow2);

/**
    Applies an user provided maximum anisotropy to the parameter block.
    @param INtsCB The Granite Shader Runtime tile set parameter block
    @param resolutionOffsetPow2 The new anisotropy calculated as follows: maxAnisotropyLog2 = log2(maxAnisotropy).
    @return The Granite Shader Runtime tile set parameter block transformed with the new maximum anisotropy.
*/
GraniteTilesetConstantBuffer Granite_SetMaxAnisotropy(in GraniteTilesetConstantBuffer INtsCB, in float maxAnisotropyLog2);
#else
/**
    Applies an additional resolution offset to the parameter block.

    @param INtsCB The Granite Shader Runtime tile set parameter block
    @param resolutionOffsetPow2 The additional resolution offset calculated as follows: resolutionOffsetPow2 = 2^resolutionOffset.
*/
void Granite_ApplyResolutionOffset(inout GraniteTilesetConstantBuffer tsCB, in float resolutionOffsetPow2);

/**
    Applies an user provided maximum anisotropy to the parameter block.

    @param tsCB The Granite Shader Runtime tile set parameter block
    @param resolutionOffsetPow2 The new anisotropy calculated as follows: maxAnisotropyLog2 = log2(maxAnisotropy).
*/
void Granite_SetMaxAnisotropy(inout GraniteTilesetConstantBuffer tsCB, in float maxAnisotropyLog2);
#endif
/**
    Pack the (unpacked) returned resolve output.
    Should only be used when GRA_PACK_RESOLVE_OUTPUT equals 0.

    @param paramBlock The Granite Shader Runtime parameter block
    @param unpackedTileID The TileID you wish to pack

    @return The packed tile ID
*/
gra_Float4 Granite_PackTileId(in gra_Float4 unpackedTileID);

#if (GRA_HLSL_5 == 1)
/**
    Pack the (unpacked) returned resolve output.
    Should only be used when GRA_PACK_RESOLVE_OUTPUT equals 0.

    @param resolve The resolve output
    @param resolveTexture RWTexture2D resource where the resolve output is written to
    @param screenPos The pixel coordinates of the pixel on the screen (SV_Position)
*/
void Granite_DitherResolveOutput(in gra_Float4 resolve, in RWTexture2D<GRA_UNORM gra_Float4> resolveTexture, in gra_Float2 screenPos, in float alpha /*= 1.0f*/);
#endif

/**
    Get the dimensions (in pixels) of a streaming texture

    @param grSTCB The Granite Shader Runtime streaming texture parameter block

    @return The streaming texture dimensions
*/
gra_Float2 Granite_GetTextureDimensions(in GraniteStreamingTextureConstantBuffer grSTCB);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Linear(   in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Linear(  in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Linear(   in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Linear(  in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Linear( in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Linear( in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed UDIM tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM_Linear(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a UDIM tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM_Linear( in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed UDIM tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM_Linear(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a UDIM tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM_Linear( in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM_Linear(    in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM_Linear(    in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed Clamped tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp_Linear( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a Clamped tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp_Linear(    in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed Clamped tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp_Linear( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a Clamped tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp_Linear(    in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp_Linear(   in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp_Linear(   in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Anisotropic(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Anisotropic( in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Anisotropic(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Anisotropic( in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Anisotropic(    in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Anisotropic(    in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed UDIM tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM_Anisotropic( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a UDIM tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed UDIM tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM_Anisotropic( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a UDIM tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM_Anisotropic(   in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM_Anisotropic(   in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed Clamped tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a Clamped tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp_Anisotropic(   in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed Clamped tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a Clamped tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp_Anisotropic(   in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp_Anisotropic(  in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp_Anisotropic(  in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a preTransformed tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed(  in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a tile set with explicit level-of-detail using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup( in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube(    in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a preTransformed UDIM tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM( in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a UDIM tile set with explicit level-of-detail using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM(    in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM(   in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a preTransformed Clamped tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp(    in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a Clamped tile set with explicit level-of-detail using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp(   in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp(  in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup using wrapped texture addressing (tiling) and dynamic udim selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Dynamic_Linear(  in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup using wrapped texture addressing (tiling) and dynamic udim selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @param LOD The Level of Detail to sample.
*   @param graniteLODLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/

int Granite_Lookup_Dynamic_Linear(  in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM, in float LOD,
                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup using wrapped texture addressing (tiling) and dynamic udim selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Dynamic_Anisotropic( in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup using wrapped texture addressing (tiling) and dynamic udim selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @param LOD The Level of Detail to sample.
*   @param graniteLODLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/

int Granite_Lookup_Dynamic_Anisotropic( in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM, in float LOD,
                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Samples a single layer of a tile set.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param graniteLookupData The Granite Lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample( in GraniteConstantBuffers grCB,
                                        in GraniteLookupData graniteLookupData,
                                        in GraniteCacheTexture cacheTexture, in int layer,
                                        out gra_Float4 result);

/**
*   Samples a single layer of a cube tile set.
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param graniteLookupData The Granite Lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample( in GraniteCubeConstantBuffers grCB,
                                        in GraniteLookupData graniteLookupData,
                                        in GraniteCacheTexture cacheTexture, in int layer,
                                        out gra_Float4 result);

/**
*   Samples a single layer of a tile set with high quality filtering
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param graniteLookupData The Granite Lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample_HQ(  in GraniteConstantBuffers grCB,
                                                in GraniteLookupData graniteLookupData,
                                                in GraniteCacheTexture cacheTexture, in int layer,
                                                out gra_Float4 result);

/**
*   Samples a single layer of a cube tile set with high quality filtering
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param graniteLookupData The Granite Lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample_HQ(  in GraniteCubeConstantBuffers grCB,
                                                in GraniteLookupData graniteLookupData,
                                                in GraniteCacheTexture cacheTexture, in int layer,
                                                out gra_Float4 result);

/**
*   Samples a single layer of a tile set with explicit level-of-detail.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param graniteLookupData The Granite lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample( in GraniteConstantBuffers grCB,
                                        in GraniteLODLookupData graniteLookupData,
                                        in GraniteCacheTexture cacheTexture, in int layer,
                                        out gra_Float4 result);

/**
*   Samples a single layer of a cube tile set with explicit level-of-detail.
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param graniteLookupData The Granite lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample( in GraniteCubeConstantBuffers grCB,
                                        in GraniteLODLookupData graniteLookupData,
                                        in GraniteCacheTexture cacheTexture, in int layer,
                                        out gra_Float4 result);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output with explicit level-of-detail
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output of a cubemap with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output with explicit level-of-detail
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output of a cubemap with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output with explicit level-of-detail
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output of a cubemap with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the resolver output using wrapping texture addressing with dynamic UDIM selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Dynamic_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in bool asUDIM);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the resolver output using wrapping texture addressing with dynamic UDIM selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Dynamic_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in bool asUDIM);

//@IGNORE_END


/*
    END OF PUBLIC INTERFACE
    Everything below this point should be treated as private to GraniteShaderLib.h
*/

//@INSERT_DEFINES
#define gra_TilesetBuffer grCB.tilesetBuffer
#define gra_TilesetBufferInternal  tsCB.data[0]
#define gra_TilesetCacheBuffer  tsCB.data[1]

#define gra_StreamingTextureCB grCB.streamingTextureBuffer
#define gra_StreamingTextureCubeCB grCB.streamingTextureCubeBuffer

#define gra_Transform grCB.streamingTextureBuffer.data[0]
#define gra_CubeTransform grCB.streamingTextureCubeBuffer.data

#define gra_StreamingTextureTransform grSTCB.data[0]
#define gra_StreamingTextureInfo grSTCB.data[1]

#define gra_NumLevels gra_StreamingTextureInfo.x
#define gra_AssetWidthRcp gra_StreamingTextureInfo.y
#define gra_AssetHeightRcp gra_StreamingTextureInfo.z

#if GRA_ROW_MAJOR == 1

    #define gra_TranslationTableBias            gra_TilesetBufferInternal[0][0]
    #define gra_MaxAnisotropyLog2               gra_TilesetBufferInternal[1][0]
    #define gra_CalcMiplevelDeltaScale      gra_Float2(gra_TilesetBufferInternal[2][0], gra_TilesetBufferInternal[3][0])
    #define gra_CalcMiplevelDeltaScaleX     gra_TilesetBufferInternal[2][0]
    #define gra_CalcMiplevelDeltaScaleY     gra_TilesetBufferInternal[3][0]
    #define gra_LodBiasPow2                             gra_TilesetBufferInternal[0][1]
    #define gra_TileContentInTiles              gra_Float2(gra_TilesetBufferInternal[0][2], gra_TilesetBufferInternal[1][2])
    #define gra_Level0NumTilesX                     gra_TilesetBufferInternal[0][3]
    #define gra_NumTilesYScale                      gra_TilesetBufferInternal[1][3]
    #define gra_TextureMagic                            gra_TilesetBufferInternal[2][3]
    #define gra_TextureId                               gra_TilesetBufferInternal[3][3]

    #define gra_RcpCacheInTiles(l)              gra_Float2(gra_TilesetCacheBuffer[0][l], gra_TilesetCacheBuffer[1][l])
    #define gra_BorderPixelsRcpCache(l)     gra_Float2(gra_TilesetCacheBuffer[2][l], gra_TilesetCacheBuffer[3][l])

#else

    #define gra_TranslationTableBias            gra_TilesetBufferInternal[0][0]
    #define gra_MaxAnisotropyLog2               gra_TilesetBufferInternal[0][1]
    #define gra_CalcMiplevelDeltaScale      gra_Float2(gra_TilesetBufferInternal[0][2], gra_TilesetBufferInternal[0][3])
    #define gra_CalcMiplevelDeltaScaleX     gra_TilesetBufferInternal[0][2]
    #define gra_CalcMiplevelDeltaScaleY     gra_TilesetBufferInternal[0][3]
    #define gra_LodBiasPow2                             gra_TilesetBufferInternal[1][0]
    #define gra_TileContentInTiles              gra_Float2(gra_TilesetBufferInternal[2][0], gra_TilesetBufferInternal[2][1])
    #define gra_Level0NumTilesX                     gra_TilesetBufferInternal[3][0]
    #define gra_NumTilesYScale                      gra_TilesetBufferInternal[3][1]
    #define gra_TextureMagic                            gra_TilesetBufferInternal[3][2]
    #define gra_TextureId                               gra_TilesetBufferInternal[3][3]

    #define gra_RcpCacheInTiles(l)              gra_Float2(gra_TilesetCacheBuffer[l][0], gra_TilesetCacheBuffer[l][1])
    #define gra_BorderPixelsRcpCache(l)     gra_Float2(gra_TilesetCacheBuffer[l][2], gra_TilesetCacheBuffer[l][3])

#endif

#if (GRA_GLSL_120==1)
    // Extension needed for texture2DLod
    #extension GL_ARB_shader_texture_lod : enable
    // Extensions needed fot texture2DGrad
    #extension GL_EXT_gpu_shader4 : enable
    // Extensions needed for bit manipulation
    #extension GL_ARB_shader_bit_encoding : enable
#endif


#if (GRA_TEXTURE_ARRAY_SUPPORT==1)
    gra_Float4 GranitePrivate_SampleArray(in GraniteCacheTexture tex, in gra_Float3 texCoord)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.TextureArray.Sample(tex.Sampler, texCoord);
    #elif (GRA_GLSL_330 == 1)
        return texture(tex, texCoord);
    #else
        #error using unsupported function
    #endif
    }

    gra_Float4 GranitePrivate_SampleGradArray(in GraniteCacheTexture tex, in gra_Float3 texCoord, in gra_Float2 dX, in gra_Float2 dY)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.TextureArray.SampleGrad(tex.Sampler,texCoord,dX,dY);
    #elif (GRA_GLSL_330 == 1)
        return textureGrad(tex, texCoord, dX, dY);
    #else
        #error using unsupported function
    #endif
    }

    gra_Float4 GranitePrivate_SampleLevelArray(in GraniteCacheTexture tex, in gra_Float3 texCoord, in float level)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.TextureArray.SampleLevel(tex.Sampler, texCoord, level);
    #elif (GRA_GLSL_330 == 1)
        return textureLod(tex, texCoord, level);
    #else
        #error using unsupported function
    #endif
    }
#else
    gra_Float4 GranitePrivate_Sample(in GraniteCacheTexture tex, in gra_Float2 texCoord)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.Texture.Sample(tex.Sampler,texCoord);
    #elif (GRA_HLSL_3 == 1)
        return tex2D(tex,texCoord);
    #elif (GRA_GLSL_120 == 1) || (GRA_GLSL_130 == 1)
        return texture2D(tex, texCoord);
    #elif (GRA_GLSL_330 == 1)
        return texture(tex, texCoord);
    #endif
    }

    gra_Float4 GranitePrivate_SampleLevel(in GraniteCacheTexture tex, in gra_Float2 texCoord, in float level)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.Texture.SampleLevel(tex.Sampler, texCoord, level);
    #elif (GRA_HLSL_3 == 1)
        return tex2Dlod(tex,gra_Float4(texCoord,0.0,level));
    #elif (GRA_GLSL_120 == 1)
        return texture2DLod(tex, texCoord, level);
    #elif (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
        return textureLod(tex, texCoord, level);
    #endif
    }

    gra_Float4 GranitePrivate_SampleGrad(in GraniteCacheTexture tex, in gra_Float2 texCoord, in gra_Float2 dX, in gra_Float2 dY)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.Texture.SampleGrad(tex.Sampler,texCoord,dX,dY);
    #elif (GRA_HLSL_3 == 1)
        return tex2D(tex,texCoord,dX,dY);
    #elif (GRA_GLSL_120 == 1)
        return texture2DGrad(tex, texCoord, dX, dY);
    #elif (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
        return textureGrad(tex, texCoord, dX, dY);
    #endif
    }
#endif //#if (GRA_TEXTURE_ARRAY_SUPPORT==1)

#if (GRA_LOAD_INSTR==1)
gra_Float4 GranitePrivate_Load(in GraniteTranslationTexture tex, in gra_Int3 location)
{
#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
    return tex.Texture.Load(location);
#elif (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
    return texelFetch(tex, location.xy, location.z);
#elif (GRA_HLSL_3 == 1) || (GRA_GLSL_120 == 1)
    #error using unsupported function
#endif
}
#endif

//work-around shader compiler bug
//compiler gets confused with GranitePrivate_SampleLevel taking a GraniteCacheTexture as argument when array support is disabled
//Without array support, GraniteCacheTexture and GraniteTranslationTexture are the same (but still different types!)
//compiler is confused (ERR_AMBIGUOUS_FUNCTION_CALL). Looks like somebody is over enthusiastic optimizing...
gra_Float4 GranitePrivate_SampleLevel_Translation(in GraniteTranslationTexture tex, in gra_Float2 texCoord, in float level)
{
#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
    return tex.Texture.SampleLevel(tex.Sampler, texCoord, level);
#elif (GRA_HLSL_3 == 1)
    return tex2Dlod(tex,gra_Float4(texCoord,0.0,level));
#elif (GRA_GLSL_120 == 1)
    return texture2DLod(tex, texCoord, level);
#elif (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
    return textureLod(tex, texCoord, level);
#endif
}

float GranitePrivate_Saturate(in float value)
{
#if GRA_HLSL_FAMILY
    return saturate(value);
#elif GRA_GLSL_FAMILY
    return clamp(value, 0.0f, 1.0f);
#endif
}

#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_330 == 1)
uint GranitePrivate_FloatAsUint(float value)
{
#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
    return asuint(value);
#elif (GRA_GLSL_330 == 1)
    return floatBitsToUint(value);
#endif
}
#endif

// IOS gl shader compiler doesn't like overloads with int and uint. It thinks they are the same giving the error
// redefinition of 'GranitePrivate_Pow2'.
// So we only declare the one we exactly need for the given platform.
#if (GRA_FORCE_SM3 == 0) && ((GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_330 == 1))
float GranitePrivate_Pow2(uint exponent)
{
#if GRA_HLSL_FAMILY
    return pow(2.0, exponent);
#else
    return pow(2.0, float(exponent));
#endif
}
#else
float GranitePrivate_Pow2(int exponent)
{
#if GRA_HLSL_FAMILY
    return pow(2.0, exponent);
#else
    return pow(2.0, float(exponent));
#endif
}
#endif

gra_Float2 GranitePrivate_RepeatUV(in gra_Float2 uv, in GraniteStreamingTextureConstantBuffer grSTCB)
{
    return frac(uv);
}

gra_Float2 GranitePrivate_UdimUV(in gra_Float2 uv, in GraniteStreamingTextureConstantBuffer grSTCB)
{
    return uv;
}

gra_Float2 GranitePrivate_ClampUV(in gra_Float2 uv, in GraniteStreamingTextureConstantBuffer grSTCB)
{
  gra_Float2 epsilon2 = gra_Float2(gra_AssetWidthRcp, gra_AssetHeightRcp);
  return clamp(uv, epsilon2, gra_Float2(1,1) - epsilon2);
}

gra_Float2 GranitePrivate_MirrorUV(in gra_Float2 uv, in GraniteStreamingTextureConstantBuffer grSTCB)
{
    gra_Float2 t = frac(uv*0.5)*2.0;
    gra_Float2 l = gra_Float2(1.0,1.0);
    return l-abs(t-l);
}

// function definitons for private functions
gra_Float4 GranitePrivate_PackTileId(in gra_Float2 tileXY, in float level, in float textureID);

gra_Float4 Granite_DebugPackedTileId64(in gra_Float4 PackedTile)
{
#if GRA_64BIT_RESOLVER
    gra_Float4 output;

    const float scale = 1.0f / 65535.0f;
    gra_Float4 temp = PackedTile / scale;

    output.x = fmod(temp.x, 256.0f);
    output.y = floor(temp.x / 256.0f) + fmod(temp.y, 16.0f) * 16.0f;
    output.z = floor(temp.y / 16.0f);
    output.w = temp.z + temp.a * 16.0f;

    return gra_Float4
    (
        (float)output.x / 255.0f,
        (float)output.y / 255.0f,
        (float)output.z / 255.0f,
        (float)output.w / 255.0f
    );
#else
    return PackedTile;
#endif
}

gra_Float3 Granite_UnpackNormal(in gra_Float4 PackedNormal, float scale)
{
    gra_Float2 reconstructed = gra_Float2(PackedNormal.x * PackedNormal.a, PackedNormal.y) * 2.0f - 1.0f;
    reconstructed *= scale;
    float z = sqrt(1.0f - GranitePrivate_Saturate(dot(reconstructed, reconstructed)));
    return gra_Float3(reconstructed, z);
}

gra_Float3 Granite_UnpackNormal(in gra_Float4 PackedNormal)
{
    return Granite_UnpackNormal(PackedNormal, 1.0);
}

#if GRA_HLSL_FAMILY
GraniteTilesetConstantBuffer Granite_ApplyResolutionOffset(in GraniteTilesetConstantBuffer INtsCB, in float resolutionOffsetPow2)
{
    GraniteTilesetConstantBuffer tsCB = INtsCB;
    gra_LodBiasPow2 *= resolutionOffsetPow2;
    //resolutionOffsetPow2 *= resolutionOffsetPow2; //Square it before multiplying it in below
    gra_CalcMiplevelDeltaScaleX *= resolutionOffsetPow2;
    gra_CalcMiplevelDeltaScaleY *= resolutionOffsetPow2;
    return tsCB;
}

GraniteTilesetConstantBuffer Granite_SetMaxAnisotropy(in GraniteTilesetConstantBuffer INtsCB, in float maxAnisotropyLog2)
{
    GraniteTilesetConstantBuffer tsCB = INtsCB;
    gra_MaxAnisotropyLog2 = min(gra_MaxAnisotropyLog2, maxAnisotropyLog2);
    return tsCB;
}
#else
void Granite_ApplyResolutionOffset(inout GraniteTilesetConstantBuffer tsCB, in float resolutionOffsetPow2)
{
    gra_LodBiasPow2 *= resolutionOffsetPow2;
    //resolutionOffsetPow2 *= resolutionOffsetPow2; //Square it before multiplying it in below
    gra_CalcMiplevelDeltaScaleX *= resolutionOffsetPow2;
    gra_CalcMiplevelDeltaScaleY *= resolutionOffsetPow2;
}

void Granite_SetMaxAnisotropy(inout GraniteTilesetConstantBuffer tsCB, in float maxAnisotropyLog2)
{
    gra_MaxAnisotropyLog2 = min(gra_MaxAnisotropyLog2, maxAnisotropyLog2);
}
#endif

gra_Float2 Granite_Transform(in GraniteStreamingTextureConstantBuffer grSTCB, in gra_Float2 textureCoord)
{
    return textureCoord * gra_StreamingTextureTransform.zw  + gra_StreamingTextureTransform.xy;
}

gra_Float4 Granite_MergeResolveOutputs(in gra_Float4 resolve0, in gra_Float4 resolve1, in gra_Float2 pixelLocation)
{
    gra_Float2 screenPos = frac(pixelLocation * 0.5f);
    bool dither = (screenPos.x != screenPos.y);
    return (dither) ? resolve0 : resolve1;
}

gra_Float4 Granite_PackTileId(in gra_Float4 unpackedTileID)
{
    return GranitePrivate_PackTileId(unpackedTileID.xy, unpackedTileID.z, unpackedTileID.w);
}

#if (GRA_HLSL_5 == 1)
void Granite_DitherResolveOutput(in gra_Float4 resolve, in RWTexture2D<GRA_UNORM gra_Float4> resolveTexture, in gra_Float2 screenPos, in float alpha)
{
    const uint2 pixelPos = int2(screenPos);
    const uint2 pixelLocation = pixelPos % GRA_RWTEXTURE2D_SCALE;
    bool dither = (pixelLocation.x  == 0) && (pixelLocation.y  == 0);
    uint2 writePos = pixelPos / GRA_RWTEXTURE2D_SCALE;

    if ( alpha == 0 )
    {
        dither = false;
    }
    else if (alpha != 1.0)
    {
        // Do a 4x4 dither patern so alternating pixels resolve to the first or the second texture
        gra_Float2 pixelLocationAlpha = frac(screenPos * 0.25f); // We don't scale after the frac so this will give coords 0, 0.25, 0.5, 0.75
        int pixelId = (int)(pixelLocationAlpha.y * 16 + pixelLocationAlpha.x * 4); //faster as a dot2 ?

        // Clamp
        // This ensures that for example alpha=0.95 still resolves some tiles of the surfaces behind it
        // and alpha=0.05 still resolves some tiles of this surface
        alpha = min(max(alpha, 0.0625), 0.9375);

        // Modern hardware supports array indexing with per pixel varying indexes
        // on old hardware this will be expanded to a conditional tree by the compiler
        const float thresholdMaxtrix[16] = {    1.0f / 17.0f, 9.0f / 17.0f, 3.0f / 17.0f, 11.0f / 17.0f,
                                                    13.0f / 17.0f,  5.0f / 17.0f, 15.0f / 17.0f, 7.0f / 17.0f,
                                                    4.0f / 17.0f, 12.0f / 17.0f, 2.0f / 17.0f, 10.0f / 17.0f,
                                                    16.0f / 17.0f, 8.0f / 17.0f, 14.0f / 17.0f, 6.0f / 17.0f};
        float threshold = thresholdMaxtrix[pixelId];

        if (alpha < threshold)
        {
            dither = false;
        }
    }

    gra_Branch if (dither)
    {
#if (GRA_PACK_RESOLVE_OUTPUT==0)
        resolveTexture[writePos] = Granite_PackTileId(resolve);
#else
        resolveTexture[writePos] = resolve;
#endif
    }
}
#endif

float GranitePrivate_CalcMiplevelAnisotropic(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB, in gra_Float2 ddxTc, in gra_Float2 ddyTc)
{
    // Calculate the required mipmap level, this uses a similar
    // formula as the GL spec.
    // To reduce sqrt's and log2's we do some stuff in squared space here and further below in log space
    // i.e. we wait with the sqrt untill we can do it for 'free' later during the log2

  ddxTc *= gra_CalcMiplevelDeltaScale;
  ddyTc *= gra_CalcMiplevelDeltaScale;

  float lenDxSqr = dot(ddxTc, ddxTc);
    float lenDySqr = dot(ddyTc, ddyTc);
    float dMaxSqr = max(lenDxSqr, lenDySqr);
    float dMinSqr = min(lenDxSqr, lenDySqr);

    // Calculate mipmap levels directly from sqared distances. This uses log2(sqrt(x)) = 0.5 * log2(x) to save some sqrt's
    float maxLevel = 0.5 * log2( dMaxSqr );
    float minLevel = 0.5 * log2( dMinSqr );

    // Calculate the log2 of the anisotropy and clamp it by the max supported. This uses log2(a/b) = log2(a)-log2(b) and min(log(a),log(b)) = log(min(a,b))
    float anisoLog2 = maxLevel - minLevel;
    anisoLog2 = min( anisoLog2, gra_MaxAnisotropyLog2 );

    // Adjust for anisotropy & clamp to level 0
    float result = max(maxLevel - anisoLog2 - 0.5f, 0.0f); //Subtract 0.5 to compensate for trilinear mipmapping

    // Added clamping to avoid "hot pink" on small tilesets that try to sample past the 1x1 tile miplevel
    // This happens if you for example import a relatively small texture and zoom out
    return min(result, gra_NumLevels);
}

float GranitePrivate_CalcMiplevelLinear(in  GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB, in gra_Float2 ddxTc, in gra_Float2 ddyTc)
{
    // Calculate the required mipmap level, this uses a similar
    // formula as the GL spec.
    // To reduce sqrt's and log2's we do some stuff in squared space here and further below in log space
    // i.e. we wait with the sqrt untill we can do it for 'free' later during the log2

  ddxTc *= gra_CalcMiplevelDeltaScale;
  ddyTc *= gra_CalcMiplevelDeltaScale;

    float lenDxSqr = dot(ddxTc, ddxTc);
    float lenDySqr = dot(ddyTc, ddyTc);
    float dMaxSqr = max(lenDxSqr, lenDySqr);

    // Calculate mipmap levels directly from squared distances. This uses log2(sqrt(x)) = 0.5 * log2(x) to save some sqrt's
    float maxLevel = 0.5 * log2(dMaxSqr) - 0.5f;  //Subtract 0.5 to compensate for trilinear mipmapping

    return clamp(maxLevel, 0.0f, gra_NumLevels);
}

gra_Float4 GranitePrivate_PackTileId(in gra_Float2 tileXY, in float level, in float textureID)
{
#if GRA_64BIT_RESOLVER == 0
    gra_Float4 resultBits;

    resultBits.x = fmod(tileXY.x, 256.0f);
    resultBits.y = floor(tileXY.x / 256.0f) + fmod(tileXY.y, 32.0f) * 8.0f;
    resultBits.z = floor(tileXY.y / 32.0f) + fmod(level, 4.0f) * 64.0f;
    resultBits.w = floor(level / 4.0f) + textureID * 4.0f;

    const float scale = 1.0f / 255.0f;

#if GRA_BGRA == 0
    return scale * gra_Float4
    (
        float(resultBits.x),
        float(resultBits.y),
        float(resultBits.z),
        float(resultBits.w)
    );
#else
    return scale * gra_Float4
    (
        float(resultBits.z),
        float(resultBits.y),
        float(resultBits.x),
        float(resultBits.w)
    );
#endif
#else
    const float scale = 1.0f / 65535.0f;
    return gra_Float4(tileXY.x, tileXY.y, level, textureID) * scale;
#endif

}

gra_Float4 GranitePrivate_UnpackTileId(in gra_Float4 packedTile)
{
    gra_Float4 swiz;
#if GRA_BGRA == 0
    swiz = packedTile;
#else
    swiz = packedTile.zyxw;
#endif
    swiz *= 255.0f;

    float tileX = swiz.x + fmod(swiz.y, 16.0f) * 256.0f;
    float tileY = floor(swiz.y / 16.0f) + swiz.z * 16.0f;
    float level = fmod(swiz.w, 16.0f);
    float tex   = floor(swiz.w /  16.0f);

    return gra_Float4(tileX, tileY, level, tex);
}

gra_Float3 GranitePrivate_TranslateCoord(in GraniteTilesetConstantBuffer tsCB, in gra_Float2 inputTexCoord, in gra_Float4 translationData, in int layer, out gra_Float2 numPagesOnLevel)
{
#if (GRA_FORCE_SM3 == 0) && ((GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_330 == 1))
    // The translation table contains uint32_t values so we have to get to the individual bits of the float data
    uint data = GranitePrivate_FloatAsUint(translationData[layer]);

    // Slice Index: 7 bits, Cache X: 10 bits, Cache Y: 10 bits, Tile Level: 4 bits
    uint slice  = (data >> 24u) & 0x7Fu;
    uint cacheX = (data >> 14u) & 0x3FFu;
    uint cacheY = (data >> 4u) & 0x3FFu;
    uint revLevel = data & 0xFu;
#else
    // The translation table contains integer float values so we have to cast the float value to an int (which works up to 24-bit integer values)
    int data = int(translationData[layer]);

    int slice  = 0;
    int cacheX = (data / 16384);
    int cacheY = (data % 16384) / 16;
    int revLevel = (data % 16);
#endif

    gra_Float2 numTilesOnLevel;
    numTilesOnLevel.x = GranitePrivate_Pow2(revLevel);
    numTilesOnLevel.y = numTilesOnLevel.x * gra_NumTilesYScale;

    gra_Float2 tileTexCoord = frac(inputTexCoord * numTilesOnLevel);

    gra_Float2 tileTexCoordCache = tileTexCoord * gra_TileContentInTiles + gra_Float2(cacheX, cacheY);
    gra_Float3 final = gra_Float3(tileTexCoordCache * gra_RcpCacheInTiles(layer) + gra_BorderPixelsRcpCache(layer), slice);

    numPagesOnLevel = numTilesOnLevel * gra_TileContentInTiles * gra_RcpCacheInTiles(layer);

    return final;
}

gra_Float4 GranitePrivate_DrawDebugTiles(in gra_Float4 sourceColor, in gra_Float2 textureCoord, in gra_Float2 numPagesOnLevel)
{
    // Calculate the border values
    gra_Float2 cacheOffs = frac(textureCoord * numPagesOnLevel);
    float borderTemp = max(cacheOffs.x, 1.0-cacheOffs.x);
    borderTemp = max(max(cacheOffs.y, 1.0-cacheOffs.y), borderTemp);
    float border = smoothstep(0.98, 0.99, borderTemp);

    // White
    gra_Float4 borderColor = gra_Float4(1,1,1,1);

    //Lerp it over the source color
    return lerp(sourceColor, borderColor, border);
}

gra_Float4 GranitePrivate_MakeResolveOutput(in GraniteTilesetConstantBuffer tsCB, in gra_Float2 tileXY, in float level)
{
#if GRA_PACK_RESOLVE_OUTPUT
    return GranitePrivate_PackTileId(tileXY, level, gra_TextureId);
#else
    return gra_Float4(tileXY, level, gra_TextureId);
#endif
}

gra_Float4 GranitePrivate_ResolverPixel(in GraniteTilesetConstantBuffer tsCB, in gra_Float2 inputTexCoord, in float LOD)
{
    float level = floor(LOD + 0.5f);

    // Number of tiles on level zero
    gra_Float2 level0NumTiles;
    level0NumTiles.x = gra_Level0NumTilesX;
    level0NumTiles.y = gra_Level0NumTilesX * gra_NumTilesYScale;

    // Calculate xy of the tiles to load
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));

    return GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);
}

void GranitePrivate_CalculateCubemapCoordinates(in gra_Float3 inputTexCoord, in gra_Float3 dVx, in gra_Float3 dVy, in GraniteStreamingTextureCubeConstantBuffer transforms, out int faceIdx, out gra_Float2 texCoord, out gra_Float2 dX, out gra_Float2 dY)
{
    gra_Float2 contTexCoord;
    gra_Float3 derivX;
    gra_Float3 derivY;

    float majorAxis;
    if (abs(inputTexCoord.z) >= abs(inputTexCoord.x) && abs(inputTexCoord.z) >= abs(inputTexCoord.y))
    {
        // Z major axis
        if(inputTexCoord.z < 0.0)
        {
            faceIdx = 5;
            texCoord.x = -inputTexCoord.x;
        }
        else
        {
            faceIdx = 4;
            texCoord.x = inputTexCoord.x;
        }
        texCoord.y = -inputTexCoord.y;
        majorAxis = inputTexCoord.z;

        contTexCoord = gra_Float2(inputTexCoord.x, inputTexCoord.y);
        derivX = gra_Float3(dVx.x, dVx.y, dVx.z);
        derivY = gra_Float3(dVy.x, dVy.y, dVy.z);
    }
    else if (abs(inputTexCoord.y) >= abs(inputTexCoord.x))
    {
        // Y major axis
        if(inputTexCoord.y < 0.0)
        {
            faceIdx = 3;
            texCoord.y = -inputTexCoord.z;
        }
        else
        {
            faceIdx = 2;
            texCoord.y = inputTexCoord.z;
        }
        texCoord.x = inputTexCoord.x;
        majorAxis = inputTexCoord.y;

        contTexCoord = gra_Float2(inputTexCoord.x, inputTexCoord.z);
        derivX = gra_Float3(dVx.x, dVx.z, dVx.y);
        derivY = gra_Float3(dVy.x, dVy.z, dVy.y);
    }
    else
    {
        // X major axis
        if(inputTexCoord.x < 0.0)
        {
            faceIdx = 1;
            texCoord.x = inputTexCoord.z;
        }
        else
        {
            faceIdx = 0;
            texCoord.x = -inputTexCoord.z;
        }
        texCoord.y = -inputTexCoord.y;
        majorAxis = inputTexCoord.x;

        contTexCoord = gra_Float2(inputTexCoord.z, inputTexCoord.y);
        derivX = gra_Float3(dVx.z, dVx.y, dVx.x);
        derivY = gra_Float3(dVy.z, dVy.y, dVy.x);
    }
    texCoord = (texCoord + majorAxis) / (2.0 * abs(majorAxis));

#if GRA_HQ_CUBEMAPPING
    dX = /*contTexCoord **/ ((contTexCoord + derivX.xy) / ( 2.0 * (majorAxis + derivX.z)) - (contTexCoord / (2.0 * majorAxis)));
    dY = /*contTexCoord **/ ((contTexCoord + derivY.xy) / ( 2.0 * (majorAxis + derivY.z)) - (contTexCoord / (2.0 * majorAxis)));
#else
    dX = ((/*contTexCoord **/ derivX.xy) / (2.0 * abs(majorAxis)));
    dY = ((/*contTexCoord **/ derivY.xy) / (2.0 * abs(majorAxis)));
#endif

    // Now scale the derivatives with the texture transform scale
    dX *= transforms.data[faceIdx].data[0].zw;
    dY *= transforms.data[faceIdx].data[0].zw;
}

// Auto-level
void GranitePrivate_CalculateCubemapCoordinates(in gra_Float3 inputTexCoord, in GraniteStreamingTextureCubeConstantBuffer transforms, out int faceIdx, out gra_Float2 texCoord, out gra_Float2 dX, out gra_Float2 dY)
{
    gra_Float3 dVx = ddx(inputTexCoord);
    gra_Float3 dVy = ddy(inputTexCoord);

    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, dVx, dVy, transforms, faceIdx, texCoord, dX, dY);
}

gra_Float2 Granite_GetTextureDimensions(in GraniteStreamingTextureConstantBuffer grSTCB)
{
    return gra_Float2(1.0 / gra_AssetWidthRcp, 1.0 / gra_AssetHeightRcp); //TODO(ddebaets) use HLSL rcp here
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Linear
//! GranitePrivate_Lookup_Software_Linear
//! GranitePrivate_Lookup_PreTransformed_Software_Linear

// General
int GranitePrivate_Lookup_Software_General_Linear(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelLinear(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_Linear(   in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Linear(   in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_Linear(  in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Linear(  in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_Linear( in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_Linear( in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_UDIM_Linear
//! GranitePrivate_Lookup_Software_UDIM_Linear
//! GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear

// General
int GranitePrivate_Lookup_Software_General_UDIM_Linear(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelLinear(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_UDIM_Linear(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_UDIM_Linear(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_UDIM_Linear( in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_UDIM_Linear( in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_UDIM_Linear(    in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_UDIM_Linear(    in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Clamp_Linear
//! GranitePrivate_Lookup_Software_Clamp_Linear
//! GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear

// General
int GranitePrivate_Lookup_Software_General_Clamp_Linear(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelLinear(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_Clamp_Linear( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Clamp_Linear( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_Clamp_Linear(    in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Clamp_Linear(    in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_Clamp_Linear(   in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_Clamp_Linear(   in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Anisotropic
//! GranitePrivate_Lookup_Software_Anisotropic
//! GranitePrivate_Lookup_PreTransformed_Software_Anisotropic

// General
int GranitePrivate_Lookup_Software_General_Anisotropic(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelAnisotropic(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_Anisotropic(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Anisotropic(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_Anisotropic( in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Anisotropic( in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_Anisotropic(    in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_Anisotropic(    in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_UDIM_Anisotropic
//! GranitePrivate_Lookup_Software_UDIM_Anisotropic
//! GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic

// General
int GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelAnisotropic(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_UDIM_Anisotropic( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_UDIM_Anisotropic( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_UDIM_Anisotropic(    in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_UDIM_Anisotropic(    in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_UDIM_Anisotropic(   in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_UDIM_Anisotropic(   in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Clamp_Anisotropic
//! GranitePrivate_Lookup_Software_Clamp_Anisotropic
//! GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic

// General
int GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelAnisotropic(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_Clamp_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Clamp_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_Clamp_Anisotropic(   in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Clamp_Anisotropic(   in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_Clamp_Anisotropic(  in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_Clamp_Anisotropic(  in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General
//! GranitePrivate_Lookup_Software
//! GranitePrivate_Lookup_PreTransformed_Software


//**
//* GranitePrivate software sampling lod
//**

//
//  Explicit level
//

// General
int GranitePrivate_Lookup_Software_General( in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                    in gra_Float2 inputTexCoord, in float LOD, in GraniteTranslationTexture translationTable,
                                                                                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Clamp to the highest mip available in the streaming texture
    LOD = clamp(LOD, 0.0f, gra_NumLevels);

    float level = floor(LOD);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, LOD);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    // Do a simple cache sample with trilinear filtering
    float cacheLevel = frac(LOD);

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.cacheLevel = cacheLevel;

    return 1;
}

// PreTransformed
int GranitePrivate_Lookup_PreTransformed_Software(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// regular
int GranitePrivate_Lookup_Software(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed(  in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup( in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Cube(    in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_UDIM
//! GranitePrivate_Lookup_Software_UDIM
//! GranitePrivate_Lookup_PreTransformed_Software_UDIM


//**
//* GranitePrivate software sampling lod
//**

//
//  Explicit level
//

// General
int GranitePrivate_Lookup_Software_General_UDIM(    in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                    in gra_Float2 inputTexCoord, in float LOD, in GraniteTranslationTexture translationTable,
                                                                                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Clamp to the highest mip available in the streaming texture
    LOD = clamp(LOD, 0.0f, gra_NumLevels);

    float level = floor(LOD);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, LOD);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    // Do a simple cache sample with trilinear filtering
    float cacheLevel = frac(LOD);

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.cacheLevel = cacheLevel;

    return 1;
}

// PreTransformed
int GranitePrivate_Lookup_PreTransformed_Software_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_General_UDIM(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// regular
int GranitePrivate_Lookup_Software_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_UDIM(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_UDIM( in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_UDIM(    in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Cube_UDIM(   in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Clamp
//! GranitePrivate_Lookup_Software_Clamp
//! GranitePrivate_Lookup_PreTransformed_Software_Clamp


//**
//* GranitePrivate software sampling lod
//**

//
//  Explicit level
//

// General
int GranitePrivate_Lookup_Software_General_Clamp(   in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                    in gra_Float2 inputTexCoord, in float LOD, in GraniteTranslationTexture translationTable,
                                                                                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Clamp to the highest mip available in the streaming texture
    LOD = clamp(LOD, 0.0f, gra_NumLevels);

    float level = floor(LOD);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, LOD);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    // Do a simple cache sample with trilinear filtering
    float cacheLevel = frac(LOD);

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.cacheLevel = cacheLevel;

    return 1;
}

// PreTransformed
int GranitePrivate_Lookup_PreTransformed_Software_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_General_Clamp(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// regular
int GranitePrivate_Lookup_Software_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Clamp(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Clamp(    in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Clamp(   in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Cube_Clamp(  in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Dynamic_Linear(  in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Dynamic_Linear(  in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM, in float LOD,
                                    out GraniteLODLookupData graniteLODLookupData, out gra_Float4 resolveResult)
{
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLODLookupData, resolveResult);
}

int Granite_Lookup_Dynamic_Anisotropic( in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Dynamic_Anisotropic( in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM, in float LOD,
                                    out GraniteLODLookupData graniteLODLookupData, out gra_Float4 resolveResult)
{
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLODLookupData, resolveResult);
}


int Granite_Sample_Internal(in GraniteTilesetConstantBuffer tsCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
    // Convert from pixels to [0-1] and look up in the physical page texture
    gra_Float2 deltaScale;
    gra_Float3 cacheCoord = GranitePrivate_TranslateCoord(tsCB, graniteLookupData.textureCoordinates, graniteLookupData.translationTableData, layer, deltaScale);

    // This leads to small artefacts at tile borders but is generally not noticable unless the texture
    // is greatly magnified
#if GRA_TEXTURE_ARRAY_SUPPORT
    result = GranitePrivate_SampleArray(cacheTexture, cacheCoord);
#else
    result = GranitePrivate_Sample(cacheTexture, cacheCoord.xy);
#endif

#if GRA_DEBUG == 1
    if ( gra_TextureMagic != 2202.0f )
    {
        result = gra_Float4(1,0,0,1);
    }
#endif

#if GRA_DEBUG_TILES == 1
    //result.xyz = GranitePrivate_DrawDebugTiles(result, graniteLookupData.textureCoordinates, numPagesOnLevel).xyz;
#endif

    return 1;
}

int Granite_Sample_HQ_Internal(in GraniteTilesetConstantBuffer tsCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
    // Convert from pixels to [0-1] and look up in the physical page texture
    gra_Float2 deltaScale;
    gra_Float3 cacheCoord = GranitePrivate_TranslateCoord(tsCB, graniteLookupData.textureCoordinates, graniteLookupData.translationTableData, layer, deltaScale);

    deltaScale *= gra_LodBiasPow2;

    // Calculate the delta scale this works by first converting the [0-1] texcoord deltas to
    // pixel deltas on the current mip level, then dividing by the cache size to convert to [0-1] cache deltas
    gra_Float2 sampDeltaX = graniteLookupData.dX*deltaScale;
    gra_Float2 sampDeltaY = graniteLookupData.dY*deltaScale;

#if GRA_TEXTURE_ARRAY_SUPPORT
    result = GranitePrivate_SampleGradArray(cacheTexture, cacheCoord, sampDeltaX, sampDeltaY);
#else
    result = GranitePrivate_SampleGrad(cacheTexture, cacheCoord.xy, sampDeltaX, sampDeltaY);
#endif

#if GRA_DEBUG == 1
    if ( gra_TextureMagic != 2202.0f )
    {
        result = gra_Float4(1,0,0,1);
    }
#endif

#if GRA_DEBUG_TILES == 1
    //result.xyz = GranitePrivate_DrawDebugTiles(result, graniteLookupData.textureCoordinates, numPagesOnLevel).xyz;
#endif

    return 1;
}

int Granite_Sample_Interal(in GraniteTilesetConstantBuffer tsCB, in GraniteLODLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
    gra_Float2 deltaScale;
    gra_Float3 cacheCoord = GranitePrivate_TranslateCoord(tsCB, graniteLookupData.textureCoordinates, graniteLookupData.translationTableData, layer, deltaScale);

#if GRA_TEXTURE_ARRAY_SUPPORT
    result = GranitePrivate_SampleLevelArray(cacheTexture, cacheCoord, graniteLookupData.cacheLevel);
#else
    result = GranitePrivate_SampleLevel(cacheTexture, cacheCoord.xy, graniteLookupData.cacheLevel);
#endif

#if GRA_DEBUG == 1
    if ( gra_TextureMagic != 2202.0f )
    {
        result = gra_Float4(1,0,0,1);
    }
#endif

#if GRA_DEBUG_TILES == 1
    //result.xyz = GranitePrivate_DrawDebugTiles(result, inputTexCoord, numPagesOnLevel).xyz;
#endif

    return 1;
}

// LQ
int Granite_Sample(in GraniteConstantBuffers grCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_Internal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}
int Granite_Sample(in GraniteCubeConstantBuffers grCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_Internal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}

// HQ
int Granite_Sample_HQ(in GraniteConstantBuffers grCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_HQ_Internal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}
int Granite_Sample_HQ(in GraniteCubeConstantBuffers grCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_HQ_Internal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}

// LOD
int Granite_Sample(in GraniteConstantBuffers grCB, in GraniteLODLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_Interal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}
int Granite_Sample(in GraniteCubeConstantBuffers grCB, in GraniteLODLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_Interal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}



//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_PreTransformed(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//
// Tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//**
//* Granite resolver for cubemaps
//**

// Explicit level
gra_Float4 Granite_ResolverPixel_Cube(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, LOD);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//
// Tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//**
//* Granite resolver for cubemaps
//**

// Explicit level
gra_Float4 Granite_ResolverPixel_Cube_UDIM(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, LOD);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//
// Tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//**
//* Granite resolver for cubemaps
//**

// Explicit level
gra_Float4 Granite_ResolverPixel_Cube_Clamp(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, LOD);
}


gra_Float4 Granite_ResolverPixel_Dynamic_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in bool asUDIM)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}


gra_Float4 Granite_ResolverPixel_Dynamic_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in bool asUDIM)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\GraniteShaderLib3.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\HLSLSupport.cginc---------------


#ifndef HLSL_SUPPORT_INCLUDED
#define HLSL_SUPPORT_INCLUDED

#if defined(SHADER_TARGET_SURFACE_ANALYSIS)
    // surface shader analysis is complicated, and is done via two compilers:
    // - Mojoshader for source level analysis (to find out structs/functions with their members & parameters).
    //   This step can understand DX9 style HLSL syntax.
    // - HLSL compiler for "what actually got read & written to" (taking dead code etc into account), via a dummy
    //   compilation and reflection of the shader. This step can understand DX9 & DX11 HLSL syntax.
    // Neither of these compilers are "Cg", but we used to use Cg in the past for this; keep the macro
    // name intact in case some user-written shaders depend on it being that.
    #define UNITY_COMPILER_CG
#elif defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) || defined(SHADER_API_GLES) || defined(SHADER_API_WEBGPU)
    #define UNITY_COMPILER_HLSL
    #define UNITY_COMPILER_HLSLCC
#elif defined(SHADER_API_D3D11)
    #define UNITY_COMPILER_HLSL
#else
    #define UNITY_COMPILER_CG
#endif

#if defined(STEREO_MULTIVIEW_ON) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN)) && !(defined(SHADER_API_SWITCH))
    #define UNITY_STEREO_MULTIVIEW_ENABLED
#endif

#if (defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && !defined(UNITY_COMPILER_DXC))) && defined(STEREO_INSTANCING_ON)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// Is this shader API able to read the current pixel depth value, be it via texture fetch of via renderpass inputs,
//   from the depth buffer while it is simultaneously bound as a z-buffer?
// When native renderpass is not supported (using fallback implementation) this generally works on DX11, GL
//   in this case the temporary copy of depth would be created and bound for reading
// TODO: Check DX12 and consoles, implement read-only depth if possible
// When native renderpass is supported, some platofrms still allow only color readback (Metal)
//   thus for depth readback temporary RT is needed, which will be bound to "color" attachments and used to store depth data
// We had UNITY_SUPPORT_DEPTH_FETCH define, which was wrongly set, and set in here
// We have moved on to set it from the editor, and renamed it; we keep the old define for the sake of backwards compatibility
#ifdef UNITY_PLATFORM_SUPPORTS_DEPTH_FETCH
	#define UNITY_SUPPORT_DEPTH_FETCH 1
#endif

#if !defined(UNITY_COMPILER_DXC)
#if defined(UNITY_FRAMEBUFFER_FETCH_AVAILABLE) && defined(UNITY_FRAMEBUFFER_FETCH_ENABLED) && defined(UNITY_COMPILER_HLSLCC)
// In the fragment shader, setting inout <type> var : SV_Target would result to
// compiler error, unless SV_Target is defined to COLOR semantic for compatibility
// reasons. Unfortunately, we still need to have a clear distinction between
// vertex shader COLOR output and SV_Target, so the following workaround abuses
// the fact that semantic names are case insensitive and preprocessor macros
// are not. The resulting HLSL bytecode has semantics in case preserving form,
// helps code generator to do extra work required for framebuffer fetch

// You should always declare color inouts against SV_Target
#define SV_Target CoLoR
#define SV_Target0 CoLoR0
#define SV_Target1 CoLoR1
#define SV_Target2 CoLoR2
#define SV_Target3 CoLoR3
#define SV_Target4 CoLoR4
#define SV_Target5 CoLoR5
#define SV_Target6 CoLoR6
#define SV_Target7 CoLoR7

#define COLOR VCOLOR
#define COLOR0 VCOLOR0
#define COLOR1 VCOLOR1
#define COLOR2 VCOLOR2
#define COLOR3 VCOLOR3
#define COLOR4 VCOLOR4
#define COLOR5 VCOLOR5
#define COLOR6 VCOLOR6
#define COLOR7 VCOLOR7

#endif

// SV_Target[n] / SV_Depth defines, if not defined by compiler already
#if !defined(SV_Target)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target COLOR
#   endif
#endif
#if !defined(SV_Target0)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target0 COLOR0
#   endif
#endif
#if !defined(SV_Target1)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target1 COLOR1
#   endif
#endif
#if !defined(SV_Target2)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target2 COLOR2
#   endif
#endif
#if !defined(SV_Target3)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target3 COLOR3
#   endif
#endif
#if !defined(SV_Depth)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Depth DEPTH
#   endif
#endif

#endif // !defined(UNITY_COMPILER_DXC)

#if (defined(SHADER_API_GLES3) && !defined(SHADER_API_DESKTOP)) || defined(SHADER_API_GLES) || defined(SHADER_API_N3DS)
    #define UNITY_ALLOWED_MRT_COUNT 4
#else
    #define UNITY_ALLOWED_MRT_COUNT 8
#endif

#if (SHADER_TARGET < 30) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLES) || defined(SHADER_API_N3DS)
    //no fast coherent dynamic branching on these hardware
#else
    #define UNITY_FAST_COHERENT_DYNAMIC_BRANCHING 1
#endif

// Disable warnings we aren't interested in
#if defined(UNITY_COMPILER_HLSL)
#pragma warning (disable : 3205) // conversion of larger type to smaller
#pragma warning (disable : 3568) // unknown pragma ignored
#pragma warning (disable : 3571) // "pow(f,e) will not work for negative f"; however in majority of our calls to pow we know f is not negative
#pragma warning (disable : 3206) // implicit truncation of vector type
#endif

// DXC no longer supports DX9-style HLSL syntax for sampler2D, tex2D and the like.
// These are emulated for backwards compatibility using our own small structs and functions which manually combine samplers and textures.
#if defined(UNITY_COMPILER_DXC) && !defined(DXC_SAMPLER_COMPATIBILITY)
#define DXC_SAMPLER_COMPATIBILITY 1

// On DXC platforms which don't care about explicit sampler precison we want the emulated types to work directly e.g without needing to redefine 'sampler2D' to 'sampler2D_f'
#if !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !defined(SHADER_API_METAL) && !defined(SHADER_API_SWITCH) && !defined(SHADER_API_WEBGPU)
    #define sampler1D_f sampler1D
    #define sampler2D_f sampler2D
    #define sampler3D_f sampler3D
    #define samplerCUBE_f samplerCUBE
#endif

struct sampler1D_f      { Texture1D<float4> t; SamplerState s; };
struct sampler2D_f      { Texture2D<float4> t; SamplerState s; };
struct sampler3D_f      { Texture3D<float4> t; SamplerState s; };
struct samplerCUBE_f    { TextureCube<float4> t; SamplerState s; };

float4 tex1D(sampler1D_f x, float v)        { return x.t.Sample(x.s, v); }
float4 tex2D(sampler2D_f x, float2 v)       { return x.t.Sample(x.s, v); }
float4 tex3D(sampler3D_f x, float3 v)       { return x.t.Sample(x.s, v); }
float4 texCUBE(samplerCUBE_f x, float3 v)   { return x.t.Sample(x.s, v); }

float4 tex1Dbias(sampler1D_f x, in float4 t)        { return x.t.SampleBias(x.s, t.x, t.w); }
float4 tex2Dbias(sampler2D_f x, in float4 t)        { return x.t.SampleBias(x.s, t.xy, t.w); }
float4 tex3Dbias(sampler3D_f x, in float4 t)        { return x.t.SampleBias(x.s, t.xyz, t.w); }
float4 texCUBEbias(samplerCUBE_f x, in float4 t)    { return x.t.SampleBias(x.s, t.xyz, t.w); }

float4 tex1Dlod(sampler1D_f x, in float4 t)     { return x.t.SampleLevel(x.s, t.x, t.w); }
float4 tex2Dlod(sampler2D_f x, in float4 t)     { return x.t.SampleLevel(x.s, t.xy, t.w); }
float4 tex3Dlod(sampler3D_f x, in float4 t)     { return x.t.SampleLevel(x.s, t.xyz, t.w); }
float4 texCUBElod(samplerCUBE_f x, in float4 t) { return x.t.SampleLevel(x.s, t.xyz, t.w); }

float4 tex1Dgrad(sampler1D_f x, float t, float dx, float dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex2Dgrad(sampler2D_f x, float2 t, float2 dx, float2 dy)     { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex3Dgrad(sampler3D_f x, float3 t, float3 dx, float3 dy)     { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 texCUBEgrad(samplerCUBE_f x, float3 t, float3 dx, float3 dy) { return x.t.SampleGrad(x.s, t, dx, dy); }

float4 tex1D(sampler1D_f x, float t, float dx, float dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex2D(sampler2D_f x, float2 t, float2 dx, float2 dy)     { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex3D(sampler3D_f x, float3 t, float3 dx, float3 dy)     { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 texCUBE(samplerCUBE_f x, float3 t, float3 dx, float3 dy) { return x.t.SampleGrad(x.s, t, dx, dy); }

float4 tex1Dproj(sampler1D_f s, in float2 t)        { return tex1D(s, t.x / t.y); }
float4 tex1Dproj(sampler1D_f s, in float4 t)        { return tex1D(s, t.x / t.w); }
float4 tex2Dproj(sampler2D_f s, in float3 t)        { return tex2D(s, t.xy / t.z); }
float4 tex2Dproj(sampler2D_f s, in float4 t)        { return tex2D(s, t.xy / t.w); }
float4 tex3Dproj(sampler3D_f s, in float4 t)        { return tex3D(s, t.xyz / t.w); }
float4 texCUBEproj(samplerCUBE_f s, in float4 t)    { return texCUBE(s, t.xyz / t.w); }

// Half precision emulated samplers used instead the sampler.*_half unity types
struct sampler1D_h      { Texture1D<min16float4> t; SamplerState s; };
struct sampler2D_h      { Texture2D<min16float4> t; SamplerState s; };
struct sampler3D_h      { Texture3D<min16float4> t; SamplerState s; };
struct samplerCUBE_h    { TextureCube<min16float4> t; SamplerState s; };

min16float4 tex1D(sampler1D_h x, float v)       { return x.t.Sample(x.s, v); }
min16float4 tex2D(sampler2D_h x, float2 v)      { return x.t.Sample(x.s, v); }
min16float4 tex3D(sampler3D_h x, float3 v)      { return x.t.Sample(x.s, v); }
min16float4 texCUBE(samplerCUBE_h x, float3 v)  { return x.t.Sample(x.s, v); }

min16float4 tex1Dbias(sampler1D_h x, in float4 t)       { return x.t.SampleBias(x.s, t.x, t.w); }
min16float4 tex2Dbias(sampler2D_h x, in float4 t)       { return x.t.SampleBias(x.s, t.xy, t.w); }
min16float4 tex3Dbias(sampler3D_h x, in float4 t)       { return x.t.SampleBias(x.s, t.xyz, t.w); }
min16float4 texCUBEbias(samplerCUBE_h x, in float4 t)   { return x.t.SampleBias(x.s, t.xyz, t.w); }

min16float4 tex1Dlod(sampler1D_h x, in float4 t)        { return x.t.SampleLevel(x.s, t.x, t.w); }
min16float4 tex2Dlod(sampler2D_h x, in float4 t)        { return x.t.SampleLevel(x.s, t.xy, t.w); }
min16float4 tex3Dlod(sampler3D_h x, in float4 t)        { return x.t.SampleLevel(x.s, t.xyz, t.w); }
min16float4 texCUBElod(samplerCUBE_h x, in float4 t)    { return x.t.SampleLevel(x.s, t.xyz, t.w); }

min16float4 tex1Dgrad(sampler1D_h x, float t, float dx, float dy)           { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 tex2Dgrad(sampler2D_h x, float2 t, float2 dx, float2 dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 tex3Dgrad(sampler3D_h x, float3 t, float3 dx, float3 dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 texCUBEgrad(samplerCUBE_h x, float3 t, float3 dx, float3 dy)    { return x.t.SampleGrad(x.s, t, dx, dy); }

min16float4 tex1D(sampler1D_h x, float t, float dx, float dy)           { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 tex2D(sampler2D_h x, float2 t, float2 dx, float2 dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 tex3D(sampler3D_h x, float3 t, float3 dx, float3 dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 texCUBE(samplerCUBE_h x, float3 t, float3 dx, float3 dy)    { return x.t.SampleGrad(x.s, t, dx, dy); }

min16float4 tex1Dproj(sampler1D_h s, in float2 t)       { return tex1D(s, t.x / t.y); }
min16float4 tex1Dproj(sampler1D_h s, in float4 t)       { return tex1D(s, t.x / t.w); }
min16float4 tex2Dproj(sampler2D_h s, in float3 t)       { return tex2D(s, t.xy / t.z); }
min16float4 tex2Dproj(sampler2D_h s, in float4 t)       { return tex2D(s, t.xy / t.w); }
min16float4 tex3Dproj(sampler3D_h s, in float4 t)       { return tex3D(s, t.xyz / t.w); }
min16float4 texCUBEproj(samplerCUBE_h s, in float4 t)   { return texCUBE(s, t.xyz / t.w); }
#endif

// Ensure broader support by overriding half into min16float
#if defined(UNITY_UNIFIED_SHADER_PRECISION_MODEL) && (defined(UNITY_COMPILER_HLSL) || defined(UNITY_COMPILER_DXC))
#define UNITY_FIXED_IS_HALF 1
#define half min16float
#define half2 min16float2
#define half3 min16float3
#define half4 min16float4
#define half2x2 min16float2x2
#define half3x3 min16float3x3
#define half4x4 min16float4x4
#endif

// Define "fixed" precision to be half on non-GLSL platforms,
// and sampler*_prec to be just simple samplers.
#if !defined(SHADER_API_GLES) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !defined(SHADER_API_METAL) && !defined(SHADER_API_SWITCH) && !defined(SHADER_API_WEBGPU)
#define UNITY_FIXED_IS_HALF 1
#define sampler1D_half sampler1D
#define sampler1D_float sampler1D
#define sampler2D_half sampler2D
#define sampler2D_float sampler2D
#define samplerCUBE_half samplerCUBE
#define samplerCUBE_float samplerCUBE
#define sampler3D_float sampler3D
#define sampler3D_half sampler3D
#define Texture2D_half Texture2D
#define Texture2D_float Texture2D
#define Texture2DArray_half Texture2DArray
#define Texture2DArray_float Texture2DArray
#define Texture2DMS_half Texture2DMS
#define Texture2DMS_float Texture2DMS
#define TextureCube_half TextureCube
#define TextureCube_float TextureCube
#define TextureCubeArray_half TextureCubeArray
#define TextureCubeArray_float TextureCubeArray
#define Texture3D_float Texture3D
#define Texture3D_half Texture3D
#endif

#if !defined(UNITY_UNIFIED_SHADER_PRECISION_MODEL) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || (defined(SHADER_API_MOBILE) && (defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_WEBGPU))) || defined(SHADER_API_SWITCH))
// with HLSLcc, use DX11.1 partial precision for translation
// we specifically define fixed to be float16 (same as half) as all new GPUs seems to agree on float16 being minimal precision float
#define UNITY_FIXED_IS_HALF 1
#define half min16float
#define half2 min16float2
#define half3 min16float3
#define half4 min16float4
#define half2x2 min16float2x2
#define half3x3 min16float3x3
#define half4x4 min16float4x4
#endif

#if !defined(UNITY_UNIFIED_SHADER_PRECISION_MODEL) && ((!defined(SHADER_API_MOBILE) && (defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_WEBGPU))))
#define fixed float
#define fixed2 float2
#define fixed3 float3
#define fixed4 float4
#define fixed4x4 float4x4
#define fixed3x3 float3x3
#define fixed2x2 float2x2
#define half float
#define half2 float2
#define half3 float3
#define half4 float4
#define half2x2 float2x2
#define half3x3 float3x3
#define half4x4 float4x4
#endif

#if defined(UNITY_FIXED_IS_HALF)
#define fixed half
#define fixed2 half2
#define fixed3 half3
#define fixed4 half4
#define fixed4x4 half4x4
#define fixed3x3 half3x3
#define fixed2x2 half2x2
#endif

// Define min16float/min10float to be half/fixed on non-D3D11 platforms.
// This allows people to use min16float and friends in their shader code if they
// really want to (making that will make shaders not load before DX11.1, e.g. on Win7,
// but if they target WSA/WP exclusively that's fine).
#if !defined(SHADER_API_D3D11) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !defined(SHADER_API_METAL) && !defined(SHADER_API_GLES) && !defined(SHADER_API_SWITCH) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_WEBGPU)
#define min16float half
#define min16float2 half2
#define min16float3 half3
#define min16float4 half4
#define min10float fixed
#define min10float2 fixed2
#define min10float3 fixed3
#define min10float4 fixed4
#endif

#if defined(SHADER_API_GLES)
#define uint int
#define uint1 int1
#define uint2 int2
#define uint3 int3
#define uint4 int4

#define min16uint int
#define min16uint1 int1
#define min16uint2 int2
#define min16uint3 int3
#define min16uint4 int4

#define uint1x1 int1x1
#define uint1x2 int1x2
#define uint1x3 int1x3
#define uint1x4 int1x4
#define uint2x1 int2x1
#define uint2x2 int2x2
#define uint2x3 int2x3
#define uint2x4 int2x4
#define uint3x1 int3x1
#define uint3x2 int3x2
#define uint3x3 int3x3
#define uint3x4 int3x4
#define uint4x1 int4x1
#define uint4x2 int4x2
#define uint4x3 int4x3
#define uint4x4 int4x4

#define asuint(x) asint(x)
#endif

// specifically for samplers that are provided as arguments to entry functions
#if defined(SHADER_API_PSSL)
#define SAMPLER_UNIFORM uniform
#define SHADER_UNIFORM
#else
#define SAMPLER_UNIFORM
#endif

#if defined(SHADER_API_D3D11) || defined(UNITY_ENABLE_CBUFFER) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL)
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };
#else
// On specific platforms, like OpenGL and GLES3, constant buffers may still be used for instancing
#define CBUFFER_START(name)
#define CBUFFER_END
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) || ((defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED)) && (defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_WEBGPU)))
    #define GLOBAL_CBUFFER_START(name)    cbuffer name {
    #define GLOBAL_CBUFFER_END            }
#else
    #define GLOBAL_CBUFFER_START(name)    CBUFFER_START(name)
    #define GLOBAL_CBUFFER_END            CBUFFER_END
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
// OVR_multiview
// In order to convey this info over the DX compiler, we wrap it into a cbuffer.
#define UNITY_DECLARE_MULTIVIEW(number_of_views) GLOBAL_CBUFFER_START(OVR_multiview) uint gl_ViewID; uint numViews_##number_of_views; GLOBAL_CBUFFER_END
#define UNITY_VIEWID gl_ViewID
#endif

// Special declaration macro for requiring the extended blend functionality
#if defined(SHADER_API_GLES3)
// Declare the need for the KHR_blend_equation_advanced extension plus the specific blend mode (see the extension spec for list or "all_equations" for all)
#define UNITY_REQUIRE_ADVANCED_BLEND(mode) uint hlslcc_blend_support_##mode;
#else
#define UNITY_REQUIRE_ADVANCED_BLEND(mode)
#endif

#define UNITY_PROJ_COORD(a) a

// Depth texture sampling helpers.
// On most platforms you can just sample them, but some (e.g. PSP2) need special handling.
//
// SAMPLE_DEPTH_TEXTURE(sampler,uv): returns scalar depth
// SAMPLE_DEPTH_TEXTURE_PROJ(sampler,uv): projected sample
// SAMPLE_DEPTH_TEXTURE_LOD(sampler,uv): sample with LOD level

    // Sample depth, just the red component.
#   define SAMPLE_DEPTH_TEXTURE(sampler, uv) (tex2D(sampler, uv).r)
#   define SAMPLE_DEPTH_TEXTURE_PROJ(sampler, uv) (tex2Dproj(sampler, uv).r)
#   define SAMPLE_DEPTH_TEXTURE_LOD(sampler, uv) (tex2Dlod(sampler, uv).r)
    // Sample depth, all components.
#   define SAMPLE_RAW_DEPTH_TEXTURE(sampler, uv) (tex2D(sampler, uv))
#   define SAMPLE_RAW_DEPTH_TEXTURE_PROJ(sampler, uv) (tex2Dproj(sampler, uv))
#   define SAMPLE_RAW_DEPTH_TEXTURE_LOD(sampler, uv) (tex2Dlod(sampler, uv))
#   define SAMPLE_DEPTH_CUBE_TEXTURE(sampler, uv) (texCUBE(sampler, uv).r)

// Deprecated; use SAMPLE_DEPTH_TEXTURE & SAMPLE_DEPTH_TEXTURE_PROJ instead
#define UNITY_SAMPLE_DEPTH(value) (value).r


// Macros to declare and sample shadow maps.
//
// UNITY_DECLARE_SHADOWMAP declares a shadowmap.
// UNITY_SAMPLE_SHADOW samples with a float3 coordinate (UV in xy, Z in z) and returns 0..1 scalar result.
// UNITY_SAMPLE_SHADOW_PROJ samples with a projected coordinate (UV and Z divided by w).


#if !defined(SHADER_API_GLES)
    // all platforms except GLES2.0 have built-in shadow comparison samplers
    #define SHADOWS_NATIVE
#endif

#if defined(SHADER_API_D3D11) || (defined(UNITY_COMPILER_HLSLCC) && defined(SHADOWS_NATIVE)) || defined(SHADER_API_PSSL)
    // DX11 & hlslcc platforms and PS4: built-in PCF
    #define UNITY_DECLARE_SHADOWMAP(tex) Texture2D_float tex; SamplerComparisonState sampler##tex
    #define UNITY_DECLARE_TEXCUBE_SHADOWMAP(tex) TextureCube_float tex; SamplerComparisonState sampler##tex
    #define UNITY_SAMPLE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy,(coord).z)
    #define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
    #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) || defined(SHADER_API_WEBGPU)
        // GLSL does not have textureLod(samplerCubeShadow, ...) support. GLES2 does not have core support for samplerCubeShadow, so we ignore it.
        #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) tex.SampleCmp (sampler##tex,(coord).xyz,(coord).w)
    #else
       #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xyz,(coord).w)
    #endif
#else
    // Fallback / No built-in shadowmap comparison sampling: regular texture sample and do manual depth comparison
    #define UNITY_DECLARE_SHADOWMAP(tex) sampler2D_float tex
    #define UNITY_DECLARE_TEXCUBE_SHADOWMAP(tex) samplerCUBE_float tex
    #define UNITY_SAMPLE_SHADOW(tex,coord) ((SAMPLE_DEPTH_TEXTURE(tex,(coord).xy) < (coord).z) ? 0.0 : 1.0)
    #define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) ((SAMPLE_DEPTH_TEXTURE_PROJ(tex,UNITY_PROJ_COORD(coord)) < ((coord).z/(coord).w)) ? 0.0 : 1.0)
    #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) ((SAMPLE_DEPTH_CUBE_TEXTURE(tex,(coord).xyz) < (coord).w) ? 0.0 : 1.0)
#endif


// Macros to declare textures and samplers, possibly separately. For platforms
// that have separate samplers & textures (like DX11), and we'd want to conserve
// the samplers.
//  - UNITY_DECLARE_TEX*_NOSAMPLER declares a texture, without a sampler.
//  - UNITY_SAMPLE_TEX*_SAMPLER samples a texture, using sampler from another texture.
//      That another texture must also be actually used in the current shader, otherwise
//      the correct sampler will not be set.
#if defined(SHADER_API_D3D11) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))
    // DX11 style HLSL syntax; separate textures and samplers
    //
    // Note: for HLSLcc we have special unity-specific syntax to pass sampler precision information.
    //
    // Note: for surface shader analysis, go into DX11 syntax path when non-mojoshader part of analysis is done,
    // this allows surface shaders to use _NOSAMPLER and similar macros, without using up a sampler register.
    // Don't do that for mojoshader part, as that one can only parse DX9 style HLSL.

    #define UNITY_SEPARATE_TEXTURE_SAMPLER

    // 2D textures
    #define UNITY_DECLARE_TEX2D(tex) Texture2D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER(tex) Texture2D tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_INT(tex) Texture2D<int4> tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_UINT(tex) Texture2D<uint4> tex
    #define UNITY_SAMPLE_TEX2D(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEX2D_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex, samplertex, coord, lod) tex.SampleLevel (sampler##samplertex, coord, lod)

#if defined(UNITY_COMPILER_HLSLCC) && (!defined(SHADER_API_GLCORE) || defined(SHADER_API_SWITCH)) // GL Core doesn't have the _half mangling, the rest of them do. Workaround for Nintendo Switch.
    #define UNITY_DECLARE_TEX2D_HALF(tex) Texture2D<half4> tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_FLOAT(tex) Texture2D<float4> tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(tex) Texture2D<half4> tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(tex) Texture2D<float4> tex
#else
    #define UNITY_DECLARE_TEX2D_HALF(tex) Texture2D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_FLOAT(tex) Texture2D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(tex) Texture2D tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(tex) Texture2D tex
#endif

    // Cubemaps
    #define UNITY_DECLARE_TEXCUBE(tex) TextureCube tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEXCUBE_NOSAMPLER(tex) TextureCube tex

    #define UNITY_ARGS_TEXCUBE(tex) TextureCube tex, SamplerState sampler##tex
    #define UNITY_PASS_TEXCUBE(tex) tex, sampler##tex
    #define UNITY_PASS_TEXCUBE_SAMPLER(tex,samplertex) tex, sampler##samplertex
    #define UNITY_PASS_TEXCUBE_SAMPLER_LOD(tex, samplertex, lod) tex, sampler##samplertex, lod
    #define UNITY_ARGS_TEXCUBE_NOSAMPLER(tex) TextureCube tex
    #define UNITY_PASS_TEXCUBE_NOSAMPLER(tex) tex

    #define UNITY_SAMPLE_TEXCUBE(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEXCUBE_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(tex, samplertex, coord, lod) tex.SampleLevel (sampler##samplertex, coord, lod)
    // 3D textures
    #define UNITY_DECLARE_TEX3D(tex) Texture3D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX3D_NOSAMPLER(tex) Texture3D tex
    #define UNITY_SAMPLE_TEX3D(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEX3D_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEX3D_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEX3D_SAMPLER_LOD(tex, samplertex, coord, lod) tex.SampleLevel(sampler##samplertex, coord, lod)

#if defined(UNITY_COMPILER_HLSLCC) && !defined(SHADER_API_GLCORE) // GL Core doesn't have the _half mangling, the rest of them do.
    #define UNITY_DECLARE_TEX3D_FLOAT(tex) Texture3D<float4> tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX3D_HALF(tex) Texture3D<half4> tex; SamplerState sampler##tex
#else
    #define UNITY_DECLARE_TEX3D_FLOAT(tex) Texture3D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX3D_HALF(tex) Texture3D tex; SamplerState sampler##tex
#endif

    // 2D arrays
    #define UNITY_DECLARE_TEX2DARRAY_MS(tex) Texture2DMSArray<float> tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2DARRAY_MS_NOSAMPLER(tex) Texture2DArray<float> tex
    #define UNITY_DECLARE_TEX2DARRAY(tex) Texture2DArray tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(tex) Texture2DArray tex

    #define UNITY_ARGS_TEX2DARRAY(tex) Texture2DArray tex, SamplerState sampler##tex
    #define UNITY_PASS_TEX2DARRAY(tex) tex, sampler##tex
    #define UNITY_ARGS_TEX2DARRAY_NOSAMPLER(tex) Texture2DArray tex
    #define UNITY_PASS_TEX2DARRAY_NOSAMPLER(tex) tex

    #define UNITY_SAMPLE_TEX2DARRAY(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,coord,lod)

    // Cube arrays
    #define UNITY_DECLARE_TEXCUBEARRAY(tex) TextureCubeArray tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEXCUBEARRAY_NOSAMPLER(tex) TextureCubeArray tex

    #define UNITY_ARGS_TEXCUBEARRAY(tex) TextureCubeArray tex, SamplerState sampler##tex
    #define UNITY_PASS_TEXCUBEARRAY(tex) tex, sampler##tex
    #define UNITY_ARGS_TEXCUBEARRAY_NOSAMPLER(tex) TextureCubeArray tex
    #define UNITY_PASS_TEXCUBEARRAY_NOSAMPLER(tex) tex
#if defined(SHADER_API_PSSL)
    // round the layer index to get DX11-like behaviour (otherwise fractional indices result in mixed up cubemap faces)
    #define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) tex.Sample (sampler##tex,float4((coord).xyz, round((coord).w)))
    #define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,float4((coord).xyz, round((coord).w)), lod)
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,float4((coord).xyz, round((coord).w)))
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,float4((coord).xyz, round((coord).w)), lod)
#else
    #define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,coord,lod)
#endif


#else
    // DX9 style HLSL syntax; same object for texture+sampler
    // 2D textures
    #define UNITY_DECLARE_TEX2D(tex) sampler2D tex
    #define UNITY_DECLARE_TEX2D_HALF(tex) sampler2D_half tex
    #define UNITY_DECLARE_TEX2D_FLOAT(tex) sampler2D_float tex

    #define UNITY_DECLARE_TEX2D_NOSAMPLER(tex) sampler2D tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(tex) sampler2D_half tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(tex) sampler2D_float tex

    #define UNITY_SAMPLE_TEX2D(tex,coord) tex2D (tex,coord)
    #define UNITY_SAMPLE_TEX2D_SAMPLER(tex,samplertex,coord) tex2D (tex,coord)
    // Cubemaps
    #define UNITY_DECLARE_TEXCUBE(tex) samplerCUBE tex
    #define UNITY_ARGS_TEXCUBE(tex) samplerCUBE tex
    #define UNITY_PASS_TEXCUBE(tex) tex
    #define UNITY_PASS_TEXCUBE_SAMPLER(tex,samplertex) tex
    #define UNITY_DECLARE_TEXCUBE_NOSAMPLER(tex) samplerCUBE tex
    #define UNITY_SAMPLE_TEXCUBE(tex,coord) texCUBE (tex,coord)
    #define UNITY_ARGS_TEXCUBE_NOSAMPLER(tex) samplerCUBE tex
    #define UNITY_PASS_TEXCUBE_NOSAMPLER(tex) tex

    #define UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod) texCUBElod (tex, half4(coord, lod))
    #define UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(tex,samplertex,coord,lod) UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod)
    #define UNITY_SAMPLE_TEXCUBE_SAMPLER(tex,samplertex,coord) texCUBE (tex,coord)

    // 3D textures
    #define UNITY_DECLARE_TEX3D(tex) sampler3D tex
    #define UNITY_DECLARE_TEX3D_NOSAMPLER(tex) sampler3D tex
    #define UNITY_DECLARE_TEX3D_FLOAT(tex) sampler3D_float tex
    #define UNITY_DECLARE_TEX3D_HALF(tex) sampler3D_float tex
    #define UNITY_SAMPLE_TEX3D(tex,coord) tex3D (tex,coord)
    #define UNITY_SAMPLE_TEX3D_LOD(tex,coord,lod) tex3D (tex,float4(coord,lod))
    #define UNITY_SAMPLE_TEX3D_SAMPLER(tex,samplertex,coord) tex3D (tex,coord)
    #define UNITY_SAMPLE_TEX3D_SAMPLER_LOD(tex,samplertex,coord,lod) tex3D (tex,float4(coord,lod))

    // 2D array syntax for surface shader analysis
    #if defined(SHADER_TARGET_SURFACE_ANALYSIS)
        #define UNITY_DECLARE_TEX2DARRAY(tex) sampler2DArray tex
        #define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(tex) sampler2DArray tex
        #define UNITY_ARGS_TEX2DARRAY(tex) sampler2DArray tex
        #define UNITY_PASS_TEX2DARRAY(tex) tex
        #define UNITY_ARGS_TEX2DARRAY_NOSAMPLER(tex) sampler2DArray tex
        #define UNITY_PASS_TEX2DARRAY_NOSAMPLER(tex) tex
        #define UNITY_SAMPLE_TEX2DARRAY(tex,coord) tex2DArray (tex,coord)
        #define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod) tex2DArraylod (tex, float4(coord,lod))
        #define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex,samplertex,coord) tex2DArray (tex,coord)
        #define UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex2DArraylod (tex, float4(coord,lod))
    #endif

    // surface shader analysis; just pretend that 2D arrays are cubemaps
    #if defined(SHADER_TARGET_SURFACE_ANALYSIS)
        #define sampler2DArray samplerCUBE
        #define tex2DArray texCUBE
        #define tex2DArraylod texCUBElod
    #endif

#endif

// For backwards compatibility, so we won't accidentally break shaders written by user
#define SampleCubeReflection(env, dir, lod) UNITY_SAMPLE_TEXCUBE_LOD(env, dir, lod)


#define samplerRECT sampler2D
#define texRECT tex2D
#define texRECTlod tex2Dlod
#define texRECTbias tex2Dbias
#define texRECTproj tex2Dproj

#if defined(SHADER_API_PSSL) || (defined (SHADER_API_SWITCH) && defined(UNITY_COMPILER_DXC))
#define VPOS            SV_Position
#elif defined(UNITY_COMPILER_CG)
// Cg seems to use WPOS instead of VPOS semantic?
#define VPOS WPOS
// Cg does not have tex2Dgrad and friends, but has tex2D overload that
// can take the derivatives
#define tex2Dgrad tex2D
#define texCUBEgrad texCUBE
#define tex3Dgrad tex3D
#endif


// Data type to be used for "screen space position" pixel shader input semantic; just a float4 now (used to be float2 when on D3D9)
#define UNITY_VPOS_TYPE float4



#if defined(UNITY_COMPILER_HLSL)
#define FOGC FOG
#endif

// Use VFACE pixel shader input semantic in your shaders to get front-facing scalar value.
// Requires shader model 3.0 or higher.
// Back when D3D9 existed UNITY_VFACE_AFFECTED_BY_PROJECTION macro used to be defined there too.
#if defined(UNITY_COMPILER_CG)
#define VFACE FACE
#endif
#if defined(SHADER_API_PSSL)
#undef VFACE
#define VFACE SV_IsFrontFace
#endif


#if !defined(SHADER_API_D3D11) && !defined(UNITY_COMPILER_HLSLCC) && !defined(SHADER_API_PSSL)
#define SV_POSITION POSITION
#endif


// On D3D reading screen space coordinates from fragment shader requires SM3.0
#define UNITY_POSITION(pos) float4 pos : SV_POSITION

// Kept for backwards-compatibility
#define UNITY_ATTEN_CHANNEL r

#if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) || defined(SHADER_API_WEBGPU)
#define UNITY_UV_STARTS_AT_TOP 1
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) || defined(SHADER_API_WEBGPU)
// D3D style platforms where clip space z is [0, 1].
#define UNITY_REVERSED_Z 1
#endif

#if defined(UNITY_REVERSED_Z)
#define UNITY_NEAR_CLIP_VALUE (1.0)
#else
#define UNITY_NEAR_CLIP_VALUE (-1.0)
#endif

// "platform caps" defines that were moved to editor, so they are set automatically when compiling shader
// UNITY_NO_DXT5nm              - no DXT5NM support, so normal maps will encoded in rgb
// UNITY_NO_RGBM                - no RGBM support, so doubleLDR
// UNITY_NO_SCREENSPACE_SHADOWS - no screenspace cascaded shadowmaps
// UNITY_FRAMEBUFFER_FETCH_AVAILABLE    - framebuffer fetch
// UNITY_ENABLE_REFLECTION_BUFFERS - render reflection probes in deferred way, when using deferred shading


// On most platforms, use floating point render targets to store depth of point
// light shadowmaps. However, on some others they either have issues, or aren't widely
// supported; in which case fallback to encoding depth into RGBA channels.
// Make sure this define matches GraphicsCaps.useRGBAForPointShadows.
#if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
#define UNITY_USE_RGBA_FOR_POINT_SHADOWS
#endif


// Initialize arbitrary structure with zero values.
// Not supported on some backends (e.g. Cg-based particularly with nested structs).
#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
#define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0;
#else
#define UNITY_INITIALIZE_OUTPUT(type,name)
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
#define UNITY_CAN_COMPILE_TESSELLATION 1
#   define UNITY_domain                 domain
#   define UNITY_partitioning           partitioning
#   define UNITY_outputtopology         outputtopology
#   define UNITY_patchconstantfunc      patchconstantfunc
#   define UNITY_outputcontrolpoints    outputcontrolpoints
#endif

// Not really needed anymore, but did ship in Unity 4.0; with D3D11_9X remapping them to .r channel.
// Now that's not used.
#define UNITY_SAMPLE_1CHANNEL(x,y) tex2D(x,y).a
#define UNITY_ALPHA_CHANNEL a


// HLSL attributes
#if defined(UNITY_COMPILER_HLSL)
    #define UNITY_BRANCH    [branch]
    #define UNITY_FLATTEN   [flatten]
    #define UNITY_UNROLL    [unroll]
    #define UNITY_LOOP      [loop]
    #define UNITY_FASTOPT   [fastopt]
#else
    #define UNITY_BRANCH
    #define UNITY_FLATTEN
    #define UNITY_UNROLL
    #define UNITY_LOOP
    #define UNITY_FASTOPT
#endif


// Unity 4.x shaders used to mostly work if someone used WPOS semantic,
// which was accepted by Cg. The correct semantic to use is "VPOS",
// so define that so that old shaders keep on working.
#if !defined(UNITY_COMPILER_CG)
#define WPOS VPOS
#endif

// define use to identify platform with modern feature like texture 3D with filtering, texture array etc...
#define UNITY_SM40_PLUS_PLATFORM (!((SHADER_TARGET < 30) || defined (SHADER_API_MOBILE) || defined(SHADER_API_GLES)))

// Ability to manually set descriptor set and binding numbers (Vulkan only)
#if defined(SHADER_API_VULKAN) || defined(SHADER_API_WEBGPU)
    #define CBUFFER_START_WITH_BINDING(Name, Set, Binding) CBUFFER_START(Name##Xhlslcc_set_##Set##_bind_##Binding##X)
    // Sampler / image declaration with set/binding decoration
    #define DECL_WITH_BINDING(Type, Name, Set, Binding) Type Name##hlslcc_set_##Set##_bind_##Binding
#else
    #define CBUFFER_START_WITH_BINDING(Name, Set, Binding) CBUFFER_START(Name)
    #define DECL_WITH_BINDING(Type, Name, Set, Binding) Type Name
#endif

// TODO: Really need a better define for iOS Metal than the framebuffer fetch one, that's also enabled on android and webgl (???)
#if defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_WEBGPU)

    #if defined(UNITY_COMPILER_DXC)

        //Subpass inputs are disallowed in non-fragment shader stages with DXC so we need some dummy value to use in the fragment function while it's not bing compiled
        #if defined(SHADER_STAGE_FRAGMENT)
            #define UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(type, idx) [[vk::input_attachment_index(idx)]] SubpassInput<type##4> hlslcc_fbinput_##idx
            #define UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(type, idx) [[vk::input_attachment_index(idx)]] SubpassInputMS<type##4> hlslcc_fbinput_##idx
        #else
            //declaring dummy resources here so that non-fragment shader stage automatic bindings wouldn't diverge from the fragment shader (important for vulkan)
            #define UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(type, idx) Texture2D dxc_dummy_fbinput_resource##idx; static type DXC_DummySubpassVariable##idx = type(0).xxxx;
            #define UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(type, idx) Texture2D dxc_dummy_fbinput_resource##idx; static type DXC_DummySubpassVariable##idx = type(0).xxxx
        #endif
        // Renderpass inputs: Vulkan/Metal subpass input
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(float, idx)
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT_MS(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(float, idx)
        // For halfs
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(half, idx)
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF_MS(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(half, idx)
        // For ints
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(int, idx)
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT_MS(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(int, idx)
        // For uints
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(uint, idx)
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT_MS(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(uint, idx)

        #if defined(SHADER_STAGE_FRAGMENT)
            #define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fname) hlslcc_fbinput_##idx.SubpassLoad()
            #define UNITY_READ_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname) hlslcc_fbinput_##idx.SubpassLoad(sampleIdx)
        #else
            #define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fname) DXC_DummySubpassVariable##idx
            #define UNITY_READ_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname) DXC_DummySubpassVariable##idx
        #endif

    #elif defined(SHADER_API_METAL) && defined(UNITY_NEEDS_RENDERPASS_FBFETCH_FALLBACK)

        // On desktop metal we need special magic due to the need to support both intel and apple silicon
        // since the former does not support framebuffer fetch
        // Due to this we have special considerations:
        // 1. since we might need to bind the copy texture, to simplify our lives we always declare _UnityFBInput texture
        //    in metal translation we will add function_constant, but we still want to generate binding in hlsl
        //    so that unity knows about the possibility
        // 2. hlsl do not have anything like function constants, hence we will add bool to the fake cbuffer for subpass
        //    again, this is done only for hlsl to generate proper code - in translation it will be changed to
        //    a proper function constant (i.e. hlslcc_SubpassInput_f_ cbuffer is just "metadata" and is absent in metal code)
        // 3. we want to generate an actual if command (not conditional move), hence we need to have an interim function
        //    alas we are not able to hide in it the texture coords: we are guaranteed to have just one "declare fb input"
        //    per index, but nothing stops users to have several "read fb input", hence we need to generate function code
        //    in the former, where we do not know the source of uv coords
        //    while the usage looks weird (we pass hlslcc_fbfetch_ in the function), it is ok due to the way hlsl compiler works
        //    it will generate an actual if and access hlslcc_fbfetch_ only if framebuffer fetch is available
        //    and when creating metal program, compiler takes care of this (function_constant magic)

        #define UNITY_RENDERPASS_DECLARE_FALLBACK(T, idx)                                                       \
            Texture2D<T> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;                             \
            inline T ReadFBInput_##idx(bool var, uint2 coord) {                                                 \
                [branch]if(var) { return hlslcc_fbinput_##idx; }                                                \
                else { return _UnityFBInput##idx.Load(uint3(coord,0)); }                                        \
            }
        #define UNITY_RENDERPASS_DECLARE_FALLBACK_MS(T, idx)                                                    \
            Texture2DMS<T> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;                           \
            inline T ReadFBInput_##idx(bool var, uint2 coord, uint sampleIdx) {                                 \
                [branch]if(var) { return hlslcc_fbinput_##idx[sampleIdx]; }                                     \
                else { return _UnityFBInput##idx.Load(coord,sampleIdx); }                                       \
            }

        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(idx)                                                      \
            cbuffer hlslcc_SubpassInput_f_##idx { float4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };    \
            UNITY_RENDERPASS_DECLARE_FALLBACK(float4, idx)

        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT_MS(idx)                                                   \
            cbuffer hlslcc_SubpassInput_F_##idx { float4 hlslcc_fbinput_##idx[8]; bool hlslcc_fbfetch_##idx; }; \
            UNITY_RENDERPASS_DECLARE_FALLBACK_MS(float4, idx)

        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(idx)                                                       \
            cbuffer hlslcc_SubpassInput_h_##idx { half4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };     \
            UNITY_RENDERPASS_DECLARE_FALLBACK(half4, idx)

        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF_MS(idx)                                                    \
            cbuffer hlslcc_SubpassInput_H_##idx { half4 hlslcc_fbinput_##idx[8]; bool hlslcc_fbfetch_##idx; };  \
            UNITY_RENDERPASS_DECLARE_FALLBACK_MS(half4, idx)

        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT(idx)                                                        \
            cbuffer hlslcc_SubpassInput_i_##idx { int4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };      \
            UNITY_RENDERPASS_DECLARE_FALLBACK(int4, idx)

        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT_MS(idx)                                                     \
            cbuffer hlslcc_SubpassInput_I_##idx { int4 hlslcc_fbinput_##idx[8]; bool hlslcc_fbfetch_##idx; };   \
            UNITY_RENDERPASS_DECLARE_FALLBACK_MS(int4, idx)

        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT(idx)                                                       \
            cbuffer hlslcc_SubpassInput_u_##idx { uint4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };     \
            UNITY_RENDERPASS_DECLARE_FALLBACK(uint4, idx)

        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT_MS(idx)                                                    \
            cbuffer hlslcc_SubpassInput_U_##idx { uint4 hlslcc_fbinput_##idx[8]; bool hlslcc_fbfetch_##idx; };  \
            UNITY_RENDERPASS_DECLARE_FALLBACK_MS(uint4, idx)

        #define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fname) ReadFBInput_##idx(hlslcc_fbfetch_##idx, uint2(v2fname.xy))
        #define UNITY_READ_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname) ReadFBInput_##idx(hlslcc_fbfetch_##idx, uint2(v2fname.xy), sampleIdx)

    #else

        // Renderpass inputs: Vulkan/Metal subpass input
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(idx) cbuffer hlslcc_SubpassInput_f_##idx { float4 hlslcc_fbinput_##idx; }
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT_MS(idx) cbuffer hlslcc_SubpassInput_F_##idx { float4 hlslcc_fbinput_##idx[8]; }
        // For halfs
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(idx) cbuffer hlslcc_SubpassInput_h_##idx { half4 hlslcc_fbinput_##idx; }
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF_MS(idx) cbuffer hlslcc_SubpassInput_H_##idx { half4 hlslcc_fbinput_##idx[8]; }
        // For ints
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT(idx) cbuffer hlslcc_SubpassInput_i_##idx { int4 hlslcc_fbinput_##idx; }
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT_MS(idx) cbuffer hlslcc_SubpassInput_I_##idx { int4 hlslcc_fbinput_##idx[8]; }
        // For uints
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT(idx) cbuffer hlslcc_SubpassInput_u_##idx { uint4 hlslcc_fbinput_##idx; }
        #define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT_MS(idx) cbuffer hlslcc_SubpassInput_U_##idx { uint4 hlslcc_fbinput_##idx[8]; }

        #define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fname) hlslcc_fbinput_##idx
        #define UNITY_READ_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname) hlslcc_fbinput_##idx[sampleIdx]

    #endif //defined(UNITY_COMPILER_DXC)

#else

    // Renderpass inputs: General fallback path
    #define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(idx) UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize;
    #define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(idx) UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize;
    #define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT(idx) UNITY_DECLARE_TEX2D_NOSAMPLER_INT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize;
    #define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT(idx) UNITY_DECLARE_TEX2D_NOSAMPLER_UINT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize;

    #define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fvertexname) _UnityFBInput##idx.Load(uint3(v2fvertexname.xy, 0))

    // MSAA input framebuffers via tex2dms

    #define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT_MS(idx) Texture2DMS<float4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;
    #define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF_MS(idx) Texture2DMS<float4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;
    #define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT_MS(idx) Texture2DMS<int4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;
    #define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT_MS(idx) Texture2DMS<uint4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;

    #define UNITY_READ_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fvertexname) _UnityFBInput##idx.Load(uint2(v2fvertexname.xy), sampleIdx)

#endif

// ---- Shader keyword backwards compatibility
// We used to have some built-in shader keywords, but they got removed at some point to save on shader keyword count.
// However some existing shader code might be checking for the old names, so define them as regular
// macros based on other criteria -- so that existing code keeps on working.

// Unity 5.0 renamed HDR_LIGHT_PREPASS_ON to UNITY_HDR_ON
#if defined(UNITY_HDR_ON)
#define HDR_LIGHT_PREPASS_ON 1
#endif

// UNITY_NO_LINEAR_COLORSPACE was removed in 5.4 when UNITY_COLORSPACE_GAMMA was introduced as a platform keyword and runtime gamma fallback removed.
#if !defined(UNITY_NO_LINEAR_COLORSPACE) && defined(UNITY_COLORSPACE_GAMMA)
#define UNITY_NO_LINEAR_COLORSPACE 1
#endif

#if !defined(DIRLIGHTMAP_OFF) && !defined(DIRLIGHTMAP_COMBINED)
#define DIRLIGHTMAP_OFF 1
#endif

#if !defined(LIGHTMAP_OFF) && !defined(LIGHTMAP_ON)
#define LIGHTMAP_OFF 1
#endif

#if !defined(DYNAMICLIGHTMAP_OFF) && !defined(DYNAMICLIGHTMAP_ON)
#define DYNAMICLIGHTMAP_OFF 1
#endif


#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)

    #undef UNITY_DECLARE_DEPTH_TEXTURE_MS
    #define UNITY_DECLARE_DEPTH_TEXTURE_MS(tex)  UNITY_DECLARE_TEX2DARRAY_MS (tex)

    #undef UNITY_DECLARE_DEPTH_TEXTURE
    #define UNITY_DECLARE_DEPTH_TEXTURE(tex) UNITY_DECLARE_TEX2DARRAY (tex)

    #undef SAMPLE_DEPTH_TEXTURE
    #define SAMPLE_DEPTH_TEXTURE(sampler, uv) UNITY_SAMPLE_TEX2DARRAY(sampler, float3((uv).x, (uv).y, (float)unity_StereoEyeIndex)).r

    #undef SAMPLE_DEPTH_TEXTURE_PROJ
    #define SAMPLE_DEPTH_TEXTURE_PROJ(sampler, uv) UNITY_SAMPLE_TEX2DARRAY(sampler, float3((uv).x/(uv).w, (uv).y/(uv).w, (float)unity_StereoEyeIndex)).r

    #undef SAMPLE_DEPTH_TEXTURE_LOD
    #define SAMPLE_DEPTH_TEXTURE_LOD(sampler, uv) UNITY_SAMPLE_TEX2DARRAY_LOD(sampler, float3((uv).xy, (float)unity_StereoEyeIndex), (uv).w).r

    #undef SAMPLE_RAW_DEPTH_TEXTURE
    #define SAMPLE_RAW_DEPTH_TEXTURE(tex, uv) UNITY_SAMPLE_TEX2DARRAY(tex, float3((uv).xy, (float)unity_StereoEyeIndex))

    #undef SAMPLE_RAW_DEPTH_TEXTURE_PROJ
    #define SAMPLE_RAW_DEPTH_TEXTURE_PROJ(sampler, uv) UNITY_SAMPLE_TEX2DARRAY(sampler, float3((uv).x/(uv).w, (uv).y/(uv).w, (float)unity_StereoEyeIndex))

    #undef SAMPLE_RAW_DEPTH_TEXTURE_LOD
    #define SAMPLE_RAW_DEPTH_TEXTURE_LOD(sampler, uv) UNITY_SAMPLE_TEX2DARRAY_LOD(sampler, float3((uv).xy, (float)unity_StereoEyeIndex), (uv).w)

    #define UNITY_DECLARE_SCREENSPACE_SHADOWMAP UNITY_DECLARE_TEX2DARRAY
    #define UNITY_SAMPLE_SCREEN_SHADOW(tex, uv) UNITY_SAMPLE_TEX2DARRAY( tex, float3((uv).x/(uv).w, (uv).y/(uv).w, (float)unity_StereoEyeIndex) ).r

    #define UNITY_DECLARE_SCREENSPACE_TEXTURE UNITY_DECLARE_TEX2DARRAY
    #define UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, uv) UNITY_SAMPLE_TEX2DARRAY(tex, float3((uv).xy, (float)unity_StereoEyeIndex))
#else
    #define UNITY_DECLARE_DEPTH_TEXTURE_MS(tex)  Texture2DMS<float> tex;
    #define UNITY_DECLARE_DEPTH_TEXTURE(tex) sampler2D_float tex
    #define UNITY_DECLARE_SCREENSPACE_SHADOWMAP(tex) sampler2D tex
    #define UNITY_SAMPLE_SCREEN_SHADOW(tex, uv) tex2Dproj( tex, UNITY_PROJ_COORD(uv) ).r
    #define UNITY_DECLARE_SCREENSPACE_TEXTURE(tex) sampler2D_float tex;
    #define UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, uv) tex2D(tex, uv)
#endif

// Vulkan SwapChain preTransform
#define UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0   0
#define UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_90  1
#define UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_180 2
#define UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_270 3

#ifdef UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION
#   ifdef UNITY_COMPILER_DXC
        [[vk::constant_id(1)]] const int UnityDisplayOrientationPreTransform = 0;
#   else
        cbuffer UnityDisplayOrientationPreTransformData { int UnityDisplayOrientationPreTransform; };
#   endif
#   define UNITY_DISPLAY_ORIENTATION_PRETRANSFORM UnityDisplayOrientationPreTransform
#else
#   define UNITY_DISPLAY_ORIENTATION_PRETRANSFORM UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0
#endif

#endif // HLSL_SUPPORT_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\HLSLSupport.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\Lighting.cginc---------------


#ifndef LIGHTING_INCLUDED
#define LIGHTING_INCLUDED

#include "UnityLightingCommon.cginc"
#include "UnityGBuffer.cginc"
#include "UnityGlobalIllumination.cginc"

struct SurfaceOutput {
    fixed3 Albedo;
    fixed3 Normal;
    fixed3 Emission;
    half Specular;
    fixed Gloss;
    fixed Alpha;
};

#ifndef USING_DIRECTIONAL_LIGHT
#if defined (DIRECTIONAL_COOKIE) || defined (DIRECTIONAL)
#define USING_DIRECTIONAL_LIGHT
#endif
#endif

#if defined(UNITY_SHOULD_SAMPLE_SH) || defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
    #define UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
#endif

inline fixed4 UnityLambertLight (SurfaceOutput s, UnityLight light)
{
    fixed diff = max (0, dot (s.Normal, light.dir));

    fixed4 c;
    c.rgb = s.Albedo * light.color * diff;
    c.a = s.Alpha;
    return c;
}

inline fixed4 LightingLambert (SurfaceOutput s, UnityGI gi)
{
    fixed4 c;
    c = UnityLambertLight (s, gi.light);

    #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
        c.rgb += s.Albedo * gi.indirect.diffuse;
    #endif

    return c;
}

inline half4 LightingLambert_Deferred (SurfaceOutput s, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    UnityStandardData data;
    data.diffuseColor   = s.Albedo;
    data.occlusion      = 1;
    data.specularColor  = 0;
    data.smoothness     = 0;
    data.normalWorld    = s.Normal;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    half4 emission = half4(s.Emission, 1);

    #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
        emission.rgb += s.Albedo * gi.indirect.diffuse;
    #endif

    return emission;
}

inline void LightingLambert_GI (
    SurfaceOutput s,
    UnityGIInput data,
    inout UnityGI gi)
{
    gi = UnityGlobalIllumination (data, 1.0, s.Normal);
}

// NOTE: some intricacy in shader compiler on some GLES2.0 platforms (iOS) needs 'viewDir' & 'h'
// to be mediump instead of lowp, otherwise specular highlight becomes too bright.
inline fixed4 UnityBlinnPhongLight (SurfaceOutput s, half3 viewDir, UnityLight light)
{
    half3 h = normalize (light.dir + viewDir);

    fixed diff = max (0, dot (s.Normal, light.dir));

    float nh = max (0, dot (s.Normal, h));
    float spec = pow (nh, s.Specular*128.0) * s.Gloss;

    fixed4 c;
    c.rgb = s.Albedo * light.color * diff + light.color * _SpecColor.rgb * spec;
    c.a = s.Alpha;

    return c;
}

inline fixed4 LightingBlinnPhong (SurfaceOutput s, half3 viewDir, UnityGI gi)
{
    fixed4 c;
    c = UnityBlinnPhongLight (s, viewDir, gi.light);

    #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
        c.rgb += s.Albedo * gi.indirect.diffuse;
    #endif

    return c;
}

inline half4 LightingBlinnPhong_Deferred (SurfaceOutput s, half3 viewDir, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    UnityStandardData data;
    data.diffuseColor   = s.Albedo;
    data.occlusion      = 1;
    // PI factor come from StandardBDRF (UnityStandardBRDF.cginc:351 for explanation)
    data.specularColor  = _SpecColor.rgb * s.Gloss * (1/UNITY_PI);
    data.smoothness     = s.Specular;
    data.normalWorld    = s.Normal;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    half4 emission = half4(s.Emission, 1);

    #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
        emission.rgb += s.Albedo * gi.indirect.diffuse;
    #endif

    return emission;
}

inline void LightingBlinnPhong_GI (
    SurfaceOutput s,
    UnityGIInput data,
    inout UnityGI gi)
{
    gi = UnityGlobalIllumination (data, 1.0, s.Normal);
}

#ifdef UNITY_CAN_COMPILE_TESSELLATION
struct UnityTessellationFactors {
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};
#endif // UNITY_CAN_COMPILE_TESSELLATION

// Deprecated, kept around for existing user shaders.
#define UNITY_DIRBASIS \
const half3x3 unity_DirBasis = half3x3( \
  half3( 0.81649658,  0.0,        0.57735027), \
  half3(-0.40824830,  0.70710678, 0.57735027), \
  half3(-0.40824830, -0.70710678, 0.57735027) \
);

// Deprecated, kept around for existing user shaders. Only sampling the flat lightmap now.
half3 DirLightmapDiffuse(in half3x3 dirBasis, fixed4 color, fixed4 scale, half3 normal, bool surfFuncWritesNormal, out half3 scalePerBasisVector)
{
    scalePerBasisVector = 1;
    return DecodeLightmap (color);
}

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\Lighting.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTree8Common.cginc---------------


///////////////////////////////////////////////////////////////////////
//  SpeedTree8Common.cginc

#ifndef SPEEDTREE8_COMMON_INCLUDED
#define SPEEDTREE8_COMMON_INCLUDED

#include "UnityCG.cginc"
#include "UnityPBSLighting.cginc"
#include "SpeedTreeShaderLibrary.cginc"

#if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
    #define SPEEDTREE_Y_UP
    #include "SpeedTreeWind.cginc"
    UNITY_INSTANCING_BUFFER_START(STWind)
        UNITY_DEFINE_INSTANCED_PROP(float, _GlobalWindTime)
    UNITY_INSTANCING_BUFFER_END(STWind)
#endif

struct Input
{
    half2   uv_MainTex  : TEXCOORD0;
    fixed4  color       : COLOR;

    #ifdef EFFECT_BACKSIDE_NORMALS
        fixed   facing      : VFACE;
    #endif
};

sampler2D _MainTex;
fixed4 _Color;
int _TwoSided;

#ifdef EFFECT_BUMP
    sampler2D _BumpMap;
#endif

#ifdef EFFECT_EXTRA_TEX
    sampler2D _ExtraTex;
#else
    half _Glossiness;
    half _Metallic;
#endif

#ifdef EFFECT_HUE_VARIATION
    half4 _HueVariationColor;
#endif

#ifdef EFFECT_BILLBOARD
    half _BillboardShadowFade;
#endif

#ifdef EFFECT_SUBSURFACE
    sampler2D _SubsurfaceTex;
    fixed4 _SubsurfaceColor;
    half _SubsurfaceIndirect;
#endif

#define GEOM_TYPE_BRANCH 0
#define GEOM_TYPE_FROND 1
#define GEOM_TYPE_LEAF 2
#define GEOM_TYPE_FACINGLEAF 3


///////////////////////////////////////////////////////////////////////
//  OffsetSpeedTreeVertex

void OffsetSpeedTreeVertex(inout appdata_full data, float lodValue)
{
    // smooth LOD
    #if defined(LOD_FADE_PERCENTAGE) && !defined(EFFECT_BILLBOARD)
        data.vertex.xyz = lerp(data.vertex.xyz, data.texcoord2.xyz, lodValue);
    #endif

    // determine vertex geom type
    float geometryType = (int)(data.texcoord3.w + 0.25);
    bool leafTwo = false;
    if (geometryType > GEOM_TYPE_FACINGLEAF)
    {
        geometryType -= 2;
        leafTwo = true;
    }

    // camera facing leaves
    #if !defined(EFFECT_BILLBOARD)
    if (geometryType == GEOM_TYPE_FACINGLEAF)
    {
        float3 anchor = float3(data.texcoord1.zw, data.texcoord2.w);
        data.vertex.xyz = DoLeafFacing(data.vertex.xyz, anchor);
    }
    #endif

    // wind
    #if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
        float3 rotatedWindVector = TransformWindVectorFromWorldToLocalSpace(_ST_WindVector.xyz);
        if(dot(rotatedWindVector,rotatedWindVector) < 1e-4)
        {
            return; // bail out if no wind data
        }
        float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
        float3 windyPosition = data.vertex.xyz;

        #ifndef EFFECT_BILLBOARD
            // leaves
            if (geometryType > GEOM_TYPE_FROND)
            {
                #if defined(_WINDQUALITY_FAST) || defined(_WINDQUALITY_BETTER) || defined(_WINDQUALITY_BEST)
                    float3 anchor = float3(data.texcoord1.zw, data.texcoord2.w);
                    #ifdef _WINDQUALITY_BEST
                        bool bBestWind = true;
                    #else
                        bool bBestWind = false;
                    #endif
                    float leafWindTrigOffset = anchor.x + anchor.y;
                    windyPosition = LeafWind(bBestWind, leafTwo, windyPosition, data.normal, data.texcoord3.x, anchor, data.texcoord3.y, data.texcoord3.z, leafWindTrigOffset, rotatedWindVector);
                #endif
            }

            // frond wind
            bool bPalmWind = false;
            #ifdef _WINDQUALITY_PALM
                bPalmWind = true;
                if (geometryType == GEOM_TYPE_FROND)
                {
                    windyPosition = RippleFrond(windyPosition, data.normal, data.texcoord.x, data.texcoord.y, data.texcoord3.x, data.texcoord3.y, data.texcoord3.z);
                }
            #endif

            // branch wind (applies to all 3D geometry)
            #if defined(_WINDQUALITY_BETTER) || defined(_WINDQUALITY_BEST) || defined(_WINDQUALITY_PALM)
                float3 rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)unity_ObjectToWorld)) * _ST_WindBranchAnchor.w;
                windyPosition = BranchWind(bPalmWind, windyPosition, treePos, float4(data.texcoord.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor);
            #endif

        #endif // !EFFECT_BILLBOARD

        // global wind
        float globalWindTime = _ST_WindGlobal.x;
        #if defined(EFFECT_BILLBOARD) && defined(UNITY_INSTANCING_ENABLED)
            globalWindTime += UNITY_ACCESS_INSTANCED_PROP(STWind, _GlobalWindTime);
        #endif
        windyPosition = GlobalWind(windyPosition, treePos, true, rotatedWindVector, globalWindTime);
        data.vertex.xyz = windyPosition;
    #endif
}


///////////////////////////////////////////////////////////////////////
//  vertex program

void SpeedTreeVert(inout appdata_full v)
{
    // handle speedtree wind and lod
    OffsetSpeedTreeVertex(v, unity_LODFade.x);

    float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);

    #if defined(EFFECT_BILLBOARD)

    BillboardSeamCrossfade(v, treePos);

    #endif

    // color already contains (ao, ao, ao, blend)
    // put hue variation amount in there
    #ifdef EFFECT_HUE_VARIATION
        float hueVariationAmount = frac(treePos.x + treePos.y + treePos.z);
        v.color.g = saturate(hueVariationAmount * _HueVariationColor.a);
    #endif
}


///////////////////////////////////////////////////////////////////////
//  lighting function to add subsurface

half4 LightingSpeedTreeSubsurface(inout SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
{
    #ifdef EFFECT_SUBSURFACE
        half fSubsurfaceRough = 0.7 - s.Smoothness * 0.5;
        half fSubsurface = GGXTerm(clamp(-dot(gi.light.dir, viewDir), 0, 1), fSubsurfaceRough);

        // put modulated subsurface back into emission
        s.Emission *= (gi.indirect.diffuse * _SubsurfaceIndirect + gi.light.color * fSubsurface);
    #endif

    return LightingStandard(s, viewDir, gi);
}

void LightingSpeedTreeSubsurface_GI(inout SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
{
    #ifdef EFFECT_BILLBOARD
        // fade off the shadows on billboards to avoid artifacts
        data.atten = lerp(data.atten, 1.0, _BillboardShadowFade);
    #endif

    LightingStandard_GI(s, data, gi);
}

half4 LightingSpeedTreeSubsurface_Deferred(SurfaceOutputStandard s, half3 viewDir, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    // no light/shadow info in deferred, so stop subsurface
    s.Emission = half3(0,0,0);

    return LightingStandard_Deferred(s, viewDir, gi, outGBuffer0, outGBuffer1, outGBuffer2);
}


///////////////////////////////////////////////////////////////////////
//  surface shader

void SpeedTreeSurf(Input IN, inout SurfaceOutputStandard OUT)
{
    fixed4 color = tex2D(_MainTex, IN.uv_MainTex) * _Color;

    // transparency
    OUT.Alpha = color.a * IN.color.a;
    clip(OUT.Alpha - 0.3333);

    // color
    OUT.Albedo = color.rgb;

    // hue variation
    #ifdef EFFECT_HUE_VARIATION
        half3 shiftedColor = lerp(OUT.Albedo, _HueVariationColor.rgb, IN.color.g);

        // preserve vibrance
        half maxBase = max(OUT.Albedo.r, max(OUT.Albedo.g, OUT.Albedo.b));
        half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
        maxBase /= newMaxBase;
        maxBase = maxBase * 0.5f + 0.5f;
        shiftedColor.rgb *= maxBase;

        OUT.Albedo = saturate(shiftedColor);
    #endif

    // normal
    #ifdef EFFECT_BUMP
        OUT.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
    #elif defined(EFFECT_BACKSIDE_NORMALS) || defined(EFFECT_BILLBOARD)
        OUT.Normal = float3(0, 0, 1);
    #endif

    // flip normal on backsides
    #ifdef EFFECT_BACKSIDE_NORMALS
        if (IN.facing < 0.5)
        {
            OUT.Normal.z = -OUT.Normal.z;
        }
    #endif

    // adjust billboard normals to improve GI and matching
    #ifdef EFFECT_BILLBOARD
        OUT.Normal.z *= 0.5;
        OUT.Normal = normalize(OUT.Normal);
    #endif

    // extra
    #ifdef EFFECT_EXTRA_TEX
        fixed4 extra = tex2D(_ExtraTex, IN.uv_MainTex);
        OUT.Smoothness = extra.r; // no slider is exposed when ExtraTex is not available, hence we skip the multiplication here
        OUT.Metallic = extra.g;
        OUT.Occlusion = extra.b * IN.color.r;
    #else
        OUT.Smoothness = _Glossiness;
        OUT.Metallic = _Metallic;
        OUT.Occlusion = IN.color.r;
    #endif

    // subsurface (hijack emissive)
    #ifdef EFFECT_SUBSURFACE
        OUT.Emission = tex2D(_SubsurfaceTex, IN.uv_MainTex) * _SubsurfaceColor;
    #endif
}


#endif // SPEEDTREE8_COMMON_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTree8Common.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeBillboardCommon.cginc---------------


#ifndef SPEEDTREE_BILLBOARD_COMMON_INCLUDED
#define SPEEDTREE_BILLBOARD_COMMON_INCLUDED

#define SPEEDTREE_ALPHATEST
fixed _Cutoff;

#include "SpeedTreeCommon.cginc"

CBUFFER_START(UnityBillboardPerCamera)
    float3 unity_BillboardNormal;
    float3 unity_BillboardTangent;
    float4 unity_BillboardCameraParams;
    #define unity_BillboardCameraPosition (unity_BillboardCameraParams.xyz)
    #define unity_BillboardCameraXZAngle (unity_BillboardCameraParams.w)
CBUFFER_END

CBUFFER_START(UnityBillboardPerBatch)
    float4 unity_BillboardInfo; // x: num of billboard slices; y: 1.0f / (delta angle between slices)
    float4 unity_BillboardSize; // x: width; y: height; z: bottom
    float4 unity_BillboardImageTexCoords[16];
CBUFFER_END

struct SpeedTreeBillboardData
{
    float4 vertex       : POSITION;
    float2 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float3 normal       : NORMAL;
    float4 tangent      : TANGENT;
    float4 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

void SpeedTreeBillboardVert(inout SpeedTreeBillboardData IN, out Input OUT)
{
    UNITY_INITIALIZE_OUTPUT(Input, OUT);

    // assume no scaling & rotation
    float3 worldPos = IN.vertex.xyz + float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);

#ifdef BILLBOARD_FACE_CAMERA_POS
    float3 eyeVec = normalize(unity_BillboardCameraPosition - worldPos);
    float3 billboardTangent = normalize(float3(-eyeVec.z, 0, eyeVec.x));            // cross(eyeVec, {0,1,0})
    float3 billboardNormal = float3(billboardTangent.z, 0, -billboardTangent.x);    // cross({0,1,0},billboardTangent)
    float3 angle = atan2(billboardNormal.z, billboardNormal.x);                     // signed angle between billboardNormal to {0,0,1}
    angle += angle < 0 ? 2 * UNITY_PI : 0;
#else
    float3 billboardTangent = unity_BillboardTangent;
    float3 billboardNormal = unity_BillboardNormal;
    float angle = unity_BillboardCameraXZAngle;
#endif

    float widthScale = IN.texcoord1.x;
    float heightScale = IN.texcoord1.y;
    float rotation = IN.texcoord1.z;

    float2 percent = IN.texcoord.xy;
    float3 billboardPos = (percent.x - 0.5f) * unity_BillboardSize.x * widthScale * billboardTangent;
    billboardPos.y += (percent.y * unity_BillboardSize.y + unity_BillboardSize.z) * heightScale;

#ifdef ENABLE_WIND
    float windEnabled = dot(_ST_WindVector.xyz, _ST_WindVector.xyz);
    if (_WindQuality * windEnabled)
        billboardPos = GlobalWind(billboardPos, worldPos, true, _ST_WindVector.xyz, IN.texcoord1.w);
#endif

    IN.vertex.xyz += billboardPos;
    IN.vertex.w = 1.0f;
    IN.normal = billboardNormal.xyz;
    IN.tangent = float4(billboardTangent.xyz,-1);

    float slices = unity_BillboardInfo.x;
    float invDelta = unity_BillboardInfo.y;
    angle += rotation;

    float imageIndex = fmod(floor(angle * invDelta + 0.5f), slices);
    float4 imageTexCoords = unity_BillboardImageTexCoords[imageIndex];
    if (imageTexCoords.w < 0)
    {
        OUT.mainTexUV = imageTexCoords.xy - imageTexCoords.zw * percent.yx;
    }
    else
    {
        OUT.mainTexUV = imageTexCoords.xy + imageTexCoords.zw * percent;
    }

    OUT.color = _Color;

#ifdef EFFECT_HUE_VARIATION
    float hueVariationAmount = frac(worldPos.x + worldPos.y + worldPos.z);
    OUT.HueVariationAmount = saturate(hueVariationAmount * _HueVariation.a);
#endif
}

#endif // SPEEDTREE_BILLBOARD_COMMON_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeBillboardCommon.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeCommon.cginc---------------


#ifndef SPEEDTREE_COMMON_INCLUDED
#define SPEEDTREE_COMMON_INCLUDED

#include "UnityCG.cginc"

#define SPEEDTREE_Y_UP

#ifdef GEOM_TYPE_BRANCH_DETAIL
    #define GEOM_TYPE_BRANCH
#endif

#include "SpeedTreeVertex.cginc"

// Define Input structure

struct Input
{
    fixed4 color;
    half3 interpolator1;
    #ifdef GEOM_TYPE_BRANCH_DETAIL
        half3 interpolator2;
    #endif
};

// Define uniforms

#define mainTexUV interpolator1.xy
sampler2D _MainTex;

#ifdef GEOM_TYPE_BRANCH_DETAIL
    #define Detail interpolator2
    sampler2D _DetailTex;
#endif

#if defined(GEOM_TYPE_FROND) || defined(GEOM_TYPE_LEAF) || defined(GEOM_TYPE_FACING_LEAF)
    #define SPEEDTREE_ALPHATEST
    fixed _Cutoff;
#endif

#ifdef EFFECT_HUE_VARIATION
    #define HueVariationAmount interpolator1.z
    half4 _HueVariation;
#endif

#if defined(EFFECT_BUMP) && !defined(LIGHTMAP_ON)
    sampler2D _BumpMap;
#endif

fixed4 _Color;

// Vertex processing

void SpeedTreeVert(inout SpeedTreeVB IN, out Input OUT)
{
    UNITY_INITIALIZE_OUTPUT(Input, OUT);

    OUT.mainTexUV = IN.texcoord.xy;
    OUT.color = _Color;
    OUT.color.rgb *= IN.color.r; // ambient occlusion factor

    #ifdef EFFECT_HUE_VARIATION
        float hueVariationAmount = frac(unity_ObjectToWorld[0].w + unity_ObjectToWorld[1].w + unity_ObjectToWorld[2].w);
        hueVariationAmount += frac(IN.vertex.x + IN.normal.y + IN.normal.x) * 0.5 - 0.3;
        OUT.HueVariationAmount = saturate(hueVariationAmount * _HueVariation.a);
    #endif

    #ifdef GEOM_TYPE_BRANCH_DETAIL
        // The two types are always in different sub-range of the mesh so no interpolation (between detail and blend) problem.
        OUT.Detail.xy = IN.texcoord2.xy;
        if (IN.color.a == 0) // Blend
            OUT.Detail.z = IN.texcoord2.z;
        else // Detail texture
            OUT.Detail.z = 2.5f; // stay out of Blend's .z range
    #endif

    OffsetSpeedTreeVertex(IN, unity_LODFade.x);
}

// Fragment processing

#if defined(EFFECT_BUMP)
    #define SPEEDTREE_DATA_NORMAL           fixed3 Normal;
    #define SPEEDTREE_COPY_NORMAL(to, from) to.Normal = from.Normal;
#else
    #define SPEEDTREE_DATA_NORMAL
    #define SPEEDTREE_COPY_NORMAL(to, from)
#endif

#define SPEEDTREE_COPY_FRAG(to, from)   \
    to.Albedo = from.Albedo;            \
    to.Alpha = from.Alpha;              \
    SPEEDTREE_COPY_NORMAL(to, from)

struct SpeedTreeFragOut
{
    fixed3 Albedo;
    fixed Alpha;
    SPEEDTREE_DATA_NORMAL
};

void SpeedTreeFrag(Input IN, out SpeedTreeFragOut OUT)
{
    half4 diffuseColor = tex2D(_MainTex, IN.mainTexUV);

    OUT.Alpha = diffuseColor.a * _Color.a;
    #ifdef SPEEDTREE_ALPHATEST
        clip(OUT.Alpha - _Cutoff);
    #endif

    #ifdef GEOM_TYPE_BRANCH_DETAIL
        half4 detailColor = tex2D(_DetailTex, IN.Detail.xy);
        diffuseColor.rgb = lerp(diffuseColor.rgb, detailColor.rgb, IN.Detail.z < 2.0f ? saturate(IN.Detail.z) : detailColor.a);
    #endif

    #ifdef EFFECT_HUE_VARIATION
        half3 shiftedColor = lerp(diffuseColor.rgb, _HueVariation.rgb, IN.HueVariationAmount);
        half maxBase = max(diffuseColor.r, max(diffuseColor.g, diffuseColor.b));
        half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
        maxBase /= newMaxBase;
        maxBase = maxBase * 0.5f + 0.5f;
        // preserve vibrance
        shiftedColor.rgb *= maxBase;
        diffuseColor.rgb = saturate(shiftedColor);
    #endif

    OUT.Albedo = diffuseColor.rgb * IN.color.rgb;

    #if defined(EFFECT_BUMP)
        #if defined(LIGHTMAP_ON)
            OUT.Normal = fixed3(0,0,1);
        #else
            OUT.Normal = UnpackNormal(tex2D(_BumpMap, IN.mainTexUV));
            #ifdef GEOM_TYPE_BRANCH_DETAIL
                half3 detailNormal = UnpackNormal(tex2D(_BumpMap, IN.Detail.xy));
                OUT.Normal = lerp(OUT.Normal, detailNormal, IN.Detail.z < 2.0f ? saturate(IN.Detail.z) : detailColor.a);
            #endif
        #endif
    #endif
}

#endif // SPEEDTREE_COMMON_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeCommon.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeShaderLibrary.cginc---------------


///////////////////////////////////////////////////////////////////////
//  SpeedTreeLibrary.cginc

#ifndef SPEEDTREE_LIBRARY_INCLUDED
#define SPEEDTREE_LIBRARY_INCLUDED

#include "UnityCG.cginc"

void BillboardSeamCrossfade(inout appdata_full v, float3 treePos)
{
    // crossfade faces
    bool topDown = (v.texcoord.z > 0.5);
    float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;
    float3 cameraDir = normalize(mul((float3x3) unity_WorldToObject, _WorldSpaceCameraPos - treePos));
    float viewDot = max(dot(viewDir, v.normal), dot(cameraDir, v.normal));
    viewDot *= viewDot;
    viewDot *= viewDot;
    viewDot += topDown ? 0.38 : 0.18; // different scales for horz and vert billboards to fix transition zone
    v.color = float4(1, 1, 1, clamp(viewDot, 0, 1));

    // if invisible, avoid overdraw
    if (viewDot < 0.3333)
    {
        v.vertex.xyz = float3(0, 0, 0);
    }

    // adjust lighting on billboards to prevent seams between the different faces
    if (topDown)
    {
        v.normal += cameraDir;
    }
    else
    {
        half3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;
        float3 right = cross(cameraDir, binormal);
        v.normal = cross(binormal, right);
    }
    v.normal = normalize(v.normal);
}

float3 DoLeafFacing(float3 vPos, float3 anchor)
{
    float3 facingPosition = vPos - anchor; // move to origin
    float offsetLen = length(facingPosition);

    // rotate X -90deg: normals keep looking 'up' while cards/leaves now 'stand up' and face the view plane
    facingPosition = float3(facingPosition.x, -facingPosition.z, facingPosition.y);

    // extract scale from model matrix
    float3 scale = float3(
        length(float3(UNITY_MATRIX_M[0][0], UNITY_MATRIX_M[1][0], UNITY_MATRIX_M[2][0])),
        length(float3(UNITY_MATRIX_M[0][1], UNITY_MATRIX_M[1][1], UNITY_MATRIX_M[2][1])),
        length(float3(UNITY_MATRIX_M[0][2], UNITY_MATRIX_M[1][2], UNITY_MATRIX_M[2][2]))
    );
    
    // inverse of model : discards object rotations & scale
    // inverse of view  : discards camera rotations
    float3x3 matCardFacingTransform = mul((float3x3)unity_WorldToObject, (float3x3) UNITY_MATRIX_I_V);
    
    // re-encode the scale into the final transformation (otherwise cards would look small if tree is scaled up via world transform)
    matCardFacingTransform[0] *= scale.x;
    matCardFacingTransform[1] *= scale.y;
    matCardFacingTransform[2] *= scale.z;

    // make the leaves/cards face the camera
    facingPosition = mul(matCardFacingTransform, facingPosition.xyz);
    facingPosition = normalize(facingPosition) * offsetLen; // make sure the offset vector is still scaled
    
    return facingPosition + anchor; // move back to branch
}


#define SPEEDTREE_SUPPORT_NON_UNIFORM_SCALING 0
float3 TransformWindVectorFromWorldToLocalSpace(float3 vWindDirection)
{
    // we intend to transform the world-space wind vector into local space.
#if SPEEDTREE_SUPPORT_NON_UNIFORM_SCALING 
    // the inverse world matrix would contain scale transformation as well, so we need
    // to get rid of scaling of the wind direction while doing inverse rotation.
    float3 scaleInv = float3(
        length(float3(UNITY_MATRIX_M[0][0], UNITY_MATRIX_M[1][0], UNITY_MATRIX_M[2][0])),
        length(float3(UNITY_MATRIX_M[0][1], UNITY_MATRIX_M[1][1], UNITY_MATRIX_M[2][1])),
        length(float3(UNITY_MATRIX_M[0][2], UNITY_MATRIX_M[1][2], UNITY_MATRIX_M[2][2]))
    );
    float3x3 matWorldToLocalSpaceRotation = float3x3( // 3x3 discards translation
        UNITY_MATRIX_I_M[0][0] * scaleInv.x, UNITY_MATRIX_I_M[0][1]             , UNITY_MATRIX_I_M[0][2],
        UNITY_MATRIX_I_M[1][0]             , UNITY_MATRIX_I_M[1][1] * scaleInv.y, UNITY_MATRIX_I_M[1][2],
        UNITY_MATRIX_I_M[2][0]             , UNITY_MATRIX_I_M[2][1]             , UNITY_MATRIX_I_M[2][2] * scaleInv.z
    );
    float3 vLocalSpaceWind = mul(matWorldToLocalSpaceRotation, vWindDirection);
#else
    // Assume uniform scaling for the object -- discard translation and invert object rotations (and scale).
    // We'll normalize to get rid of scaling after the transformation.
    // - mul((float3x3) UNITY_MATRIX_I_M, vWindDirection)   <-- UNITY_MATRIX_I_M not defined
    // - mul(vWindDirection, (float3x3) UNITY_MATRIX_M  )   <-- UNITY_MATRIX_M can be used, which is the transpose of UNITY_MATRIX_I_M ignoring scaling and translate
    float3 vLocalSpaceWind = mul(vWindDirection, (float3x3) UNITY_MATRIX_M);
#endif
    float windVecLength = length(vLocalSpaceWind);
    if (windVecLength > 1e-5)
        vLocalSpaceWind *= (1.0f / windVecLength); // normalize
    return vLocalSpaceWind;
}
#endif // SPEEDTREE_LIBRARY_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeShaderLibrary.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeVertex.cginc---------------


#ifndef SPEEDTREE_VERTEX_INCLUDED
#define SPEEDTREE_VERTEX_INCLUDED

///////////////////////////////////////////////////////////////////////
//  SpeedTree v6 Vertex Processing

///////////////////////////////////////////////////////////////////////
//  struct SpeedTreeVB

// texcoord setup
//
//      BRANCHES                        FRONDS                      LEAVES
// 0    diffuse uv, branch wind xy      "                           "
// 1    lod xyz, 0                      lod xyz, 0                  anchor xyz, lod scalar
// 2    detail/seam uv, seam amount, 0  frond wind xyz, 0           leaf wind xyz, leaf group

struct SpeedTreeVB
{
    float4 vertex       : POSITION;
    float4 tangent      : TANGENT;
    float3 normal       : NORMAL;
    float4 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float4 texcoord2    : TEXCOORD2;
    float2 texcoord3    : TEXCOORD3;
    half4 color         : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


///////////////////////////////////////////////////////////////////////
//  SpeedTree winds

#ifdef ENABLE_WIND

#define WIND_QUALITY_NONE       0
#define WIND_QUALITY_FASTEST    1
#define WIND_QUALITY_FAST       2
#define WIND_QUALITY_BETTER     3
#define WIND_QUALITY_BEST       4
#define WIND_QUALITY_PALM       5

uniform half _WindQuality;

#define SPEEDTREE_8_WIND 1 // v7 and v8 use the same wind
#include "SpeedTreeWind.cginc"

#endif

///////////////////////////////////////////////////////////////////////
//  OffsetSpeedTreeVertex

void OffsetSpeedTreeVertex(inout SpeedTreeVB data, float lodValue)
{
    float3 finalPosition = data.vertex.xyz;

    #ifdef ENABLE_WIND
        float windEnabled = dot(_ST_WindVector.xyz, _ST_WindVector.xyz) > 0.0f ? 1.0f : 0.0f;
        half windQuality = _WindQuality * windEnabled;

        float3 rotatedWindVector, rotatedBranchAnchor;
        if (windQuality <= WIND_QUALITY_NONE)
        {
            rotatedWindVector = float3(0.0f, 0.0f, 0.0f);
            rotatedBranchAnchor = float3(0.0f, 0.0f, 0.0f);
        }
        else
        {
            // compute rotated wind parameters
            rotatedWindVector = normalize(mul(_ST_WindVector.xyz, (float3x3)unity_ObjectToWorld));
            rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)unity_ObjectToWorld)) * _ST_WindBranchAnchor.w;
        }
    #endif

    #if defined(GEOM_TYPE_BRANCH) || defined(GEOM_TYPE_FROND)

        // smooth LOD
        #ifdef LOD_FADE_PERCENTAGE
            finalPosition = lerp(finalPosition, data.texcoord1.xyz, lodValue);
        #endif

        // frond wind, if needed
        #if defined(ENABLE_WIND) && defined(GEOM_TYPE_FROND)
            if (windQuality == WIND_QUALITY_PALM)
                finalPosition = RippleFrond(finalPosition, data.normal, data.texcoord.x, data.texcoord.y, data.texcoord2.x, data.texcoord2.y, data.texcoord2.z);
        #endif

    #elif defined(GEOM_TYPE_LEAF)

        // remove anchor position
        finalPosition -= data.texcoord1.xyz;

        bool isFacingLeaf = data.color.a == 0;
        if (isFacingLeaf)
        {
            #ifdef LOD_FADE_PERCENTAGE
                finalPosition *= lerp(1.0, data.texcoord1.w, lodValue);
            #endif
            // face camera-facing leaf to camera
            float offsetLen = length(finalPosition);
            finalPosition = mul(finalPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * finalPosition
            finalPosition = normalize(finalPosition) * offsetLen; // make sure the offset vector is still scaled
        }
        else
        {
            #ifdef LOD_FADE_PERCENTAGE
                float3 lodPosition = float3(data.texcoord1.w, data.texcoord3.x, data.texcoord3.y);
                finalPosition = lerp(finalPosition, lodPosition, lodValue);
            #endif
        }

        #ifdef ENABLE_WIND
            // leaf wind
            if (windQuality > WIND_QUALITY_FASTEST && windQuality < WIND_QUALITY_PALM)
            {
                float leafWindTrigOffset = data.texcoord1.x + data.texcoord1.y;
                finalPosition = LeafWind(windQuality == WIND_QUALITY_BEST, data.texcoord2.w > 0.0, finalPosition, data.normal, data.texcoord2.x, float3(0,0,0), data.texcoord2.y, data.texcoord2.z, leafWindTrigOffset, rotatedWindVector);
            }
        #endif

        // move back out to anchor
        finalPosition += data.texcoord1.xyz;

    #endif

    #ifdef ENABLE_WIND
        float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);

        #ifndef GEOM_TYPE_MESH
            if (windQuality >= WIND_QUALITY_BETTER)
            {
                // branch wind (applies to all 3D geometry)
                finalPosition = BranchWind(windQuality == WIND_QUALITY_PALM, finalPosition, treePos, float4(data.texcoord.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor);
            }
        #endif

        if (windQuality > WIND_QUALITY_NONE)
        {
            // global wind
            finalPosition = GlobalWind(finalPosition, treePos, true, rotatedWindVector, _ST_WindGlobal.x);
        }
    #endif

    data.vertex.xyz = finalPosition;
}

#endif // SPEEDTREE_VERTEX_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeVertex.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeWind.cginc---------------


#ifndef SPEEDTREE_WIND_INCLUDED
#define SPEEDTREE_WIND_INCLUDED

#if !defined(SPEEDTREE_8_WIND) && !defined(SPEEDTREE_9_WIND)
#define SPEEDTREE_8_WIND 0
#define SPEEDTREE_9_WIND 1
#endif

#if SPEEDTREE_8_WIND

///////////////////////////////////////////////////////////////////////
//  Wind Info

CBUFFER_START(SpeedTreeWind)
    float4 _ST_WindVector;
    float4 _ST_WindGlobal;
    float4 _ST_WindBranch;
    float4 _ST_WindBranchTwitch;
    float4 _ST_WindBranchWhip;
    float4 _ST_WindBranchAnchor;
    float4 _ST_WindBranchAdherences;
    float4 _ST_WindTurbulences;
    float4 _ST_WindLeaf1Ripple;
    float4 _ST_WindLeaf1Tumble;
    float4 _ST_WindLeaf1Twitch;
    float4 _ST_WindLeaf2Ripple;
    float4 _ST_WindLeaf2Tumble;
    float4 _ST_WindLeaf2Twitch;
    float4 _ST_WindFrondRipple;
    float4 _ST_WindAnimation;
CBUFFER_END

///////////////////////////////////////////////////////////////////////
//  UnpackNormalFromFloat

float3 UnpackNormalFromFloat(float fValue)
{
    float3 vDecodeKey = float3(16.0, 1.0, 0.0625);

    // decode into [0,1] range
    float3 vDecodedValue = frac(fValue / vDecodeKey);

    // move back into [-1,1] range & normalize
    return (vDecodedValue * 2.0 - 1.0);
}


///////////////////////////////////////////////////////////////////////
//  CubicSmooth

float4 CubicSmooth(float4 vData)
{
    return vData * vData * (3.0 - 2.0 * vData);
}


///////////////////////////////////////////////////////////////////////
//  TriangleWave

float4 TriangleWave(float4 vData)
{
    return abs((frac(vData + 0.5) * 2.0) - 1.0);
}


///////////////////////////////////////////////////////////////////////
//  TrigApproximate

float4 TrigApproximate(float4 vData)
{
    return (CubicSmooth(TriangleWave(vData)) - 0.5) * 2.0;
}


///////////////////////////////////////////////////////////////////////
//  RotationMatrix
//
//  Constructs an arbitrary axis rotation matrix

float3x3 RotationMatrix(float3 vAxis, float fAngle)
{
    // compute sin/cos of fAngle
    float2 vSinCos;
    #ifdef OPENGL
        vSinCos.x = sin(fAngle);
        vSinCos.y = cos(fAngle);
    #else
        sincos(fAngle, vSinCos.x, vSinCos.y);
    #endif

    const float c = vSinCos.y;
    const float s = vSinCos.x;
    const float t = 1.0 - c;
    const float x = vAxis.x;
    const float y = vAxis.y;
    const float z = vAxis.z;

    return float3x3(t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
                    t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
                    t * x * z - s * y,  t * y * z + s * x,  t * z * z + c);
}


///////////////////////////////////////////////////////////////////////
//  mul_float3x3_float3x3

float3x3 mul_float3x3_float3x3(float3x3 mMatrixA, float3x3 mMatrixB)
{
    return mul(mMatrixA, mMatrixB);
}


///////////////////////////////////////////////////////////////////////
//  mul_float3x3_float3

float3 mul_float3x3_float3(float3x3 mMatrix, float3 vVector)
{
    return mul(mMatrix, vVector);
}


///////////////////////////////////////////////////////////////////////
//  cross()'s parameters are backwards in GLSL

#define wind_cross(a, b) cross((a), (b))

///////////////////////////////////////////////////////////////////////
//  Roll

float Roll(float fCurrent,
           float fMaxScale,
           float fMinScale,
           float fSpeed,
           float fRipple,
           float3 vPos,
           float fTime,
           float3 vRotatedWindVector)
{
    float fWindAngle = dot(vPos, -vRotatedWindVector) * fRipple;
    float fAdjust = TrigApproximate(float4(fWindAngle + fTime * fSpeed, 0.0, 0.0, 0.0)).x;
    fAdjust = (fAdjust + 1.0) * 0.5;

    return lerp(fCurrent * fMinScale, fCurrent * fMaxScale, fAdjust);
}


///////////////////////////////////////////////////////////////////////
//  Twitch

float Twitch(float3 vPos, float fAmount, float fSharpness, float fTime)
{
    const float c_fTwitchFudge = 0.87;
    float4 vOscillations = TrigApproximate(float4(fTime + (vPos.x + vPos.z), c_fTwitchFudge * fTime + vPos.y, 0.0, 0.0));

    //float fTwitch = sin(fFreq1 * fTime + (vPos.x + vPos.z)) * cos(fFreq2 * fTime + vPos.y);
    float fTwitch = vOscillations.x * vOscillations.y * vOscillations.y;
    fTwitch = (fTwitch + 1.0) * 0.5;

    return fAmount * pow(saturate(fTwitch), fSharpness);
}


///////////////////////////////////////////////////////////////////////
//  Oscillate
//
//  This function computes an oscillation value and whip value if necessary.
//  Whip and oscillation are combined like this to minimize calls to
//  TrigApproximate( ) when possible.

float Oscillate(float3 vPos,
                float fTime,
                float fOffset,
                float fWeight,
                float fWhip,
                bool bWhip,
                bool bRoll,
                bool bComplex,
                float fTwitch,
                float fTwitchFreqScale,
                inout float4 vOscillations,
                float3 vRotatedWindVector)
{
    float fOscillation = 1.0;
    if (bComplex)
    {
        if (bWhip)
            vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * fTwitchFreqScale + fOffset, fTwitchFreqScale * 0.5 * (fTime + fOffset), fTime + fOffset + (1.0 - fWeight)));
        else
            vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * fTwitchFreqScale + fOffset, fTwitchFreqScale * 0.5 * (fTime + fOffset), 0.0));

        float fFineDetail = vOscillations.x;
        float fBroadDetail = vOscillations.y * vOscillations.z;

        float fTarget = 1.0;
        float fAmount = fBroadDetail;
        if (fBroadDetail < 0.0)
        {
            fTarget = -fTarget;
            fAmount = -fAmount;
        }

        fBroadDetail = lerp(fBroadDetail, fTarget, fAmount);
        fBroadDetail = lerp(fBroadDetail, fTarget, fAmount);

        fOscillation = fBroadDetail * fTwitch * (1.0 - _ST_WindVector.w) + fFineDetail * (1.0 - fTwitch);

        if (bWhip)
            fOscillation *= 1.0 + (vOscillations.w * fWhip);
    }
    else
    {
        if (bWhip)
            vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * 0.689 + fOffset, 0.0, fTime + fOffset + (1.0 - fWeight)));
        else
            vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * 0.689 + fOffset, 0.0, 0.0));

        fOscillation = vOscillations.x + vOscillations.y * vOscillations.x;

        if (bWhip)
            fOscillation *= 1.0 + (vOscillations.w * fWhip);
    }

    //if (bRoll)
    //{
    //  fOscillation = Roll(fOscillation, _ST_WindRollingBranches.x, _ST_WindRollingBranches.y, _ST_WindRollingBranches.z, _ST_WindRollingBranches.w, vPos.xyz, fTime + fOffset, vRotatedWindVector);
    //}

    return fOscillation;
}


///////////////////////////////////////////////////////////////////////
//  Turbulence

float Turbulence(float fTime, float fOffset, float fGlobalTime, float fTurbulence)
{
    const float c_fTurbulenceFactor = 0.1;

    float4 vOscillations = TrigApproximate(float4(fTime * c_fTurbulenceFactor + fOffset, fGlobalTime * fTurbulence * c_fTurbulenceFactor + fOffset, 0.0, 0.0));

    return 1.0 - (vOscillations.x * vOscillations.y * vOscillations.x * vOscillations.y * fTurbulence);
}


///////////////////////////////////////////////////////////////////////
//  GlobalWind
//
//  This function positions any tree geometry based on their untransformed
//  position and 4 wind floats.

float3 GlobalWind(float3 vPos, float3 vInstancePos, bool bPreserveShape, float3 vRotatedWindVector, float time)
{
    // WIND_LOD_GLOBAL may be on, but if the global wind effect (WIND_EFFECT_GLOBAL_ST_Wind)
    // was disabled for the tree in the Modeler, we should skip it

    float fLength = 1.0;
    if (bPreserveShape)
        fLength = length(vPos.xyz);

    // compute how much the height contributes
    #ifdef SPEEDTREE_Z_UP
        float fAdjust = max(vPos.z - (1.0 / _ST_WindGlobal.z) * 0.25, 0.0) * _ST_WindGlobal.z;
    #else
        float fAdjust = max(vPos.y - (1.0 / _ST_WindGlobal.z) * 0.25, 0.0) * _ST_WindGlobal.z;
    #endif
    if (fAdjust != 0.0)
        fAdjust = pow(abs(fAdjust), _ST_WindGlobal.w);

    // primary oscillation
    float4 vOscillations = TrigApproximate(float4(vInstancePos.x + time, vInstancePos.y + time * 0.8, 0.0, 0.0));
    float fOsc = vOscillations.x + (vOscillations.y * vOscillations.y);
    float fMoveAmount = _ST_WindGlobal.y * fOsc;

    // move a minimum amount based on direction adherence
    fMoveAmount += _ST_WindBranchAdherences.x / _ST_WindGlobal.z;

    // adjust based on how high up the tree this vertex is
    fMoveAmount *= fAdjust;

    // xy component
    #ifdef SPEEDTREE_Z_UP
        vPos.xy += vRotatedWindVector.xy * fMoveAmount;
    #else
        vPos.xz += vRotatedWindVector.xz * fMoveAmount;
    #endif

    if (bPreserveShape)
        vPos.xyz = normalize(vPos.xyz) * fLength;

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  SimpleBranchWind

float3 SimpleBranchWind(float3 vPos,
                        float3 vInstancePos,
                        float fWeight,
                        float fOffset,
                        float fTime,
                        float fDistance,
                        float fTwitch,
                        float fTwitchScale,
                        float fWhip,
                        bool bWhip,
                        bool bRoll,
                        bool bComplex,
                        float3 vRotatedWindVector)
{
    // turn the offset back into a nearly normalized vector
    float3 vWindVector = UnpackNormalFromFloat(fOffset);
    vWindVector = vWindVector * fWeight;

    // try to fudge time a bit so that instances aren't in sync
    fTime += vInstancePos.x + vInstancePos.y;

    // oscillate
    float4 vOscillations;
    float fOsc = Oscillate(vPos, fTime, fOffset, fWeight, fWhip, bWhip, bRoll, bComplex, fTwitch, fTwitchScale, vOscillations, vRotatedWindVector);

    vPos.xyz += vWindVector * fOsc * fDistance;

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  DirectionalBranchWind

float3 DirectionalBranchWind(float3 vPos,
                             float3 vInstancePos,
                             float fWeight,
                             float fOffset,
                             float fTime,
                             float fDistance,
                             float fTurbulence,
                             float fAdherence,
                             float fTwitch,
                             float fTwitchScale,
                             float fWhip,
                             bool bWhip,
                             bool bRoll,
                             bool bComplex,
                             bool bTurbulence,
                             float3 vRotatedWindVector)
{
    // turn the offset back into a nearly normalized vector
    float3 vWindVector = UnpackNormalFromFloat(fOffset);
    vWindVector = vWindVector * fWeight;

    // try to fudge time a bit so that instances aren't in sync
    fTime += vInstancePos.x + vInstancePos.y;

    // oscillate
    float4 vOscillations;
    float fOsc = Oscillate(vPos, fTime, fOffset, fWeight, fWhip, bWhip, false, bComplex, fTwitch, fTwitchScale, vOscillations, vRotatedWindVector);

    vPos.xyz += vWindVector * fOsc * fDistance;

    // add in the direction, accounting for turbulence
    float fAdherenceScale = 1.0;
    if (bTurbulence)
        fAdherenceScale = Turbulence(fTime, fOffset, _ST_WindAnimation.x, fTurbulence);

    if (bWhip)
        fAdherenceScale += vOscillations.w * _ST_WindVector.w * fWhip;

    //if (bRoll)
    //  fAdherenceScale = Roll(fAdherenceScale, _ST_WindRollingBranches.x, _ST_WindRollingBranches.y, _ST_WindRollingBranches.z, _ST_WindRollingBranches.w, vPos.xyz, fTime + fOffset, vRotatedWindVector);

    vPos.xyz += vRotatedWindVector * fAdherence * fAdherenceScale * fWeight;

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  DirectionalBranchWindFrondStyle

float3 DirectionalBranchWindFrondStyle(float3 vPos,
                                       float3 vInstancePos,
                                       float fWeight,
                                       float fOffset,
                                       float fTime,
                                       float fDistance,
                                       float fTurbulence,
                                       float fAdherence,
                                       float fTwitch,
                                       float fTwitchScale,
                                       float fWhip,
                                       bool bWhip,
                                       bool bRoll,
                                       bool bComplex,
                                       bool bTurbulence,
                                       float3 vRotatedWindVector,
                                       float3 vRotatedBranchAnchor)
{
    // turn the offset back into a nearly normalized vector
    float3 vWindVector = UnpackNormalFromFloat(fOffset);
    vWindVector = vWindVector * fWeight;

    // try to fudge time a bit so that instances aren't in sync
    fTime += vInstancePos.x + vInstancePos.y;

    // oscillate
    float4 vOscillations;
    float fOsc = Oscillate(vPos, fTime, fOffset, fWeight, fWhip, bWhip, false, bComplex, fTwitch, fTwitchScale, vOscillations, vRotatedWindVector);

    vPos.xyz += vWindVector * fOsc * fDistance;

    // add in the direction, accounting for turbulence
    float fAdherenceScale = 1.0;
    if (bTurbulence)
        fAdherenceScale = Turbulence(fTime, fOffset, _ST_WindAnimation.x, fTurbulence);

    //if (bRoll)
    //  fAdherenceScale = Roll(fAdherenceScale, _ST_WindRollingBranches.x, _ST_WindRollingBranches.y, _ST_WindRollingBranches.z, _ST_WindRollingBranches.w, vPos.xyz, fTime + fOffset, vRotatedWindVector);

    if (bWhip)
        fAdherenceScale += vOscillations.w * _ST_WindVector.w * fWhip;

    float3 vWindAdherenceVector = vRotatedBranchAnchor - vPos.xyz;
    vPos.xyz += vWindAdherenceVector * fAdherence * fAdherenceScale * fWeight;

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  BranchWind

// Apply only to better, best, palm winds
float3 BranchWind(bool isPalmWind, float3 vPos, float3 vInstancePos, float4 vWindData, float3 vRotatedWindVector, float3 vRotatedBranchAnchor)
{
    if (isPalmWind)
    {
        vPos = DirectionalBranchWindFrondStyle(vPos, vInstancePos, vWindData.x, vWindData.y, _ST_WindBranch.x, _ST_WindBranch.y, _ST_WindTurbulences.x, _ST_WindBranchAdherences.y, _ST_WindBranchTwitch.x, _ST_WindBranchTwitch.y, _ST_WindBranchWhip.x, true, false, true, true, vRotatedWindVector, vRotatedBranchAnchor);
    }
    else
    {
        vPos = SimpleBranchWind(vPos, vInstancePos, vWindData.x, vWindData.y, _ST_WindBranch.x, _ST_WindBranch.y, _ST_WindBranchTwitch.x, _ST_WindBranchTwitch.y, _ST_WindBranchWhip.x, false, false, true, vRotatedWindVector);
    }

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  LeafRipple

float3 LeafRipple(float3 vPos,
                  inout float3 vDirection,
                  float fScale,
                  float fPackedRippleDir,
                  float fTime,
                  float fAmount,
                  bool bDirectional,
                  float fTrigOffset)
{
    // compute how much to move
    float4 vInput = float4(fTime + fTrigOffset, 0.0, 0.0, 0.0);
    float fMoveAmount = fAmount * TrigApproximate(vInput).x;

    if (bDirectional)
    {
        vPos.xyz += vDirection.xyz * fMoveAmount * fScale;
    }
    else
    {
        float3 vRippleDir = UnpackNormalFromFloat(fPackedRippleDir);
        vPos.xyz += vRippleDir * fMoveAmount * fScale;
    }

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  LeafTumble

float3 LeafTumble(float3 vPos,
                  inout float3 vDirection,
                  float fScale,
                  float3 vAnchor,
                  float3 vGrowthDir,
                  float fTrigOffset,
                  float fTime,
                  float fFlip,
                  float fTwist,
                  float fAdherence,
                  float3 vTwitch,
                  float4 vRoll,
                  bool bTwitch,
                  bool bRoll,
                  float3 vRotatedWindVector)
{
    // compute all oscillations up front
    float3 vFracs = frac((vAnchor + fTrigOffset) * 30.3);
    float fOffset = vFracs.x + vFracs.y + vFracs.z;
    float4 vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * 0.75 - fOffset, fTime * 0.01 + fOffset, fTime * 1.0 + fOffset));

    // move to the origin and get the growth direction
    float3 vOriginPos = vPos.xyz - vAnchor;
    float fLength = length(vOriginPos);

    // twist
    float fOsc = vOscillations.x + vOscillations.y * vOscillations.y;
    float3x3 matTumble = RotationMatrix(vGrowthDir, fScale * fTwist * fOsc);

    // with wind
    float3 vAxis = wind_cross(vGrowthDir, vRotatedWindVector);
    float fDot = clamp(dot(vRotatedWindVector, vGrowthDir), -1.0, 1.0);
    #ifdef SPEEDTREE_Z_UP
        vAxis.z += fDot;
    #else
        vAxis.y += fDot;
    #endif
    vAxis = normalize(vAxis);

    float fAngle = acos(fDot);

    float fAdherenceScale = 1.0;
    //if (bRoll)
    //{
    //  fAdherenceScale = Roll(fAdherenceScale, vRoll.x, vRoll.y, vRoll.z, vRoll.w, vAnchor.xyz, fTime, vRotatedWindVector);
    //}

    fOsc = vOscillations.y - vOscillations.x * vOscillations.x;

    float fTwitch = 0.0;
    if (bTwitch)
        fTwitch = Twitch(vAnchor.xyz, vTwitch.x, vTwitch.y, vTwitch.z + fOffset);

    matTumble = mul_float3x3_float3x3(matTumble, RotationMatrix(vAxis, fScale * (fAngle * fAdherence * fAdherenceScale + fOsc * fFlip + fTwitch)));

    vDirection = mul_float3x3_float3(matTumble, vDirection);
    vOriginPos = mul_float3x3_float3(matTumble, vOriginPos);

    vOriginPos = normalize(vOriginPos) * fLength;

    return (vOriginPos + vAnchor);
}


///////////////////////////////////////////////////////////////////////
//  LeafWind
//  Optimized (for instruction count) version. Assumes leaf 1 and 2 have the same options

float3 LeafWind(bool isBestWind,
                bool bLeaf2,
                float3 vPos,
                inout float3 vDirection,
                float fScale,
                float3 vAnchor,
                float fPackedGrowthDir,
                float fPackedRippleDir,
                float fRippleTrigOffset,
                float3 vRotatedWindVector)
{

    vPos = LeafRipple(vPos, vDirection, fScale, fPackedRippleDir,
                            (bLeaf2 ? _ST_WindLeaf2Ripple.x : _ST_WindLeaf1Ripple.x),
                            (bLeaf2 ? _ST_WindLeaf2Ripple.y : _ST_WindLeaf1Ripple.y),
                            false, fRippleTrigOffset);

    if (isBestWind)
    {
        float3 vGrowthDir = UnpackNormalFromFloat(fPackedGrowthDir);
        vPos = LeafTumble(vPos, vDirection, fScale, vAnchor, vGrowthDir, fPackedGrowthDir,
                          (bLeaf2 ? _ST_WindLeaf2Tumble.x : _ST_WindLeaf1Tumble.x),
                          (bLeaf2 ? _ST_WindLeaf2Tumble.y : _ST_WindLeaf1Tumble.y),
                          (bLeaf2 ? _ST_WindLeaf2Tumble.z : _ST_WindLeaf1Tumble.z),
                          (bLeaf2 ? _ST_WindLeaf2Tumble.w : _ST_WindLeaf1Tumble.w),
                          (bLeaf2 ? _ST_WindLeaf2Twitch.xyz : _ST_WindLeaf1Twitch.xyz),
                          0.0f,
                          (bLeaf2 ? true : true),
                          (bLeaf2 ? true : true),
                          vRotatedWindVector);
    }

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  RippleFrondOneSided

float3 RippleFrondOneSided(float3 vPos,
                           inout float3 vDirection,
                           float fU,
                           float fV,
                           float fRippleScale
#ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
                           , float3 vBinormal
                           , float3 vTangent
#endif
                           )
{
    float fOffset = 0.0;
    if (fU < 0.5)
        fOffset = 0.75;

    float4 vOscillations = TrigApproximate(float4((_ST_WindFrondRipple.x + fV) * _ST_WindFrondRipple.z + fOffset, 0.0, 0.0, 0.0));

    float fAmount = fRippleScale * vOscillations.x * _ST_WindFrondRipple.y;
    float3 vOffset = fAmount * vDirection;
    vPos.xyz += vOffset;

    #ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
        vTangent.xyz = normalize(vTangent.xyz + vOffset * _ST_WindFrondRipple.w);
        float3 vNewNormal = normalize(wind_cross(vBinormal.xyz, vTangent.xyz));
        if (dot(vNewNormal, vDirection.xyz) < 0.0)
            vNewNormal = -vNewNormal;
        vDirection.xyz = vNewNormal;
    #endif

    return vPos;
}

///////////////////////////////////////////////////////////////////////
//  RippleFrondTwoSided

float3 RippleFrondTwoSided(float3 vPos,
                           inout float3 vDirection,
                           float fU,
                           float fLengthPercent,
                           float fPackedRippleDir,
                           float fRippleScale
#ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
                           , float3 vBinormal
                           , float3 vTangent
#endif
                           )
{
    float4 vOscillations = TrigApproximate(float4(_ST_WindFrondRipple.x * fLengthPercent * _ST_WindFrondRipple.z, 0.0, 0.0, 0.0));

    float3 vRippleDir = UnpackNormalFromFloat(fPackedRippleDir);


    float fAmount = fRippleScale * vOscillations.x * _ST_WindFrondRipple.y;
    float3 vOffset = fAmount * vRippleDir;

    vPos.xyz += vOffset;

    #ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
        vTangent.xyz = normalize(vTangent.xyz + vOffset * _ST_WindFrondRipple.w);
        float3 vNewNormal = normalize(wind_cross(vBinormal.xyz, vTangent.xyz));
        if (dot(vNewNormal, vDirection.xyz) < 0.0)
            vNewNormal = -vNewNormal;
        vDirection.xyz = vNewNormal;
    #endif

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  RippleFrond

float3 RippleFrond(float3 vPos,
                   inout float3 vDirection,
                   float fU,
                   float fV,
                   float fPackedRippleDir,
                   float fRippleScale,
                   float fLenghtPercent
                #ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
                   , float3 vBinormal
                   , float3 vTangent
                #endif
                   )
{
    return RippleFrondOneSided(vPos,
                                vDirection,
                                fU,
                                fV,
                                fRippleScale
                            #ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
                                , vBinormal
                                , vTangent
                            #endif
                                );
}

#endif // SPEEDTREE_8_WIND


#if SPEEDTREE_9_WIND
//
// DATA DEFINITIONS
//
struct WindBranchState // 8 floats | 32B
{
    float3 m_vNoisePosTurbulence;
    float m_fIndependence;
    float m_fBend;
    float m_fOscillation;
    float m_fTurbulence;
    float m_fFlexibility;
};
struct WindRippleState // 8 floats | 32B
{
    float3 m_vNoisePosTurbulence;
    float m_fIndependence;
    float m_fPlanar;
    float m_fDirectional;
    float m_fFlexibility;
    float m_fShimmer;
};
struct CBufferSpeedTree9 // 44 floats | 176B
{
    float3 m_vWindDirection;
    float  m_fWindStrength;

    float3 m_vTreeExtents;
    float  m_fSharedHeightStart;

    float m_fBranch1StretchLimit;
    float m_fBranch2StretchLimit;
    float m_fWindIndependence; 
    float m_fImportScaling;

    WindBranchState m_sShared;
    WindBranchState m_sBranch1;
    WindBranchState m_sBranch2;
    WindRippleState m_sRipple;
};


#include "SpeedTreeShaderLibrary.cginc"

//
// CONSTANT BUFFER
//
CBUFFER_START(SpeedTreeWind)
    float4 _ST_WindVector;
    float4 _ST_TreeExtents_SharedHeightStart;
    float4 _ST_BranchStretchLimits;
    float4 _ST_Shared_NoisePosTurbulence_Independence;
    float4 _ST_Shared_Bend_Oscillation_Turbulence_Flexibility;
    float4 _ST_Branch1_NoisePosTurbulence_Independence;
    float4 _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility;
    float4 _ST_Branch2_NoisePosTurbulence_Independence;
    float4 _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility;
    float4 _ST_Ripple_NoisePosTurbulence_Independence;
    float4 _ST_Ripple_Planar_Directional_Flexibility_Shimmer;

    float4 _ST_HistoryWindVector;
    float4 _ST_HistoryTreeExtents_SharedHeightStart;
    float4 _ST_HistoryBranchStretchLimits;
    float4 _ST_HistoryShared_NoisePosTurbulence_Independence;
    float4 _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility;
    float4 _ST_HistoryBranch1_NoisePosTurbulence_Independence;
    float4 _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility;
    float4 _ST_HistoryBranch2_NoisePosTurbulence_Independence;
    float4 _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility;
    float4 _ST_HistoryRipple_NoisePosTurbulence_Independence;
    float4 _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer;
CBUFFER_END

CBufferSpeedTree9 ReadCBuffer(bool bHistory /*must be known compile-time*/)
{
    CBufferSpeedTree9 cb;
    cb.m_vWindDirection                 = bHistory ? _ST_HistoryWindVector.xyz                    : _ST_WindVector.xyz;
    cb.m_fWindStrength                  = bHistory ? _ST_HistoryWindVector.w                      : _ST_WindVector.w;
    cb.m_vTreeExtents                   = bHistory ? _ST_HistoryTreeExtents_SharedHeightStart.xyz : _ST_TreeExtents_SharedHeightStart.xyz;
    cb.m_fSharedHeightStart             = bHistory ? _ST_HistoryTreeExtents_SharedHeightStart.w   : _ST_TreeExtents_SharedHeightStart.w;
    cb.m_fBranch1StretchLimit           = bHistory ? _ST_HistoryBranchStretchLimits.x             : _ST_BranchStretchLimits.x;
    cb.m_fBranch2StretchLimit           = bHistory ? _ST_HistoryBranchStretchLimits.y             : _ST_BranchStretchLimits.y;
    cb.m_fWindIndependence              = bHistory ? _ST_HistoryBranchStretchLimits.z             : _ST_BranchStretchLimits.z;
    cb.m_fImportScaling                 = bHistory ? _ST_HistoryBranchStretchLimits.w             : _ST_BranchStretchLimits.w;

    // Shared Wind State
    cb.m_sShared.m_vNoisePosTurbulence  = bHistory ? _ST_HistoryShared_NoisePosTurbulence_Independence.xyz       : _ST_Shared_NoisePosTurbulence_Independence.xyz;
    cb.m_sShared.m_fIndependence        = bHistory ? _ST_HistoryShared_NoisePosTurbulence_Independence.w         : _ST_Shared_NoisePosTurbulence_Independence.w;
    cb.m_sShared.m_fBend                = bHistory ? _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility.x : _ST_Shared_Bend_Oscillation_Turbulence_Flexibility.x;
    cb.m_sShared.m_fOscillation         = bHistory ? _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility.y : _ST_Shared_Bend_Oscillation_Turbulence_Flexibility.y;
    cb.m_sShared.m_fTurbulence          = bHistory ? _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility.z : _ST_Shared_Bend_Oscillation_Turbulence_Flexibility.z;
    cb.m_sShared.m_fFlexibility         = bHistory ? _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility.w : _ST_Shared_Bend_Oscillation_Turbulence_Flexibility.w;

    // Branch1 Wind State
    cb.m_sBranch1.m_vNoisePosTurbulence  = bHistory ? _ST_HistoryBranch1_NoisePosTurbulence_Independence.xyz       : _ST_Branch1_NoisePosTurbulence_Independence.xyz;
    cb.m_sBranch1.m_fIndependence        = bHistory ? _ST_HistoryBranch1_NoisePosTurbulence_Independence.w         : _ST_Branch1_NoisePosTurbulence_Independence.w;
    cb.m_sBranch1.m_fBend                = bHistory ? _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility.x : _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility.x;
    cb.m_sBranch1.m_fOscillation         = bHistory ? _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility.y : _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility.y;
    cb.m_sBranch1.m_fTurbulence          = bHistory ? _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility.z : _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility.z;
    cb.m_sBranch1.m_fFlexibility         = bHistory ? _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility.w : _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility.w;

    // Branch2 Wind State
    cb.m_sBranch2.m_vNoisePosTurbulence  = bHistory ? _ST_HistoryBranch2_NoisePosTurbulence_Independence.xyz       : _ST_Branch2_NoisePosTurbulence_Independence.xyz;
    cb.m_sBranch2.m_fIndependence        = bHistory ? _ST_HistoryBranch2_NoisePosTurbulence_Independence.w         : _ST_Branch2_NoisePosTurbulence_Independence.w;
    cb.m_sBranch2.m_fBend                = bHistory ? _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility.x : _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility.x;
    cb.m_sBranch2.m_fOscillation         = bHistory ? _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility.y : _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility.y;
    cb.m_sBranch2.m_fTurbulence          = bHistory ? _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility.z : _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility.z;
    cb.m_sBranch2.m_fFlexibility         = bHistory ? _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility.w : _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility.w;

    // Ripple Wind State
    cb.m_sRipple.m_vNoisePosTurbulence   = bHistory ? _ST_HistoryRipple_NoisePosTurbulence_Independence.xyz      : _ST_Ripple_NoisePosTurbulence_Independence.xyz;
    cb.m_sRipple.m_fIndependence         = bHistory ? _ST_HistoryRipple_NoisePosTurbulence_Independence.w        : _ST_Ripple_NoisePosTurbulence_Independence.w;
    cb.m_sRipple.m_fPlanar               = bHistory ? _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer.x : _ST_Ripple_Planar_Directional_Flexibility_Shimmer.x;
    cb.m_sRipple.m_fDirectional          = bHistory ? _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer.y : _ST_Ripple_Planar_Directional_Flexibility_Shimmer.y;
    cb.m_sRipple.m_fFlexibility          = bHistory ? _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer.z : _ST_Ripple_Planar_Directional_Flexibility_Shimmer.z;
    cb.m_sRipple.m_fShimmer              = bHistory ? _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer.w : _ST_Ripple_Planar_Directional_Flexibility_Shimmer.w;

    cb.m_vWindDirection = TransformWindVectorFromWorldToLocalSpace(cb.m_vWindDirection);
    return cb;
}


//
// UTILS
//
float NoiseHash(float n) { return frac(sin(n) * 1e4); }
float NoiseHash(float2 p){ return frac(1e4 * sin(17.0f * p.x + p.y * 0.1f) * (0.1f + abs(sin(p.y * 13.0f + p.x)))); }
float QNoise(float2 x)
{
    float2 i = floor(x);
    float2 f = frac(x);
    
    // four corners in 2D of a tile
    float a = NoiseHash(i);
    float b = NoiseHash(i + float2(1.0, 0.0));
    float c = NoiseHash(i + float2(0.0, 1.0));
    float d = NoiseHash(i + float2(1.0, 1.0));
    
    // same code, with the clamps in smoothstep and common subexpressions optimized away.
    float2 u = f * f * (float2(3.0, 3.0) - float2(2.0, 2.0) * f);
    
    return lerp(a, b, u.x) + (c - a) * u.y * (1.0f - u.x) + (d - b) * u.x * u.y;
}
float4 RuntimeSdkNoise2DFlat(float3 vNoisePos3d)
{
    float2 vNoisePos = vNoisePos3d.xz;

#ifdef USE_ST_NOISE_TEXTURE // test this toggle during shader perf tuning
    return texture2D(g_samNoiseKernel, vNoisePos.xy) - float4(0.5f, 0.5f, 0.5f, 0.5f);
#else
        // fallback, slower noise lookup method
        const float c_fFrequecyScale = 20.0f;
        const float c_fAmplitudeScale = 1.0f;
        const float	c_fAmplitueShift = 0.0f;

        float fNoiseX = (QNoise(vNoisePos           * c_fFrequecyScale) + c_fAmplitueShift) * c_fAmplitudeScale;
        float fNoiseY = (QNoise(vNoisePos.yx * 0.5f * c_fFrequecyScale) + c_fAmplitueShift) * c_fAmplitudeScale;
        return float4(fNoiseX, fNoiseY, fNoiseX+fNoiseY, 0.0f) - 0.5f.xxxx;
#endif
}
float  WindUtil_Square(float  fValue) { return fValue * fValue; }
float2 WindUtil_Square(float2 fValue) { return fValue * fValue; }
float3 WindUtil_Square(float3 fValue) { return fValue * fValue; }
float4 WindUtil_Square(float4 fValue) { return fValue * fValue; }

float3 WindUtil_UnpackNormalizedFloat(float fValue)
{
    float3 vReturn = frac(float3(fValue * 0.01f, fValue, fValue * 100.0f));

    vReturn -= 0.5f;
    vReturn *= 2.0f;
    vReturn = normalize(vReturn);
    return vReturn;
}


//
// SPEEDTREE WIND 9
//
// returns position offset (caller must apply to the vertex position)
float3 RippleWindMotion(
    float3 vUp,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float  fRippleWeight,
    float3 vRippleNoisePosTurbulence,
    float  fRippleIndependence,
    float  fRippleFlexibility,
    float  fRippleDirectional,
    float  fRipplePlanar,
    float  fTreeHeight,
    float  fImportScaling
)
{
    float fImportScalingInv = (1.0f / fImportScaling);
    
    float3 vNoisePosition = vGlobalNoisePosition
                          + vRippleNoisePosTurbulence
                          + (vVertexPositionIn * fImportScalingInv) * fRippleIndependence
                          + vWindDirection * fRippleFlexibility * fRippleWeight;

    float2 vNoise = RuntimeSdkNoise2DFlat(vNoisePosition);
    vNoise.r += 0.25f;
    
    float3 vMotion = vWindDirection * vNoise.r * fRippleDirectional
                   + vUp * (vNoise.g * fRipplePlanar)
    ;
    vMotion *= fRippleWeight;
    
    return vMotion;
}

// returns updated position
float3 BranchWindPosition(
    float3 vUp,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,
    float  fPackedBranchDir,
    float  fPackedBranchNoiseOffset,
    float  fBranchWeight,
    float  fBranchStretchLimit,
    float3 vBranchNoisePosTurbulence,
    float  fBranchIndependence,
    float  fBranchTurbulence,
    float  fBranchOscillation,
    float  fBranchBend,
    float  fBranchFlexibility,
    float  fTreeHeight,
    float  fImportScaling
)
{
    float fImportScalingInv = (1.0f / fImportScaling);
    float fLength = fBranchWeight * fBranchStretchLimit;
    if (fBranchWeight * fBranchStretchLimit <= 0.0f)
    {
        return vVertexPositionIn;
    }
    
    float3 vBranchDir = WindUtil_UnpackNormalizedFloat(fPackedBranchDir);
    float3 vBranchNoiseOffset = WindUtil_UnpackNormalizedFloat(fPackedBranchNoiseOffset);

    // SpeedTree Modeler packs Z up, rotate around X for -90deg
    vBranchDir = float3(vBranchDir.x, -vBranchDir.z, vBranchDir.y); 
    vBranchNoiseOffset = float3(vBranchNoiseOffset.x, -vBranchNoiseOffset.z, vBranchNoiseOffset.y);
    
    float3 vAnchor = vVertexPositionIn - vBranchDir * fLength;
    vVertexPositionIn -= vAnchor;

    float fBranchDotWindSq = WindUtil_Square(dot(vBranchDir, vWindDirection));
    float3 vWind = normalize(vWindDirection + vUp * fBranchDotWindSq);
    
    // Undo modifications to fBranchIndependence:
    // (1) Modeler divides fBranchIndependence by fTreeHeight before export
    // (2) Importer scales fTreeHeight by fImportScaling during import
    fBranchIndependence *= (fTreeHeight * fImportScalingInv);
    
    float3 vNoisePosition = vGlobalNoisePosition
                            + vBranchNoisePosTurbulence
                            + vBranchNoiseOffset * fBranchIndependence
                            + vWind * (fBranchFlexibility * fBranchWeight);
    
    float4 vNoise = RuntimeSdkNoise2DFlat(vNoisePosition);
    vNoise.r *= 0.65; // tune down the 'flexy' branches
    vNoise.g *= 0.50; // tune down the 'flexy' branches
    
    float3 vOscillationTurbulent = vUp * fBranchTurbulence;
    
    float3 vMotion = (vWind * vNoise.r + vOscillationTurbulent * vNoise.g) * fBranchOscillation;
    vMotion += vWind * (fBranchBend * (1.0f - vNoise.b));
    vMotion *= fBranchWeight;

    return normalize(vVertexPositionIn + vMotion) * fLength + vAnchor;
}

// returns updated position
float3 SharedWindPosition(
    float3 vUp,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float  fTreeHeight,
    float  fSharedHeightStart,
    float3 vSharedNoisePosTurbulence,
    float  fSharedTurbulence,
    float  fSharedOscillation,
    float  fSharedBend,
    float  fSharedFlexibility,
    float  fImportScaling
)
{
    float fImportScalingInv = (1.0f / fImportScaling);
    float fLengthSq = dot(vVertexPositionIn, vVertexPositionIn);
    if (fLengthSq == 0.0f)
    {
        return vVertexPositionIn;
    }
    float fLength = sqrt(fLengthSq);

    float fHeight = vVertexPositionIn.y;  // y-up
    float fMaxHeight = fTreeHeight;

    float fWeight = WindUtil_Square(max(fHeight - (fMaxHeight * fSharedHeightStart), 0.0f) / fMaxHeight);

    float3 vNoisePosition = vGlobalNoisePosition
                            + vSharedNoisePosTurbulence;
                            + vWindDirection * (fSharedFlexibility * fWeight);
    
    float4 vNoise = RuntimeSdkNoise2DFlat(vNoisePosition);

    float3 vOscillationTurbulent = cross(vWindDirection, vUp) * fSharedTurbulence;
    
    float3 vMotion = (vWindDirection * vNoise.r + vOscillationTurbulent * vNoise.g) * fSharedOscillation
                   + vWindDirection * (fSharedBend * (1.0f - vNoise.b));
    ;
    vMotion *= fWeight;

    return normalize(vVertexPositionIn + vMotion) * fLength;
}

#endif // SPEEDTREE_9_WIND

#endif // SPEEDTREE_WIND_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\SpeedTreeWind.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TerrainEngine.cginc---------------


#ifndef TERRAIN_ENGINE_INCLUDED
#define TERRAIN_ENGINE_INCLUDED

// Terrain engine shader helpers

CBUFFER_START(UnityTerrain)
    // grass
    fixed4 _WavingTint;
    float4 _WaveAndDistance;    // wind speed, wave size, wind amount, max sqr distance
    float4 _CameraPosition;     // .xyz = camera position, .w = 1 / (max sqr distance)
    float3 _CameraRight, _CameraUp;

    // trees
    fixed4 _TreeInstanceColor;
    float4 _TreeInstanceScale;
    float4x4 _TerrainEngineBendTree;
    float4 _SquashPlaneNormal;
    float _SquashAmount;

    // billboards
    float3 _TreeBillboardCameraRight;
    float4 _TreeBillboardCameraUp;
    float4 _TreeBillboardCameraFront;
    float4 _TreeBillboardCameraPos;
    float4 _TreeBillboardDistances; // x = max distance ^ 2
CBUFFER_END


// ---- Vertex input structures

struct appdata_tree {
    float4 vertex : POSITION;       // position
    float4 tangent : TANGENT;       // directional AO
    float3 normal : NORMAL;         // normal
    fixed4 color : COLOR;           // .w = bend factor
    float4 texcoord : TEXCOORD0;    // UV
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct appdata_tree_billboard {
    float4 vertex : POSITION;
    fixed4 color : COLOR;           // Color
    float4 texcoord : TEXCOORD0;    // UV Coordinates
    float2 texcoord1 : TEXCOORD1;   // Billboard extrusion
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ---- Grass helpers

// Calculate a 4 fast sine-cosine pairs
// val:     the 4 input values - each must be in the range (0 to 1)
// s:       The sine of each of the 4 values
// c:       The cosine of each of the 4 values
void FastSinCos (float4 val, out float4 s, out float4 c) {
    val = val * 6.408849 - 3.1415927;
    // powers for taylor series
    float4 r5 = val * val;                  // wavevec ^ 2
    float4 r6 = r5 * r5;                        // wavevec ^ 4;
    float4 r7 = r6 * r5;                        // wavevec ^ 6;
    float4 r8 = r6 * r5;                        // wavevec ^ 8;

    float4 r1 = r5 * val;                   // wavevec ^ 3
    float4 r2 = r1 * r5;                        // wavevec ^ 5;
    float4 r3 = r2 * r5;                        // wavevec ^ 7;


    //Vectors for taylor's series expansion of sin and cos
    float4 sin7 = {1, -0.16161616, 0.0083333, -0.00019841};
    float4 cos8  = {-0.5, 0.041666666, -0.0013888889, 0.000024801587};

    // sin
    s =  val + r1 * sin7.y + r2 * sin7.z + r3 * sin7.w;

    // cos
    c = 1 + r5 * cos8.x + r6 * cos8.y + r7 * cos8.z + r8 * cos8.w;
}

fixed4 TerrainWaveGrass (inout float4 vertex, float waveAmount, fixed4 color)
{
    float4 _waveXSize = float4(0.012, 0.02, 0.06, 0.024) * _WaveAndDistance.y;
    float4 _waveZSize = float4 (0.006, .02, 0.02, 0.05) * _WaveAndDistance.y;
    float4 waveSpeed = float4 (0.3, .5, .4, 1.2) * 4;

    float4 _waveXmove = float4(0.012, 0.02, -0.06, 0.048) * 2;
    float4 _waveZmove = float4 (0.006, .02, -0.02, 0.1);

    float4 waves;
    waves = vertex.x * _waveXSize;
    waves += vertex.z * _waveZSize;

    // Add in time to model them over time
    waves += _WaveAndDistance.x * waveSpeed;

    float4 s, c;
    waves = frac (waves);
    FastSinCos (waves, s,c);

    s = s * s;

    s = s * s;

    float lighting = dot (s, normalize (float4 (1,1,.4,.2))) * .7;

    s = s * waveAmount;

    float3 waveMove = float3 (0,0,0);
    waveMove.x = dot (s, _waveXmove);
    waveMove.z = dot (s, _waveZmove);

    vertex.xz -= waveMove.xz * _WaveAndDistance.z;

    // apply color animation

    // fix for dx11/etc warning
    fixed3 waveColor = lerp (fixed3(0.5,0.5,0.5), _WavingTint.rgb, fixed3(lighting,lighting,lighting));

    // Fade the grass out before detail distance.
    // Saturate because Radeon HD drivers on OS X 10.4.10 don't saturate vertex colors properly.
    float3 offset = vertex.xyz - _CameraPosition.xyz;
    color.a = saturate (2 * (_WaveAndDistance.w - dot (offset, offset)) * _CameraPosition.w);

    return fixed4(2 * waveColor * color.rgb, color.a);
}

void TerrainBillboardGrass( inout float4 pos, float2 offset )
{
    float3 grasspos = pos.xyz - _CameraPosition.xyz;
    if (dot(grasspos, grasspos) > _WaveAndDistance.w)
        offset = 0.0;
    pos.xyz += offset.x * _CameraRight.xyz;
    pos.xyz += offset.y * _CameraUp.xyz;
}

// Grass: appdata_full usage
// color        - .xyz = color, .w = wave scale
// normal       - normal
// tangent.xy   - billboard extrusion
// texcoord     - UV coords
// texcoord1    - 2nd UV coords

void WavingGrassVert (inout appdata_full v)
{
    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;

    v.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);
}

void WavingGrassBillboardVert (inout appdata_full v)
{
    TerrainBillboardGrass (v.vertex, v.tangent.xy);
    // wave amount defined by the grass height
    float waveAmount = v.tangent.y;
    v.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);
}


// ---- Tree helpers


inline float4 Squash(in float4 pos)
{
    // To squash the tree the vertex needs to be moved in the direction
    // of the squash plane. The plane is defined by the the:
    // plane point - point lying on the plane, defined in model space
    // plane normal - _SquashPlaneNormal.xyz

    // we're pushing squashed tree plane in direction of planeNormal by amount of _SquashPlaneNormal.w
    // this squashing has to match logic of tree billboards

    float3 planeNormal = _SquashPlaneNormal.xyz;

    // unoptimized version:
    //float3 planePoint = -planeNormal * _SquashPlaneNormal.w;
    //float3 projectedVertex = pos.xyz + dot(planeNormal, (planePoint - pos)) * planeNormal;

    // optimized version:
    float3 projectedVertex = pos.xyz - (dot(planeNormal.xyz, pos.xyz) + _SquashPlaneNormal.w) * planeNormal;

    pos = float4(lerp(projectedVertex, pos.xyz, _SquashAmount), 1);

    return pos;
}

void TerrainAnimateTree( inout float4 pos, float alpha )
{
    pos.xyz *= _TreeInstanceScale.xyz;
    float3 bent = mul(_TerrainEngineBendTree, float4(pos.xyz, 0.0)).xyz;
    pos.xyz = lerp( pos.xyz, bent, alpha );

    pos = Squash(pos);
}


// ---- Billboarded tree helpers


void TerrainBillboardTree( inout float4 pos, float2 offset, float offsetz )
{
    float3 treePos = pos.xyz - _TreeBillboardCameraPos.xyz;
    float treeDistanceSqr = dot(treePos, treePos);
    if( treeDistanceSqr > _TreeBillboardDistances.x )
        offset.xy = offsetz = 0.0;

    // positioning of billboard vertices horizontally
    pos.xyz += _TreeBillboardCameraRight.xyz * offset.x;

    // tree billboards can have non-uniform scale,
    // so when looking from above (or bellow) we must use
    // billboard width as billboard height

    // 1) non-compensating
    //pos.xyz += _TreeBillboardCameraUp.xyz * offset.y;

    // 2) correct compensating (?)
    //float alpha = _TreeBillboardCameraPos.w;
    //float a = offset.y;
    //float b = offsetz;
        // 2a) using elipse-radius formula
        ////float r = abs(a * b) / sqrt(sqr(a * sin(alpha)) + sqr(b * cos(alpha))) * sign(b);
        //float r = abs(a) * b / sqrt(sqr(a * sin(alpha)) + sqr(b * cos(alpha)));
        // 2b) sin-cos lerp
        //float r = b * sin(alpha) + a * cos(alpha);
    //pos.xyz += _TreeBillboardCameraUp.xyz * r;

    // 3) incorrect compensating (using lerp)
    // _TreeBillboardCameraPos.w contains ImposterRenderTexture::GetBillboardAngleFactor()
    //float billboardAngleFactor = _TreeBillboardCameraPos.w;
    //float r = lerp(offset.y, offsetz, billboardAngleFactor);
    //pos.xyz += _TreeBillboardCameraUp.xyz * r;

    // so now we take solution #3 and complicate it even further...
    //
    // case 49851: Flying trees
    // The problem was that tree billboard was fixed on it's center, which means
    // the root of the tree is not fixed and can float around. This can be quite visible
    // on slopes (checkout the case on fogbugz for screenshots).
    //
    // We're fixing this by fixing billboards to the root of the tree.
    // Note that root of the tree is not necessary the bottom of the tree -
    // there might be significant part of the tree bellow terrain.
    // This fixation mode doesn't work when looking from above/below, because
    // billboard is so close to the ground, so we offset it by certain distance
    // when viewing angle is bigger than certain treshold (40 deg at the moment)

    // _TreeBillboardCameraPos.w contains ImposterRenderTexture::billboardAngleFactor
    float billboardAngleFactor = _TreeBillboardCameraPos.w;
    // The following line performs two things:
    // 1) peform non-uniform scale, see "3) incorrect compensating (using lerp)" above
    // 2) blend between vertical and horizontal billboard mode
    float radius = lerp(offset.y, offsetz, billboardAngleFactor);

    // positioning of billboard vertices veritally
    pos.xyz += _TreeBillboardCameraUp.xyz * radius;

    // _TreeBillboardCameraUp.w contains ImposterRenderTexture::billboardOffsetFactor
    float billboardOffsetFactor = _TreeBillboardCameraUp.w;
    // Offsetting billboad from the ground, so it doesn't get clipped by ztest.
    // In theory we should use billboardCenterOffsetY instead of offset.x,
    // but we can't because offset.y is not the same for all 4 vertices, so
    // we use offset.x which is the same for all 4 vertices (except sign).
    // And it doesn't matter a lot how much we offset, we just need to offset
    // it by some distance
    pos.xyz += _TreeBillboardCameraFront.xyz * abs(offset.x) * billboardOffsetFactor;
}


// ---- Tree Creator

float4 _Wind;

// Expand billboard and modify normal + tangent to fit
inline void ExpandBillboard (in float4x4 mat, inout float4 pos, inout float3 normal, inout float4 tangent)
{
    // tangent.w = 0 if this is a billboard
    float isBillboard = 1.0f - abs(tangent.w);

    // billboard normal
    float3 norb = normalize(mul(float4(normal, 0), mat)).xyz;

    // billboard tangent
    float3 tanb = normalize(mul(float4(tangent.xyz, 0.0f), mat)).xyz;

    pos += mul(float4(normal.xy, 0, 0), mat) * isBillboard;
    normal = lerp(normal, norb, isBillboard);
    tangent = lerp(tangent, float4(tanb, -1.0f), isBillboard);
}

float4 SmoothCurve( float4 x ) {
    return x * x *( 3.0 - 2.0 * x );
}
float4 TriangleWave( float4 x ) {
    return abs( frac( x + 0.5 ) * 2.0 - 1.0 );
}
float4 SmoothTriangleWave( float4 x ) {
    return SmoothCurve( TriangleWave( x ) );
}

// Detail bending
inline float4 AnimateVertex(float4 pos, float3 normal, float4 animParams)
{
    // animParams stored in color
    // animParams.x = branch phase
    // animParams.y = edge flutter factor
    // animParams.z = primary factor
    // animParams.w = secondary factor

    float fDetailAmp = 0.1f;
    float fBranchAmp = 0.3f;

    // Phases (object, vertex, branch)
    float fObjPhase = dot(unity_ObjectToWorld._14_24_34, 1);
    float fBranchPhase = fObjPhase + animParams.x;

    float fVtxPhase = dot(pos.xyz, animParams.y + fBranchPhase);

    // x is used for edges; y is used for branches
    float2 vWavesIn = _Time.yy + float2(fVtxPhase, fBranchPhase );

    // 1.975, 0.793, 0.375, 0.193 are good frequencies
    float4 vWaves = (frac( vWavesIn.xxyy * float4(1.975, 0.793, 0.375, 0.193) ) * 2.0 - 1.0);

    vWaves = SmoothTriangleWave( vWaves );
    float2 vWavesSum = vWaves.xz + vWaves.yw;

    // Edge (xz) and branch bending (y)
    float3 bend = animParams.y * fDetailAmp * normal.xyz;
    bend.y = animParams.w * fBranchAmp;
    pos.xyz += ((vWavesSum.xyx * bend) + (_Wind.xyz * vWavesSum.y * animParams.w)) * _Wind.w;

    // Primary bending
    // Displace position
    pos.xyz += animParams.z * _Wind.xyz;

    return pos;
}

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TerrainEngine.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TerrainPreview.cginc---------------


#ifndef TERRAIN_PREVIEW_INCLUDED
#define TERRAIN_PREVIEW_INCLUDED


// function to convert paint context pixels to heightmap uv
sampler2D _Heightmap;
float2 _HeightmapUV_PCPixelsX;
float2 _HeightmapUV_PCPixelsY;
float2 _HeightmapUV_Offset;
float2 PaintContextPixelsToHeightmapUV(float2 pcPixels)
{
    return _HeightmapUV_PCPixelsX * pcPixels.x +
        _HeightmapUV_PCPixelsY * pcPixels.y +
        _HeightmapUV_Offset;
}

// function to convert paint context pixels to object position (terrain position)
float3 _ObjectPos_PCPixelsX;
float3 _ObjectPos_PCPixelsY;
float3 _ObjectPos_HeightMapSample;
float3 _ObjectPos_Offset;
float3 PaintContextPixelsToObjectPosition(float2 pcPixels, float heightmapSample)
{
    // note: we could assume no object space rotation and make this dramatically simpler
    return _ObjectPos_PCPixelsX * pcPixels.x +
        _ObjectPos_PCPixelsY * pcPixels.y +
        _ObjectPos_HeightMapSample * heightmapSample +
        _ObjectPos_Offset;
}

// function to convert paint context pixels to brush uv
float2 _BrushUV_PCPixelsX;
float2 _BrushUV_PCPixelsY;
float2 _BrushUV_Offset;
float2 PaintContextPixelsToBrushUV(float2 pcPixels)
{
    return _BrushUV_PCPixelsX * pcPixels.x +
        _BrushUV_PCPixelsY * pcPixels.y +
        _BrushUV_Offset;
}

// function to convert terrain object position to world position
// We would normally use the ObjectToWorld / ObjectToClip calls to do this, but DrawProcedural does not set them
// 'luckily' terrains cannot be rotated or scaled, so this transform is very simple
float3 _TerrainObjectToWorldOffset;
float3 TerrainObjectToWorldPosition(float3 objectPosition)
{
    return objectPosition + _TerrainObjectToWorldOffset;
}

// function to build a procedural quad mesh
// based on the quad resolution defined by _QuadRez
// returns integer positions, starting with (0, 0), and ending with (_QuadRez.xy - 1)
float4 _QuadRez;    // quads X, quads Y, vertexCount, vertSkip
float2 BuildProceduralQuadMeshVertex(uint vertexID)
{
    int quadIndex = vertexID / 6;                       // quad index, each quad is made of 6 vertices
    int vertIndex = vertexID - quadIndex * 6;           // vertex index within the quad [0..5]
    int qY = floor((quadIndex + 0.5f) / _QuadRez.x);    // quad coords for current quad (Y)
    int qX = round(quadIndex - qY * _QuadRez.x);        // quad coords for current quad (X)

    // each quad is defined by 6 vertices (two triangles), offset from (qX,qY) as follows:
    // vX = 0, 0, 1, 1, 1, 0
    // vY = 0, 1, 1, 1, 0, 0
    float sequence[6] = { 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f };
    float vX = sequence[vertIndex];
    float vY = sequence[5 - vertIndex];     // vY is just vX reversed
    float2 coord = float2(qX + vX, qY + vY);
    return coord * _QuadRez.w;
}


float Stripe(in float x, in float stripeX, in float pixelWidth)
{
    // compute derivatives to get ddx / pixel
    float2 derivatives = float2(ddx(x), ddy(x));
    float derivLen = length(derivatives);
    float sharpen = 1.0f / max(derivLen, 0.00001f);
    return saturate(0.5f + 0.5f * (0.5f * pixelWidth - sharpen * abs(x - stripeX)));
}

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TerrainPreview.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TerrainSplatmapCommon.cginc---------------


#ifndef TERRAIN_SPLATMAP_COMMON_CGINC_INCLUDED
#define TERRAIN_SPLATMAP_COMMON_CGINC_INCLUDED

// Since 2018.3 we changed from _TERRAIN_NORMAL_MAP to _NORMALMAP to save 1 keyword.
// Since 2019.2 terrain keywords are changed to  local keywords so it doesn't really matter. You can use both.
#if defined(_NORMALMAP) && !defined(_TERRAIN_NORMAL_MAP)
    #define _TERRAIN_NORMAL_MAP
#elif !defined(_NORMALMAP) && defined(_TERRAIN_NORMAL_MAP)
    #define _NORMALMAP
#endif

#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLES)
    // GL doesn't support sperating the samplers from the texture object
    #undef TERRAIN_USE_SEPARATE_VERTEX_SAMPLER
#else
    #define TERRAIN_USE_SEPARATE_VERTEX_SAMPLER
#endif

struct Input
{
    float4 tc;
    #ifndef TERRAIN_BASE_PASS
        UNITY_FOG_COORDS(0) // needed because finalcolor oppresses fog code generation.
    #endif
};

sampler2D _Control;
float4 _Control_ST;
float4 _Control_TexelSize;
sampler2D _Splat0, _Splat1, _Splat2, _Splat3;
float4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;

#if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X)
    // Some drivers have undefined behaviors when samplers are used from the vertex shader
    // with anisotropic filtering enabled. This causes some artifacts on some devices. To be
    // sure to avoid this we use the vertex_linear_clamp_sampler sampler to sample terrain
    // maps from the VS when we can.
    #if defined(TERRAIN_USE_SEPARATE_VERTEX_SAMPLER)
        UNITY_DECLARE_TEX2D(_TerrainHeightmapTexture);
        UNITY_DECLARE_TEX2D(_TerrainNormalmapTexture);
        SamplerState sampler__TerrainNormalmapTexture;
        SamplerState vertex_linear_clamp_sampler;
    #else
        sampler2D _TerrainHeightmapTexture;
        sampler2D _TerrainNormalmapTexture;
    #endif

    float4    _TerrainHeightmapRecipSize;   // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
    float4    _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
#endif

UNITY_INSTANCING_BUFFER_START(Terrain)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData) // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

#ifdef _NORMALMAP
    sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
    float _NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3;
#endif

#ifdef _ALPHATEST_ON
    sampler2D _TerrainHolesTexture;

    void ClipHoles(float2 uv)
    {
        float hole = tex2D(_TerrainHolesTexture, uv).r;
        // Fixes bug where compression is enabled and 0 isn't actually 0 but low like 1/2047. (UUM-61913)
        float epsilon = 0.0005f;
        clip(hole < epsilon ? -1 : 1);
    }
#endif

#if defined(TERRAIN_BASE_PASS) && defined(UNITY_PASS_META)
    // When we render albedo for GI baking, we actually need to take the ST
    float4 _MainTex_ST;
#endif

void SplatmapVert(inout appdata_full v, out Input data)
{
    UNITY_INITIALIZE_OUTPUT(Input, data);

#if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X)

    float2 patchVertex = v.vertex.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float4 uvscale = instanceData.z * _TerrainHeightmapRecipSize;
    float4 uvoffset = instanceData.xyxy * uvscale;
    uvoffset.xy += 0.5f * _TerrainHeightmapRecipSize.xy;
    float2 sampleCoords = (patchVertex.xy * uvscale.xy + uvoffset.xy);

    #if defined(TERRAIN_USE_SEPARATE_VERTEX_SAMPLER)
        float hm = UnpackHeightmap(_TerrainHeightmapTexture.SampleLevel(vertex_linear_clamp_sampler, sampleCoords, 0));
    #else
        float hm = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, float4(sampleCoords, 0, 0)));
    #endif

    v.vertex.xz = (patchVertex.xy + instanceData.xy) * _TerrainHeightmapScale.xz * instanceData.z;  //(x + xBase) * hmScale.x * skipScale;
    v.vertex.y = hm * _TerrainHeightmapScale.y;
    v.vertex.w = 1.0f;

    v.texcoord.xy = (patchVertex.xy * uvscale.zw + uvoffset.zw);
    v.texcoord3 = v.texcoord2 = v.texcoord1 = v.texcoord;

    #ifdef TERRAIN_INSTANCED_PERPIXEL_NORMAL
        v.normal = float3(0, 1, 0); // TODO: reconstruct the tangent space in the pixel shader. Seems to be hard with surface shader especially when other attributes are packed together with tSpace.
        data.tc.zw = sampleCoords;
    #else
        #if defined(TERRAIN_USE_SEPARATE_VERTEX_SAMPLER)
            float3 nor = _TerrainNormalmapTexture.SampleLevel(vertex_linear_clamp_sampler, sampleCoords, 0).xyz;
        #else
            float3 nor = tex2Dlod(_TerrainNormalmapTexture, float4(sampleCoords, 0, 0)).xyz;
        #endif
        v.normal = 2.0f * nor - 1.0f;
    #endif
#endif

    v.tangent.xyz = cross(v.normal, float3(0,0,1));
    v.tangent.w = -1;

    data.tc.xy = v.texcoord.xy;
#ifdef TERRAIN_BASE_PASS
    #ifdef UNITY_PASS_META
        data.tc.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
    #endif
#else
    float4 pos = UnityObjectToClipPos(v.vertex);
    UNITY_TRANSFER_FOG(data, pos);
#endif
}

#ifndef TERRAIN_BASE_PASS

#ifdef TERRAIN_STANDARD_SHADER
void SplatmapMix(Input IN, half4 defaultAlpha, out half4 splat_control, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal)
#else
void SplatmapMix(Input IN, out half4 splat_control, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal)
#endif
{
    #ifdef _ALPHATEST_ON
        ClipHoles(IN.tc.xy);
    #endif

    // adjust splatUVs so the edges of the terrain tile lie on pixel centers
    float2 splatUV = (IN.tc.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    splat_control = tex2D(_Control, splatUV);
    weight = dot(splat_control, half4(1,1,1,1));

    #if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
        clip(weight == 0.0f ? -1 : 1);
    #endif

    // Normalize weights before lighting and restore weights in final modifier functions so that the overal
    // lighting result can be correctly weighted.
    splat_control /= (weight + 1e-3f);

    float2 uvSplat0 = TRANSFORM_TEX(IN.tc.xy, _Splat0);
    float2 uvSplat1 = TRANSFORM_TEX(IN.tc.xy, _Splat1);
    float2 uvSplat2 = TRANSFORM_TEX(IN.tc.xy, _Splat2);
    float2 uvSplat3 = TRANSFORM_TEX(IN.tc.xy, _Splat3);

    mixedDiffuse = 0.0f;
    #ifdef TERRAIN_STANDARD_SHADER
        mixedDiffuse += splat_control.r * tex2D(_Splat0, uvSplat0) * half4(1.0, 1.0, 1.0, defaultAlpha.r);
        mixedDiffuse += splat_control.g * tex2D(_Splat1, uvSplat1) * half4(1.0, 1.0, 1.0, defaultAlpha.g);
        mixedDiffuse += splat_control.b * tex2D(_Splat2, uvSplat2) * half4(1.0, 1.0, 1.0, defaultAlpha.b);
        mixedDiffuse += splat_control.a * tex2D(_Splat3, uvSplat3) * half4(1.0, 1.0, 1.0, defaultAlpha.a);
    #else
        mixedDiffuse += splat_control.r * tex2D(_Splat0, uvSplat0);
        mixedDiffuse += splat_control.g * tex2D(_Splat1, uvSplat1);
        mixedDiffuse += splat_control.b * tex2D(_Splat2, uvSplat2);
        mixedDiffuse += splat_control.a * tex2D(_Splat3, uvSplat3);
    #endif

    #ifdef _NORMALMAP
        mixedNormal  = UnpackNormalWithScale(tex2D(_Normal0, uvSplat0), _NormalScale0) * splat_control.r;
        mixedNormal += UnpackNormalWithScale(tex2D(_Normal1, uvSplat1), _NormalScale1) * splat_control.g;
        mixedNormal += UnpackNormalWithScale(tex2D(_Normal2, uvSplat2), _NormalScale2) * splat_control.b;
        mixedNormal += UnpackNormalWithScale(tex2D(_Normal3, uvSplat3), _NormalScale3) * splat_control.a;
#if defined(SHADER_API_SWITCH)
        mixedNormal.z += UNITY_HALF_MIN; // to avoid nan after normalizing
#else
        mixedNormal.z += 1e-5f; // to avoid nan after normalizing
#endif
    #endif

    #if defined(INSTANCING_ON) && defined(SHADER_TARGET_SURFACE_ANALYSIS) && defined(TERRAIN_INSTANCED_PERPIXEL_NORMAL)
        mixedNormal = float3(0, 0, 1); // make sure that surface shader compiler realizes we write to normal, as UNITY_INSTANCING_ENABLED is not defined for SHADER_TARGET_SURFACE_ANALYSIS.
    #endif

    #if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X) && defined(TERRAIN_INSTANCED_PERPIXEL_NORMAL)

        #if defined(TERRAIN_USE_SEPARATE_VERTEX_SAMPLER)
            float3 geomNormal = normalize(_TerrainNormalmapTexture.Sample(sampler__TerrainNormalmapTexture, IN.tc.zw).xyz * 2 - 1);
        #else
            float3 geomNormal = normalize(tex2D(_TerrainNormalmapTexture, IN.tc.zw).xyz * 2 - 1);
        #endif

        #ifdef _NORMALMAP
            float3 geomTangent = normalize(cross(geomNormal, float3(0, 0, 1)));
            float3 geomBitangent = normalize(cross(geomTangent, geomNormal));
            mixedNormal = mixedNormal.x * geomTangent
                          + mixedNormal.y * geomBitangent
                          + mixedNormal.z * geomNormal;
        #else
            mixedNormal = geomNormal;
        #endif
        mixedNormal = mixedNormal.xzy;
    #endif
}

#ifndef TERRAIN_SURFACE_OUTPUT
    #define TERRAIN_SURFACE_OUTPUT SurfaceOutput
#endif

void SplatmapFinalColor(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 color)
{
    color *= o.Alpha;
    #ifdef TERRAIN_SPLAT_ADDPASS
        UNITY_APPLY_FOG_COLOR(IN.fogCoord, color, fixed4(0,0,0,0));
    #else
        UNITY_APPLY_FOG(IN.fogCoord, color);
    #endif
}

void SplatmapFinalGBuffer(Input IN, TERRAIN_SURFACE_OUTPUT o, inout half4 outGBuffer0, inout half4 outGBuffer1, inout half4 outGBuffer2, inout half4 emission)
{
    UnityStandardDataApplyWeightToGbuffer(outGBuffer0, outGBuffer1, outGBuffer2, o.Alpha);
    emission *= o.Alpha;
}

#endif // TERRAIN_BASE_PASS

#endif // TERRAIN_SPLATMAP_COMMON_CGINC_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TerrainSplatmapCommon.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TerrainTool.cginc---------------


#ifndef TERRAIN_TOOL_INCLUDED
#define TERRAIN_TOOL_INCLUDED


// function to convert paint context UV to brush uv
float4 _PCUVToBrushUVScales;
float2 _PCUVToBrushUVOffset;
float2 PaintContextUVToBrushUV(float2 pcUV)
{
    return _PCUVToBrushUVScales.xy * pcUV.x +
           _PCUVToBrushUVScales.zw * pcUV.y +
           _PCUVToBrushUVOffset;
}


float2 PaintContextUVToHeightmapUV(float2 pcUV)
{
    return pcUV;
}


#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TerrainTool.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\Tessellation.cginc---------------


#ifndef TESSELLATION_CGINC_INCLUDED
#define TESSELLATION_CGINC_INCLUDED

#include "UnityShaderVariables.cginc"

// ---- utility functions

float UnityCalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess)
{
    float3 wpos = mul(unity_ObjectToWorld,vertex).xyz;
    float dist = distance (wpos, _WorldSpaceCameraPos);
    float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
    return f;
}

float4 UnityCalcTriEdgeTessFactors (float3 triVertexFactors)
{
    float4 tess;
    tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
    tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
    tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
    tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
    return tess;
}

float UnityCalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen)
{
    // distance to edge center
    float dist = distance (0.5 * (wpos0+wpos1), _WorldSpaceCameraPos);
    // length of the edge
    float len = distance(wpos0, wpos1);
    // edgeLen is approximate desired size in pixels
    float f = max(len * _ScreenParams.y / (edgeLen * dist), 1.0);
    return f;
}

float UnityDistanceFromPlane (float3 pos, float4 plane)
{
    float d = dot (float4(pos,1.0f), plane);
    return d;
}


// Returns true if triangle with given 3 world positions is outside of camera's view frustum.
// cullEps is distance outside of frustum that is still considered to be inside (i.e. max displacement)
bool UnityWorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps)
{
    float4 planeTest;

    // left
    planeTest.x = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f );
    // right
    planeTest.y = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f );
    // top
    planeTest.z = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f );
    // bottom
    planeTest.w = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f );

    // has to pass all 4 plane tests to be visible
    return !all (planeTest);
}



// ---- functions that compute tessellation factors


// Distance based tessellation:
// Tessellation level is "tess" before "minDist" from camera, and linearly decreases to 1
// up to "maxDist" from camera.
float4 UnityDistanceBasedTess (float4 v0, float4 v1, float4 v2, float minDist, float maxDist, float tess)
{
    float3 f;
    f.x = UnityCalcDistanceTessFactor (v0,minDist,maxDist,tess);
    f.y = UnityCalcDistanceTessFactor (v1,minDist,maxDist,tess);
    f.z = UnityCalcDistanceTessFactor (v2,minDist,maxDist,tess);

    return UnityCalcTriEdgeTessFactors (f);
}

// Desired edge length based tessellation:
// Approximate resulting edge length in pixels is "edgeLength".
// Does not take viewing FOV into account, just flat out divides factor by distance.
float4 UnityEdgeLengthBasedTess (float4 v0, float4 v1, float4 v2, float edgeLength)
{
    float3 pos0 = mul(unity_ObjectToWorld,v0).xyz;
    float3 pos1 = mul(unity_ObjectToWorld,v1).xyz;
    float3 pos2 = mul(unity_ObjectToWorld,v2).xyz;
    float4 tess;
    tess.x = UnityCalcEdgeTessFactor (pos1, pos2, edgeLength);
    tess.y = UnityCalcEdgeTessFactor (pos2, pos0, edgeLength);
    tess.z = UnityCalcEdgeTessFactor (pos0, pos1, edgeLength);
    tess.w = (tess.x + tess.y + tess.z) / 3.0f;
    return tess;
}


// Same as UnityEdgeLengthBasedTess, but also does patch frustum culling:
// patches outside of camera's view are culled before GPU tessellation. Saves some wasted work.
float4 UnityEdgeLengthBasedTessCull (float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement)
{
    float3 pos0 = mul(unity_ObjectToWorld,v0).xyz;
    float3 pos1 = mul(unity_ObjectToWorld,v1).xyz;
    float3 pos2 = mul(unity_ObjectToWorld,v2).xyz;
    float4 tess;

    if (UnityWorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement))
    {
        tess = 0.0f;
    }
    else
    {
        tess.x = UnityCalcEdgeTessFactor (pos1, pos2, edgeLength);
        tess.y = UnityCalcEdgeTessFactor (pos2, pos0, edgeLength);
        tess.z = UnityCalcEdgeTessFactor (pos0, pos1, edgeLength);
        tess.w = (tess.x + tess.y + tess.z) / 3.0f;
    }
    return tess;
}



#endif // TESSELLATION_CGINC_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\Tessellation.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TextCoreProperties.cginc---------------


// UI Editable properties
uniform sampler2D   _FaceTex;                   // Alpha : Signed Distance
uniform float       _FaceUVSpeedX;
uniform float       _FaceUVSpeedY;
uniform fixed4      _FaceColor;                 // RGBA : Color + Opacity
uniform float       _FaceDilate;                // v[ 0, 1]
uniform float       _OutlineSoftness;           // v[ 0, 1]

uniform sampler2D   _OutlineTex;                // RGBA : Color + Opacity
uniform float       _OutlineUVSpeedX;
uniform float       _OutlineUVSpeedY;
uniform fixed4      _OutlineColor;              // RGBA : Color + Opacity
uniform float       _OutlineWidth;              // v[ 0, 1]

uniform float       _Bevel;                     // v[ 0, 1]
uniform float       _BevelOffset;               // v[-1, 1]
uniform float       _BevelWidth;                // v[-1, 1]
uniform float       _BevelClamp;                // v[ 0, 1]
uniform float       _BevelRoundness;            // v[ 0, 1]

uniform sampler2D   _BumpMap;                   // Normal map
uniform float       _BumpOutline;               // v[ 0, 1]
uniform float       _BumpFace;                  // v[ 0, 1]

uniform samplerCUBE _Cube;                      // Cube / sphere map
uniform fixed4      _ReflectFaceColor;          // RGB intensity
uniform fixed4      _ReflectOutlineColor;
//uniform float     _EnvTiltX;                  // v[-1, 1]
//uniform float     _EnvTiltY;                  // v[-1, 1]
uniform float3      _EnvMatrixRotation;
uniform float4x4    _EnvMatrix;

uniform fixed4      _SpecularColor;             // RGB intensity
uniform float       _LightAngle;                // v[ 0,Tau]
uniform float       _SpecularPower;             // v[ 0, 1]
uniform float       _Reflectivity;              // v[ 5, 15]
uniform float       _Diffuse;                   // v[ 0, 1]
uniform float       _Ambient;                   // v[ 0, 1]

uniform fixed4      _UnderlayColor;             // RGBA : Color + Opacity
uniform float       _UnderlayOffsetX;           // v[-1, 1]
uniform float       _UnderlayOffsetY;           // v[-1, 1]
uniform float       _UnderlayDilate;            // v[-1, 1]
uniform float       _UnderlaySoftness;          // v[ 0, 1]

uniform fixed4      _GlowColor;                 // RGBA : Color + Intesity
uniform float       _GlowOffset;                // v[-1, 1]
uniform float       _GlowOuter;                 // v[ 0, 1]
uniform float       _GlowInner;                 // v[ 0, 1]
uniform float       _GlowPower;                 // v[ 1, 1/(1+4*4)]

// API Editable properties
uniform float       _ShaderFlags;
uniform float       _WeightNormal;
uniform float       _WeightBold;

uniform float       _ScaleRatioA;
uniform float       _ScaleRatioB;
uniform float       _ScaleRatioC;

uniform float       _VertexOffsetX;
uniform float       _VertexOffsetY;

//uniform float     _UseClipRect;
uniform float       _MaskID;
uniform sampler2D   _MaskTex;
uniform float4      _MaskCoord;
uniform float4      _ClipRect;  // bottom left(x,y) : top right(z,w)
//uniform float     _MaskWipeControl;
//uniform float     _MaskEdgeSoftness;
//uniform fixed4        _MaskEdgeColor;
//uniform bool      _MaskInverse;

uniform float       _MaskSoftnessX;
uniform float       _MaskSoftnessY;

// Font Atlas properties
uniform sampler2D   _MainTex;
uniform float       _TextureWidth;
uniform float       _TextureHeight;
uniform float       _GradientScale;
uniform float       _ScaleX;
uniform float       _ScaleY;
uniform float       _PerspectiveFilter;
uniform float       _Sharpness;


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TextCoreProperties.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TextCore_Properties.cginc---------------


// UI Editable properties
uniform sampler2D   _FaceTex;                   // Alpha : Signed Distance
uniform float       _FaceUVSpeedX;
uniform float       _FaceUVSpeedY;
uniform fixed4      _FaceColor;                 // RGBA : Color + Opacity
uniform float       _FaceDilate;                // v[ 0, 1]
uniform float       _OutlineSoftness;           // v[ 0, 1]

uniform sampler2D   _OutlineTex;                // RGBA : Color + Opacity
uniform float       _OutlineUVSpeedX;
uniform float       _OutlineUVSpeedY;
uniform fixed4      _OutlineColor;              // RGBA : Color + Opacity
uniform float       _OutlineWidth;              // v[ 0, 1]

uniform float       _Bevel;                     // v[ 0, 1]
uniform float       _BevelOffset;               // v[-1, 1]
uniform float       _BevelWidth;                // v[-1, 1]
uniform float       _BevelClamp;                // v[ 0, 1]
uniform float       _BevelRoundness;            // v[ 0, 1]

uniform sampler2D   _BumpMap;                   // Normal map
uniform float       _BumpOutline;               // v[ 0, 1]
uniform float       _BumpFace;                  // v[ 0, 1]

uniform samplerCUBE _Cube;                      // Cube / sphere map
uniform fixed4      _ReflectFaceColor;          // RGB intensity
uniform fixed4      _ReflectOutlineColor;
//uniform float     _EnvTiltX;                  // v[-1, 1]
//uniform float     _EnvTiltY;                  // v[-1, 1]
uniform float3      _EnvMatrixRotation;
uniform float4x4    _EnvMatrix;

uniform fixed4      _SpecularColor;             // RGB intensity
uniform float       _LightAngle;                // v[ 0,Tau]
uniform float       _SpecularPower;             // v[ 0, 1]
uniform float       _Reflectivity;              // v[ 5, 15]
uniform float       _Diffuse;                   // v[ 0, 1]
uniform float       _Ambient;                   // v[ 0, 1]

uniform fixed4      _UnderlayColor;             // RGBA : Color + Opacity
uniform float       _UnderlayOffsetX;           // v[-1, 1]
uniform float       _UnderlayOffsetY;           // v[-1, 1]
uniform float       _UnderlayDilate;            // v[-1, 1]
uniform float       _UnderlaySoftness;          // v[ 0, 1]

uniform fixed4      _GlowColor;                 // RGBA : Color + Intesity
uniform float       _GlowOffset;                // v[-1, 1]
uniform float       _GlowOuter;                 // v[ 0, 1]
uniform float       _GlowInner;                 // v[ 0, 1]
uniform float       _GlowPower;                 // v[ 1, 1/(1+4*4)]

// API Editable properties
uniform float       _ShaderFlags;
uniform float       _WeightNormal;
uniform float       _WeightBold;

uniform float       _ScaleRatioA;
uniform float       _ScaleRatioB;
uniform float       _ScaleRatioC;

uniform float       _VertexOffsetX;
uniform float       _VertexOffsetY;

//uniform float     _UseClipRect;
uniform float       _MaskID;
uniform sampler2D   _MaskTex;
uniform float4      _MaskCoord;
uniform float4      _ClipRect;  // bottom left(x,y) : top right(z,w)
//uniform float     _MaskWipeControl;
//uniform float     _MaskEdgeSoftness;
//uniform fixed4        _MaskEdgeColor;
//uniform bool      _MaskInverse;

uniform float       _MaskSoftnessX;
uniform float       _MaskSoftnessY;

// Font Atlas properties
uniform sampler2D   _MainTex;
uniform float       _TextureWidth;
uniform float       _TextureHeight;
uniform float       _GradientScale;
uniform float       _ScaleX;
uniform float       _ScaleY;
uniform float       _PerspectiveFilter;
uniform float       _Sharpness;


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TextCore_Properties.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TextCore_SDF_SSD.cginc---------------


struct vertex_t
{
    float4  position        : POSITION;
    float3  normal          : NORMAL;
    float4  color           : COLOR;
    float4  texcoord0       : TEXCOORD0;
    float2  texcoord1       : TEXCOORD1;
};

struct pixel_t
{
    float4  position        : SV_POSITION;
    float4  faceColor       : COLOR;
    float4  outlineColor    : COLOR1;
    float2  texcoord0       : TEXCOORD0;
    float4  param           : TEXCOORD1;        // weight, scaleRatio
    float2  clipUV          : TEXCOORD2;
    #if (UNDERLAY_ON || UNDERLAY_INNER)
    float4  texcoord2       : TEXCOORD3;
    float4  underlayColor   : COLOR2;
    #endif
};

sampler2D _GUIClipTexture;
uniform float4x4 unity_GUIClipTextureMatrix;
float4 _MainTex_TexelSize;

float4 SRGBToLinear(float4 rgba)
{
    return float4(lerp(rgba.rgb / 12.92f, pow((rgba.rgb + 0.055f) / 1.055f, 2.4f), step(0.04045f, rgba.rgb)), rgba.a);
}

pixel_t VertShader(vertex_t input)
{
    pixel_t output;

    float bold = step(input.texcoord1.y, 0);

    float4 vert = input.position;
    vert.x += _VertexOffsetX;
    vert.y += _VertexOffsetY;

    float4 vPosition = UnityObjectToClipPos(vert);

    float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
    weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

    // Generate UV for the Clip Texture
    float3 eyePos = UnityObjectToViewPos(input.position);
    float2 clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));

    float4 color = input.color;
    #if (FORCE_LINEAR && !UNITY_COLORSPACE_GAMMA)
    color = SRGBToLinear(input.color);
    #endif

    float opacity = color.a;
    #if (UNDERLAY_ON | UNDERLAY_INNER)
    opacity = 1.0;
    #endif

    float4 faceColor = float4(color.rgb, opacity) * _FaceColor;
    faceColor.rgb *= faceColor.a;

    float4 outlineColor = _OutlineColor;
    outlineColor.a *= opacity;
    outlineColor.rgb *= outlineColor.a;

    output.position = vPosition;
    output.faceColor = faceColor;
    output.outlineColor = outlineColor;
    output.texcoord0 = float2(input.texcoord0.xy);
    output.param = float4(0.5 - weight, 1.3333 * _GradientScale * (_Sharpness + 1) / _MainTex_TexelSize.w, _OutlineWidth * _ScaleRatioA * 0.5, 0);
    output.clipUV = clipUV;

    #if (UNDERLAY_ON || UNDERLAY_INNER)
    float4 underlayColor = _UnderlayColor;
    underlayColor.rgb *= underlayColor.a;

    float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _MainTex_TexelSize.z;
    float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _MainTex_TexelSize.w;

    output.texcoord2 = float4(input.texcoord0 + float2(x, y), input.color.a, 0);
    output.underlayColor = underlayColor;
    #endif

    return output;
}

float4 PixShader(pixel_t input) : SV_Target
{
    float d = tex2D(_MainTex, input.texcoord0.xy).a;

    float2 UV = input.texcoord0.xy;

    float ps = abs(ddx(UV.y)) + abs(ddy(UV.y)); // Size of a pixel in texel space (approximation)
    float scale = input.param.y / ps;

    #if (UNDERLAY_ON | UNDERLAY_INNER)
    float layerScale = scale;
    layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
    float layerBias = input.param.x * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);
    #endif

    scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);

    float4 faceColor = input.faceColor * saturate((d - input.param.x) * scale + 0.5);

    #ifdef OUTLINE_ON
    float4 outlineColor = lerp(input.faceColor, input.outlineColor, sqrt(min(1.0, input.param.z * scale * 2)));
    faceColor = lerp(outlineColor, input.faceColor, saturate((d - input.param.x - input.param.z) * scale + 0.5));
    faceColor *= saturate((d - input.param.x + input.param.z) * scale + 0.5);
    #endif

    #if UNDERLAY_ON
    d = tex2D(_MainTex, input.texcoord2.xy).a * layerScale;
    faceColor += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate(d - layerBias) * (1 - faceColor.a);
    #endif

    #if UNDERLAY_INNER
    float bias = input.param.x * scale - 0.5;
    float sd = saturate(d * scale - bias - input.param.z);
    d = tex2D(_MainTex, input.texcoord2.xy).a * layerScale;
    faceColor += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate(d - layerBias)) * sd * (1 - faceColor.a);
    #endif

    #if (UNDERLAY_ON | UNDERLAY_INNER)
    faceColor *= input.texcoord2.z;
    #endif

    faceColor *= tex2D(_GUIClipTexture, input.clipUV).a;

    return faceColor;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\TextCore_SDF_SSD.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityBuiltin2xTreeLibrary.cginc---------------


// Shared tree shader functionality for Unity 2.x tree shaders

#include "HLSLSupport.cginc"
#include "UnityCG.cginc"
#include "TerrainEngine.cginc"

float _Occlusion, _AO, _BaseLight;
fixed4 _Color;

#ifdef USE_CUSTOM_LIGHT_DIR
CBUFFER_START(UnityTerrainImposter)
    float3 _TerrainTreeLightDirections[4];
    float4 _TerrainTreeLightColors[4];
CBUFFER_END
#endif

CBUFFER_START(UnityPerCamera2)
float4x4 _CameraToWorld;
CBUFFER_END

float _HalfOverCutoff;

struct v2f {
    float4 pos : SV_POSITION;
    float4 uv : TEXCOORD0;
    half4 color : TEXCOORD1;
    UNITY_FOG_COORDS(2)
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f leaves(appdata_tree v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainAnimateTree(v.vertex, v.color.w);

    float3 viewpos = UnityObjectToViewPos(v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord;

    float4 lightDir = 0;
    float4 lightColor = 0;
    lightDir.w = _AO;

    float4 light = UNITY_LIGHTMODEL_AMBIENT;

    for (int i = 0; i < 4; i++) {
        float atten = 1.0;
        #ifdef USE_CUSTOM_LIGHT_DIR
            lightDir.xyz = _TerrainTreeLightDirections[i];
            lightColor = _TerrainTreeLightColors[i];
        #else
                float3 toLight = unity_LightPosition[i].xyz - viewpos.xyz * unity_LightPosition[i].w;
                toLight.z *= -1.0;
                lightDir.xyz = mul( (float3x3)_CameraToWorld, normalize(toLight) );
                float lengthSq = dot(toLight, toLight);
                atten = 1.0 / (1.0 + lengthSq * unity_LightAtten[i].z);

                lightColor.rgb = unity_LightColor[i].rgb;
        #endif

        lightDir.xyz *= _Occlusion;
        float occ =  dot (v.tangent, lightDir);
        occ = max(0, occ);
        occ += _BaseLight;
        light += lightColor * (occ * atten);
    }

    o.color = light * _Color * _TreeInstanceColor;
    o.color.a = 0.5 * _HalfOverCutoff;

    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
}

v2f bark(appdata_tree v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainAnimateTree(v.vertex, v.color.w);

    float3 viewpos = UnityObjectToViewPos(v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord;

    float4 lightDir = 0;
    float4 lightColor = 0;
    lightDir.w = _AO;

    float4 light = UNITY_LIGHTMODEL_AMBIENT;

    for (int i = 0; i < 4; i++) {
        float atten = 1.0;
        #ifdef USE_CUSTOM_LIGHT_DIR
            lightDir.xyz = _TerrainTreeLightDirections[i];
            lightColor = _TerrainTreeLightColors[i];
        #else
                float3 toLight = unity_LightPosition[i].xyz - viewpos.xyz * unity_LightPosition[i].w;
                toLight.z *= -1.0;
                lightDir.xyz = mul( (float3x3)_CameraToWorld, normalize(toLight) );
                float lengthSq = dot(toLight, toLight);
                atten = 1.0 / (1.0 + lengthSq * unity_LightAtten[i].z);

                lightColor.rgb = unity_LightColor[i].rgb;
        #endif


        float diffuse = dot (v.normal, lightDir.xyz);
        diffuse = max(0, diffuse);
        diffuse *= _AO * v.tangent.w + _BaseLight;
        light += lightColor * (diffuse * atten);
    }

    light.a = 1;
    o.color = light * _Color * _TreeInstanceColor;

    #ifdef WRITE_ALPHA_1
    o.color.a = 1;
    #endif

    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityBuiltin2xTreeLibrary.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityBuiltin3xTreeLibrary.cginc---------------


#ifndef UNITY_BUILTIN_3X_TREE_LIBRARY_INCLUDED
#define UNITY_BUILTIN_3X_TREE_LIBRARY_INCLUDED

// Shared tree shader functionality for Unity 3.x Tree Creator shaders

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "TerrainEngine.cginc"

fixed4 _Color;
fixed3 _TranslucencyColor;
fixed _TranslucencyViewDependency;
half _ShadowStrength;

struct LeafSurfaceOutput {
    fixed3 Albedo;
    fixed3 Normal;
    fixed3 Emission;
    fixed Translucency;
    half Specular;
    fixed Gloss;
    fixed Alpha;
};

inline half4 LightingTreeLeaf (LeafSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
{
    half3 h = normalize (lightDir + viewDir);

    half nl = dot (s.Normal, lightDir);

    half nh = max (0, dot (s.Normal, h));
    half spec = pow (nh, s.Specular * 128.0) * s.Gloss;

    // view dependent back contribution for translucency
    fixed backContrib = saturate(dot(viewDir, -lightDir));

    // normally translucency is more like -nl, but looks better when it's view dependent
    backContrib = lerp(saturate(-nl), backContrib, _TranslucencyViewDependency);

    fixed3 translucencyColor = backContrib * s.Translucency * _TranslucencyColor;

    // wrap-around diffuse
    nl = max(0, nl * 0.6 + 0.4);

    fixed4 c;
    /////@TODO: what is is this multiply 2x here???
    c.rgb = s.Albedo * (translucencyColor * 2 + nl);
    c.rgb = c.rgb * _LightColor0.rgb + spec;

    // For directional lights, apply less shadow attenuation
    // based on shadow strength parameter.
    #if defined(DIRECTIONAL) || defined(DIRECTIONAL_COOKIE)
    c.rgb *= lerp(1, atten, _ShadowStrength);
    #else
    c.rgb *= atten;
    #endif

    c.a = s.Alpha;

    return c;
}

// -------- Per-vertex lighting functions for "Tree Creator Leaves Fast" shaders

fixed3 ShadeTranslucentMainLight (float4 vertex, float3 normal)
{
    float3 viewDir = normalize(WorldSpaceViewDir(vertex));
    float3 lightDir = normalize(WorldSpaceLightDir(vertex));
    fixed3 lightColor = _LightColor0.rgb;

    float nl = dot (normal, lightDir);

    // view dependent back contribution for translucency
    fixed backContrib = saturate(dot(viewDir, -lightDir));

    // normally translucency is more like -nl, but looks better when it's view dependent
    backContrib = lerp(saturate(-nl), backContrib, _TranslucencyViewDependency);

    // wrap-around diffuse
    fixed diffuse = max(0, nl * 0.6 + 0.4);

    return lightColor.rgb * (diffuse + backContrib * _TranslucencyColor);
}

fixed3 ShadeTranslucentLights (float4 vertex, float3 normal)
{
    float3 viewDir = normalize(WorldSpaceViewDir(vertex));
    float3 mainLightDir = normalize(WorldSpaceLightDir(vertex));
    float3 frontlight = ShadeSH9 (float4(normal,1.0));
    float3 backlight = ShadeSH9 (float4(-normal,1.0));
    #ifdef VERTEXLIGHT_ON
    float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;
    frontlight += Shade4PointLights (
        unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
        unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
        unity_4LightAtten0, worldPos, normal);
    backlight += Shade4PointLights (
        unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
        unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
        unity_4LightAtten0, worldPos, -normal);
    #endif

    // view dependent back contribution for translucency using main light as a cue
    fixed backContrib = saturate(dot(viewDir, -mainLightDir));
    backlight = lerp(backlight, backlight * backContrib, _TranslucencyViewDependency);

    // as we integrate over whole sphere instead of normal hemi-sphere
    // lighting gets too washed out, so let's half it down
    return 0.5 * (frontlight + backlight * _TranslucencyColor);
}

void TreeVertBark (inout appdata_full v)
{
    v.vertex.xyz *= _TreeInstanceScale.xyz;
    v.vertex = AnimateVertex(v.vertex, v.normal, float4(v.color.xy, v.texcoord1.xy));

    v.vertex = Squash(v.vertex);

    v.color.rgb = _TreeInstanceColor.rgb * _Color.rgb;
    v.normal = normalize(v.normal);
    v.tangent.xyz = normalize(v.tangent.xyz);
}

void TreeVertLeaf (inout appdata_full v)
{
    ExpandBillboard (UNITY_MATRIX_IT_MV, v.vertex, v.normal, v.tangent);
    v.vertex.xyz *= _TreeInstanceScale.xyz;
    v.vertex = AnimateVertex (v.vertex,v.normal, float4(v.color.xy, v.texcoord1.xy));

    v.vertex = Squash(v.vertex);

    v.color.rgb = _TreeInstanceColor.rgb * _Color.rgb;
    v.normal = normalize(v.normal);
    v.tangent.xyz = normalize(v.tangent.xyz);
}

float ScreenDitherToAlpha(float x, float y, float c0)
{
#if (SHADER_TARGET > 30) || defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)
    //dither matrix reference: https://en.wikipedia.org/wiki/Ordered_dithering
    const float dither[64] = {
        0, 32, 8, 40, 2, 34, 10, 42,
        48, 16, 56, 24, 50, 18, 58, 26 ,
        12, 44, 4, 36, 14, 46, 6, 38 ,
        60, 28, 52, 20, 62, 30, 54, 22,
        3, 35, 11, 43, 1, 33, 9, 41,
        51, 19, 59, 27, 49, 17, 57, 25,
        15, 47, 7, 39, 13, 45, 5, 37,
        63, 31, 55, 23, 61, 29, 53, 21 };

    int xMat = int(x) & 7;
    int yMat = int(y) & 7;

    float limit = (dither[yMat * 8 + xMat] + 11.0) / 64.0;
    //could also use saturate(step(0.995, c0) + limit*(c0));
    //original step(limit, c0 + 0.01);

    return lerp(limit*c0, 1.0, c0);
#else
    return 1.0;
#endif
}

float ComputeAlphaCoverage(float4 screenPos, float fadeAmount)
{
#if (SHADER_TARGET > 30) || defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)
    float2 pixelPosition = screenPos.xy / (screenPos.w + 0.00001);
    pixelPosition *= _ScreenParams;
    float coverage = ScreenDitherToAlpha(pixelPosition.x, pixelPosition.y, fadeAmount);
    return coverage;
#else
    return 1.0;
#endif
}

#endif // UNITY_BUILTIN_3X_TREE_LIBRARY_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityBuiltin3xTreeLibrary.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityCG.cginc---------------


#ifndef UNITY_CG_INCLUDED
#define UNITY_CG_INCLUDED

#define UNITY_PI            3.14159265359f
#define UNITY_TWO_PI        6.28318530718f
#define UNITY_FOUR_PI       12.56637061436f
#define UNITY_INV_PI        0.31830988618f
#define UNITY_INV_TWO_PI    0.15915494309f
#define UNITY_INV_FOUR_PI   0.07957747155f
#define UNITY_HALF_PI       1.57079632679f
#define UNITY_INV_HALF_PI   0.636619772367f

#define UNITY_HALF_MIN      6.103515625e-5  // 2^-14, the same value for 10, 11 and 16-bit: https://www.khronos.org/opengl/wiki/Small_Float_Formats

// Should SH (light probe / ambient) calculations be performed?
// - When both static and dynamic lightmaps are available, no SH evaluation is performed
// - When static and dynamic lightmaps are not available, SH evaluation is always performed
// - For low level LODs, static lightmap and real-time GI from light probes can be combined together
// - Passes that don't do ambient (additive, shadowcaster etc.) should not do SH either.
#define UNITY_SHOULD_SAMPLE_SH (defined(LIGHTPROBE_SH) && !defined(UNITY_PASS_FORWARDADD) && !defined(UNITY_PASS_SHADOWCASTER) && !defined(UNITY_PASS_META))

#include "UnityShaderVariables.cginc"
#include "UnityShaderUtilities.cginc"
#include "UnityInstancing.cginc"

#ifdef UNITY_COLORSPACE_GAMMA
#define unity_ColorSpaceGrey fixed4(0.5, 0.5, 0.5, 0.5)
#define unity_ColorSpaceDouble fixed4(2.0, 2.0, 2.0, 2.0)
#define unity_ColorSpaceDielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
#define unity_ColorSpaceLuminance half4(0.22, 0.707, 0.071, 0.0) // Legacy: alpha is set to 0.0 to specify gamma mode
#else // Linear values
#define unity_ColorSpaceGrey fixed4(0.214041144, 0.214041144, 0.214041144, 0.5)
#define unity_ColorSpaceDouble fixed4(4.59479380, 4.59479380, 4.59479380, 2.0)
#define unity_ColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
#define unity_ColorSpaceLuminance half4(0.0396819152, 0.458021790, 0.00609653955, 1.0) // Legacy: alpha is set to 1.0 to specify linear mode
#endif

// -------------------------------------------------------------------
//  helper functions and macros used in many standard shaders


#if defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE) || defined (POINT) || defined (SPOT) || defined (POINT_NOATT) || defined (POINT_COOKIE)
#define USING_LIGHT_MULTI_COMPILE
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) || defined(SHADER_API_WEBGPU) // D3D11, D3D12, XB1, PS4, iOS, macOS, tvOS, glcore, gles3, webgl2.0, Switch
// Real-support for depth-format cube shadow map.
#define SHADOWS_CUBE_IN_DEPTH_TEX
#endif

#define SCALED_NORMAL v.normal


// These constants must be kept in sync with RGBMRanges.h
#define LIGHTMAP_RGBM_SCALE 5.0
#define EMISSIVE_RGBM_SCALE 97.0

struct appdata_base {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct appdata_tan {
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct appdata_full {
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;
    float4 texcoord3 : TEXCOORD3;
    fixed4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Legacy for compatibility with existing shaders
inline bool IsGammaSpace()
{
    #ifdef UNITY_COLORSPACE_GAMMA
        return true;
    #else
        return false;
    #endif
}

inline float GammaToLinearSpaceExact (float value)
{
    if (value <= 0.04045F)
        return value / 12.92F;
    else if (value < 1.0F)
        return pow((value + 0.055F)/1.055F, 2.4F);
    else
        return pow(value, 2.2F);
}

inline half3 GammaToLinearSpace (half3 sRGB)
{
    // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);

    // Precise version, useful for debugging.
    //return half3(GammaToLinearSpaceExact(sRGB.r), GammaToLinearSpaceExact(sRGB.g), GammaToLinearSpaceExact(sRGB.b));
}

inline float LinearToGammaSpaceExact (float value)
{
    if (value <= 0.0F)
        return 0.0F;
    else if (value <= 0.0031308F)
        return 12.92F * value;
    else if (value < 1.0F)
        return 1.055F * pow(value, 0.4166667F) - 0.055F;
    else
        return pow(value, 0.45454545F);
}

inline half3 LinearToGammaSpace (half3 linRGB)
{
    linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
    // An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);

    // Exact version, useful for debugging.
    //return half3(LinearToGammaSpaceExact(linRGB.r), LinearToGammaSpaceExact(linRGB.g), LinearToGammaSpaceExact(linRGB.b));
}

// Tranforms position from world to homogenous space
inline float4 UnityWorldToClipPos( in float3 pos )
{
    return mul(UNITY_MATRIX_VP, float4(pos, 1.0));
}

// Tranforms position from view to homogenous space
inline float4 UnityViewToClipPos( in float3 pos )
{
    return mul(UNITY_MATRIX_P, float4(pos, 1.0));
}

// Tranforms position from object to camera space
inline float3 UnityObjectToViewPos( in float3 pos )
{
    return mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, float4(pos, 1.0))).xyz;
}
inline float3 UnityObjectToViewPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return UnityObjectToViewPos(pos.xyz);
}

// Tranforms position from world to camera space
inline float3 UnityWorldToViewPos( in float3 pos )
{
    return mul(UNITY_MATRIX_V, float4(pos, 1.0)).xyz;
}

// Transforms direction from object to world space
inline float3 UnityObjectToWorldDir( in float3 dir )
{
    return normalize(mul((float3x3)unity_ObjectToWorld, dir));
}

// Transforms direction from world to object space
inline float3 UnityWorldToObjectDir( in float3 dir )
{
    return normalize(mul((float3x3)unity_WorldToObject, dir));
}

// Transforms normal from object to world space
inline float3 UnityObjectToWorldNormal( in float3 norm )
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return UnityObjectToWorldDir(norm);
#else
    // mul(IT_M, norm) => mul(norm, I_M) => {dot(norm, I_M.col0), dot(norm, I_M.col1), dot(norm, I_M.col2)}
    return normalize(mul(norm, (float3x3)unity_WorldToObject));
#endif
}

// Computes world space light direction, from world space position
inline float3 UnityWorldSpaceLightDir( in float3 worldPos )
{
    #ifndef USING_LIGHT_MULTI_COMPILE
        return _WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w;
    #else
        #ifndef USING_DIRECTIONAL_LIGHT
        return _WorldSpaceLightPos0.xyz - worldPos;
        #else
        return _WorldSpaceLightPos0.xyz;
        #endif
    #endif
}

// Computes world space light direction, from object space position
// *Legacy* Please use UnityWorldSpaceLightDir instead
inline float3 WorldSpaceLightDir( in float4 localPos )
{
    float3 worldPos = mul(unity_ObjectToWorld, localPos).xyz;
    return UnityWorldSpaceLightDir(worldPos);
}

// Computes object space light direction
inline float3 ObjSpaceLightDir( in float4 v )
{
    float3 objSpaceLightPos = mul(unity_WorldToObject, _WorldSpaceLightPos0).xyz;
    #ifndef USING_LIGHT_MULTI_COMPILE
        return objSpaceLightPos.xyz - v.xyz * _WorldSpaceLightPos0.w;
    #else
        #ifndef USING_DIRECTIONAL_LIGHT
        return objSpaceLightPos.xyz - v.xyz;
        #else
        return objSpaceLightPos.xyz;
        #endif
    #endif
}

// Computes world space view direction from object position in world space
inline float3 UnityWorldSpaceViewDir( in float3 worldPos )
{
    return _WorldSpaceCameraPos.xyz - worldPos;
}

// Computes world space view direction, from object space position
// *Legacy* Please use UnityWorldSpaceViewDir instead
inline float3 WorldSpaceViewDir( in float4 localPos )
{
    float3 worldPos = mul(unity_ObjectToWorld, localPos).xyz;
    return UnityWorldSpaceViewDir(worldPos);
}

// Computes object space view direction
inline float3 ObjSpaceViewDir( in float4 v )
{
    float3 objSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
    return objSpaceCameraPos - v.xyz;
}

// Declares 3x3 matrix 'rotation', filled with tangent space basis
#define TANGENT_SPACE_ROTATION \
    float3 binormal = cross( normalize(v.normal), normalize(v.tangent.xyz) ) * v.tangent.w; \
    float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal )



// Used in ForwardBase pass: Calculates diffuse lighting from 4 point lights, with data packed in a special way.
float3 Shade4PointLights (
    float4 lightPosX, float4 lightPosY, float4 lightPosZ,
    float3 lightColor0, float3 lightColor1, float3 lightColor2, float3 lightColor3,
    float4 lightAttenSq,
    float3 pos, float3 normal)
{
    // to light vectors
    float4 toLightX = lightPosX - pos.x;
    float4 toLightY = lightPosY - pos.y;
    float4 toLightZ = lightPosZ - pos.z;
    // squared lengths
    float4 lengthSq = 0;
    lengthSq += toLightX * toLightX;
    lengthSq += toLightY * toLightY;
    lengthSq += toLightZ * toLightZ;
    // don't produce NaNs if some vertex position overlaps with the light
    lengthSq = max(lengthSq, 0.000001);

    // NdotL
    float4 ndotl = 0;
    ndotl += toLightX * normal.x;
    ndotl += toLightY * normal.y;
    ndotl += toLightZ * normal.z;
    // correct NdotL
    float4 corr = rsqrt(lengthSq);
    ndotl = max (float4(0,0,0,0), ndotl * corr);
    // attenuation
    float4 atten = 1.0 / (1.0 + lengthSq * lightAttenSq);
    float4 diff = ndotl * atten;
    // final color
    float3 col = 0;
    col += lightColor0 * diff.x;
    col += lightColor1 * diff.y;
    col += lightColor2 * diff.z;
    col += lightColor3 * diff.w;
    return col;
}

// Used in Vertex pass: Calculates diffuse lighting from lightCount lights. Specifying true to spotLight is more expensive
// to calculate but lights are treated as spot lights otherwise they are treated as point lights.
float3 ShadeVertexLightsFull (float4 vertex, float3 normal, int lightCount, bool spotLight)
{
    float3 viewpos = UnityObjectToViewPos (vertex.xyz);
    float3 viewN = normalize (mul ((float3x3)UNITY_MATRIX_IT_MV, normal));

    float3 lightColor = UNITY_LIGHTMODEL_AMBIENT.xyz;
    for (int i = 0; i < lightCount; i++) {
        float3 toLight = unity_LightPosition[i].xyz - viewpos.xyz * unity_LightPosition[i].w;
        float lengthSq = dot(toLight, toLight);

        // don't produce NaNs if some vertex position overlaps with the light
        lengthSq = max(lengthSq, 0.000001);

        toLight *= rsqrt(lengthSq);

        float atten = 1.0 / (1.0 + lengthSq * unity_LightAtten[i].z);
        if (spotLight)
        {
            float rho = max (0, dot(toLight, unity_SpotDirection[i].xyz));
            float spotAtt = (rho - unity_LightAtten[i].x) * unity_LightAtten[i].y;
            atten *= saturate(spotAtt);
        }

        float diff = max (0, dot (viewN, toLight));
        lightColor += unity_LightColor[i].rgb * (diff * atten);
    }
    return lightColor;
}

float3 ShadeVertexLights (float4 vertex, float3 normal)
{
    return ShadeVertexLightsFull (vertex, normal, 4, false);
}

// normal should be normalized, w=1.0
half3 SHEvalLinearL0L1 (half4 normal)
{
    half3 x;

    // Linear (L1) + constant (L0) polynomial terms
    x.r = dot(unity_SHAr,normal);
    x.g = dot(unity_SHAg,normal);
    x.b = dot(unity_SHAb,normal);

    return x;
}

// normal should be normalized, w=1.0
half3 SHEvalLinearL2 (half4 normal)
{
    half3 x1, x2;
    // 4 of the quadratic (L2) polynomials
    half4 vB = normal.xyzz * normal.yzzx;
    x1.r = dot(unity_SHBr,vB);
    x1.g = dot(unity_SHBg,vB);
    x1.b = dot(unity_SHBb,vB);

    // Final (5th) quadratic (L2) polynomial
    half vC = normal.x*normal.x - normal.y*normal.y;
    x2 = unity_SHC.rgb * vC;

    return x1 + x2;
}

// normal should be normalized, w=1.0
// output in active color space
half3 ShadeSH9 (half4 normal)
{
    // Linear + constant polynomial terms
    half3 res = SHEvalLinearL0L1 (normal);

    // Quadratic polynomials
    res += SHEvalLinearL2 (normal);

#   ifdef UNITY_COLORSPACE_GAMMA
        res = LinearToGammaSpace (res);
#   endif

    return res;
}

// OBSOLETE: for backwards compatibility with 5.0
half3 ShadeSH3Order(half4 normal)
{
    // Quadratic polynomials
    half3 res = SHEvalLinearL2 (normal);

#   ifdef UNITY_COLORSPACE_GAMMA
        res = LinearToGammaSpace (res);
#   endif

    return res;
}

#if UNITY_LIGHT_PROBE_PROXY_VOLUME

// normal should be normalized, w=1.0
half3 SHEvalLinearL0L1_SampleProbeVolume (half4 normal, float3 worldPos)
{
    const float transformToLocal = unity_ProbeVolumeParams.y;
    const float texelSizeX = unity_ProbeVolumeParams.z;

    //The SH coefficients textures and probe occlusion are packed into 1 atlas.
    //-------------------------
    //| ShR | ShG | ShB | Occ |
    //-------------------------

    float3 position = (transformToLocal == 1.0f) ? mul(unity_ProbeVolumeWorldToObject, float4(worldPos, 1.0)).xyz : worldPos;
    float3 texCoord = (position - unity_ProbeVolumeMin.xyz) * unity_ProbeVolumeSizeInv.xyz;
    texCoord.x = texCoord.x * 0.25f;

    // We need to compute proper X coordinate to sample.
    // Clamp the coordinate otherwize we'll have leaking between RGB coefficients
    float texCoordX = clamp(texCoord.x, 0.5f * texelSizeX, 0.25f - 0.5f * texelSizeX);

    // sampler state comes from SHr (all SH textures share the same sampler)
    texCoord.x = texCoordX;
    half4 SHAr = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

    texCoord.x = texCoordX + 0.25f;
    half4 SHAg = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

    texCoord.x = texCoordX + 0.5f;
    half4 SHAb = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

    // Linear + constant polynomial terms
    half3 x1;
    x1.r = dot(SHAr, normal);
    x1.g = dot(SHAg, normal);
    x1.b = dot(SHAb, normal);

    return x1;
}
#endif

// normal should be normalized, w=1.0
half3 ShadeSH12Order (half4 normal)
{
    // Linear + constant polynomial terms
    half3 res = SHEvalLinearL0L1 (normal);

#   ifdef UNITY_COLORSPACE_GAMMA
        res = LinearToGammaSpace (res);
#   endif

    return res;
}

// Transforms 2D UV by scale/bias property
#define TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)

// Deprecated. Used to transform 4D UV by a fixed function texture matrix. Now just returns the passed UV.
#define TRANSFORM_UV(idx) v.texcoord.xy



struct v2f_vertex_lit {
    float2 uv   : TEXCOORD0;
    fixed4 diff : COLOR0;
    fixed4 spec : COLOR1;
};

inline fixed4 VertexLight(v2f_vertex_lit i, sampler2D mainTex)
{
    fixed4 texcol = tex2D(mainTex, i.uv);
    fixed4 c;
    c.xyz = ( texcol.xyz * i.diff.xyz + i.spec.xyz * texcol.a );
    c.w = texcol.w * i.diff.w;
    return c;
}


// Calculates UV offset for parallax bump mapping
inline float2 ParallaxOffset( half h, half height, half3 viewDir )
{
    h = h * height - height/2.0;
    float3 v = normalize(viewDir);
    v.z += 0.42;
    return h * (v.xy / v.z);
}

// Converts color to luminance (grayscale)
inline half Luminance(half3 rgb)
{
    return dot(rgb, unity_ColorSpaceLuminance.rgb);
}

// Convert rgb to luminance
// with rgb in linear space with sRGB primaries and D65 white point
half LinearRgbToLuminance(half3 linearRgb)
{
    return dot(linearRgb, half3(0.2126729f,  0.7151522f, 0.0721750f));
}

half4 UnityEncodeRGBM (half3 color, float maxRGBM)
{
    float kOneOverRGBMMaxRange = 1.0 / maxRGBM;
    const float kMinMultiplier = 2.0 * 1e-2;

    float3 rgb = color * kOneOverRGBMMaxRange;
    float alpha = max(max(rgb.r, rgb.g), max(rgb.b, kMinMultiplier));
    alpha = ceil(alpha * 255.0) / 255.0;

    // Division-by-zero warning from d3d9, so make compiler happy.
    alpha = max(alpha, kMinMultiplier);

    return half4(rgb / alpha, alpha);
}

// Decodes HDR textures
// handles dLDR, RGBM formats
inline half3 DecodeHDR(half4 data, half4 decodeInstructions, int colorspaceIsGamma)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    half alpha = decodeInstructions.w * (data.a - 1.0) + 1.0;

    // If Linear mode is not supported we can skip exponent part
    if(colorspaceIsGamma)
        return (decodeInstructions.x * alpha) * data.rgb;

    return (decodeInstructions.x * pow(alpha, decodeInstructions.y)) * data.rgb;
}

// Decodes HDR textures
// handles dLDR, RGBM formats
inline half3 DecodeHDR (half4 data, half4 decodeInstructions)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return DecodeHDR(data, decodeInstructions, 1);
    #else
    return DecodeHDR(data, decodeInstructions, 0);
    #endif
}


// Decodes HDR textures
// handles dLDR, RGBM formats
inline half3 DecodeLightmapRGBM (half4 data, half4 decodeInstructions)
{
    // If Linear mode is not supported we can skip exponent part
    #if defined(UNITY_COLORSPACE_GAMMA)
    # if defined(UNITY_FORCE_LINEAR_READ_FOR_RGBM)
        return (decodeInstructions.x * data.a) * sqrt(data.rgb);
    # else
        return (decodeInstructions.x * data.a) * data.rgb;
    # endif
    #else
        return (decodeInstructions.x * pow(data.a, decodeInstructions.y)) * data.rgb;
    #endif
}

// Decodes doubleLDR encoded lightmaps.
inline half3 DecodeLightmapDoubleLDR( fixed4 color, half4 decodeInstructions)
{
    // decodeInstructions.x contains 2.0 when gamma color space is used or pow(2.0, 2.2) = 4.59 when linear color space is used on mobile platforms
    return decodeInstructions.x * color.rgb;
}

inline half3 DecodeLightmap( fixed4 color, half4 decodeInstructions)
{
#if defined(UNITY_LIGHTMAP_DLDR_ENCODING)
    return DecodeLightmapDoubleLDR(color, decodeInstructions);
#elif defined(UNITY_LIGHTMAP_RGBM_ENCODING)
    return DecodeLightmapRGBM(color, decodeInstructions);
#else //defined(UNITY_LIGHTMAP_FULL_HDR)
    return color.rgb;
#endif
}

half4 unity_Lightmap_HDR;

inline half3 DecodeLightmap( fixed4 color )
{
    return DecodeLightmap( color, unity_Lightmap_HDR );
}

half4 unity_DynamicLightmap_HDR;

// Decodes Enlighten RGBM encoded lightmaps
// NOTE: Enlighten dynamic texture RGBM format is _different_ from standard Unity HDR textures
// (such as Baked Lightmaps, Reflection Probes and IBL images)
// Instead Enlighten provides RGBM texture in _Linear_ color space with _different_ exponent.
// WARNING: 3 pow operations, might be very expensive for mobiles!
inline half3 DecodeRealtimeLightmap( fixed4 color )
{
    //@TODO: Temporary until Geomerics gives us an API to convert lightmaps to RGBM in gamma space on the enlighten thread before we upload the textures.
#if defined(UNITY_FORCE_LINEAR_READ_FOR_RGBM)
    return pow ((unity_DynamicLightmap_HDR.x * color.a) * sqrt(color.rgb), unity_DynamicLightmap_HDR.y);
#else
    return pow ((unity_DynamicLightmap_HDR.x * color.a) * color.rgb, unity_DynamicLightmap_HDR.y);
#endif
}

inline half3 DecodeDirectionalLightmap(half3 color, fixed4 dirTex, half3 normalWorld)
{
    // In directional (non-specular) mode Enlighten bakes dominant light direction
    // in a way, that using it for half Lambert and then dividing by a "rebalancing coefficient"
    // gives a result close to plain diffuse response lightmaps, but normalmapped.

    // Note that dir is not unit length on purpose. Its length is "directionality", like
    // for the directional specular lightmaps.

    half halfLambert = dot(normalWorld, dirTex.xyz - 0.5) + 0.5;

    return color * halfLambert / max(1e-4h, dirTex.w);
}

// Encoding/decoding [0..1) floats into 8 bit/channel RGBA. Note that 1.0 will not be encoded properly.
inline float4 EncodeFloatRGBA( float v )
{
    float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
    float kEncodeBit = 1.0/255.0;
    float4 enc = kEncodeMul * v;
    enc = frac (enc);
    enc -= enc.yzww * kEncodeBit;
    return enc;
}
inline float DecodeFloatRGBA( float4 enc )
{
    float4 kDecodeDot = float4(1.0, 1/255.0, 1/65025.0, 1/16581375.0);
    return dot( enc, kDecodeDot );
}

// Encoding/decoding [0..1) floats into 8 bit/channel RG. Note that 1.0 will not be encoded properly.
inline float2 EncodeFloatRG( float v )
{
    float2 kEncodeMul = float2(1.0, 255.0);
    float kEncodeBit = 1.0/255.0;
    float2 enc = kEncodeMul * v;
    enc = frac (enc);
    enc.x -= enc.y * kEncodeBit;
    return enc;
}
inline float DecodeFloatRG( float2 enc )
{
    float2 kDecodeDot = float2(1.0, 1/255.0);
    return dot( enc, kDecodeDot );
}


// Encoding/decoding view space normals into 2D 0..1 vector
inline float2 EncodeViewNormalStereo( float3 n )
{
    float kScale = 1.7777;
    float2 enc;
    enc = n.xy / (n.z+1);
    enc /= kScale;
    enc = enc*0.5+0.5;
    return enc;
}
inline float3 DecodeViewNormalStereo( float4 enc4 )
{
    float kScale = 1.7777;
    float3 nn = enc4.xyz*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
    float g = 2.0 / dot(nn.xyz,nn.xyz);
    float3 n;
    n.xy = g*nn.xy;
    n.z = g-1;
    return n;
}

inline float4 EncodeDepthNormal( float depth, float3 normal )
{
    float4 enc;
    enc.xy = EncodeViewNormalStereo (normal);
    enc.zw = EncodeFloatRG (depth);
    return enc;
}

inline void DecodeDepthNormal( float4 enc, out float depth, out float3 normal )
{
    depth = DecodeFloatRG (enc.zw);
    normal = DecodeViewNormalStereo (enc);
}

inline fixed3 UnpackNormalDXT5nm (fixed4 packednormal)
{
    fixed3 normal;
    normal.xy = packednormal.wy * 2 - 1;
    normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

// Unpack normal as DXT5nm (1, y, 1, x) or BC5 (x, y, 0, 1)
// Note neutral texture like "bump" is (0, 0, 1, 1) to work with both plain RGB normal and DXT5nm/BC5
fixed3 UnpackNormalmapRGorAG(fixed4 packednormal)
{
    // This do the trick
   packednormal.x *= packednormal.w;

    fixed3 normal;
    normal.xy = packednormal.xy * 2 - 1;
    normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}
inline fixed3 UnpackNormal(fixed4 packednormal)
{
#if defined(UNITY_NO_DXT5nm)
    return packednormal.xyz * 2 - 1;
#elif defined(UNITY_ASTC_NORMALMAP_ENCODING)
    return UnpackNormalDXT5nm(packednormal);
#else
    return UnpackNormalmapRGorAG(packednormal);
#endif
}

fixed3 UnpackNormalWithScale(fixed4 packednormal, float scale)
{
#if defined(UNITY_ASTC_NORMALMAP_ENCODING)
    // (y, y, y, x), preferred for ASTC
    packednormal.x = packednormal.w;
#elif !defined(UNITY_NO_DXT5nm)
    // Unpack normal as DXT5nm (1, y, 1, x) or BC5 (x, y, 0, 1)
    // Note neutral texture like "bump" is (0, 0, 1, 1) to work with both plain RGB normal and DXT5nm/BC5
    packednormal.x *= packednormal.w;
#endif // UNITY_NO_DXT5nm
    fixed3 normal;
    normal.xy = (packednormal.xy * 2 - 1) * scale;
    normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

// Z buffer to linear 0..1 depth
inline float Linear01Depth( float z )
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}
// Z buffer to linear depth
inline float LinearEyeDepth( float z )
{
    return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
}


inline float2 UnityStereoScreenSpaceUVAdjustInternal(float2 uv, float4 scaleAndOffset)
{
    return uv.xy * scaleAndOffset.xy + scaleAndOffset.zw;
}

inline float4 UnityStereoScreenSpaceUVAdjustInternal(float4 uv, float4 scaleAndOffset)
{
    return float4(UnityStereoScreenSpaceUVAdjustInternal(uv.xy, scaleAndOffset), UnityStereoScreenSpaceUVAdjustInternal(uv.zw, scaleAndOffset));
}

#define UnityStereoScreenSpaceUVAdjust(x, y) UnityStereoScreenSpaceUVAdjustInternal(x, y)

#if defined(UNITY_SINGLE_PASS_STEREO)
float2 TransformStereoScreenSpaceTex(float2 uv, float w)
{
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
}

inline float2 UnityStereoTransformScreenSpaceTex(float2 uv)
{
    return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
}

inline float4 UnityStereoTransformScreenSpaceTex(float4 uv)
{
    return float4(UnityStereoTransformScreenSpaceTex(uv.xy), UnityStereoTransformScreenSpaceTex(uv.zw));
}
inline float2 UnityStereoClamp(float2 uv, float4 scaleAndOffset)
{
    return float2(clamp(uv.x, scaleAndOffset.z, scaleAndOffset.z + scaleAndOffset.x), uv.y);
}
#else
#define TransformStereoScreenSpaceTex(uv, w) uv
#define UnityStereoTransformScreenSpaceTex(uv) uv
#define UnityStereoClamp(uv, scaleAndOffset) uv
#endif

// Depth render texture helpers
#define DECODE_EYEDEPTH(i) LinearEyeDepth(i)
#define COMPUTE_EYEDEPTH(o) o = -UnityObjectToViewPos( v.vertex ).z
#define COMPUTE_DEPTH_01 -(UnityObjectToViewPos( v.vertex ).z * _ProjectionParams.w)
#define COMPUTE_VIEW_NORMAL normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal))

// Helpers used in image effects. Most image effects use the same
// minimal vertex shader (vert_img).

struct appdata_img
{
    float4 vertex : POSITION;
    half2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_img
{
    float4 pos : SV_POSITION;
    half2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float2 MultiplyUV (float4x4 mat, float2 inUV) {
    float4 temp = float4 (inUV.x, inUV.y, 0, 0);
    temp = mul (mat, temp);
    return temp.xy;
}

v2f_img vert_img( appdata_img v )
{
    v2f_img o;
    UNITY_INITIALIZE_OUTPUT(v2f_img, o);
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = v.texcoord;
    return o;
}

// Projected screen position helpers
#define V2F_SCREEN_TYPE float4

inline float4 ComputeNonStereoScreenPos(float4 pos) {
    float4 o = pos * 0.5f;
#ifdef UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION
    switch (UNITY_DISPLAY_ORIENTATION_PRETRANSFORM)
    {
    default: break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_90: o.xy = float2(-o.y, o.x); break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_180: o.xy = -o.xy; break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_270: o.xy = float2(o.y, -o.x); break;
    }
#endif
    o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
    o.zw = pos.zw;
    return o;
}

inline float4 ComputeScreenPos(float4 pos) {
    float4 o = ComputeNonStereoScreenPos(pos);
#if defined(UNITY_SINGLE_PASS_STEREO)
    o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
#endif
    return o;
}

inline float4 ComputeGrabScreenPos (float4 pos) {
    #if UNITY_UV_STARTS_AT_TOP
    float scale = -1.0;
    #else
    float scale = 1.0;
    #endif
    float4 o = pos * 0.5f;
#ifdef UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION
    switch (UNITY_DISPLAY_ORIENTATION_PRETRANSFORM)
    {
    default: break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_90: o.xy = float2(-o.y, o.x); break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_180: o.xy = -o.xy; break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_270: o.xy = float2(o.y, -o.x); break;
    }
#endif
    o.xy = float2(o.x, o.y*scale) + o.w;
#ifdef UNITY_SINGLE_PASS_STEREO
    o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
#endif
    o.zw = pos.zw;
    return o;
}

// snaps post-transformed position to screen pixels
inline float4 UnityPixelSnap (float4 pos)
{
    float2 hpc = _ScreenParams.xy * 0.5f;
#if  SHADER_API_PSSL
// An old sdk used to implement round() as floor(x+0.5) current sdks use the round to even method so we manually use the old method here for compatabilty.
    float2 temp = ((pos.xy / pos.w) * hpc) + float2(0.5f,0.5f);
    float2 pixelPos = float2(floor(temp.x), floor(temp.y));
#else
    float2 pixelPos = round ((pos.xy / pos.w) * hpc);
#endif
    pos.xy = pixelPos / hpc * pos.w;
    return pos;
}

inline float2 TransformViewToProjection (float2 v) {
    return mul((float2x2)UNITY_MATRIX_P, v);
}

inline float3 TransformViewToProjection (float3 v) {
    return mul((float3x3)UNITY_MATRIX_P, v);
}

// Shadow caster pass helpers

float4 UnityEncodeCubeShadowDepth (float z)
{
    #ifdef UNITY_USE_RGBA_FOR_POINT_SHADOWS
    return EncodeFloatRGBA (min(z, 0.999));
    #else
    return z;
    #endif
}

float UnityDecodeCubeShadowDepth (float4 vals)
{
    #ifdef UNITY_USE_RGBA_FOR_POINT_SHADOWS
    return DecodeFloatRGBA (vals);
    #else
    return vals.r;
    #endif
}


float4 UnityClipSpaceShadowCasterPos(float4 vertex, float3 normal)
{
    float4 wPos = mul(unity_ObjectToWorld, vertex);

    if (unity_LightShadowBias.z != 0.0)
    {
        float3 wNormal = UnityObjectToWorldNormal(normal);
        float3 wLight = normalize(UnityWorldSpaceLightDir(wPos.xyz));

        // apply normal offset bias (inset position along the normal)
        // bias needs to be scaled by sine between normal and light direction
        // (http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/)
        //
        // unity_LightShadowBias.z contains user-specified normal offset amount
        // scaled by world space texel size.

        float shadowCos = dot(wNormal, wLight);
        float shadowSine = sqrt(1-shadowCos*shadowCos);
        float normalBias = unity_LightShadowBias.z * shadowSine;

        wPos.xyz -= wNormal * normalBias;
    }

    return mul(UNITY_MATRIX_VP, wPos);
}
// Legacy, not used anymore; kept around to not break existing user shaders
float4 UnityClipSpaceShadowCasterPos(float3 vertex, float3 normal)
{
    return UnityClipSpaceShadowCasterPos(float4(vertex, 1), normal);
}


float4 UnityApplyLinearShadowBias(float4 clipPos)

{
    // For point lights that support depth cube map, the bias is applied in the fragment shader sampling the shadow map.
    // This is because the legacy behaviour for point light shadow map cannot be implemented by offseting the vertex position
    // in the vertex shader generating the shadow map.
#if !(defined(SHADOWS_CUBE) && defined(SHADOWS_CUBE_IN_DEPTH_TEX))
    #if defined(UNITY_REVERSED_Z)
        // We use max/min instead of clamp to ensure proper handling of the rare case
        // where both numerator and denominator are zero and the fraction becomes NaN.
        clipPos.z += max(-1, min(unity_LightShadowBias.x / clipPos.w, 0));
    #else
        clipPos.z += saturate(unity_LightShadowBias.x/clipPos.w);
    #endif
#endif

#if defined(UNITY_REVERSED_Z)
    float clamped = min(clipPos.z, clipPos.w*UNITY_NEAR_CLIP_VALUE);
#else
    float clamped = max(clipPos.z, clipPos.w*UNITY_NEAR_CLIP_VALUE);
#endif
    clipPos.z = lerp(clipPos.z, clamped, unity_LightShadowBias.y);
    return clipPos;
}


#if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
    // Rendering into point light (cubemap) shadows
    #define V2F_SHADOW_CASTER_NOPOS float3 vec : TEXCOORD0;
    #define TRANSFER_SHADOW_CASTER_NOPOS_LEGACY(o,opos) o.vec = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz; opos = UnityObjectToClipPos(v.vertex);
    #define TRANSFER_SHADOW_CASTER_NOPOS(o,opos) o.vec = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz; opos = UnityObjectToClipPos(v.vertex);
    #define SHADOW_CASTER_FRAGMENT(i) return UnityEncodeCubeShadowDepth ((length(i.vec) + unity_LightShadowBias.x) * _LightPositionRange.w);

#else
    // Rendering into directional or spot light shadows
    #define V2F_SHADOW_CASTER_NOPOS
    // Let embedding code know that V2F_SHADOW_CASTER_NOPOS is empty; so that it can workaround
    // empty structs that could possibly be produced.
    #define V2F_SHADOW_CASTER_NOPOS_IS_EMPTY
    #define TRANSFER_SHADOW_CASTER_NOPOS_LEGACY(o,opos) \
        opos = UnityObjectToClipPos(v.vertex.xyz); \
        opos = UnityApplyLinearShadowBias(opos);
    #define TRANSFER_SHADOW_CASTER_NOPOS(o,opos) \
        opos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal); \
        opos = UnityApplyLinearShadowBias(opos);
    #define SHADOW_CASTER_FRAGMENT(i) return 0;
#endif

// Declare all data needed for shadow caster pass output (any shadow directions/depths/distances as needed),
// plus clip space position.
#define V2F_SHADOW_CASTER V2F_SHADOW_CASTER_NOPOS UNITY_POSITION(pos)

// Vertex shader part, with support for normal offset shadows. Requires
// position and normal to be present in the vertex input.
#define TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) TRANSFER_SHADOW_CASTER_NOPOS(o,o.pos)

// Vertex shader part, legacy. No support for normal offset shadows - because
// that would require vertex normals, which might not be present in user-written shaders.
#define TRANSFER_SHADOW_CASTER(o) TRANSFER_SHADOW_CASTER_NOPOS_LEGACY(o,o.pos)


// ------------------------------------------------------------------
//  Alpha helper

#define UNITY_OPAQUE_ALPHA(outputAlpha) outputAlpha = 1.0


// ------------------------------------------------------------------
//  Fog helpers
//
//  multi_compile_fog Will compile fog variants.
//  UNITY_FOG_COORDS(texcoordindex) Declares the fog data interpolator.
//  UNITY_TRANSFER_FOG(outputStruct,clipspacePos) Outputs fog data from the vertex shader.
//  UNITY_APPLY_FOG(fogData,col) Applies fog to color "col". Automatically applies black fog when in forward-additive pass.
//  Can also use UNITY_APPLY_FOG_COLOR to supply your own fog color.

// In case someone by accident tries to compile fog code in one of the g-buffer or shadow passes:
// treat it as fog is off.
#if defined(UNITY_PASS_DEFERRED) || defined(UNITY_PASS_SHADOWCASTER)
#undef FOG_LINEAR
#undef FOG_EXP
#undef FOG_EXP2
#endif

#if defined(UNITY_REVERSED_Z)
    #if UNITY_REVERSED_Z == 1
        //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
        //max is required to protect ourselves from near plane not being correct/meaningfull in case of oblique matrices.
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
    #else
        //GL with reversed z => z clip range is [near, -far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(-(coord), 0)
    #endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#endif

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = (coord) * unity_FogParams.z + unity_FogParams.w
#elif defined(FOG_EXP)
    // factor = exp(-density*z)
    #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = unity_FogParams.y * (coord); unityFogFactor = exp2(-unityFogFactor)
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = unity_FogParams.x * (coord); unityFogFactor = exp2(-unityFogFactor*unityFogFactor)
#else
    #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = 0.0
#endif

#define UNITY_CALC_FOG_FACTOR(coord) UNITY_CALC_FOG_FACTOR_RAW(UNITY_Z_0_FAR_FROM_CLIPSPACE(coord))

#define UNITY_FOG_COORDS_PACKED(idx, vectype) vectype fogCoord : TEXCOORD##idx;

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    #define UNITY_FOG_COORDS(idx) UNITY_FOG_COORDS_PACKED(idx, float1)

    #if (SHADER_TARGET < 30) || defined(SHADER_API_MOBILE)
        // mobile or SM2.0: calculate fog factor per-vertex
        #define UNITY_TRANSFER_FOG(o,outpos) UNITY_CALC_FOG_FACTOR((outpos).z); o.fogCoord.x = unityFogFactor
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,outpos) UNITY_CALC_FOG_FACTOR((outpos).z); o.tSpace1.y = tangentSign; o.tSpace2.y = unityFogFactor
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,outpos) UNITY_CALC_FOG_FACTOR((outpos).z); o.worldPos.w = unityFogFactor
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o,outpos) UNITY_CALC_FOG_FACTOR((outpos).z); o.eyeVec.w = unityFogFactor
    #else
        // SM3.0 and PC/console: calculate fog distance per-vertex, and fog factor per-pixel
        #define UNITY_TRANSFER_FOG(o,outpos) o.fogCoord.x = (outpos).z
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,outpos) o.tSpace2.y = (outpos).z
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,outpos) o.worldPos.w = (outpos).z
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o,outpos) o.eyeVec.w = (outpos).z
    #endif
#else
    #define UNITY_FOG_COORDS(idx)
    #define UNITY_TRANSFER_FOG(o,outpos)
    #define UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,outpos)
    #define UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,outpos)
    #define UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o,outpos)
#endif

#define UNITY_FOG_LERP_COLOR(col,fogCol,fogFac) col.rgb = lerp((fogCol).rgb, (col).rgb, saturate(fogFac))


#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    #if (SHADER_TARGET < 30) || defined(SHADER_API_MOBILE)
        // mobile or SM2.0: fog factor was already calculated per-vertex, so just lerp the color
        #define UNITY_APPLY_FOG_COLOR(coord,col,fogCol) UNITY_FOG_LERP_COLOR(col,fogCol,(coord).x)
    #else
        // SM3.0 and PC/console: calculate fog factor and lerp fog color
        #define UNITY_APPLY_FOG_COLOR(coord,col,fogCol) UNITY_CALC_FOG_FACTOR((coord).x); UNITY_FOG_LERP_COLOR(col,fogCol,unityFogFactor)
    #endif
    #define UNITY_EXTRACT_FOG(name) float _unity_fogCoord = name.fogCoord
    #define UNITY_EXTRACT_FOG_FROM_TSPACE(name) float _unity_fogCoord = name.tSpace2.y
    #define UNITY_EXTRACT_FOG_FROM_WORLD_POS(name) float _unity_fogCoord = name.worldPos.w
    #define UNITY_EXTRACT_FOG_FROM_EYE_VEC(name) float _unity_fogCoord = name.eyeVec.w
#else
    #define UNITY_APPLY_FOG_COLOR(coord,col,fogCol)
    #define UNITY_EXTRACT_FOG(name)
    #define UNITY_EXTRACT_FOG_FROM_TSPACE(name)
    #define UNITY_EXTRACT_FOG_FROM_WORLD_POS(name)
    #define UNITY_EXTRACT_FOG_FROM_EYE_VEC(name)
#endif

#ifdef UNITY_PASS_FORWARDADD
    #define UNITY_APPLY_FOG(coord,col) UNITY_APPLY_FOG_COLOR(coord,col,fixed4(0,0,0,0))
#else
    #define UNITY_APPLY_FOG(coord,col) UNITY_APPLY_FOG_COLOR(coord,col,unity_FogColor)
#endif

// ------------------------------------------------------------------
//  TBN helpers
#define UNITY_EXTRACT_TBN_0(name) fixed3 _unity_tbn_0 = name.tSpace0.xyz
#define UNITY_EXTRACT_TBN_1(name) fixed3 _unity_tbn_1 = name.tSpace1.xyz
#define UNITY_EXTRACT_TBN_2(name) fixed3 _unity_tbn_2 = name.tSpace2.xyz

#define UNITY_EXTRACT_TBN(name) UNITY_EXTRACT_TBN_0(name); UNITY_EXTRACT_TBN_1(name); UNITY_EXTRACT_TBN_2(name)

#define UNITY_EXTRACT_TBN_T(name) fixed3 _unity_tangent = fixed3(name.tSpace0.x, name.tSpace1.x, name.tSpace2.x)
#define UNITY_EXTRACT_TBN_N(name) fixed3 _unity_normal = fixed3(name.tSpace0.z, name.tSpace1.z, name.tSpace2.z)
#define UNITY_EXTRACT_TBN_B(name) fixed3 _unity_binormal = cross(_unity_normal, _unity_tangent)
#define UNITY_CORRECT_TBN_B_SIGN(name) _unity_binormal *= name.tSpace1.y;
#define UNITY_RECONSTRUCT_TBN_0 fixed3 _unity_tbn_0 = fixed3(_unity_tangent.x, _unity_binormal.x, _unity_normal.x)
#define UNITY_RECONSTRUCT_TBN_1 fixed3 _unity_tbn_1 = fixed3(_unity_tangent.y, _unity_binormal.y, _unity_normal.y)
#define UNITY_RECONSTRUCT_TBN_2 fixed3 _unity_tbn_2 = fixed3(_unity_tangent.z, _unity_binormal.z, _unity_normal.z)

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    #define UNITY_RECONSTRUCT_TBN(name) UNITY_EXTRACT_TBN_T(name); UNITY_EXTRACT_TBN_N(name); UNITY_EXTRACT_TBN_B(name); UNITY_CORRECT_TBN_B_SIGN(name); UNITY_RECONSTRUCT_TBN_0; UNITY_RECONSTRUCT_TBN_1; UNITY_RECONSTRUCT_TBN_2
#else
    #define UNITY_RECONSTRUCT_TBN(name) UNITY_EXTRACT_TBN(name)
#endif

//  LOD cross fade helpers
// keep all the old macros
#define UNITY_DITHER_CROSSFADE_COORDS
#define UNITY_DITHER_CROSSFADE_COORDS_IDX(idx)
#define UNITY_TRANSFER_DITHER_CROSSFADE(o,v)
#define UNITY_TRANSFER_DITHER_CROSSFADE_HPOS(o,hpos)

#ifdef LOD_FADE_CROSSFADE
    #define UNITY_APPLY_DITHER_CROSSFADE(vpos)  UnityApplyDitherCrossFade(vpos)
    sampler2D unity_DitherMask;
    void UnityApplyDitherCrossFade(float2 vpos)
    {
        vpos /= 4; // the dither mask texture is 4x4
        float mask = tex2D(unity_DitherMask, vpos).a;
        float sgn = unity_LODFade.x > 0 ? 1.0f : -1.0f;
        clip(unity_LODFade.x - mask * sgn);
    }
#else
    #define UNITY_APPLY_DITHER_CROSSFADE(vpos)
#endif


// ------------------------------------------------------------------
//  Deprecated things: these aren't used; kept here
//  just so that various existing shaders still compile, more or less.


// Note: deprecated shadow collector pass helpers
#ifdef SHADOW_COLLECTOR_PASS

#if !defined(SHADOWMAPSAMPLER_DEFINED)
UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
#endif

// Note: V2F_SHADOW_COLLECTOR and TRANSFER_SHADOW_COLLECTOR are deprecated
#define V2F_SHADOW_COLLECTOR float4 pos : SV_POSITION; float3 _ShadowCoord0 : TEXCOORD0; float3 _ShadowCoord1 : TEXCOORD1; float3 _ShadowCoord2 : TEXCOORD2; float3 _ShadowCoord3 : TEXCOORD3; float4 _WorldPosViewZ : TEXCOORD4
#define TRANSFER_SHADOW_COLLECTOR(o)    \
    o.pos = UnityObjectToClipPos(v.vertex); \
    float4 wpos = mul(unity_ObjectToWorld, v.vertex); \
    o._WorldPosViewZ.xyz = wpos; \
    o._WorldPosViewZ.w = -UnityObjectToViewPos(v.vertex).z; \
    o._ShadowCoord0 = mul(unity_WorldToShadow[0], wpos).xyz; \
    o._ShadowCoord1 = mul(unity_WorldToShadow[1], wpos).xyz; \
    o._ShadowCoord2 = mul(unity_WorldToShadow[2], wpos).xyz; \
    o._ShadowCoord3 = mul(unity_WorldToShadow[3], wpos).xyz;

// Note: SAMPLE_SHADOW_COLLECTOR_SHADOW is deprecated
#define SAMPLE_SHADOW_COLLECTOR_SHADOW(coord) \
    half shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture,coord); \
    shadow = _LightShadowData.r + shadow * (1-_LightShadowData.r);

// Note: COMPUTE_SHADOW_COLLECTOR_SHADOW is deprecated
#define COMPUTE_SHADOW_COLLECTOR_SHADOW(i, weights, shadowFade) \
    float4 coord = float4(i._ShadowCoord0 * weights[0] + i._ShadowCoord1 * weights[1] + i._ShadowCoord2 * weights[2] + i._ShadowCoord3 * weights[3], 1); \
    SAMPLE_SHADOW_COLLECTOR_SHADOW(coord) \
    float4 res; \
    res.x = saturate(shadow + shadowFade); \
    res.y = 1.0; \
    res.zw = EncodeFloatRG (1 - i._WorldPosViewZ.w * _ProjectionParams.w); \
    return res;

// Note: deprecated
#if defined (SHADOWS_SPLIT_SPHERES)
#define SHADOW_COLLECTOR_FRAGMENT(i) \
    float3 fromCenter0 = i._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[0].xyz; \
    float3 fromCenter1 = i._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[1].xyz; \
    float3 fromCenter2 = i._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[2].xyz; \
    float3 fromCenter3 = i._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[3].xyz; \
    float4 distances2 = float4(dot(fromCenter0,fromCenter0), dot(fromCenter1,fromCenter1), dot(fromCenter2,fromCenter2), dot(fromCenter3,fromCenter3)); \
    float4 cascadeWeights = float4(distances2 < unity_ShadowSplitSqRadii); \
    cascadeWeights.yzw = saturate(cascadeWeights.yzw - cascadeWeights.xyz); \
    float sphereDist = distance(i._WorldPosViewZ.xyz, unity_ShadowFadeCenterAndType.xyz); \
    float shadowFade = saturate(sphereDist * _LightShadowData.z + _LightShadowData.w); \
    COMPUTE_SHADOW_COLLECTOR_SHADOW(i, cascadeWeights, shadowFade)
#else
#define SHADOW_COLLECTOR_FRAGMENT(i) \
    float4 viewZ = i._WorldPosViewZ.w; \
    float4 zNear = float4( viewZ >= _LightSplitsNear ); \
    float4 zFar = float4( viewZ < _LightSplitsFar ); \
    float4 cascadeWeights = zNear * zFar; \
    float shadowFade = saturate(i._WorldPosViewZ.w * _LightShadowData.z + _LightShadowData.w); \
    COMPUTE_SHADOW_COLLECTOR_SHADOW(i, cascadeWeights, shadowFade)
#endif

#endif // #ifdef SHADOW_COLLECTOR_PASS


// Legacy; used to do something on platforms that had to emulate depth textures manually. Now all platforms have native depth textures.
#define UNITY_TRANSFER_DEPTH(oo)
// Legacy; used to do something on platforms that had to emulate depth textures manually. Now all platforms have native depth textures.
#define UNITY_OUTPUT_DEPTH(i) return 0



#define API_HAS_GUARANTEED_R16_SUPPORT !(SHADER_API_VULKAN || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_WEBGPU)

float4 PackHeightmap(float height)
{
    #if (API_HAS_GUARANTEED_R16_SUPPORT)
        return height;
    #else
        uint a = (uint)(65535.0f * height);
        return float4((a >> 0) & 0xFF, (a >> 8) & 0xFF, 0, 0) / 255.0f;
    #endif
}

float UnpackHeightmap(float4 height)
{
    #if (API_HAS_GUARANTEED_R16_SUPPORT)
        return height.r;
    #else
        return (height.r + height.g * 256.0f) / 257.0f; // (255.0f * height.r + 255.0f * 256.0f * height.g) / 65535.0f
    #endif
}

#endif // UNITY_CG_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityCG.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityColorGamut.cginc---------------


#ifndef UNITY_COLOR_GAMUT_INCLUDED
#define UNITY_COLOR_GAMUT_INCLUDED

// Conversion methods for dealing with HDR encoding within the built-in render pipeline.

// These values must match the ColorGamut enum in ColorGamut.h
#define kColorGamutSRGB         0
#define kColorGamutRec709       1
#define kColorGamutRec2020      2
#define kColorGamutDisplayP3    3
#define kColorGamutHDR10        4
#define kColorGamutDolbyHDR     5
#define kColorGamutP3D65G22     6

#if SHADER_API_METAL
#define kReferenceLuminanceWhiteForRec709 100
#else
#define kReferenceLuminanceWhiteForRec709 80
#endif

float3 LinearToSRGB(float3 color)
{
    // Approximately pow(color, 1.0 / 2.2)
    return color < 0.0031308 ? 12.92 * color : 1.055 * pow(abs(color), 1.0 / 2.4) - 0.055;
}

float3 SRGBToLinear(float3 color)
{
    // Approximately pow(color, 2.2)
    return color < 0.04045 ? color / 12.92 : pow(abs(color + 0.055) / 1.055, 2.4);
}

static const float3x3 Rec709ToRec2020 =
{
    0.627402, 0.329292, 0.043306,
    0.069095, 0.919544, 0.011360,
    0.016394, 0.088028, 0.895578
};

static const float3x3 Rec2020ToRec709 =
{
    1.660496, -0.587656, -0.072840,
    -0.124547, 1.132895, -0.008348,
    -0.018154, -0.100597, 1.118751
};

#define PQ_M1 (2610.0 / 4096.0 / 4)
#define PQ_M2 (2523.0 / 4096.0 * 128)
#define PQ_C1 (3424.0 / 4096.0)
#define PQ_C2 (2413.0 / 4096.0 * 32)
#define PQ_C3 (2392.0 / 4096.0 * 32)

float3 LinearToST2084(float3 color)
{
    float3 cp = pow(abs(color), PQ_M1);
    return pow((PQ_C1 + PQ_C2 * cp) / (1 + PQ_C3 * cp), PQ_M2);
}

float3 ST2084ToLinear(float3 color)
{
    float3 x = pow(abs(color), 1.0 / PQ_M2);
    return pow(max(x - PQ_C1, 0) / (PQ_C2 - PQ_C3 * x), 1.0 / PQ_M1);
}


static const float3x3 Rec709ToP3D65Mat =
{
    0.822462, 0.177538, 0.000000,
    0.033194, 0.966806, 0.000000,
    0.017083, 0.072397, 0.910520
};

static const float3x3 P3D65MatToRec709 =
{
     1.224940, -0.224940,  0.000000,
    -0.042056,  1.042056,  0.000000,
    -0.019637, -0.078636,  1.098273
};

float3 LinearToGamma22(float3 color)
{
    return pow(abs(color.rgb), float3(0.454545454545455, 0.454545454545455, 0.454545454545455));
}

float3 Gamma22ToLinear(float3 color)
{
    return pow(abs(color.rgb), float3(2.2, 2.2, 2.2));
}


float3 SimpleHDRDisplayToneMapAndOETF(float3 scene, int colorGamut, bool forceGammaToLinear, float nitsForPaperWhite, float maxDisplayNits)
{
        float3 result = (IsGammaSpace() || forceGammaToLinear) ? float3(GammaToLinearSpaceExact(scene.r), GammaToLinearSpaceExact(scene.g), GammaToLinearSpaceExact(scene.b)) : scene.rgb;

        if (colorGamut == kColorGamutSRGB)
        {
            if (!IsGammaSpace())
                result = LinearToSRGB(result);
        }
        else if (colorGamut == kColorGamutHDR10)
        {
            const float st2084max = 10000.0;
            const float hdrScalar = nitsForPaperWhite / st2084max;
            // The HDR scene is in Rec.709, but the display is Rec.2020
            result = mul(Rec709ToRec2020, result);
            // Apply the ST.2084 curve to the scene.
            result = LinearToST2084(result * hdrScalar);
        }
        else if (colorGamut == kColorGamutP3D65G22)
        {
            const float hdrScalar = nitsForPaperWhite / maxDisplayNits;
            // The HDR scene is in Rec.709, but the display is P3
            result = mul(Rec709ToP3D65Mat, result);
            // Apply gamma 2.2
            result = LinearToGamma22(result * hdrScalar);
        }
        else // colorGamut == kColorGamutRec709
        {
            const float hdrScalar = nitsForPaperWhite / kReferenceLuminanceWhiteForRec709;
            result *= hdrScalar;
        }
    return result;
}

float3 InverseSimpleHDRDisplayToneMapAndOETF(float3 result, int colorGamut, bool forceGammaToLinear, float nitsForPaperWhite, float maxDisplayNits)
{
        if (colorGamut == kColorGamutSRGB)
        {
            if (!IsGammaSpace())
                result = SRGBToLinear(result);
        }
        else if (colorGamut == kColorGamutHDR10)
        {
            const float st2084max = 10000.0;
            const float hdrScalar = nitsForPaperWhite / st2084max;

            // Unapply the ST.2084 curve to the scene.
            result = ST2084ToLinear(result);
            result = result / hdrScalar;

            // The display is Rec.2020, but HDR scene is in Rec.709
            result = mul(Rec2020ToRec709, result);

        }
        else if (colorGamut == kColorGamutP3D65G22)
        {
            const float hdrScalar = nitsForPaperWhite / maxDisplayNits;

            // Unapply gamma 2.2
            result = Gamma22ToLinear(result);
            result = result / hdrScalar;

            // The display is P3, but he HDR scene is in Rec.709
            result = mul(P3D65MatToRec709, result);
        }
        else // colorGamut == kColorGamutRec709
        {
            const float hdrScalar = nitsForPaperWhite / kReferenceLuminanceWhiteForRec709;
            result /= hdrScalar;
        }

        result = (IsGammaSpace() || forceGammaToLinear) ? float3(LinearToGammaSpaceExact(result.r), LinearToGammaSpaceExact(result.g), LinearToGammaSpaceExact(result.b)) : result.rgb;

        return result;
}

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityColorGamut.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityCustomRenderTexture.cginc---------------


#ifndef UNITY_CUSTOM_TEXTURE_INCLUDED
#define UNITY_CUSTOM_TEXTURE_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardConfig.cginc"

// Keep in sync with CustomRenderTexture.h
#define kCustomTextureBatchSize 16

#define customTextureVertexNum 6

struct appdata_customrendertexture
{
    uint    vertexID    : SV_VertexID;
};

// User facing vertex to fragment shader structure
struct v2f_customrendertexture
{
    float4 vertex           : SV_POSITION;
    float3 localTexcoord    : TEXCOORD0;    // Texcoord local to the update zone (== globalTexcoord if no partial update zone is specified)
    float3 globalTexcoord   : TEXCOORD1;    // Texcoord relative to the complete custom texture
    uint primitiveID        : TEXCOORD2;    // Index of the update zone (correspond to the index in the updateZones of the Custom Texture)
    float3 direction        : TEXCOORD3;    // For cube textures, direction of the pixel being rendered in the cubemap
};

float2 CustomRenderTextureRotate2D(float2 pos, float angle)
{
    float sn = sin(angle);
    float cs = cos(angle);

    return float2( pos.x * cs - pos.y * sn, pos.x * sn + pos.y * cs);
}

// Internal
float4      CustomRenderTextureCenters[kCustomTextureBatchSize];
float4      CustomRenderTextureSizesAndRotations[kCustomTextureBatchSize];
float       CustomRenderTexturePrimitiveIDs[kCustomTextureBatchSize];

float4      CustomRenderTextureParameters;
#define     CustomRenderTextureUpdateSpace  CustomRenderTextureParameters.x // Normalized(0)/PixelSpace(1)
#define     CustomRenderTexture3DTexcoordW  CustomRenderTextureParameters.y
#define     CustomRenderTextureIs3D         CustomRenderTextureParameters.z

// User facing uniform variables
float4      _CustomRenderTextureInfo; // x = width, y = height, z = depth, w = face/3DSlice

// Helpers
#define _CustomRenderTextureWidth   _CustomRenderTextureInfo.x
#define _CustomRenderTextureHeight  _CustomRenderTextureInfo.y
#define _CustomRenderTextureDepth   _CustomRenderTextureInfo.z

// Those two are mutually exclusive so we can use the same slot
#define _CustomRenderTextureCubeFace    _CustomRenderTextureInfo.w
#define _CustomRenderTexture3DSlice     _CustomRenderTextureInfo.w

sampler2D   _SelfTexture2D;
samplerCUBE _SelfTextureCube;
sampler3D   _SelfTexture3D;

float3 CustomRenderTextureComputeCubeDirection(float2 globalTexcoord)
{
    float2 xy = globalTexcoord * 2.0 - 1.0;
    float3 direction;
    if(_CustomRenderTextureCubeFace == 0.0)
    {
        direction = normalize(float3(1.0, -xy.y, -xy.x));
    }
    else if(_CustomRenderTextureCubeFace == 1.0)
    {
        direction = normalize(float3(-1.0, -xy.y, xy.x));
    }
    else if(_CustomRenderTextureCubeFace == 2.0)
    {
        direction = normalize(float3(xy.x, 1.0, xy.y));
    }
    else if(_CustomRenderTextureCubeFace == 3.0)
    {
        direction = normalize(float3(xy.x, -1.0, -xy.y));
    }
    else if(_CustomRenderTextureCubeFace == 4.0)
    {
        direction = normalize(float3(xy.x, -xy.y, 1.0));
    }
    else if(_CustomRenderTextureCubeFace == 5.0)
    {
        direction = normalize(float3(-xy.x, -xy.y, -1.0));
    }

    return direction;
}

// standard custom texture vertex shader that should always be used
v2f_customrendertexture CustomRenderTextureVertexShader(appdata_customrendertexture IN)
{
    v2f_customrendertexture OUT;

#if UNITY_UV_STARTS_AT_TOP
    const float2 vertexPositions[customTextureVertexNum] =
    {
        { -1.0f,  1.0f },
        { -1.0f, -1.0f },
        {  1.0f, -1.0f },
        {  1.0f,  1.0f },
        { -1.0f,  1.0f },
        {  1.0f, -1.0f }
    };

    const float2 texCoords[customTextureVertexNum] =
    {
        { 0.0f, 0.0f },
        { 0.0f, 1.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f },
        { 0.0f, 0.0f },
        { 1.0f, 1.0f }
    };
#else
    const float2 vertexPositions[customTextureVertexNum] =
    {
        {  1.0f,  1.0f },
        { -1.0f, -1.0f },
        { -1.0f,  1.0f },
        { -1.0f, -1.0f },
        {  1.0f,  1.0f },
        {  1.0f, -1.0f }
    };

    const float2 texCoords[customTextureVertexNum] =
    {
        { 1.0f, 1.0f },
        { 0.0f, 0.0f },
        { 0.0f, 1.0f },
        { 0.0f, 0.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f }
    };
#endif

    uint primitiveID = (IN.vertexID / customTextureVertexNum) % kCustomTextureBatchSize;
    uint vertexID = IN.vertexID % customTextureVertexNum;
    float3 updateZoneCenter = CustomRenderTextureCenters[primitiveID].xyz;
    float3 updateZoneSize = CustomRenderTextureSizesAndRotations[primitiveID].xyz;
    float rotation = CustomRenderTextureSizesAndRotations[primitiveID].w * UNITY_PI / 180.0f;

#if !UNITY_UV_STARTS_AT_TOP
    rotation = -rotation;
#endif

    // Normalize rect if needed
    if (CustomRenderTextureUpdateSpace > 0.0) // Pixel space
    {
        // Normalize xy because we need it in clip space.
        updateZoneCenter.xy /= _CustomRenderTextureInfo.xy;
        updateZoneSize.xy /= _CustomRenderTextureInfo.xy;
    }
    else // normalized space
    {
        // Un-normalize depth because we need actual slice index for culling
        updateZoneCenter.z *= _CustomRenderTextureInfo.z;
        updateZoneSize.z *= _CustomRenderTextureInfo.z;
    }

    // Compute rotation

    // Compute quad vertex position
    float2 clipSpaceCenter = updateZoneCenter.xy * 2.0 - 1.0;
    float2 pos = vertexPositions[vertexID] * updateZoneSize.xy;
    pos = CustomRenderTextureRotate2D(pos, rotation);
    pos.x += clipSpaceCenter.x;
#if UNITY_UV_STARTS_AT_TOP
    pos.y += clipSpaceCenter.y;
#else
    pos.y -= clipSpaceCenter.y;
#endif

    // For 3D texture, cull quads outside of the update zone
    // This is neeeded in additional to the preliminary minSlice/maxSlice done on the CPU because update zones can be disjointed.
    // ie: slices [1..5] and [10..15] for two differents zones so we need to cull out slices 0 and [6..9]
    if (CustomRenderTextureIs3D > 0.0)
    {
        int minSlice = (int)(updateZoneCenter.z - updateZoneSize.z * 0.5);
        int maxSlice = minSlice + (int)updateZoneSize.z;
        if (_CustomRenderTexture3DSlice < minSlice || _CustomRenderTexture3DSlice >= maxSlice)
        {
            pos.xy = float2(1000.0, 1000.0); // Vertex outside of ncs
        }
    }

    OUT.vertex = float4(pos, UNITY_NEAR_CLIP_VALUE, 1.0);
    OUT.primitiveID = asuint(CustomRenderTexturePrimitiveIDs[primitiveID]);
    OUT.localTexcoord = float3(texCoords[vertexID], CustomRenderTexture3DTexcoordW);
    OUT.globalTexcoord = float3(pos.xy * 0.5 + 0.5, CustomRenderTexture3DTexcoordW);
#if UNITY_UV_STARTS_AT_TOP
    OUT.globalTexcoord.y = 1.0 - OUT.globalTexcoord.y;
#endif
    OUT.direction = CustomRenderTextureComputeCubeDirection(OUT.globalTexcoord.xy);

    return OUT;
}

struct appdata_init_customrendertexture
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
};

// User facing vertex to fragment structure for initialization materials
struct v2f_init_customrendertexture
{
    float4 vertex : SV_POSITION;
    float3 texcoord : TEXCOORD0;
    float3 direction : TEXCOORD1;
};

// standard custom texture vertex shader that should always be used for initialization shaders
v2f_init_customrendertexture InitCustomRenderTextureVertexShader (appdata_init_customrendertexture v)
{
    v2f_init_customrendertexture o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.texcoord = float3(v.texcoord.xy, CustomRenderTexture3DTexcoordW);
    o.direction = CustomRenderTextureComputeCubeDirection(v.texcoord.xy);
    return o;
}

#endif // UNITY_CUSTOM_TEXTURE_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityCustomRenderTexture.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityDeferredLibrary.cginc---------------


#ifndef UNITY_DEFERRED_LIBRARY_INCLUDED
#define UNITY_DEFERRED_LIBRARY_INCLUDED

// Deferred shading helpers


// --------------------------------------------------------
// Vertex shader

struct unity_v2f_deferred {
    float4 pos : SV_POSITION;
    float4 uv : TEXCOORD0;
    float3 ray : TEXCOORD1;
};

float _LightAsQuad;

unity_v2f_deferred vert_deferred (float4 vertex : POSITION, float3 normal : NORMAL)
{
    unity_v2f_deferred o;
    o.pos = UnityObjectToClipPos(vertex);
    o.uv = ComputeScreenPos(o.pos);
    o.ray = UnityObjectToViewPos(vertex) * float3(-1,-1,1);

    // normal contains a ray pointing from the camera to one of near plane's
    // corners in camera space when we are drawing a full screen quad.
    // Otherwise, when rendering 3D shapes, use the ray calculated here.
    o.ray = lerp(o.ray, normal, _LightAsQuad);

    return o;
}


// --------------------------------------------------------
// Shared uniforms


UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

float4 _LightDir;
float4 _LightPos;
float4 _LightColor;
float4 unity_LightmapFade;
float4x4 unity_WorldToLight;
sampler2D_float _LightTextureB0;

#if defined (POINT_COOKIE)
samplerCUBE_float _LightTexture0;
#else
sampler2D_float _LightTexture0;
#endif

#if defined (SHADOWS_SCREEN)
sampler2D _ShadowMapTexture;
#endif

#if defined (SHADOWS_SHADOWMASK)
sampler2D _CameraGBufferTexture4;
#endif

// --------------------------------------------------------
// Shadow/fade helpers

// Receiver plane depth bias create artifacts when depth is retrieved from
// the depth buffer. see UnityGetReceiverPlaneDepthBias in UnityShadowLibrary.cginc
#ifdef UNITY_USE_RECEIVER_PLANE_BIAS
    #undef UNITY_USE_RECEIVER_PLANE_BIAS
#endif

#include "UnityShadowLibrary.cginc"


//Note :
// SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING -> ShadowMask mode
// SHADOWS_SHADOWMASK only -> Distance shadowmask mode

// --------------------------------------------------------
half UnityDeferredSampleShadowMask(float2 uv)
{
    half shadowMaskAttenuation = 1.0f;

    #if defined (SHADOWS_SHADOWMASK)
        half4 shadowMask = tex2D(_CameraGBufferTexture4, uv);
        shadowMaskAttenuation = saturate(dot(shadowMask, unity_OcclusionMaskSelector));
    #endif

    return shadowMaskAttenuation;
}

// --------------------------------------------------------
half UnityDeferredSampleRealtimeShadow(half fade, float3 vec, float2 uv)
{
    half shadowAttenuation = 1.0f;

    #if defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
        #if defined(SHADOWS_SCREEN)
            shadowAttenuation = tex2D(_ShadowMapTexture, uv).r;
        #endif
    #endif

    #if defined(UNITY_FAST_COHERENT_DYNAMIC_BRANCHING) && defined(SHADOWS_SOFT) && !defined(LIGHTMAP_SHADOW_MIXING)
    //avoid expensive shadows fetches in the distance where coherency will be good
    UNITY_BRANCH
    if (fade < (1.0f - 1e-2f))
    {
    #endif

        #if defined(SPOT)
            #if defined(SHADOWS_DEPTH)
                float4 shadowCoord = mul(unity_WorldToShadow[0], float4(vec, 1));
                shadowAttenuation = UnitySampleShadowmap(shadowCoord);
            #endif
        #endif

        #if defined (POINT) || defined (POINT_COOKIE)
            #if defined(SHADOWS_CUBE)
                shadowAttenuation = UnitySampleShadowmap(vec);
            #endif
        #endif

    #if defined(UNITY_FAST_COHERENT_DYNAMIC_BRANCHING) && defined(SHADOWS_SOFT) && !defined(LIGHTMAP_SHADOW_MIXING)
    }
    #endif

    return shadowAttenuation;
}

// --------------------------------------------------------
half UnityDeferredComputeShadow(float3 vec, float fadeDist, float2 uv)
{

    half fade                      = UnityComputeShadowFade(fadeDist);
    half shadowMaskAttenuation     = UnityDeferredSampleShadowMask(uv);
    half realtimeShadowAttenuation = UnityDeferredSampleRealtimeShadow(fade, vec, uv);

    return UnityMixRealtimeAndBakedShadows(realtimeShadowAttenuation, shadowMaskAttenuation, fade);
}

// --------------------------------------------------------
// Common lighting data calculation (direction, attenuation, ...)
void UnityDeferredCalculateLightParams (
    unity_v2f_deferred i,
    out float3 outWorldPos,
    out float2 outUV,
    out half3 outLightDir,
    out float outAtten,
    out float outFadeDist)
{
    i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
    float2 uv = i.uv.xy / i.uv.w;

    // read depth and reconstruct world position
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
    depth = Linear01Depth (depth);
    float4 vpos = float4(i.ray * depth,1);
    float3 wpos = mul (unity_CameraToWorld, vpos).xyz;

    float fadeDist = UnityComputeShadowFadeDistance(wpos, vpos.z);

    // spot light case
    #if defined (SPOT)
        float3 tolight = _LightPos.xyz - wpos;
        half3 lightDir = normalize (tolight);

        float4 uvCookie = mul (unity_WorldToLight, float4(wpos,1));
        // negative bias because http://aras-p.info/blog/2010/01/07/screenspace-vs-mip-mapping/
        float atten = tex2Dbias (_LightTexture0, float4(uvCookie.xy / uvCookie.w, 0, -8)).w;
        atten *= uvCookie.w < 0;
        float att = dot(tolight, tolight) * _LightPos.w;
        atten *= tex2D (_LightTextureB0, att.rr).r;

        atten *= UnityDeferredComputeShadow (wpos, fadeDist, uv);

    // directional light case
    #elif defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
        half3 lightDir = -_LightDir.xyz;
        float atten = 1.0;

        atten *= UnityDeferredComputeShadow (wpos, fadeDist, uv);

        #if defined (DIRECTIONAL_COOKIE)
        atten *= tex2Dbias (_LightTexture0, float4(mul(unity_WorldToLight, half4(wpos,1)).xy, 0, -8)).w;
        #endif //DIRECTIONAL_COOKIE

    // point light case
    #elif defined (POINT) || defined (POINT_COOKIE)
        float3 tolight = wpos - _LightPos.xyz;
        half3 lightDir = -normalize (tolight);

        float att = dot(tolight, tolight) * _LightPos.w;
        float atten = tex2D (_LightTextureB0, att.rr).r;

        atten *= UnityDeferredComputeShadow (tolight, fadeDist, uv);

        #if defined (POINT_COOKIE)
        atten *= texCUBEbias(_LightTexture0, float4(mul(unity_WorldToLight, half4(wpos,1)).xyz, -8)).w;
        #endif //POINT_COOKIE
    #else
        half3 lightDir = 0;
        float atten = 0;
    #endif

    outWorldPos = wpos;
    outUV = uv;
    outLightDir = lightDir;
    outAtten = atten;
    outFadeDist = fadeDist;
}

#endif // UNITY_DEFERRED_LIBRARY_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityDeferredLibrary.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityDeprecated.cginc---------------


//-----------------------------------------------------------------------------
// NOTICE:
// All functions in this file are deprecated and should not be use, they will be remove in a later version.
// They are let here for backward compatibility.
// This file gather several function related to different part of shader code like BRDF or image based lighting
// to avoid to create multiple deprecated file, this file include deprecated function based on a define
// to when including this file, it is expected that the caller define which deprecated function group he want to enable
// Example, following code will include all deprecated BRDF functions:
// #define INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED
// #include "UnityDeprecated.cginc"
// #undef INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED
//-----------------------------------------------------------------------------

#ifdef INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED

inline half3 LazarovFresnelTerm (half3 F0, half roughness, half cosA)
{
    half t = Pow5 (1 - cosA);   // ala Schlick interpoliation
    t /= 4 - 3 * roughness;
    return F0 + (1-F0) * t;
}
inline half3 SebLagardeFresnelTerm (half3 F0, half roughness, half cosA)
{
    half t = Pow5 (1 - cosA);   // ala Schlick interpoliation
    return F0 + (max (F0, roughness) - F0) * t;
}

// Cook-Torrance visibility term, doesn't take roughness into account
inline half CookTorranceVisibilityTerm (half NdotL, half NdotV,  half NdotH, half VdotH)
{
    VdotH += 1e-5f;
    half G = min (1.0, min (
        (2.0 * NdotH * NdotV) / VdotH,
        (2.0 * NdotH * NdotL) / VdotH));
    return G / (NdotL * NdotV + 1e-4f);
}

// Kelemen-Szirmay-Kalos is an approximation to Cook-Torrance visibility term
// http://sirkan.iit.bme.hu/~szirmay/scook.pdf
inline half KelemenVisibilityTerm (half LdotH)
{
    return 1.0 / (LdotH * LdotH);
}

// Modified Kelemen-Szirmay-Kalos which takes roughness into account, based on: http://www.filmicworlds.com/2014/04/21/optimizing-ggx-shaders-with-dotlh/
inline half ModifiedKelemenVisibilityTerm (half LdotH, half perceptualRoughness)
{
    half c = 0.797884560802865h; // c = sqrt(2 / Pi)
    half k = PerceptualRoughnessToRoughness(perceptualRoughness) * c;
    half gH = LdotH * (1-k) + k;
    return 1.0 / (gH * gH);
}

// Smith-Schlick derived for GGX
inline half SmithGGXVisibilityTerm (half NdotL, half NdotV, half perceptualRoughness)
{
    half k = (PerceptualRoughnessToRoughness(perceptualRoughness)) / 2; // derived by B. Karis, http://graphicrants.blogspot.se/2013/08/specular-brdf-reference.html
    return SmithVisibilityTerm (NdotL, NdotV, k);
}

inline half ImplicitVisibilityTerm ()
{
    return 1;
}

// BlinnPhong normalized as reflection density­sity function (RDF)
// ready for use directly as specular: spec=D
// http://www.thetenthplanet.de/archives/255
inline half RDFBlinnPhongNormalizedTerm (half NdotH, half n)
{
    half normTerm = (n + 2.0) / (8.0 * UNITY_PI);
    half specTerm = pow (NdotH, n);
    return specTerm * normTerm;
}

// Decodes HDR textures
// sm 2.0 is no longer supported
inline half3 DecodeHDR_NoLinearSupportInSM2 (half4 data, half4 decodeInstructions)
{
    // If Linear mode is not supported we can skip exponent part
    // In Standard shader SM2.0 and SM3.0 paths are always using different shader variations
    // SM2.0: hardware does not support Linear, we can skip exponent part
#if defined(UNITY_COLORSPACE_GAMMA) && (SHADER_TARGET < 30)
    return (data.a * decodeInstructions.x) * data.rgb;
#else
    return DecodeHDR(data, decodeInstructions);
#endif
}

inline half DotClamped (half3 a, half3 b)
{
    #if (SHADER_TARGET < 30)
        return saturate(dot(a, b));
    #else
        return max(0.0h, dot(a, b));
    #endif
}

inline half LambertTerm (half3 normal, half3 lightDir)
{
    return DotClamped (normal, lightDir);
}

inline half BlinnTerm (half3 normal, half3 halfDir)
{
    return DotClamped (normal, halfDir);
}

half RoughnessToSpecPower (half roughness)
{
    return PerceptualRoughnessToSpecPower (roughness);
}

//-------------------------------------------------------------------------------------
// Legacy, to keep backwards compatibility for (pre Unity 5.3) custom user shaders:
#ifdef UNITY_COLORSPACE_GAMMA
#   define unity_LightGammaCorrectionConsts_PIDiv4 ((UNITY_PI/4)*(UNITY_PI/4))
#   define unity_LightGammaCorrectionConsts_HalfDivPI ((.5h/UNITY_PI)*(.5h/UNITY_PI))
#   define unity_LightGammaCorrectionConsts_8 (8*8)
#   define unity_LightGammaCorrectionConsts_SqrtHalfPI (2/UNITY_PI)
#else
#   define unity_LightGammaCorrectionConsts_PIDiv4 (UNITY_PI/4)
#   define unity_LightGammaCorrectionConsts_HalfDivPI (.5h/UNITY_PI)
#   define unity_LightGammaCorrectionConsts_8 (8)
#   define unity_LightGammaCorrectionConsts_SqrtHalfPI (0.79788)
#endif

#endif // INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED

#ifdef INCLUDE_UNITY_IMAGE_BASED_LIGHTING_DEPRECATED

// Old Unity_GlossyEnvironment signature. Kept only for backward compatibility and will be removed soon
half3 Unity_GlossyEnvironment (UNITY_ARGS_TEXCUBE(tex), half4 hdr, half3 worldNormal, half perceptualRoughness)
{
    Unity_GlossyEnvironmentData g;
    g.roughness /* perceptualRoughness */ = perceptualRoughness;
    g.reflUVW   = worldNormal;

    return Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(tex), hdr, g);
}

#endif // INCLUDE_UNITY_IMAGE_BASED_LIGHTING_DEPRECATED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityDeprecated.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityGBuffer.cginc---------------


#ifndef UNITY_GBUFFER_INCLUDED
#define UNITY_GBUFFER_INCLUDED

//-----------------------------------------------------------------------------
// Main structure that store the data from the standard shader (i.e user input)
struct UnityStandardData
{
    half3   diffuseColor;
    half    occlusion;

    half3   specularColor;
    half    smoothness;

    float3  normalWorld;        // normal in world space
};

//-----------------------------------------------------------------------------
// This will encode UnityStandardData into GBuffer
void UnityStandardDataToGbuffer(UnityStandardData data, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    // RT0: diffuse color (rgb), occlusion (a) - sRGB rendertarget
    outGBuffer0 = half4(data.diffuseColor, data.occlusion);

    // RT1: spec color (rgb), smoothness (a) - sRGB rendertarget
    outGBuffer1 = half4(data.specularColor, data.smoothness);

    // RT2: normal (rgb), --unused, very low precision-- (a)
    outGBuffer2 = half4(data.normalWorld * 0.5f + 0.5f, 1.0f);
}
//-----------------------------------------------------------------------------
// This decode the Gbuffer in a UnityStandardData struct
UnityStandardData UnityStandardDataFromGbuffer(half4 inGBuffer0, half4 inGBuffer1, half4 inGBuffer2)
{
    UnityStandardData data;

    data.diffuseColor   = inGBuffer0.rgb;
    data.occlusion      = inGBuffer0.a;

    data.specularColor  = inGBuffer1.rgb;
    data.smoothness     = inGBuffer1.a;

    data.normalWorld    = normalize((float3)inGBuffer2.rgb * 2 - 1);

    return data;
}
//-----------------------------------------------------------------------------
// In some cases like for terrain, the user want to apply a specific weight to the attribute
// The function below is use for this
void UnityStandardDataApplyWeightToGbuffer(inout half4 inOutGBuffer0, inout half4 inOutGBuffer1, inout half4 inOutGBuffer2, half alpha)
{
    // With UnityStandardData current encoding, We can apply the weigth directly on the gbuffer
    inOutGBuffer0.rgb   *= alpha; // diffuseColor
    inOutGBuffer1       *= alpha; // SpecularColor and Smoothness
    inOutGBuffer2.rgb   *= alpha; // Normal
}
//-----------------------------------------------------------------------------

#endif // #ifndef UNITY_GBUFFER_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityGBuffer.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityGlobalIllumination.cginc---------------


#ifndef UNITY_GLOBAL_ILLUMINATION_INCLUDED
#define UNITY_GLOBAL_ILLUMINATION_INCLUDED

// Functions sampling light environment data (lightmaps, light probes, reflection probes), which is then returned as the UnityGI struct.

#include "UnityImageBasedLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityShadowLibrary.cginc"

inline half3 DecodeDirectionalSpecularLightmap (half3 color, half4 dirTex, half3 normalWorld, bool isRealtimeLightmap, fixed4 realtimeNormalTex, out UnityLight o_light)
{
    o_light.color = color;
    o_light.dir = dirTex.xyz * 2 - 1;
    o_light.ndotl = 0; // Not use;

    // The length of the direction vector is the light's "directionality", i.e. 1 for all light coming from this direction,
    // lower values for more spread out, ambient light.
    half directionality = max(0.001, length(o_light.dir));
    o_light.dir /= directionality;

    #ifdef DYNAMICLIGHTMAP_ON
    if (isRealtimeLightmap)
    {
        // Realtime directional lightmaps' intensity needs to be divided by N.L
        // to get the incoming light intensity. Baked directional lightmaps are already
        // output like that (including the max() to prevent div by zero).
        half3 realtimeNormal = realtimeNormalTex.xyz * 2 - 1;
        o_light.color /= max(0.125, dot(realtimeNormal, o_light.dir));
    }
    #endif

    // Split light into the directional and ambient parts, according to the directionality factor.
    half3 ambient = o_light.color * (1 - directionality);
    o_light.color = o_light.color * directionality;

    // Technically this is incorrect, but helps hide jagged light edge at the object silhouettes and
    // makes normalmaps show up.
    ambient *= saturate(dot(normalWorld, o_light.dir));
    return ambient;
}

inline void ResetUnityLight(out UnityLight outLight)
{
    outLight.color = half3(0, 0, 0);
    outLight.dir = half3(0, 1, 0); // Irrelevant direction, just not null
    outLight.ndotl = 0; // Not used
}

inline half3 SubtractMainLightWithRealtimeAttenuationFromLightmap (half3 lightmap, half attenuation, half4 bakedColorTex, half3 normalWorld)
{
    // Let's try to make realtime shadows work on a surface, which already contains
    // baked lighting and shadowing from the main sun light.
    half3 shadowColor = unity_ShadowColor.rgb;
    half shadowStrength = _LightShadowData.x;

    // Summary:
    // 1) Calculate possible value in the shadow by subtracting estimated light contribution from the places occluded by realtime shadow:
    //      a) preserves other baked lights and light bounces
    //      b) eliminates shadows on the geometry facing away from the light
    // 2) Clamp against user defined ShadowColor.
    // 3) Pick original lightmap value, if it is the darkest one.


    // 1) Gives good estimate of illumination as if light would've been shadowed during the bake.
    //    Preserves bounce and other baked lights
    //    No shadows on the geometry facing away from the light
    half ndotl = LambertTerm (normalWorld, _WorldSpaceLightPos0.xyz);
    half3 estimatedLightContributionMaskedByInverseOfShadow = ndotl * (1- attenuation) * _LightColor0.rgb;
    half3 subtractedLightmap = lightmap - estimatedLightContributionMaskedByInverseOfShadow;

    // 2) Allows user to define overall ambient of the scene and control situation when realtime shadow becomes too dark.
    half3 realtimeShadow = max(subtractedLightmap, shadowColor);
    realtimeShadow = lerp(realtimeShadow, lightmap, shadowStrength);

    // 3) Pick darkest color
    return min(lightmap, realtimeShadow);
}

inline void ResetUnityGI(out UnityGI outGI)
{
    ResetUnityLight(outGI.light);
    outGI.indirect.diffuse = 0;
    outGI.indirect.specular = 0;
}

inline UnityGI UnityGI_Base(UnityGIInput data, half occlusion, half3 normalWorld)
{
    UnityGI o_gi;
    ResetUnityGI(o_gi);

    // Base pass with Lightmap support is responsible for handling ShadowMask / blending here for performance reason
    #if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
        half bakedAtten = UnitySampleBakedOcclusion(data.lightmapUV.xy, data.worldPos);
        float zDist = dot(_WorldSpaceCameraPos - data.worldPos, UNITY_MATRIX_V[2].xyz);
        float fadeDist = UnityComputeShadowFadeDistance(data.worldPos, zDist);
        data.atten = UnityMixRealtimeAndBakedShadows(data.atten, bakedAtten, UnityComputeShadowFade(fadeDist));
    #endif

    o_gi.light = data.light;
    o_gi.light.color *= data.atten;

    #if UNITY_SHOULD_SAMPLE_SH
        o_gi.indirect.diffuse = ShadeSHPerPixel(normalWorld, data.ambient, data.worldPos);
    #endif

    #if defined(LIGHTMAP_ON)
        // Baked lightmaps
        half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, data.lightmapUV.xy);
        half3 bakedColor = DecodeLightmap(bakedColorTex);

        #ifdef DIRLIGHTMAP_COMBINED
            fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, data.lightmapUV.xy);
            o_gi.indirect.diffuse += DecodeDirectionalLightmap (bakedColor, bakedDirTex, normalWorld);

            #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
                ResetUnityLight(o_gi.light);
                o_gi.indirect.diffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap (o_gi.indirect.diffuse, data.atten, bakedColorTex, normalWorld);
            #endif

        #else // not directional lightmap
            o_gi.indirect.diffuse += bakedColor;

            #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
                ResetUnityLight(o_gi.light);
                o_gi.indirect.diffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap(o_gi.indirect.diffuse, data.atten, bakedColorTex, normalWorld);
            #endif

        #endif
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        // Dynamic lightmaps
        fixed4 realtimeColorTex = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, data.lightmapUV.zw);
        half3 realtimeColor = DecodeRealtimeLightmap (realtimeColorTex);

        #ifdef DIRLIGHTMAP_COMBINED
            half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, data.lightmapUV.zw);
            o_gi.indirect.diffuse += DecodeDirectionalLightmap (realtimeColor, realtimeDirTex, normalWorld);
        #else
            o_gi.indirect.diffuse += realtimeColor;
        #endif
    #endif

    o_gi.indirect.diffuse *= occlusion;
    return o_gi;
}


inline half3 UnityGI_IndirectSpecular(UnityGIInput data, half occlusion, Unity_GlossyEnvironmentData glossIn)
{
    half3 specular;

    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
        // we will tweak reflUVW in glossIn directly (as we pass it to Unity_GlossyEnvironment twice for probe0 and probe1), so keep original to pass into BoxProjectedCubemapDirection
        half3 originalReflUVW = glossIn.reflUVW;
        glossIn.reflUVW = BoxProjectedCubemapDirection (originalReflUVW, data.worldPos, data.probePosition[0], data.boxMin[0], data.boxMax[0]);
    #endif

    #ifdef _GLOSSYREFLECTIONS_OFF
        specular = unity_IndirectSpecColor.rgb;
    #else
        half3 env0 = Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(unity_SpecCube0), data.probeHDR[0], glossIn);
        #ifdef UNITY_SPECCUBE_BLENDING
            const float kBlendFactor = 0.99999;
            float blendLerp = data.boxMin[0].w;
            UNITY_BRANCH
            if (blendLerp < kBlendFactor)
            {
                #ifdef UNITY_SPECCUBE_BOX_PROJECTION
                    glossIn.reflUVW = BoxProjectedCubemapDirection (originalReflUVW, data.worldPos, data.probePosition[1], data.boxMin[1], data.boxMax[1]);
                #endif

                half3 env1 = Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1,unity_SpecCube0), data.probeHDR[1], glossIn);
                specular = lerp(env1, env0, blendLerp);
            }
            else
            {
                specular = env0;
            }
        #else
            specular = env0;
        #endif
    #endif

    return specular * occlusion;
}

// Deprecated old prototype but can't be move to Deprecated.cginc file due to order dependency
inline half3 UnityGI_IndirectSpecular(UnityGIInput data, half occlusion, half3 normalWorld, Unity_GlossyEnvironmentData glossIn)
{
    // normalWorld is not used
    return UnityGI_IndirectSpecular(data, occlusion, glossIn);
}

inline UnityGI UnityGlobalIllumination (UnityGIInput data, half occlusion, half3 normalWorld)
{
    return UnityGI_Base(data, occlusion, normalWorld);
}

inline UnityGI UnityGlobalIllumination (UnityGIInput data, half occlusion, half3 normalWorld, Unity_GlossyEnvironmentData glossIn)
{
    UnityGI o_gi = UnityGI_Base(data, occlusion, normalWorld);
    o_gi.indirect.specular = UnityGI_IndirectSpecular(data, occlusion, glossIn);
    return o_gi;
}

//
// Old UnityGlobalIllumination signatures. Kept only for backward compatibility and will be removed soon
//

inline UnityGI UnityGlobalIllumination (UnityGIInput data, half occlusion, half smoothness, half3 normalWorld, bool reflections)
{
    if(reflections)
    {
        Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(smoothness, data.worldViewDir, normalWorld, float3(0, 0, 0));
        return UnityGlobalIllumination(data, occlusion, normalWorld, g);
    }
    else
    {
        return UnityGlobalIllumination(data, occlusion, normalWorld);
    }
}
inline UnityGI UnityGlobalIllumination (UnityGIInput data, half occlusion, half smoothness, half3 normalWorld)
{
#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
    // No need to sample reflection probes during deferred G-buffer pass
    bool sampleReflections = false;
#else
    bool sampleReflections = true;
#endif
    return UnityGlobalIllumination (data, occlusion, smoothness, normalWorld, sampleReflections);
}


#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityGlobalIllumination.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityImageBasedLighting.cginc---------------


#ifndef UNITY_IMAGE_BASED_LIGHTING_INCLUDED
#define UNITY_IMAGE_BASED_LIGHTING_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityStandardBRDF.cginc"

// ----------------------------------------------------------------------------

#if 0

// ----------------------------------------------------------------------------
// Unity is Y up - left handed

//-----------------------------------------------------------------------------
// Sample generator
//-----------------------------------------------------------------------------
// Ref: http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
uint ReverseBits32(uint bits)
{
#if 0 // Shader model 5
    return reversebits(bits);
#else
    bits = ( bits << 16) | ( bits >> 16);
    bits = ((bits & 0x00ff00ff) << 8) | ((bits & 0xff00ff00) >> 8);
    bits = ((bits & 0x0f0f0f0f) << 4) | ((bits & 0xf0f0f0f0) >> 4);
    bits = ((bits & 0x33333333) << 2) | ((bits & 0xcccccccc) >> 2);
    bits = ((bits & 0x55555555) << 1) | ((bits & 0xaaaaaaaa) >> 1);
    return bits;
#endif
}
//-----------------------------------------------------------------------------
float RadicalInverse_VdC(uint bits)
{
    return float(ReverseBits32(bits)) * 2.3283064365386963e-10; // 0x100000000
}

//-----------------------------------------------------------------------------
float2 Hammersley2d(uint i, uint maxSampleCount)
{
    return float2(float(i) / float(maxSampleCount), RadicalInverse_VdC(i));
}

//-----------------------------------------------------------------------------
float Hash(uint s)
{
    s = s ^ 2747636419u;
    s = s * 2654435769u;
    s = s ^ (s >> 16);
    s = s * 2654435769u;
    s = s ^ (s >> 16);
    s = s * 2654435769u;
    return float(s) / 4294967295.0f;
}

//-----------------------------------------------------------------------------
float2 InitRandom(float2 input)
{
    float2 r;
    r.x = Hash(uint(input.x * 4294967295.0f));
    r.y = Hash(uint(input.y * 4294967295.0f));

    return r;
}

//-----------------------------------------------------------------------------
// Util
//-----------------------------------------------------------------------------

// generate an orthonormalBasis from 3d unit vector.
void GetLocalFrame(float3 N, out float3 tangentX, out float3 tangentY)
{
    float3 upVector     = abs(N.z) < 0.999f ? float3(0.0f, 0.0f, 1.0f) : float3(1.0f, 0.0f, 0.0f);
    tangentX            = normalize(cross(upVector, N));
    tangentY            = cross(N, tangentX);
}

/*
// http://orbit.dtu.dk/files/57573287/onb_frisvad_jgt2012.pdf
void GetLocalFrame(float3 N, out float3 tangentX, out float3 tangentY)
{
    if (N.z < -0.999f) // Handle the singularity
    {
        tangentX = Vec3f (0.0f, -1.0f, 0.0f);
        tangentY = Vec3f (-1.0f, 0.0f, 0.0f);
        return ;
    }

    float a     = 1.0f / (1.0f + N.z);
    float b     = -N.x * N.y * a ;
    tangentX    = float3(1.0f - N.x * N.x * a , b, -N.x);
    tangentY    = float3(b, 1.0f - N.y * N.y * a, -N.y);
}
*/

// ----------------------------------------------------------------------------
// Sampling
// ----------------------------------------------------------------------------

void ImportanceSampleCosDir(float2 u,
                            float3 N,
                            float3 tangentX,
                            float3 tangentY,
                            out float3 L)
{
    // Cosine sampling - ref: http://www.rorydriscoll.com/2009/01/07/better-sampling/
    float cosTheta = sqrt(max(0.0f, 1.0f - u.x));
    float sinTheta = sqrt(u.x);
    float phi = UNITY_TWO_PI * u.y;

    // Transform from spherical into cartesian
    L = float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta);
    // Local to world
    L = tangentX * L.x + tangentY * L.y + N * L.z;
}

//-------------------------------------------------------------------------------------
void ImportanceSampleGGXDir(float2 u,
                            float3 V,
                            float3 N,
                            float3 tangentX,
                            float3 tangentY,
                            float roughness,
                            out float3 H,
                            out float3 L)
{
    // GGX NDF sampling
    float cosThetaH = sqrt((1.0f - u.x) / (1.0f + (roughness * roughness - 1.0f) * u.x));
    float sinThetaH = sqrt(max(0.0f, 1.0f - cosThetaH * cosThetaH));
    float phiH      = UNITY_TWO_PI * u.y;

    // Transform from spherical into cartesian
    H = float3(sinThetaH * cos(phiH), sinThetaH * sin(phiH), cosThetaH);
    // Local to world
    H = tangentX * H.x + tangentY * H.y + N * H.z;

    // Convert sample from half angle to incident angle
    L = 2.0f * dot(V, H) * H - V;
}

// ----------------------------------------------------------------------------
// weightOverPdf return the weight (without the diffuseAlbedo term) over pdf. diffuseAlbedo term must be apply by the caller.
void ImportanceSampleLambert(
    float2 u,
    float3 N,
    float3 tangentX,
    float3 tangentY,
    out float3 L,
    out float NdotL,
    out float weightOverPdf)
{
    ImportanceSampleCosDir(u, N, tangentX, tangentY, L);

    NdotL = saturate(dot(N, L));

    // Importance sampling weight for each sample
    // pdf = N.L / PI
    // weight = fr * (N.L) with fr = diffuseAlbedo / PI
    // weight over pdf is:
    // weightOverPdf = (diffuseAlbedo / PI) * (N.L) / (N.L / PI)
    // weightOverPdf = diffuseAlbedo
    // diffuseAlbedo is apply outside the function

    weightOverPdf = 1.0f;
}

// ----------------------------------------------------------------------------
// weightOverPdf return the weight (without the Fresnel term) over pdf. Fresnel term must be apply by the caller.
void ImportanceSampleGGX(
    float2 u,
    float3 V,
    float3 N,
    float3 tangentX,
    float3 tangentY,
    float roughness,
    float NdotV,
    out float3 L,
    out float VdotH,
    out float NdotL,
    out float weightOverPdf)
{
    float3 H;
    ImportanceSampleGGXDir(u, V, N, tangentX, tangentY, roughness, H, L);

    float NdotH = saturate(dot(N, H));
    // Note: since L and V are symmetric around H, LdotH == VdotH
    VdotH = saturate(dot(V, H));
    NdotL = saturate(dot(N, L));

    // Importance sampling weight for each sample
    // pdf = D(H) * (N.H) / (4 * (L.H))
    // weight = fr * (N.L) with fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
    // weight over pdf is:
    // weightOverPdf = F(H) * G(V, L) * (L.H) / ((N.H) * (N.V))
    // weightOverPdf = F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
    // F is apply outside the function

    float Vis = SmithJointGGXVisibilityTerm(NdotL, NdotV, roughness);
    weightOverPdf = 4.0f * Vis * NdotL * VdotH / NdotH;
}

//-----------------------------------------------------------------------------
// Reference
// ----------------------------------------------------------------------------

// Ref: Moving Frostbite to PBR (Appendix A)
void IntegrateLambertDiffuseIBLRef( out float3 diffuseLighting,
                                    UNITY_ARGS_TEXCUBE(tex),
                                    float4 texHdrParam, // Multiplier to apply on hdr texture (in case of rgbm)
                                    float3 N,
                                    float3 diffuseAlbedo,
                                    uint sampleCount = 2048)
{
    float3 acc      = float3(0.0f, 0.0f, 0.0f);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(N.xy * 0.5f + 0.5f);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5f);

        float3 L;
        float NdotL;
        float weightOverPdf;
        ImportanceSampleLambert(u, N, tangentX, tangentY, L, NdotL, weightOverPdf);

        if (NdotL > 0.0f)
        {
            float4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, L, 0).rgba;
            float3 val = DecodeHDR(rgbm, texHdrParam);

            // diffuse Albedo is apply here as describe in ImportanceSampleLambert function
            acc += diffuseAlbedo * weightOverPdf * val;
        }
    }

    diffuseLighting = acc / sampleCount;
}

// ----------------------------------------------------------------------------

void IntegrateDisneyDiffuseIBLRef(  out float3 diffuseLighting,
                                    UNITY_ARGS_TEXCUBE(tex),
                                    float4 texHdrParam, // Multiplier to apply on hdr texture (in case of rgbm)
                                    float3 N,
                                    float3 V,
                                    float roughness,
                                    float3 diffuseAlbedo,
                                    uint sampleCount = 2048)
{
    float NdotV = dot(N, V);
    float3 acc  = float3(0.0f, 0.0f, 0.0f);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(N.xy * 0.5f + 0.5f);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5f);

        float3 L;
        float NdotL;
        float weightOverPdf;
        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, N, tangentX, tangentY, L, NdotL, weightOverPdf);

        if (NdotL > 0.0f)
        {
            float4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, L, 0).rgba;
            float3 val = DecodeHDR(rgbm, texHdrParam);

            float3 H = normalize(L + V);
            float LdotH = dot(L, H);
            // Note: we call DisneyDiffuse that require to multiply by Albedo / PI. Divide by PI is already taken into account
            // in weightOverPdf of ImportanceSampleLambert call.
            float disneyDiffuse = DisneyDiffuse(NdotV, NdotL, LdotH, RoughnessToPerceptualRoughness(roughness));

            // diffuse Albedo is apply here as describe in ImportanceSampleLambert function
            acc += diffuseAlbedo * disneyDiffuse * weightOverPdf * val;
        }
    }

    diffuseLighting = acc / sampleCount;
}

// ----------------------------------------------------------------------------
// Ref: Moving Frostbite to PBR (Appendix A)
void IntegrateSpecularGGXIBLRef(out float3 specularLighting,
                                UNITY_ARGS_TEXCUBE(tex),
                                float4 texHdrParam, // Multiplier to apply on hdr texture (in case of rgbm)
                                float3 N,
                                float3 V,
                                float roughness,
                                float3 f0,
                                float f90,
                                uint sampleCount = 2048)
{
    float NdotV     = saturate(dot(N, V));
    float3 acc      = float3(0.0f, 0.0f, 0.0f);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5f + 0.5f);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5f);

        float VdotH;
        float NdotL;
        float3 L;
        float weightOverPdf;

        // GGX BRDF
        ImportanceSampleGGX(u, V, N, tangentX, tangentY, roughness, NdotV,
                            L, VdotH, NdotL, weightOverPdf);

        if (NdotL > 0.0f)
        {
            // Fresnel component is apply here as describe in ImportanceSampleGGX function
            float3 FweightOverPdf = FresnelLerp(f0, f90, VdotH) * weightOverPdf;

            float4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, L, 0).rgba;
            float3 val = DecodeHDR(rgbm, texHdrParam);

            acc += FweightOverPdf * val;
        }
    }

    specularLighting = acc / sampleCount;
}

// ----------------------------------------------------------------------------
// Pre-integration
// ----------------------------------------------------------------------------

// Ref: Listing 18 in "Moving Frostbite to PBR" + https://knarkowicz.wordpress.com/2014/12/27/analytical-dfg-term-for-ibl/
float4 IntegrateDFG(float3 V, float3 N, float roughness, uint sampleCount)
{
    float NdotV     = saturate(dot(N, V));
    float4 acc      = float4(0.0f, 0.0f, 0.0f, 0.0f);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5f + 0.5f);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5f);

        float VdotH;
        float NdotL;
        float weightOverPdf;

        float3 L; // Unused
        ImportanceSampleGGX(u, V, N, tangentX, tangentY, roughness, NdotV,
                            L, VdotH, NdotL, weightOverPdf);

        if (NdotL > 0.0f)
        {
            // Integral is
            //   1 / NumSample * \int[  L * fr * (N.L) / pdf ]  with pdf =  D(H) * (N.H) / (4 * (L.H)) and fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
            // This is split  in two part:
            //   A) \int[ L * (N.L) ]
            //   B) \int[ F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) ] with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
            //      = \int[ F(H) * weightOverPdf ]

            // Recombine at runtime with: ( f0 * weightOverPdf * (1 - Fc) + f90 * weightOverPdf * Fc ) with Fc =(1 - V.H)^5
            float Fc            = pow(1.0f - VdotH, 5.0f);
            acc.x               += (1.0f - Fc) * weightOverPdf;
            acc.y               += Fc * weightOverPdf;
        }

        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, N, tangentX, tangentY, L, NdotL, weightOverPdf);

        if (NdotL > 0.0f)
        {
            float3 H = normalize(L + V);
            float LdotH = dot(L, H);
            float disneyDiffuse = DisneyDiffuse(NdotV, NdotL, LdotH, RoughnessToPerceptualRoughness(roughness));

            acc.z += disneyDiffuse * weightOverPdf;
        }
    }

    return acc / sampleCount;
}

// ----------------------------------------------------------------------------
// Ref: Listing 19 in "Moving Frostbite to PBR"
// IntegrateLD will not work with RGBM cubemap. For now it is use with fp16 cubemap such as those use for real time cubemap.
float4 IntegrateLD( UNITY_ARGS_TEXCUBE(tex),
                    float3 V,
                    float3 N,
                    float roughness,
                    float mipmapcount,
                    float invOmegaP,
                    uint sampleCount,
                    bool prefilter = true) // static bool
{
    float3 acc          = float3(0.0f, 0.0f, 0.0f);
    float  accWeight    = 0;

    float2 randNum  = InitRandom(V.xy * 0.5f + 0.5f);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5f);

        float3 H;
        float3 L;
        ImportanceSampleGGXDir(u, V, N, tangentX, tangentY, roughness, H, L);

        float NdotL = saturate(dot(N,L));

        float mipLevel;

        if (!prefilter) // BRDF importance sampling
        {
            mipLevel = 0.0f;
        }
        else // Prefiltered BRDF importance sampling
        {
            float NdotH = saturate(dot(N, H));
            // Note: since L and V are symmetric around H, LdotH == VdotH
            float LdotH = saturate(dot(L, H));

            // Use pre - filtered importance sampling (i.e use lower mipmap
            // level for fetching sample with low probability in order
            // to reduce the variance ).
            // ( Reference : GPU Gem3: http://http.developer.nvidia.com/GPUGems3/gpugems3_ch20.html)
            //
            // Since we pre - integrate the result for normal direction ,
            // N == V and then NdotH == LdotH . This is why the BRDF pdf
            // can be simplifed from :
            // pdf = D * NdotH /(4* LdotH ) to pdf = D / 4;
            //
            // - OmegaS : Solid angle associated to a sample
            // - OmegaP : Solid angle associated to a pixel of the cubemap

            float pdf       = GGXTerm(NdotH, roughness) * NdotH / (4 * LdotH);
            float omegaS    = 1.0f / (sampleCount * pdf);                           // Solid angle associated to a sample
            // invOmegaP is precomputed on CPU and provide as a parameter of the function
            // float omegaP = UNITY_FOUR_PI / (6.0f * cubemapWidth * cubemapWidth); // Solid angle associated to a pixel of the cubemap
            // Clamp is not necessary as the hardware will do it.
            // mipLevel     = clamp(0.5f * log2(omegaS * invOmegaP), 0, mipmapcount);
            mipLevel        = 0.5f * log2(omegaS * invOmegaP); // Clamp is not necessary as the hardware will do it.
        }

        if (NdotL > 0.0f)
        {
            // No rgbm format here, only fp16
            float3 val = UNITY_SAMPLE_TEXCUBE_LOD(tex, L, mipLevel).rgba;

            // See p63 equation (53) of moving Frostbite to PBR v2 for the extra NdotL here (both in weight and value)
            acc             += val * NdotL;
            accWeight       += NdotL;
        }
    }

    return float4(acc * (1.0f / accWeight), 1.0f);
}

#endif // 0

// ----------------------------------------------------------------------------
// GlossyEnvironment - Function to integrate the specular lighting with default sky or reflection probes
// ----------------------------------------------------------------------------
struct Unity_GlossyEnvironmentData
{
    // - Deferred case have one cubemap
    // - Forward case can have two blended cubemap (unusual should be deprecated).

    // Surface properties use for cubemap integration
    half    roughness; // CAUTION: This is perceptualRoughness but because of compatibility this name can't be change :(
    half3   reflUVW;
};

// ----------------------------------------------------------------------------

Unity_GlossyEnvironmentData UnityGlossyEnvironmentSetup(half Smoothness, half3 worldViewDir, half3 Normal, half3 fresnel0)
{
    Unity_GlossyEnvironmentData g;

    g.roughness /* perceptualRoughness */   = SmoothnessToPerceptualRoughness(Smoothness);
    g.reflUVW   = reflect(-worldViewDir, Normal);

    return g;
}

// ----------------------------------------------------------------------------
half perceptualRoughnessToMipmapLevel(half perceptualRoughness)
{
    return perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
}

// ----------------------------------------------------------------------------
half mipmapLevelToPerceptualRoughness(half mipmapLevel)
{
    return mipmapLevel / UNITY_SPECCUBE_LOD_STEPS;
}

// ----------------------------------------------------------------------------
half3 Unity_GlossyEnvironment (UNITY_ARGS_TEXCUBE(tex), half4 hdr, Unity_GlossyEnvironmentData glossIn)
{
    half perceptualRoughness = glossIn.roughness /* perceptualRoughness */ ;

// TODO: CAUTION: remap from Morten may work only with offline convolution, see impact with runtime convolution!
// For now disabled
#if 0
    float m = PerceptualRoughnessToRoughness(perceptualRoughness); // m is the real roughness parameter
    const float fEps = 1.192092896e-07F;        // smallest such that 1.0+FLT_EPSILON != 1.0  (+1e-4h is NOT good here. is visibly very wrong)
    float n =  (2.0/max(fEps, m*m))-2.0;        // remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf

    n /= 4;                                     // remap from n_dot_h formulatino to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

    perceptualRoughness = pow( 2/(n+2), 0.25);      // remap back to square root of real roughness (0.25 include both the sqrt root of the conversion and sqrt for going from roughness to perceptualRoughness)
#else
    // MM: came up with a surprisingly close approximation to what the #if 0'ed out code above does.
    perceptualRoughness = perceptualRoughness*(1.7 - 0.7*perceptualRoughness);
#endif


    half mip = perceptualRoughnessToMipmapLevel(perceptualRoughness);
    half3 R = glossIn.reflUVW;
    half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, R, mip);

    return DecodeHDR(rgbm, hdr);
}

// ----------------------------------------------------------------------------
// Include deprecated function
#define INCLUDE_UNITY_IMAGE_BASED_LIGHTING_DEPRECATED
#include "UnityDeprecated.cginc"
#undef INCLUDE_UNITY_IMAGE_BASED_LIGHTING_DEPRECATED

// ----------------------------------------------------------------------------

#endif // UNITY_IMAGE_BASED_LIGHTING_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityImageBasedLighting.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityIndirect.cginc---------------


#ifndef UNITY_INDIRECT_INCLUDED
#define UNITY_INDIRECT_INCLUDED

// Command ID
uint unity_BaseCommandID;
uint GetCommandID(uint svDrawID) { return unity_BaseCommandID + svDrawID; }
#define unity_BaseCommandID Use_GetCommandID_function_instead_of_unity_BaseCommandID


// Non-indexed indirect draw
struct IndirectDrawArgs
{
    uint vertexCountPerInstance;
    uint instanceCount;
    uint startVertex;
    uint startInstance;
};
void GetIndirectDrawArgs(out IndirectDrawArgs args, ByteAddressBuffer argsBuffer, uint commandId)
{
    uint offset = commandId * 16;
    args.vertexCountPerInstance = argsBuffer.Load(offset + 0);
    args.instanceCount = argsBuffer.Load(offset + 4);
    args.startVertex = argsBuffer.Load(offset + 8);
    args.startInstance = argsBuffer.Load(offset + 12);
}
uint GetIndirectInstanceCount(IndirectDrawArgs args) { return args.instanceCount; }
uint GetIndirectVertexCount(IndirectDrawArgs args) { return args.vertexCountPerInstance; }
#if defined(SHADER_API_VULKAN) || defined(SHADER_API_WEBGPU)
uint GetIndirectInstanceID(IndirectDrawArgs args, uint svInstanceID) { return svInstanceID - args.startInstance; }
uint GetIndirectInstanceID_Base(IndirectDrawArgs args, uint svInstanceID) { return svInstanceID; }
#else
uint GetIndirectInstanceID(IndirectDrawArgs args, uint svInstanceID) { return svInstanceID; }
uint GetIndirectInstanceID_Base(IndirectDrawArgs args, uint svInstanceID) { return svInstanceID + args.startInstance; }
#endif
#if defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_WEBGPU)
uint GetIndirectVertexID(IndirectDrawArgs args, uint svVertexID) { return svVertexID - args.startVertex; }
uint GetIndirectVertexID_Base(IndirectDrawArgs args, uint svVertexID) { return svVertexID; }
#else
uint GetIndirectVertexID(IndirectDrawArgs args, uint svVertexID) { return svVertexID; }
uint GetIndirectVertexID_Base(IndirectDrawArgs args, uint svVertexID) { return svVertexID + args.startVertex; }
#endif


// Indexed indirect draw
struct IndirectDrawIndexedArgs
{
    uint indexCountPerInstance;
    uint instanceCount;
    uint startIndex;
    uint baseVertexIndex;
    uint startInstance;
};
void GetIndirectDrawArgs(out IndirectDrawIndexedArgs args, ByteAddressBuffer argsBuffer, uint commandId)
{
    uint offset = commandId * 20;
    args.indexCountPerInstance = argsBuffer.Load(offset + 0);
    args.instanceCount = argsBuffer.Load(offset + 4);
    args.startIndex = argsBuffer.Load(offset + 8);
    args.baseVertexIndex = argsBuffer.Load(offset + 12);
    args.startInstance = argsBuffer.Load(offset + 16);
}
uint GetIndirectInstanceCount(IndirectDrawIndexedArgs args) { return args.instanceCount; }
uint GetIndirectVertexCount(IndirectDrawIndexedArgs args) { return args.indexCountPerInstance; }
#if defined(SHADER_API_VULKAN) || defined(SHADER_API_WEBGPU)
uint GetIndirectInstanceID(IndirectDrawIndexedArgs args, uint svInstanceID) { return svInstanceID - args.startInstance; }
uint GetIndirectInstanceID_Base(IndirectDrawIndexedArgs args, uint svInstanceID) { return svInstanceID; }
#else
uint GetIndirectInstanceID(IndirectDrawIndexedArgs args, uint svInstanceID) { return svInstanceID; }
uint GetIndirectInstanceID_Base(IndirectDrawIndexedArgs args, uint svInstanceID) { return svInstanceID + args.startInstance; }
#endif
uint GetIndirectVertexID(IndirectDrawIndexedArgs args, uint svVertexID) { return svVertexID; }
uint GetIndirectVertexID_Base(IndirectDrawIndexedArgs args, uint svVertexID) { return svVertexID + args.startIndex; }


// Indirect draw ID accessors
#ifdef UNITY_INDIRECT_DRAW_ARGS
ByteAddressBuffer unity_IndirectDrawArgs;
static UNITY_INDIRECT_DRAW_ARGS globalIndirectDrawArgs;

void InitIndirectDrawArgs(uint svDrawID) { GetIndirectDrawArgs(globalIndirectDrawArgs, unity_IndirectDrawArgs, GetCommandID(svDrawID)); }
uint GetIndirectInstanceCount() { return GetIndirectInstanceCount(globalIndirectDrawArgs); }
uint GetIndirectVertexCount() { return GetIndirectVertexCount(globalIndirectDrawArgs); }
uint GetIndirectInstanceID(uint svInstanceID) { return GetIndirectInstanceID(globalIndirectDrawArgs, svInstanceID); }
uint GetIndirectInstanceID_Base(uint svInstanceID) { return GetIndirectInstanceID_Base(globalIndirectDrawArgs, svInstanceID); }
uint GetIndirectVertexID(uint svVertexID) { return GetIndirectVertexID(globalIndirectDrawArgs, svVertexID); }
uint GetIndirectVertexID_Base(uint svVertexID) { return GetIndirectVertexID_Base(globalIndirectDrawArgs, svVertexID); }
#endif

#endif // UNITY_INDIRECT_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityIndirect.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityInstancing.cginc---------------


#ifndef UNITY_INSTANCING_INCLUDED
#define UNITY_INSTANCING_INCLUDED

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
    // We will redefine some built-in shader params e.g. unity_ObjectToWorld and unity_WorldToObject.
    #error "Please include UnityShaderVariables.cginc first."
#endif

#ifndef UNITY_SHADER_UTILITIES_INCLUDED
    // We will redefine some built-in shader functions e.g.UnityObjectToClipPos.
    #error "Please include UnityShaderUtilities.cginc first."
#endif

#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_WEBGPU))
    #define UNITY_SUPPORT_INSTANCING
#endif

#if defined(SHADER_API_SWITCH)
    #define UNITY_SUPPORT_INSTANCING
#endif

#ifdef SHADER_TARGET_SURFACE_ANALYSIS
    // Treat instancing as not supported for surface shader analysis step -- it does not affect what is being read/written by the shader anyway.
    #undef UNITY_SUPPORT_INSTANCING
    #ifdef UNITY_MAX_INSTANCE_COUNT
        #undef UNITY_MAX_INSTANCE_COUNT
    #endif
    #ifdef UNITY_FORCE_MAX_INSTANCE_COUNT
        #undef UNITY_FORCE_MAX_INSTANCE_COUNT
    #endif
    // in analysis pass we force array size to be 1
    #define UNITY_FORCE_MAX_INSTANCE_COUNT 1
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL)
    #define UNITY_SUPPORT_STEREO_INSTANCING
#endif

// These platforms support dynamically adjusting the instancing CB size according to the current batch.
#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) || defined(SHADER_API_WEBGPU)
    #define UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE
#endif

#if defined(SHADER_TARGET_SURFACE_ANALYSIS) && defined(UNITY_SUPPORT_INSTANCING)
    #undef UNITY_SUPPORT_INSTANCING
#endif

////////////////////////////////////////////////////////
// instancing paths
// - UNITY_INSTANCING_ENABLED               Defined if instancing path is taken.
// - UNITY_PROCEDURAL_INSTANCING_ENABLED    Defined if procedural instancing path is taken.
// - UNITY_STEREO_INSTANCING_ENABLED        Defined if stereo instancing path is taken.
#if defined(UNITY_SUPPORT_INSTANCING) && defined(INSTANCING_ON)
    #define UNITY_INSTANCING_ENABLED
#endif
#if defined(UNITY_SUPPORT_INSTANCING) && defined(PROCEDURAL_INSTANCING_ON)
    #define UNITY_PROCEDURAL_INSTANCING_ENABLED
#endif
#if defined(UNITY_SUPPORT_STEREO_INSTANCING) && defined(STEREO_INSTANCING_ON)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

#if defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) || defined(SHADER_API_WEBGPU)
    // These platforms have constant buffers disabled normally, but not here (see CBUFFER_START/CBUFFER_END in HLSLSupport.cginc).
    #define UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(name)  cbuffer name {
    #define UNITY_INSTANCING_CBUFFER_SCOPE_END          }
#else
    #define UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(name)  CBUFFER_START(name)
    #define UNITY_INSTANCING_CBUFFER_SCOPE_END          CBUFFER_END
#endif

////////////////////////////////////////////////////////
// basic instancing setups
// - UNITY_VERTEX_INPUT_INSTANCE_ID     Declare instance ID field in vertex shader input / output struct.
// - UNITY_GET_INSTANCE_ID              (Internal) Get the instance ID from input struct.
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)

    // A global instance ID variable that functions can directly access.
    static uint unity_InstanceID;

    // Don't make UnityDrawCallInfo an actual CB on GL
    #if (!defined(SHADER_API_GLES3) && !defined(SHADER_API_GLCORE)) || defined(SHADER_API_SWITCH)
        UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(UnityDrawCallInfo)
    #endif
            int unity_BaseInstanceID;
            int unity_InstanceCount;
    #if (!defined(SHADER_API_GLES3) && !defined(SHADER_API_GLCORE)) || defined(SHADER_API_SWITCH)
        UNITY_INSTANCING_CBUFFER_SCOPE_END
    #endif

    #ifdef SHADER_API_PSSL
        #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID;
        #define UNITY_GET_INSTANCE_ID(input)    _GETINSTANCEID(input)
    #else
        #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID : SV_InstanceID;
        #define UNITY_GET_INSTANCE_ID(input)    input.instanceID
    #endif

#else
    #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
#endif // UNITY_INSTANCING_ENABLED || UNITY_PROCEDURAL_INSTANCING_ENABLED || UNITY_STEREO_INSTANCING_ENABLED

#if !defined(UNITY_VERTEX_INPUT_INSTANCE_ID)
#   define UNITY_VERTEX_INPUT_INSTANCE_ID DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
#endif

////////////////////////////////////////////////////////
// basic stereo instancing setups
// - UNITY_VERTEX_OUTPUT_STEREO             Declare stereo target eye field in vertex shader output struct.
// - UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO  Assign the stereo target eye.
// - UNITY_TRANSFER_VERTEX_OUTPUT_STEREO    Copy stero target from input struct to output struct. Used in vertex shader.
// - UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
#ifdef UNITY_STEREO_INSTANCING_ENABLED
#if defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          uint stereoTargetEyeIndex : BLENDINDICES0; uint stereoTargetEyeIndexSV : SV_RenderTargetArrayIndex;
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)       output.stereoTargetEyeIndexSV = unity_StereoEyeIndex; output.stereoTargetEyeIndex = unity_StereoEyeIndex;
#else
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)       output.stereoTargetEyeIndex = unity_StereoEyeIndex
#endif
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO_EYE_INDEX                uint stereoTargetEyeIndex : BLENDINDICES0;
    #define DEFAULT_UNITY_INITIALIZE_OUTPUT_STEREO_EYE_INDEX(output)    output.stereoTargetEyeIndex = unity_StereoEyeIndex;
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)  output.stereoTargetEyeIndex = input.stereoTargetEyeIndex;
    #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) unity_StereoEyeIndex = input.stereoTargetEyeIndex;
#elif defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO uint stereoTargetEyeIndex : BLENDINDICES0;
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO_EYE_INDEX uint stereoTargetEyeIndex : BLENDINDICES0;
    #define DEFAULT_UNITY_INITIALIZE_OUTPUT_STEREO_EYE_INDEX(output) output.stereoTargetEyeIndex = unity_StereoEyeIndex;
    // HACK: Workaround for Mali shader compiler issues with directly using GL_ViewID_OVR (GL_OVR_multiview). This array just contains the values 0 and 1.
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output) output.stereoTargetEyeIndex = unity_StereoEyeIndex;
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output) output.stereoTargetEyeIndex = input.stereoTargetEyeIndex;
    #if defined(SHADER_STAGE_VERTEX)
        #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
    #else
        #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) unity_StereoEyeIndex.x = input.stereoTargetEyeIndex;
    #endif
#else
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO_EYE_INDEX
    #define DEFAULT_UNITY_INITIALIZE_OUTPUT_STEREO_EYE_INDEX(output)
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)
    #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
#endif

#if !defined(UNITY_VERTEX_OUTPUT_STEREO_EYE_INDEX)
#   define UNITY_VERTEX_OUTPUT_STEREO_EYE_INDEX                 DEFAULT_UNITY_VERTEX_OUTPUT_STEREO_EYE_INDEX
#endif
#if !defined(UNITY_INITIALIZE_OUTPUT_STEREO_EYE_INDEX)
#   define UNITY_INITIALIZE_OUTPUT_STEREO_EYE_INDEX(output)     DEFAULT_UNITY_INITIALIZE_OUTPUT_STEREO_EYE_INDEX(output)
#endif
#if !defined(UNITY_VERTEX_OUTPUT_STEREO)
#   define UNITY_VERTEX_OUTPUT_STEREO                           DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
#endif
#if !defined(UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO)
#   define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)        DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
#endif
#if !defined(UNITY_TRANSFER_VERTEX_OUTPUT_STEREO)
#   define UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)   DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)
#endif
#if !defined(UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX)
#   define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)      DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
#endif

////////////////////////////////////////////////////////
// - UNITY_SETUP_INSTANCE_ID        Should be used at the very beginning of the vertex shader / fragment shader,
//                                  so that succeeding code can have access to the global unity_InstanceID.
//                                  Also procedural function is called to setup instance data.
// - UNITY_TRANSFER_INSTANCE_ID     Copy instance ID from input struct to output struct. Used in vertex shader.

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
    void UnitySetupInstanceID(uint inputInstanceID)
    {
        #ifdef UNITY_STEREO_INSTANCING_ENABLED
            #if defined(SHADER_API_GLES3)
                // We must calculate the stereo eye index differently for GLES3
                // because otherwise,  the unity shader compiler will emit a bitfieldInsert function.
                // bitfieldInsert requires support for glsl version 400 or later.  Therefore the
                // generated glsl code will fail to compile on lower end devices.  By changing the
                // way we calculate the stereo eye index,  we can help the shader compiler to avoid
                // emitting the bitfieldInsert function and thereby increase the number of devices we
                // can run stereo instancing on.
                unity_StereoEyeIndex = round(fmod(inputInstanceID, 2.0));
                unity_InstanceID = unity_BaseInstanceID + (inputInstanceID >> 1);
            #else
                // stereo eye index is automatically figured out from the instance ID
                unity_StereoEyeIndex = inputInstanceID & 0x01;
                unity_InstanceID = unity_BaseInstanceID + (inputInstanceID >> 1);
            #endif
        #else
            unity_InstanceID = inputInstanceID + unity_BaseInstanceID;
        #endif
    }
    void UnitySetupCompoundMatrices();
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        #ifndef UNITY_INSTANCING_PROCEDURAL_FUNC
            #error "UNITY_INSTANCING_PROCEDURAL_FUNC must be defined."
        #else
            void UNITY_INSTANCING_PROCEDURAL_FUNC(); // forward declaration of the procedural function
            #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)      { UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input)); UNITY_INSTANCING_PROCEDURAL_FUNC(); UnitySetupCompoundMatrices(); }
        #endif
    #else
        #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)          { UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input)); UnitySetupCompoundMatrices(); }
    #endif
    #define UNITY_TRANSFER_INSTANCE_ID(input, output)   output.instanceID = UNITY_GET_INSTANCE_ID(input)
#else
    #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)
    #define UNITY_TRANSFER_INSTANCE_ID(input, output)
#endif

#if !defined(UNITY_SETUP_INSTANCE_ID)
#   define UNITY_SETUP_INSTANCE_ID(input) DEFAULT_UNITY_SETUP_INSTANCE_ID(input)
#endif

////////////////////////////////////////////////////////
// instanced property arrays
#if defined(UNITY_INSTANCING_ENABLED)

    #ifdef UNITY_FORCE_MAX_INSTANCE_COUNT
        #define UNITY_INSTANCED_ARRAY_SIZE  UNITY_FORCE_MAX_INSTANCE_COUNT
    #elif defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE) && !(defined(UNITY_COMPILER_DXC) && defined(SHADER_API_METAL) && defined(SHADER_API_MOBILE))
        #define UNITY_INSTANCED_ARRAY_SIZE  2 // minimum array size that ensures dynamic indexing (does not work on iOS with DXC)
    #elif defined(UNITY_MAX_INSTANCE_COUNT)
        #define UNITY_INSTANCED_ARRAY_SIZE  UNITY_MAX_INSTANCE_COUNT
    #else
        #if (defined(SHADER_API_VULKAN) && defined(SHADER_API_MOBILE)) || defined(SHADER_API_WEBGPU)
            #define UNITY_INSTANCED_ARRAY_SIZE  250
        #else
            #define UNITY_INSTANCED_ARRAY_SIZE  500
        #endif
    #endif

    #define UNITY_INSTANCING_BUFFER_START(buf)      UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(UnityInstancing_##buf) struct  _type_##buf {
    #define UNITY_INSTANCING_BUFFER_END(arr)        } arr##Array[UNITY_INSTANCED_ARRAY_SIZE]; UNITY_INSTANCING_CBUFFER_SCOPE_END
    #define UNITY_DEFINE_INSTANCED_PROP(type, var)  type var;
    #define UNITY_ACCESS_INSTANCED_PROP(arr, var)   arr##Array[unity_InstanceID].var
    #define UNITY_ACCESS_MERGED_INSTANCED_PROP(arr, var)   arr[unity_InstanceID].var

    // Put worldToObject array to a separate CB if UNITY_ASSUME_UNIFORM_SCALING is defined. Most of the time it will not be used.
    #ifdef UNITY_ASSUME_UNIFORM_SCALING
        #define UNITY_WORLDTOOBJECTARRAY_CB 1
    #else
        #define UNITY_WORLDTOOBJECTARRAY_CB 0
    #endif

    #if defined(UNITY_INSTANCED_LOD_FADE) && (defined(LOD_FADE_PERCENTAGE) || defined(LOD_FADE_CROSSFADE))
        #define UNITY_USE_LODFADE_ARRAY
    #endif

    #if defined(UNITY_INSTANCED_RENDERING_LAYER)
        #define UNITY_USE_RENDERINGLAYER_ARRAY
    #endif


    #ifdef UNITY_INSTANCED_LIGHTMAPSTS
        #ifdef LIGHTMAP_ON
            #define UNITY_USE_LIGHTMAPST_ARRAY
        #endif
        #ifdef DYNAMICLIGHTMAP_ON
            #define UNITY_USE_DYNAMICLIGHTMAPST_ARRAY
        #endif
    #endif

    #if defined(UNITY_INSTANCED_SH) && !defined(LIGHTMAP_ON)
        #if UNITY_SHOULD_SAMPLE_SH
            #define UNITY_USE_SHCOEFFS_ARRAYS
        #endif
        #if defined(UNITY_PASS_DEFERRED) && defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
            #define UNITY_USE_PROBESOCCLUSION_ARRAY
        #endif
    #endif

    UNITY_INSTANCING_BUFFER_START(PerDraw0)
        #ifndef UNITY_DONT_INSTANCE_OBJECT_MATRICES
            UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_ObjectToWorldArray)
            #if UNITY_WORLDTOOBJECTARRAY_CB == 0
                UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_WorldToObjectArray)
            #endif
        #endif
        #if defined(UNITY_USE_LODFADE_ARRAY) && defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
            UNITY_DEFINE_INSTANCED_PROP(float2, unity_LODFadeArray)
            // the quantized fade value (unity_LODFade.y) is automatically used for cross-fading instances
            #define unity_LODFade UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, unity_LODFadeArray).xyxx
        #endif
        #if defined(UNITY_USE_RENDERINGLAYER_ARRAY) && defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
            UNITY_DEFINE_INSTANCED_PROP(float, unity_RenderingLayerArray)
            #define unity_RenderingLayer UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, unity_RenderingLayerArray).xxxx
        #endif
    UNITY_INSTANCING_BUFFER_END(unity_Builtins0)

    UNITY_INSTANCING_BUFFER_START(PerDraw1)
        #if !defined(UNITY_DONT_INSTANCE_OBJECT_MATRICES) && UNITY_WORLDTOOBJECTARRAY_CB == 1
            UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_WorldToObjectArray)
        #endif
        #if defined(UNITY_USE_LODFADE_ARRAY) && !defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
            UNITY_DEFINE_INSTANCED_PROP(float2, unity_LODFadeArray)
            // the quantized fade value (unity_LODFade.y) is automatically used for cross-fading instances
            #define unity_LODFade UNITY_ACCESS_INSTANCED_PROP(unity_Builtins1, unity_LODFadeArray).xyxx
        #endif
        #if defined(UNITY_USE_RENDERINGLAYER_ARRAY) && !defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
            UNITY_DEFINE_INSTANCED_PROP(float, unity_RenderingLayerArray)
            #define unity_RenderingLayer UNITY_ACCESS_INSTANCED_PROP(unity_Builtins1, unity_RenderingLayerArray).xxxx
        #endif
    UNITY_INSTANCING_BUFFER_END(unity_Builtins1)

    UNITY_INSTANCING_BUFFER_START(PerDraw2)
        #ifdef UNITY_USE_LIGHTMAPST_ARRAY
            UNITY_DEFINE_INSTANCED_PROP(float4, unity_LightmapSTArray)
            #define unity_LightmapST UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_LightmapSTArray)
        #endif
        #ifdef UNITY_USE_DYNAMICLIGHTMAPST_ARRAY
            UNITY_DEFINE_INSTANCED_PROP(float4, unity_DynamicLightmapSTArray)
            #define unity_DynamicLightmapST UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_DynamicLightmapSTArray)
        #endif
        #ifdef UNITY_USE_SHCOEFFS_ARRAYS
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHArArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHAgArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHAbArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHBrArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHBgArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHBbArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHCArray)
            #define unity_SHAr UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHArArray)
            #define unity_SHAg UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHAgArray)
            #define unity_SHAb UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHAbArray)
            #define unity_SHBr UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHBrArray)
            #define unity_SHBg UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHBgArray)
            #define unity_SHBb UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHBbArray)
            #define unity_SHC  UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHCArray)
        #endif
        #ifdef UNITY_USE_PROBESOCCLUSION_ARRAY
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_ProbesOcclusionArray)
            #define unity_ProbesOcclusion UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_ProbesOcclusionArray)
        #endif
    UNITY_INSTANCING_BUFFER_END(unity_Builtins2)

    UNITY_INSTANCING_BUFFER_START(PerDraw3)
        UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_PrevObjectToWorldArray)
        UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_PrevWorldToObjectArray)
    UNITY_INSTANCING_BUFFER_END(unity_Builtins3)

    #ifndef UNITY_DONT_INSTANCE_OBJECT_MATRICES
        #define unity_ObjectToWorld     UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, unity_ObjectToWorldArray)
        #define MERGE_UNITY_BUILTINS_INDEX(X) unity_Builtins##X##Array
        #define CALL_MERGE(X) MERGE_UNITY_BUILTINS_INDEX(X)
        #define unity_WorldToObject     UNITY_ACCESS_MERGED_INSTANCED_PROP(CALL_MERGE(UNITY_WORLDTOOBJECTARRAY_CB), unity_WorldToObjectArray)

        inline float4 UnityObjectToClipPosODSInstanced(float3 inPos)
        {
            float4 clipPos;
            float3 posWorld = mul(unity_ObjectToWorld, float4(inPos, 1.0)).xyz;
            #if defined(STEREO_CUBEMAP_RENDER_ON)
            float3 offset = ODSOffset(posWorld, unity_HalfStereoSeparation.x);
            clipPos = mul(UNITY_MATRIX_VP, float4(posWorld + offset, 1.0));
            #else
            clipPos = mul(UNITY_MATRIX_VP, float4(posWorld, 1.0));
            #endif
            return clipPos;
        }

        inline float4 UnityObjectToClipPosInstanced(in float3 pos)
        {
            #if defined(STEREO_CUBEMAP_RENDER_ON)
            return UnityObjectToClipPosODSInstanced(pos);
            #else
            // More efficient than computing M*VP matrix product
            return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
            #endif
        }
        inline float4 UnityObjectToClipPosInstanced(float4 pos)
        {
            return UnityObjectToClipPosInstanced(pos.xyz);
        }
        #define UnityObjectToClipPosODS UnityObjectToClipPosODSInstanced
        #define UnityObjectToClipPos UnityObjectToClipPosInstanced
    #endif

#else // UNITY_INSTANCING_ENABLED

    // in procedural mode we don't need cbuffer, and properties are not uniforms
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        #define UNITY_INSTANCING_BUFFER_START(buf)
        #define UNITY_INSTANCING_BUFFER_END(arr)
        #define UNITY_DEFINE_INSTANCED_PROP(type, var)      static type var;
    #else
        #define UNITY_INSTANCING_BUFFER_START(buf)          CBUFFER_START(buf)
        #define UNITY_INSTANCING_BUFFER_END(arr)            CBUFFER_END
        #define UNITY_DEFINE_INSTANCED_PROP(type, var)      type var;
    #endif

    #define UNITY_ACCESS_INSTANCED_PROP(arr, var)           var

#endif // UNITY_INSTANCING_ENABLED

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)

    #ifdef UNITY_DONT_INSTANCE_OBJECT_MATRICES
        void UnitySetupCompoundMatrices() {}
    #else
        // The following matrix evaluations depend on the static var unity_InstanceID & unity_StereoEyeIndex. They need to be initialized after UnitySetupInstanceID.
        static float4x4 unity_MatrixMVP_Instanced;
        static float4x4 unity_MatrixMV_Instanced;
        static float4x4 unity_MatrixTMV_Instanced;
        static float4x4 unity_MatrixITMV_Instanced;
        void UnitySetupCompoundMatrices()
        {
            unity_MatrixMVP_Instanced = mul(unity_MatrixVP, unity_ObjectToWorld);
            unity_MatrixMV_Instanced = mul(unity_MatrixV, unity_ObjectToWorld);
            unity_MatrixTMV_Instanced = transpose(unity_MatrixMV_Instanced);
            unity_MatrixITMV_Instanced = transpose(mul(unity_WorldToObject, unity_MatrixInvV));
        }
        #undef UNITY_MATRIX_MVP
        #undef UNITY_MATRIX_MV
        #undef UNITY_MATRIX_T_MV
        #undef UNITY_MATRIX_IT_MV
        #define UNITY_MATRIX_MVP    unity_MatrixMVP_Instanced
        #define UNITY_MATRIX_MV     unity_MatrixMV_Instanced
        #define UNITY_MATRIX_T_MV   unity_MatrixTMV_Instanced
        #define UNITY_MATRIX_IT_MV  unity_MatrixITMV_Instanced
    #endif // UNITY_DONT_INSTANCE_OBJECT_MATRICES
#endif // UNITY_INSTANCING_ENABLED || UNITY_PROCEDURAL_INSTANCING_ENABLED || UNITY_STEREO_INSTANCING_ENABLED

#endif // UNITY_INSTANCING_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityInstancing.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityLegacyTextureStack.cginc---------------


#ifndef TEXTURESTACK_include
#define TEXTURESTACK_include

#define GRA_HLSL_5 1
#define GRA_ROW_MAJOR 1
#define GRA_TEXTURE_ARRAY_SUPPORT 1
#define GRA_PACK_RESOLVE_OUTPUT 0
#if SHADER_API_PSSL
#define GRA_NO_UNORM 1
#endif
#include "GraniteShaderLib3.cginc"

// Keep in sync with the TextureStack.hlsl in the SRP repo.
// Works with legacy render's macros to declare things like texture samplers
// NOTE: Vt is not supported in the legacy renderer. However certain legacy systems
// need to be partially aware of VT and use this header. (Tests, VT Debug UI)

#if UNITY_VIRTUAL_TEXTURING
#define VIRTUAL_TEXTURES_ACTIVE 1
#else
#define VIRTUAL_TEXTURES_ACTIVE 0
#endif

#if VIRTUAL_TEXTURES_ACTIVE

struct StackInfo
{
    GraniteLookupData lookupData;
    GraniteLODLookupData lookupDataLod;
    float4 resolveOutput;
};

#ifdef TEXTURESTACK_CLAMP
    #define GR_LOOKUP Granite_Lookup_Clamp_Linear
    #define GR_LOOKUP_LOD Granite_Lookup_Clamp
#else
    #define GR_LOOKUP Granite_Lookup_Anisotropic
    #define GR_LOOKUP_LOD Granite_Lookup
#endif

// This can be used by certain resolver implementations to override screen space derivatives
#ifndef RESOLVE_SCALE_OVERRIDE
#define RESOLVE_SCALE_OVERRIDE float2(1,1)
#endif

StructuredBuffer<GraniteTilesetConstantBuffer> _VTTilesetBuffer;
SamplerState _vt_cacheSampler_trilinear_clamp_aniso4;

#define DECLARE_STACK_CB(stackName) \
    float4x4 stackName##_spaceparams[2];\
    float4 stackName##_atlasparams[2];\

#define DECLARE_STACK_BASE(stackName) \
UNITY_DECLARE_TEX2D(stackName##_transtab);\
\
GraniteTilesetConstantBuffer GetConstantBuffer_##stackName() \
{ \
    int idx = (int)stackName##_atlasparams[1].w; \
    GraniteTilesetConstantBuffer graniteParamBlock; \
    graniteParamBlock = _VTTilesetBuffer[idx]; \
    \
    /* hack resolve scale into constant buffer here */\
    graniteParamBlock.data[0][2][0] *= RESOLVE_SCALE_OVERRIDE.x; \
    graniteParamBlock.data[0][3][0] *= RESOLVE_SCALE_OVERRIDE.y; \
    \
    return graniteParamBlock; \
} \
StackInfo PrepareVT_##stackName(float2 uv)\
    {\
    GraniteStreamingTextureConstantBuffer textureParamBlock;\
    textureParamBlock.data[0] = stackName##_atlasparams[0];\
    textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
    GraniteTilesetConstantBuffer graniteParamBlock = GetConstantBuffer_##stackName(); \
\
    GraniteConstantBuffers grCB;\
    grCB.tilesetBuffer = graniteParamBlock;\
    grCB.streamingTextureBuffer = textureParamBlock;\
\
    GraniteTranslationTexture translationTable;\
    translationTable.Texture = stackName##_transtab;\
    translationTable.Sampler = sampler##stackName##_transtab;\
\
    StackInfo info;\
    GR_LOOKUP(grCB, translationTable, uv, info.lookupData, info.resolveOutput);\
    return info;\
} \
StackInfo PrepareVTLod_##stackName(float2 uv, float mip) \
{ \
    GraniteStreamingTextureConstantBuffer textureParamBlock;\
    textureParamBlock.data[0] = stackName##_atlasparams[0];\
    textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
GraniteTilesetConstantBuffer graniteParamBlock = GetConstantBuffer_##stackName(); \
\
    GraniteConstantBuffers grCB;\
    grCB.tilesetBuffer = graniteParamBlock;\
    grCB.streamingTextureBuffer = textureParamBlock;\
\
    GraniteTranslationTexture translationTable;\
    translationTable.Texture = stackName##_transtab;\
    translationTable.Sampler = sampler##stackName##_transtab;\
\
    StackInfo info;\
    GR_LOOKUP_LOD(grCB, translationTable, uv, mip, info.lookupDataLod, info.resolveOutput);\
    return info;\
}
#define jj2(a, b) a##b
#define jj(a, b) jj2(a, b)

#define DECLARE_STACK_LAYER(stackName, layerSamplerName, layerIndex) \
UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(stackName##_c##layerIndex);\
\
float4 SampleVT_##layerSamplerName(StackInfo info)\
{\
    GraniteStreamingTextureConstantBuffer textureParamBlock;\
    textureParamBlock.data[0] = stackName##_atlasparams[0];\
    textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
    GraniteTilesetConstantBuffer graniteParamBlock = GetConstantBuffer_##stackName(); \
\
    GraniteConstantBuffers grCB;\
    grCB.tilesetBuffer = graniteParamBlock;\
    grCB.streamingTextureBuffer = textureParamBlock;\
\
    GraniteCacheTexture cache;\
    cache.TextureArray = stackName##_c##layerIndex;\
    cache.Sampler = _vt_cacheSampler_trilinear_clamp_aniso4;\
\
    float4 output;\
    Granite_Sample_HQ(grCB, info.lookupData, cache, layerIndex, output);\
    return output;\
} \
float3 SampleVT_Normal_##layerSamplerName(StackInfo info, float scale)\
{\
    return Granite_UnpackNormal( jj(SampleVT_,layerSamplerName)( info ), scale ); \
} \
float4 SampleVTLod_##layerSamplerName(StackInfo info)\
{\
    GraniteStreamingTextureConstantBuffer textureParamBlock;\
    textureParamBlock.data[0] = stackName##_atlasparams[0];\
    textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
    GraniteTilesetConstantBuffer graniteParamBlock = GetConstantBuffer_##stackName(); \
\
    GraniteConstantBuffers grCB;\
    grCB.tilesetBuffer = graniteParamBlock;\
    grCB.streamingTextureBuffer = textureParamBlock;\
\
    GraniteCacheTexture cache;\
    cache.TextureArray = stackName##_c##layerIndex;\
    cache.Sampler = _vt_cacheSampler_trilinear_clamp_aniso4;\
\
    float4 output;\
    Granite_Sample(grCB, info.lookupDataLod, cache, layerIndex, output);\
    return output;\
} \
float3 SampleVTLod_Normal_##layerSamplerName(StackInfo info, float scale)\
{\
    return Granite_UnpackNormal( jj(SampleVTLod_,layerSamplerName)( info ), scale ); \
}

#define DECLARE_STACK_RESOLVE(stackName)\
float4 ResolveVT_##stackName(float2 uv)\
{\
    GraniteStreamingTextureConstantBuffer textureParamBlock;\
    textureParamBlock.data[0] = stackName##_atlasparams[0];\
    textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
    GraniteTilesetConstantBuffer graniteParamBlock = GetConstantBuffer_##stackName(); \
\
    GraniteConstantBuffers grCB;\
    grCB.tilesetBuffer = graniteParamBlock;\
    grCB.streamingTextureBuffer = textureParamBlock;\
\
    return Granite_ResolverPixel_Anisotropic(grCB, uv);\
}

#define DECLARE_STACK(stackName, layer0SamplerName)\
    DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_RESOLVE(stackName)\
    DECLARE_STACK_LAYER(stackName, layer0SamplerName,0)

#define DECLARE_STACK2(stackName, layer0SamplerName, layer1SamplerName)\
    DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_RESOLVE(stackName)\
    DECLARE_STACK_LAYER(stackName, layer0SamplerName,0)\
    DECLARE_STACK_LAYER(stackName, layer1SamplerName,1)

#define DECLARE_STACK3(stackName, layer0SamplerName, layer1SamplerName, layer2SamplerName)\
    DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_RESOLVE(stackName)\
    DECLARE_STACK_LAYER(stackName, layer0SamplerName,0)\
    DECLARE_STACK_LAYER(stackName, layer1SamplerName,1)\
    DECLARE_STACK_LAYER(stackName, layer2SamplerName,2)

#define DECLARE_STACK4(stackName, layer0SamplerName, layer1SamplerName, layer2SamplerName, layer3SamplerName)\
    DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_RESOLVE(stackName)\
    DECLARE_STACK_LAYER(stackName, layer0SamplerName,0)\
    DECLARE_STACK_LAYER(stackName, layer1SamplerName,1)\
    DECLARE_STACK_LAYER(stackName, layer2SamplerName,2)\
    DECLARE_STACK_LAYER(stackName, layer3SamplerName,3)

#define PrepareStack(uv, stackName) PrepareVT_##stackName(uv)
#define PrepareStackLod(uv, stackName, mip) PrepareVTLod_##stackName(uv, mip)
#define SampleStack(info, textureName) SampleVT_##textureName(info)
#define SampleStackLod(info, textureName) SampleVTLod_##textureName(info)
#define SampleStackNormal(info, textureName, scale) (SampleVT_Normal_##textureName(info, scale)).xyz
#define SampleStackLodNormal(info, textureName, scale) SampleVTLod_Normal_##textureName(info, scale)
#define GetResolveOutput(info) info.resolveOutput
#define PackResolveOutput(output) Granite_PackTileId(output)
#define ResolveStack(uv, stackName) ResolveVT_##stackName(uv)

float4 GetPackedVTFeedback(float4 feedback)
{
    return Granite_PackTileId(feedback);
}

#else

// Stacks amount to nothing when VT is off
#define DECLARE_STACK(stackName, layer0)
#define DECLARE_STACK2(stackName, layer0, layer1)
#define DECLARE_STACK3(stackName, layer0, layer1, layer2)
#define DECLARE_STACK4(stackName, layer0, layer1, layer2, layer3)
#define DECLARE_STACK_CB(stackName)

// Info is just the uv's
// We could do a straight #defube StackInfo float2 but this makes it a bit more type safe
// and allows us to do things like function overloads,...
struct StackInfo
{
    float2 uv;
    float lod;
};

StackInfo MakeStackInfo(float2 uv)
{
    StackInfo result;
    result.uv = uv;
    return result;
}
StackInfo MakeStackInfoLod(float2 uv, float lod)
{
    StackInfo result;
    result.uv = uv;
    result.lod = lod;
    return result;
}

// Prepare just passes the texture coord around
#define PrepareStack(uv, stackName) MakeStackInfo(uv)
#define PrepareStackLod(uv, stackName, mip) MakeStackInfoLod(uv, mip)

// Sample just samples the texture
#define SampleStack(info, texture) UNITY_SAMPLE_TEX2D_SAMPLER(texture, texture, info.uv)
#define SampleStackNormal(info, texture, scale) UnpackNormalWithScale(UNITY_SAMPLE_TEX2D_SAMPLER(texture, texture, info.uv), scale)

#define SampleStackLod(info, texture) texture.SampleLevel(sampler##texture, info.uv, info.lod)
#define SampleStackLodNormal(info, texture, scale) UnpackNormalWithScale(texture.SampleLevel(sampler##texture, info.uv, info.lod), scale)

// Resolve does nothing
#define GetResolveOutput(info) float4(1,1,1,1)
#define ResolveStack(uv, stackName) float4(1,1,1,1)
#define PackResolveOutput(output) output

#endif

#endif //TEXTURESTACK_include


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityLegacyTextureStack.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityLightingCommon.cginc---------------


#ifndef UNITY_LIGHTING_COMMON_INCLUDED
#define UNITY_LIGHTING_COMMON_INCLUDED

fixed4 _LightColor0;
fixed4 _SpecColor;

struct UnityLight
{
    half3 color;
    half3 dir;
    half  ndotl; // Deprecated: Ndotl is now calculated on the fly and is no longer stored. Do not used it.
};

struct UnityIndirect
{
    half3 diffuse;
    half3 specular;
};

struct UnityGI
{
    UnityLight light;
    UnityIndirect indirect;
};

struct UnityGIInput
{
    UnityLight light; // pixel light, sent from the engine

    float3 worldPos;
    half3 worldViewDir;
    half atten;
    half3 ambient;

    // interpolated lightmap UVs are passed as full float precision data to fragment shaders
    // so lightmapUV (which is used as a tmp inside of lightmap fragment shaders) should
    // also be full float precision to avoid data loss before sampling a texture.
    float4 lightmapUV; // .xy = static lightmap UV, .zw = dynamic lightmap UV

    #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION) || defined(UNITY_ENABLE_REFLECTION_BUFFERS)
    float4 boxMin[2];
    #endif
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
    float4 boxMax[2];
    float4 probePosition[2];
    #endif
    // HDR cubemap properties, use to decompress HDR texture
    float4 probeHDR[2];
};

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityLightingCommon.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityMetaPass.cginc---------------


#ifndef UNITY_META_PASS_INCLUDED
#define UNITY_META_PASS_INCLUDED


CBUFFER_START(UnityMetaPass)
    // x = use uv1 as raster position
    // y = use uv2 as raster position
    bool4 unity_MetaVertexControl;

    // x = return albedo
    // y = return normal
    bool4 unity_MetaFragmentControl;

    // Control which VisualizationMode we will
    // display in the editor
    int unity_VisualizationMode;
CBUFFER_END


struct UnityMetaInput
{
    half3 Albedo;
    half3 Emission;
    half3 SpecularColor;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV;
    float4 LightCoord;
#endif
};

#ifdef EDITOR_VISUALIZATION

//Visualization defines
// Should be kept in sync with the EditorVisualizationMode enum in EditorCameraDrawing.cpp
#define EDITORVIZ_PBR_VALIDATION_ALBEDO         0
#define EDITORVIZ_PBR_VALIDATION_METALSPECULAR  1
#define EDITORVIZ_TEXTURE                       2
#define EDITORVIZ_SHOWLIGHTMASK                 3
// Old names...
#define PBR_VALIDATION_ALBEDO EDITORVIZ_PBR_VALIDATION_ALBEDO
#define PBR_VALIDATION_METALSPECULAR EDITORVIZ_PBR_VALIDATION_METALSPECULAR

uniform int _CheckPureMetal = 0;// flag to check only full metal, not partial metal, known because it has metallic features and pure black albedo
uniform int _CheckAlbedo = 0; // if 0, pass through untouched color
uniform half4 _AlbedoCompareColor = half4(0.0, 0.0, 0.0, 0.0);
uniform half _AlbedoMinLuminance = 0.0;
uniform half _AlbedoMaxLuminance = 1.0;
uniform half _AlbedoHueTolerance = 0.1;
uniform half _AlbedoSaturationTolerance = 0.1;

uniform sampler2D unity_EditorViz_Texture;
uniform half4 unity_EditorViz_Texture_ST;
uniform int unity_EditorViz_UVIndex;
uniform half4 unity_EditorViz_Decode_HDR;
uniform bool unity_EditorViz_ConvertToLinearSpace;
uniform half4 unity_EditorViz_ColorMul;
uniform half4 unity_EditorViz_ColorAdd;
uniform half unity_EditorViz_Exposure;
uniform sampler2D unity_EditorViz_LightTexture;
uniform sampler2D unity_EditorViz_LightTextureB;
#define unity_EditorViz_ChannelSelect unity_EditorViz_ColorMul
#define unity_EditorViz_Color         unity_EditorViz_ColorAdd
#define unity_EditorViz_LightType     unity_EditorViz_UVIndex
uniform float4x4 unity_EditorViz_WorldToLight;

uniform half4 unity_MaterialValidateLowColor = half4(1.0f, 0.0f, 0.0f, 0.0f);
uniform half4 unity_MaterialValidateHighColor = half4(0.0f, 0.0f, 1.0f, 0.0f);
uniform half4 unity_MaterialValidatePureMetalColor = half4(1.0f, 1.0f, 0.0f, 0.0f);

// Define bounds value in linear RGB for fresnel0 values
static const float dieletricMin = 0.02;
static const float dieletricMax = 0.07;
static const float gemsMin      = 0.07;
static const float gemsMax      = 0.22;
static const float conductorMin = 0.45;
static const float conductorMax = 1.00;
static const float albedoMin    = 0.012;
static const float albedoMax    = 0.9;

half3 UnityMeta_RGBToHSVHelper(float offset, half dominantColor, half colorone, half colortwo)
{
    half H, S, V;
    V = dominantColor;

    if (V != 0.0)
    {
        half small = 0.0;
        if (colorone > colortwo)
            small = colortwo;
        else
            small = colorone;

        half diff = V - small;

        if (diff != 0)
        {
            S = diff / V;
            H = offset + ((colorone - colortwo)/diff);
        }
        else
        {
            S = 0;
            H = offset + (colorone - colortwo);
        }

        H /= 6.0;

        if (H < 6.0)
        {
            H += 1.0;
        }
    }
    else
    {
        S = 0;
        H = 0;
    }
    return half3(H, S, V);
}

half3 UnityMeta_RGBToHSV(half3 rgbColor)
{
    // when blue is highest valued
    if((rgbColor.b > rgbColor.g) && (rgbColor.b > rgbColor.r))
        return UnityMeta_RGBToHSVHelper(4.0, rgbColor.b, rgbColor.r, rgbColor.g);
    //when green is highest valued
    else if(rgbColor.g > rgbColor.r)
        return UnityMeta_RGBToHSVHelper(2.0, rgbColor.g, rgbColor.b, rgbColor.r);
    //when red is highest valued
    else
        return UnityMeta_RGBToHSVHelper(0.0, rgbColor.r, rgbColor.g, rgbColor.b);
}

// Pass 0 - Albedo
half4 UnityMeta_pbrAlbedo(UnityMetaInput IN)
{
    half3 SpecularColor = IN.SpecularColor;
    half3 baseColor = IN.Albedo;

    if (IsGammaSpace())
    {
        baseColor = half3( GammaToLinearSpaceExact(baseColor.x), GammaToLinearSpaceExact(baseColor.y), GammaToLinearSpaceExact(baseColor.z) ); //GammaToLinearSpace(baseColor);
        SpecularColor = GammaToLinearSpace(SpecularColor);
    }

    half3 unTouched = LinearRgbToLuminance(baseColor).xxx; // if no errors, leave color as it was in render

    bool isMetal = dot(SpecularColor, float3(0.3333,0.3333,0.3333)) >= conductorMin;
    // When checking full range we do not take the luminance but the mean because often in game blue color are highlight as too low whereas this is what we are looking for.
    half value = _CheckAlbedo ? LinearRgbToLuminance(baseColor) : dot(baseColor, half3(0.3333, 0.3333, 0.3333));

     // Check if we are pure metal with black albedo
    if (_CheckPureMetal && isMetal && value != 0.0)
        return unity_MaterialValidatePureMetalColor;

    if (_CheckAlbedo == 0)
    {
        // If we have a metallic object, don't complain about low albedo
        if (!isMetal && value < albedoMin)
        {
            return unity_MaterialValidateLowColor;
        }
        else if (value > albedoMax)
        {
            return unity_MaterialValidateHighColor;
        }
        else
        {
            return half4(unTouched, 0);
        }
    }
    else
    {
        if (_AlbedoMinLuminance > value)
        {
             return unity_MaterialValidateLowColor;
        }
        else if (_AlbedoMaxLuminance < value)
        {
             return unity_MaterialValidateHighColor;
        }
        else
        {
            half3 hsv = UnityMeta_RGBToHSV(IN.Albedo);
            half hue = hsv.r;
            half sat = hsv.g;

            half3 compHSV = UnityMeta_RGBToHSV(_AlbedoCompareColor.rgb);
            half compHue = compHSV.r;
            half compSat = compHSV.g;

            if ((compSat - _AlbedoSaturationTolerance > sat) || ((compHue - _AlbedoHueTolerance > hue) && (compHue - _AlbedoHueTolerance + 1.0 > hue)))
            {
                return unity_MaterialValidateLowColor;
            }
            else if ((sat > compSat + _AlbedoSaturationTolerance) || ((hue > compHue + _AlbedoHueTolerance) && (hue > compHue + _AlbedoHueTolerance - 1.0)))
            {
                return unity_MaterialValidateHighColor;
            }
            else
            {
                return half4(unTouched, 0);
            }
        }
    }

    return half4(1.0, 0, 0, 1);
}

// Pass 1 - Metal Specular
half4 UnityMeta_pbrMetalspec(UnityMetaInput IN)
{
    half3 SpecularColor = IN.SpecularColor;
    half4 baseColor = half4(IN.Albedo, 0);

    if (IsGammaSpace())
    {
        baseColor.xyz = GammaToLinearSpace(baseColor.xyz);
        SpecularColor = GammaToLinearSpace(SpecularColor);
    }

    // Take the mean of three channel, works ok.
    half value = dot(SpecularColor, half3(0.3333,0.3333,0.3333));
    bool isMetal = value >= conductorMin;

    half4 outColor = half4(LinearRgbToLuminance(baseColor.xyz).xxx, 1.0f);

    if (value < conductorMin)
    {
         outColor = unity_MaterialValidateLowColor;
    }
    else if (value > conductorMax)
    {
        outColor = unity_MaterialValidateHighColor;
    }
    else if (isMetal)
    {
         // If we are here we supposed the users want to have a metal, so check if we have a pure metal (black albedo) or not
        // if it is not a pure metal, highlight it
        if (_CheckPureMetal)
            outColor = dot(baseColor.xyz, half3(1,1,1)) == 0 ? outColor : unity_MaterialValidatePureMetalColor;
    }

    return outColor;
}

#endif // EDITOR_VISUALIZATION

float2 UnityMetaVizUV(int uvIndex, float2 uv0, float2 uv1, float2 uv2, float4 st)
{
    if (uvIndex == 0)
        return uv0 * st.xy + st.zw;
    else if (uvIndex == 1)
        return uv1 * st.xy + st.zw;
    else
        return uv2 * st.xy + st.zw;
}

float4 UnityMetaVertexPosition(float4 vertex, float2 uv1, float2 uv2, float4 lightmapST, float4 dynlightmapST)
{
#if !defined(EDITOR_VISUALIZATION)
    if (unity_MetaVertexControl.x)
    {
        vertex.xy = uv1 * lightmapST.xy + lightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        vertex.z = vertex.z > 0 ? 1.0e-4f : 0.0f;
    }
    if (unity_MetaVertexControl.y)
    {
        vertex.xy = uv2 * dynlightmapST.xy + dynlightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        vertex.z = vertex.z > 0 ? 1.0e-4f : 0.0f;
    }
    return mul(UNITY_MATRIX_VP, float4(vertex.xyz, 1.0));
#else
    return UnityObjectToClipPos(vertex);
#endif
}

float unity_OneOverOutputBoost;
float unity_MaxOutputValue;
float unity_UseLinearSpace;

half4 UnityMetaFragment (UnityMetaInput IN)
{
    half4 res = 0;
#if !defined(EDITOR_VISUALIZATION)
    if (unity_MetaFragmentControl.x)
    {
        res = half4(IN.Albedo,1);

        // Apply Albedo Boost from LightmapSettings.
        res.rgb = clamp(pow(res.rgb, saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
    }
    if (unity_MetaFragmentControl.y)
    {
        half3 emission;
        if (unity_UseLinearSpace)
            emission = IN.Emission;
        else
            emission = GammaToLinearSpace(IN.Emission);

        res = half4(emission, 1.0);
    }
#else
    if ( unity_VisualizationMode == EDITORVIZ_PBR_VALIDATION_ALBEDO)
    {
        res = UnityMeta_pbrAlbedo(IN);
    }
    else if (unity_VisualizationMode == EDITORVIZ_PBR_VALIDATION_METALSPECULAR)
    {
        res = UnityMeta_pbrMetalspec(IN);
    }
    else if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
    {
        res = tex2D(unity_EditorViz_Texture, IN.VizUV);

        if (unity_EditorViz_Decode_HDR.x > 0)
            res = half4(DecodeHDR(res, unity_EditorViz_Decode_HDR), 1);

        if (unity_EditorViz_ConvertToLinearSpace)
            res.rgb = LinearToGammaSpace(res.rgb);

        res *= unity_EditorViz_ColorMul;
        res += unity_EditorViz_ColorAdd;
        res *= exp2(unity_EditorViz_Exposure);
    }
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        float result = dot(unity_EditorViz_ChannelSelect, tex2D(unity_EditorViz_Texture, IN.VizUV).rgba);
        if (result == 0)
            discard;

        float atten = 1;
        if (unity_EditorViz_LightType == 0)
        {
            // directional:  no attenuation
        }
        else if (unity_EditorViz_LightType == 1)
        {
            // point
            atten = tex2D(unity_EditorViz_LightTexture, dot(IN.LightCoord.xyz, IN.LightCoord.xyz).xx).r;
        }
        else if (unity_EditorViz_LightType == 2)
        {
            // spot
            atten = tex2D(unity_EditorViz_LightTexture, dot(IN.LightCoord.xyz, IN.LightCoord.xyz).xx).r;
            float cookie = tex2D(unity_EditorViz_LightTextureB, IN.LightCoord.xy / IN.LightCoord.w + 0.5).w;
            atten *= (IN.LightCoord.z > 0) * cookie;
        }
        clip(atten - 0.001f);

        res = float4(unity_EditorViz_Color.xyz * result, unity_EditorViz_Color.w);
    }
#endif // EDITOR_VISUALIZATION
    return res;
}

#endif // UNITY_META_PASS_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityMetaPass.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityPBSLighting.cginc---------------


#ifndef UNITY_PBS_LIGHTING_INCLUDED
#define UNITY_PBS_LIGHTING_INCLUDED

#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityLightingCommon.cginc"
#include "UnityGBuffer.cginc"
#include "UnityGlobalIllumination.cginc"

//-------------------------------------------------------------------------------------
// Default BRDF to use:
#if !defined (UNITY_BRDF_PBS) // allow to explicitly override BRDF in custom shader
    // still add safe net for low shader models, otherwise we might end up with shaders failing to compile
    #if SHADER_TARGET < 30 || defined(SHADER_TARGET_SURFACE_ANALYSIS) // only need "something" for surface shader analysis pass; pick the cheap one
        #define UNITY_BRDF_PBS BRDF3_Unity_PBS
    #elif defined(UNITY_PBS_USE_BRDF3)
        #define UNITY_BRDF_PBS BRDF3_Unity_PBS
    #elif defined(UNITY_PBS_USE_BRDF2)
        #define UNITY_BRDF_PBS BRDF2_Unity_PBS
    #elif defined(UNITY_PBS_USE_BRDF1)
        #define UNITY_BRDF_PBS BRDF1_Unity_PBS
    #else
        #error something broke in auto-choosing BRDF
    #endif
#endif

//-------------------------------------------------------------------------------------
// little helpers for GI calculation
// CAUTION: This is deprecated and not use in Untiy shader code, but some asset store plugin still use it, so let here for compatibility

#if !defined (UNITY_BRDF_GI)
    #define UNITY_BRDF_GI BRDF_Unity_Indirect
#endif

inline half3 BRDF_Unity_Indirect (half3 baseColor, half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, half occlusion, UnityGI gi)
{
    return half3(0,0,0);
}

#define UNITY_GLOSSY_ENV_FROM_SURFACE(x, s, data)               \
    Unity_GlossyEnvironmentData g;                              \
    g.roughness /* perceptualRoughness */   = SmoothnessToPerceptualRoughness(s.Smoothness); \
    g.reflUVW = reflect(-data.worldViewDir, s.Normal);  \


#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
    #define UNITY_GI(x, s, data) x = UnityGlobalIllumination (data, s.Occlusion, s.Normal);
#else
    #define UNITY_GI(x, s, data)                                \
        UNITY_GLOSSY_ENV_FROM_SURFACE(g, s, data);              \
        x = UnityGlobalIllumination (data, s.Occlusion, s.Normal, g);
#endif

// Surface shader output structure to be used with physically
// based shading model.

//-------------------------------------------------------------------------------------
// Metallic workflow

struct SurfaceOutputStandard
{
    fixed3 Albedo;      // base (diffuse or specular) color
    float3 Normal;      // tangent space normal, if written
    half3 Emission;
    half Metallic;      // 0=non-metal, 1=metal
    // Smoothness is the user facing name, it should be perceptual smoothness but user should not have to deal with it.
    // Everywhere in the code you meet smoothness it is perceptual smoothness
    half Smoothness;    // 0=rough, 1=smooth
    half Occlusion;     // occlusion (default 1)
    fixed Alpha;        // alpha for transparencies
};

inline half4 LightingStandard (SurfaceOutputStandard s, float3 viewDir, UnityGI gi)
{
    s.Normal = normalize(s.Normal);

    half oneMinusReflectivity;
    half3 specColor;
    s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    // shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
    half outputAlpha;
    s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

    half4 c = UNITY_BRDF_PBS (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
    c.a = outputAlpha;
    return c;
}

inline half4 LightingStandard_Deferred (SurfaceOutputStandard s, float3 viewDir, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    half oneMinusReflectivity;
    half3 specColor;
    s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    half4 c = UNITY_BRDF_PBS (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);

    UnityStandardData data;
    data.diffuseColor   = s.Albedo;
    data.occlusion      = s.Occlusion;
    data.specularColor  = specColor;
    data.smoothness     = s.Smoothness;
    data.normalWorld    = s.Normal;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    half4 emission = half4(s.Emission + c.rgb, 1);
    return emission;
}

inline void LightingStandard_GI (
    SurfaceOutputStandard s,
    UnityGIInput data,
    inout UnityGI gi)
{
#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
    gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
#else
    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.Smoothness, data.worldViewDir, s.Normal, lerp(unity_ColorSpaceDielectricSpec.rgb, s.Albedo, s.Metallic));
    gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal, g);
#endif
}

//-------------------------------------------------------------------------------------
// Specular workflow

struct SurfaceOutputStandardSpecular
{
    fixed3 Albedo;      // diffuse color
    fixed3 Specular;    // specular color
    float3 Normal;      // tangent space normal, if written
    half3 Emission;
    half Smoothness;    // 0=rough, 1=smooth
    half Occlusion;     // occlusion (default 1)
    fixed Alpha;        // alpha for transparencies
};

inline half4 LightingStandardSpecular (SurfaceOutputStandardSpecular s, float3 viewDir, UnityGI gi)
{
    s.Normal = normalize(s.Normal);

    // energy conservation
    half oneMinusReflectivity;
    s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, /*out*/ oneMinusReflectivity);

    // shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
    half outputAlpha;
    s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

    half4 c = UNITY_BRDF_PBS (s.Albedo, s.Specular, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
    c.a = outputAlpha;
    return c;
}

inline half4 LightingStandardSpecular_Deferred (SurfaceOutputStandardSpecular s, float3 viewDir, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    // energy conservation
    half oneMinusReflectivity;
    s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, /*out*/ oneMinusReflectivity);

    half4 c = UNITY_BRDF_PBS (s.Albedo, s.Specular, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);

    UnityStandardData data;
    data.diffuseColor   = s.Albedo;
    data.occlusion      = s.Occlusion;
    data.specularColor  = s.Specular;
    data.smoothness     = s.Smoothness;
    data.normalWorld    = s.Normal;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    half4 emission = half4(s.Emission + c.rgb, 1);
    return emission;
}

inline void LightingStandardSpecular_GI (
    SurfaceOutputStandardSpecular s,
    UnityGIInput data,
    inout UnityGI gi)
{
#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
    gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
#else
    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.Smoothness, data.worldViewDir, s.Normal, s.Specular);
    gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal, g);
#endif
}

#endif // UNITY_PBS_LIGHTING_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityPBSLighting.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityRayQuery.cginc---------------


#ifndef UNITY_RAY_QUERY_INCLUDED
#define UNITY_RAY_QUERY_INCLUDED

// Does the platform provide its own definition of UnityRayQuery
#ifndef PLATFORM_RAYQUERY
#define PLATFORM_RAYQUERY

#define UnityRayQuery RayQuery

#endif

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityRayQuery.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityRayTracingMeshUtils.cginc---------------


#ifndef UNITY_RAY_TRACING_MESH_UTILS_INCLUDED
#define UNITY_RAY_TRACING_MESH_UTILS_INCLUDED

// This helper file contains a list of utility functions needed to fetch vertex attributes from within closesthit or anyhit shaders.

// HLSL example:
// struct Vertex
// {
//     float3 position;
//     float2 texcoord;
// };

// Vertex FetchVertex(uint vertexIndex)
// {
//      Vertex v;
//      v.position = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
//      v.texcoord = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
//      return v;
// }

// uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
// Vertex v0, v1, v2;
// v0 = FetchVertex(triangleIndices.x);
// v1 = FetchVertex(triangleIndices.y);
// v2 = FetchVertex(triangleIndices.z);
// Interpolate the vertices using the barycentric coordinates available as input to the closesthit or anyhit shaders.

#define kMaxVertexStreams 8

struct MeshInfo
{
    uint vertexSize[kMaxVertexStreams];                 // The stride between 2 consecutive vertices in the vertex buffer. There is an entry for each vertex stream.
    uint baseVertex;                                    // A value added to each index before reading a vertex from the vertex buffer.
    uint vertexStart;
    uint indexSize;                                     // 0 when an index buffer is not used, 2 for 16-bit indices or 4 for 32-bit indices.
    uint indexStart;                                    // The location of the first index to read from the index buffer.
};

struct VertexAttributeInfo
{
    uint Stream;                                        // The stream index used to fetch the vertex attribute. There can be up to kMaxVertexStreams streams.
    uint Format;                                        // One of the kVertexFormat* values from bellow.
    uint ByteOffset;                                    // The attribute offset in bytes into the vertex structure.
    uint Dimension;                                     // The dimension (#channels) of the vertex attribute.
};

// Valid values for the attributeType parameter in UnityRayTracingFetchVertexAttribute* functions.
#define kVertexAttributePosition    0
#define kVertexAttributeNormal      1
#define kVertexAttributeTangent     2
#define kVertexAttributeColor       3
#define kVertexAttributeTexCoord0   4
#define kVertexAttributeTexCoord1   5
#define kVertexAttributeTexCoord2   6
#define kVertexAttributeTexCoord3   7
#define kVertexAttributeTexCoord4   8
#define kVertexAttributeTexCoord5   9
#define kVertexAttributeTexCoord6   10
#define kVertexAttributeTexCoord7   11
#define kVertexAttributeCount       12

static float4 unity_DefaultVertexAttributes[kVertexAttributeCount] =
{
    float4(0, 0, 0, 0),     // kVertexAttributePosition - always present in ray tracing.
    float4(0, 0, 1, 0),     // kVertexAttributeNormal
    float4(1, 0, 0, 1),     // kVertexAttributeTangent
    float4(1, 1, 1, 1),     // kVertexAttributeColor
    float4(0, 0, 0, 0),     // kVertexAttributeTexCoord0
    float4(0, 0, 0, 0),     // kVertexAttributeTexCoord1
    float4(0, 0, 0, 0),     // kVertexAttributeTexCoord2
    float4(0, 0, 0, 0),     // kVertexAttributeTexCoord3
    float4(0, 0, 0, 0),     // kVertexAttributeTexCoord4
    float4(0, 0, 0, 0),     // kVertexAttributeTexCoord5
    float4(0, 0, 0, 0),     // kVertexAttributeTexCoord6
    float4(0, 0, 0, 0),     // kVertexAttributeTexCoord7
};

// Supported
#define kVertexFormatFloat          0
#define kVertexFormatFloat16        1
#define kVertexFormatUNorm8         2
#define kVertexFormatUNorm16        4
#define kVertexFormatSNorm16        5
// Not supported
#define kVertexFormatSNorm8         3
#define kVertexFormatUInt8          6
#define kVertexFormatSInt8          7
#define kVertexFormatUInt16         8
#define kVertexFormatSInt16         9
#define kVertexFormatUInt32         10
#define kVertexFormatSInt32         11

StructuredBuffer<MeshInfo>              unity_MeshInfo_RT;
StructuredBuffer<VertexAttributeInfo>   unity_MeshVertexDeclaration_RT;
#if defined(SHADER_API_PS5)
Buffer<ByteAddressBuffer> unity_MeshVertexBuffers_RT;
#else
ByteAddressBuffer unity_MeshVertexBuffers_RT[kMaxVertexStreams];
#endif
ByteAddressBuffer unity_MeshIndexBuffer_RT;

static float4 unity_VertexChannelMask_RT[5] =
{
    float4(0, 0, 0, 0),
    float4(1, 0, 0, 0),
    float4(1, 1, 0, 0),
    float4(1, 1, 1, 0),
    float4(1, 1, 1, 1)
};

// A normalized short (16-bit signed integer) is encode into data. Returns a float in the range [-1, 1].
float DecodeSNorm16(uint data)
{
    const float invRange = 1.0f / (float)0x7fff;

    // Get the two's complement if the sign bit is set (0x8000) meaning the bits will represent a short negative number.
    int signedValue = data & 0x8000 ? -1 * ((~data & 0x7fff) + 1) : data;

    // Use max otherwise a value of 32768 as input would be decoded to -1.00003052f. https://www.khronos.org/opengl/wiki/Normalized_Integer
    return max(signedValue * invRange, -1.0f);
}

uint3 UnityRayTracingFetchTriangleIndices(uint primitiveIndex)
{
    uint3 indices;

    MeshInfo meshInfo = unity_MeshInfo_RT[0];

    if (meshInfo.indexSize == 2)
    {
        const uint offsetInBytes = (meshInfo.indexStart + primitiveIndex * 3) << 1;
        const uint dwordAlignedOffset = offsetInBytes & ~3;
        const uint2 fourIndices = unity_MeshIndexBuffer_RT.Load2(dwordAlignedOffset);

        if (dwordAlignedOffset == offsetInBytes)
        {
            indices.x = fourIndices.x & 0xffff;
            indices.y = (fourIndices.x >> 16) & 0xffff;
            indices.z = fourIndices.y & 0xffff;
        }
        else
        {
            indices.x = (fourIndices.x >> 16) & 0xffff;
            indices.y = fourIndices.y & 0xffff;
            indices.z = (fourIndices.y >> 16) & 0xffff;
        }

        indices = indices + meshInfo.baseVertex.xxx;
    }
    else if (meshInfo.indexSize == 4)
    {
        const uint offsetInBytes = (meshInfo.indexStart + primitiveIndex * 3) << 2;
        indices = unity_MeshIndexBuffer_RT.Load3(offsetInBytes) + meshInfo.baseVertex.xxx;
    }
    else // meshInfo.indexSize == 0
    {
        const uint firstVertexIndex = primitiveIndex * 3 + meshInfo.vertexStart;
        indices = firstVertexIndex.xxx + uint3(0, 1, 2);
    }

    return indices;
}

// Checks if the vertex attribute attributeType is present in one of the unity_MeshVertexBuffers_RT vertex streams.
bool UnityRayTracingHasVertexAttribute(uint attributeType)
{
    VertexAttributeInfo vertexDecl = unity_MeshVertexDeclaration_RT[attributeType];

    return vertexDecl.Dimension != 0;
}

// attributeType is one of the kVertexAttribute* defines
float2 UnityRayTracingFetchVertexAttribute2(uint vertexIndex, uint attributeType)
{
    VertexAttributeInfo vertexDecl = unity_MeshVertexDeclaration_RT[attributeType];

    const uint attributeDimension = vertexDecl.Dimension;

    if (!UnityRayTracingHasVertexAttribute(attributeType) || attributeDimension > 4)
        return unity_DefaultVertexAttributes[attributeType].xy;

    const uint attributeByteOffset  = vertexDecl.ByteOffset;
    const uint vertexSize           = unity_MeshInfo_RT[0].vertexSize[vertexDecl.Stream];
    const uint vertexAddress        = vertexIndex * vertexSize;
    const uint attributeAddress     = vertexAddress + attributeByteOffset;
    const uint attributeFormat      = vertexDecl.Format;

    float2 value = float2(0, 0);

    ByteAddressBuffer vertexBuffer = unity_MeshVertexBuffers_RT[NonUniformResourceIndex(vertexDecl.Stream)];

    if (attributeFormat == kVertexFormatFloat)
    {
        value = asfloat(vertexBuffer.Load2(attributeAddress));
    }
    else if (attributeFormat == kVertexFormatFloat16)
    {
        const uint twoHalfs = vertexBuffer.Load(attributeAddress);
        value = float2(f16tof32(twoHalfs), f16tof32(twoHalfs >> 16));
    }
    else if (attributeFormat == kVertexFormatSNorm16)
    {
        const uint twoShorts = vertexBuffer.Load(attributeAddress);
        const float x = DecodeSNorm16(twoShorts & 0xffff);
        const float y = DecodeSNorm16((twoShorts & 0xffff0000) >> 16);
        value = float2(x, y);
    }
    else if (attributeFormat == kVertexFormatUNorm16)
    {
        const uint twoShorts = vertexBuffer.Load(attributeAddress);
        const float x = (twoShorts & 0xffff) / float(0xffff);
        const float y = ((twoShorts & 0xffff0000) >> 16) / float(0xffff);
        value = float2(x, y);
    }

    return unity_VertexChannelMask_RT[attributeDimension].xy * value;
}

// attributeType is one of the kVertexAttribute* defines
float3 UnityRayTracingFetchVertexAttribute3(uint vertexIndex, uint attributeType)
{
    VertexAttributeInfo vertexDecl = unity_MeshVertexDeclaration_RT[attributeType];

    const uint attributeDimension = vertexDecl.Dimension;

    if (!UnityRayTracingHasVertexAttribute(attributeType) || attributeDimension > 4)
        return unity_DefaultVertexAttributes[attributeType].xyz;

    const uint attributeByteOffset  = vertexDecl.ByteOffset;
    const uint vertexSize           = unity_MeshInfo_RT[0].vertexSize[vertexDecl.Stream];
    const uint vertexAddress        = vertexIndex * vertexSize;
    const uint attributeAddress     = vertexAddress + attributeByteOffset;
    const uint attributeFormat      = vertexDecl.Format;

    float3 value = float3(0, 0, 0);

    ByteAddressBuffer vertexBuffer = unity_MeshVertexBuffers_RT[NonUniformResourceIndex(vertexDecl.Stream)];

    if (attributeFormat == kVertexFormatFloat)
    {
        value = asfloat(vertexBuffer.Load3(attributeAddress));
    }
    else if (attributeFormat == kVertexFormatFloat16)
    {
        const uint2 fourHalfs = vertexBuffer.Load2(attributeAddress);
        value = float3(f16tof32(fourHalfs.x), f16tof32(fourHalfs.x >> 16), f16tof32(fourHalfs.y));
    }
    else if (attributeFormat == kVertexFormatSNorm16)
    {
        const uint2 fourShorts = vertexBuffer.Load2(attributeAddress);
        const float x = DecodeSNorm16(fourShorts.x & 0xffff);
        const float y = DecodeSNorm16((fourShorts.x & 0xffff0000) >> 16);
        const float z = DecodeSNorm16(fourShorts.y & 0xffff);
        value = float3(x, y, z);
    }
    else if (attributeFormat == kVertexFormatUNorm16)
    {
        const uint2 fourShorts = vertexBuffer.Load2(attributeAddress);
        const float x = (fourShorts.x & 0xffff) / float(0xffff);
        const float y = ((fourShorts.x & 0xffff0000) >> 16) / float(0xffff);
        const float z = (fourShorts.y & 0xffff) / float(0xffff);
        value = float3(x, y, z);
    }
    else if (attributeFormat == kVertexFormatUNorm8)
    {
        const uint data = vertexBuffer.Load(attributeAddress);
        value = float3(data & 0xff, (data & 0xff00) >> 8, (data & 0xff0000) >> 16) / 255.0f;
    }

    return unity_VertexChannelMask_RT[attributeDimension].xyz * value;
}

// attributeType is one of the kVertexAttribute* defines
float4 UnityRayTracingFetchVertexAttribute4(uint vertexIndex, uint attributeType)
{
    VertexAttributeInfo vertexDecl = unity_MeshVertexDeclaration_RT[attributeType];

    const uint attributeDimension = vertexDecl.Dimension;

    if (!UnityRayTracingHasVertexAttribute(attributeType) || attributeDimension > 4)
        return unity_DefaultVertexAttributes[attributeType];

    const uint attributeByteOffset  = vertexDecl.ByteOffset;
    const uint vertexSize           = unity_MeshInfo_RT[0].vertexSize[vertexDecl.Stream];
    const uint vertexAddress        = vertexIndex * vertexSize;
    const uint attributeAddress     = vertexAddress + attributeByteOffset;
    const uint attributeFormat      = vertexDecl.Format;

    float4 value = float4(0, 0, 0, 0);

    ByteAddressBuffer vertexBuffer = unity_MeshVertexBuffers_RT[NonUniformResourceIndex(vertexDecl.Stream)];

    if (attributeFormat == kVertexFormatFloat)
    {
        value = asfloat(vertexBuffer.Load4(attributeAddress));
    }
    else if (attributeFormat == kVertexFormatFloat16)
    {
        const uint2 fourHalfs = vertexBuffer.Load2(attributeAddress);
        value = float4(f16tof32(fourHalfs.x), f16tof32(fourHalfs.x >> 16), f16tof32(fourHalfs.y), f16tof32(fourHalfs.y >> 16));
    }
    else if (attributeFormat == kVertexFormatSNorm16)
    {
        const uint2 fourShorts = vertexBuffer.Load2(attributeAddress);
        const float x = DecodeSNorm16(fourShorts.x & 0xffff);
        const float y = DecodeSNorm16((fourShorts.x & 0xffff0000) >> 16);
        const float z = DecodeSNorm16(fourShorts.y & 0xffff);
        const float w = DecodeSNorm16((fourShorts.y & 0xffff0000) >> 16);
        value = float4(x, y, z, w);
    }
    else if (attributeFormat == kVertexFormatUNorm16)
    {
        const uint2 fourShorts = vertexBuffer.Load2(attributeAddress);
        const float x = (fourShorts.x & 0xffff) / float(0xffff);
        const float y = ((fourShorts.x & 0xffff0000) >> 16) / float(0xffff);
        const float z = (fourShorts.y & 0xffff) / float(0xffff);
        const float w = ((fourShorts.y & 0xffff0000) >> 16) / float(0xffff);
        value = float4(x, y, z, w);
    }
    else if (attributeFormat == kVertexFormatUNorm8)
    {
        const uint data = vertexBuffer.Load(attributeAddress);
        value = float4(data & 0xff, (data & 0xff00) >> 8, (data & 0xff0000) >> 16, (data & 0xff000000) >> 24) / 255.0f;
    }

    return unity_VertexChannelMask_RT[attributeDimension] * value;
}

#endif  //#ifndef UNITY_RAY_TRACING_MESH_UTILS_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityRayTracingMeshUtils.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityShaderUtilities.cginc---------------


#ifndef UNITY_SHADER_UTILITIES_INCLUDED
#define UNITY_SHADER_UTILITIES_INCLUDED

// This file is always included in all unity shaders.

#include "UnityShaderVariables.cginc"

float3 ODSOffset(float3 worldPos, float ipd)
{
    //based on google's omni-directional stereo rendering thread
    const float EPSILON = 2.4414e-4;
    float3 worldUp = float3(0.0, 1.0, 0.0);
    float3 camOffset = worldPos.xyz - _WorldSpaceCameraPos.xyz;
    float4 direction = float4(camOffset.xyz, dot(camOffset.xyz, camOffset.xyz));
    direction.w = max(EPSILON, direction.w);
    direction *= rsqrt(direction.w);

    float3 tangent = cross(direction.xyz, worldUp.xyz);
    if (dot(tangent, tangent) < EPSILON)
        return float3(0, 0, 0);
    tangent = normalize(tangent);

    float directionMinusIPD = max(EPSILON, direction.w*direction.w - ipd*ipd);
    float a = ipd * ipd / direction.w;
    float b = ipd / direction.w * sqrt(directionMinusIPD);
    float3 offset = -a * direction.xyz + b * tangent;
    return offset;
}

inline float4 UnityObjectToClipPosODS(float3 inPos)
{
    float4 clipPos;
    float3 posWorld = mul(unity_ObjectToWorld, float4(inPos, 1.0)).xyz;
#if defined(STEREO_CUBEMAP_RENDER_ON)
    float3 offset = ODSOffset(posWorld, unity_HalfStereoSeparation.x);
    clipPos = mul(UNITY_MATRIX_VP, float4(posWorld + offset, 1.0));
#else
    clipPos = mul(UNITY_MATRIX_VP, float4(posWorld, 1.0));
#endif
    return clipPos;
}

// Tranforms position from object to homogenous space
inline float4 UnityObjectToClipPos(in float3 pos)
{
#if defined(STEREO_CUBEMAP_RENDER_ON)
    return UnityObjectToClipPosODS(pos);
#else
    // More efficient than computing M*VP matrix product
    return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
#endif
}
inline float4 UnityObjectToClipPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return UnityObjectToClipPos(pos.xyz);
}

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityShaderUtilities.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityShaderVariables.cginc---------------


#ifndef UNITY_SHADER_VARIABLES_INCLUDED
#define UNITY_SHADER_VARIABLES_INCLUDED

#include "HLSLSupport.cginc"

#if defined (DIRECTIONAL_COOKIE) || defined (DIRECTIONAL)
#define USING_DIRECTIONAL_LIGHT
#endif

#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define USING_STEREO_MATRICES
#endif

#if defined(USING_STEREO_MATRICES)
    #define glstate_matrix_projection unity_StereoMatrixP[unity_StereoEyeIndex]
    #define unity_MatrixV unity_StereoMatrixV[unity_StereoEyeIndex]
    #define unity_MatrixInvV unity_StereoMatrixInvV[unity_StereoEyeIndex]
    #define unity_MatrixVP unity_StereoMatrixVP[unity_StereoEyeIndex]

    #define unity_CameraProjection unity_StereoCameraProjection[unity_StereoEyeIndex]
    #define unity_CameraInvProjection unity_StereoCameraInvProjection[unity_StereoEyeIndex]
    #define unity_WorldToCamera unity_StereoWorldToCamera[unity_StereoEyeIndex]
    #define unity_CameraToWorld unity_StereoCameraToWorld[unity_StereoEyeIndex]
    #define _WorldSpaceCameraPos unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex]
#endif

#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_M unity_ObjectToWorld

#define UNITY_LIGHTMODEL_AMBIENT (glstate_lightmodel_ambient * 2)

// ----------------------------------------------------------------------------


CBUFFER_START(UnityPerCamera)
    // Time (t = time since current level load) values from Unity
    float4 _Time; // (t/20, t, t*2, t*3)
    float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
    float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
    float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt

#if !defined(USING_STEREO_MATRICES)
    float3 _WorldSpaceCameraPos;
#endif

    // x = 1 or -1 (-1 if projection is flipped)
    // y = near plane
    // z = far plane
    // w = 1/far plane
    float4 _ProjectionParams;

    // x = width
    // y = height
    // z = 1 + 1.0/width
    // w = 1 + 1.0/height
    float4 _ScreenParams;

    // Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
    // x = 1-far/near
    // y = far/near
    // z = x/far
    // w = y/far
    // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
    // x = -1+far/near
    // y = 1
    // z = x/far
    // w = 1/far
    float4 _ZBufferParams;

    // x = orthographic camera's width
    // y = orthographic camera's height
    // z = unused
    // w = 1.0 if camera is ortho, 0.0 if perspective
    float4 unity_OrthoParams;
#if defined(STEREO_CUBEMAP_RENDER_ON)
    //x-component is the half stereo separation value, which a positive for right eye and negative for left eye. The y,z,w components are unused.
    float4 unity_HalfStereoSeparation;
#endif
CBUFFER_END


CBUFFER_START(UnityPerCameraRare)
    float4 unity_CameraWorldClipPlanes[6];

#if !defined(USING_STEREO_MATRICES)
    // Projection matrices of the camera. Note that this might be different from projection matrix
    // that is set right now, e.g. while rendering shadows the matrices below are still the projection
    // of original camera.
    float4x4 unity_CameraProjection;
    float4x4 unity_CameraInvProjection;
    float4x4 unity_WorldToCamera;
    float4x4 unity_CameraToWorld;
#endif
CBUFFER_END



// ----------------------------------------------------------------------------

CBUFFER_START(UnityLighting)

    #ifdef USING_DIRECTIONAL_LIGHT
    half4 _WorldSpaceLightPos0;
    #else
    float4 _WorldSpaceLightPos0;
    #endif

    float4 _LightPositionRange; // xyz = pos, w = 1/range
    float4 _LightProjectionParams; // for point light projection: x = zfar / (znear - zfar), y = (znear * zfar) / (znear - zfar), z=shadow bias, w=shadow scale bias

    float4 unity_4LightPosX0;
    float4 unity_4LightPosY0;
    float4 unity_4LightPosZ0;
    half4 unity_4LightAtten0;

    half4 unity_LightColor[8];


    float4 unity_LightPosition[8]; // view-space vertex light positions (position,1), or (-direction,0) for directional lights.
    // x = cos(spotAngle/2) or -1 for non-spot
    // y = 1/cos(spotAngle/4) or 1 for non-spot
    // z = quadratic attenuation
    // w = range*range
    half4 unity_LightAtten[8];
    float4 unity_SpotDirection[8]; // view-space spot light directions, or (0,0,1,0) for non-spot

    // SH lighting environment
    half4 unity_SHAr;
    half4 unity_SHAg;
    half4 unity_SHAb;
    half4 unity_SHBr;
    half4 unity_SHBg;
    half4 unity_SHBb;
    half4 unity_SHC;

    // part of Light because it can be used outside of shadow distance
    fixed4 unity_OcclusionMaskSelector;
    fixed4 unity_ProbesOcclusion;
CBUFFER_END

CBUFFER_START(UnityLightingOld)
    half3 unity_LightColor0, unity_LightColor1, unity_LightColor2, unity_LightColor3; // keeping those only for any existing shaders; remove in 4.0
CBUFFER_END


// ----------------------------------------------------------------------------

CBUFFER_START(UnityShadows)
    float4 unity_ShadowSplitSpheres[4];
    float4 unity_ShadowSplitSqRadii;
    float4 unity_LightShadowBias;
    float4 _LightSplitsNear;
    float4 _LightSplitsFar;
    float4x4 unity_WorldToShadow[4];
    float4 _LightShadowData;
    float4 unity_ShadowFadeCenterAndType;
CBUFFER_END

// ----------------------------------------------------------------------------

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
    float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms
    float4 unity_RenderingLayer;
CBUFFER_END

#if defined(USING_STEREO_MATRICES)
GLOBAL_CBUFFER_START(UnityStereoGlobals)
    float4x4 unity_StereoMatrixP[2];
    float4x4 unity_StereoMatrixV[2];
    float4x4 unity_StereoMatrixInvV[2];
    float4x4 unity_StereoMatrixVP[2];

    float4x4 unity_StereoCameraProjection[2];
    float4x4 unity_StereoCameraInvProjection[2];
    float4x4 unity_StereoWorldToCamera[2];
    float4x4 unity_StereoCameraToWorld[2];

    float3 unity_StereoWorldSpaceCameraPos[2];
    float4 unity_StereoScaleOffset[2];
GLOBAL_CBUFFER_END
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
    #define unity_StereoEyeIndex UNITY_VIEWID
    UNITY_DECLARE_MULTIVIEW(2);
#elif defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    static uint unity_StereoEyeIndex;
#elif defined(UNITY_SINGLE_PASS_STEREO)
    GLOBAL_CBUFFER_START(UnityStereoEyeIndex)
        int unity_StereoEyeIndex;
    GLOBAL_CBUFFER_END
#endif

CBUFFER_START(UnityPerDrawRare)
    float4x4 glstate_matrix_transpose_modelview0;
CBUFFER_END


// ----------------------------------------------------------------------------

CBUFFER_START(UnityPerFrame)

    fixed4 glstate_lightmodel_ambient;
    fixed4 unity_AmbientSky;
    fixed4 unity_AmbientEquator;
    fixed4 unity_AmbientGround;
    fixed4 unity_IndirectSpecColor;

#if !defined(USING_STEREO_MATRICES)
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;
    int unity_StereoEyeIndex;
#endif

    fixed4 unity_ShadowColor;
CBUFFER_END


// ----------------------------------------------------------------------------

CBUFFER_START(UnityFog)
    fixed4 unity_FogColor;
    // x = density / sqrt(ln(2)), useful for Exp2 mode
    // y = density / ln(2), useful for Exp mode
    // z = -1/(end-start), useful for Linear mode
    // w = end/(end-start), useful for Linear mode
    float4 unity_FogParams;
CBUFFER_END


// ----------------------------------------------------------------------------
// Lightmaps

// Main lightmap
UNITY_DECLARE_TEX2D_HALF(unity_Lightmap);
// Directional lightmap (always used with unity_Lightmap, so can share sampler)
UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(unity_LightmapInd);
// Shadowmasks
UNITY_DECLARE_TEX2D(unity_ShadowMask);

// Dynamic GI lightmap
UNITY_DECLARE_TEX2D(unity_DynamicLightmap);
UNITY_DECLARE_TEX2D_NOSAMPLER(unity_DynamicDirectionality);
UNITY_DECLARE_TEX2D_NOSAMPLER(unity_DynamicNormal);

CBUFFER_START(UnityLightmaps)
    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;
CBUFFER_END


// ----------------------------------------------------------------------------
// Reflection Probes

UNITY_DECLARE_TEXCUBE(unity_SpecCube0);
UNITY_DECLARE_TEXCUBE_NOSAMPLER(unity_SpecCube1);

CBUFFER_START(UnityReflectionProbes)
    float4 unity_SpecCube0_BoxMax;
    float4 unity_SpecCube0_BoxMin;
    float4 unity_SpecCube0_ProbePosition;
    half4  unity_SpecCube0_HDR;

    float4 unity_SpecCube1_BoxMax;
    float4 unity_SpecCube1_BoxMin;
    float4 unity_SpecCube1_ProbePosition;
    half4  unity_SpecCube1_HDR;
CBUFFER_END


// ----------------------------------------------------------------------------
// Light Probe Proxy Volume

// UNITY_LIGHT_PROBE_PROXY_VOLUME is used as a shader keyword coming from tier settings and may be also disabled with nolppv pragma.
// We need to convert it to 0/1 and doing a second check for safety.
#ifdef UNITY_LIGHT_PROBE_PROXY_VOLUME
    #undef UNITY_LIGHT_PROBE_PROXY_VOLUME
    #if !defined(UNITY_NO_LPPV)
        #define UNITY_LIGHT_PROBE_PROXY_VOLUME 1
    #else
        #define UNITY_LIGHT_PROBE_PROXY_VOLUME 0
    #endif
#else
    #define UNITY_LIGHT_PROBE_PROXY_VOLUME 0
#endif

#if UNITY_LIGHT_PROBE_PROXY_VOLUME
    UNITY_DECLARE_TEX3D_FLOAT(unity_ProbeVolumeSH);

    CBUFFER_START(UnityProbeVolume)
        // x = Disabled(0)/Enabled(1)
        // y = Computation are done in global space(0) or local space(1)
        // z = Texel size on U texture coordinate
        float4 unity_ProbeVolumeParams;

        float4x4 unity_ProbeVolumeWorldToObject;
        float3 unity_ProbeVolumeSizeInv;
        float3 unity_ProbeVolumeMin;
    CBUFFER_END
#endif

static float4x4 unity_MatrixMVP = mul(unity_MatrixVP, unity_ObjectToWorld);
static float4x4 unity_MatrixMV = mul(unity_MatrixV, unity_ObjectToWorld);
static float4x4 unity_MatrixTMV = transpose(unity_MatrixMV);
static float4x4 unity_MatrixITMV = transpose(mul(unity_WorldToObject, unity_MatrixInvV));
// make them macros so that they can be redefined in UnityInstancing.cginc
#define UNITY_MATRIX_MVP    unity_MatrixMVP
#define UNITY_MATRIX_MV     unity_MatrixMV
#define UNITY_MATRIX_T_MV   unity_MatrixTMV
#define UNITY_MATRIX_IT_MV  unity_MatrixITMV

// ----------------------------------------------------------------------------
//  Deprecated

// There used to be fixed function-like texture matrices, defined as UNITY_MATRIX_TEXTUREn. These are gone now; and are just defined to identity.
#define UNITY_MATRIX_TEXTURE0 float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)
#define UNITY_MATRIX_TEXTURE1 float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)
#define UNITY_MATRIX_TEXTURE2 float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)
#define UNITY_MATRIX_TEXTURE3 float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityShaderVariables.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityShadowLibrary.cginc---------------


#ifndef UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED
#define UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED

// Shadowmap helpers.
#if defined( SHADOWS_SCREEN ) && defined( LIGHTMAP_ON )
    #define HANDLE_SHADOWS_BLENDING_IN_GI 1
#endif

#define unityShadowCoord float
#define unityShadowCoord2 float2
#define unityShadowCoord3 float3
#define unityShadowCoord4 float4
#define unityShadowCoord4x4 float4x4

half    UnitySampleShadowmap_PCF7x7(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (7x7 kernel)
half    UnitySampleShadowmap_PCF5x5(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (5x5 kernel)
half    UnitySampleShadowmap_PCF3x3(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (3x3 kernel)
float3  UnityGetReceiverPlaneDepthBias(float3 shadowCoord, float biasbiasMultiply); // Receiver plane depth bias

// ------------------------------------------------------------------
// Spot light shadows
// ------------------------------------------------------------------

#if defined (SHADOWS_DEPTH) && defined (SPOT)

    // declare shadowmap
    #if !defined(SHADOWMAPSAMPLER_DEFINED)
        UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
        #define SHADOWMAPSAMPLER_DEFINED
    #endif

    // shadow sampling offsets and texel size
    #if defined (SHADOWS_SOFT)
        float4 _ShadowOffsets[4];
        float4 _ShadowMapTexture_TexelSize;
        #define SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
    #endif

inline fixed UnitySampleShadowmap (float4 shadowCoord)
{
    #if defined (SHADOWS_SOFT)

        half shadow = 1;

        // No hardware comparison sampler (ie some mobile + xbox360) : simple 4 tap PCF
        #if !defined (SHADOWS_NATIVE)
            float3 coord = shadowCoord.xyz / shadowCoord.w;
            float4 shadowVals;
            shadowVals.x = SAMPLE_DEPTH_TEXTURE(_ShadowMapTexture, coord + _ShadowOffsets[0].xy);
            shadowVals.y = SAMPLE_DEPTH_TEXTURE(_ShadowMapTexture, coord + _ShadowOffsets[1].xy);
            shadowVals.z = SAMPLE_DEPTH_TEXTURE(_ShadowMapTexture, coord + _ShadowOffsets[2].xy);
            shadowVals.w = SAMPLE_DEPTH_TEXTURE(_ShadowMapTexture, coord + _ShadowOffsets[3].xy);
            half4 shadows = (shadowVals < coord.zzzz) ? _LightShadowData.rrrr : 1.0f;
            shadow = dot(shadows, 0.25f);
        #else
            // Mobile with comparison sampler : 4-tap linear comparison filter
            #if defined(SHADER_API_MOBILE)
                float3 coord = shadowCoord.xyz / shadowCoord.w;
                half4 shadows;
                shadows.x = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, coord + _ShadowOffsets[0]);
                shadows.y = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, coord + _ShadowOffsets[1]);
                shadows.z = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, coord + _ShadowOffsets[2]);
                shadows.w = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, coord + _ShadowOffsets[3]);
                shadow = dot(shadows, 0.25f);
            // Everything else
            #else
                float3 coord = shadowCoord.xyz / shadowCoord.w;
                float3 receiverPlaneDepthBias = UnityGetReceiverPlaneDepthBias(coord, 1.0f);
                shadow = UnitySampleShadowmap_PCF3x3(float4(coord, 1), receiverPlaneDepthBias);
            #endif
        shadow = lerp(_LightShadowData.r, 1.0f, shadow);
        #endif
    #else
        // 1-tap shadows
        #if defined (SHADOWS_NATIVE)
            half shadow = UNITY_SAMPLE_SHADOW_PROJ(_ShadowMapTexture, shadowCoord);
            shadow = lerp(_LightShadowData.r, 1.0f, shadow);
        #else
            half shadow = SAMPLE_DEPTH_TEXTURE_PROJ(_ShadowMapTexture, UNITY_PROJ_COORD(shadowCoord)) < (shadowCoord.z / shadowCoord.w) ? _LightShadowData.r : 1.0;
        #endif

    #endif

    return shadow;
}

#endif // #if defined (SHADOWS_DEPTH) && defined (SPOT)

// ------------------------------------------------------------------
// Point light shadows
// ------------------------------------------------------------------

#if defined (SHADOWS_CUBE)

#if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
    UNITY_DECLARE_TEXCUBE_SHADOWMAP(_ShadowMapTexture);
#else
    UNITY_DECLARE_TEXCUBE(_ShadowMapTexture);
    inline float SampleCubeDistance (float3 vec)
    {
        return UnityDecodeCubeShadowDepth(UNITY_SAMPLE_TEXCUBE_LOD(_ShadowMapTexture, vec, 0));
    }

#endif

inline half UnitySampleShadowmap (float3 vec)
{
    #if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
        float3 absVec = abs(vec);
        float dominantAxis = max(max(absVec.x, absVec.y), absVec.z); // TODO use max3() instead
        dominantAxis = max(0.00001, dominantAxis - _LightProjectionParams.z); // shadow bias from point light is apllied here.
        dominantAxis *= _LightProjectionParams.w; // bias
        float mydist = -_LightProjectionParams.x + _LightProjectionParams.y/dominantAxis; // project to shadow map clip space [0; 1]

        #if defined(UNITY_REVERSED_Z)
        mydist = 1.0 - mydist; // depth buffers are reversed! Additionally we can move this to CPP code!
        #endif
    #else
        float mydist = length(vec) * _LightPositionRange.w;
        mydist *= _LightProjectionParams.w; // bias
    #endif

    #if defined (SHADOWS_SOFT)
        float z = 1.0/128.0;
        float4 shadowVals;
        // No hardware comparison sampler (ie some mobile + xbox360) : simple 4 tap PCF
        #if defined (SHADOWS_CUBE_IN_DEPTH_TEX)
            shadowVals.x = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec+float3( z, z, z), mydist));
            shadowVals.y = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec+float3(-z,-z, z), mydist));
            shadowVals.z = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec+float3(-z, z,-z), mydist));
            shadowVals.w = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec+float3( z,-z,-z), mydist));
            half shadow = dot(shadowVals, 0.25);
            return lerp(_LightShadowData.r, 1.0, shadow);
        #else
            shadowVals.x = SampleCubeDistance (vec+float3( z, z, z));
            shadowVals.y = SampleCubeDistance (vec+float3(-z,-z, z));
            shadowVals.z = SampleCubeDistance (vec+float3(-z, z,-z));
            shadowVals.w = SampleCubeDistance (vec+float3( z,-z,-z));
            half4 shadows = (shadowVals < mydist.xxxx) ? _LightShadowData.rrrr : 1.0f;
            return dot(shadows, 0.25);
        #endif
    #else
        #if defined (SHADOWS_CUBE_IN_DEPTH_TEX)
            half shadow = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec, mydist));
            return lerp(_LightShadowData.r, 1.0, shadow);
        #else
            half shadowVal = UnityDecodeCubeShadowDepth(UNITY_SAMPLE_TEXCUBE(_ShadowMapTexture, vec));
            half shadow = shadowVal < mydist ? _LightShadowData.r : 1.0;
            return shadow;
        #endif
    #endif

}
#endif // #if defined (SHADOWS_CUBE)


// ------------------------------------------------------------------
// Baked shadows
// ------------------------------------------------------------------

#if UNITY_LIGHT_PROBE_PROXY_VOLUME

half4 LPPV_SampleProbeOcclusion(float3 worldPos)
{
    const float transformToLocal = unity_ProbeVolumeParams.y;
    const float texelSizeX = unity_ProbeVolumeParams.z;

    //The SH coefficients textures and probe occlusion are packed into 1 atlas.
    //-------------------------
    //| ShR | ShG | ShB | Occ |
    //-------------------------

    float3 position = (transformToLocal == 1.0f) ? mul(unity_ProbeVolumeWorldToObject, float4(worldPos, 1.0)).xyz : worldPos;

    //Get a tex coord between 0 and 1
    float3 texCoord = (position - unity_ProbeVolumeMin.xyz) * unity_ProbeVolumeSizeInv.xyz;

    // Sample fourth texture in the atlas
    // We need to compute proper U coordinate to sample.
    // Clamp the coordinate otherwize we'll have leaking between ShB coefficients and Probe Occlusion(Occ) info
    texCoord.x = max(texCoord.x * 0.25f + 0.75f, 0.75f + 0.5f * texelSizeX);

    return UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);
}

#endif //#if UNITY_LIGHT_PROBE_PROXY_VOLUME

// ------------------------------------------------------------------
// Used by the forward rendering path
fixed UnitySampleBakedOcclusion (float2 lightmapUV, float3 worldPos)
{
    #if defined (SHADOWS_SHADOWMASK)
        #if defined(LIGHTMAP_ON)
            fixed4 rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
        #else
            fixed4 rawOcclusionMask = fixed4(1.0, 1.0, 1.0, 1.0);
            #if UNITY_LIGHT_PROBE_PROXY_VOLUME
                if (unity_ProbeVolumeParams.x == 1.0)
                    rawOcclusionMask = LPPV_SampleProbeOcclusion(worldPos);
                else
                    rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
            #else
                rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
            #endif
        #endif
        return saturate(dot(rawOcclusionMask, unity_OcclusionMaskSelector));

    #else

        //In forward dynamic objects can only get baked occlusion from LPPV, light probe occlusion is done on the CPU by attenuating the light color.
        fixed atten = 1.0f;
        #if defined(UNITY_INSTANCING_ENABLED) && defined(UNITY_USE_SHCOEFFS_ARRAYS)
            // ...unless we are doing instancing, and the attenuation is packed into SHC array's .w component.
            atten = unity_SHC.w;
        #endif

        #if UNITY_LIGHT_PROBE_PROXY_VOLUME && !defined(LIGHTMAP_ON) && !UNITY_STANDARD_SIMPLE
            fixed4 rawOcclusionMask = atten.xxxx;
            if (unity_ProbeVolumeParams.x == 1.0)
                rawOcclusionMask = LPPV_SampleProbeOcclusion(worldPos);
            return saturate(dot(rawOcclusionMask, unity_OcclusionMaskSelector));
        #endif

        return atten;
    #endif
}

// ------------------------------------------------------------------
// Used by the deferred rendering path (in the gbuffer pass)
fixed4 UnityGetRawBakedOcclusions(float2 lightmapUV, float3 worldPos)
{
    #if defined (SHADOWS_SHADOWMASK)
        #if defined(LIGHTMAP_ON)
            return UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
        #else
            half4 probeOcclusion = unity_ProbesOcclusion;

            #if UNITY_LIGHT_PROBE_PROXY_VOLUME
                if (unity_ProbeVolumeParams.x == 1.0)
                    probeOcclusion = LPPV_SampleProbeOcclusion(worldPos);
            #endif

            return probeOcclusion;
        #endif
    #else
        return fixed4(1.0, 1.0, 1.0, 1.0);
    #endif
}

// ------------------------------------------------------------------
// Used by both the forward and the deferred rendering path
half UnityMixRealtimeAndBakedShadows(half realtimeShadowAttenuation, half bakedShadowAttenuation, half fade)
{
    // -- Static objects --
    // FWD BASE PASS
    // ShadowMask mode          = LIGHTMAP_ON + SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = LIGHTMAP_ON + SHADOWS_SHADOWMASK
    // Subtractive mode         = LIGHTMAP_ON + LIGHTMAP_SHADOW_MIXING
    // Pure realtime direct lit = LIGHTMAP_ON

    // FWD ADD PASS
    // ShadowMask mode          = SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = SHADOWS_SHADOWMASK
    // Pure realtime direct lit = LIGHTMAP_ON

    // DEFERRED LIGHTING PASS
    // ShadowMask mode          = LIGHTMAP_ON + SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = LIGHTMAP_ON + SHADOWS_SHADOWMASK
    // Pure realtime direct lit = LIGHTMAP_ON

    // -- Dynamic objects --
    // FWD BASE PASS + FWD ADD ASS
    // ShadowMask mode          = LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = N/A
    // Subtractive mode         = LIGHTMAP_SHADOW_MIXING (only matter for LPPV. Light probes occlusion being done on CPU)
    // Pure realtime direct lit = N/A

    // DEFERRED LIGHTING PASS
    // ShadowMask mode          = SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = SHADOWS_SHADOWMASK
    // Pure realtime direct lit = N/A

    #if !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_CUBE)
        #if defined(LIGHTMAP_ON) && defined (LIGHTMAP_SHADOW_MIXING) && !defined (SHADOWS_SHADOWMASK)
            //In subtractive mode when there is no shadow we kill the light contribution as direct as been baked in the lightmap.
            return 0.0;
        #else
            return bakedShadowAttenuation;
        #endif
    #endif

    #if (SHADER_TARGET <= 20) || UNITY_STANDARD_SIMPLE
        //no fading nor blending on SM 2.0 because of instruction count limit.
        #if defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING)
            return min(realtimeShadowAttenuation, bakedShadowAttenuation);
        #else
            return realtimeShadowAttenuation;
        #endif
    #endif

    #if defined(LIGHTMAP_SHADOW_MIXING)
        //Subtractive or shadowmask mode
        realtimeShadowAttenuation = saturate(realtimeShadowAttenuation + fade);
        return min(realtimeShadowAttenuation, bakedShadowAttenuation);
    #endif

    //In distance shadowmask or realtime shadow fadeout we lerp toward the baked shadows (bakedShadowAttenuation will be 1 if no baked shadows)
    return lerp(realtimeShadowAttenuation, bakedShadowAttenuation, fade);
}

// ------------------------------------------------------------------
// Shadow fade
// ------------------------------------------------------------------

float UnityComputeShadowFadeDistance(float3 wpos, float z)
{
    float sphereDist = distance(wpos, unity_ShadowFadeCenterAndType.xyz);
    return lerp(z, sphereDist, unity_ShadowFadeCenterAndType.w);
}

// ------------------------------------------------------------------
half UnityComputeShadowFade(float fadeDist)
{
    return saturate(fadeDist * _LightShadowData.z + _LightShadowData.w);
}


// ------------------------------------------------------------------
//  Bias
// ------------------------------------------------------------------

/**
* Computes the receiver plane depth bias for the given shadow coord in screen space.
* Inspirations:
*   http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*   http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2012/10/Isidoro-ShadowMapping.pdf
*/
float3 UnityGetReceiverPlaneDepthBias(float3 shadowCoord, float biasMultiply)
{
    // Should receiver plane bias be used? This estimates receiver slope using derivatives,
    // and tries to tilt the PCF kernel along it. However, when doing it in screenspace from the depth texture
    // (ie all light in deferred and directional light in both forward and deferred), the derivatives are wrong
    // on edges or intersections of objects, leading to shadow artifacts. Thus it is disabled by default.
    float3 biasUVZ = 0;

#if defined(UNITY_USE_RECEIVER_PLANE_BIAS) && defined(SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED)
    float3 dx = ddx(shadowCoord);
    float3 dy = ddy(shadowCoord);

    biasUVZ.x = dy.y * dx.z - dx.y * dy.z;
    biasUVZ.y = dx.x * dy.z - dy.x * dx.z;
    biasUVZ.xy *= biasMultiply / ((dx.x * dy.y) - (dx.y * dy.x));

    // Static depth biasing to make up for incorrect fractional sampling on the shadow map grid.
    const float UNITY_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR = 0.01f;
    float fractionalSamplingError = dot(_ShadowMapTexture_TexelSize.xy, abs(biasUVZ.xy));
    biasUVZ.z = -min(fractionalSamplingError, UNITY_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR);
    #if defined(UNITY_REVERSED_Z)
        biasUVZ.z *= -1;
    #endif
#endif

    return biasUVZ;
}

/**
* Combines the different components of a shadow coordinate and returns the final coordinate.
* See UnityGetReceiverPlaneDepthBias
*/
float3 UnityCombineShadowcoordComponents(float2 baseUV, float2 deltaUV, float depth, float3 receiverPlaneDepthBias)
{
    float3 uv = float3(baseUV + deltaUV, depth + receiverPlaneDepthBias.z);
    uv.z += dot(deltaUV, receiverPlaneDepthBias.xy);
    return uv;
}

// ------------------------------------------------------------------
//  PCF Filtering helpers
// ------------------------------------------------------------------

/**
* Assuming a isoceles rectangle triangle of height "triangleHeight" (as drawn below).
* This function return the area of the triangle above the first texel.
*
* |\      <-- 45 degree slop isosceles rectangle triangle
* | \
* ----    <-- length of this side is "triangleHeight"
* _ _ _ _ <-- texels
*/
float _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(float triangleHeight)
{
    return triangleHeight - 0.5;
}

/**
* Assuming a isoceles triangle of 1.5 texels height and 3 texels wide lying on 4 texels.
* This function return the area of the triangle above each of those texels.
*    |    <-- offset from -0.5 to 0.5, 0 meaning triangle is exactly in the center
*   / \   <-- 45 degree slop isosceles triangle (ie tent projected in 2D)
*  /   \
* _ _ _ _ <-- texels
* X Y Z W <-- result indices (in computedArea.xyzw and computedAreaUncut.xyzw)
*/
void _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(float offset, out float4 computedArea, out float4 computedAreaUncut)
{
    //Compute the exterior areas
    float offset01SquaredHalved = (offset + 0.5) * (offset + 0.5) * 0.5;
    computedAreaUncut.x = computedArea.x = offset01SquaredHalved - offset;
    computedAreaUncut.w = computedArea.w = offset01SquaredHalved;

    //Compute the middle areas
    //For Y : We find the area in Y of as if the left section of the isoceles triangle would
    //intersect the axis between Y and Z (ie where offset = 0).
    computedAreaUncut.y = _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(1.5 - offset);
    //This area is superior to the one we are looking for if (offset < 0) thus we need to
    //subtract the area of the triangle defined by (0,1.5-offset), (0,1.5+offset), (-offset,1.5).
    float clampedOffsetLeft = min(offset,0);
    float areaOfSmallLeftTriangle = clampedOffsetLeft * clampedOffsetLeft;
    computedArea.y = computedAreaUncut.y - areaOfSmallLeftTriangle;

    //We do the same for the Z but with the right part of the isoceles triangle
    computedAreaUncut.z = _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(1.5 + offset);
    float clampedOffsetRight = max(offset,0);
    float areaOfSmallRightTriangle = clampedOffsetRight * clampedOffsetRight;
    computedArea.z = computedAreaUncut.z - areaOfSmallRightTriangle;
}

/**
 * Assuming a isoceles triangle of 1.5 texels height and 3 texels wide lying on 4 texels.
 * This function return the weight of each texels area relative to the full triangle area.
 */
void _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(float offset, out float4 computedWeight)
{
    float4 dummy;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedWeight, dummy);
    computedWeight *= 0.44444;//0.44 == 1/(the triangle area)
}

/**
* Assuming a isoceles triangle of 2.5 texel height and 5 texels wide lying on 6 texels.
* This function return the weight of each texels area relative to the full triangle area.
*  /       \
* _ _ _ _ _ _ <-- texels
* 0 1 2 3 4 5 <-- computed area indices (in texelsWeights[])
*/
void _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(float offset, out float3 texelsWeightsA, out float3 texelsWeightsB)
{
    //See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
    float4 computedArea_From3texelTriangle;
    float4 computedAreaUncut_From3texelTriangle;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

    //Triangle slop is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
    //the 5 texel wide triangle can be seen as the 3 texel wide one but shifted up by one unit/texel.
    //0.16 is 1/(the triangle area)
    texelsWeightsA.x = 0.16 * (computedArea_From3texelTriangle.x);
    texelsWeightsA.y = 0.16 * (computedAreaUncut_From3texelTriangle.y);
    texelsWeightsA.z = 0.16 * (computedArea_From3texelTriangle.y + 1);
    texelsWeightsB.x = 0.16 * (computedArea_From3texelTriangle.z + 1);
    texelsWeightsB.y = 0.16 * (computedAreaUncut_From3texelTriangle.z);
    texelsWeightsB.z = 0.16 * (computedArea_From3texelTriangle.w);
}

/**
* Assuming a isoceles triangle of 3.5 texel height and 7 texels wide lying on 8 texels.
* This function return the weight of each texels area relative to the full triangle area.
*  /           \
* _ _ _ _ _ _ _ _ <-- texels
* 0 1 2 3 4 5 6 7 <-- computed area indices (in texelsWeights[])
*/
void _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(float offset, out float4 texelsWeightsA, out float4 texelsWeightsB)
{
    //See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
    float4 computedArea_From3texelTriangle;
    float4 computedAreaUncut_From3texelTriangle;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

    //Triangle slop is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
    //the 7 texel wide triangle can be seen as the 3 texel wide one but shifted up by two unit/texel.
    //0.081632 is 1/(the triangle area)
    texelsWeightsA.x = 0.081632 * (computedArea_From3texelTriangle.x);
    texelsWeightsA.y = 0.081632 * (computedAreaUncut_From3texelTriangle.y);
    texelsWeightsA.z = 0.081632 * (computedAreaUncut_From3texelTriangle.y + 1);
    texelsWeightsA.w = 0.081632 * (computedArea_From3texelTriangle.y + 2);
    texelsWeightsB.x = 0.081632 * (computedArea_From3texelTriangle.z + 2);
    texelsWeightsB.y = 0.081632 * (computedAreaUncut_From3texelTriangle.z + 1);
    texelsWeightsB.z = 0.081632 * (computedAreaUncut_From3texelTriangle.z);
    texelsWeightsB.w = 0.081632 * (computedArea_From3texelTriangle.w);
}

// ------------------------------------------------------------------
//  PCF Filtering
// ------------------------------------------------------------------

/**
* PCF gaussian shadowmap filtering based on a 3x3 kernel (9 taps no PCF hardware support)
*/
half UnitySampleShadowmap_PCF3x3NoHardwareSupport(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
    // when we don't have hardware PCF sampling, then the above 5x5 optimized PCF really does not work.
    // Fallback to a simple 3x3 sampling with averaged results.
    float2 base_uv = coord.xy;
    float2 ts = _ShadowMapTexture_TexelSize.xy;
    shadow = 0;
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, ts.y), coord.z, receiverPlaneDepthBias));
    shadow /= 9.0;
#endif

    return shadow;
}

/**
* PCF tent shadowmap filtering based on a 3x3 kernel (optimized with 4 taps)
*/
half UnitySampleShadowmap_PCF3x3Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 3x3 base thus covering from 9 to 12 texels, thus we need 4 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based
    float4 texelsWeightsU, texelsWeightsV;
    _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU);
    _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float2 fetchesWeightsU = texelsWeightsU.xz + texelsWeightsU.yw;
    float2 fetchesWeightsV = texelsWeightsV.xz + texelsWeightsV.yw;

    // move the PCF bilinear fetches to respect texels weights
    float2 fetchesOffsetsU = texelsWeightsU.yw / fetchesWeightsU.xy + float2(-1.5,0.5);
    float2 fetchesOffsetsV = texelsWeightsV.yw / fetchesWeightsV.xy + float2(-1.5,0.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow =  fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/**
* PCF tent shadowmap filtering based on a 5x5 kernel (optimized with 9 taps)
*/
half UnitySampleShadowmap_PCF5x5Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 5x5 base thus covering from 25 to 36 texels, thus we need 9 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based on the area of a 45 degree slop tent above each of them.
    float3 texelsWeightsU_A, texelsWeightsU_B;
    float3 texelsWeightsV_A, texelsWeightsV_B;
    _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float3 fetchesWeightsU = float3(texelsWeightsU_A.xz, texelsWeightsU_B.y) + float3(texelsWeightsU_A.y, texelsWeightsU_B.xz);
    float3 fetchesWeightsV = float3(texelsWeightsV_A.xz, texelsWeightsV_B.y) + float3(texelsWeightsV_A.y, texelsWeightsV_B.xz);

    // move the PCF bilinear fetches to respect texels weights
    float3 fetchesOffsetsU = float3(texelsWeightsU_A.y, texelsWeightsU_B.xz) / fetchesWeightsU.xyz + float3(-2.5,-0.5,1.5);
    float3 fetchesOffsetsV = float3(texelsWeightsV_A.y, texelsWeightsV_B.xz) / fetchesWeightsV.xyz + float3(-2.5,-0.5,1.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xxx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yyy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow  = fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/**
* PCF tent shadowmap filtering based on a 7x7 kernel (optimized with 16 taps)
*/
half UnitySampleShadowmap_PCF7x7Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 7x7 base thus covering from 49 to 64 texels, thus we need 16 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based on the area of a 45 degree slop tent above each of them.
    float4 texelsWeightsU_A, texelsWeightsU_B;
    float4 texelsWeightsV_A, texelsWeightsV_B;
    _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float4 fetchesWeightsU = float4(texelsWeightsU_A.xz, texelsWeightsU_B.xz) + float4(texelsWeightsU_A.yw, texelsWeightsU_B.yw);
    float4 fetchesWeightsV = float4(texelsWeightsV_A.xz, texelsWeightsV_B.xz) + float4(texelsWeightsV_A.yw, texelsWeightsV_B.yw);

    // move the PCF bilinear fetches to respect texels weights
    float4 fetchesOffsetsU = float4(texelsWeightsU_A.yw, texelsWeightsU_B.yw) / fetchesWeightsU.xyzw + float4(-3.5,-1.5,0.5,2.5);
    float4 fetchesOffsetsV = float4(texelsWeightsV_A.yw, texelsWeightsV_B.yw) / fetchesWeightsV.xyzw + float4(-3.5,-1.5,0.5,2.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xxxx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yyyy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow  = fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/**
* PCF gaussian shadowmap filtering based on a 3x3 kernel (optimized with 4 taps)
*
* Algorithm: http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/
* Implementation example: http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*/
half UnitySampleShadowmap_PCF3x3Gaussian(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    const float2 offset = float2(0.5, 0.5);
    float2 uv = (coord.xy * _ShadowMapTexture_TexelSize.zw) + offset;
    float2 base_uv = (floor(uv) - offset) * _ShadowMapTexture_TexelSize.xy;
    float2 st = frac(uv);

    float2 uw = float2(3 - 2 * st.x, 1 + 2 * st.x);
    float2 u = float2((2 - st.x) / uw.x - 1, (st.x) / uw.y + 1);
    u *= _ShadowMapTexture_TexelSize.x;

    float2 vw = float2(3 - 2 * st.y, 1 + 2 * st.y);
    float2 v = float2((2 - st.y) / vw.x - 1, (st.y) / vw.y + 1);
    v *= _ShadowMapTexture_TexelSize.y;

    half sum = 0;

    sum += uw[0] * vw[0] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[0], v[0]), coord.z, receiverPlaneDepthBias));
    sum += uw[1] * vw[0] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[1], v[0]), coord.z, receiverPlaneDepthBias));
    sum += uw[0] * vw[1] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[0], v[1]), coord.z, receiverPlaneDepthBias));
    sum += uw[1] * vw[1] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[1], v[1]), coord.z, receiverPlaneDepthBias));

    shadow = sum / 16.0f;
#endif

    return shadow;
}

/**
* PCF gaussian shadowmap filtering based on a 5x5 kernel (optimized with 9 taps)
*
* Algorithm: http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/
* Implementation example: http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*/
half UnitySampleShadowmap_PCF5x5Gaussian(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    const float2 offset = float2(0.5, 0.5);
    float2 uv = (coord.xy * _ShadowMapTexture_TexelSize.zw) + offset;
    float2 base_uv = (floor(uv) - offset) * _ShadowMapTexture_TexelSize.xy;
    float2 st = frac(uv);

    float3 uw = float3(4 - 3 * st.x, 7, 1 + 3 * st.x);
    float3 u = float3((3 - 2 * st.x) / uw.x - 2, (3 + st.x) / uw.y, st.x / uw.z + 2);
    u *= _ShadowMapTexture_TexelSize.x;

    float3 vw = float3(4 - 3 * st.y, 7, 1 + 3 * st.y);
    float3 v = float3((3 - 2 * st.y) / vw.x - 2, (3 + st.y) / vw.y, st.y / vw.z + 2);
    v *= _ShadowMapTexture_TexelSize.y;

    half sum = 0.0f;

    half3 accum = uw * vw.x;
    sum += accum.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.x), coord.z, receiverPlaneDepthBias));
    sum += accum.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.x), coord.z, receiverPlaneDepthBias));
    sum += accum.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.x), coord.z, receiverPlaneDepthBias));

    accum = uw * vw.y;
    sum += accum.x *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.y), coord.z, receiverPlaneDepthBias));
    sum += accum.y *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.y), coord.z, receiverPlaneDepthBias));
    sum += accum.z *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.y), coord.z, receiverPlaneDepthBias));

    accum = uw * vw.z;
    sum += accum.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.z), coord.z, receiverPlaneDepthBias));
    sum += accum.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.z), coord.z, receiverPlaneDepthBias));
    sum += accum.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.z), coord.z, receiverPlaneDepthBias));
    shadow = sum / 144.0f;

#endif

    return shadow;
}

half UnitySampleShadowmap_PCF3x3(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF3x3Tent(coord, receiverPlaneDepthBias);
}

half UnitySampleShadowmap_PCF5x5(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF5x5Tent(coord, receiverPlaneDepthBias);
}

half UnitySampleShadowmap_PCF7x7(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF7x7Tent(coord, receiverPlaneDepthBias);
}

#endif // UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityShadowLibrary.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnitySprites.cginc---------------


#ifndef UNITY_SPRITES_INCLUDED
#define UNITY_SPRITES_INCLUDED

#include "UnityCG.cginc"

#ifdef UNITY_INSTANCING_ENABLED

    UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
        // SpriteRenderer.Color while Non-Batched/Instanced.
        UNITY_DEFINE_INSTANCED_PROP(fixed4, unity_SpriteRendererColorArray)
        // this could be smaller but that's how bit each entry is regardless of type
        UNITY_DEFINE_INSTANCED_PROP(fixed2, unity_SpriteFlipArray)
    UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

    #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
    #define _Flip           UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)

#endif // instancing

CBUFFER_START(UnityPerDrawSprite)
#ifndef UNITY_INSTANCING_ENABLED
    fixed4 _RendererColor;
    fixed2 _Flip;
#endif
    float _EnableExternalAlpha;
CBUFFER_END

// Material Color.
fixed4 _Color;

struct appdata_t
{
    float4 vertex   : POSITION;
    float4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex   : SV_POSITION;
    fixed4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

inline float4 UnityFlipSprite(in float3 pos, in fixed2 flip)
{
    return float4(pos.xy * flip, pos.z, 1.0);
}

v2f SpriteVert(appdata_t IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID (IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
    OUT.vertex = UnityObjectToClipPos(OUT.vertex);
    OUT.texcoord = IN.texcoord;
    OUT.color = IN.color * _Color * _RendererColor;

    #ifdef PIXELSNAP_ON
    OUT.vertex = UnityPixelSnap (OUT.vertex);
    #endif

    return OUT;
}

sampler2D _MainTex;
sampler2D _AlphaTex;

fixed4 SampleSpriteTexture (float2 uv)
{
    fixed4 color = tex2D (_MainTex, uv);

#if ETC1_EXTERNAL_ALPHA
    fixed4 alpha = tex2D (_AlphaTex, uv);
    color.a = lerp (color.a, alpha.r, _EnableExternalAlpha);
#endif

    return color;
}

fixed4 SpriteFrag(v2f IN) : SV_Target
{
    fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
    c.rgb *= c.a;
    return c;
}

#endif // UNITY_SPRITES_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnitySprites.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardBRDF.cginc---------------


#ifndef UNITY_STANDARD_BRDF_INCLUDED
#define UNITY_STANDARD_BRDF_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityLightingCommon.cginc"

//-----------------------------------------------------------------------------
// Helper to convert smoothness to roughness
//-----------------------------------------------------------------------------

float PerceptualRoughnessToRoughness(float perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

half RoughnessToPerceptualRoughness(half roughness)
{
    return sqrt(roughness);
}

// Smoothness is the user facing name
// it should be perceptualSmoothness but we don't want the user to have to deal with this name
half SmoothnessToRoughness(half smoothness)
{
    return (1 - smoothness) * (1 - smoothness);
}

float SmoothnessToPerceptualRoughness(float smoothness)
{
    return (1 - smoothness);
}

//-------------------------------------------------------------------------------------

inline half Pow4 (half x)
{
    return x*x*x*x;
}

inline float2 Pow4 (float2 x)
{
    return x*x*x*x;
}

inline half3 Pow4 (half3 x)
{
    return x*x*x*x;
}

inline half4 Pow4 (half4 x)
{
    return x*x*x*x;
}

// Pow5 uses the same amount of instructions as generic pow(), but has 2 advantages:
// 1) better instruction pipelining
// 2) no need to worry about NaNs
inline half Pow5 (half x)
{
    return x*x * x*x * x;
}

inline half2 Pow5 (half2 x)
{
    return x*x * x*x * x;
}

inline half3 Pow5 (half3 x)
{
    return x*x * x*x * x;
}

inline half4 Pow5 (half4 x)
{
    return x*x * x*x * x;
}

inline half3 FresnelTerm (half3 F0, half cosA)
{
    half t = Pow5 (1 - cosA);   // ala Schlick interpoliation
    return F0 + (1-F0) * t;
}
inline half3 FresnelLerp (half3 F0, half3 F90, half cosA)
{
    half t = Pow5 (1 - cosA);   // ala Schlick interpoliation
    return lerp (F0, F90, t);
}
// approximage Schlick with ^4 instead of ^5
inline half3 FresnelLerpFast (half3 F0, half3 F90, half cosA)
{
    half t = Pow4 (1 - cosA);
    return lerp (F0, F90, t);
}

// Note: Disney diffuse must be multiply by diffuseAlbedo / PI. This is done outside of this function.
half DisneyDiffuse(half NdotV, half NdotL, half LdotH, half perceptualRoughness)
{
    half fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
    // Two schlick fresnel term
    half lightScatter   = (1 + (fd90 - 1) * Pow5(1 - NdotL));
    half viewScatter    = (1 + (fd90 - 1) * Pow5(1 - NdotV));

    return lightScatter * viewScatter;
}

// NOTE: Visibility term here is the full form from Torrance-Sparrow model, it includes Geometric term: V = G / (N.L * N.V)
// This way it is easier to swap Geometric terms and more room for optimizations (except maybe in case of CookTorrance geom term)

// Generic Smith-Schlick visibility term
inline half SmithVisibilityTerm (half NdotL, half NdotV, half k)
{
    half gL = NdotL * (1-k) + k;
    half gV = NdotV * (1-k) + k;
    return 1.0 / (gL * gV + 1e-5f); // This function is not intended to be running on Mobile,
                                    // therefore epsilon is smaller than can be represented by half
}

// Smith-Schlick derived for Beckmann
inline half SmithBeckmannVisibilityTerm (half NdotL, half NdotV, half roughness)
{
    half c = 0.797884560802865h; // c = sqrt(2 / Pi)
    half k = roughness * c;
    return SmithVisibilityTerm (NdotL, NdotV, k) * 0.25f; // * 0.25 is the 1/4 of the visibility term
}

// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
inline float SmithJointGGXVisibilityTerm (float NdotL, float NdotV, float roughness)
{
#if 0
    // Original formulation:
    //  lambda_v    = (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5f;
    //  lambda_l    = (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5f;
    //  G           = 1 / (1 + lambda_v + lambda_l);

    // Reorder code to be more optimal
    half a          = roughness;
    half a2         = a * a;

    half lambdaV    = NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
    half lambdaL    = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    // Simplify visibility term: (2.0f * NdotL * NdotV) /  ((4.0f * NdotL * NdotV) * (lambda_v + lambda_l + 1e-5f));
    return 0.5f / (lambdaV + lambdaL + 1e-5f);  // This function is not intended to be running on Mobile,
                                                // therefore epsilon is smaller than can be represented by half
#else
    // Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
    float a = roughness;
    float lambdaV = NdotL * (NdotV * (1 - a) + a);
    float lambdaL = NdotV * (NdotL * (1 - a) + a);

#if defined(SHADER_API_SWITCH)
    return 0.5f / (lambdaV + lambdaL + UNITY_HALF_MIN);
#else
    return 0.5f / (lambdaV + lambdaL + 1e-5f);
#endif

#endif
}

inline float GGXTerm (float NdotH, float roughness)
{
    float a2 = roughness * roughness;
    float d = (NdotH * a2 - NdotH) * NdotH + 1.0f; // 2 mad
    return UNITY_INV_PI * a2 / (d * d + 1e-7f); // This function is not intended to be running on Mobile,
                                            // therefore epsilon is smaller than what can be represented by half
}

inline half PerceptualRoughnessToSpecPower (half perceptualRoughness)
{
    half m = PerceptualRoughnessToRoughness(perceptualRoughness);   // m is the true academic roughness.
    half sq = max(1e-4f, m*m);
    half n = (2.0 / sq) - 2.0;                          // https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf
    n = max(n, 1e-4f);                                  // prevent possible cases of pow(0,0), which could happen when roughness is 1.0 and NdotH is zero
    return n;
}

// BlinnPhong normalized as normal distribution function (NDF)
// for use in micro-facet model: spec=D*G*F
// eq. 19 in https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf
inline half NDFBlinnPhongNormalizedTerm (half NdotH, half n)
{
    // norm = (n+2)/(2*pi)
    half normTerm = (n + 2.0) * (0.5/UNITY_PI);

    half specTerm = pow (NdotH, n);
    return specTerm * normTerm;
}

//-------------------------------------------------------------------------------------
/*
// https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

const float k0 = 0.00098, k1 = 0.9921;
// pass this as a constant for optimization
const float fUserMaxSPow = 100000; // sqrt(12M)
const float g_fMaxT = ( exp2(-10.0/fUserMaxSPow) - k0)/k1;
float GetSpecPowToMip(float fSpecPow, int nMips)
{
   // Default curve - Inverse of TB2 curve with adjusted constants
   float fSmulMaxT = ( exp2(-10.0/sqrt( fSpecPow )) - k0)/k1;
   return float(nMips-1)*(1.0 - clamp( fSmulMaxT/g_fMaxT, 0.0, 1.0 ));
}

    //float specPower = PerceptualRoughnessToSpecPower(perceptualRoughness);
    //float mip = GetSpecPowToMip (specPower, 7);
*/

inline float3 Unity_SafeNormalize(float3 inVec)
{
    float dp3 = max(0.001f, dot(inVec, inVec));
    return inVec * rsqrt(dp3);
}

//-------------------------------------------------------------------------------------

// Note: BRDF entry points use smoothness and oneMinusReflectivity for optimization
// purposes, mostly for DX9 SM2.0 level. Most of the math is being done on these (1-x) values, and that saves
// a few precious ALU slots.


// Main Physically Based BRDF
// Derived from Disney work and based on Torrance-Sparrow micro-facet model
//
//   BRDF = kD / pi + kS * (D * V * F) / 4
//   I = BRDF * NdotL
//
// * NDF (depending on UNITY_BRDF_GGX):
//  a) Normalized BlinnPhong
//  b) GGX
// * Smith for Visiblity term
// * Schlick approximation for Fresnel
half4 BRDF1_Unity_PBS (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
    float3 normal, float3 viewDir,
    UnityLight light, UnityIndirect gi)
{
    float perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
    float3 halfDir = Unity_SafeNormalize (float3(light.dir) + viewDir);

// NdotV should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
// In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
// but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of NdotV (less correct but works too).
// Following define allow to control this. Set it to 0 if ALU is critical on your platform.
// This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
// Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
#define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0

#if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
    // The amount we shift the normal toward the view vector is defined by the dot product.
    half shiftAmount = dot(normal, viewDir);
    normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
    // A re-normalization should be applied here but as the shift is small we don't do it to save ALU.
    //normal = normalize(normal);

    float nv = saturate(dot(normal, viewDir)); // TODO: this saturate should no be necessary here
#else
    half nv = abs(dot(normal, viewDir));    // This abs allow to limit artifact
#endif

    float nl = saturate(dot(normal, light.dir));
    float nh = saturate(dot(normal, halfDir));

    half lv = saturate(dot(light.dir, viewDir));
    half lh = saturate(dot(light.dir, halfDir));

    // Diffuse term
    half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;

    // Specular term
    // HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
    // BUT 1) that will make shader look significantly darker than Legacy ones
    // and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
#if UNITY_BRDF_GGX
    // GGX with roughtness to 0 would mean no specular at all, using max(roughness, 0.002) here to match HDrenderloop roughtness remapping.
    roughness = max(roughness, 0.002);
    float V = SmithJointGGXVisibilityTerm (nl, nv, roughness);
    float D = GGXTerm (nh, roughness);
#else
    // Legacy
    half V = SmithBeckmannVisibilityTerm (nl, nv, roughness);
    half D = NDFBlinnPhongNormalizedTerm (nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
#endif

    float specularTerm = V*D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

#   ifdef UNITY_COLORSPACE_GAMMA
        specularTerm = sqrt(max(1e-4h, specularTerm));
#   endif

    // specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
    specularTerm = max(0, specularTerm * nl);
#if defined(_SPECULARHIGHLIGHTS_OFF)
    specularTerm = 0.0;
#endif

    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
    half surfaceReduction;
#   ifdef UNITY_COLORSPACE_GAMMA
        surfaceReduction = 1.0-0.28*roughness*perceptualRoughness;      // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#   else
        surfaceReduction = 1.0 / (roughness*roughness + 1.0);           // fade \in [0.5;1]
#   endif

    // To provide true Lambert lighting, we need to be able to kill specular completely.
    specularTerm *= any(specColor) ? 1.0 : 0.0;

    half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));
    half3 color =   diffColor * (gi.diffuse + light.color * diffuseTerm)
                    + specularTerm * light.color * FresnelTerm (specColor, lh)
                    + surfaceReduction * gi.specular * FresnelLerp (specColor, grazingTerm, nv);

    return half4(color, 1);
}

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF (depending on UNITY_BRDF_GGX):
//  a) BlinnPhong
//  b) [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half4 BRDF2_Unity_PBS (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
    float3 normal, float3 viewDir,
    UnityLight light, UnityIndirect gi)
{
    float3 halfDir = Unity_SafeNormalize (float3(light.dir) + viewDir);

    half nl = saturate(dot(normal, light.dir));
    float nh = saturate(dot(normal, halfDir));
    half nv = saturate(dot(normal, viewDir));
    float lh = saturate(dot(light.dir, halfDir));

    // Specular term
    half perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
    half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

#if UNITY_BRDF_GGX

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155
    float a = roughness;
    float a2 = a*a;

    float d = nh * nh * (a2 - 1.f) + 1.00001f;
#ifdef UNITY_COLORSPACE_GAMMA
    // Tighter approximation for Gamma only rendering mode!
    // DVF = sqrt(DVF);
    // DVF = (a * sqrt(.25)) / (max(sqrt(0.1), lh)*sqrt(roughness + .5) * d);
    float specularTerm = a / (max(0.32f, lh) * (1.5f + roughness) * d);
#else
    float specularTerm = a2 / (max(0.1f, lh*lh) * (roughness + 0.5f) * (d * d) * 4);
#endif

    // on mobiles (where half actually means something) denominator have risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE)
    specularTerm = specularTerm - 1e-4f;
#endif

#else

    // Legacy
    half specularPower = PerceptualRoughnessToSpecPower(perceptualRoughness);
    // Modified with approximate Visibility function that takes roughness into account
    // Original ((n+1)*N.H^n) / (8*Pi * L.H^3) didn't take into account roughness
    // and produced extremely bright specular at grazing angles

    half invV = lh * lh * smoothness + perceptualRoughness * perceptualRoughness; // approx ModifiedKelemenVisibilityTerm(lh, perceptualRoughness);
    half invF = lh;

    half specularTerm = ((specularPower + 1) * pow (nh, specularPower)) / (8 * invV * invF + 1e-4h);

#ifdef UNITY_COLORSPACE_GAMMA
    specularTerm = sqrt(max(1e-4f, specularTerm));
#endif

#endif

#if defined (SHADER_API_MOBILE)
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif
#if defined(_SPECULARHIGHLIGHTS_OFF)
    specularTerm = 0.0;
#endif

    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(realRoughness^2+1)

    // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
    // 1-x^3*(0.6-0.08*x)   approximation for 1/(x^4+1)
#ifdef UNITY_COLORSPACE_GAMMA
    half surfaceReduction = 0.28;
#else
    half surfaceReduction = (0.6-0.08*perceptualRoughness);
#endif

    surfaceReduction = 1.0 - roughness*perceptualRoughness*surfaceReduction;

    half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));
    half3 color =   (diffColor + specularTerm * specColor) * light.color * nl
                    + gi.diffuse * diffColor
                    + surfaceReduction * gi.specular * FresnelLerpFast (specColor, grazingTerm, nv);

    return half4(color, 1);
}

sampler2D_float unity_NHxRoughness;
half3 BRDF3_Direct(half3 diffColor, half3 specColor, half rlPow4, half smoothness)
{
    half LUT_RANGE = 16.0; // must match range in NHxRoughness() function in GeneratedTextures.cpp
    // Lookup texture to save instructions
    half specular = tex2D(unity_NHxRoughness, half2(rlPow4, SmoothnessToPerceptualRoughness(smoothness))).r * LUT_RANGE;
#if defined(_SPECULARHIGHLIGHTS_OFF)
    specular = 0.0;
#endif

    return diffColor + specular * specColor;
}

half3 BRDF3_Indirect(half3 diffColor, half3 specColor, UnityIndirect indirect, half grazingTerm, half fresnelTerm)
{
    half3 c = indirect.diffuse * diffColor;
    c += indirect.specular * lerp (specColor, grazingTerm, fresnelTerm);
    return c;
}

// Old school, not microfacet based Modified Normalized Blinn-Phong BRDF
// Implementation uses Lookup texture for performance
//
// * Normalized BlinnPhong in RDF form
// * Implicit Visibility term
// * No Fresnel term
//
// TODO: specular is too weak in Linear rendering mode
half4 BRDF3_Unity_PBS (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
    float3 normal, float3 viewDir,
    UnityLight light, UnityIndirect gi)
{
    float3 reflDir = reflect (viewDir, normal);

    half nl = saturate(dot(normal, light.dir));
    half nv = saturate(dot(normal, viewDir));

    // Vectorize Pow4 to save instructions
    half2 rlPow4AndFresnelTerm = Pow4 (float2(dot(reflDir, light.dir), 1-nv));  // use R.L instead of N.H to save couple of instructions
    half rlPow4 = rlPow4AndFresnelTerm.x; // power exponent must match kHorizontalWarpExp in NHxRoughness() function in GeneratedTextures.cpp
    half fresnelTerm = rlPow4AndFresnelTerm.y;

    half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));

    half3 color = BRDF3_Direct(diffColor, specColor, rlPow4, smoothness);
    color *= light.color * nl;
    color += BRDF3_Indirect(diffColor, specColor, gi, grazingTerm, fresnelTerm);

    return half4(color, 1);
}

// Include deprecated function
#define INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED
#include "UnityDeprecated.cginc"
#undef INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED

#endif // UNITY_STANDARD_BRDF_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardBRDF.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardConfig.cginc---------------


#ifndef UNITY_STANDARD_CONFIG_INCLUDED
#define UNITY_STANDARD_CONFIG_INCLUDED

// Define Specular cubemap constants
#ifndef UNITY_SPECCUBE_LOD_EXPONENT
#define UNITY_SPECCUBE_LOD_EXPONENT (1.5)
#endif
#ifndef UNITY_SPECCUBE_LOD_STEPS
#define UNITY_SPECCUBE_LOD_STEPS (6)
#endif

// Energy conservation for Specular workflow is Monochrome. For instance: Red metal will make diffuse Black not Cyan
#ifndef UNITY_CONSERVE_ENERGY
#define UNITY_CONSERVE_ENERGY 1
#endif
#ifndef UNITY_CONSERVE_ENERGY_MONOCHROME
#define UNITY_CONSERVE_ENERGY_MONOCHROME 1
#endif

// "platform caps" defines: they are controlled from TierSettings (Editor will determine values and pass them to compiler)
// UNITY_SPECCUBE_BOX_PROJECTION:                   TierSettings.reflectionProbeBoxProjection
// UNITY_SPECCUBE_BLENDING:                         TierSettings.reflectionProbeBlending
// UNITY_ENABLE_DETAIL_NORMALMAP:                   TierSettings.detailNormalMap
// UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS:  TierSettings.semitransparentShadows

// disregarding what is set in TierSettings, some features have hardware restrictions
// so we still add safety net, otherwise we might end up with shaders failing to compile

#if defined(SHADER_TARGET_SURFACE_ANALYSIS)
    // For surface shader code analysis pass, disable some features that don't affect inputs/outputs
    #undef UNITY_SPECCUBE_BOX_PROJECTION
    #undef UNITY_SPECCUBE_BLENDING
    #undef UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS
#elif SHADER_TARGET < 30
    #undef UNITY_SPECCUBE_BOX_PROJECTION
    #undef UNITY_SPECCUBE_BLENDING
    #undef UNITY_ENABLE_DETAIL_NORMALMAP
    #ifdef _PARALLAXMAP
        #undef _PARALLAXMAP
    #endif
#endif
#if (SHADER_TARGET < 30) || defined(SHADER_API_GLES)
    #undef UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS
#endif

#ifndef UNITY_SAMPLE_FULL_SH_PER_PIXEL
    // Lightmap UVs and ambient color from SHL2 are shared in the vertex to pixel interpolators. Do full SH evaluation in the pixel shader when static lightmap and LIGHTPROBE_SH is enabled.
    #define UNITY_SAMPLE_FULL_SH_PER_PIXEL (LIGHTMAP_ON && LIGHTPROBE_SH)

    // Shaders might fail to compile due to shader instruction count limit. Leave only baked lightmaps on SM20 hardware.
    #if UNITY_SAMPLE_FULL_SH_PER_PIXEL && (SHADER_TARGET < 25)
        #undef UNITY_SAMPLE_FULL_SH_PER_PIXEL
        #undef LIGHTPROBE_SH
    #endif
#endif

#ifndef UNITY_BRDF_GGX
#define UNITY_BRDF_GGX 1
#endif

// Orthnormalize Tangent Space basis per-pixel
// Necessary to support high-quality normal-maps. Compatible with Maya and Marmoset.
// However xNormal expects oldschool non-orthnormalized basis - essentially preventing good looking normal-maps :(
// Due to the fact that xNormal is probably _the most used tool to bake out normal-maps today_ we have to stick to old ways for now.
//
// Disabled by default, until xNormal has an option to bake proper normal-maps.
#ifndef UNITY_TANGENT_ORTHONORMALIZE
#define UNITY_TANGENT_ORTHONORMALIZE 0
#endif


// Some extra optimizations

// Simplified Standard Shader is off by default and should not be used for Legacy Shaders
#ifndef UNITY_STANDARD_SIMPLE
    #define UNITY_STANDARD_SIMPLE 0
#endif

// Setup a new define with meaningful name to know if we require world pos in fragment shader
#if UNITY_STANDARD_SIMPLE
    #define UNITY_REQUIRE_FRAG_WORLDPOS 0
#else
    #define UNITY_REQUIRE_FRAG_WORLDPOS 1
#endif

// Should we pack worldPos along tangent (saving an interpolator)
// We want to skip this on mobile platforms, because worldpos gets packed into mediump
#if UNITY_REQUIRE_FRAG_WORLDPOS && !defined(_PARALLAXMAP) && !defined(SHADER_API_MOBILE)
    #define UNITY_PACK_WORLDPOS_WITH_TANGENT 1
#else
    #define UNITY_PACK_WORLDPOS_WITH_TANGENT 0
#endif

#endif // UNITY_STANDARD_CONFIG_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardConfig.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardCore.cginc---------------


#ifndef UNITY_STANDARD_CORE_INCLUDED
#define UNITY_STANDARD_CORE_INCLUDED

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityStandardInput.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"

#include "AutoLight.cginc"
//-------------------------------------------------------------------------------------
// counterpart for NormalizePerPixelNormal
// skips normalization per-vertex and expects normalization to happen per-pixel
half3 NormalizePerVertexNormal (float3 n) // takes float to avoid overflow
{
    #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        return normalize(n);
    #else
        return n; // will normalize per-pixel instead
    #endif
}

float3 NormalizePerPixelNormal (float3 n)
{
    #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        return n;
    #else
        return normalize((float3)n); // takes float to avoid overflow
    #endif
}

//-------------------------------------------------------------------------------------
UnityLight MainLight ()
{
    UnityLight l;

    l.color = _LightColor0.rgb;
    l.dir = _WorldSpaceLightPos0.xyz;
    return l;
}

UnityLight AdditiveLight (half3 lightDir, half atten)
{
    UnityLight l;

    l.color = _LightColor0.rgb;
    l.dir = lightDir;
    #ifndef USING_DIRECTIONAL_LIGHT
        l.dir = NormalizePerPixelNormal(l.dir);
    #endif

    // shadow the light
    l.color *= atten;
    return l;
}

UnityLight DummyLight ()
{
    UnityLight l;
    l.color = 0;
    l.dir = half3 (0,1,0);
    return l;
}

UnityIndirect ZeroIndirect ()
{
    UnityIndirect ind;
    ind.diffuse = 0;
    ind.specular = 0;
    return ind;
}

//-------------------------------------------------------------------------------------
// Common fragment setup

// deprecated
half3 WorldNormal(half4 tan2world[3])
{
    return normalize(tan2world[2].xyz);
}

// deprecated
#ifdef _TANGENT_TO_WORLD
    half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
    {
        half3 t = tan2world[0].xyz;
        half3 b = tan2world[1].xyz;
        half3 n = tan2world[2].xyz;

    #if UNITY_TANGENT_ORTHONORMALIZE
        n = NormalizePerPixelNormal(n);

        // ortho-normalize Tangent
        t = normalize (t - n * dot(t, n));

        // recalculate Binormal
        half3 newB = cross(n, t);
        b = newB * sign (dot (newB, b));
    #endif

        return half3x3(t, b, n);
    }
#else
    half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
    {
        return half3x3(0,0,0,0,0,0,0,0,0);
    }
#endif

float3 PerPixelWorldNormal(float4 i_tex, float4 tangentToWorld[3])
{
#ifdef _NORMALMAP
    half3 tangent = tangentToWorld[0].xyz;
    half3 binormal = tangentToWorld[1].xyz;
    half3 normal = tangentToWorld[2].xyz;

    #if UNITY_TANGENT_ORTHONORMALIZE
        normal = NormalizePerPixelNormal(normal);

        // ortho-normalize Tangent
        tangent = normalize (tangent - normal * dot(tangent, normal));

        // recalculate Binormal
        half3 newB = cross(normal, tangent);
        binormal = newB * sign (dot (newB, binormal));
    #endif

    half3 normalTangent = NormalInTangentSpace(i_tex);
    float3 normalWorld = NormalizePerPixelNormal(tangent * normalTangent.x + binormal * normalTangent.y + normal * normalTangent.z); // @TODO: see if we can squeeze this normalize on SM2.0 as well
#else
    float3 normalWorld = normalize(tangentToWorld[2].xyz);
#endif
    return normalWorld;
}

#ifdef _PARALLAXMAP
    #define IN_VIEWDIR4PARALLAX(i) NormalizePerPixelNormal(half3(i.tangentToWorldAndPackedData[0].w,i.tangentToWorldAndPackedData[1].w,i.tangentToWorldAndPackedData[2].w))
    #define IN_VIEWDIR4PARALLAX_FWDADD(i) NormalizePerPixelNormal(i.viewDirForParallax.xyz)
#else
    #define IN_VIEWDIR4PARALLAX(i) half3(0,0,0)
    #define IN_VIEWDIR4PARALLAX_FWDADD(i) half3(0,0,0)
#endif

#if UNITY_REQUIRE_FRAG_WORLDPOS
    #if UNITY_PACK_WORLDPOS_WITH_TANGENT
        #define IN_WORLDPOS(i) half3(i.tangentToWorldAndPackedData[0].w,i.tangentToWorldAndPackedData[1].w,i.tangentToWorldAndPackedData[2].w)
    #else
        #define IN_WORLDPOS(i) i.posWorld
    #endif
    #define IN_WORLDPOS_FWDADD(i) i.posWorld
#else
    #define IN_WORLDPOS(i) half3(0,0,0)
    #define IN_WORLDPOS_FWDADD(i) half3(0,0,0)
#endif

#define IN_LIGHTDIR_FWDADD(i) half3(i.tangentToWorldAndLightDir[0].w, i.tangentToWorldAndLightDir[1].w, i.tangentToWorldAndLightDir[2].w)

#define FRAGMENT_SETUP(x) FragmentCommonData x = \
    FragmentSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX(i), i.tangentToWorldAndPackedData, IN_WORLDPOS(i));

#define FRAGMENT_SETUP_FWDADD(x) FragmentCommonData x = \
    FragmentSetup(i.tex, i.eyeVec.xyz, IN_VIEWDIR4PARALLAX_FWDADD(i), i.tangentToWorldAndLightDir, IN_WORLDPOS_FWDADD(i));

struct FragmentCommonData
{
    half3 diffColor, specColor;
    // Note: smoothness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
    // Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
    half oneMinusReflectivity, smoothness;
    float3 normalWorld;
    float3 eyeVec;
    half alpha;
    float3 posWorld;

#if UNITY_STANDARD_SIMPLE
    half3 reflUVW;
#endif

#if UNITY_STANDARD_SIMPLE
    half3 tangentSpaceNormal;
#endif
};

#ifndef UNITY_SETUP_BRDF_INPUT
    #define UNITY_SETUP_BRDF_INPUT SpecularSetup
#endif

inline FragmentCommonData SpecularSetup (float4 i_tex)
{
    half4 specGloss = SpecularGloss(i_tex.xy);
    half3 specColor = specGloss.rgb;
    half smoothness = specGloss.a;

    half oneMinusReflectivity;
    half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular (Albedo(i_tex), specColor, /*out*/ oneMinusReflectivity);

    FragmentCommonData o = (FragmentCommonData)0;
    o.diffColor = diffColor;
    o.specColor = specColor;
    o.oneMinusReflectivity = oneMinusReflectivity;
    o.smoothness = smoothness;
    return o;
}

inline FragmentCommonData RoughnessSetup(float4 i_tex)
{
    half2 metallicGloss = MetallicRough(i_tex.xy);
    half metallic = metallicGloss.x;
    half smoothness = metallicGloss.y; // this is 1 minus the square root of real roughness m.

    half oneMinusReflectivity;
    half3 specColor;
    half3 diffColor = DiffuseAndSpecularFromMetallic(Albedo(i_tex), metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    FragmentCommonData o = (FragmentCommonData)0;
    o.diffColor = diffColor;
    o.specColor = specColor;
    o.oneMinusReflectivity = oneMinusReflectivity;
    o.smoothness = smoothness;
    return o;
}

inline FragmentCommonData MetallicSetup (float4 i_tex)
{
    half2 metallicGloss = MetallicGloss(i_tex.xy);
    half metallic = metallicGloss.x;
    half smoothness = metallicGloss.y; // this is 1 minus the square root of real roughness m.

    half oneMinusReflectivity;
    half3 specColor;
    half3 diffColor = DiffuseAndSpecularFromMetallic (Albedo(i_tex), metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    FragmentCommonData o = (FragmentCommonData)0;
    o.diffColor = diffColor;
    o.specColor = specColor;
    o.oneMinusReflectivity = oneMinusReflectivity;
    o.smoothness = smoothness;
    return o;
}

// parallax transformed texcoord is used to sample occlusion
inline FragmentCommonData FragmentSetup (inout float4 i_tex, float3 i_eyeVec, half3 i_viewDirForParallax, float4 tangentToWorld[3], float3 i_posWorld)
{
    i_tex = Parallax(i_tex, i_viewDirForParallax);

    half alpha = Alpha(i_tex.xy);
    #if defined(_ALPHATEST_ON)
        clip (alpha - _Cutoff);
    #endif

    FragmentCommonData o = UNITY_SETUP_BRDF_INPUT (i_tex);
    o.normalWorld = PerPixelWorldNormal(i_tex, tangentToWorld);
    o.eyeVec = NormalizePerPixelNormal(i_eyeVec);
    o.posWorld = i_posWorld;

    // NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    o.diffColor = PreMultiplyAlpha (o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
    return o;
}

inline UnityGI FragmentGI (FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light, bool reflections)
{
    UnityGIInput d;
    d.light = light;
    d.worldPos = s.posWorld;
    d.worldViewDir = -s.eyeVec;
    d.atten = atten;
    #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
        d.ambient = 0;
        d.lightmapUV = i_ambientOrLightmapUV;
    #else
        d.ambient = i_ambientOrLightmapUV.rgb;
        d.lightmapUV = 0;
    #endif

    d.probeHDR[0] = unity_SpecCube0_HDR;
    d.probeHDR[1] = unity_SpecCube1_HDR;
    #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
      d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
    #endif
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
      d.boxMax[0] = unity_SpecCube0_BoxMax;
      d.probePosition[0] = unity_SpecCube0_ProbePosition;
      d.boxMax[1] = unity_SpecCube1_BoxMax;
      d.boxMin[1] = unity_SpecCube1_BoxMin;
      d.probePosition[1] = unity_SpecCube1_ProbePosition;
    #endif

    if(reflections)
    {
        Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.smoothness, -s.eyeVec, s.normalWorld, s.specColor);
        // Replace the reflUVW if it has been compute in Vertex shader. Note: the compiler will optimize the calcul in UnityGlossyEnvironmentSetup itself
        #if UNITY_STANDARD_SIMPLE
            g.reflUVW = s.reflUVW;
        #endif

        return UnityGlobalIllumination (d, occlusion, s.normalWorld, g);
    }
    else
    {
        return UnityGlobalIllumination (d, occlusion, s.normalWorld);
    }
}

inline UnityGI FragmentGI (FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light)
{
    return FragmentGI(s, occlusion, i_ambientOrLightmapUV, atten, light, true);
}


//-------------------------------------------------------------------------------------
half4 OutputForward (half4 output, half alphaFromSurface)
{
    #if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
        output.a = alphaFromSurface;
    #else
        UNITY_OPAQUE_ALPHA(output.a);
    #endif
    return output;
}

inline half4 VertexGIForward(VertexInput v, float3 posWorld, half3 normalWorld)
{
    half4 ambientOrLightmapUV = 0;
    // Static lightmaps
    #ifdef LIGHTMAP_ON
        ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        ambientOrLightmapUV.zw = 0;
    // Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
    #elif UNITY_SHOULD_SAMPLE_SH
        #ifdef VERTEXLIGHT_ON
            // Approximated illumination from non-important point lights
            ambientOrLightmapUV.rgb = Shade4PointLights (
                unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                unity_4LightAtten0, posWorld, normalWorld);
        #endif

        ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, ambientOrLightmapUV.rgb);
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    return ambientOrLightmapUV;
}

// ------------------------------------------------------------------
//  Base forward pass (directional light, emission, lightmaps, ...)

struct VertexOutputForwardBase
{
    UNITY_POSITION(pos);
    float4 tex                            : TEXCOORD0;
    float4 eyeVec                         : TEXCOORD1;    // eyeVec.xyz | fogCoord
    float4 tangentToWorldAndPackedData[3] : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax or worldPos]
    half4 ambientOrLightmapUV             : TEXCOORD5;    // SH or Lightmap UV
    UNITY_LIGHTING_COORDS(6,7)

    // next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT
    float3 posWorld                     : TEXCOORD8;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardBase vertForwardBase (VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputForwardBase o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    #if UNITY_REQUIRE_FRAG_WORLDPOS
        #if UNITY_PACK_WORLDPOS_WITH_TANGENT
            o.tangentToWorldAndPackedData[0].w = posWorld.x;
            o.tangentToWorldAndPackedData[1].w = posWorld.y;
            o.tangentToWorldAndPackedData[2].w = posWorld.z;
        #else
            o.posWorld = posWorld.xyz;
        #endif
    #endif
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = TexCoords(v);
    o.eyeVec.xyz = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndPackedData[0].xyz = 0;
        o.tangentToWorldAndPackedData[1].xyz = 0;
        o.tangentToWorldAndPackedData[2].xyz = normalWorld;
    #endif

    //We need this for shadow receving
    UNITY_TRANSFER_LIGHTING(o, v.uv1);

    o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

    #ifdef _PARALLAXMAP
        TANGENT_SPACE_ROTATION;
        half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
        o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
        o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
        o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
    #endif

    UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o,o.pos);
    return o;
}

half4 fragForwardBaseInternal (VertexOutputForwardBase i)
{
    FRAGMENT_SETUP(s)

    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    UnityLight mainLight = MainLight ();
    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

    half occlusion = Occlusion(i.tex.xy);
    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

    half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
    c.rgb += Emission(i.tex.xy);

    UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
    UNITY_APPLY_FOG(_unity_fogCoord, c.rgb);
    return OutputForward (c, s.alpha);
}

half4 fragForwardBase (VertexOutputForwardBase i) : SV_Target   // backward compatibility (this used to be the fragment entry function)
{
    return fragForwardBaseInternal(i);
}

// ------------------------------------------------------------------
//  Additive forward pass (one light per pass)

struct VertexOutputForwardAdd
{
    UNITY_POSITION(pos);
    float4 tex                          : TEXCOORD0;
    float4 eyeVec                       : TEXCOORD1;    // eyeVec.xyz | fogCoord
    float4 tangentToWorldAndLightDir[3] : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:lightDir]
    float3 posWorld                     : TEXCOORD5;
    UNITY_LIGHTING_COORDS(6, 7)

    // next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if defined(_PARALLAXMAP)
    half3 viewDirForParallax            : TEXCOORD8;
#endif

    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardAdd vertForwardAdd (VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputForwardAdd o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = TexCoords(v);
    o.eyeVec.xyz = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    o.posWorld = posWorld.xyz;
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndLightDir[0].xyz = 0;
        o.tangentToWorldAndLightDir[1].xyz = 0;
        o.tangentToWorldAndLightDir[2].xyz = normalWorld;
    #endif
    //We need this for shadow receiving and lighting
    UNITY_TRANSFER_LIGHTING(o, v.uv1);

    float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
    #ifndef USING_DIRECTIONAL_LIGHT
        lightDir = NormalizePerVertexNormal(lightDir);
    #endif
    o.tangentToWorldAndLightDir[0].w = lightDir.x;
    o.tangentToWorldAndLightDir[1].w = lightDir.y;
    o.tangentToWorldAndLightDir[2].w = lightDir.z;

    #ifdef _PARALLAXMAP
        TANGENT_SPACE_ROTATION;
        o.viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
    #endif

    UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o, o.pos);
    return o;
}

half4 fragForwardAddInternal (VertexOutputForwardAdd i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    FRAGMENT_SETUP_FWDADD(s)

    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
    UnityLight light = AdditiveLight (IN_LIGHTDIR_FWDADD(i), atten);
    UnityIndirect noIndirect = ZeroIndirect ();

    half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, noIndirect);

    UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
    UNITY_APPLY_FOG_COLOR(_unity_fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
    return OutputForward (c, s.alpha);
}

half4 fragForwardAdd (VertexOutputForwardAdd i) : SV_Target     // backward compatibility (this used to be the fragment entry function)
{
    return fragForwardAddInternal(i);
}

// ------------------------------------------------------------------
//  Deferred pass

struct VertexOutputDeferred
{
    UNITY_POSITION(pos);
    float4 tex                            : TEXCOORD0;
    float3 eyeVec                         : TEXCOORD1;
    float4 tangentToWorldAndPackedData[3] : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax or worldPos]
    half4 ambientOrLightmapUV             : TEXCOORD5;    // SH or Lightmap UVs

    #if UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT
        float3 posWorld                     : TEXCOORD6;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


VertexOutputDeferred vertDeferred (VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputDeferred o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputDeferred, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    #if UNITY_REQUIRE_FRAG_WORLDPOS
        #if UNITY_PACK_WORLDPOS_WITH_TANGENT
            o.tangentToWorldAndPackedData[0].w = posWorld.x;
            o.tangentToWorldAndPackedData[1].w = posWorld.y;
            o.tangentToWorldAndPackedData[2].w = posWorld.z;
        #else
            o.posWorld = posWorld.xyz;
        #endif
    #endif
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = TexCoords(v);
    o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndPackedData[0].xyz = 0;
        o.tangentToWorldAndPackedData[1].xyz = 0;
        o.tangentToWorldAndPackedData[2].xyz = normalWorld;
    #endif

    o.ambientOrLightmapUV = 0;
    #ifdef LIGHTMAP_ON
        o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #elif UNITY_SHOULD_SAMPLE_SH
        o.ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, o.ambientOrLightmapUV.rgb);
    #endif
    #ifdef DYNAMICLIGHTMAP_ON
        o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    #ifdef _PARALLAXMAP
        TANGENT_SPACE_ROTATION;
        half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
        o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
        o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
        o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
    #endif

    return o;
}

void fragDeferred (
    VertexOutputDeferred i,
    out half4 outGBuffer0 : SV_Target0,
    out half4 outGBuffer1 : SV_Target1,
    out half4 outGBuffer2 : SV_Target2,
    out half4 outEmission : SV_Target3          // RT3: emission (rgb), --unused-- (a)
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
    ,out half4 outShadowMask : SV_Target4       // RT4: shadowmask (rgba)
#endif
)
{
    #if (SHADER_TARGET < 30)
        outGBuffer0 = 1;
        outGBuffer1 = 1;
        outGBuffer2 = 0;
        outEmission = 0;
        #if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
            outShadowMask = 1;
        #endif
        return;
    #endif

    FRAGMENT_SETUP(s)
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    // no analytic lights in this pass
    UnityLight dummyLight = DummyLight ();
    half atten = 1;

    // only GI
    half occlusion = Occlusion(i.tex.xy);
#if UNITY_ENABLE_REFLECTION_BUFFERS
    bool sampleReflectionsInDeferred = false;
#else
    bool sampleReflectionsInDeferred = true;
#endif

    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

    half3 emissiveColor = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;

    #ifdef _EMISSION
        emissiveColor += Emission (i.tex.xy);
    #endif

    #ifndef UNITY_HDR_ON
        emissiveColor.rgb = exp2(-emissiveColor.rgb);
    #endif

    UnityStandardData data;
    data.diffuseColor   = s.diffColor;
    data.occlusion      = occlusion;
    data.specularColor  = s.specColor;
    data.smoothness     = s.smoothness;
    data.normalWorld    = s.normalWorld;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    // Emissive lighting buffer
    outEmission = half4(emissiveColor, 1);

    // Baked direct lighting occlusion if any
    #if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
        outShadowMask = UnityGetRawBakedOcclusions(i.ambientOrLightmapUV.xy, IN_WORLDPOS(i));
    #endif
}


//
// Old FragmentGI signature. Kept only for backward compatibility and will be removed soon
//

inline UnityGI FragmentGI(
    float3 posWorld,
    half occlusion, half4 i_ambientOrLightmapUV, half atten, half smoothness, half3 normalWorld, half3 eyeVec,
    UnityLight light,
    bool reflections)
{
    // we init only fields actually used
    FragmentCommonData s = (FragmentCommonData)0;
    s.smoothness = smoothness;
    s.normalWorld = normalWorld;
    s.eyeVec = eyeVec;
    s.posWorld = posWorld;
    return FragmentGI(s, occlusion, i_ambientOrLightmapUV, atten, light, reflections);
}
inline UnityGI FragmentGI (
    float3 posWorld,
    half occlusion, half4 i_ambientOrLightmapUV, half atten, half smoothness, half3 normalWorld, half3 eyeVec,
    UnityLight light)
{
    return FragmentGI (posWorld, occlusion, i_ambientOrLightmapUV, atten, smoothness, normalWorld, eyeVec, light, true);
}

#endif // UNITY_STANDARD_CORE_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardCore.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardCoreForward.cginc---------------


#ifndef UNITY_STANDARD_CORE_FORWARD_INCLUDED
#define UNITY_STANDARD_CORE_FORWARD_INCLUDED

#if defined(UNITY_NO_FULL_STANDARD_SHADER)
#   define UNITY_STANDARD_SIMPLE 1
#endif

#include "UnityStandardConfig.cginc"

#if UNITY_STANDARD_SIMPLE
    #include "UnityStandardCoreForwardSimple.cginc"
    VertexOutputBaseSimple vertBase (VertexInput v) { return vertForwardBaseSimple(v); }
    VertexOutputForwardAddSimple vertAdd (VertexInput v) { return vertForwardAddSimple(v); }
    half4 fragBase (VertexOutputBaseSimple i) : SV_Target { return fragForwardBaseSimpleInternal(i); }
    half4 fragAdd (VertexOutputForwardAddSimple i) : SV_Target { return fragForwardAddSimpleInternal(i); }
#else
    #include "UnityStandardCore.cginc"
    VertexOutputForwardBase vertBase (VertexInput v) { return vertForwardBase(v); }
    VertexOutputForwardAdd vertAdd (VertexInput v) { return vertForwardAdd(v); }
    half4 fragBase (VertexOutputForwardBase i) : SV_Target { return fragForwardBaseInternal(i); }
    half4 fragAdd (VertexOutputForwardAdd i) : SV_Target { return fragForwardAddInternal(i); }
#endif

#endif // UNITY_STANDARD_CORE_FORWARD_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardCoreForward.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardCoreForwardSimple.cginc---------------


#ifndef UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED
#define UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED

#include "UnityStandardCore.cginc"

//  Does not support: _PARALLAXMAP, DIRLIGHTMAP_COMBINED
#define GLOSSMAP (defined(_SPECGLOSSMAP) || defined(_METALLICGLOSSMAP))

#ifndef SPECULAR_HIGHLIGHTS
    #define SPECULAR_HIGHLIGHTS (!defined(_SPECULAR_HIGHLIGHTS_OFF))
#endif

struct VertexOutputBaseSimple
{
    UNITY_POSITION(pos);
    float4 tex                          : TEXCOORD0;
    half4 eyeVec                        : TEXCOORD1; // w: grazingTerm

    half4 ambientOrLightmapUV           : TEXCOORD2; // SH or Lightmap UV
    SHADOW_COORDS(3)
    UNITY_FOG_COORDS_PACKED(4, half4) // x: fogCoord, yzw: reflectVec

    half4 normalWorld                   : TEXCOORD5; // w: fresnelTerm

#ifdef _NORMALMAP
    half3 tangentSpaceLightDir          : TEXCOORD6;
    #if SPECULAR_HIGHLIGHTS
        half3 tangentSpaceEyeVec        : TEXCOORD7;
    #endif
#endif
#if UNITY_REQUIRE_FRAG_WORLDPOS
    float3 posWorld                     : TEXCOORD8;
#endif

    UNITY_VERTEX_OUTPUT_STEREO
};

// UNIFORM_REFLECTIVITY(): workaround to get (uniform) reflecivity based on UNITY_SETUP_BRDF_INPUT
half MetallicSetup_Reflectivity()
{
    return 1.0h - OneMinusReflectivityFromMetallic(_Metallic);
}

half SpecularSetup_Reflectivity()
{
    return SpecularStrength(_SpecColor.rgb);
}

half RoughnessSetup_Reflectivity()
{
    return MetallicSetup_Reflectivity();
}

#define JOIN2(a, b) a##b
#define JOIN(a, b) JOIN2(a,b)
#define UNIFORM_REFLECTIVITY JOIN(UNITY_SETUP_BRDF_INPUT, _Reflectivity)


#ifdef _NORMALMAP

half3 TransformToTangentSpace(half3 tangent, half3 binormal, half3 normal, half3 v)
{
    // Mali400 shader compiler prefers explicit dot product over using a half3x3 matrix
    return half3(dot(tangent, v), dot(binormal, v), dot(normal, v));
}

void TangentSpaceLightingInput(half3 normalWorld, half4 vTangent, half3 lightDirWorld, half3 eyeVecWorld, out half3 tangentSpaceLightDir, out half3 tangentSpaceEyeVec)
{
    half3 tangentWorld = UnityObjectToWorldDir(vTangent.xyz);
    half sign = half(vTangent.w) * half(unity_WorldTransformParams.w);
    half3 binormalWorld = cross(normalWorld, tangentWorld) * sign;
    tangentSpaceLightDir = TransformToTangentSpace(tangentWorld, binormalWorld, normalWorld, lightDirWorld);
    #if SPECULAR_HIGHLIGHTS
        tangentSpaceEyeVec = normalize(TransformToTangentSpace(tangentWorld, binormalWorld, normalWorld, eyeVecWorld));
    #else
        tangentSpaceEyeVec = 0;
    #endif
}

#endif // _NORMALMAP

VertexOutputBaseSimple vertForwardBaseSimple (VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputBaseSimple o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputBaseSimple, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.tex = TexCoords(v);

    half3 eyeVec = normalize(posWorld.xyz - _WorldSpaceCameraPos);
    half3 normalWorld = UnityObjectToWorldNormal(v.normal);

    o.normalWorld.xyz = normalWorld;
    o.eyeVec.xyz = eyeVec;

    #ifdef _NORMALMAP
        half3 tangentSpaceEyeVec;
        TangentSpaceLightingInput(normalWorld, v.tangent, _WorldSpaceLightPos0.xyz, eyeVec, o.tangentSpaceLightDir, tangentSpaceEyeVec);
        #if SPECULAR_HIGHLIGHTS
            o.tangentSpaceEyeVec = tangentSpaceEyeVec;
        #endif
    #endif

    //We need this for shadow receiving
    TRANSFER_SHADOW(o);

    o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

    o.fogCoord.yzw = reflect(eyeVec, normalWorld);

    o.normalWorld.w = Pow4(1 - saturate(dot(normalWorld, -eyeVec))); // fresnel term
    #if !GLOSSMAP
        o.eyeVec.w = saturate(_Glossiness + UNIFORM_REFLECTIVITY()); // grazing term
    #endif

    UNITY_TRANSFER_FOG(o, o.pos);
    return o;
}


FragmentCommonData FragmentSetupSimple(VertexOutputBaseSimple i)
{
    half alpha = Alpha(i.tex.xy);
    #if defined(_ALPHATEST_ON)
        clip (alpha - _Cutoff);
    #endif

    FragmentCommonData s = UNITY_SETUP_BRDF_INPUT (i.tex);

    // NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    s.diffColor = PreMultiplyAlpha (s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);

    s.normalWorld = i.normalWorld.xyz;
    s.eyeVec = i.eyeVec.xyz;
    s.posWorld = IN_WORLDPOS(i);
    s.reflUVW = i.fogCoord.yzw;

    #ifdef _NORMALMAP
        s.tangentSpaceNormal =  NormalInTangentSpace(i.tex);
    #else
        s.tangentSpaceNormal =  0;
    #endif

    return s;
}

UnityLight MainLightSimple(VertexOutputBaseSimple i, FragmentCommonData s)
{
    UnityLight mainLight = MainLight();
    return mainLight;
}

half PerVertexGrazingTerm(VertexOutputBaseSimple i, FragmentCommonData s)
{
    #if GLOSSMAP
        return saturate(s.smoothness + (1-s.oneMinusReflectivity));
    #else
        return i.eyeVec.w;
    #endif
}

half PerVertexFresnelTerm(VertexOutputBaseSimple i)
{
    return i.normalWorld.w;
}

#if !SPECULAR_HIGHLIGHTS
#   define REFLECTVEC_FOR_SPECULAR(i, s) half3(0, 0, 0)
#elif defined(_NORMALMAP)
#   define REFLECTVEC_FOR_SPECULAR(i, s) reflect(i.tangentSpaceEyeVec, s.tangentSpaceNormal)
#else
#   define REFLECTVEC_FOR_SPECULAR(i, s) s.reflUVW
#endif

half3 LightDirForSpecular(VertexOutputBaseSimple i, UnityLight mainLight)
{
    #if SPECULAR_HIGHLIGHTS && defined(_NORMALMAP)
        return i.tangentSpaceLightDir;
    #else
        return mainLight.dir;
    #endif
}

half3 BRDF3DirectSimple(half3 diffColor, half3 specColor, half smoothness, half rl)
{
    #if SPECULAR_HIGHLIGHTS
        return BRDF3_Direct(diffColor, specColor, Pow4(rl), smoothness);
    #else
        return diffColor;
    #endif
}

half4 fragForwardBaseSimpleInternal (VertexOutputBaseSimple i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FragmentCommonData s = FragmentSetupSimple(i);

    UnityLight mainLight = MainLightSimple(i, s);

    #if !defined(LIGHTMAP_ON) && defined(_NORMALMAP)
    half ndotl = saturate(dot(s.tangentSpaceNormal, i.tangentSpaceLightDir));
    #else
    half ndotl = saturate(dot(s.normalWorld, mainLight.dir));
    #endif

    //we can't have worldpos here (not enough interpolator on SM 2.0) so no shadow fade in that case.
    half shadowMaskAttenuation = UnitySampleBakedOcclusion(i.ambientOrLightmapUV, 0);
    half realtimeShadowAttenuation = SHADOW_ATTENUATION(i);
    half atten = UnityMixRealtimeAndBakedShadows(realtimeShadowAttenuation, shadowMaskAttenuation, 0);

    half occlusion = Occlusion(i.tex.xy);
    half rl = dot(REFLECTVEC_FOR_SPECULAR(i, s), LightDirForSpecular(i, mainLight));

    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);
    half3 attenuatedLightColor = gi.light.color * ndotl;

    half3 c = BRDF3_Indirect(s.diffColor, s.specColor, gi.indirect, PerVertexGrazingTerm(i, s), PerVertexFresnelTerm(i));
    c += BRDF3DirectSimple(s.diffColor, s.specColor, s.smoothness, rl) * attenuatedLightColor;
    c += Emission(i.tex.xy);

    UNITY_APPLY_FOG(i.fogCoord, c);

    return OutputForward (half4(c, 1), s.alpha);
}

half4 fragForwardBaseSimple (VertexOutputBaseSimple i) : SV_Target  // backward compatibility (this used to be the fragment entry function)
{
    return fragForwardBaseSimpleInternal(i);
}

struct VertexOutputForwardAddSimple
{
    UNITY_POSITION(pos);
    float4 tex                          : TEXCOORD0;
    float3 posWorld                     : TEXCOORD1;

#if !defined(_NORMALMAP) && SPECULAR_HIGHLIGHTS
    UNITY_FOG_COORDS_PACKED(2, half4) // x: fogCoord, yzw: reflectVec
#else
    UNITY_FOG_COORDS_PACKED(2, half1)
#endif

    half3 lightDir                      : TEXCOORD3;

#if defined(_NORMALMAP)
    #if SPECULAR_HIGHLIGHTS
        half3 tangentSpaceEyeVec        : TEXCOORD4;
    #endif
#else
    half3 normalWorld                   : TEXCOORD4;
#endif

    UNITY_LIGHTING_COORDS(5, 6)

    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardAddSimple vertForwardAddSimple (VertexInput v)
{
    VertexOutputForwardAddSimple o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAddSimple, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.tex = TexCoords(v);
    o.posWorld = posWorld.xyz;

    //We need this for shadow receiving and lighting
    UNITY_TRANSFER_LIGHTING(o, v.uv1);

    half3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
    #ifndef USING_DIRECTIONAL_LIGHT
        lightDir = NormalizePerVertexNormal(lightDir);
    #endif

    #if SPECULAR_HIGHLIGHTS
        half3 eyeVec = normalize(posWorld.xyz - _WorldSpaceCameraPos);
    #endif

    half3 normalWorld = UnityObjectToWorldNormal(v.normal);

    #ifdef _NORMALMAP
        #if SPECULAR_HIGHLIGHTS
            TangentSpaceLightingInput(normalWorld, v.tangent, lightDir, eyeVec, o.lightDir, o.tangentSpaceEyeVec);
        #else
            half3 ignore;
            TangentSpaceLightingInput(normalWorld, v.tangent, lightDir, 0, o.lightDir, ignore);
        #endif
    #else
        o.lightDir = lightDir;
        o.normalWorld = normalWorld;
        #if SPECULAR_HIGHLIGHTS
            o.fogCoord.yzw = reflect(eyeVec, normalWorld);
        #endif
    #endif

    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
}

FragmentCommonData FragmentSetupSimpleAdd(VertexOutputForwardAddSimple i)
{
    half alpha = Alpha(i.tex.xy);
    #if defined(_ALPHATEST_ON)
        clip (alpha - _Cutoff);
    #endif

    FragmentCommonData s = UNITY_SETUP_BRDF_INPUT (i.tex);

    // NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    s.diffColor = PreMultiplyAlpha (s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);

    s.eyeVec = 0;
    s.posWorld = i.posWorld;

    #ifdef _NORMALMAP
        s.tangentSpaceNormal = NormalInTangentSpace(i.tex);
        s.normalWorld = 0;
    #else
        s.tangentSpaceNormal = 0;
        s.normalWorld = i.normalWorld;
    #endif

    #if SPECULAR_HIGHLIGHTS && !defined(_NORMALMAP)
        s.reflUVW = i.fogCoord.yzw;
    #else
        s.reflUVW = 0;
    #endif

    return s;
}

half3 LightSpaceNormal(VertexOutputForwardAddSimple i, FragmentCommonData s)
{
    #ifdef _NORMALMAP
        return s.tangentSpaceNormal;
    #else
        return i.normalWorld;
    #endif
}

half4 fragForwardAddSimpleInternal (VertexOutputForwardAddSimple i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FragmentCommonData s = FragmentSetupSimpleAdd(i);

    half3 c = BRDF3DirectSimple(s.diffColor, s.specColor, s.smoothness, dot(REFLECTVEC_FOR_SPECULAR(i, s), i.lightDir));

    #if SPECULAR_HIGHLIGHTS // else diffColor has premultiplied light color
        c *= _LightColor0.rgb;
    #endif

    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
    c *= atten * saturate(dot(LightSpaceNormal(i, s), i.lightDir));

    UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
    return OutputForward (half4(c, 1), s.alpha);
}

half4 fragForwardAddSimple (VertexOutputForwardAddSimple i) : SV_Target // backward compatibility (this used to be the fragment entry function)
{
    return fragForwardAddSimpleInternal(i);
}

#endif // UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardCoreForwardSimple.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardInput.cginc---------------


#ifndef UNITY_STANDARD_INPUT_INCLUDED
#define UNITY_STANDARD_INPUT_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityPBSLighting.cginc" // TBD: remove
#include "UnityStandardUtils.cginc"

//---------------------------------------
// Directional lightmaps & Parallax require tangent space too
#if (_NORMALMAP || DIRLIGHTMAP_COMBINED || _PARALLAXMAP)
    #define _TANGENT_TO_WORLD 1
#endif

#if (_DETAIL_MULX2 || _DETAIL_MUL || _DETAIL_ADD || _DETAIL_LERP)
    #define _DETAIL 1
#endif

//---------------------------------------
half4       _Color;
half        _Cutoff;

sampler2D   _MainTex;
float4      _MainTex_ST;

sampler2D   _DetailAlbedoMap;
float4      _DetailAlbedoMap_ST;

sampler2D   _BumpMap;
half        _BumpScale;

sampler2D   _DetailMask;
sampler2D   _DetailNormalMap;
half        _DetailNormalMapScale;

sampler2D   _SpecGlossMap;
sampler2D   _MetallicGlossMap;
half        _Metallic;
float       _Glossiness;
float       _GlossMapScale;

sampler2D   _OcclusionMap;
half        _OcclusionStrength;

sampler2D   _ParallaxMap;
half        _Parallax;
half        _UVSec;

half4       _EmissionColor;
sampler2D   _EmissionMap;

//-------------------------------------------------------------------------------------
// Input functions

struct VertexInput
{
    float4 vertex   : POSITION;
    half3 normal    : NORMAL;
    float2 uv0      : TEXCOORD0;
    float2 uv1      : TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
    float2 uv2      : TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
    half4 tangent   : TANGENT;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 TexCoords(VertexInput v)
{
    float4 texcoord;
    texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
    texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
    return texcoord;
}

half DetailMask(float2 uv)
{
    return tex2D (_DetailMask, uv).a;
}

half3 Albedo(float4 texcoords)
{
    half3 albedo = _Color.rgb * tex2D (_MainTex, texcoords.xy).rgb;
#if _DETAIL
    #if (SHADER_TARGET < 30)
        // SM20: instruction count limitation
        // SM20: no detail mask
        half mask = 1;
    #else
        half mask = DetailMask(texcoords.xy);
    #endif
    half3 detailAlbedo = tex2D (_DetailAlbedoMap, texcoords.zw).rgb;
    #if _DETAIL_MULX2
        albedo *= LerpWhiteTo (detailAlbedo * unity_ColorSpaceDouble.rgb, mask);
    #elif _DETAIL_MUL
        albedo *= LerpWhiteTo (detailAlbedo, mask);
    #elif _DETAIL_ADD
        albedo += detailAlbedo * mask;
    #elif _DETAIL_LERP
        albedo = lerp (albedo, detailAlbedo, mask);
    #endif
#endif
    return albedo;
}

half Alpha(float2 uv)
{
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    return _Color.a;
#else
    return tex2D(_MainTex, uv).a * _Color.a;
#endif
}

half Occlusion(float2 uv)
{
#if (SHADER_TARGET < 30)
    // SM20: instruction count limitation
    // SM20: simpler occlusion
    return tex2D(_OcclusionMap, uv).g;
#else
    half occ = tex2D(_OcclusionMap, uv).g;
    return LerpOneTo (occ, _OcclusionStrength);
#endif
}

half4 SpecularGloss(float2 uv)
{
    half4 sg;
#ifdef _SPECGLOSSMAP
    #if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
        sg.rgb = tex2D(_SpecGlossMap, uv).rgb;
        sg.a = tex2D(_MainTex, uv).a;
    #else
        sg = tex2D(_SpecGlossMap, uv);
    #endif
    sg.a *= _GlossMapScale;
#else
    sg.rgb = _SpecColor.rgb;
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        sg.a = tex2D(_MainTex, uv).a * _GlossMapScale;
    #else
        sg.a = _Glossiness;
    #endif
#endif
    return sg;
}

half2 MetallicGloss(float2 uv)
{
    half2 mg;

#ifdef _METALLICGLOSSMAP
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        mg.r = tex2D(_MetallicGlossMap, uv).r;
        mg.g = tex2D(_MainTex, uv).a;
    #else
        mg = tex2D(_MetallicGlossMap, uv).ra;
    #endif
    mg.g *= _GlossMapScale;
#else
    mg.r = _Metallic;
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        mg.g = tex2D(_MainTex, uv).a * _GlossMapScale;
    #else
        mg.g = _Glossiness;
    #endif
#endif
    return mg;
}

half2 MetallicRough(float2 uv)
{
    half2 mg;
#ifdef _METALLICGLOSSMAP
    mg.r = tex2D(_MetallicGlossMap, uv).r;
#else
    mg.r = _Metallic;
#endif

#ifdef _SPECGLOSSMAP
    mg.g = 1.0f - tex2D(_SpecGlossMap, uv).r;
#else
    mg.g = 1.0f - _Glossiness;
#endif
    return mg;
}

half3 Emission(float2 uv)
{
#ifndef _EMISSION
    return 0;
#else
    return tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif
}

#ifdef _NORMALMAP
half3 NormalInTangentSpace(float4 texcoords)
{
    half3 normalTangent = UnpackScaleNormal(tex2D (_BumpMap, texcoords.xy), _BumpScale);

#if _DETAIL && defined(UNITY_ENABLE_DETAIL_NORMALMAP)
    half mask = DetailMask(texcoords.xy);
    half3 detailNormalTangent = UnpackScaleNormal(tex2D (_DetailNormalMap, texcoords.zw), _DetailNormalMapScale);
    #if _DETAIL_LERP
        normalTangent = lerp(
            normalTangent,
            detailNormalTangent,
            mask);
    #else
        normalTangent = lerp(
            normalTangent,
            BlendNormals(normalTangent, detailNormalTangent),
            mask);
    #endif
#endif

    return normalTangent;
}
#endif

float4 Parallax (float4 texcoords, half3 viewDir)
{
#if !defined(_PARALLAXMAP) || (SHADER_TARGET < 30)
    // Disable parallax on pre-SM3.0 shader target models
    return texcoords;
#else
    half h = tex2D (_ParallaxMap, texcoords.xy).g;
    float2 offset = ParallaxOffset1Step (h, _Parallax, viewDir);
    return float4(texcoords.xy + offset, texcoords.zw + offset);
#endif

}

#endif // UNITY_STANDARD_INPUT_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardInput.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardMeta.cginc---------------


#ifndef UNITY_STANDARD_META_INCLUDED
#define UNITY_STANDARD_META_INCLUDED

// Functionality for Standard shader "meta" pass
// (extracts albedo/emission for lightmapper etc.)

#include "UnityCG.cginc"
#include "UnityStandardInput.cginc"
#include "UnityMetaPass.cginc"
#include "UnityStandardCore.cginc"

struct v2f_meta
{
    float4 pos      : SV_POSITION;
    float4 uv       : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
    float2 vizUV        : TEXCOORD1;
    float4 lightCoord   : TEXCOORD2;
#endif
};

v2f_meta vert_meta (VertexInput v)
{
    v2f_meta o;
    o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
    o.uv = TexCoords(v);
#ifdef EDITOR_VISUALIZATION
    o.vizUV = 0;
    o.lightCoord = 0;
    if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
        o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.uv0.xy, v.uv1.xy, v.uv2.xy, unity_EditorViz_Texture_ST);
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        o.vizUV = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
    }
#endif
    return o;
}

// Albedo for lightmapping should basically be diffuse color.
// But rough metals (black diffuse) still scatter quite a lot of light around, so
// we want to take some of that into account too.
half3 UnityLightmappingAlbedo (half3 diffuse, half3 specular, half smoothness)
{
    half roughness = SmoothnessToRoughness(smoothness);
    half3 res = diffuse;
    res += specular * roughness * 0.5;
    return res;
}

float4 frag_meta (v2f_meta i) : SV_Target
{
    // we're interested in diffuse & specular colors,
    // and surface roughness to produce final albedo.
    FragmentCommonData data = UNITY_SETUP_BRDF_INPUT (i.uv);

    UnityMetaInput o;
    UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

#ifdef EDITOR_VISUALIZATION
    o.Albedo = data.diffColor;
    o.VizUV = i.vizUV;
    o.LightCoord = i.lightCoord;
#else
    o.Albedo = UnityLightmappingAlbedo (data.diffColor, data.specColor, data.smoothness);
#endif
    o.SpecularColor = data.specColor;
    o.Emission = Emission(i.uv.xy);

    return UnityMetaFragment(o);
}

#endif // UNITY_STANDARD_META_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardMeta.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardParticleEditor.cginc---------------


#ifndef UNITY_STANDARD_PARTICLE_EDITOR_INCLUDED
#define UNITY_STANDARD_PARTICLE_EDITOR_INCLUDED

#if _REQUIRE_UV2
#define _FLIPBOOK_BLENDING 1
#endif

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardParticleInstancing.cginc"

#ifdef _ALPHATEST_ON
half        _Cutoff;
#endif
sampler2D   _MainTex;
float4      _MainTex_ST;

float _ObjectId;
float _PassValue;
float4 _SelectionID;
uniform float _SelectionAlphaCutoff;

struct VertexInput
{
    float4 vertex   : POSITION;
    float3 normal   : NORMAL;
    fixed4 color    : COLOR;
    #if defined(_FLIPBOOK_BLENDING) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
        float4 texcoords : TEXCOORD0;
        float texcoordBlend : TEXCOORD1;
    #else
        float2 texcoords : TEXCOORD0;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float2 texcoord : TEXCOORD0;
    #ifdef _FLIPBOOK_BLENDING
        float3 texcoord2AndBlend : TEXCOORD1;
    #endif
    fixed4 color : TEXCOORD2;
};

void vertEditorPass(VertexInput v, out VertexOutput o, out float4 opos : SV_POSITION)
{
    UNITY_SETUP_INSTANCE_ID(v);

    opos = UnityObjectToClipPos(v.vertex);

    #ifdef _FLIPBOOK_BLENDING
        #ifdef UNITY_PARTICLE_INSTANCING_ENABLED
            vertInstancingUVs(v.texcoords.xy, o.texcoord, o.texcoord2AndBlend);
        #else
            o.texcoord = v.texcoords.xy;
            o.texcoord2AndBlend.xy = v.texcoords.zw;
            o.texcoord2AndBlend.z = v.texcoordBlend;
        #endif
    #else
        #ifdef UNITY_PARTICLE_INSTANCING_ENABLED
            vertInstancingUVs(v.texcoords.xy, o.texcoord);
            o.texcoord = TRANSFORM_TEX(o.texcoord, _MainTex);
        #else
            o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
        #endif
    #endif
    o.color = v.color;
}

void fragSceneClip(VertexOutput i)
{
    half alpha = tex2D(_MainTex, i.texcoord).a;
#ifdef _FLIPBOOK_BLENDING
    half alpha2 = tex2D(_MainTex, i.texcoord2AndBlend.xy);
    alpha = lerp(alpha, alpha2, i.texcoord2AndBlend.z);
#endif
    alpha *= i.color.a;

#ifdef _ALPHATEST_ON
    clip(alpha - _Cutoff);
#endif
}

half4 fragSceneHighlightPass(VertexOutput i) : SV_Target
{
    fragSceneClip(i);
    return float4(_ObjectId, _PassValue, 1, 1);
}

half4 fragScenePickingPass(VertexOutput i) : SV_Target
{
    fragSceneClip(i);
    return _SelectionID;
}

#endif // UNITY_STANDARD_PARTICLE_EDITOR_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardParticleEditor.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardParticleInstancing.cginc---------------


#ifndef UNITY_STANDARD_PARTICLE_INSTANCING_INCLUDED
#define UNITY_STANDARD_PARTICLE_INSTANCING_INCLUDED

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && !defined(SHADER_TARGET_SURFACE_ANALYSIS)
#define UNITY_PARTICLE_INSTANCING_ENABLED
#endif

#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)

#ifndef UNITY_PARTICLE_INSTANCE_DATA
#define UNITY_PARTICLE_INSTANCE_DATA DefaultParticleInstanceData
#endif

struct DefaultParticleInstanceData
{
    float3x4 transform;
    uint color;
    float animFrame;
};

StructuredBuffer<UNITY_PARTICLE_INSTANCE_DATA> unity_ParticleInstanceData;
float4 unity_ParticleUVShiftData;
float unity_ParticleUseMeshColors;

void vertInstancingMatrices(out float4x4 objectToWorld, out float4x4 worldToObject)
{
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

    // transform matrix
    objectToWorld._11_21_31_41 = float4(data.transform._11_21_31, 0.0f);
    objectToWorld._12_22_32_42 = float4(data.transform._12_22_32, 0.0f);
    objectToWorld._13_23_33_43 = float4(data.transform._13_23_33, 0.0f);
    objectToWorld._14_24_34_44 = float4(data.transform._14_24_34, 1.0f);

    // inverse transform matrix
    float3x3 w2oRotation;
    w2oRotation[0] = objectToWorld[1].yzx * objectToWorld[2].zxy - objectToWorld[1].zxy * objectToWorld[2].yzx;
    w2oRotation[1] = objectToWorld[0].zxy * objectToWorld[2].yzx - objectToWorld[0].yzx * objectToWorld[2].zxy;
    w2oRotation[2] = objectToWorld[0].yzx * objectToWorld[1].zxy - objectToWorld[0].zxy * objectToWorld[1].yzx;

    float det = dot(objectToWorld[0].xyz, w2oRotation[0]);

    w2oRotation = transpose(w2oRotation);

    w2oRotation *= rcp(det);

    float3 w2oPosition = mul(w2oRotation, -objectToWorld._14_24_34);

    worldToObject._11_21_31_41 = float4(w2oRotation._11_21_31, 0.0f);
    worldToObject._12_22_32_42 = float4(w2oRotation._12_22_32, 0.0f);
    worldToObject._13_23_33_43 = float4(w2oRotation._13_23_33, 0.0f);
    worldToObject._14_24_34_44 = float4(w2oPosition, 1.0f);
}

void vertInstancingSetup()
{
    vertInstancingMatrices(unity_ObjectToWorld, unity_WorldToObject);
}

void vertInstancingColor(inout fixed4 color)
{
#ifndef UNITY_PARTICLE_INSTANCE_DATA_NO_COLOR
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];
    color = lerp(fixed4(1.0f, 1.0f, 1.0f, 1.0f), color, unity_ParticleUseMeshColors);
    color *= float4(data.color & 255, (data.color >> 8) & 255, (data.color >> 16) & 255, (data.color >> 24) & 255) * (1.0f / 255);
#endif
}

void vertInstancingUVs(in float2 uv, out float2 texcoord, out float3 texcoord2AndBlend)
{
    if (unity_ParticleUVShiftData.x != 0.0f)
    {
        UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

        float numTilesX = unity_ParticleUVShiftData.y;
        float2 animScale = unity_ParticleUVShiftData.zw;
#ifdef UNITY_PARTICLE_INSTANCE_DATA_NO_ANIM_FRAME
        float sheetIndex = 0.0f;
#else
        float sheetIndex = data.animFrame;
#endif

        float index0 = floor(sheetIndex);
        float vIdx0 = floor(index0 / numTilesX);
        float uIdx0 = floor(index0 - vIdx0 * numTilesX);
        float2 offset0 = float2(uIdx0 * animScale.x, (1.0f - animScale.y) - vIdx0 * animScale.y);

        texcoord = uv * animScale.xy + offset0.xy;

#ifdef _FLIPBOOK_BLENDING
        float index1 = floor(sheetIndex + 1.0f);
        float vIdx1 = floor(index1 / numTilesX);
        float uIdx1 = floor(index1 - vIdx1 * numTilesX);
        float2 offset1 = float2(uIdx1 * animScale.x, (1.0f - animScale.y) - vIdx1 * animScale.y);

        texcoord2AndBlend.xy = uv * animScale.xy + offset1.xy;
        texcoord2AndBlend.z = frac(sheetIndex);
#else
        texcoord2AndBlend.xy = texcoord;
        texcoord2AndBlend.z = 0.0f;
#endif
    }
    else
    {
        texcoord = uv;
        texcoord2AndBlend.xy = uv;
        texcoord2AndBlend.z = 0.0f;
    }
}

void vertInstancingUVs(in float2 uv, out float2 texcoord)
{
    float3 texcoord2AndBlend = 0.0f;
    vertInstancingUVs(uv, texcoord, texcoord2AndBlend);
}

#else

void vertInstancingSetup() {}
void vertInstancingColor(inout fixed4 color) {}
void vertInstancingUVs(in float2 uv, out float2 texcoord, out float3 texcoord2AndBlend) { texcoord = 0.0f; texcoord2AndBlend = 0.0f; }
void vertInstancingUVs(in float2 uv, out float2 texcoord) { texcoord = 0.0f; }

#endif

#endif // UNITY_STANDARD_PARTICLE_INSTANCING_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardParticleInstancing.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardParticles.cginc---------------


#ifndef UNITY_STANDARD_PARTICLES_INCLUDED
#define UNITY_STANDARD_PARTICLES_INCLUDED

#if _REQUIRE_UV2
#define _FLIPBOOK_BLENDING 1
#endif

#if EFFECT_BUMP
#define _DISTORTION_ON 1
#endif

#include "HLSLSupport.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardParticleInstancing.cginc"

// Particles surface shader has a lot of variants in it, but some of those do not affect
// code generation (i.e. don't have inpact on which Input/SurfaceOutput things are read or written into).
// Surface shader analysis done during import time skips "completely identical" shader variants, so to
// help this process we'll turn off some features that we know are not affecting the inputs/outputs.
//
// If you change the logic of what the below variants do, make sure to not regress code generation though,
// e.g. compare full "show generated code" output of the surface shader before & after the change.
#if defined(SHADER_TARGET_SURFACE_ANALYSIS)
    // All these only alter the color in various ways
    #undef _COLOROVERLAY_ON
    #undef _COLORCOLOR_ON
    #undef _COLORADDSUBDIFF_ON
    #undef _ALPHAMODULATE_ON
    #undef _ALPHATEST_ON

    // For inputs/outputs analysis SoftParticles and Fading are identical; so make sure to only keep one
    // of them ever defined.
    #if defined(SOFTPARTICLES_ON)
        #undef SOFTPARTICLES_ON
        #define _FADING_ON
    #endif
#endif


// Vertex shader input
struct appdata_particles
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    fixed4 color : COLOR;
    #if defined(_FLIPBOOK_BLENDING) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    float4 texcoords : TEXCOORD0;
    float texcoordBlend : TEXCOORD1;
    #else
    float2 texcoords : TEXCOORD0;
    #endif
    #if defined(_NORMALMAP)
    float4 tangent : TANGENT;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Surface shader input
struct Input
{
    float4 color : COLOR;
    float2 texcoord;
    #if defined(_FLIPBOOK_BLENDING)
    float3 texcoord2AndBlend;
    #endif
    #if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
    float4 projectedPosition;
    #endif
    #if _DISTORTION_ON
    float4 grabPassPosition;
    #endif
};

// Non-surface shader v2f structure
struct VertexOutput
{
    float4 vertex : SV_POSITION;
    float4 color : COLOR;
    UNITY_FOG_COORDS(0)
    float2 texcoord : TEXCOORD1;
    #if defined(_FLIPBOOK_BLENDING)
    float3 texcoord2AndBlend : TEXCOORD2;
    #endif
    #if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
    float4 projectedPosition : TEXCOORD3;
    #endif
    #if _DISTORTION_ON
    float4 grabPassPosition : TEXCOORD4;
    #endif
    UNITY_VERTEX_OUTPUT_STEREO

};

fixed4 readTexture(sampler2D tex, Input IN)
{
    fixed4 color = tex2D (tex, IN.texcoord);
    #ifdef _FLIPBOOK_BLENDING
    fixed4 color2 = tex2D(tex, IN.texcoord2AndBlend.xy);
    color = lerp(color, color2, IN.texcoord2AndBlend.z);
    #endif
    return color;
}

fixed4 readTexture(sampler2D tex, VertexOutput IN)
{
    fixed4 color = tex2D (tex, IN.texcoord);
    #ifdef _FLIPBOOK_BLENDING
    fixed4 color2 = tex2D(tex, IN.texcoord2AndBlend.xy);
    color = lerp(color, color2, IN.texcoord2AndBlend.z);
    #endif
    return color;
}

sampler2D _MainTex;
float4 _MainTex_ST;
half4 _Color;
sampler2D _BumpMap;
half _BumpScale;
sampler2D _EmissionMap;
half3 _EmissionColor;
sampler2D _MetallicGlossMap;
half _Metallic;
half _Glossiness;
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
float4 _SoftParticleFadeParams;
float4 _CameraFadeParams;
half _Cutoff;
int _DstBlend;

#define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x
#define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y

#define CAMERA_NEAR_FADE _CameraFadeParams.x
#define CAMERA_INV_FADE_DISTANCE _CameraFadeParams.y

#if (defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)) && !SHADER_TARGET_SURFACE_ANALYSIS
    #define SampleGrabPassTexture(tex, t) UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, t.xy / t.w)
#else
    #define SampleGrabPassTexture(tex, t) tex2Dproj(tex, t)
#endif


#if _DISTORTION_ON
    #if SHADER_TARGET_SURFACE_ANALYSIS
        sampler2D _GrabTexture;
    #else
        UNITY_DECLARE_SCREENSPACE_TEXTURE(_GrabTexture);
    #endif
    half _DistortionStrengthScaled;
    half _DistortionBlend;
#endif

#if defined (_COLORADDSUBDIFF_ON)
half4 _ColorAddSubDiff;
#endif

#if defined(_COLORCOLOR_ON)
half3 RGBtoHSV(half3 arg1)
{
    half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    half4 P = lerp(half4(arg1.bg, K.wz), half4(arg1.gb, K.xy), step(arg1.b, arg1.g));
    half4 Q = lerp(half4(P.xyw, arg1.r), half4(arg1.r, P.yzx), step(P.x, arg1.r));
    half D = Q.x - min(Q.w, Q.y);
    half E = 1e-4;
    return half3(abs(Q.z + (Q.w - Q.y) / (6.0 * D + E)), D / (Q.x + E), Q.x);
}

half3 HSVtoRGB(half3 arg1)
{
    half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    half3 P = abs(frac(arg1.xxx + K.xyz) * 6.0 - K.www);
    return arg1.z * lerp(K.xxx, saturate(P - K.xxx), arg1.y);
}
#endif

// Color function
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
#define vertColor(c) \
        vertInstancingColor(c);
#else
#define vertColor(c)
#endif

// Flipbook vertex function
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    #if defined(_FLIPBOOK_BLENDING)
    #define vertTexcoord(v, o) \
        vertInstancingUVs(v.texcoords.xy, o.texcoord, o.texcoord2AndBlend);
    #else
    #define vertTexcoord(v, o) \
        vertInstancingUVs(v.texcoords.xy, o.texcoord); \
        o.texcoord = TRANSFORM_TEX(o.texcoord, _MainTex);
    #endif
#else
    #if defined(_FLIPBOOK_BLENDING)
    #define vertTexcoord(v, o) \
        o.texcoord = v.texcoords.xy; \
        o.texcoord2AndBlend.xy = v.texcoords.zw; \
        o.texcoord2AndBlend.z = v.texcoordBlend;
    #else
    #define vertTexcoord(v, o) \
        o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
    #endif
#endif

// Fading vertex function
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
#define vertFading(o) \
    o.projectedPosition = ComputeScreenPos (clipPosition); \
    COMPUTE_EYEDEPTH(o.projectedPosition.z);
#else
#define vertFading(o)
#endif

// Distortion vertex function
#if _DISTORTION_ON
#define vertDistortion(o) \
    o.grabPassPosition = ComputeGrabScreenPos (clipPosition);
#else
#define vertDistortion(o)
#endif

// Color blending fragment function
#if defined(_COLOROVERLAY_ON)
#define fragColorMode(i) \
    albedo.rgb = lerp(1 - 2 * (1 - albedo.rgb) * (1 - i.color.rgb), 2 * albedo.rgb * i.color.rgb, step(albedo.rgb, 0.5)); \
    albedo.a *= i.color.a;
#elif defined(_COLORCOLOR_ON)
#define fragColorMode(i) \
    half3 aHSL = RGBtoHSV(albedo.rgb); \
    half3 bHSL = RGBtoHSV(i.color.rgb); \
    half3 rHSL = fixed3(bHSL.x, bHSL.y, aHSL.z); \
    albedo = fixed4(HSVtoRGB(rHSL), albedo.a * i.color.a);
#elif defined(_COLORADDSUBDIFF_ON)
#define fragColorMode(i) \
    albedo.rgb = albedo.rgb + i.color.rgb * _ColorAddSubDiff.x; \
    albedo.rgb = lerp(albedo.rgb, abs(albedo.rgb), _ColorAddSubDiff.y); \
    albedo.a *= i.color.a;
#else
#define fragColorMode(i) \
    albedo *= i.color;
#endif

// Pre-multiplied alpha helper
#if defined(_ALPHAPREMULTIPLY_ON)
#define ALBEDO_MUL albedo
#else
#define ALBEDO_MUL albedo.a
#endif

// Soft particles fragment function
#if defined(SOFTPARTICLES_ON) && defined(_FADING_ON)
#define fragSoftParticles(i) \
    float softParticlesFade = 1.0f; \
    if (SOFT_PARTICLE_NEAR_FADE > 0.0 || SOFT_PARTICLE_INV_FADE_DISTANCE > 0.0) \
    { \
        float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projectedPosition))); \
        softParticlesFade = saturate (SOFT_PARTICLE_INV_FADE_DISTANCE * ((sceneZ - SOFT_PARTICLE_NEAR_FADE) - i.projectedPosition.z)); \
        ALBEDO_MUL *= softParticlesFade; \
    }
#else
#define fragSoftParticles(i) \
    float softParticlesFade = 1.0f;
#endif

// Camera fading fragment function
#if defined(_FADING_ON)
#define fragCameraFading(i) \
    float cameraFade = saturate((i.projectedPosition.z - CAMERA_NEAR_FADE) * CAMERA_INV_FADE_DISTANCE); \
    ALBEDO_MUL *= cameraFade;
#else
#define fragCameraFading(i) \
    float cameraFade = 1.0f;
#endif

#if _DISTORTION_ON
#define fragDistortion(i) \
    float4 grabPosUV = UNITY_PROJ_COORD(i.grabPassPosition); \
    grabPosUV.xy += normal.xy * _DistortionStrengthScaled * albedo.a; \
half3 grabPass = SampleGrabPassTexture(_GrabTexture, grabPosUV).rgb; \
albedo.rgb = lerp(grabPass, albedo.rgb, saturate(albedo.a - _DistortionBlend));
#else
#define fragDistortion(i)
#endif

void vert (inout appdata_particles v, out Input o)
{
    UNITY_INITIALIZE_OUTPUT(Input, o);
    float4 clipPosition = UnityObjectToClipPos(v.vertex);

    vertColor(v.color);
    vertTexcoord(v, o);
    vertFading(o);
    vertDistortion(o);
}

void surf (Input IN, inout SurfaceOutputStandard o)
{
    half4 albedo = readTexture (_MainTex, IN);
    albedo *= _Color;

    fragColorMode(IN);
    fragSoftParticles(IN);
    fragCameraFading(IN);

    #if defined(_METALLICGLOSSMAP)
    fixed2 metallicGloss = readTexture (_MetallicGlossMap, IN).ra * fixed2(1.0, _Glossiness);
    #else
    fixed2 metallicGloss = fixed2(_Metallic, _Glossiness);
    #endif

    #if defined(_NORMALMAP)
    float3 normal = normalize (UnpackScaleNormal (readTexture (_BumpMap, IN), _BumpScale));
    #else
    float3 normal = float3(0,0,1);
    #endif

    #if defined(_EMISSION)
    half3 emission = readTexture (_EmissionMap, IN).rgb * cameraFade * softParticlesFade;
    #else
    half3 emission = 0;
    #endif

    fragDistortion(IN);

    o.Albedo = albedo.rgb;
    #if defined(_NORMALMAP)
    o.Normal = normal;
    #endif
    o.Emission = emission * _EmissionColor;
    o.Metallic = metallicGloss.r;
    o.Smoothness = metallicGloss.g;

    #if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAOVERLAY_ON)
    o.Alpha = albedo.a;
    #else
    o.Alpha = 1;
    #endif

    #if defined(_ALPHAMODULATE_ON)
    o.Albedo = lerp(half3(1.0, 1.0, 1.0), albedo.rgb, albedo.a);
    #endif

    #if defined(_ALPHATEST_ON)
    clip (albedo.a - _Cutoff + 0.0001);
    #endif
}

void vertParticleUnlit (appdata_particles v, out VertexOutput o)
{
    UNITY_SETUP_INSTANCE_ID(v);

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 clipPosition = UnityObjectToClipPos(v.vertex);
    o.vertex = clipPosition;
    o.color = v.color;

    vertColor(o.color);
    vertTexcoord(v, o);
    vertFading(o);
    vertDistortion(o);

    UNITY_TRANSFER_FOG(o, o.vertex);
}

half4 fragParticleUnlit(VertexOutput IN) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
    half4 albedo = readTexture (_MainTex, IN);
    albedo *= _Color;

    fragColorMode(IN);
    fragSoftParticles(IN);
    fragCameraFading(IN);

    #if defined(_NORMALMAP)
    float3 normal = normalize (UnpackScaleNormal (readTexture (_BumpMap, IN), _BumpScale));
    #else
    float3 normal = float3(0,0,1);
    #endif

    #if defined(_EMISSION)
    half3 emission = readTexture (_EmissionMap, IN).rgb;
    #else
    half3 emission = 0;
    #endif

    fragDistortion(IN);

    half4 result = albedo;

    #if defined(_ALPHAMODULATE_ON)
    result.rgb = lerp(half3(1.0, 1.0, 1.0), albedo.rgb, albedo.a);
    #endif

    result.rgb += emission * _EmissionColor * cameraFade * softParticlesFade;

    #if !defined(_ALPHABLEND_ON) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_ALPHAOVERLAY_ON)
    result.a = 1;
    #endif

    #if defined(_ALPHATEST_ON)
    clip (albedo.a - _Cutoff + 0.0001);
    #endif

    #if defined(_ALPHAMODULATE_ON)
    UNITY_APPLY_FOG_COLOR(IN.fogCoord, result, fixed4(1, 1, 1, 0));         // modulate - fog to white color
    #elif !defined(_ALPHATEST_ON) && defined(_ALPHABLEND_ON) && !defined(_ALPHAPREMULTIPLY_ON)
    if (_DstBlend == 1)
    {
        UNITY_APPLY_FOG_COLOR(IN.fogCoord, result, fixed4(0, 0, 0, 0));     // additive - fog to black color
    }
    else
    {
        UNITY_APPLY_FOG(IN.fogCoord, result);                               // fade - normal fog
    }
    #else
    UNITY_APPLY_FOG(IN.fogCoord, result);                                   // opaque - normal fog
    #endif

    return result;
}

#endif // UNITY_STANDARD_PARTICLES_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardParticles.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardParticleShadow.cginc---------------


#ifndef UNITY_STANDARD_PARTICLE_SHADOW_INCLUDED
#define UNITY_STANDARD_PARTICLE_SHADOW_INCLUDED

// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)

#if _REQUIRE_UV2
#define _FLIPBOOK_BLENDING 1
#endif

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardParticleInstancing.cginc"

#if (defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)) && defined(UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS)
    #define UNITY_STANDARD_USE_DITHER_MASK 1
#endif

// Need to output UVs in shadow caster, since we need to sample texture and do clip/dithering based on it
#if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define UNITY_STANDARD_USE_SHADOW_UVS 1
#endif

// Has a non-empty shadow caster output struct (it's an error to have empty structs on some platforms...)
#if !defined(V2F_SHADOW_CASTER_NOPOS_IS_EMPTY) || defined(UNITY_STANDARD_USE_SHADOW_UVS)
#define UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT 1
#endif

#ifdef UNITY_STEREO_INSTANCING_ENABLED
#define UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT 1
#endif

#ifdef _ALPHATEST_ON
half        _Cutoff;
#endif
sampler2D   _MainTex;
float4      _MainTex_ST;
#ifdef UNITY_STANDARD_USE_DITHER_MASK
sampler3D   _DitherMaskLOD;
#endif

// Handle PremultipliedAlpha from Fade or Transparent shading mode
half        _Metallic;
#ifdef _METALLICGLOSSMAP
sampler2D   _MetallicGlossMap;
#endif

half MetallicSetup_ShadowGetOneMinusReflectivity(half2 uv)
{
    half metallicity = _Metallic;
    #ifdef _METALLICGLOSSMAP
        metallicity = tex2D(_MetallicGlossMap, uv).r;
    #endif
    return OneMinusReflectivityFromMetallic(metallicity);
}

struct VertexInput
{
    float4 vertex   : POSITION;
    float3 normal   : NORMAL;
    fixed4 color    : COLOR;
    #if defined(_FLIPBOOK_BLENDING) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
        float4 texcoords : TEXCOORD0;
        float texcoordBlend : TEXCOORD1;
    #else
        float2 texcoords : TEXCOORD0;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
struct VertexOutputShadowCaster
{
    V2F_SHADOW_CASTER_NOPOS
    #ifdef UNITY_STANDARD_USE_SHADOW_UVS
        float2 texcoord : TEXCOORD1;
        #ifdef _FLIPBOOK_BLENDING
            float3 texcoord2AndBlend : TEXCOORD2;
        #endif
        fixed4 color : TEXCOORD3;
    #endif
};
#endif

#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
struct VertexOutputStereoShadowCaster
{
    UNITY_VERTEX_OUTPUT_STEREO
};
#endif

// We have to do these dances of outputting SV_POSITION separately from the vertex shader,
// and inputting VPOS in the pixel shader, since they both map to "POSITION" semantic on
// some platforms, and then things don't go well.


void vertParticleShadowCaster (VertexInput v,
    #ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    out VertexOutputShadowCaster o,
    #endif
    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
    out VertexOutputStereoShadowCaster os,
    #endif
    out float4 opos : SV_POSITION)
{
    UNITY_SETUP_INSTANCE_ID(v);
    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(os);
    #endif
    TRANSFER_SHADOW_CASTER_NOPOS(o,opos)
    #ifdef UNITY_STANDARD_USE_SHADOW_UVS
        #ifdef _FLIPBOOK_BLENDING
            #ifdef UNITY_PARTICLE_INSTANCING_ENABLED
                vertInstancingUVs(v.texcoords.xy, o.texcoord, o.texcoord2AndBlend);
            #else
                o.texcoord = v.texcoords.xy;
                o.texcoord2AndBlend.xy = v.texcoords.zw;
                o.texcoord2AndBlend.z = v.texcoordBlend;
            #endif
        #else
            #ifdef UNITY_PARTICLE_INSTANCING_ENABLED
                vertInstancingUVs(v.texcoords.xy, o.texcoord);
                o.texcoord = TRANSFORM_TEX(o.texcoord, _MainTex);
            #else
                o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
            #endif
        #endif
        o.color = v.color;
    #endif
}

half4 fragParticleShadowCaster (
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    VertexOutputShadowCaster i
#endif
#ifdef UNITY_STANDARD_USE_DITHER_MASK
    , UNITY_VPOS_TYPE vpos : VPOS
#endif
    ) : SV_Target
{
    #ifdef UNITY_STANDARD_USE_SHADOW_UVS
        half alpha = tex2D(_MainTex, i.texcoord).a;
        #ifdef _FLIPBOOK_BLENDING
            half alpha2 = tex2D(_MainTex, i.texcoord2AndBlend.xy).a;
            alpha = lerp(alpha, alpha2, i.texcoord2AndBlend.z);
        #endif
        alpha *= i.color.a;

        #ifdef _ALPHATEST_ON
            clip (alpha - _Cutoff);
        #endif
        #if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
            #ifdef _ALPHAPREMULTIPLY_ON
                half outModifiedAlpha;
                PreMultiplyAlpha(half3(0, 0, 0), alpha, MetallicSetup_ShadowGetOneMinusReflectivity(i.texcoord), outModifiedAlpha);
                alpha = outModifiedAlpha;
            #endif
            #ifdef UNITY_STANDARD_USE_DITHER_MASK
                // Use dither mask for alpha blended shadows, based on pixel position xy
                // and alpha level. Our dither texture is 4x4x16.
                half alphaRef = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25,alpha*0.9375)).a;
                clip (alphaRef - 0.01);
            #else
                clip (alpha - 0.5);
            #endif
        #endif
    #endif // UNITY_STANDARD_USE_SHADOW_UVS)

    SHADOW_CASTER_FRAGMENT(i)
}

#endif // UNITY_STANDARD_PARTICLE_SHADOW_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardParticleShadow.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardShadow.cginc---------------


#ifndef UNITY_STANDARD_SHADOW_INCLUDED
#define UNITY_STANDARD_SHADOW_INCLUDED

// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)


#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityStandardUtils.cginc"

#if (defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)) && defined(UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS)
    #define UNITY_STANDARD_USE_DITHER_MASK 1
#endif

// Need to output UVs in shadow caster, since we need to sample texture and do clip/dithering based on it
#if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define UNITY_STANDARD_USE_SHADOW_UVS 1
#endif

// Has a non-empty shadow caster output struct (it's an error to have empty structs on some platforms...)
#if !defined(V2F_SHADOW_CASTER_NOPOS_IS_EMPTY) || defined(UNITY_STANDARD_USE_SHADOW_UVS)
#define UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT 1
#endif

#ifdef UNITY_STEREO_INSTANCING_ENABLED
#define UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT 1
#endif


half4       _Color;
half        _Cutoff;
sampler2D   _MainTex;
float4      _MainTex_ST;
#ifdef UNITY_STANDARD_USE_DITHER_MASK
sampler3D   _DitherMaskLOD;
#endif

// Handle PremultipliedAlpha from Fade or Transparent shading mode
half4       _SpecColor;
half        _Metallic;
#ifdef _SPECGLOSSMAP
sampler2D   _SpecGlossMap;
#endif
#ifdef _METALLICGLOSSMAP
sampler2D   _MetallicGlossMap;
#endif

#if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
sampler2D   _ParallaxMap;
half        _Parallax;
#endif

half MetallicSetup_ShadowGetOneMinusReflectivity(half2 uv)
{
    half metallicity = _Metallic;
    #ifdef _METALLICGLOSSMAP
        metallicity = tex2D(_MetallicGlossMap, uv).r;
    #endif
    return OneMinusReflectivityFromMetallic(metallicity);
}

half RoughnessSetup_ShadowGetOneMinusReflectivity(half2 uv)
{
    half metallicity = _Metallic;
#ifdef _METALLICGLOSSMAP
    metallicity = tex2D(_MetallicGlossMap, uv).r;
#endif
    return OneMinusReflectivityFromMetallic(metallicity);
}

half SpecularSetup_ShadowGetOneMinusReflectivity(half2 uv)
{
    half3 specColor = _SpecColor.rgb;
    #ifdef _SPECGLOSSMAP
        specColor = tex2D(_SpecGlossMap, uv).rgb;
    #endif
    return (1 - SpecularStrength(specColor));
}

// SHADOW_ONEMINUSREFLECTIVITY(): workaround to get one minus reflectivity based on UNITY_SETUP_BRDF_INPUT
#define SHADOW_JOIN2(a, b) a##b
#define SHADOW_JOIN(a, b) SHADOW_JOIN2(a,b)
#define SHADOW_ONEMINUSREFLECTIVITY SHADOW_JOIN(UNITY_SETUP_BRDF_INPUT, _ShadowGetOneMinusReflectivity)

struct VertexInput
{
    float4 vertex   : POSITION;
    float3 normal   : NORMAL;
    float2 uv0      : TEXCOORD0;
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS) && defined(_PARALLAXMAP)
        half4 tangent   : TANGENT;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    UNITY_POSITION(pos);
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
struct VertexOutputShadowCaster
{
    V2F_SHADOW_CASTER_NOPOS
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
        float2 tex : TEXCOORD1;

        #if defined(_PARALLAXMAP)
            half3 viewDirForParallax : TEXCOORD2;
        #endif
    #endif
};
#endif

#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
struct VertexOutputStereoShadowCaster
{
    UNITY_VERTEX_OUTPUT_STEREO
};
#endif

// We have to do these dances of outputting SV_POSITION separately from the vertex shader,
// and inputting VPOS in the pixel shader, since they both map to "POSITION" semantic on
// some platforms, and then things don't go well.


void vertShadowCaster (VertexInput v
    , out VertexOutput output
    #ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    , out VertexOutputShadowCaster o
    #endif
    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
    , out VertexOutputStereoShadowCaster os
    #endif
)
{
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, output);

    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(os);
    #endif
    TRANSFER_SHADOW_CASTER_NOPOS(o, output.pos)
    #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
        o.tex = TRANSFORM_TEX(v.uv0, _MainTex);

        #ifdef _PARALLAXMAP
            TANGENT_SPACE_ROTATION;
            o.viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
        #endif
    #endif
}

half4 fragShadowCaster (VertexOutput input
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    , VertexOutputShadowCaster i
#endif
) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
        #if defined(_PARALLAXMAP) && (SHADER_TARGET >= 30)
            half3 viewDirForParallax = normalize(i.viewDirForParallax);
            fixed h = tex2D (_ParallaxMap, i.tex.xy).g;
            half2 offset = ParallaxOffset1Step (h, _Parallax, viewDirForParallax);
            i.tex.xy += offset;
        #endif

        #if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
            half alpha = _Color.a;
        #else
            half alpha = tex2D(_MainTex, i.tex.xy).a * _Color.a;
        #endif
        #if defined(_ALPHATEST_ON)
            clip (alpha - _Cutoff);
        #endif
        #if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
            #if defined(_ALPHAPREMULTIPLY_ON)
                half outModifiedAlpha;
                PreMultiplyAlpha(half3(0, 0, 0), alpha, SHADOW_ONEMINUSREFLECTIVITY(i.tex), outModifiedAlpha);
                alpha = outModifiedAlpha;
            #endif
            #if defined(UNITY_STANDARD_USE_DITHER_MASK)
                // Use dither mask for alpha blended shadows, based on pixel position xy
                // and alpha level. Our dither texture is 4x4x16.
                #ifdef LOD_FADE_CROSSFADE
                    #define _LOD_FADE_ON_ALPHA
                    alpha *= unity_LODFade.y;
                #endif
                half alphaRef = tex3D(_DitherMaskLOD, float3(input.pos.xy*0.25,alpha*0.9375)).a;
                clip (alphaRef - 0.01);
            #else
                clip (alpha - _Cutoff);
            #endif
        #endif
    #endif // #if defined(UNITY_STANDARD_USE_SHADOW_UVS)

    #ifdef LOD_FADE_CROSSFADE
        #ifdef _LOD_FADE_ON_ALPHA
            #undef _LOD_FADE_ON_ALPHA
        #else
            UnityApplyDitherCrossFade(input.pos.xy);
        #endif
    #endif

    SHADOW_CASTER_FRAGMENT(i)
}

#endif // UNITY_STANDARD_SHADOW_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardShadow.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardUtils.cginc---------------


#ifndef UNITY_STANDARD_UTILS_INCLUDED
#define UNITY_STANDARD_UTILS_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardConfig.cginc"

// Helper functions, maybe move into UnityCG.cginc

half SpecularStrength(half3 specular)
{
    #if (SHADER_TARGET < 30)
        // SM2.0: instruction count limitation
        // SM2.0: simplified SpecularStrength
        return specular.r; // Red channel - because most metals are either monocrhome or with redish/yellowish tint
    #else
        return max (max (specular.r, specular.g), specular.b);
    #endif
}

// Diffuse/Spec Energy conservation
inline half3 EnergyConservationBetweenDiffuseAndSpecular (half3 albedo, half3 specColor, out half oneMinusReflectivity)
{
    oneMinusReflectivity = 1 - SpecularStrength(specColor);
    #if !UNITY_CONSERVE_ENERGY
        return albedo;
    #elif UNITY_CONSERVE_ENERGY_MONOCHROME
        return albedo * oneMinusReflectivity;
    #else
        return albedo * (half3(1,1,1) - specColor);
    #endif
}

inline half OneMinusReflectivityFromMetallic(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in unity_ColorSpaceDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = unity_ColorSpaceDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

inline half3 DiffuseAndSpecularFromMetallic (half3 albedo, half metallic, out half3 specColor, out half oneMinusReflectivity)
{
    specColor = lerp (unity_ColorSpaceDielectricSpec.rgb, albedo, metallic);
    oneMinusReflectivity = OneMinusReflectivityFromMetallic(metallic);
    return albedo * oneMinusReflectivity;
}

inline half3 PreMultiplyAlpha (half3 diffColor, half alpha, half oneMinusReflectivity, out half outModifiedAlpha)
{
    #if defined(_ALPHAPREMULTIPLY_ON)
        // NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)

        // Transparency 'removes' from Diffuse component
        diffColor *= alpha;

        #if (SHADER_TARGET < 30)
            // SM2.0: instruction count limitation
            // Instead will sacrifice part of physically based transparency where amount Reflectivity is affecting Transparency
            // SM2.0: uses unmodified alpha
            outModifiedAlpha = alpha;
        #else
            // Reflectivity 'removes' from the rest of components, including Transparency
            // outAlpha = 1-(1-alpha)*(1-reflectivity) = 1-(oneMinusReflectivity - alpha*oneMinusReflectivity) =
            //          = 1-oneMinusReflectivity + alpha*oneMinusReflectivity
            outModifiedAlpha = 1-oneMinusReflectivity + alpha*oneMinusReflectivity;
        #endif
    #else
        outModifiedAlpha = alpha;
    #endif
    return diffColor;
}

// Same as ParallaxOffset in Unity CG, except:
//  *) precision - half instead of float
half2 ParallaxOffset1Step (half h, half height, half3 viewDir)
{
    h = h * height - height/2.0;
    half3 v = normalize(viewDir);
    v.z += 0.42;
    return h * (v.xy / v.z);
}

half LerpOneTo(half b, half t)
{
    half oneMinusT = 1 - t;
    return oneMinusT + b * t;
}

half3 LerpWhiteTo(half3 b, half t)
{
    half oneMinusT = 1 - t;
    return half3(oneMinusT, oneMinusT, oneMinusT) + b * t;
}

half3 UnpackScaleNormalDXT5nm(half4 packednormal, half bumpScale)
{
    half3 normal;
    normal.xy = (packednormal.wy * 2 - 1);
    #if (SHADER_TARGET >= 30)
        // SM2.0: instruction count limitation
        // SM2.0: normal scaler is not supported
        normal.xy *= bumpScale;
    #endif
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

half3 UnpackScaleNormalRGorAG(half4 packednormal, half bumpScale)
{
    #if defined(UNITY_NO_DXT5nm)
        half3 normal = packednormal.xyz * 2 - 1;
        #if (SHADER_TARGET >= 30)
            // SM2.0: instruction count limitation
            // SM2.0: normal scaler is not supported
            normal.xy *= bumpScale;
        #endif
        return normal;
    #elif defined(UNITY_ASTC_NORMALMAP_ENCODING)
        half3 normal;
        normal.xy = (packednormal.wy * 2 - 1);
        normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
        normal.xy *= bumpScale;
        return normal;
    #else
        // This do the trick
        packednormal.x *= packednormal.w;

        half3 normal;
        normal.xy = (packednormal.xy * 2 - 1);
        #if (SHADER_TARGET >= 30)
            // SM2.0: instruction count limitation
            // SM2.0: normal scaler is not supported
            normal.xy *= bumpScale;
        #endif
        normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
        return normal;
    #endif
}

half3 UnpackScaleNormal(half4 packednormal, half bumpScale)
{
    return UnpackScaleNormalRGorAG(packednormal, bumpScale);
}

half3 BlendNormals(half3 n1, half3 n2)
{
    return normalize(half3(n1.xy + n2.xy, n1.z*n2.z));
}

half3x3 CreateTangentToWorldPerVertex(half3 normal, half3 tangent, half tangentSign)
{
    // For odd-negative scale transforms we need to flip the sign
    half sign = tangentSign * unity_WorldTransformParams.w;
    half3 binormal = cross(normal, tangent) * sign;
    return half3x3(tangent, binormal, normal);
}

//-------------------------------------------------------------------------------------
half3 ShadeSHPerVertex (half3 normal, half3 ambient)
{
    #if UNITY_SAMPLE_FULL_SH_PER_PIXEL
        // Completely per-pixel
        // nothing to do here
    #elif (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        // Completely per-vertex
        ambient += max(half3(0,0,0), ShadeSH9 (half4(normal, 1.0)));
    #else
        // L2 per-vertex, L0..L1 & gamma-correction per-pixel

        // NOTE: SH data is always in Linear AND calculation is split between vertex & pixel
        // Convert ambient to Linear and do final gamma-correction at the end (per-pixel)
        #ifdef UNITY_COLORSPACE_GAMMA
            ambient = GammaToLinearSpace (ambient);
        #endif
        ambient += SHEvalLinearL2 (half4(normal, 1.0));     // no max since this is only L2 contribution
    #endif

    return ambient;
}

half3 ShadeSHPerPixel (half3 normal, half3 ambient, float3 worldPos)
{
    half3 ambient_contrib = 0.0;

    #if UNITY_SAMPLE_FULL_SH_PER_PIXEL
        // Completely per-pixel
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            if (unity_ProbeVolumeParams.x == 1.0)
                ambient_contrib = SHEvalLinearL0L1_SampleProbeVolume(half4(normal, 1.0), worldPos);
            else
                ambient_contrib = SHEvalLinearL0L1(half4(normal, 1.0));
        #else
            ambient_contrib = SHEvalLinearL0L1(half4(normal, 1.0));
        #endif

            ambient_contrib += SHEvalLinearL2(half4(normal, 1.0));

            ambient += max(half3(0, 0, 0), ambient_contrib);

        #ifdef UNITY_COLORSPACE_GAMMA
            ambient = LinearToGammaSpace(ambient);
        #endif
    #elif (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        // Completely per-vertex
        // nothing to do here. Gamma conversion on ambient from SH takes place in the vertex shader, see ShadeSHPerVertex.
    #else
        // L2 per-vertex, L0..L1 & gamma-correction per-pixel
        // Ambient in this case is expected to be always Linear, see ShadeSHPerVertex()
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            if (unity_ProbeVolumeParams.x == 1.0)
                ambient_contrib = SHEvalLinearL0L1_SampleProbeVolume (half4(normal, 1.0), worldPos);
            else
                ambient_contrib = SHEvalLinearL0L1 (half4(normal, 1.0));
        #else
            ambient_contrib = SHEvalLinearL0L1 (half4(normal, 1.0));
        #endif

        ambient = max(half3(0, 0, 0), ambient+ambient_contrib);     // include L2 contribution in vertex shader before clamp.
        #ifdef UNITY_COLORSPACE_GAMMA
            ambient = LinearToGammaSpace (ambient);
        #endif
    #endif

    return ambient;
}

//-------------------------------------------------------------------------------------
inline float3 BoxProjectedCubemapDirection (float3 worldRefl, float3 worldPos, float4 cubemapCenter, float4 boxMin, float4 boxMax)
{
    // Do we have a valid reflection probe?
    UNITY_BRANCH
    if (cubemapCenter.w > 0.0)
    {
        float3 nrdir = normalize(worldRefl);

        #if 1
            float3 rbmax = (boxMax.xyz - worldPos) / nrdir;
            float3 rbmin = (boxMin.xyz - worldPos) / nrdir;

            float3 rbminmax = (nrdir > 0.0f) ? rbmax : rbmin;

        #else // Optimized version
            float3 rbmax = (boxMax.xyz - worldPos);
            float3 rbmin = (boxMin.xyz - worldPos);

            float3 select = step (float3(0,0,0), nrdir);
            float3 rbminmax = lerp (rbmax, rbmin, select);
            rbminmax /= nrdir;
        #endif

        float fa = min(min(rbminmax.x, rbminmax.y), rbminmax.z);

        worldPos -= cubemapCenter.xyz;
        worldRefl = worldPos + nrdir * fa;
    }
    return worldRefl;
}


//-------------------------------------------------------------------------------------
// Derivative maps
// http://www.rorydriscoll.com/2012/01/11/derivative-maps/
// For future use.

// Project the surface gradient (dhdx, dhdy) onto the surface (n, dpdx, dpdy)
half3 CalculateSurfaceGradient(half3 n, half3 dpdx, half3 dpdy, half dhdx, half dhdy)
{
    half3 r1 = cross(dpdy, n);
    half3 r2 = cross(n, dpdx);
    return (r1 * dhdx + r2 * dhdy) / dot(dpdx, r1);
}

// Move the normal away from the surface normal in the opposite surface gradient direction
half3 PerturbNormal(half3 n, half3 dpdx, half3 dpdy, half dhdx, half dhdy)
{
    //TODO: normalize seems to be necessary when scales do go beyond the 2...-2 range, should we limit that?
    //how expensive is a normalize? Anything cheaper for this case?
    return normalize(n - CalculateSurfaceGradient(n, dpdx, dpdy, dhdx, dhdy));
}

// Calculate the surface normal using the uv-space gradient (dhdu, dhdv)
half3 CalculateSurfaceNormal(half3 position, half3 normal, half2 gradient, half2 uv)
{
    half3 dpdx = ddx(position);
    half3 dpdy = ddy(position);

    half dhdx = dot(gradient, ddx(uv));
    half dhdy = dot(gradient, ddy(uv));

    return PerturbNormal(normal, dpdx, dpdy, dhdx, dhdy);
}


#endif // UNITY_STANDARD_UTILS_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStandardUtils.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityUI.cginc---------------


#ifndef UNITY_UI_INCLUDED
#define UNITY_UI_INCLUDED

inline float UnityGet2DClipping (in float2 position, in float4 clipRect)
{
    float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
    return inside.x * inside.y;
}

inline fixed4 UnityGetUIDiffuseColor(in float2 position, in sampler2D mainTexture, in sampler2D alphaTexture, fixed4 textureSampleAdd)
{
    return fixed4(tex2D(mainTexture, position).rgb + textureSampleAdd.rgb, tex2D(alphaTexture, position).r + textureSampleAdd.a);
}

// This piecewise approximation has a precision better than 0.5 / 255 in gamma space over the [0..255] range
// i.e. abs(l2g_exact(g2l_approx(value)) - value) < 0.5 / 255
// It is much more precise than GammaToLinearSpace but remains relatively cheap
half3 UIGammaToLinear(half3 value)
{
    half3 low = 0.0849710 * value - 0.000163029;
    half3 high = value * (value * (value * 0.265885 + 0.736584) - 0.00980184) + 0.00319697;

    // We should be 0.5 away from any actual gamma value stored in an 8 bit channel
    const half3 split = (half3)0.0725490; // Equals 18.5 / 255
    return (value < split) ? low : high;
}
#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityUI.cginc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\Internal\UnityUIE.cginc---------------
.
.
#ifndef UNITY_UIE_INCLUDED
#define UNITY_UIE_INCLUDED

#define UIE_TEXTURE_SLOT_COUNT 8
#define UIE_TEXTURE_SLOT_SIZE 2

#ifndef UIE_COLORSPACE_GAMMA
    // Note: When the editor shader is compiled, UNITY_COLORSPACE_GAMMA is ALWAYS set because it is the color space
    // of the editor resources project.
    #if defined(UNITY_COLORSPACE_GAMMA) || defined(UIE_FORCE_GAMMA)
        #define UIE_COLORSPACE_GAMMA 1
    #else
        #define UIE_COLORSPACE_GAMMA 0
    #endif // UNITY_COLORSPACE_GAMMA
#endif // UIE_COLORSPACE_GAMMA

#ifndef UIE_FRAG_T
    #if UIE_COLORSPACE_GAMMA
        #define UIE_FRAG_T fixed4
    #else
        #define UIE_FRAG_T half4
    #endif // UIE_COLORSPACE_GAMMA
#endif // UIE_FRAG_T

#ifndef UIE_V2F_COLOR_T
    #if UIE_COLORSPACE_GAMMA
        #define UIE_V2F_COLOR_T fixed4
    #else
        #define UIE_V2F_COLOR_T half4
    #endif // UIE_COLORSPACE_GAMMA
#endif // UIE_V2F_COLOR_T

#ifndef UIE_NOINTERPOLATION
    #ifdef UNITY_PLATFORM_WEBGL
        // UUM-57628 Safari leaks when using nointerpolation (resulting in flat in glsl)
        #define UIE_NOINTERPOLATION
    #else
        #define UIE_NOINTERPOLATION nointerpolation
    #endif
#endif

#include "UnityCG.cginc"
#include "HLSLSupport.cginc"

UNITY_DECLARE_TEX2D(_GradientSettingsTex);
UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(_ShaderInfoTex);
float4 _TextureInfo[UIE_TEXTURE_SLOT_COUNT * UIE_TEXTURE_SLOT_SIZE];
UNITY_DECLARE_TEX2D(_Texture0);
UNITY_DECLARE_TEX2D(_Texture1);
UNITY_DECLARE_TEX2D(_Texture2);
UNITY_DECLARE_TEX2D(_Texture3);
UNITY_DECLARE_TEX2D(_Texture4);
UNITY_DECLARE_TEX2D(_Texture5);
UNITY_DECLARE_TEX2D(_Texture6);
UNITY_DECLARE_TEX2D(_Texture7);

// This piecewise approximation has a precision better than 0.5 / 255 in gamma space over the [0..255] range
// i.e. abs(l2g_exact(g2l_approx(value)) - value) < 0.5 / 255
// It is much more precise than GammaToLinearSpace but remains relatively cheap
half3 uie_gamma_to_linear(half3 value)
{
    half3 low = 0.0849710 * value - 0.000163029;
    half3 high = value * (value * (value * 0.265885 + 0.736584) - 0.00980184) + 0.00319697;

    // We should be 0.5 away from any actual gamma value stored in an 8 bit channel
    const half3 split = (half3)0.0725490; // Equals 18.5 / 255
    return (value < split) ? low : high;
}

// This piecewise approximation has a very precision veryclose to that of LinearToGammaSpaceExact but explicitly
// avoids branching
half3 uie_linear_to_gamma(half3 value)
{
    half3 low = 12.92F * value;
    half3 high =  1.055F * pow(value, 0.4166667F) - 0.055F;

    const half3 split = (half3)0.0031308;
    return (value < split) ? low : high;
}

struct appdata_t
{
    float4 vertex   : POSITION;
    float4 color    : COLOR;
    float2 uv       : TEXCOORD0;
    float4 xformClipPages : TEXCOORD1; // Top-left of xform and clip pages: XY,XY
    float4 ids      : TEXCOORD2; //XYZW (xform,clip,opacity,color/textcore)
    float4 flags    : TEXCOORD3; //X (flags) Y (textcore-dilate) Z (is-arc) W (is-dynamic-colored)
    float4 opacityColorPages : TEXCOORD4; //XY: Opacity page, ZW: color page/textcore setting
    float4 settingIndex : TEXCOORD5; // XY: SVG setting index
    float4 circle   : TEXCOORD6; // XY (outer) ZW (inner)
    float  textureId : TEXCOORD7; // X (textureId)
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    UIE_V2F_COLOR_T color : COLOR;
    float4 uvClip  : TEXCOORD0; // UV and ZW contains the relative coords within the clipping rect
    UIE_NOINTERPOLATION half4 typeTexSettings : TEXCOORD1; // X: Render Type Y: Tex Index Z: SVG Gradient Index/Text Opacity W: Is Arc (textured + solid)
#ifdef UNITY_PLATFORM_WEBGL
    // UUM-90736 Safari-WebGL hangs when using uint2 in v2f struct
    UIE_NOINTERPOLATION float2 textCoreLoc : TEXCOORD3; // Location of the TextCore data in the shader info
#else
    UIE_NOINTERPOLATION uint2 textCoreLoc : TEXCOORD3; // Location of the TextCore data in the shader info
#endif
    float4 circle : TEXCOORD4; // XY (outer) ZW (inner) | X (Text Extra Dilate)
    UNITY_VERTEX_OUTPUT_STEREO
};

static const float kUIEMeshZ = 0.0f; // Keep in track with UIRUtility.k_MeshPosZ
static const float kUIEMaskZ = 1.0f; // Keep in track with UIRUtility.k_MaskPosZ

struct TextureInfo
{
    float textureId;
    float sdfScale;
    float2 texelSize;
    float2 textureSize;
    float sharpness;
};

// index: integer between [0..UIE_TEXTURE_SLOT_COUNT[
TextureInfo GetTextureInfo(half index)
{
    half offset = index * UIE_TEXTURE_SLOT_SIZE;
    float4 data0 = _TextureInfo[offset];
    float4 data1 = _TextureInfo[offset + 1];

    TextureInfo info;
    info.textureId = data0.x;
    info.texelSize = data0.yz;
    info.sdfScale = data0.w;
    info.textureSize = data1.xy;
    info.sharpness = data1.z;

    return info;
}

// returns: Integer between 0 and UIE_TEXTURE_SLOT_COUNT - 1
half FindTextureSlot(float textureId)
{
    for (half i = 0 ; i < UIE_TEXTURE_SLOT_COUNT - 1 ; ++i)
        if (GetTextureInfo(i).textureId == textureId)
            return i;
    return UIE_TEXTURE_SLOT_COUNT - 1;
}

#define UIE_BRANCH(OP) \
    [branch] if (index < 4) \
    { \
        [branch] if (index < 2) \
        { \
            [branch] if (index < 1) \
                {OP(0)} \
            else \
                {OP(1)} \
        } \
        else \
        { \
            [branch] if (index < 3) \
                {OP(2)} \
            else \
                {OP(3)} \
        } \
    } \
    else \
    { \
        [branch] if (index < 6) \
        { \
            [branch] if (index < 5) \
                {OP(4)} \
            else \
                {OP(5)} \
        } \
        else \
        { \
            [branch] if (index < 7) \
                {OP(6)} \
            else \
                {OP(7)} \
        } \
    }

#define UIE_SAMPLE1(index) \
    result = UNITY_SAMPLE_TEX2D(_Texture##index, uv);

// index: integer between [0..UIE_TEXTURE_SLOT_COUNT[
float4 SampleTextureSlot(half index, float2 uv)
{
    float4 result;
    UIE_BRANCH(UIE_SAMPLE1)
    return result;
}

#define UIE_SAMPLE2(index) \
    result1 = UNITY_SAMPLE_TEX2D(_Texture##index, uv1); \
    result2 = UNITY_SAMPLE_TEX2D(_Texture##index, uv2);

// index: integer between [0..UIE_TEXTURE_SLOT_COUNT[
void SampleTextureSlot2(half index, float2 uv1, float2 uv2, out float4 result1, out float4 result2)
{
    UIE_BRANCH(UIE_SAMPLE2)
}

float4 ReadShaderInfo(min16uint2 location)
{
    return _ShaderInfoTex.Load(min16uint3(location, 0));
}

// Notes on UIElements Spaces (Local, Bone, Group, World and Clip)
//
// Consider the following example:
//      *     <- Clip Space (GPU Clip Coordinates)
//    Proj
//      |     <- World Space
//   VEroot
//      |
//     VE1 (RenderHint = Group)
//      |     <- Group Space
//     VE2 (RenderHint = Bone)
//      |     <- Bone Space
//     VE3
//
// A VisualElement always emits vertices in local-space. They do not embed the transform of the emitting VisualElement.
// The renderer transforms the vertices on CPU from local-space to bone space (if available), or to the group space (if available),
// or ultimately to world-space if there is no ancestor with a bone transform or group transform.
//
// The world-to-clip transform is stored in UNITY_MATRIX_P
// The group-to-world transform is stored in UNITY_MATRIX_V
// The bone-to-group transform is stored in uie_toWorldMat.
//
// In this shader, we consider that vertices are always in bone-space, and we always apply the bone-to-group and the group-to-world
// transforms. It does not matter because in the event where there is no ancestor with a Group or Bone RenderHint, these transform
// will be identities.

static float4x4 uie_toWorldMat;

// Let min and max, the bottom-left and top-right corners of the clipping rect. We want to remap our position so that we
// get -1 at min and 1 at max. The rasterizer can linearly interpolate the value and the fragment shader will interpret
// |value| > 1 as being outside the clipping rect, meaning the fragment should be discarded.
//
// min      avg      max  pos
//  |--------|--------|----|
// -1        0        1
//
// avg = (min + max) / 2
// pos'= (pos - avg) / (max - avg)
//     = pos * [1 / (max - avg)] + [- avg / (max - avg)]
//     = pos * a + b
// a   = 1 / (max - avg)
//     = 1 / [max - (min + max) / 2]
//     = 2 / (max - min)
// b   = - avg / (max - avg)
//     = -[(min + max) / 2] / [max - ((min + max) / 2)]
//     = -[min + max] / [2 * max - (min + max)]
//     = (min + max) / (min - max)
//
// a    : see above
// b    : see above
// pos  : position, in group space
float2 ComputeRelativeClipRectCoords(float2 a, float2 b, float2 pos)
{
    return pos * a + b;
}

float uie_fragment_clip(v2f IN)
{
    float2 dist = abs(IN.uvClip.zw);
    return dist.x < 1.0001f & dist.y < 1.0001f;
}

min16uint2 uie_decode_shader_info_texel_pos(float2 encodedPage, float encodedId, min16uint yStride)
{
    const min16uint kShaderInfoPageWidth = 32; // If this ever changes, adjust the DynamicColor test accordingly
    const min16uint kShaderInfoPageHeight = 8;

    min16uint id = round(encodedId * 255.0f);
    min16uint2 pageXY = round(encodedPage * 255.0f); // From [0,1] to [0,255]
    min16uint idY = id / kShaderInfoPageWidth; // Must use uint division for better performance
    min16uint idX = id - idY * kShaderInfoPageWidth;

    return min16uint2(
        pageXY.x * kShaderInfoPageWidth + idX,
        pageXY.y * kShaderInfoPageHeight + idY * yStride);
}

void uie_vert_load_dynamic_transform(appdata_t v)
{
    min16uint2 xformTexel = uie_decode_shader_info_texel_pos(v.xformClipPages.xy, v.ids.x, 3);
    min16uint2 row0Loc = xformTexel + min16uint2(0, 0);
    min16uint2 row1Loc = xformTexel + min16uint2(0, 1);
    min16uint2 row2Loc = xformTexel + min16uint2(0, 2);

    uie_toWorldMat = float4x4(
        ReadShaderInfo(row0Loc),
        ReadShaderInfo(row1Loc),
        ReadShaderInfo(row2Loc),
        float4(0, 0, 0, 1));
}

float2 uie_unpack_float2(fixed4 c)
{
    return float2(c.r*255 + c.g, c.b*255 + c.a);
}

float2 uie_ray_unit_circle_first_hit(float2 rayStart, float2 rayDir)
{
    float tca = dot(-rayStart, rayDir);
    float d2 = dot(rayStart, rayStart) - tca * tca;
    float thc = sqrt(1.0f - d2);
    float t0 = tca - thc;
    float t1 = tca + thc;
    float t = min(t0, t1);
    if (t < 0.0f)
        t = max(t0, t1);
    return rayStart + rayDir * t;
}

float uie_radial_address(float2 uv, float2 focus)
{
    uv = (uv - float2(0.5f, 0.5f)) * 2.0f;
    float2 pointOnPerimeter = uie_ray_unit_circle_first_hit(focus, normalize(uv - focus));
    float2 diff = pointOnPerimeter - focus;
    if (abs(diff.x) > 0.0001f)
        return (uv.x - focus.x) / diff.x;
    if (abs(diff.y) > 0.0001f)
        return (uv.y - focus.y) / diff.y;
    return 0.0f;
}

struct GradientLocation
{
    float2 uv;
    float4 location;
};

GradientLocation uie_sample_gradient_location(min16uint settingIndex, float2 uv)
{
    // Gradient settings are stored in 3 consecutive texels:
    // - texel 0: (float4, 1 byte per float)
    //    x = gradient type (0 = tex/linear, 1 = radial)
    //    y = address mode (0 = wrap, 1 = clamp, 2 = mirror)
    //    z = radialFocus.x
    //    w = radialFocus.y
    // - texel 1: (float2, 2 bytes per float) atlas entry position
    //    xy = pos.x
    //    zw = pos.y
    // - texel 2: (float2, 2 bytes per float) atlas entry size
    //    xy = size.x
    //    zw = size.y

    min16uint2 settingLoc = min16uint2(0, settingIndex);
    fixed4 gradSettings = _GradientSettingsTex.Load(min16uint3(settingLoc, 0));
    if (gradSettings.x > 0.0f)
    {
        // Radial texture case
        float2 focus = (gradSettings.zw - float2(0.5f, 0.5f)) * 2.0f; // bring focus in the (-1,1) range
        uv = float2(uie_radial_address(uv, focus), 0.0);
    }

    min16uint addressing = round(gradSettings.y * 255);
    uv.x = (addressing == 0) ? fmod(uv.x,1.0f) : uv.x; // Wrap
    uv.x = (addressing == 1) ? max(min(uv.x,1.0f), 0.0f) : uv.x; // Clamp
    float w = fmod(uv.x,2.0f);
    uv.x = (addressing == 2) ? (w > 1.0f ? 1.0f-fmod(w,1.0f) : w) : uv.x; // Mirror

    GradientLocation grad;
    grad.uv = uv;

    // Adjust UV to atlas position
    min16uint2 nextUV = min16uint2(1, 0);
    grad.location.xy = uie_unpack_float2(_GradientSettingsTex.Load(min16uint3(settingLoc + nextUV, 0)) * 255) + float2(0.5f, 0.5f);
    grad.location.zw = uie_unpack_float2(_GradientSettingsTex.Load(min16uint3(settingLoc + nextUV * 2, 0)) * 255);

    return grad;
}

bool fpEqual(float a, float b)
{
#if SHADER_API_GLES || SHADER_API_GLES3
    return abs(a-b) < 0.0001;
#else
    return a == b;
#endif
}

// 1 layer : Face only
// sd           : Signed distance / sdfScale + 0.5
// sdfSizeRCP   : 1 / texture width
// sdfScale     : Signed Distance Field Scale
// isoPerimeter : Dilate / Contract the shape
float sd_to_coverage(float sd, float2 uv, float sdfSizeRCP, float sdfScale, float isoPerimeter)
{
    float ta = ddx(uv.x) * ddy(uv.y) - ddy(uv.x) * ddx(uv.y);   // Texel area covered by this pixel (parallelogram area)
    float ssr = rsqrt(abs(ta)) * sdfSizeRCP;                    // Texture to Screen Space Ratio (unit is Texel/Pixel)
    sd = (sd - 0.5) * sdfScale + isoPerimeter;                  // Signed Distance to edge (in texture space)
    return saturate(0.5 + 2.0 * sd * ssr);                      // Screen pixel coverage : center + (1 / sampling radius) * signed distance
}

// 3 Layers : Face, Outline, Underlay
// sd           : Signed distance / sdfScale + 0.5
// sdfSize      : texture height
// sdfScale     : Signed Distance Field Scale
// isoPerimeter : Dilate / Contract the shape
// softness     : softness of each outer edges
// sharpness    : sharpness of the text
float3 sd_to_coverage(float3 sd, float2 uv, float sdfSize, float sdfScale, float3 isoPerimeter, float3 softness, float sharpness)
{
    // Case 1349202: The underline stretches its middle quad, making parallelogram area evaluation invalid and resulting
    //               in visual artifacts. For that reason, we can only rely on uv.y for the length ratio leading in some
    //               error when a rotation/skew/non-uniform scaling takes place.
    float ps = abs(ddx(uv.y)) + abs(ddy(uv.y));                                 // Size of a pixel in texel space (approximation)
    float stsr = sdfSize * ps;                                                  // Screen to Texture Space Ratio (unit is Pixel/Texel)
    sd = (sd - 0.5) * sdfScale + isoPerimeter;                                  // Signed Distance to edge (in texture space)
    return saturate(0.5 + 2.0 * sd / (stsr / (sharpness + 1.0f) + softness));   // Screen pixel coverage : center + (1 / sampling radius) * signed distance
}

UIE_FRAG_T uie_textcore(float2 uv, half textureSlot, min16uint2 textCoreLoc, float4 vertexColor, float sdfScale, bool isDynamicColor, float sharpness, float extraDilate)
{
    min16uint2 row3Loc = textCoreLoc + min16uint2(0, 3);
    float4 settings = ReadShaderInfo(row3Loc);

    settings *= sdfScale - 1.5f;
    float2 underlayOffset = settings.xy;
    float underlaySoftness = settings.z;
    float outlineDilate = settings.w * 0.25f;
    float3 dilate = float3(-outlineDilate, outlineDilate, 0);
    float3 softness = float3(0, 0, underlaySoftness);

    // Distance to Alpha
    TextureInfo ti = GetTextureInfo(textureSlot);
    float texelWidth = ti.texelSize.x;
    float textureHeight = ti.textureSize.y;
    float4 tex1, tex2;
    SampleTextureSlot2(textureSlot, uv, uv + underlayOffset * texelWidth, tex1, tex2);
    float alpha1 = tex1.a;
    float alpha2 = tex2.a;
    float3 alpha = sd_to_coverage(float3(alpha1, alpha1, alpha2), uv, textureHeight, sdfScale, dilate + extraDilate, softness, sharpness);

    // Blending of the 3 ARGB layers
    float4 faceColor = vertexColor;
    UIE_FRAG_T color = faceColor * alpha.x;

    min16uint2 row1Loc = textCoreLoc + min16uint2(0, 1);
    float4 outlineColor = ReadShaderInfo(row1Loc);
    color += outlineColor * ((1 - alpha.x) * alpha.y);

    min16uint2 row2Loc = textCoreLoc + min16uint2(0, 2);
    float4 underlayColor = ReadShaderInfo(row2Loc);
    color += underlayColor * ((1 - alpha.x) * (1 - alpha.y) * alpha.z);

    color.rgb /= (color.a > 0.0f ? color.a : 1.0f);

    return color;
}

float pixelDist(float2 uv)
{
    float dist = length(uv) - 1.0f; // Bring from [0,...] to [-1,...] range
    float2 ddist = float2(ddx(dist), ddy(dist));
    return dist / length(ddist);
}

float ComputeCoverage(float2 outer, float2 inner)
{
    float coverage = 1;
    // Don't evaluate circles defined as kUnusedArc
    [branch] if (outer.x > -9999.0f)
    {
        float outerDist = pixelDist(outer);
        coverage *= saturate(0.5f-outerDist);
    }
    [branch] if (inner.x > -9999.0f)
    {
        float innerDist = pixelDist(inner);
        coverage *= 1.0f - saturate(0.5f-innerDist);
    }

    return coverage;
}

static const half k_VertTypeSolid = 0;
static const half k_VertTypeText = 1;
static const half k_VertTypeTexture = 2;
static const half k_VertTypeDynamicTexture = 3; // Dynamically Sized Texture (e.g. Dynamic Atlas - UVs must be patched)
static const half k_VertTypeSvgGradient = 4;

static const half k_FragTypeSolid = 0;
static const half k_FragTypeTexture = 1;
static const half k_FragTypeText = 2;
static const half k_FragTypeSvgGradient = 3;

bool TestType(half type, half constType) // Types are meant to be tested in ascending order
{
    return type < constType + 0.5f;
}

bool TestIsArc(half flag)
{
    return flag > 0.5f / 255;
}

bool TestIsDynamicColor(half flag)
{
    return flag > 0.5f / 255;
}

bool TestIsDynamicTextColor(half flag)
{
    return flag > 1.5f / 255;
}

v2f uie_std_vert(appdata_t v)
{
    v2f OUT;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    // Position
    uie_vert_load_dynamic_transform(v);
    v.vertex.xyz = mul(uie_toWorldMat, v.vertex); // Apply Dynamic Transform
    OUT.pos = UnityObjectToClipPos(v.vertex); // Apply Group Transform + Projection

    // Dynamic Opacity
    min16uint2 opacityLoc = uie_decode_shader_info_texel_pos(v.opacityColorPages.xy, v.ids.z, 1);
    half opacity = ReadShaderInfo(opacityLoc).a;

    // Color
    UIE_V2F_COLOR_T color;
    [branch] if (TestIsDynamicColor(v.flags.w))
    {
        // When flags.w is 2 instead of 1, the color is stored in text settings
        min16uint dynamicColorStride = TestIsDynamicTextColor(v.flags.w) ? 4 : 1;
        min16uint2 dynamicColorLoc = uie_decode_shader_info_texel_pos(v.opacityColorPages.zw, v.ids.w, dynamicColorStride);
        color = ReadShaderInfo(dynamicColorLoc); // Color is stored in shader info
    }
    else
    {
#if UIE_COLORSPACE_GAMMA
        color = v.color;
#else // !UIE_COLORSPACE_GAMMA
        // Keep this in the VS to ensure that interpolation is performed in the right color space
        color = UIE_V2F_COLOR_T(uie_gamma_to_linear(v.color.rgb), v.color.a);
#endif // UIE_COLORSPACE_GAMMA
    }
    
    // Fragment Shader Discard Clipping Rect
    min16uint2 clipRectLoc = uie_decode_shader_info_texel_pos(v.xformClipPages.zw, v.ids.y, 1);
    float4 rectClippingData = ReadShaderInfo(clipRectLoc);
    OUT.uvClip.zw = ComputeRelativeClipRectCoords(rectClippingData.xy, rectClippingData.zw, v.vertex.xy);

    // Others
    OUT.uvClip.xy = v.uv; // Dynamic texture overrides this value.
    OUT.circle = v.circle; // Arc-AA Data. Text overrides this value.
    OUT.textCoreLoc.xy = -1; // Mostly unused. Text overrides this value.

    const half vertType = v.flags.x * 255.0f;
    half fragType, yData, zData, wData;
    [branch] if (TestType(vertType, k_VertTypeSolid))
    {
        color.a *= opacity;
        fragType = k_FragTypeSolid;
        yData = -1; // Unused
        zData = -1; // Unused
        wData = v.flags.z; // IsArc
    }
    else [branch] if (TestType(vertType, k_VertTypeText))
    {
        fragType = k_FragTypeText;
        yData = FindTextureSlot(v.textureId);
        zData = opacity; // Case 1379601: Text needs to have the separate opacity as well (applied in FS)
        wData = -1; // Unused

        OUT.circle.x = v.flags.y; // Text Extra Dilate
        OUT.textCoreLoc.xy = uie_decode_shader_info_texel_pos(v.opacityColorPages.ba, v.ids.w, 4);

        // SDF color must be premultiplied
        TextureInfo info = GetTextureInfo(yData);
        half multiplier = info.sdfScale > 0.0f ? color.a : 1;
        color.rgb *= multiplier;
    }
    else [branch] if (TestType(vertType, k_VertTypeTexture))
    {
        color.a *= opacity;
        fragType = k_FragTypeTexture;
        yData = FindTextureSlot(v.textureId);
        zData = -1; // Unused
        wData = v.flags.z; // IsArc
    }
    else [branch] if (TestType(vertType, k_VertTypeDynamicTexture)) 
    {
        color.a *= opacity;
        fragType = k_FragTypeTexture;
        yData = FindTextureSlot(v.textureId);
        zData = -1; // Unused
        wData = v.flags.z; // IsArc

        // Patch UVs
        TextureInfo ti = GetTextureInfo(yData);
        OUT.uvClip.xy = v.uv * ti.texelSize;
    }
    else // k_VertTypeSvgGradient
    {
        color.a *= opacity;
        fragType = k_FragTypeSvgGradient;
        yData = FindTextureSlot(v.textureId);
        zData = v.settingIndex.x * (255.0f*255.0f) + v.settingIndex.y * 255.0f;
        wData = -1; // Unused
    }

    OUT.color = color;
    OUT.typeTexSettings = half4(fragType, yData, zData, wData);

    return OUT;
}

UIE_FRAG_T uie_std_frag(v2f IN)
{
    float2 uv = IN.uvClip.xy;
    half renderType = IN.typeTexSettings.x;
    half textureSlot = IN.typeTexSettings.y;

    UIE_FRAG_T color;
    float coverage;
    [branch] if (TestType(renderType, k_FragTypeSolid))
    {
        color = IN.color;
        coverage = 1;
        [branch] if (TestIsArc(IN.typeTexSettings.w))
            coverage = ComputeCoverage(IN.circle.xy, IN.circle.zw);
    }
    else [branch] if (TestType(renderType, k_FragTypeTexture))
    {
        color = SampleTextureSlot(textureSlot, uv);
#if UIE_FORCE_GAMMA
        color.rgb = uie_linear_to_gamma(color.rgb);
#endif
        color *= IN.color;
        coverage = 1;
        [branch] if (TestIsArc(IN.typeTexSettings.w))
            coverage = ComputeCoverage(IN.circle.xy, IN.circle.zw);
    }
    else [branch] if (TestType(renderType, k_FragTypeText))
    {
        TextureInfo info = GetTextureInfo(textureSlot);
        [branch] if (info.sdfScale > 0.0f)
        {
            bool isDynamicColor = TestIsDynamicColor(IN.typeTexSettings.w);
            float extraDilate = IN.circle.x;
#if UNITY_PLATFORM_WEBGL
            min16uint2 textCoreLoc = min16uint2(round(IN.textCoreLoc));
#else
            min16uint2 textCoreLoc = IN.textCoreLoc;
#endif
            color = uie_textcore(uv, textureSlot, textCoreLoc, IN.color, info.sdfScale, isDynamicColor, info.sharpness, extraDilate);
        }
        else
        {
            float textAlpha = SampleTextureSlot(textureSlot, uv).a;
            color = IN.color;
            color.a *= textAlpha;
        }
        half opacity = IN.typeTexSettings.z;
        color.a *= opacity;
        coverage = 1;
    }
    else // k_FragTypeSvgGradient
    {
        min16uint settingIndex  = round(IN.typeTexSettings.z);
        float2 texelSize = GetTextureInfo(textureSlot).texelSize;
        GradientLocation grad = uie_sample_gradient_location(settingIndex, uv);
        grad.location *= texelSize.xyxy;
        grad.uv *= grad.location.zw;
        grad.uv += grad.location.xy;
        color = SampleTextureSlot(textureSlot, grad.uv);
#if !UIE_COLORSPACE_GAMMA
        // Unlike the textured render type, the gradient texture is ALWAYS with the UNORM format, meaning that we always
        // read sRGB-encoded values.
        color.rgb = uie_gamma_to_linear(color.rgb);
#endif
        color *= IN.color;
        coverage = 1;
    }

    coverage *= uie_fragment_clip(IN);

    // Clip fragments when coverage is close to 0 (< 1/256 here).
    // This will write proper masks values in the stencil buffer.
    clip(coverage - 0.003f);

    color.a *= coverage;
    return color;
}

#ifndef UIE_CUSTOM_SHADER

v2f vert(appdata_t v) { return uie_std_vert(v); }
UIE_FRAG_T frag(v2f IN) : SV_Target { return uie_std_frag(IN); }

#endif // UIE_CUSTOM_SHADER

#endif // UNITY_UIE_INCLUDED
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\Internal\UnityUIE.cginc---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.ugui\Editor Resources\Shaders\TMP_Properties.cginc---------------
.
.
// UI Editable properties
uniform sampler2D	_FaceTex;					// Alpha : Signed Distance
uniform float		_FaceUVSpeedX;
uniform float		_FaceUVSpeedY;
uniform fixed4		_FaceColor;					// RGBA : Color + Opacity
uniform float		_FaceDilate;				// v[ 0, 1]
uniform float		_OutlineSoftness;			// v[ 0, 1]

uniform sampler2D	_OutlineTex;				// RGBA : Color + Opacity
uniform float		_OutlineUVSpeedX;
uniform float		_OutlineUVSpeedY;
uniform fixed4		_OutlineColor;				// RGBA : Color + Opacity
uniform float		_OutlineWidth;				// v[ 0, 1]

uniform float		_Bevel;						// v[ 0, 1]
uniform float		_BevelOffset;				// v[-1, 1]
uniform float		_BevelWidth;				// v[-1, 1]
uniform float		_BevelClamp;				// v[ 0, 1]
uniform float		_BevelRoundness;			// v[ 0, 1]

uniform sampler2D	_BumpMap;					// Normal map
uniform float		_BumpOutline;				// v[ 0, 1]
uniform float		_BumpFace;					// v[ 0, 1]

uniform samplerCUBE	_Cube;						// Cube / sphere map
uniform fixed4 		_ReflectFaceColor;			// RGB intensity
uniform fixed4		_ReflectOutlineColor;
//uniform float		_EnvTiltX;					// v[-1, 1]
//uniform float		_EnvTiltY;					// v[-1, 1]
uniform float3      _EnvMatrixRotation;
uniform float4x4	_EnvMatrix;

uniform fixed4		_SpecularColor;				// RGB intensity
uniform float		_LightAngle;				// v[ 0,Tau]
uniform float		_SpecularPower;				// v[ 0, 1]
uniform float		_Reflectivity;				// v[ 5, 15]
uniform float		_Diffuse;					// v[ 0, 1]
uniform float		_Ambient;					// v[ 0, 1]

uniform fixed4		_UnderlayColor;				// RGBA : Color + Opacity
uniform float		_UnderlayOffsetX;			// v[-1, 1]
uniform float		_UnderlayOffsetY;			// v[-1, 1]
uniform float		_UnderlayDilate;			// v[-1, 1]
uniform float		_UnderlaySoftness;			// v[ 0, 1]

uniform fixed4 		_GlowColor;					// RGBA : Color + Intesity
uniform float 		_GlowOffset;				// v[-1, 1]
uniform float 		_GlowOuter;					// v[ 0, 1]
uniform float 		_GlowInner;					// v[ 0, 1]
uniform float 		_GlowPower;					// v[ 1, 1/(1+4*4)]

// API Editable properties
uniform float 		_ShaderFlags;
uniform float		_WeightNormal;
uniform float		_WeightBold;

uniform float		_ScaleRatioA;
uniform float		_ScaleRatioB;
uniform float		_ScaleRatioC;

uniform float		_VertexOffsetX;
uniform float		_VertexOffsetY;

//uniform float		_UseClipRect;
uniform float		_MaskID;
uniform sampler2D	_MaskTex;
uniform float4		_MaskCoord;
uniform float4		_ClipRect;	// bottom left(x,y) : top right(z,w)
//uniform float		_MaskWipeControl;
//uniform float		_MaskEdgeSoftness;
//uniform fixed4		_MaskEdgeColor;
//uniform bool		_MaskInverse;

uniform float		_MaskSoftnessX;
uniform float		_MaskSoftnessY;

// Font Atlas properties
uniform sampler2D	_MainTex;
uniform float		_TextureWidth;
uniform float		_TextureHeight;
uniform float 		_GradientScale;
uniform float		_ScaleX;
uniform float		_ScaleY;
uniform float		_PerspectiveFilter;
uniform float		_Sharpness;
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.ugui\Editor Resources\Shaders\TMP_Properties.cginc---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.ugui\Editor Resources\Shaders\TMP_SDF_SSD.cginc---------------
.
.
﻿struct vertex_t
{
    float4	position		: POSITION;
    float3	normal			: NORMAL;
    float4	color			: COLOR;
    float4	texcoord0		: TEXCOORD0;
    float2	texcoord1		: TEXCOORD1;
};

struct pixel_t
{
    float4	position		: SV_POSITION;
    float4	faceColor		: COLOR;
    float4	outlineColor	: COLOR1;
    float2	texcoord0		: TEXCOORD0;
    float4	param			: TEXCOORD1;		// weight, scaleRatio
    float2	clipUV			: TEXCOORD2;
    #if (UNDERLAY_ON || UNDERLAY_INNER)
    float4	texcoord2		: TEXCOORD3;
    float4	underlayColor	: COLOR2;
    #endif
};

sampler2D _GUIClipTexture;
uniform float4x4 unity_GUIClipTextureMatrix;
float4 _MainTex_TexelSize;

float4 SRGBToLinear(float4 rgba)
{
    return float4(lerp(rgba.rgb / 12.92f, pow((rgba.rgb + 0.055f) / 1.055f, 2.4f), step(0.04045f, rgba.rgb)), rgba.a);
}

pixel_t VertShader(vertex_t input)
{
    pixel_t output;

    float bold = step(input.texcoord0.w, 0);

    float4 vert = input.position;
    vert.x += _VertexOffsetX;
    vert.y += _VertexOffsetY;

    float4 vPosition = UnityObjectToClipPos(vert);

    float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
    weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

    // Generate UV for the Clip Texture
    float3 eyePos = UnityObjectToViewPos(input.position);
    float2 clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));

    float4 color = input.color;
    #if (FORCE_LINEAR && !UNITY_COLORSPACE_GAMMA)
    color = SRGBToLinear(input.color);
    #endif

    float opacity = color.a;
    #if (UNDERLAY_ON | UNDERLAY_INNER)
    opacity = 1.0;
    #endif

    float4 faceColor = float4(color.rgb, opacity) * _FaceColor;
    faceColor.rgb *= faceColor.a;

    float4 outlineColor = _OutlineColor;
    outlineColor.a *= opacity;
    outlineColor.rgb *= outlineColor.a;

    output.position = vPosition;
    output.faceColor = faceColor;
    output.outlineColor = outlineColor;
    output.texcoord0 = float2(input.texcoord0.xy);
    output.param = float4(0.5 - weight, 1.3333 * _GradientScale * (_Sharpness + 1) / _MainTex_TexelSize.z , _OutlineWidth * _ScaleRatioA * 0.5, 0);
    output.clipUV = clipUV;

    #if (UNDERLAY_ON || UNDERLAY_INNER)
    float4 underlayColor = _UnderlayColor;
    underlayColor.rgb *= underlayColor.a;

    float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _MainTex_TexelSize.z;
    float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _MainTex_TexelSize.w;

    output.texcoord2 = float4(input.texcoord0 + float2(x, y), input.color.a, 0);
    output.underlayColor = underlayColor;
    #endif

    return output;
}

float4 PixShader(pixel_t input) : SV_Target
{
    float d = tex2D(_MainTex, input.texcoord0.xy).a;

    float2 UV = input.texcoord0.xy;
    float scale = rsqrt(abs(ddx(UV.x) * ddy(UV.y) - ddy(UV.x) * ddx(UV.y))) * input.param.y;

    #if (UNDERLAY_ON | UNDERLAY_INNER)
    float layerScale = scale;
    layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
    float layerBias = input.param.x * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);
    #endif

    scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);

    float4 faceColor = input.faceColor * saturate((d - input.param.x) * scale + 0.5);

    #ifdef OUTLINE_ON
    float4 outlineColor = lerp(input.faceColor, input.outlineColor, sqrt(min(1.0, input.param.z * scale * 2)));
    faceColor = lerp(outlineColor, input.faceColor, saturate((d - input.param.x - input.param.z) * scale + 0.5));
    faceColor *= saturate((d - input.param.x + input.param.z) * scale + 0.5);
    #endif

    #if UNDERLAY_ON
    d = tex2D(_MainTex, input.texcoord2.xy).a * layerScale;
    faceColor += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate(d - layerBias) * (1 - faceColor.a);
    #endif

    #if UNDERLAY_INNER
    float bias = input.param.x * scale - 0.5;
    float sd = saturate(d * scale - bias - input.param.z);
    d = tex2D(_MainTex, input.texcoord2.xy).a * layerScale;
    faceColor += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate(d - layerBias)) * sd * (1 - faceColor.a);
    #endif

    #if (UNDERLAY_ON | UNDERLAY_INNER)
    faceColor *= input.texcoord2.z;
    #endif

    faceColor *= tex2D(_GUIClipTexture, input.clipUV).a;

    return faceColor;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.ugui\Editor Resources\Shaders\TMP_SDF_SSD.cginc---------------
.
.
