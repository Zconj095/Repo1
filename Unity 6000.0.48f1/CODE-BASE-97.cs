 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Accumulate.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

#define SHNUMCOEFFICIENTS 9

// 1 / (2*sqrt(kPI))
#define K1DIV2SQRTPI        0.28209479177387814347403972578039
// sqrt(3) / (2*sqrt(kPI))
#define KSQRT3DIV2SQRTPI    0.48860251190291992158638462283835
// sqrt(15) / (2*sqrt(kPI))
#define KSQRT15DIV2SQRTPI   1.0925484305920790705433857058027
// 3 * sqrtf(5) / (4*sqrt(kPI))
#define K3SQRT5DIV4SQRTPI   0.94617469575756001809268107088713
// sqrt(15) / (4*sqrt(kPI))
#define KSQRT15DIV4SQRTPI   0.54627421529603953527169285290135
// sqrtf(5) / (4*sqrt(kPI)) (the constant term in the Y_2,0 basis function of the standard real SH basis)
#define SQRT5DIV4SQRTPI     0.315391565252520050

void accumulateSH(int target, vec4 col, vec3 dir)
{
    float outsh[SHNUMCOEFFICIENTS];
    outsh[0] = K1DIV2SQRTPI;
    outsh[1] = dir.x * KSQRT3DIV2SQRTPI;
    outsh[2] = dir.y * KSQRT3DIV2SQRTPI;
    outsh[3] = dir.z * KSQRT3DIV2SQRTPI;
    outsh[4] = dir.x * dir.y * KSQRT15DIV2SQRTPI;
    outsh[5] = dir.y * dir.z * KSQRT15DIV2SQRTPI;
    outsh[6] = (dir.z * dir.z * K3SQRT5DIV4SQRTPI) - SQRT5DIV4SQRTPI;
    outsh[7] = dir.x * dir.z * KSQRT15DIV2SQRTPI;
    outsh[8] = (dir.x * dir.x - dir.y * dir.y) * KSQRT15DIV4SQRTPI;

    for (int c = GetFBOAttachmentIndex(target); c < SHNUMCOEFFICIENTS; c++)
        accumulate(c, vec4(col.xyz * outsh[c] * KPI, 0.0));
}

// Calculate luminance like Unity does it
float unityLinearLuminance(vec3 color)
{
    vec3 lumW = vec3(0.22, 0.707, 0.071);
    return dot(color, lumW);
}

void accumulateDirectional(int target, vec4 color, vec3 dir)
{
    float luminance = unityLinearLuminance(color.xyz);
    vec4 directionality = vec4(dir, 1.0) * luminance;

    if (target == GI_DIRECT_BUFFER)
        accumulate(GetFBOAttachmentIndex(DIRECTIONAL_FROM_DIRECT_BUFFER), directionality);
    else if (target == GI_BUFFER || target == ENV_BUFFER)
        accumulate(GetFBOAttachmentIndex(DIRECTIONAL_FROM_GI_BUFFER), directionality);

    accumulate(GetFBOAttachmentIndex(target), color);
}

// may be better to recompile/load a different shader
void Accumulate(int target, vec4 color, vec3 direction, LightmapMode lightmapMode)
{
    if(target == PROBE_BUFFER)
    {
        accumulateSH(target, color, direction);
    }
    else if(lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
    {
        accumulateDirectional(target, color, direction);
    }
    else
    {
        accumulate(GetFBOAttachmentIndex(target), color);
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Accumulate.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\CustomBakeFrameShader.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniform sampler2D  PositionsTex;
uniform int PassIdx;
uniform int SamplesPerPass;
uniform int SamplesSoFar;
uniform int TotalSampleCount;

void setup()
{
    rl_OutputRayCount = SamplesPerPass;
}

void ProbeSampling(vec3 pos, int rayCount, int totalRayCount, float rayOffset)
{
    int sampleIndex = totalRayCount;
    float weight = 4.0/float(TotalSampleCount);

    vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
    float cpShiftOffset = cpShift.z; // This dimension is used to randomize the position of the probe samples.

    for(int i = 0; i < rayCount; ++i, ++sampleIndex)
    {
        vec2 rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift.xy);
        float randOffset = 0.1 * rayOffset + 0.9 * rayOffset * fract(SobolSample(sampleIndex, 2, 0) + cpShiftOffset);

        vec3 direction = SphereSample(rnd);

        // We don't want the full sphere, we only want the upper hemisphere.
        if (direction.y < 0.0)
            direction = vec3(direction.x, -direction.y, direction.z);

        createRay();
        rl_OutRay.origin           = pos + direction * randOffset;
        rl_OutRay.direction        = direction;
        rl_OutRay.color            = vec4(1.0); // multiplied by transmission in the Standard shader
        rl_OutRay.probeDir         = normalize(direction);
        rl_OutRay.defaultPrimitive = GetEnvPrimitive();
        rl_OutRay.renderTarget     = CUSTOM_BAKE_BUFFER;
        rl_OutRay.isOutgoing       = true;
        rl_OutRay.sampleIndex      = sampleIndex;
        rl_OutRay.rayType          = PVR_RAY_TYPE_GI;
        rl_OutRay.rayClass         = PVR_RAY_CLASS_GI;
        rl_OutRay.depth            = 0;
        rl_OutRay.weight           = weight;
        // Needs to be false, otherwise rl_FrontFacing is never set/never false.
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.albedo           = vec3(1.0);
        rl_OutRay.sameOriginCount  = 0;
        rl_OutRay.lightmapMode     = LIGHTMAPMODE_NONDIRECTIONAL; // Not used with probe sampling.
        rl_OutRay.lodParam         = 0;
        rl_OutRay.lodOrigin        = rl_OutRay.origin;
        rl_OutRay.originalT        = rl_OutRay.maxT;
        emitRayWithoutDifferentials();
    }
}

void main()
{
    vec2  frameCoord  = rl_FrameCoord.xy / rl_FrameSize.xy;

    vec4 posTex = texture2D(PositionsTex, frameCoord);

    // Unused texels
    if(posTex.w < 0.0)
        return;

    ProbeSampling(posTex.xyz, SamplesPerPass, SamplesSoFar, posTex.w);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\CustomBakeFrameShader.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\CustomEnvImplement.rlsl---------------


void main()
{
    accumulate(vec4(rl_InRay.color.x, rl_InRay.color.y, rl_InRay.color.z, 0.0));
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\CustomEnvImplement.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Debugging.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniformblock DebugProperties
{
    int ActiveDebugPixelX;
    int ActiveDebugPixelY;
    int InteractiveModeEnabled;
};

#define ERROR_CODE   1

#define ENABLE_DEBUGGING 0
#define ENABLE_INTERACTIVE_MODE 1

#if ENABLE_INTERACTIVE_MODE
bool InteractiveModeEnabled()
{
    return DebugProperties.InteractiveModeEnabled!=0;
}
#else
#define InteractiveModeEnabled() (false)
#endif

bool IsDebugRow(float offset)
{
    return abs(rl_FrameCoord.y - (float(DebugProperties.ActiveDebugPixelY)+0.5))<=offset;
}

bool IsDebugColumn(float offset)
{
    return abs(rl_FrameCoord.x - (float(DebugProperties.ActiveDebugPixelX)+0.5))<=offset;
}

bool IsDebugPixel(float offset)
{
    return (IsDebugRow(offset) && IsDebugColumn(offset));
}

bool DrawDebugCrosshair(float offset)
{
#if ENABLE_DEBUGGING
    if(IsDebugPixel(offset))
    {
        accumulate (vec3(1,0,0));
        return true;
    }

    if(IsDebugColumn(0.0))
        Accumulate(vec3(0,1,0));
    if(IsDebugRow(0.0))
        Accumulate(vec3(0,0,1));
#endif
    return false;

}


#if ENABLE_DEBUGGING

#define DEBUG_ALWAYS(_x, _y)     debug(_x,_y)
#define DEBUG(_x, _y)            if(IsDebugPixel(0.0))       debug(_x,_y)
#define CROSSHAIR_DEBUG(_x, _y)  if(DrawDebugCrosshair(0.0)) debug(_x, _y)


#else

#define DEBUG_ALWAYS(_x, _y)
#define DEBUG(_x, _y)
#define CROSSHAIR_DEBUG(_x, _y)

#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Debugging.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\DefaultVertexShader.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

attribute vec3 Vertex;
attribute vec3 Normal;
attribute vec2 TexCoord0;
attribute vec2 TexCoord1;

normalized transformed varying vec3 NormalVarying;
varying vec2 TexCoord0Varying;
varying vec2 TexCoord1Varying;


void main()
{

    rl_Position = vec4(Vertex, 1.0);
    NormalVarying = Normal;
    TexCoord0Varying = TexCoord0;
    TexCoord1Varying = TexCoord1;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\DefaultVertexShader.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Defines.rlsl---------------


/* Shared defines to be included from both rlsl and cpp files */

// Target aka unique buffer name
// (can't overlap between lightmaps and light probes)
#define GI_BUFFER                            0
#define AO_BUFFER                            1
#define ENV_BUFFER                           2
#define GI_DIRECT_BUFFER                     3
#define GI_SAMPLES_BUFFER                    4
#define DIRECT_SAMPLES_BUFFER                5
#define ENV_SAMPLES_BUFFER                   6
#define VALIDITY_BUFFER                      7
#define DIRECTIONAL_FROM_DIRECT_BUFFER       8
#define DIRECTIONAL_FROM_GI_BUFFER           9
#define SHADOW_MASK_BUFFER                  10
#define SHADOW_MASK_SAMPLE_BUFFER           11
#define PROBE_BUFFER                        12
#define PROBE_OCCLUSION_BUFFER              13
#define PROBE_VALIDITY_BUFFER               14
#define CUSTOM_BAKE_BUFFER                  15

// convergence bitmasks
#define PVR_CONVERGED_DIRECT    (1<<0)
#define PVR_CONVERGED_GI        (1<<1)
#define PVR_CONVERGED_ENV       (1<<2)

// limits
#ifndef PVR_MAX_ENVSAMPLES
#define PVR_MAX_ENVSAMPLES      16384
#endif
// a few light related constants
#define PVR_MAX_LIGHTS          32768       // maximum number of lights for the whole scene
#define PVR_MAX_LIGHT_REGIONS   8192        // maximum number of lightgrid cells
#define PVR_MAX_COOKIES         65536       // maximum number of cookie slices
// Reserve 4MB for cdfs. This roughly gives us 512 lights per light region if we got 8K regions, which should be good enough (tm).
// The conservative value obeying PVR_MAX_LIGHTS and PVR_MAX_LIGHT_REGIONS would require about 256MB which seems excessive.
#define PVR_MAX_CDFS            4202496
#define PVR_MAX_SHADOW_RAYS     4           // limit of how many shadow rays are shot per bounce excluding directional light shadows


//Keep in sync with CPPsharedCLincludes.h
#define PLM_MAX_DIR_LIGHTS      8           // limit of how many directional lights are supported
#define PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE 3
#define PLM_MAX_BOUNCE_FOR_CRANLEY_PATTERSON_ROTATION 4
#define PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION (PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE * PLM_MAX_BOUNCE_FOR_CRANLEY_PATTERSON_ROTATION)

//Max size of the golden samples (for cranley patterson rotation) map
//this is a square map with PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION in each texel
#define PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP 512

// needs to be kept in sync with "LightmapBake::InitializeDataAndTextures()" and Defines.rlsl (PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP and PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION)
//equals to 512*512*12
#define MAX_GOLDEN_SAMPLES 3145728

//keep in sync with CPPsharedCLincludes.h
#define PLM_USE_BLUE_NOISE_SAMPLING 1

#define PLM_BLUE_NOISE_TILE_SIZE 128
#define PLM_BLUE_NOISE_MAX_DIMENSIONS 256
#define PLM_BLUE_NOISE_MAX_SAMPLES 256
#define PLM_BLUE_NOISE_MAX_RANKING_DIMENSIONS 8
#define PLM_BLUE_NOISE_MAX_SCRAMBLING_DIMENSIONS 8

// equivalent PLM_BLUE_NOISE_MAX_DIMENSIONS * PLM_BLUE_NOISE_MAX_SAMPLES
#define PLM_BLUE_NOISE_SAMPLING_BUFFER_SIZE 65536

//equivalent to PLM_BLUE_NOISE_MAX_RANKING_DIMENSIONS * PLM_BLUE_NOISE_TILE_SIZE * PLM_BLUE_NOISE_TILE_SIZE * 9 (9 distributions for progressive sampling 1-256 samples)
#define PLM_BLUE_NOISE_RANKING_BUFFER_SIZE 1179648
#define PLM_BLUE_NOISE_SCRAMBLING_BUFFER_SIZE 1179648

#define PLM_MIN_PUSHOFF 0.0001f

// Update Standard shader's setup() when adding more ray classes.
// Warning: When adding a new ray class, make sure it's correctly handled by the RayClassVisibility enum in PVRHelpers.h
// Level of Detail 0
#define PVR_RAY_CLASS_GI        0
#define PVR_RAY_CLASS_OCC       1
// lod occlusion rays
#define PVR_RAY_CLASS_LOD_0     1 // not a typo that this is 1 - it's only a helper, never meant to be set as an actual ray class
#define PVR_RAY_CLASS_LOD_1     2
#define PVR_RAY_CLASS_LOD_2     3
#define PVR_RAY_CLASS_LOD_3     4
#define PVR_RAY_CLASS_LOD_4     5
#define PVR_RAY_CLASS_LOD_5     6
#define PVR_RAY_CLASS_LOD_6     7
// ray visibility only supports up to 8 bits, so this should never be set on a ray
// #define PVR_RAY_CLASS_LOD_7  8

#define PVR_RAY_TYPE_GI         0
#define PVR_RAY_TYPE_SHADOW     1
#define PVR_RAY_TYPE_ENV        2

#define PVR_LOD_0_BIT            1 // 1 << 0
#define PVR_LOD_6_BIT           64 // 1 << 6
#define PVR_LOD_7_BIT          128 // 1 << 7

#define PVR_LODMASK_SHIFT       24 // offset of the lodmask in lparam, we're using the top 8 bits
#define PVR_LODGROUP_MASK       ((1 << PVR_LODMASK_SHIFT)-1) // mask to used to extract the group Id from the lodParam
#define PVR_FLT_MAX             1e37
#define PVR_FLT_EPSILON         1e-19 // Small value that is unlikely to result in a denormalised product.


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Defines.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\EnvImplement.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

vec3 DecodeRGBM( vec4 color )
{
    return (8.0 * color.a) * (color.rgb);
}


void main()
{
    if(rl_InRay.depth == 0 && (rl_InRay.renderTarget != PROBE_BUFFER) && rl_InRay.rayType != PVR_RAY_TYPE_ENV)
        accumulate(AO_BUFFER, vec3(1.0,1.0,1.0));

    // we need to write 1.0 into the alpha so dilation during compositing doesn't kill the contribution if no lights are present in the scene
    if (rl_InRay.rayType == PVR_RAY_TYPE_ENV)
        Accumulate(rl_InRay.renderTarget, vec4( rl_InRay.color.rgb * rl_InRay.weight * rl_InRay.albedo, 1.0), rl_InRay.probeDir, rl_InRay.lightmapMode);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\EnvImplement.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Environment.rlsl---------------


/*
    This file contains helper functions for importance sampling the environment/sky.
*/


// EnvironmentLightingSamples contains data used to importance sample the environment map.
uniformblock EnvironmentLightingSamples
{
    int   NumRaysIndirect;                          // number of environment rays shot for each subsequent intersection of a path
    int   NumSamples;                               // the actual number of valid samples in the Directions and WeightedIntensities arrays
    float EnvmapIntegral;                           // contains the integral of the environment over the sphere surface according to a given metric
    int   Flags;                                    // various flags
    vec4  Directions[PVR_MAX_ENVSAMPLES];           // contains a direction into the environment map
    vec4  WeightedIntensities[PVR_MAX_ENVSAMPLES];  // contains (sampleIntensity / EnvironmentPDF)
};

uniformblock SkyboxTextures
{
    sampler2D FrontTex;
    sampler2D BackTex;
    sampler2D LeftTex;
    sampler2D RightTex;
    sampler2D UpTex;
    sampler2D DownTex;
};

bool UseEnvironmentImportanceSampling()
{
    return (EnvironmentLightingSamples.Flags & 1) != 0;
}

bool SampleDirectEnvironment()
{
    return (EnvironmentLightingSamples.Flags & 2) == 0 && IntegratorSamples.maxBounces > 0;
}

bool SampleIndirectEnvironment()
{
    return (EnvironmentLightingSamples.Flags & 4) == 0;
}

int GetRaysPerEnvironmentIndirect()
{
    return EnvironmentLightingSamples.NumRaysIndirect;
}


vec3 GetSkyBoxColor(vec3 direction)
{
    vec2 skyTexCoord;

    vec3 tempDir = normalize(direction);
    direction = tempDir;

    vec3 absDir = abs(direction);

    vec4 texColor = vec4(1.0);

    //See if the X axis is dominant in the direction vector
    if (absDir.x > absDir.y && absDir.x > absDir.z) {
        if (direction.x > 0.0) {
            skyTexCoord = vec2(-direction.z / absDir.x, -direction.y / absDir.x) / vec2(2.0) + vec2(0.5);
            texColor = texture2D(SkyboxTextures.LeftTex, skyTexCoord);
        }
        else {
            skyTexCoord = vec2(direction.z / absDir.x, -direction.y / absDir.x) / vec2(2.0) + vec2(0.5);
            texColor = texture2D(SkyboxTextures.RightTex, skyTexCoord);
        }
    }
    else {
        if (absDir.y > absDir.z) {
            if (direction.y > 0.0) {
                skyTexCoord = vec2(direction.x / absDir.y, direction.z / absDir.y) / vec2(2.0) + vec2(0.5);
                texColor = texture2D(SkyboxTextures.UpTex, skyTexCoord);
            }
            else {
                skyTexCoord = vec2(direction.x / absDir.y, -direction.z / absDir.y) / vec2(2.0) + vec2(0.5);
                texColor = texture2D(SkyboxTextures.DownTex, skyTexCoord);
            }
        }
        else {
            if (direction.z > 0.0) {
                skyTexCoord = vec2(direction.x / absDir.z, -direction.y / absDir.z) / vec2(2.0) + vec2(0.5);
                texColor = texture2D(SkyboxTextures.FrontTex, skyTexCoord);
            }
            else {
                skyTexCoord = vec2(-direction.x / absDir.z, -direction.y / absDir.z) / vec2(2.0) + vec2(0.5);
                texColor = texture2D(SkyboxTextures.BackTex, skyTexCoord);
            }
        }
    }

    return texColor.xyz;
}

// sets up parameters for an environment ray
void CastEnvironment(int target, vec3 pos, vec3 dir, vec3 firstDir, vec3 albedo, vec3 intensity, float weight, int depth, int transDepth, LightmapMode lightmapMode, int lodParam, float lodT)
{
    createRay();
    rl_OutRay.origin           = pos.xyz;
    rl_OutRay.direction        = dir;
    rl_OutRay.color            = vec4(intensity, 0.0 );
    rl_OutRay.probeDir         = firstDir;
    rl_OutRay.renderTarget     = target;
    rl_OutRay.isOutgoing       = true;          // undocumented built-in boolean
    rl_OutRay.sampleIndex      = 0;             // unused
    rl_OutRay.depth            = depth;
    rl_OutRay.weight           = weight;
    rl_OutRay.albedo           = albedo;
    rl_OutRay.sameOriginCount  = 0;
    rl_OutRay.transmissionDepth= transDepth;
    rl_OutRay.lightmapMode     = lightmapMode;
    rl_OutRay.lodParam         = lodParam;
    rl_OutRay.rayType          = PVR_RAY_TYPE_ENV;
    rl_OutRay.rayClass         = MapLodParamToRayClass(PVR_RAY_TYPE_ENV, lodParam);
    rl_OutRay.lodOrigin        = rl_OutRay.origin;
    rl_OutRay.originalT        = rl_OutRay.maxT;
    if (lodParam == 0)
    {
        rl_OutRay.occlusionTest    = true;
        rl_OutRay.defaultPrimitive = GetEnvPrimitive();
    }
    else
    {
        rl_OutRay.maxT             = lodT;
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.defaultPrimitive = GetLodMissPrimitive();
    }
    emitRayWithoutDifferentials();
}

// importance sampling path related functions to estimate pdfs
float EnvironmentMetric(vec3 intensity)
{
    // use the max intensity as a metric. Keep in sync with EnvironmentMetric in .cpp
    return max(max(intensity.r, intensity.g), intensity.b);
}

// calculates the weight using a balanced heuristic
float EnvironmentHeuristic(float pdf1, float pdf2)
{
    float denom = pdf1 + pdf2;
    return denom > 0.0 ? (pdf1 / denom) : 0.0;
}

// importance sampling path for sampling the environment according to the one-sample model
void SurfaceSampleEnvironmentIS(int target, vec3 position, vec3 firstDir, vec3 interpNormal, vec3 geomNormal, vec3 albedo, vec3 rand, float weight, int depth, int transDepth, LightmapMode lightmapMode, int lodParam, float lodT, bool firstDirValid)
{
    // frame of reference for sampling hemisphere
    vec3 b1, b2;
    CreateOrthoNormalBasis(interpNormal, b1, b2);
    mat3 onb = mat3(b1, b2, interpNormal);

    vec3 direction, intensity;
    bool shootRay;

    // Use one random sample rule instead of estimating both pdfs.
    // Due to this the chosen path has its is weight multiplied by 2.0 as we're evenly drawing from the two pdfs.

    if (rand.z > 0.5)
    {
        int   sampleIndex = int(fract(rand.x) * float(EnvironmentLightingSamples.NumSamples)) % EnvironmentLightingSamples.NumSamples;
              direction   = EnvironmentLightingSamples.Directions[sampleIndex].xyz;
        float cosdir      = max( 0.0, dot(direction, onb[2]));
        float pdf_diffuse = cosdir / PI;
              intensity   = GetSkyBoxColor(direction);
        float metric      = EnvironmentMetric(intensity);
        float pdf_envmap  = metric / EnvironmentLightingSamples.EnvmapIntegral;
        float is_weight  = EnvironmentHeuristic(pdf_envmap, pdf_diffuse);

        // The final weight is "2.0 * is_weight * intensity / pdf_environment * cos(dir, N) / PI", but we have pre-calculated "intensity / pdf_environment" on the CPU already
        // The multiplication by 2.0 compensates for us not drawing from both densities, but randomly and evenly choosing one of them.
        vec3 weightedIntensity = EnvironmentLightingSamples.WeightedIntensities[sampleIndex].xyz;
             intensity         = 2.0 * is_weight * weightedIntensity * cosdir / PI;
             shootRay          = dot(direction, geomNormal) > 0.0 && cosdir > 0.0;
    }
    else
    {
              direction   = HemisphereCosineSample(rand.xy);                    // cosine weighted samples
        float pdf_diffuse = direction.z / PI;                                   // cosine weight
              direction   = onb * direction;
              intensity   = GetSkyBoxColor(direction);
        float metric      = EnvironmentMetric(intensity);
        float pdf_envmap  = metric / EnvironmentLightingSamples.EnvmapIntegral;
        float is_weight  = EnvironmentHeuristic( pdf_diffuse, pdf_envmap );

        // The final weight is "2.0 * is_weight * intensity / pdf_diffuse * cos * brdf" in which case pdf_diffuse and (cos) eliminate the cosine.
        // The remaining PI is eliminated by the diffuse BRDF's 1/PI normalization that we're already handling here
        // The multiplication by 2.0 compensates for us not drawing from both densities, but randomly and evenly choosing one of them.
              intensity   = 2.0 * is_weight * intensity;
              shootRay    = dot(direction, geomNormal) > 0.0;
    }

    // Sampling the hemisphere around the interpolated normal can generate directions below the geometric surface, so we're guarding against that
    if (shootRay)
        CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, intensity, weight, depth, transDepth, lightmapMode, lodParam, lodT);
}

// non-is path
void SurfaceSampleEnvironment(int target, vec3 position, vec3 firstDir, vec3 interpNormal, vec3 geomNormal, vec3 albedo, vec2 rand, float weight, int depth, int transDepth, LightmapMode lightmapMode, int lodParam, float lodT, bool firstDirValid)
{
    vec3 b1, b2;
    CreateOrthoNormalBasis(interpNormal, b1, b2);
    mat3 onb = mat3(b1, b2, interpNormal);

    // sample hemisphere
    vec3 direction = onb * HemisphereCosineSample(rand);

    if (dot(direction, geomNormal) > 0.0)
    {
        vec3 intensity = GetSkyBoxColor(direction);
        CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, intensity, weight, depth, transDepth, lightmapMode, lodParam, lodT);
    }
}

// importance sampling path for sampling the environment according to the one-sample model
void VolumeSampleEnvironmentIS(int target, vec3 position, vec3 firstDir, vec3 albedo, vec3 rand, float weight, int depth, int transDepth, bool firstDirValid)
{
    // Use one random sample rule instead of estimating both pdfs.
    // Due to this the chosen path has its importance sampling weight multiplied by 2.0 as we're evenly drawing from the two pdfs.
    if (rand.z > 0.5)
    {
        int   sampleIndex = int(rand.x * float(EnvironmentLightingSamples.NumSamples));
        vec3  direction   = EnvironmentLightingSamples.Directions[sampleIndex].xyz;
        float pdf_diffuse = 1.0 / (4.0 * PI);
        vec3  intensity   = GetSkyBoxColor(direction);
        float metric      = EnvironmentMetric(intensity);
        float pdf_envmap  = metric / EnvironmentLightingSamples.EnvmapIntegral;
        float is_weight  = EnvironmentHeuristic(pdf_envmap, pdf_diffuse);

        // The final weight is "2.0 * is_weight * intensity / pdf_environment", but we have pre-calculated "intensity / pdf_environment" on the CPU already
        // The multiplication by 2.0 compensates for us not drawing from both densities, but randomly and evenly choosing one of them.
        vec3 weightedIntensity = EnvironmentLightingSamples.WeightedIntensities[sampleIndex].xyz;
        CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, 2.0 * is_weight * weightedIntensity / PI, weight, depth, transDepth, 0, 0, PVR_FLT_MAX);
    }
    else
    {
        vec3  direction   = SphereSample(rand.xy);
        float pdf_diffuse = 1.0 / (4.0 * PI);
        vec3  intensity   = GetSkyBoxColor(direction);
        float metric      = EnvironmentMetric(intensity);
        float pdf_envmap  = metric / EnvironmentLightingSamples.EnvmapIntegral;
        float is_weight  = EnvironmentHeuristic(pdf_diffuse, pdf_envmap);

        // The final weight is "2.0 * is_weight * intensity / pdf_diffuse".
        // The multiplication by 2.0 compensates for us not drawing from both densities, but randomly and evenly choosing one of them.
        CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, 2.0 * is_weight * intensity * 4.0, weight, depth, transDepth, 0, 0, PVR_FLT_MAX);
    }
}

// non-is path
void VolumeSampleEnvironment(int target, vec3 position, vec3 firstDir, vec3 albedo, vec2 rand, float weight, int depth, int transDepth, bool firstDirValid)
{
    vec3  direction   = SphereSample(rand);
    vec3  intensity   = GetSkyBoxColor(direction);
    float pdf_diffuse = 1.0 / 4.0; // PI in denominator cancels out with SH
    CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, intensity, weight / pdf_diffuse, depth, transDepth, 0, 0, PVR_FLT_MAX);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Environment.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\GIBakeFrameShader.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

#define ENABLE_CULLING 1

#define DIRECT_MODE_DISABLED 0
#define DIRECT_MODE_ONLY 1
#define DIRECT_MODE_GI 2

uniform sampler2D  PositionsTex;
uniform sampler2D  InterpolatedNormalsTex;
uniform sampler2D  PlaneNormalsTex;
uniform sampler2D  DirectLighting;
uniform sampler2D  PrevComposite;
uniform sampler2D  ConvergenceMap;
uniform sampler2D  CullingMap;
uniform sampler2D  GISamplesMap;
uniform sampler2D  DirectSamplesMap;
uniform sampler2D  EnvSamplesMap;
uniform sampler2D  InstanceTransforms;
uniform sampler2D  InstanceProperties;

uniform int InstanceTransformsWidth;
uniform int InstanceTransformsHeight;
uniform int InstancePropertiesWidth;
uniform int InstancePropertiesHeight;
uniform int TransformOffset;
uniform int OutputRayCount;
uniform int DirectSamplesPerPass;
uniform int GISamplesPerPass;
uniform int EnvSamplesPerPass;
uniform int DirectMaxSamples;
uniform int GIMaxSamples;
uniform int EnvMaxSamples;
uniform float PushOff;
uniform int SupersamplingMultiplier;
uniform float OneOverSupersamplingMultiplier;
uniform LightmapMode OutputlightmapMode;
uniform int LodPass;
uniform int DoShadowMask;
uniform int MaxDirectLightsCount;


void setup()
{
    rl_OutputRayCount = OutputRayCount;
}

mat4 GetInstanceTransform(int instanceIndex, out mat4 inv)
{
    int kPixelsPerInstance = 8;
    int linearIdx = instanceIndex * kPixelsPerInstance;
    int y = int(linearIdx/InstanceTransformsWidth);
    int x = linearIdx - y*InstanceTransformsWidth;
    float xTex = float(x)+0.5;
    float yTex = (float(y)+0.5)/float(InstanceTransformsHeight);
    float w = float(InstanceTransformsWidth);

    vec2 uv1 = vec2(xTex/w, yTex);
    vec2 uv2 = vec2((xTex + 1.0)/w, yTex);
    vec2 uv3 = vec2((xTex + 2.0)/w, yTex);
    vec2 uv4 = vec2((xTex + 3.0)/w, yTex);

    vec4 r1 = texture2D(InstanceTransforms, uv1);
    vec4 r2 = texture2D(InstanceTransforms, uv2);
    vec4 r3 = texture2D(InstanceTransforms, uv3);
    vec4 r4 = texture2D(InstanceTransforms, uv4);

    // load the inverse to transform normals
    float inverse_offset = 4.0 / w;
    vec2 iuv1 = vec2(uv1.x + inverse_offset, uv1.y);
    vec2 iuv2 = vec2(uv2.x + inverse_offset, uv2.y);
    vec2 iuv3 = vec2(uv3.x + inverse_offset, uv3.y);
    vec2 iuv4 = vec2(uv4.x + inverse_offset, uv4.y);

    inv[0] = texture2D(InstanceTransforms, iuv1);
    inv[1] = texture2D(InstanceTransforms, iuv2);
    inv[2] = texture2D(InstanceTransforms, iuv3);
    inv[3] = texture2D(InstanceTransforms, iuv4);

    return mat4(r1,r2,r3,r4);
}

vec4 GetInstanceProperties(int instanceIndex)
{
    int y = int(instanceIndex / InstancePropertiesWidth);
    int x = instanceIndex - y*InstancePropertiesWidth;
    float xTex = (float(x) + 0.5) / float(InstancePropertiesWidth);
    float yTex = (float(y) + 0.5) / float(InstancePropertiesHeight);
    return texture2D(InstanceProperties, vec2(xTex, yTex));
}

bool GetReceiveShadows(vec4 instanceProperties)
{
    // Keep in sync with data generation in BakeContextManager::SetInstanceReceiveShadowsData
    return instanceProperties.x > 0.5;
}

int GetLodParams(vec4 instanceProperties, out int lodMask, out float lodT)
{
    lodMask = int(instanceProperties.z);
    lodMask = (lodMask & (-lodMask));
    lodT    = instanceProperties.w;
    return PackLodParam(int(instanceProperties.y), lodMask);
}

bool SkipLodInstance(int lodMask)
{
    return LodPass == 0 ? (lodMask == PVR_LOD_7_BIT) : (lodMask != PVR_LOD_7_BIT);
}

vec2 GetSampleUV (vec2 frameCoord, vec2 frameSize, int sampleStartIndex)
{
    int supersamplingMultiplierSquared = SupersamplingMultiplier * SupersamplingMultiplier;
    int sampleIndex = sampleStartIndex % supersamplingMultiplierSquared;
    int y = int(floor(float(sampleIndex) * OneOverSupersamplingMultiplier));
    int x = sampleIndex - y * SupersamplingMultiplier;

    return (frameCoord - vec2(0.5, 0.5) + (0.5 + vec2(x, y)) * OneOverSupersamplingMultiplier) / frameSize;
}

vec2 GetRandomSampleUV (vec2 frameCoord, vec2 frameSize, int sampleIndex)
{
    float cpShift = GetCranleyPattersonRotation1D(int(frameCoord.x), int(frameCoord.y), 0);
    float ssIDxRand = fract(SobolSample(sampleIndex, 2, 0) + cpShift);

    int ss = SupersamplingMultiplier * SupersamplingMultiplier;
    int ssIDxRandInt = int( floor(float(ss) * ssIDxRand) );

    ssIDxRandInt = ssIDxRandInt % ss;
    int y = int(floor(float(ssIDxRandInt) * OneOverSupersamplingMultiplier));
    int x = ssIDxRandInt - y * SupersamplingMultiplier;

    return (frameCoord - vec2(0.5, 0.5) + (0.5 + vec2(x, y)) * OneOverSupersamplingMultiplier) / frameSize;
}

// Grab the GBuffer data and transform to world space
bool GetGBufferDataWS(vec2 uv, out vec3 position, out vec3 smoothNormal, out vec3 planeNormal, out int instanceIndex)
{
    vec4 interpObjNormal = texture2D(InterpolatedNormalsTex, uv);

    if(interpObjNormal.w < 0.0)
        return false;

    vec4 planeObjNormal = texture2D(PlaneNormalsTex, uv);
    vec4 objPosition = texture2D(PositionsTex, uv);
    instanceIndex = int(floor(objPosition.w)) + TransformOffset;

    mat4 transform_inverse;
    mat4 transform = GetInstanceTransform(instanceIndex, transform_inverse);

    // have to multiply with the transposed inverse, so invert multiplication order
    smoothNormal = normalize(mat3(transform_inverse) * interpObjNormal.xyz);
    planeNormal = normalize(mat3(transform_inverse) * planeObjNormal.xyz);

    position = (vec4(objPosition.xyz, 1.0) * transform).xyz;

    return true;
}

// Some notes on how the sampling works:
// The entire path launched from a sample uses the same sobol index.
// When the ray hits a surface, we evaluate the sobol sequence with the same index but with an increased dimension.
void GISampling(int passRayCount, int currentRayCount, int numRays, LightmapMode lightmapMode)
{
    int passRays = 0;

    for(int i = 0; i < passRayCount; ++i)
    {
        int sampleIndex = currentRayCount + i;
        vec2 gbufferUV = GetRandomSampleUV (rl_FrameCoord.xy, rl_FrameSize.xy, sampleIndex);

        // Get GBuffer data
        vec3 position;
        vec3 smoothNormal;
        vec3 planeNormal;
        int instanceIndex;
        if (!GetGBufferDataWS(gbufferUV, position, smoothNormal, planeNormal, instanceIndex))
            break;

        // LOD handling
        vec4 instanceProperties = GetInstanceProperties(instanceIndex);

        int lodMask;
        float lodT;
        int lodParam = GetLodParams(instanceProperties, lodMask, lodT);

        if (SkipLodInstance(lodMask))
            break;

        vec3 positionPushedOff = position + planeNormal * PushOff;

        // frame of reference for sampling hemisphere
        vec3 b1;
        vec3 b2;
        CreateOrthoNormalBasis(smoothNormal, b1, b2);

        // sample hemisphere
        vec2 rnd;

#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleIndex < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            rnd = vec2( BlueNoiseSobolSample(sampleIndex, 0, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset),
                        BlueNoiseSobolSample(sampleIndex, 1, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset) );
        }
        else
#endif
        {
            vec2 cpShift = GetCranleyPattersonRotation2D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
            rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift);
        }

        vec3 hamDir = HemisphereCosineSample(rnd);
        hamDir = hamDir.x*b1 + hamDir.y*b2 + hamDir.z*smoothNormal;

        passRays++;

        float dotVal = dot(hamDir, planeNormal);
        if (dotVal <= 0.0 || isnan(dotVal))
            continue;

        createRay();
        rl_OutRay.origin           = positionPushedOff;
        rl_OutRay.direction        = hamDir;
        rl_OutRay.color            = vec4(0.0); // unused, because we're not shooting against lights
        rl_OutRay.probeDir         = normalize(hamDir);
        rl_OutRay.renderTarget     = GI_BUFFER;
        rl_OutRay.isOutgoing       = true;
        rl_OutRay.sampleIndex      = sampleIndex;
        rl_OutRay.rayType          = PVR_RAY_TYPE_GI;
        rl_OutRay.rayClass         = MapLodParamToRayClass(PVR_RAY_TYPE_GI, lodParam);
        rl_OutRay.depth            = 0;
        rl_OutRay.weight           = 1.0;
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.albedo           = vec3(1.0);
        rl_OutRay.sameOriginCount  = 0;
        rl_OutRay.transmissionDepth= 0;
        rl_OutRay.lightmapMode     = lightmapMode;
        rl_OutRay.lodParam         = lodParam;
        rl_OutRay.lodOrigin        = rl_OutRay.origin;
        rl_OutRay.originalT        = rl_OutRay.maxT;
        if (lodParam == 0)
        {
            rl_OutRay.defaultPrimitive = GetEnvPrimitive();
        }
        else
        {
            rl_OutRay.maxT             = lodT;
            rl_OutRay.defaultPrimitive = GetLodMissPrimitive();
        }
        emitRayWithoutDifferentials();
    }

    accumulate(GI_SAMPLES_BUFFER, float(passRays));
}



void DirectSampling(int rayBudget, int curDirectSamples, LightmapMode lightmapMode, bool shadowmask)
{
    float maxDirectRcp = 1.0 / float(DirectMaxSamples);
    int   convergedSamples = 0;
    int   startIndex  = curDirectSamples;
    int   globalIndex = curDirectSamples;


    while (rayBudget > 0 && convergedSamples < DirectMaxSamples)
    {
        int lightIndex = int(float(globalIndex) * maxDirectRcp);
        if (lightIndex >= MaxDirectLightsCount)
            break;

        int sampleIndex = globalIndex - lightIndex * DirectMaxSamples;

        vec2 cpShift = GetCranleyPattersonRotation2D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);

        vec2 gbufferUV = GetRandomSampleUV(rl_FrameCoord.xy, rl_FrameSize.xy, sampleIndex);

        // Get GBuffer data
        vec3 position;
        vec3 smoothNormal;
        vec3 planeNormal;
        int instanceIndex;
        if (!GetGBufferDataWS(gbufferUV, position, smoothNormal, planeNormal, instanceIndex))
            break;

        // LOD handling
        vec4 instanceProperties = GetInstanceProperties(instanceIndex);

        int lodMask;
        float lodT;
        int lodParam = GetLodParams(instanceProperties, lodMask, lodT);

        if (SkipLodInstance(lodMask))
            break;

        globalIndex++;
        vec3 positionPushedOff = position + planeNormal * PushOff;

        int totalNumLights = GetTotalNumLights(positionPushedOff);
        if (lightIndex >= totalNumLights)
        {
            convergedSamples += lightIndex == 0 ? 1 : 0;
            continue;
        }

    vec2 rnd;

#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleIndex < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            rnd = vec2( BlueNoiseSobolSample(sampleIndex, 0, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.direct_blue_noise_buffer_offset),
                        BlueNoiseSobolSample(sampleIndex, 1, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.direct_blue_noise_buffer_offset) );
        }
        else
#endif
        {
            vec2 cpShift = GetCranleyPattersonRotation2D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
            rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift);
        }


        bool receiveShadows = GetReceiveShadows(instanceProperties);
        int remaining = DoShadows(lightIndex, 1, positionPushedOff, smoothNormal, vec3(1.0), GI_DIRECT_BUFFER, rnd.xyy, vec3(0.0), lightmapMode, lodParam, lodT, 0, OCCLUSIONMODE_DIRECT, vec4(-1.0), 1.0, receiveShadows);
        if (shadowmask)
            DoShadows(lightIndex, 1, positionPushedOff, smoothNormal, vec3(1.0), SHADOW_MASK_BUFFER, rnd.xyy, vec3(0.0), LIGHTMAPMODE_NOTUSED, lodParam, lodT, 0, OCCLUSIONMODE_SHADOWMASK, vec4(-1.0), 1.0, receiveShadows);

        convergedSamples += remaining == 0 ? 1 : 0; // accumulate the sample if it converged during this iteration
        rayBudget--;
    }

    int accValue = globalIndex - startIndex;
    accumulate(GI_DIRECT_BUFFER, vec4(0.0, 0.0, 0.0, convergedSamples));
    accumulate(DIRECT_SAMPLES_BUFFER, float(accValue));
}


void EnvironmentSampling(int passRayCount, int currentRayCount, LightmapMode lightmapMode)
{
    if (!SampleDirectEnvironment())
    {
        accumulate(ENV_SAMPLES_BUFFER, float(passRayCount));
        return;
    }

    int  passRays = 0;
    bool useIS = UseEnvironmentImportanceSampling();

    for (int i = 0; i < passRayCount; ++i)
    {
        int sampleIndex = currentRayCount + i;

        // Get position and normal for a sample position
        vec3 position, interpNormal, geomNormal;
        vec3 rand;
        int  lodParam = 0;
        float lodT;
        {
            // sample gbuffer data
            vec2 gbufferUV = GetRandomSampleUV(rl_FrameCoord.xy, rl_FrameSize.xy, sampleIndex);
            int  instanceIndex;

            if (!GetGBufferDataWS(gbufferUV, position, interpNormal, geomNormal, instanceIndex))
                break;

            // LOD handling (need to do this per sample, as different instances could possibly by mapped to the same lightmap texel)
            vec4 instanceProperties = GetInstanceProperties(instanceIndex);

            int lodMask;
            lodParam = GetLodParams(instanceProperties, lodMask, lodT);

            if (SkipLodInstance(lodMask))
                break;

            position += geomNormal * PushOff;

            // create 3d random variable
#if PLM_USE_BLUE_NOISE_SAMPLING
            if(sampleIndex < PLM_BLUE_NOISE_MAX_SAMPLES)
            {
            rand = vec3( BlueNoiseSobolSample(sampleIndex, 0, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.env_blue_noise_buffer_offset),
                         BlueNoiseSobolSample(sampleIndex, 1, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.env_blue_noise_buffer_offset),
                         BlueNoiseSobolSample(sampleIndex, 2, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.env_blue_noise_buffer_offset) );
            }
            else
#endif
            {
                vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
                rand = fract(vec3(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0), SobolSample(sampleIndex, 2, 0)) + cpShift);
            }
        }

        if (useIS)
            SurfaceSampleEnvironmentIS(ENV_BUFFER, position, vec3(0.0), interpNormal, geomNormal, vec3(1.0, 1.0, 1.0), rand, 1.0, 0, 0, lightmapMode, lodParam, lodT, false);
        else
            SurfaceSampleEnvironment(ENV_BUFFER, position, vec3(0.0), interpNormal, geomNormal, vec3(1.0, 1.0, 1.0), rand.xy, 1.0, 0, 0, lightmapMode, lodParam, lodT, false);

        passRays++;
    }
    accumulate(ENV_SAMPLES_BUFFER, float(passRays));
}


void main()
{
    vec2  frameCoord  = rl_FrameCoord.xy / rl_FrameSize.xy;


#if ENABLE_CULLING
    vec4 cull = texture2D(CullingMap, frameCoord);
    if(cull.r <= 0.0)
        return;
#endif
    int curGISamples = int(texture2D(GISamplesMap, frameCoord).x);
    int curDirectSamples = int(texture2D(DirectSamplesMap, frameCoord).x);
    int curEnvSamples = int(texture2D(EnvSamplesMap, frameCoord).x);

    int conv = int(texture2D(ConvergenceMap, frameCoord).x * 255.0);
    // Check against midpoints between values defined in the convergence job.
    bool isDirectConverged = (conv & PVR_CONVERGED_DIRECT) != 0;
    bool isGIConverged     = (conv & PVR_CONVERGED_GI)     != 0;
    bool isEnvConverged    = (conv & PVR_CONVERGED_ENV)    != 0;

    if (!isGIConverged && GISamplesPerPass > 0)
    {
        // Avoid overshooting GI samples
        int clampedGIsamplesPerPass = min(max(0, GIMaxSamples - curGISamples), GISamplesPerPass);
        GISampling(clampedGIsamplesPerPass, curGISamples, GIMaxSamples, OutputlightmapMode);
    }

    if (!isDirectConverged && DirectSamplesPerPass > 0)
    {
        DirectSampling(DirectSamplesPerPass, curDirectSamples, OutputlightmapMode, DoShadowMask == 1);
    }

    if (!isEnvConverged && EnvSamplesPerPass > 0)
    {
        int clampedEnvSamplesPerPass = min(max(0, EnvMaxSamples - curEnvSamples), EnvSamplesPerPass);
        EnvironmentSampling(clampedEnvSamplesPerPass, curEnvSamples, OutputlightmapMode);

    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\GIBakeFrameShader.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Integrator.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniformblock IntegratorSamples {
    float goldenSamples[MAX_GOLDEN_SAMPLES];
    int sobolMatrices[SOBOL_MATRIX_SIZE];
    int maxBounces;
    int blueNoiseSamplingBuffer[PLM_BLUE_NOISE_SAMPLING_BUFFER_SIZE];
    int blueNoiseRankingBuffer[PLM_BLUE_NOISE_RANKING_BUFFER_SIZE];
    int blueNoiseScramblingBuffer[PLM_BLUE_NOISE_SCRAMBLING_BUFFER_SIZE];
    int gi_blue_noise_buffer_offset;
    int direct_blue_noise_buffer_offset;
    int env_blue_noise_buffer_offset;
    int minBounces;
};

float BlueNoiseSobolSample(int index, int dimension, int texel_x, int texel_y, int blue_noise_buffer_offset)
{
    // wrap arguments
    int pixel_i = texel_x % PLM_BLUE_NOISE_TILE_SIZE;
    int pixel_j = texel_y % PLM_BLUE_NOISE_TILE_SIZE;
    int sampleIndex = index % PLM_BLUE_NOISE_MAX_SAMPLES;
    int sampleDimension = dimension % PLM_BLUE_NOISE_MAX_DIMENSIONS;

    int rankedSampleIndex = sampleIndex ^ IntegratorSamples.blueNoiseRankingBuffer[blue_noise_buffer_offset + sampleDimension%PLM_BLUE_NOISE_MAX_RANKING_DIMENSIONS + (pixel_i + pixel_j * PLM_BLUE_NOISE_TILE_SIZE) * PLM_BLUE_NOISE_MAX_RANKING_DIMENSIONS];

    // fetch value in sequence
    int value = IntegratorSamples.blueNoiseSamplingBuffer[sampleDimension + (rankedSampleIndex * PLM_BLUE_NOISE_MAX_DIMENSIONS)];

    // If the dimension is optimized,
    //xor sequence value based on optimized scrambling
    value = value ^ IntegratorSamples.blueNoiseScramblingBuffer[blue_noise_buffer_offset +  (sampleDimension % PLM_BLUE_NOISE_MAX_SCRAMBLING_DIMENSIONS) + (pixel_i + pixel_j * PLM_BLUE_NOISE_TILE_SIZE) * PLM_BLUE_NOISE_MAX_SCRAMBLING_DIMENSIONS];
    // convert to float and return
    float v = float(value);
    v = (0.5 + v) / 256.0;
    return v;
}

// Sample Sobol sequence
#define MATSIZE 52
float SobolSample (int index, int dimension, int scramble)
{
    int result = scramble;
    for (int i = dimension * MATSIZE; index != 0; index >>= 1, ++i)
    {
        if ((index & 1) != 0)
            result ^= int(IntegratorSamples.sobolMatrices[i]);
    }
    float res = float(result) * 2.3283064365386963e-10; // (1.f / (1ULL << 32));
    return (res < 0.0 ? res + 1.0 : res);
}

vec3 GetCranleyPattersonRotation3D(int texel_x, int texel_y, int base_dimension)
{
    //We use a modulo on base_dimension+0, base_dimension+1 and base_dimension+2 to be sure the texel doesn't uses random numbers from another texels, leading to correlation issues.
    //It can happen if someone calls this function with a dimension that exceed what we have accounted for.
    //So far we accounted for PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION.

    //We also bound the texel index in a PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP*PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP square to avoid consuming too much memory with high res lightmaps
    int texel_index = (texel_x% PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP ) + (texel_y% PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP )*min(int(rl_FrameSize.x),PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP);

    int dim0_rnd = texel_index * PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION + (base_dimension + 0) % PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION;
    int dim1_rnd = texel_index * PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION + (base_dimension + 1) % PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION;
    int dim2_rnd = texel_index * PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION + (base_dimension + 2) % PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION;

    return vec3(IntegratorSamples.goldenSamples[dim0_rnd], IntegratorSamples.goldenSamples[dim1_rnd], IntegratorSamples.goldenSamples[dim2_rnd]);
}

vec2 GetCranleyPattersonRotation2D(int texel_x, int texel_y, int base_dimension)
{
    return GetCranleyPattersonRotation3D(texel_x, texel_y, base_dimension).xy;
}

float GetCranleyPattersonRotation1D(int texel_x, int texel_y, int base_dimension)
{
    return GetCranleyPattersonRotation3D(texel_x, texel_y, base_dimension).x;
}

vec2 Rotate2D (float angle, vec2 point)
{
    float cosAng = cos (angle);
    float sinAng = sin (angle);
    return vec2 (point.x*cosAng - point.y*sinAng, point.y*cosAng + point.x*sinAng);
}

// Map sample on square to disk (http://psgraphics.blogspot.com/2011/01/improved-code-for-concentric-map.html)
vec2 MapSquareToDisk (vec2 uv)
{
    float phi;
    float r;

    float a = uv.x * 2.0 - 1.0;
    float b = uv.y * 2.0 - 1.0;

    if (a * a > b * b)
    {
        r = a;
        phi = KQUARTERPI * (b / a);
    }
    else
    {
        r = b;

        if (b == 0.0)
        {
            phi = KHALFPI;
        }
        else
        {
            phi = KHALFPI - KQUARTERPI * (a / b);
        }
    }

    return vec2(r * cos(phi), r * sin(phi));
}

vec3 HemisphereCosineSample (vec2 rnd)
{
    vec2 diskSample = MapSquareToDisk(rnd);
    return vec3(diskSample.x, diskSample.y, sqrt(1.0 - dot(diskSample,diskSample)));
}

vec3 SphereSample(vec2 rnd)
{
    float ct = 1.0 - 2.0 * rnd.y;
    float st = sqrt(1.0 - ct * ct);

    float phi = KTWOPI * rnd.x;
    float cp = cos(phi);
    float sp = sin(phi);

    return vec3 (cp * st, sp * st, ct);
}

// iHash can end up being up to INT_MAX.
// 1. Some usages were adding other non-negative values to it and then doing modulo operation. The modulo operator can return a negative value for a negative dividend
// and a positive divisor (well, it always does that unless the remainder is 0).
// 2. Similarly abs(INT_MIN) (where we can get INT_MIN from e.g. INT_MAX+1) gives INT_MIN again, as -INT_MIN can't be stored as an int in two's complement representation.
// In either case, the result of those operations couldn't be used as an index into an array.
// We could either:
// a. get the negative value conditionally back into the [0;divisor) range by adding the divisor;
// b. bring the result of iHash into a more sensible range first (by doing the modulo operation) and only then add other non-negative values to it.
// We should not:
// c. use abs() on the (possibly) nagative modulo result of 1. That doesn't behave nicely when the value jumps from INT_MAX to INT_MIN, because the result (that was
// monotonically increasing up until now) starts decreasing, so we would reuse some array items and miss some others when using the result of those operations as an array index.
// Option b. is recommended.
int GetScreenCoordHash(vec2 pixel)
{
    // combine the x and y into a 32-bit int
    int iHash = ((int(dFstrip(pixel.y)) & 0xffff) << 16) + (int(dFstrip(pixel.x)) & 0xffff);

    iHash -= (iHash << 6);
    iHash ^= (iHash >> 17);
    iHash -= (iHash << 9);
    iHash ^= (iHash << 4);
    iHash -= (iHash << 3);
    iHash ^= (iHash << 10);
    iHash ^= (iHash >> 15);

    iHash &= 0x7fffffff; //make sure it's not negative

    return iHash;
}

vec3 GetRotatedHemisphereSample (vec2 rndSq, float rnd)
{
    float rot = rnd * KTWOPI;
    vec3 hamDir = HemisphereCosineSample(rndSq);
    return vec3(Rotate2D(rot, hamDir.xy).xy, hamDir.z);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Integrator.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\LightImplement.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

void main()
{
    Accumulate(rl_InRay.renderTarget, rl_InRay.weight * rl_InRay.color * vec4(rl_InRay.albedo,1.0), rl_InRay.probeDir, rl_InRay.lightmapMode);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\LightImplement.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Lighting.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/


#define OcclusionMode int
#define OCCLUSIONMODE_DIRECT           0
#define OCCLUSIONMODE_SHADOWMASK       1
#define OCCLUSIONMODE_PROBEOCCLUSION   2
#define OCCLUSIONMODE_DIRECT_ON_BOUNCE 3 // allows to skip the check whether to do direct

//keep that enum in sync with enum AngularFalloffType in wmLight.h and LightmapBake.cpp
//it allows to switch between different falloff computation
//for spotlights
#define ANGULARFALLOFFTYPE_LUT                         0
#define ANGULARFALLOFFTYPE_ANALYTIC_AND_INNER_ANGLE    1


uniformblock Lights
{
    // Per light data
    // Bytes per light: 116 = 3 * sizeof(int) + 3 * sizeof(vec3) + 4 * sizeof(vec4) + 1 * sizeof(float)
    // MB for all lights: 3.6 = (116 * MAX_LIGHTS) / 1024^2
    int       LightTypes[PVR_MAX_LIGHTS];
    int       LightShadowMaskChannels[PVR_MAX_LIGHTS];
    int       LightFalloffIndex[PVR_MAX_LIGHTS];
    vec3      LightPositions[PVR_MAX_LIGHTS];
    vec3      LightDirections[PVR_MAX_LIGHTS];
    vec3      LightTangents[PVR_MAX_LIGHTS];
    vec4      LightProperties0[PVR_MAX_LIGHTS];
    vec4      LightProperties1[PVR_MAX_LIGHTS];
    vec4      LightProperties2[PVR_MAX_LIGHTS];
    vec4      LightColors[PVR_MAX_LIGHTS];
    float     LightPowerDistributions[PVR_MAX_LIGHTS];

    // Per region data
    ivec2     NumLights[PVR_MAX_LIGHT_REGIONS]; // x = number of lights, y = offset into the cdf array
    float     CumulativePowerDistributions[PVR_MAX_CDFS]; // first element in a region slice is the cdf sum

    // Global data
    int       MaxDirLights;
    vec3      SceneBoundsMin;
    vec3      SceneBoundsMax;
    vec3      GridRegionSize;
    ivec3     GridDims;
    int       GridLength;
    int       RaysPerSoftShadow;
    int       TotalLights;
    vec2      LightIndicesRes;
};

uniformblock LightInfo
{
    vec4      AmbientSH[7];
    float     AmbientIntensity;
    primitive LightPrimitive;
    primitive EnvPrimitive;
    primitive LodMissPrimitive;
    float     AngularFalloffTable[MAX_ANGULAR_FALLOFF_TABLE_LENGTH];
    int       AngularFalloffTableLength;
};

uniformblock FalloffInfo
{
    sampler2D LightFalloff;
    int       LightFalloffWidth;
    int       LightFalloffHeight;
};

uniformblock LightCookieInfo
{
    vec4    CookieAtlasHalfTexelSize;
    vec4    ScaleOffset[PVR_MAX_COOKIES];
    int     LightToScaleOffset[PVR_MAX_LIGHTS];
};

uniformblock AOInfo
{
    float aoMaxDistance;
    int aoEnabled;
};

uniform sampler2D LightIndices;
uniform sampler2D LightCookies;

float SampleFalloff(int falloffIndex, float normalizedSamplePosition)
{
    int y = min(falloffIndex, FalloffInfo.LightFalloffHeight-1);
    int sampleCount = FalloffInfo.LightFalloffWidth;
    float index = normalizedSamplePosition*float(sampleCount);

    // compute the index pair
    int loIndex = min(int(index), int(sampleCount - 1));
    int hiIndex = min(int(index) + 1, int(sampleCount - 1));
    float hiFraction = (index - float(loIndex));

    float yTex = (float(y) + 0.5) / float(FalloffInfo.LightFalloffHeight);
    float xTexLo = (float(loIndex) + 0.5) / float(FalloffInfo.LightFalloffWidth);
    float xTexHi = (float(hiIndex) + 0.5) / float(FalloffInfo.LightFalloffWidth);

    vec2 uv1 = vec2(xTexLo, yTex);
    vec2 uv2 = vec2(xTexHi, yTex);

    vec4 sampleLo = texture2D(FalloffInfo.LightFalloff, uv1);
    vec4 sampleHi = texture2D(FalloffInfo.LightFalloff, uv2);

    // do the lookup
    return (1.0 - hiFraction) * sampleLo.x + hiFraction * sampleHi.x;
}

// This code must be kept in sync with FalloffLUT.cpp::LookupFalloffLUT
float LookupAngularFalloffLUT(float angularScale)
{
    int sampleCount = LightInfo.AngularFalloffTableLength;

    //======================================
    // light distance falloff lookup:
    //   d = Max(0, distance - m_Radius) / (m_CutOff - m_Radius)
    //   index = (g_SampleCount - 1) / (1 + d * d * (g_SampleCount - 2))
    float tableDist = max(angularScale, 0.0);
    float index = float(sampleCount - 1) / (1.0 + tableDist * tableDist * float(sampleCount - 2));

    // compute the index pair
    int loIndex = min(int(index), int(sampleCount - 1));
    int hiIndex = min(int(index) + 1, int(sampleCount - 1));
    float hiFraction = (index - float(loIndex));

    // do the lookup
    return (1.0 - hiFraction) * LightInfo.AngularFalloffTable[loIndex] + hiFraction * LightInfo.AngularFalloffTable[hiIndex];
}

primitive GetLightPrimitive()
{
    return LightInfo.LightPrimitive;
}

primitive GetEnvPrimitive()
{
    return LightInfo.EnvPrimitive;
}

// LoD related routines
primitive GetLodMissPrimitive()
{
    return LightInfo.LodMissPrimitive;
}

int PackLodParam(int LODGroupId, int LODMask)
{
    return (LODGroupId < 0 || LODMask == PVR_LOD_0_BIT) ? 0 :
        ((LODGroupId & PVR_LODGROUP_MASK) | (LODMask << PVR_LODMASK_SHIFT));
}

void UnpackLodParam(int lodParam, out int groupId, out int mask)
{
    if (lodParam == 0)
    {
        groupId = -1;
        mask = PVR_LOD_0_BIT;
    }
    else
    {
        groupId = lodParam & PVR_LODGROUP_MASK;
        mask    = (lodParam >> PVR_LODMASK_SHIFT) & 0xff;
    }
}

int MapLodParamToRayClass(int rayType, int lodParam)
{
    if (lodParam == 0)
        return (rayType == PVR_RAY_TYPE_GI || rayType == PVR_RAY_TYPE_ENV) ? PVR_RAY_CLASS_GI : PVR_RAY_CLASS_OCC;

    int lodMask   = (lodParam >> PVR_LODMASK_SHIFT) & 0xff;
    int shift     = (lodMask == PVR_LOD_7_BIT) ? 2 : 1;
        lodMask >>= shift;
    int rayClass  = PVR_RAY_CLASS_LOD_0;
    do
    {
        rayClass++;
        lodMask >>= 1;
    }
    while (lodMask != 0);

    return rayClass;
}

void ReshootLodRay(vec3 origin, float originalT, int depth, int rayType)
{
    createRay();
    rl_OutRay.origin           = origin;
    rl_OutRay.maxT             = originalT;
    rl_OutRay.depth            = depth;
    rl_OutRay.rayType          = rayType;
    rl_OutRay.rayClass         = (rayType == PVR_RAY_TYPE_GI || rayType == PVR_RAY_TYPE_ENV) ? PVR_RAY_CLASS_GI : PVR_RAY_CLASS_OCC;
    rl_OutRay.defaultPrimitive = GetEnvPrimitive();

    if (rayType == PVR_RAY_TYPE_SHADOW)
    {
        rl_OutRay.defaultPrimitive = GetLightPrimitive();
    }

    emitRayWithoutDifferentials();
}

int GetLightIndex(int regionIdx, int lightIdx)
{
    vec2 uv = vec2(((float(lightIdx) + 0.5) / float(Lights.LightIndicesRes.x)), ((float(regionIdx) + 0.5) / float(Lights.LightIndicesRes.y)));

    float idx = texture2D(LightIndices, uv).x;

    // Integers up to 2^24 can be accurately represented as floats,
    // so a simple truncating conversion to int below is fine.
    return clamp(int(idx), 0, Lights.TotalLights - 1 + PLM_MAX_DIR_LIGHTS);
}

ivec3 GetRegionLocContaining(vec3 point)
{
    vec3 p = point-Lights.SceneBoundsMin;


    return ivec3(int(p.x/Lights.GridRegionSize.x), int(p.y/Lights.GridRegionSize.y), int(p.z/Lights.GridRegionSize.z));
}

int GetRegionContaining(vec3 point)
{
    return OpenRLCPPShared_GetRegionIdx(GetRegionLocContaining(point), Lights.GridDims);
}

int GetMaxDirLights()
{
    return Lights.MaxDirLights;
}

int GetNumLights(int regionIdx)
{
    return regionIdx < 0 ? 0 : Lights.NumLights[regionIdx].x;
}

int GetTotalNumLights(vec3 point)
{
    return GetMaxDirLights() + GetNumLights(GetRegionContaining(point));
}

void GetCdf(int regionIdx, out float cdfSumRcp, out int cdfOffset, out int cdfCount)
{
    cdfCount   = Lights.NumLights[regionIdx].x+1;
    int offset = Lights.NumLights[regionIdx].y;
    cdfSumRcp  = Lights.CumulativePowerDistributions[offset];
    cdfOffset  = offset + 1;
}

int GetRaysPerSoftShadow()
{
    return Lights.RaysPerSoftShadow;
}

vec3 GetLightPosition(int lightIdx)
{
    return Lights.LightPositions[lightIdx];
}
vec4 GetLightProperties0(int lightIdx)
{
    return Lights.LightProperties0[lightIdx];
}
vec4 GetLightProperties1(int lightIdx)
{
    return Lights.LightProperties1[lightIdx];
}

float GetShadowType(int lightIdx)
{
    return Lights.LightProperties0[lightIdx].w;

}
int  GetLightType(int lightIdx)
{
    return Lights.LightTypes[lightIdx];
}
int  GetShadowMaskChannel(int lightIdx)
{
    return Lights.LightShadowMaskChannels[lightIdx];
}

bool GetLightmapsDoDirect(int lightIdx)
{
    return Lights.LightProperties1[lightIdx].w != 0.0;
}

bool GetLightProbesDoDirect(int lightIdx)
{
    return Lights.LightProperties2[lightIdx].w != 0.0;
}

void GetJitteredLightVec(inout vec3 lightVec, int lightIdx, int rayIdx, vec3 lightOffset, mat3 lightBasis);

bool IsNormalValid(vec3 normal)
{
    return normal != vec3(0.0);
}

vec2 GetCookieSizesRcp(int lightIdx)
{
    return vec2(Lights.LightProperties1[lightIdx].y, Lights.LightProperties1[lightIdx].z); // only valid if this is a directional light
}

void ClampCookieUVs(inout vec2 uvs, vec2 scale)
{
    vec2 halfTexelSize = LightCookieInfo.CookieAtlasHalfTexelSize.xy / scale;
    uvs = clamp(uvs, halfTexelSize, vec2(1.0, 1.0) - halfTexelSize);

}

vec4 GetCookieScaleOffset(int cookieIndex, out bool tileCookie)
{
    vec4 scale_offset = LightCookieInfo.ScaleOffset[cookieIndex];
         tileCookie   = scale_offset.x < 0.0; // the sign bit is set in PVRJobLightCookies.cpp - look for it->tex.repeat
    return vec4(abs(scale_offset.x), scale_offset.y, scale_offset.z, scale_offset.w);
}

vec4 GetCookieScaleOffset(int cookieIndex)
{
    bool tileCookie;
    return GetCookieScaleOffset(cookieIndex, tileCookie);
}


// This function assumes the region contains lights. Check to make sure this is true before calling this function.
int PickLight(in int regionIdx, inout float rand, inout float weight)
{
    // Gather region information
    int   numLights    = GetNumLights(regionIdx);
    float numLightsRcp = 1.0 / float(numLights);
    // Gather cdf data for the region
    float cdfSumRcp;
    int   cdfOffset, cdfCount;
    GetCdf(regionIdx, cdfSumRcp, cdfOffset, cdfCount);

    int lightIdx = -1;
    if (rand >= 0.5) // choose power density
    {
        // do a binary search on the cdfs
        int count = cdfCount;
        int b = 0;
        int it = 0;
        int step = 0;
        // rescale used interval half to [0;1) for next event estimation
        rand = (rand - 0.5) * 2.0;

        while (count > 0)
        {
            it = b;
            step = count / 2;
            it += step;
            if (Lights.CumulativePowerDistributions[cdfOffset + it] < rand)
            {
                b = ++it;
                count -= step + 1;
            }
            else
            {
                count = step;
            }
        }

        int segmentIdx = max(b, 1);
            lightIdx   = GetLightIndex(regionIdx, segmentIdx - 1);
        // rescale the random variable again
            float seg_max = Lights.CumulativePowerDistributions[cdfOffset + segmentIdx];
            float seg_min = Lights.CumulativePowerDistributions[cdfOffset + segmentIdx - 1];
                  rand    = (rand - seg_min) / (seg_max - seg_min);
        /* original is balance heuristic calculation for reference:
         *   float pdf_equi   = numLightsRcp;
         *   float pdf_power  = Lights.LightPowerDistributions[lightIdx] / cdfSum;
         *   float is_weight = pdf_power / (pdf_equi + pdf_power);
         *   weight *= 2.0 * is_weight / pdf_power;
        */
    }
    else
    {
        // rescale used interval half to [0;1) for next event estimation
        rand     = rand * 2.0;
        lightIdx = int(rand * float(numLights));
        lightIdx = GetLightIndex(regionIdx, min(lightIdx, numLights-1));
        /* original is balance heuristic calculation for reference:
         *   float pdf_equi   = numLightsRcp;
         *   float pdf_power  = Lights.LightPowerDistributions[lightIdx] / cdfSum;
         *   float is_weight = pdf_equi / (pdf_equi + pdf_power);
         *   weight *= 2.0 * is_weight / pdf_equi;
         */
    }

    float pdf_equi = numLightsRcp;
    float pdf_power = Lights.LightPowerDistributions[lightIdx] * cdfSumRcp;
    weight *= 2.0 / (pdf_equi + pdf_power);

    return lightIdx;
}

bool CalculateDirectionalLightColor(int lightIdx, vec3 normal, vec3 position, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    bool hasNormal = IsNormalValid(normal);

    float dotVal = hasNormal ? dot(normal, Lights.LightDirections[lightIdx]) : 1.0;
    if (dotVal <= 0.0 || isnan(dotVal))
        return false;

    colorOut *= dotVal;
    lightVecOut = Lights.LightDirections[lightIdx];
    maxTOut = 1e27;

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec3 lightPos      = Lights.LightPositions[lightIdx];
        vec3 toLight       = position - lightPos;
        float dist         = dot(lightVecOut, toLight);
        vec3 projected     = (position + dist * lightVecOut) - lightPos;
        vec3 lightBitan_NZ = cross(Lights.LightDirections[lightIdx], Lights.LightTangents[lightIdx]);
        vec2 scales        = GetCookieSizesRcp(lightIdx);
        vec2 uvs           = vec2(dot(projected, scales.x * lightBitan_NZ),
                                  dot(projected, scales.y * Lights.LightTangents[lightIdx]));
             uvs           = uvs * 0.5 + 0.5;

        bool tileCookie;
        vec4 scale_offset  = GetCookieScaleOffset(cookieIdx, tileCookie);
        uvs = tileCookie ? fract(uvs) : uvs;
        bvec4 inrange = bvec4(uvs.x >= 0.0, uvs.x <= 1.0, uvs.y >= 0.0, uvs.y <= 1.0);
        if (all(inrange))
        {
            ClampCookieUVs(uvs, scale_offset.xy);
            cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
        }
    }

    colorOut *= cookieAttenuation;

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    return true;
}

vec3 vabs(vec3 vec)
{
    return vec3(abs(vec.x), abs(vec.y), abs(vec.z));
}

vec3 sampleCookie(vec3 dir, int lightIdx)
{
    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];

    if (cookieIdx < 0)
        return vec3(1.0, 1.0, 1.0);

    // rotate
    vec3 y = Lights.LightTangents[lightIdx];
    vec3 z = -Lights.LightDirections[lightIdx];
    vec3 x = cross(y, z);
    mat3 rot = mat3(x, y, z);
         dir = dir * rot; // rotate light vector into cubemap frame (transposing here instead of inverse, as rot is orthonormal)

    // find slice
    int slice = 0;

    vec2 uvs;
    vec3 absdir = vabs(dir);
    if (absdir.x >= absdir.y && absdir.x >= absdir.z)
    {
        slice = dir.x >= 0.0 ? 0 : 1;
        uvs = vec2(-dir.z / dir.x, -dir.y / absdir.x);
    }
    else if (absdir.y >= absdir.z)
    {
        slice = dir.y >= 0.0 ? 2 : 3;
        uvs = vec2(dir.x / absdir.y, dir.z / dir.y);
    }
    else
    {
        slice = dir.z >= 0.0 ? 4 : 5;
        uvs = vec2(dir.x / dir.z, -dir.y / absdir.z);
    }

         uvs               = uvs * 0.5 + 0.5;
         vec4 scale_offset = GetCookieScaleOffset(cookieIdx + slice);
         ClampCookieUVs(uvs, scale_offset.xy);
    vec3 cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;

    return cookieAttenuation;
}

bool CalculatePointLightColor(int lightIdx, vec3 shadingNormal, vec3 shadingPosWorld, inout vec3 colorOut, out vec3 lightPosLocalOut, inout float maxTOut, OcclusionMode occlusionMode, bool useCookie)
{
    // Retrieve the range and position from the light properties
    float lightRange = Lights.LightProperties0[lightIdx].x;
    vec3 lightPosWorld = Lights.LightPositions[lightIdx];

    // Calculate the position and distance of the light relative to the shading point
    vec3 lightPosLocal = lightPosWorld - shadingPosWorld;
    float lightDist = length(lightPosLocal);

    // If the shading point is outside the range of the light, we're done
    if (lightDist >= lightRange) { return false; }

    maxTOut = lightDist;

    // Distance is ~0. Just sample the falloff and we're done.
    if (lightDist < PVR_FLT_EPSILON)
    {
        colorOut *= SampleFalloff(Lights.LightFalloffIndex[lightIdx], 0.0);
        lightPosLocalOut = vec3(0.0, 0.0, 0.0);
    }
    else
    {
        // Normalise the local light position (needed by the directional accumulator)
        lightPosLocalOut = lightPosLocal / lightDist;

        // Evaluate the Lambertian BRDF and return if the light is below the horizon
        float cosTheta = IsNormalValid(shadingNormal) ? max(dot(shadingNormal, lightPosLocalOut), 0.0) : 1.0;
        if (cosTheta <= 0.0 || isnan(cosTheta)) { return false; }
        colorOut *= cosTheta;

        // Normalise the light distance to its range and evaluate the falloff
        float normLightDist = lightDist / lightRange;
        colorOut *= SampleFalloff(Lights.LightFalloffIndex[lightIdx], normLightDist);

        // If we're using cookies, evaluate them now
        if (useCookie)
        {
            colorOut *= sampleCookie(-lightPosLocal, lightIdx);
        }
    }

    // If we're using shadowmasks, accumulate them here. CalculateSpotLightColor also uses this function, so test the type.
    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK && GetLightType(lightIdx) == LIGHT_POINT)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    return true;
}


//Angle attenuation code from HDRP CommonLighting.hlsl
// Square the result to smoothen the function.
float HDRPSmoothAngleAttenuation(float cosFwd, float lightAngleScale, float lightAngleOffset)
{
   float attenuation = clamp(cosFwd * lightAngleScale + lightAngleOffset, 0.0, 1.0);
   return attenuation * attenuation;
}

bool CalculateSpotLightColor(int lightIdx, vec3 normal, vec3 position, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    if( CalculatePointLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode, false) == false )
      return false;

    float cosConeAng = Lights.LightProperties0[lightIdx].y;
    float invCosConeAng = 1.0 - cosConeAng;
    float cosInnerAngle =  Lights.LightProperties0[lightIdx].z;
    float dval = dot(lightVecOut, Lights.LightDirections[lightIdx]);

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec3 projected     = Lights.LightDirections[lightIdx] - lightVecOut / dval;
        vec3 lightBitan_NZ = cross(Lights.LightDirections[lightIdx], Lights.LightTangents[lightIdx]);
        float scale = cosConeAng / sqrt(1.0 - cosConeAng * cosConeAng);
        vec2 uvs = vec2(dot(projected, scale * lightBitan_NZ),
                        dot(projected, scale * Lights.LightTangents[lightIdx]));

        if (abs(uvs.x) > 1.0 || abs(uvs.y) > 1.0)
            return false;

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    //uses a LUT table to compute angular falloff to match legacy which uses a texture to compute angular faloff
    int angularFalloffType = int(Lights.LightProperties1[lightIdx].y);

    float angleLimit = (angularFalloffType != ANGULARFALLOFFTYPE_LUT || cookieIdx < 0) ? cosConeAng : 0.0;
    if (dval < angleLimit)
        return false;

    colorOut *= cookieAttenuation;

    if(angularFalloffType == ANGULARFALLOFFTYPE_LUT)
    {
        // builtin cookies completely control angular falloff
        if (cookieIdx < 0)
        {
            float angScale = (dval-cosConeAng)/invCosConeAng;
            float angFalloff = 1.0 - LookupAngularFalloffLUT (angScale);
            colorOut *= angFalloff;
        }
    }
    else//inner angle support AND Analyticfalloff
    {
        //There is no attenuation inside inner angle
        if(dval >= cosInnerAngle )
        {
            //nothing to do here
        }
        else//Otherwise match HDRP analytic formula for attenuation
        {
            float angleScale = 1.0 / max(0.0001, (cosInnerAngle - cosConeAng));
            float lightAngleOffset = -cosConeAng * angleScale;

            float angFalloff = HDRPSmoothAngleAttenuation(dval, angleScale, lightAngleOffset);
            colorOut *= angFalloff;
        }
    }

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }
    return true;
}

void CreateOrthoNormalBasis(vec3 n, out vec3 tangent, out vec3 bitangent);

#define PI 3.14159265359

vec3 SphQuadSample(vec3 s, vec3 ex, vec3 ey, vec3 o, float u, float v, inout float solidAngle)
{
    float exl = length(ex);
    float eyl = length(ey);
    // compute local reference system 'R'
    vec3 x = ex / exl;
    vec3 y = ey / eyl;
    vec3 z = cross(x, y);
    // compute rectangle coords in local reference system
    vec3 d = s - o;
    float z0 = dot(d, z);
    // flip 'z' to make it point against 'Q'
    if (z0 > 0.0) {
        z *= -1.0;
        z0 *= -1.0;
    }
    float z0sq = z0 * z0;
    float x0 = dot(d, x);
    float y0 = dot(d, y);
    float x1 = x0 + exl;
    float y1 = y0 + eyl;
    float y0sq = y0 * y0;
    float y1sq = y1 * y1;
    // create vectors to four vertices
    vec3 v00 = vec3(x0, y0, z0);
    vec3 v01 = vec3(x0, y1, z0);
    vec3 v10 = vec3(x1, y0, z0);
    vec3 v11 = vec3(x1, y1, z0);
    // compute normals to edges
    vec3 n0 = normalize(cross(v00, v10));
    vec3 n1 = normalize(cross(v10, v11));
    vec3 n2 = normalize(cross(v11, v01));
    vec3 n3 = normalize(cross(v01, v00));
    // compute internal angles (gamma_i)
    float g0 = acos(-dot(n0,n1));
    float g1 = acos(-dot(n1,n2));
    float g2 = acos(-dot(n2,n3));
    float g3 = acos(-dot(n3,n0));
    // compute predefined constants
    float b0 = n0.z;
    float b1 = n2.z;
    float b0sq = b0 * b0;
    float k = 2.0*PI - g2 - g3;
    // compute solid angle from internal angles
    float S = g0 + g1 - k;
    solidAngle = S;

    // 1. compute 'cu'
    float au = u * S + k;
    float fu = (cos(au) * b0 - b1) / sin(au);
    float cu = 1.0/sqrt(fu*fu + b0sq) * (fu>0.0 ? 1.0 : -1.0);
    cu = clamp(cu, -1.0, 1.0); // avoid NaNs
    // 2. compute 'xu'
    float xu = -(cu * z0) / sqrt(1.0 - cu*cu);
    xu = clamp(xu, x0, x1); // avoid Infs
    // 3. compute 'yv'
    float d_ = sqrt(xu*xu + z0sq);
    float h0 = y0 / sqrt(d_*d_ + y0sq);
    float h1 = y1 / sqrt(d_*d_ + y1sq);
    float hv = h0 + v * (h1-h0), hv2 = hv*hv;
    float eps = 0.0001;
    float yv = (hv2 < 1.0-eps) ? (hv*d_)/sqrt(1.0-hv2) : y1;

    // 4. transform (xu,yv,z0) to world coords
    return (o + xu*x + yv*y + z0*z);
}

bool SphQuadSampleDir(vec3 s, vec3 ex, vec3 ey, vec3 o, vec2 sq, out float solidAngle, out vec3 rayDir, out float rayMaxT)
{
    rayDir = SphQuadSample(s, ex, ey, o, sq.x, sq.y, solidAngle) - o;
    rayMaxT = length(rayDir);
    rayDir /= rayMaxT;

    return !isnan(solidAngle) && solidAngle > 0.0 && rayMaxT >= PVR_FLT_EPSILON;
}

//Do the lighting calculation for the provided position+normal
bool CalculateAreaLightColor(int lightIdx, vec3 normal, vec3 position, vec2 rnd, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    vec3 lightDir = normalize(Lights.LightDirections[lightIdx]);
    vec3 lightCenter = Lights.LightPositions[lightIdx];
    vec3 texelToLight = position-lightCenter;

    // light backfacing?
    if(dot(lightDir, texelToLight) > 0.0)
        return false;

    // range check
    float range = Lights.LightProperties0[lightIdx].x;
    float ttlDistSq = dot( texelToLight, texelToLight );
    if (ttlDistSq > (range*range))
        return false;

    float width = Lights.LightProperties1[lightIdx].y;
    float height = Lights.LightProperties1[lightIdx].z;

    // solid angle sampling
    vec3 lightTan = normalize(Lights.LightTangents[lightIdx]);
    vec3 lightBitan = cross(lightDir,lightTan);
    vec3 s = lightCenter - 0.5 * width * lightBitan- 0.5 * height * lightTan;

    float solidAngle, tempMaxTout;
    vec3 templightVecOut;
    if(!SphQuadSampleDir(s, lightTan * height, lightBitan * width, position, rnd, solidAngle, templightVecOut, tempMaxTout))
        return false;

    lightVecOut= templightVecOut;
    maxTOut = tempMaxTout;

    // evaluation (Note: we should  not do the division by width * height here)
    bool hasNormal = (normal != vec3(0.0));  // probes do not supply normals to this calculation
    float nDotL =  hasNormal ? max(0.0, dot(lightVecOut, normal)) : 1.0;

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec3 edge = position + lightVecOut * maxTOut - lightCenter;
        vec2 uvs = vec2(2.0 * dot(edge, -lightBitan) / width, 2.0 * dot(edge, lightTan) / height);

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    // Only accumulate samples when the sample on the rectangle is visible from this texel.
    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK && nDotL > 0.0)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    colorOut *= solidAngle * nDotL / PI * cookieAttenuation;
    return true;
}

//Do the lighting calculation for the provided position+normal
bool CalculateDiscLightColor(int lightIdx, vec3 normal, vec3 position, vec2 rnd, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    // check for early out
    vec3 lightDir = -normalize(Lights.LightDirections[lightIdx]); // we negate to undo negation in Wintermute/Scene.cpp
    vec3 lightCenter = Lights.LightPositions[lightIdx];
    vec3 texelToLight = position - lightCenter;  // account for area light size?

    // light backfacing?
    if(dot(lightDir, texelToLight) < 0.0)
        return false;

    // range check
    float range = Lights.LightProperties0[lightIdx].x;
    float ttlDistSq = dot( texelToLight, texelToLight );
    if (ttlDistSq > (range*range))
        return false;

    // Sample uniformly on 2d disc area
    float radius = Lights.LightProperties1[lightIdx].y;

    float rLocal = sqrt(rnd.x);
    float thetaLocal = 2.0 * PI * rnd.y;
    vec2 samplePointLocal = vec2(cos(thetaLocal), sin(thetaLocal)) * rLocal * radius;

    // Convert sample point to world space
    vec3 lightTan = normalize(Lights.LightTangents[lightIdx]);
    vec3 lineCross = cross(lightDir, lightTan);
    vec3 samplePointWorld = lightCenter + samplePointLocal.x * lightTan + samplePointLocal.y * lineCross;

    // Calc contribution etc.
    lightVecOut = samplePointWorld - position;
    maxTOut = length(lightVecOut);
    if (maxTOut < PVR_FLT_EPSILON)
        return false;

    lightVecOut /= maxTOut;
    bool hasNormal = (normal != vec3(0.0)); // probes do not supply normals to this calculation
    float nDotL = hasNormal ? max(0.0, dot(lightVecOut, normal)) : 1.0;

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        float scale = 1.0 / radius;
        vec3  edge  = position + lightVecOut * maxTOut - lightCenter;
        vec2  uvs   = vec2(dot(edge, lineCross) * scale, dot(edge, lightTan) * scale);

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    // Only accumulate samples when the sample on the rectangle is visible from this texel.
    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK && nDotL > 0.0)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    // * (Pi / Pi) removed from the expression below as it cancels out.
    colorOut *= cookieAttenuation * (nDotL * radius * radius * dot(lightDir, -lightVecOut)) / (maxTOut * maxTOut);
    return true;
}

bool CalculateBoxLightColor(int lightIdx, vec3 normal, vec3 position, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    bool hasNormal  = IsNormalValid(normal);
    vec3 toLight_NZ = Lights.LightDirections[lightIdx];
    vec3 lightPos   = Lights.LightPositions[lightIdx];

    float dotVal = hasNormal ? dot(normal, toLight_NZ) : 1.0;
    if (dotVal <= 0.0 || isnan(dotVal))
        return false;

    // check if the shading point is within range and in front
    float range         = Lights.LightProperties0[lightIdx].x;
    float projectedDist = dot(toLight_NZ, lightPos - position);
    if (projectedDist < 0.0 || projectedDist > range)
        return false;
    // check if the shading point is contained within the box
    vec3  lightTan_NZ   = Lights.LightTangents[lightIdx];
    vec3  lightBitan_NZ = cross(toLight_NZ, lightTan_NZ);
    vec3  pos2       = lightPos - toLight_NZ * projectedDist;
    vec3  edge       = position - pos2;
    float width      = abs(2.0 * dot(edge, lightBitan_NZ));
    float height     = abs(2.0 * dot(edge, lightTan_NZ));
    float box_width  = Lights.LightProperties1[lightIdx].y;
    float box_height = Lights.LightProperties1[lightIdx].z;
    if (width > box_width || height > box_height)
        return false;

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec2 uvs = vec2(2.0 * dot(edge, lightBitan_NZ) / box_width, 2.0 * dot(edge, lightTan_NZ) / box_height);

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    // box lights behave like directional lights, so we only
    // attenuate by the surface normal and light direction
    lightVecOut = toLight_NZ;
    colorOut   *= dotVal * cookieAttenuation;
    maxTOut     = projectedDist;

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    return true;
}

bool CalculatePyramidLightColor(int lightIdx, vec3 normal, vec3 position, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    bool  hasNormal   = IsNormalValid(normal);
    vec3  lightPos    = Lights.LightPositions[lightIdx];
    vec3  lightDir_NZ = -Lights.LightDirections[lightIdx]; // we're actually passing in transform z of a right handed coordinate system, so flip the dir
    vec3  toLight     = lightPos - position;
    float range       = Lights.LightProperties0[lightIdx].x;

    float dist       = length(toLight);
    vec3  toLight_NZ = toLight / dist;

    // out of range or wrong side?
    float dotVal = hasNormal ? dot(normal, toLight_NZ) : 1.0;
    if (dist > range || dotVal <= 0.0 || isnan(dotVal))
        return false;

    float projectedDist = dot(-toLight, lightDir_NZ);
    if (projectedDist <= 0.0)
        return false;

    // check if the shading point is contained within the pyramid
    vec3  pos2 = lightPos + lightDir_NZ;
    vec3  pos3 = lightPos - (toLight / projectedDist);
    vec3  edge =  pos3 - pos2;

    vec3  lightTan_NZ    = Lights.LightTangents[lightIdx];
    vec3  lightBitan_NZ  = cross(lightTan_NZ, lightDir_NZ);
    float width          = abs(2.0 * dot(edge, lightBitan_NZ));
    float height         = abs(2.0 * dot(edge, lightTan_NZ));
    float pyramid_width  = Lights.LightProperties1[lightIdx].y;
    float pyramid_height = Lights.LightProperties1[lightIdx].z;

    if (width > pyramid_width || height > pyramid_height)
        return false;

    // process attenuation based on distance and angle
    float distFalloff = SampleFalloff(Lights.LightFalloffIndex[lightIdx], dist / range);

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec2 uvs = vec2(2.0 * dot(edge, lightBitan_NZ) / pyramid_width, 2.0 * dot(edge, lightTan_NZ) / pyramid_height);

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    // initialize outputs
    colorOut   *= distFalloff * dotVal * cookieAttenuation;
    lightVecOut = toLight_NZ;
    maxTOut     = dist;

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    return true;
}


//Do the lighting calculation for the provided position+normal
bool CalculateLightColor(int lightIdx, vec3 normal, vec3 position, bool bounce, vec2 rnd, out vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    if (bounce)
        colorOut = Lights.LightProperties2[lightIdx].xyz;
    else
        colorOut = Lights.LightColors[lightIdx].xyz;

    int lightType = GetLightType(lightIdx);

    if (lightType == LIGHT_DIRECTIONAL)
    {
        return CalculateDirectionalLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_POINT)
    {
        return CalculatePointLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode, true);
    }
    else if (lightType == LIGHT_SPOT)
    {
        return CalculateSpotLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_AREA)
    {
        return CalculateAreaLightColor(lightIdx, normal, position, rnd, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_DISC)
    {
        return CalculateDiscLightColor(lightIdx, normal, position, rnd, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_BOX)
    {
        return CalculateBoxLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_PYRAMID)
    {
        return CalculatePyramidLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode);
    }

    return false;
}

vec3 PointInCosLobe(vec2 uv)
{
    float theta = acos(sqrt(1. - uv.x));
    float phi   = 2. * 3.14159 * uv.y;
    return vec3(cos(phi) * sin(theta), sin(phi) * sin(theta), cos(theta));
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Lighting.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\LodMissShader.rlsl---------------




void setup()
{
    rl_OutputRayCount[PVR_RAY_CLASS_GI   ] = 0;
    rl_OutputRayCount[PVR_RAY_CLASS_OCC  ] = 0;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_1] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_2] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_3] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_4] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_5] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_6] = 1;
}


void main()
{
    ReshootLodRay(rl_InRay.lodOrigin, rl_InRay.originalT, rl_InRay.depth, rl_InRay.rayType);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\LodMissShader.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\OpenRLCPPSharedIncludes.rlsl---------------


#pragma once

#if defined(UNITY_EDITOR)
#include "External/Wintermute/Vector.h"
#define SHARED_int3_type Wintermute::Vec3i
#define SHARED_CONST const
#define SHARED_INLINE inline
#else
#define SHARED_int3_type ivec3
#define SHARED_CONST in
#define SHARED_INLINE
#endif // UNITY_EDITOR

SHARED_INLINE int OpenRLCPPShared_GetRegionIdx(SHARED_CONST SHARED_int3_type loc, SHARED_CONST SHARED_int3_type gridDims)
{
    if (loc.x < 0 || loc.y < 0 || loc.z < 0 || loc.x >= gridDims.x || loc.y >= gridDims.y || loc.z >= gridDims.z)
        return -1;

    return loc.x + loc.y * gridDims.x + loc.z * gridDims.x * gridDims.y;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\OpenRLCPPSharedIncludes.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\ProbeBakeFrameShader.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniform sampler2D PositionsTex;
uniform sampler2D ProbeLightIndicesTexture;
uniform int OutputRayCount;
uniform int PassIdx;
uniform int GIMaxSamples;
uniform int GISamplesPerPass;
uniform int GISamplesSoFar;
uniform int DirectMaxSamples;
uniform int DirectSamplesPerPass;
uniform int DirectSamplesSoFar;
uniform int EnvironmentMaxSamples;
uniform int EnvironmentSamplesPerPass;
uniform int EnvironmentSamplesSoFar;
uniform int IgnoreDirectEnvironment;
uniform int DoDirect;

void setup()
{
    rl_OutputRayCount = OutputRayCount;
}

void ProbeSampling(vec3 pos, int samplesForPass, int samplesSoFar, int scramble)
{
    int sampleIndex = samplesSoFar;
    float weight = 4.0/float(GIMaxSamples);

    vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);

    for(int i = 0; i < samplesForPass; ++i, ++sampleIndex)
    {
        vec2 rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift.xy);
        vec3 direction = SphereSample(rnd);

        createRay();
        rl_OutRay.origin           = pos;
        rl_OutRay.direction        = direction;
        rl_OutRay.color            = vec4(0.0); // unused, because we're not shooting against lights
        rl_OutRay.probeDir         = normalize(direction);
        rl_OutRay.defaultPrimitive = GetEnvPrimitive();
        rl_OutRay.renderTarget     = PROBE_BUFFER;
        rl_OutRay.isOutgoing       = true;
        rl_OutRay.sampleIndex      = sampleIndex;
        rl_OutRay.rayType          = PVR_RAY_TYPE_GI;
        rl_OutRay.rayClass         = PVR_RAY_CLASS_GI;
        rl_OutRay.depth            = 0;
        rl_OutRay.weight           = weight;
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.albedo           = vec3(1.0);
        rl_OutRay.sameOriginCount  = 0;
        rl_OutRay.transmissionDepth= 0;
        rl_OutRay.lightmapMode     = LIGHTMAPMODE_NONDIRECTIONAL; // Not used with probe sampling.
        rl_OutRay.lodParam         = 0;
        rl_OutRay.lodOrigin        = rl_OutRay.origin;
        rl_OutRay.originalT        = rl_OutRay.maxT;
        emitRayWithoutDifferentials();
    }
}

void EnvironmentSampling(vec3 pos, int samplesForPass, int samplesSoFar)
{
    int   sampleIndex = samplesSoFar;
    float weight = 1.0/float(EnvironmentMaxSamples);

    vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);

    if (UseEnvironmentImportanceSampling())
    {
        for (int i = 0; i < samplesForPass; ++i, ++sampleIndex)
        {
            vec3 rand = fract(vec3(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0), SobolSample(sampleIndex, 2, 0)) + cpShift);
            VolumeSampleEnvironmentIS(PROBE_BUFFER, pos, vec3(0.0), vec3(1.0), rand, weight, 0, 0, false);
        }
    }
    else
    {
        for (int i = 0; i < samplesForPass; ++i, ++sampleIndex)
        {
            vec2 rand = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift.xy);
            VolumeSampleEnvironment(PROBE_BUFFER, pos, vec3(0.0), vec3(1.0), rand, weight, 0, 0, false);
        }
    }
}

void main()
{
    vec2  frameCoord  = rl_FrameCoord.xy / rl_FrameSize.xy;

    vec4 posTex = texture2D(PositionsTex, frameCoord);

    if(posTex.w <= 0.0)
        return;

    int scramble = GetScreenCoordHash(rl_FrameCoord.xy);

    if (DoDirect == 0)
    {
        if (GISamplesSoFar < GIMaxSamples)
        {
            int clampedGIsamplesPerPass = min (max(0, GIMaxSamples - GISamplesSoFar), GISamplesPerPass);
            ProbeSampling(posTex.xyz, clampedGIsamplesPerPass, GISamplesSoFar, scramble);
        }

        if (IgnoreDirectEnvironment == 0 && EnvironmentSamplesSoFar < EnvironmentMaxSamples && SampleDirectEnvironment())
        {
            int clampedEnvSamplesPerPass = min(max(0, EnvironmentMaxSamples - EnvironmentSamplesSoFar), EnvironmentSamplesPerPass);
            EnvironmentSampling(posTex.xyz, clampedEnvSamplesPerPass, EnvironmentSamplesSoFar);
        }
    }
    else
    {
        // Direct and probe occlusion are done in the first interation.
        if (DirectSamplesSoFar < DirectMaxSamples)
        {
            vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
            int clampedDirectsamplesPerPass = min (max(0, DirectMaxSamples - DirectSamplesSoFar), DirectSamplesPerPass);

            int sampleIndex = DirectSamplesSoFar;
            for (int i = 0; i < DirectSamplesPerPass; ++i, ++sampleIndex)
            {
                float weight = 1.0/float(DirectMaxSamples);
                vec2 rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift.xy);

                // Direct
                DoShadows(false, posTex.xyz, vec3(0.0), vec3(1.0), PROBE_BUFFER, rnd.xyy, vec3(0.0), LIGHTMAPMODE_NOTUSED, 0, PVR_FLT_MAX, 0, OCCLUSIONMODE_DIRECT, vec4(-1.0), weight, true);

                // Probe Occlusion
                vec4 lightIndices = texture2D(ProbeLightIndicesTexture, frameCoord);
                DoShadows(false, posTex.xyz, vec3(0.0), vec3(1.0), PROBE_OCCLUSION_BUFFER, rnd.xyy, vec3(0.0), LIGHTMAPMODE_NOTUSED, 0, PVR_FLT_MAX, 0, OCCLUSIONMODE_PROBEOCCLUSION, lightIndices, weight, true);
            }
        }
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\ProbeBakeFrameShader.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\RayAttributes.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

#define KPI                 3.14159265358979323846264338327950
#define KTWOPI              6.28318530717958646
#define KHALFPI             1.570796326794897
#define KQUARTERPI          0.7853981633974483


#define MAX_ANGULAR_FALLOFF_TABLE_LENGTH 128 // Needs to be kept in sync with PVRJobLoadShaders.cpp::gMaxAngularFalloffTableLength

// Keep in sync with wmLight.h
#define LIGHT_SPOT        0
#define LIGHT_DIRECTIONAL 1
#define LIGHT_POINT       2
#define LIGHT_AREA        3
#define LIGHT_DISC        4
#define LIGHT_PYRAMID     5
#define LIGHT_BOX         6

#define NO_SHADOW 0.0
#define HARD_SHADOW 1.0
#define SOFT_SHADOW 2.0


// FBO attachment index
// (can overlap between lightmaps and light probes)
// Lightmaps
// Same as unique buffer names above
// Light probes
#define PROBE_BUFFER_INDEX 0                // accumulateSH uses buffers [0;SHNUMCOEFFICIENTS-1]
#define PROBE_OCCLUSION_BUFFER_INDEX 9
// Custom bake
#define CUSTOM_BAKE_BUFFER_INDEX 0

int GetFBOAttachmentIndex(int target)
{
    if (target == PROBE_BUFFER)
        return PROBE_BUFFER_INDEX;
    else if (target == PROBE_OCCLUSION_BUFFER)
        return PROBE_OCCLUSION_BUFFER_INDEX;
    else if (target == CUSTOM_BAKE_BUFFER)
        return CUSTOM_BAKE_BUFFER_INDEX;

    // target < PROBE_BUFFER
    return target;
}

#define OutputType int
#define OUTPUTTYPE_LIGHTMAP     0
#define OUTPUTTYPE_LIGHTPROBES  1

OutputType GetOutputType(int target)
{
    if (target < PROBE_BUFFER)
        return OUTPUTTYPE_LIGHTMAP;
    else
        return OUTPUTTYPE_LIGHTPROBES;
}

#define SOBOL_MATRIX_SIZE 53248 // needs to be kept in sync with "External/qmc/SobolData.h [1024*52]"


#define LightmapMode int
#define LIGHTMAPMODE_NOTUSED        -1
#define LIGHTMAPMODE_NONDIRECTIONAL 0
#define LIGHTMAPMODE_DIRECTIONAL    1


rayattribute vec4 color;
rayattribute int renderTarget;
rayattribute float weight;
rayattribute int sampleIndex;
rayattribute vec3 probeDir;         // Used both for directionality and light probes.
rayattribute vec3 albedo;
rayattribute int sameOriginCount;
rayattribute LightmapMode lightmapMode;
rayattribute int transmissionDepth;
rayattribute int lodParam;
rayattribute int rayType;
rayattribute vec3 lodOrigin;
rayattribute float originalT;


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\RayAttributes.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Shadows.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

float sqr(float x) { return x*x; }

void EmitShadowRay(vec3, vec3, vec3, vec4, float, float, LightmapMode, int);

vec3 CalculateJitteredLightVec(int lightIdx, vec3 lightVec, vec3 position, float maxT, vec2 rnd)
{
    int lightType = GetLightType(lightIdx);

    if (lightType == LIGHT_AREA || lightType == LIGHT_DISC)
        return lightVec;

    float lightDist = maxT;
    if(lightDist == 1e27)
        lightDist = length(GetLightPosition(lightIdx) - position);

    if(lightType == LIGHT_DIRECTIONAL)
        lightDist = 1.0;

    vec3 lightOffset = lightVec * lightDist;

    float shadowRadius = GetLightProperties1(lightIdx).x;

    vec3 b1;
    vec3 b2;

    CreateOrthoNormalBasis(lightVec, b1, b2);
    mat3 lightBasis = mat3(b1.x, b1.y, b1.z, b2.x, b2.y, b2.z, lightVec.x, lightVec.y, lightVec.z);

    return GetJitteredLightVec(shadowRadius, rnd, lightOffset, lightBasis);
}

bool DoShadowsForLight(int lightIdx, vec3 position, vec3 normal, vec3 diffuse, int target, vec2 rnd, vec3 probeDir, LightmapMode lightmapMode, int lodParam, float lodT, bool bounce, OcclusionMode occlusionMode, vec4 lightIndices, float weight, bool receiveShadows)
{
    if (occlusionMode == OCCLUSIONMODE_DIRECT)
    {
        if (GetOutputType(target) == OUTPUTTYPE_LIGHTPROBES)
        {
            if (!GetLightProbesDoDirect(lightIdx))
                return false;
        }
        else // OUTPUTTYPE_LIGHTMAP
        {
            if (!GetLightmapsDoDirect(lightIdx))
                return false;
        }
    }

    int shadowMaskChannel;
    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        if (shadowMaskChannel < 0 || shadowMaskChannel > 3)
            return false;
    }

    int probeOcclusionChannel = -1;
    if (occlusionMode == OCCLUSIONMODE_PROBEOCCLUSION)
    {
        // Check if this probe wants to calculate occlusion for this light.
        if (int(lightIndices.x) == lightIdx)
            probeOcclusionChannel = 0;
        else if (int(lightIndices.y) == lightIdx)
            probeOcclusionChannel = 1;
        else if (int(lightIndices.z) == lightIdx)
            probeOcclusionChannel = 2;
        else if (int(lightIndices.w) == lightIdx)
            probeOcclusionChannel = 3;

        if (probeOcclusionChannel == -1)
            return false;
    }

    vec3 lightColor;
    vec3 lightVec;
    float maxT;
    float shadowType = GetShadowType(lightIdx);
    bool hasHardShadows = shadowType == HARD_SHADOW;

    if (!CalculateLightColor(lightIdx, normal, position, bounce, rnd, lightColor, lightVec, maxT, occlusionMode))
        return false;

    vec4 lambdiffuse = vec4(lightColor.rgb * diffuse, 0.0);

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        lambdiffuse = vec4(0.0, 0.0, 0.0, 0.0);
        lambdiffuse[shadowMaskChannel] = 1.0;
    }

    if (occlusionMode == OCCLUSIONMODE_PROBEOCCLUSION)
    {
        lambdiffuse = vec4(0.0, 0.0, 0.0, 0.0);
        lambdiffuse[probeOcclusionChannel] = 1.0;
    }

    bool hasProbeDir = probeDir != vec3(0.0);

    if (shadowType == NO_SHADOW || !receiveShadows || maxT < PVR_FLT_EPSILON)
    {
        vec3 probeDirOut = probeDir;
        if (!hasProbeDir)
            probeDirOut = lightVec;

        Accumulate(target, lambdiffuse * weight, probeDirOut, lightmapMode);
    }
    else
    {
        lightVec = CalculateJitteredLightVec(lightIdx, lightVec, position, maxT, rnd);

        vec3 probeDirOut = probeDir;
        if (!hasProbeDir)
            probeDirOut = lightVec;

        EmitShadowRay(position, lightVec, probeDirOut, lambdiffuse, maxT, weight, lightmapMode, lodParam, lodT, target);
    }

    return true;
}

bool DoShadowsForRegion(bool usePowerSampling, int regionIdx, vec3 position, vec3 normal, vec3 diffuse, int target, vec3 rnd, vec3 probeDir, LightmapMode lightmapMode, int lodParam, float lodT, int bounce, OcclusionMode occlusionMode, vec4 lightIndices, float weight, bool receiveShadows)
{
    bool didLight = false;
    // always test directional lights
    for (int i = 0, cnt = GetMaxDirLights(); i < cnt; ++i)
    {
        if (DoShadowsForLight(i, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, weight, receiveShadows))
            didLight = true;
    }

    if (regionIdx >= Lights.GridLength || regionIdx < 0)
        return didLight;

    int numLights = GetNumLights(regionIdx);

    if(numLights <= 0)
        return didLight;


    int  maxShadowRays = max(PVR_MAX_SHADOW_RAYS - bounce + 1, 1);

    if (numLights > maxShadowRays && usePowerSampling)
    {
        float origWeight = weight / float(maxShadowRays);
        float xi = rnd.z;
        for (int i = 0; i < maxShadowRays; ++i)
        {
            float rayWeight = origWeight;
            int lightIdx = PickLight(regionIdx, xi, rayWeight);
            if (lightIdx >= 0)
            {
                if (DoShadowsForLight(lightIdx, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, rayWeight, receiveShadows))
                    didLight = true;
            }
        }
    }
    else
    {
        for (int i = 0; i < numLights; ++i)
        {
            int lightIdx = GetLightIndex(regionIdx, i);
            if (DoShadowsForLight(lightIdx, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, weight, receiveShadows))
                didLight = true;
        }
    }

    return didLight;
}

bool DoShadows(bool usePowerSampling, vec3 surfPosition, vec3 surfNormal, vec3 surfDiffuseColor, int target, vec3 rnd, vec3 probeDir, LightmapMode lightmapMode, int lodParam, float lodT, int bounce, OcclusionMode occlusionMode, vec4 lightIndices, float weight, bool receiveShadows)
{
    bool didLight = DoShadowsForRegion(usePowerSampling, GetRegionContaining(surfPosition), surfPosition, surfNormal, surfDiffuseColor, target, rnd, probeDir, lightmapMode, lodParam, lodT, bounce, occlusionMode, lightIndices, weight, receiveShadows);
    return didLight;
}

int DoShadows(int startIndex, int count, vec3 position, vec3 normal, vec3 diffuse, int target, vec3 rnd, vec3 probeDir, LightmapMode lightmapMode, int lodParam, float lodT, int bounce, OcclusionMode occlusionMode, vec4 lightIndices, float weight, bool receiveShadows)
{
    bool didLight;
    int maxDirLights = GetMaxDirLights();
    int remainingDir = min(max( 0, maxDirLights - startIndex), count);
    for( int cnt = startIndex + remainingDir; startIndex < cnt; startIndex++, count-- )
    {
        if (DoShadowsForLight(startIndex, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, weight, receiveShadows))
            didLight = true;
    }
    startIndex -= maxDirLights;

    // any directional lights left?
    if (startIndex < 0)
        return 1;

    // any non-directional lights?
    int regionIdx = GetRegionContaining(position);
    if (regionIdx >= Lights.GridLength || regionIdx < 0)
        return 0;

    int numLights = GetNumLights(regionIdx);
    if (startIndex >= numLights)
        return 0;


    int remaining = min(numLights - startIndex, count);
    for( int cnt = startIndex + remaining; startIndex < cnt; startIndex++ )
    {
        int lightIdx = GetLightIndex(regionIdx, startIndex);
        if (DoShadowsForLight(lightIdx, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, weight, receiveShadows))
            didLight = true;
    }

    return numLights - startIndex;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Shadows.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\ShadowSampling.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

vec3 GetJitteredLightVec(float shadowRadius, vec2 rnd, vec3 lightOffset, mat3 lightBasis)
{
    vec2 diskSample = MapSquareToDisk (rnd);
    vec3 jitterOffset = vec3(shadowRadius * diskSample, 0.0);
    jitterOffset =  lightBasis * jitterOffset;;
    vec3 jitteredLightOffset = lightOffset + jitterOffset;
    return normalize(jitteredLightOffset);
}

void EmitShadowRay(vec3 origin, vec3 direction, vec3 probeDir, vec4 diffuse, float maxT, float weight, LightmapMode lightmapMode, int lodParam, float lodT, int target)
{
    createRay();
    rl_OutRay.origin              = origin;
    rl_OutRay.direction           = direction;
    rl_OutRay.color               = diffuse;
    rl_OutRay.probeDir            = probeDir;
    rl_OutRay.renderTarget        = target;
    rl_OutRay.isOutgoing          = true;       // ?
    rl_OutRay.sampleIndex         = 0;          // dummy, only used in the Standard.rlsl to decide on the next direction
    rl_OutRay.depth               = 0;
    rl_OutRay.weight              = weight;
    rl_OutRay.albedo              = vec3(1.0);
    rl_OutRay.maxT                = maxT;
    rl_OutRay.sameOriginCount     = 0;
    rl_OutRay.transmissionDepth   = 0;
    rl_OutRay.lightmapMode        = lightmapMode;
    rl_OutRay.lodParam            = lodParam;
    rl_OutRay.rayType             = PVR_RAY_TYPE_SHADOW;
    rl_OutRay.rayClass            = MapLodParamToRayClass(PVR_RAY_TYPE_SHADOW, lodParam);
    rl_OutRay.lodOrigin           = rl_OutRay.origin;
    rl_OutRay.originalT           = maxT;
    if (lodParam == 0)
    {
        rl_OutRay.occlusionTest    = true;
        rl_OutRay.defaultPrimitive = GetLightPrimitive();
    }
    else
    {
        rl_OutRay.maxT             = min(lodT, maxT);
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.defaultPrimitive = GetLodMissPrimitive();
    }
    emitRayWithoutDifferentials();
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\ShadowSampling.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Standard.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniform sampler2D  Albedo;
uniform sampler2D  Emissive;
uniform sampler2D  Transmission;
uniform vec4       ST;
uniform vec4       TransmissionST;
uniform int        LightmapIndex;
uniform int        IsTransmissive;
uniform int        IsNegativelyScaled;
uniform int        IsDoubleSided;
uniform int        IsShadowCaster;
uniform int        LodParam;

#define MIN_INTERSECTION_DISTANCE 0.001
#define MIN_PUSHOFF_DISTANCE 0.0001 // Keep in sync with PLM_MIN_PUSHOFF

uniformblock PushOffInfo
{
    float pushOff;
};

void setup()
{
    // The output ray count for a given ray class is how many rays can be emitted by the shader when invoked by the ray class (i.e. the rl_InRay.rayClass)
    rl_OutputRayCount[PVR_RAY_CLASS_GI   ] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect(); // worst case: directional light shadow rays + 4 shadow rays + 1 GI ray + env rays
    rl_OutputRayCount[PVR_RAY_CLASS_OCC  ] = 1; // May potentially emit a ray due to transmission
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_1] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_2] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_3] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_4] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_5] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_6] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
}

vec2 STTransform(vec2 uv)
{
    return (uv * ST.xy) + ST.zw;
}

vec2 TransmissionSTTransform(vec2 uv)
{
    return (uv * TransmissionST.xy) + TransmissionST.zw;
}

vec3 NextBounceDirection(vec2 rnd, vec3 normal)
{
    // next bounce
    vec3 hamDir = HemisphereCosineSample(rnd);

    vec3 b1;
    vec3 b2;
    CreateOrthoNormalBasis(normal, b1, b2);

    hamDir = hamDir.x*b1 + hamDir.y*b2 + hamDir.z*normal;

    return hamDir;
}

// This function should be used for ray generation where the depth attribute is not increased when generating a new ray
float GetAdjustedPushOff()
{
    return pow(2.0, float(rl_InRay.sameOriginCount)) * max(PushOffInfo.pushOff, MIN_PUSHOFF_DISTANCE);
}

void continueRay()
{
    // push off the ray and continue the path
    vec3  origin = vec3(rl_IntersectionPoint + (rl_InRay.direction * GetAdjustedPushOff()));
    float maxT   = max(0.0, rl_InRay.maxT - length(origin - rl_InRay.origin));
    // continue ray skipping this intersection
    createRay();
    rl_OutRay.origin = origin;
    rl_OutRay.maxT   = maxT;
    rl_OutRay.depth  = rl_InRay.depth;
    rl_OutRay.sameOriginCount = (rl_IntersectionT < MIN_INTERSECTION_DISTANCE) ? (rl_InRay.sameOriginCount + 1) : rl_InRay.sameOriginCount;
    emitRayWithoutDifferentials();
}

void main()
{
    int   lodParam        = rl_InRay.lodParam;
    bool  occlusionTest   = rl_InRay.rayType != PVR_RAY_TYPE_GI;
    int   sameOriginCount = rl_InRay.sameOriginCount;
    float maxT            = rl_InRay.maxT;

    // this ray was fired from a lod
    if (rl_InRay.lodParam != 0)
    {
        int rayGroupId, rayLodMask, instGroupId, instLodMask;
        bool shadowCaster = IsShadowCaster > 0;
        UnpackLodParam(rl_InRay.lodParam, rayGroupId, rayLodMask);
        UnpackLodParam(LodParam, instGroupId, instLodMask);

        bool occlusionRay  = rl_InRay.rayClass == PVR_RAY_CLASS_OCC;
        bool hitLod0       = (instLodMask & PVR_LOD_0_BIT) != 0;
        bool hitSameLod    = (instLodMask & rayLodMask) != 0;

        if (rayGroupId != instGroupId) // we hit a different lod group
        {
            if (!hitLod0) // we hit a higher lod of a different object, continue the ray
            {
                continueRay();
                return;
            }
            else if (occlusionTest && !occlusionRay && !shadowCaster)
            {
                // for lod rays we cannot terminate the path, as the object may not be a shadow caster. Continue the ray.
                continueRay();
                return;
            }

            lodParam = 0; // from now on treat this as a lod0 path
            sameOriginCount = 0;
            maxT = PVR_FLT_MAX;
        }
        else if(!hitSameLod ||                   // we hit an instance of our own lodgroup which is in a different lod layer, i.e. ray shot from lod1 hits lod0 from same group
               (occlusionTest && !shadowCaster)) // or this was an occlusion test, we hit an object in the same lod group but it's not flagged as a shadow caster
        {
            continueRay();
            return;
        }
        // self occlusion. As we bounce a lod ray again, we still need to use the max lod distance
        maxT = length(rl_InRay.origin - rl_InRay.lodOrigin) + rl_InRay.maxT;
    }

    // This is a workaround to avoid transparent hits getting stuck due to pushoff not working in very large scenes.
    if (rl_InRay.transmissionDepth > 100)
    {
        return;
    }

    // We draw a random number here for use in the sampling of translucency and we may reuse this random number
    // later we a diffuse interaction happens and the shader continues. Appropriate rescaling of the random
    // number is applied in this case.

    // Make sure we don't correlate due to reuse of dimensions, keep in sync with dimension picking below
    //matches GPU light mapper where increase the dimension each time we encounter a transmissive and we get the ray passing through it
    int   dim0 = rl_InRay.transmissionDepth + (rl_InRay.depth + 1) * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;

    float cpShift_transmission = GetCranleyPattersonRotation1D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), dim0);

    float rnd0 = fract(SobolSample(rl_InRay.sampleIndex, dim0, 0) + cpShift_transmission);

    // If a ray is intersecting a transmissive object (either from inside or outside)
    if (IsTransmissive > 0)
    {
        vec2 transmissionUV = TransmissionSTTransform(TexCoord0Varying.xy);
        vec4 transmission = texture2D(Transmission, transmissionUV);

        // NOTE: This is wrong! The probability of either reflecting or refracting a ray
        // should depend on the Fresnel of the material. However, since we do not support
        // any specularity in PVR there is currently no way to query this value, so for now
        // we use the transmission (texture) albedo.
        float probability = (transmission.x + transmission.y + transmission.z) / 3.0;
        if (probability > 0.0 && (rnd0 <= probability || occlusionTest))
        {
            createRay();
            rl_OutRay.direction = rl_InRay.direction;
            rl_OutRay.origin = vec3(rl_IntersectionPoint + (rl_InRay.direction * GetAdjustedPushOff()));

            rl_OutRay.color = rl_InRay.color;
            rl_OutRay.albedo = rl_InRay.albedo;

            if (occlusionTest)
                rl_OutRay.color *= vec4(transmission.xyz, 1.0);
            else
                rl_OutRay.albedo *= transmission.xyz;

            rl_OutRay.defaultPrimitive = rl_InRay.defaultPrimitive;
            rl_OutRay.depth = rl_InRay.depth;
            rl_OutRay.probeDir = rl_InRay.probeDir;
            rl_OutRay.renderTarget = rl_InRay.renderTarget;
            rl_OutRay.isOutgoing = rl_InRay.isOutgoing;
            rl_OutRay.sampleIndex = rl_InRay.sampleIndex;
            rl_OutRay.weight = rl_InRay.weight;
            rl_OutRay.maxT = max(0.0, rl_InRay.maxT - length(rl_OutRay.origin - rl_InRay.origin));
            rl_OutRay.sameOriginCount = (rl_IntersectionT < MIN_INTERSECTION_DISTANCE) ? sameOriginCount + 1 : sameOriginCount;
            rl_OutRay.transmissionDepth = rl_InRay.transmissionDepth + 1;
            rl_OutRay.lightmapMode = rl_InRay.lightmapMode;
            rl_OutRay.lodParam = lodParam;
            rl_OutRay.lodOrigin = rl_InRay.lodOrigin;
            rl_OutRay.originalT = rl_InRay.originalT;
            rl_OutRay.rayType = rl_InRay.rayType;
            if (lodParam == 0)
            {
                rl_OutRay.rayClass      = (rl_InRay.rayType == PVR_RAY_TYPE_GI || rl_InRay.rayType == PVR_RAY_TYPE_ENV) ? PVR_RAY_CLASS_GI : PVR_RAY_CLASS_OCC;
                rl_OutRay.occlusionTest = occlusionTest;
            }
            else
            {
                rl_OutRay.rayClass      = rl_InRay.rayClass;
                rl_OutRay.occlusionTest = rl_InRay.occlusionTest;
            }
            emitRayWithoutDifferentials();
            return;
        }
        // Rescale rnd to 0-1 range for reuse below for NEE (Realistic Ray Tracing by Peter Shirley).
        // Here we are guaranteed that rnd0 > probability and we want to rescale the rnd0 to fit into
        // the [0;1] range by "stretching" the interval [probability;1] and adjusting rnd0 accordingly.
        rnd0 = (rnd0 - probability) / (1.0 - probability);
    }

    // Shadow rays should not proceed beyond this point. Note that this shader is executed on intersections between occlusion rays and transmissive objects (RL_PRIMITIVE_IS_OCCLUDER set to false).
    if (occlusionTest)
        return;

    if(rl_IntersectionT > AOInfo.aoMaxDistance && rl_InRay.depth == 0 && (rl_InRay.renderTarget != PROBE_BUFFER))
        accumulate(AO_BUFFER, vec3(1.0,1.0,1.0));

    // check hit validity
    bool negativelyScaled = (IsNegativelyScaled > 0);
    bool doubleSided = (IsDoubleSided > 0);
    bool frontFacing = (negativelyScaled ? !rl_FrontFacing : rl_FrontFacing);

    if (!(frontFacing || doubleSided) && rl_InRay.depth == 0)
    {
        if (rl_InRay.renderTarget == CUSTOM_BAKE_BUFFER)
        {
            accumulate(vec4(0.0,0.0,0.0,1.0));
        }
        else if (rl_InRay.renderTarget != PROBE_BUFFER && rl_InRay.transmissionDepth == 0)
        {
            accumulate(VALIDITY_BUFFER, float(1.0));
            // accumulate -1 to sample buffer to discount this sample?
        }
        else if (rl_InRay.renderTarget == PROBE_BUFFER && rl_InRay.transmissionDepth == 0)
        {
            accumulate(PROBE_VALIDITY_BUFFER, float(1.0));
        }
    }

    // A custom bake should never proceed beyond this point, once a potential backface has been recorded we are done.
    if (rl_InRay.renderTarget == CUSTOM_BAKE_BUFFER)
        return;

    if((frontFacing || doubleSided) && rl_IsHit && IntegratorSamples.maxBounces > 0)
    {
        vec2 albedoUV = TexCoord1Varying.xy;

        // When intersecting backface we invert rl_GeometricNormal since this is the normal of the front face
        vec3 geometricNormal = (negativelyScaled ? -rl_GeometricNormal : rl_GeometricNormal); // account for negative scaling
        geometricNormal = (frontFacing ? geometricNormal : -geometricNormal); // account for backface intersection
        vec3 varyingNormal = (frontFacing ? NormalVarying : -NormalVarying); // account for backface intersection;

        vec3 intersectionPushedOff = vec3(rl_IntersectionPoint + (geometricNormal * PushOffInfo.pushOff));

        vec4 albedo = texture2D(Albedo, albedoUV);
        vec3 pathThroughput = albedo.xyz * rl_InRay.albedo.xyz;

        int base_dimension = (rl_InRay.depth+1)*PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;

        vec3 rnd;

#if PLM_USE_BLUE_NOISE_SAMPLING
            if(rl_InRay.sampleIndex < PLM_BLUE_NOISE_MAX_SAMPLES && rl_InRay.renderTarget == GI_BUFFER)
            {

            rnd = vec3( BlueNoiseSobolSample(rl_InRay.sampleIndex, base_dimension+0, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset),
                        BlueNoiseSobolSample(rl_InRay.sampleIndex, base_dimension+1, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset),
                        BlueNoiseSobolSample(rl_InRay.sampleIndex, base_dimension+2, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset) );
            }
            else
#endif
            {
                vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), base_dimension);
                rnd = fract(vec3(SobolSample(rl_InRay.sampleIndex, base_dimension, 0), SobolSample(rl_InRay.sampleIndex, base_dimension+1, 0), SobolSample(rl_InRay.sampleIndex, base_dimension+2, 0)) + cpShift);
            }


        // passing in false here reverts to sampling all lights in the lightgrid cell, but then rl_OutputRayCount needs to be adjusted accordingly
        DoShadows(true, intersectionPushedOff, varyingNormal, pathThroughput, rl_InRay.renderTarget, rnd, rl_InRay.probeDir, rl_InRay.lightmapMode, lodParam, maxT, rl_InRay.depth+1, OCCLUSIONMODE_DIRECT_ON_BOUNCE, vec4(-1.0), rl_InRay.weight, true);


        // add emissive
        vec4 emissive = texture2D(Emissive, albedoUV);
        Accumulate(rl_InRay.renderTarget, rl_InRay.weight * emissive * vec4(rl_InRay.albedo, 1.0), rl_InRay.probeDir, rl_InRay.lightmapMode);

        // Env importance sampling
        // The depth check prevents env light contribution at the last bounce of the path, preserving previous behavior
        if (SampleIndirectEnvironment() && (rl_InRay.depth + 1) < IntegratorSamples.maxBounces)
        {
            if (UseEnvironmentImportanceSampling())
                SurfaceSampleEnvironmentIS(rl_InRay.renderTarget, intersectionPushedOff, rl_InRay.probeDir, varyingNormal, geometricNormal, pathThroughput, rnd, rl_InRay.weight, rl_InRay.depth, rl_InRay.transmissionDepth + 1, rl_InRay.lightmapMode, lodParam, maxT, true);
            else
                SurfaceSampleEnvironment(rl_InRay.renderTarget, intersectionPushedOff, rl_InRay.probeDir, varyingNormal, geometricNormal, pathThroughput, rnd.xy, rl_InRay.weight, rl_InRay.depth, rl_InRay.transmissionDepth + 1, rl_InRay.lightmapMode, lodParam, maxT, true);
        }

        //Russian roulette
        bool russianRouletteContinuePath = true;
        if(rl_InRay.depth + 1 >= IntegratorSamples.minBounces)
        {
            float p = max(max(pathThroughput.x, pathThroughput.y), pathThroughput.z);
            if (p < rnd.z)
                russianRouletteContinuePath = false;

            pathThroughput = (1.0/p) * pathThroughput;
        }

        if(russianRouletteContinuePath && (rl_InRay.depth + 1) < IntegratorSamples.maxBounces)
        {
            // next bounce
            createRay();
            rl_OutRay.origin           = intersectionPushedOff;
            rl_OutRay.direction        = NextBounceDirection(rnd.xy, geometricNormal);
            rl_OutRay.color            = vec4(0.0); // unused, because we're not shooting against lights
            rl_OutRay.probeDir         = rl_InRay.probeDir;
            rl_OutRay.defaultPrimitive = rl_InRay.defaultPrimitive;
            rl_OutRay.renderTarget     = rl_InRay.renderTarget;
            rl_OutRay.isOutgoing       = true;
            rl_OutRay.sampleIndex      = rl_InRay.sampleIndex;
            rl_OutRay.rayClass         = rl_InRay.rayClass;
            rl_OutRay.depth            = rl_InRay.depth+1;
            rl_OutRay.weight           = rl_InRay.weight;
            rl_OutRay.occlusionTest    = false;
            rl_OutRay.albedo           = pathThroughput;
            rl_OutRay.sameOriginCount  = 0;
            rl_OutRay.transmissionDepth= 0;
            rl_OutRay.lightmapMode     = rl_InRay.lightmapMode;
            rl_OutRay.lodParam         = lodParam;
            rl_OutRay.maxT             = maxT;
            rl_OutRay.rayType          = rl_InRay.rayType;
            emitRayWithoutDifferentials();
        }
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Standard.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\TechniqueCommon.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniformblock TechniqueProperties
{
    int GridSize;
    int RaysPerPixel;
    int GridDim;
};

void CreateOrthoNormalBasis(vec3 n, out vec3 tangent, out vec3 bitangent)
{
    float sign = n.z >= 0.0 ? 1.0 : -1.0;
    float a    = -1.0 / (sign + n.z);
    float b    = n.x * n.y * a;

    tangent    = vec3(1.0 + sign * n.x * n.x * a, sign * b, -sign * n.x);
    bitangent  = vec3(b, sign + n.y * n.y * a, -n.y);
}

void pixarONB(vec3 n, inout vec3 tangent, inout vec3 bitangent)
{
    float sign = n.z >= 0.0 ? 1.0 : -1.0;
    float a = -1.0 / (sign + n.z);
    float b = n.x * n.y * a;

    tangent = vec3(1.0 + sign * n.x * n.x * a, sign * b, -sign * n.x);
    bitangent = vec3(b, sign + n.y * n.y * a, -n.y);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\TechniqueCommon.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Varyings.rlsl---------------


/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

varying vec3 NormalVarying;
varying vec2 TexCoord0Varying;
varying vec2 TexCoord1Varying;


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\RLSL\Shaders\Varyings.rlsl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Accumulate.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

#define SHNUMCOEFFICIENTS 9

// 1 / (2*sqrt(kPI))
#define K1DIV2SQRTPI        0.28209479177387814347403972578039
// sqrt(3) / (2*sqrt(kPI))
#define KSQRT3DIV2SQRTPI    0.48860251190291992158638462283835
// sqrt(15) / (2*sqrt(kPI))
#define KSQRT15DIV2SQRTPI   1.0925484305920790705433857058027
// 3 * sqrtf(5) / (4*sqrt(kPI))
#define K3SQRT5DIV4SQRTPI   0.94617469575756001809268107088713
// sqrt(15) / (4*sqrt(kPI))
#define KSQRT15DIV4SQRTPI   0.54627421529603953527169285290135
// sqrtf(5) / (4*sqrt(kPI)) (the constant term in the Y_2,0 basis function of the standard real SH basis)
#define SQRT5DIV4SQRTPI     0.315391565252520050

void accumulateSH(int target, vec4 col, vec3 dir)
{
    float outsh[SHNUMCOEFFICIENTS];
    outsh[0] = K1DIV2SQRTPI;
    outsh[1] = dir.x * KSQRT3DIV2SQRTPI;
    outsh[2] = dir.y * KSQRT3DIV2SQRTPI;
    outsh[3] = dir.z * KSQRT3DIV2SQRTPI;
    outsh[4] = dir.x * dir.y * KSQRT15DIV2SQRTPI;
    outsh[5] = dir.y * dir.z * KSQRT15DIV2SQRTPI;
    outsh[6] = (dir.z * dir.z * K3SQRT5DIV4SQRTPI) - SQRT5DIV4SQRTPI;
    outsh[7] = dir.x * dir.z * KSQRT15DIV2SQRTPI;
    outsh[8] = (dir.x * dir.x - dir.y * dir.y) * KSQRT15DIV4SQRTPI;

    for (int c = GetFBOAttachmentIndex(target); c < SHNUMCOEFFICIENTS; c++)
        accumulate(c, vec4(col.xyz * outsh[c] * KPI, 0.0));
}

// Calculate luminance like Unity does it
float unityLinearLuminance(vec3 color)
{
    vec3 lumW = vec3(0.22, 0.707, 0.071);
    return dot(color, lumW);
}

void accumulateDirectional(int target, vec4 color, vec3 dir)
{
    float luminance = unityLinearLuminance(color.xyz);
    vec4 directionality = vec4(dir, 1.0) * luminance;

    if (target == GI_DIRECT_BUFFER)
        accumulate(GetFBOAttachmentIndex(DIRECTIONAL_FROM_DIRECT_BUFFER), directionality);
    else if (target == GI_BUFFER || target == ENV_BUFFER)
        accumulate(GetFBOAttachmentIndex(DIRECTIONAL_FROM_GI_BUFFER), directionality);

    accumulate(GetFBOAttachmentIndex(target), color);
}

// may be better to recompile/load a different shader
void Accumulate(int target, vec4 color, vec3 direction, LightmapMode lightmapMode)
{
    if(target == PROBE_BUFFER)
    {
        accumulateSH(target, color, direction);
    }
    else if(lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
    {
        accumulateDirectional(target, color, direction);
    }
    else
    {
        accumulate(GetFBOAttachmentIndex(target), color);
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Accumulate.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\CustomBakeFrameShader.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniform sampler2D  PositionsTex;
uniform int PassIdx;
uniform int SamplesPerPass;
uniform int SamplesSoFar;
uniform int TotalSampleCount;

void setup()
{
    rl_OutputRayCount = SamplesPerPass;
}

void ProbeSampling(vec3 pos, int rayCount, int totalRayCount, float rayOffset)
{
    int sampleIndex = totalRayCount;
    float weight = 4.0/float(TotalSampleCount);

    vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
    float cpShiftOffset = cpShift.z; // This dimension is used to randomize the position of the probe samples.

    for(int i = 0; i < rayCount; ++i, ++sampleIndex)
    {
        vec2 rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift.xy);
        float randOffset = 0.1 * rayOffset + 0.9 * rayOffset * fract(SobolSample(sampleIndex, 2, 0) + cpShiftOffset);

        vec3 direction = SphereSample(rnd);

        // We don't want the full sphere, we only want the upper hemisphere.
        if (direction.y < 0.0)
            direction = vec3(direction.x, -direction.y, direction.z);

        createRay();
        rl_OutRay.origin           = pos + direction * randOffset;
        rl_OutRay.direction        = direction;
        rl_OutRay.color            = vec4(1.0); // multiplied by transmission in the Standard shader
        rl_OutRay.probeDir         = normalize(direction);
        rl_OutRay.defaultPrimitive = GetEnvPrimitive();
        rl_OutRay.renderTarget     = CUSTOM_BAKE_BUFFER;
        rl_OutRay.isOutgoing       = true;
        rl_OutRay.sampleIndex      = sampleIndex;
        rl_OutRay.rayType          = PVR_RAY_TYPE_GI;
        rl_OutRay.rayClass         = PVR_RAY_CLASS_GI;
        rl_OutRay.depth            = 0;
        rl_OutRay.weight           = weight;
        // Needs to be false, otherwise rl_FrontFacing is never set/never false.
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.albedo           = vec3(1.0);
        rl_OutRay.sameOriginCount  = 0;
        rl_OutRay.lightmapMode     = LIGHTMAPMODE_NONDIRECTIONAL; // Not used with probe sampling.
        rl_OutRay.lodParam         = 0;
        rl_OutRay.lodOrigin        = rl_OutRay.origin;
        rl_OutRay.originalT        = rl_OutRay.maxT;
        emitRayWithoutDifferentials();
    }
}

void main()
{
    vec2  frameCoord  = rl_FrameCoord.xy / rl_FrameSize.xy;

    vec4 posTex = texture2D(PositionsTex, frameCoord);

    // Unused texels
    if(posTex.w < 0.0)
        return;

    ProbeSampling(posTex.xyz, SamplesPerPass, SamplesSoFar, posTex.w);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\CustomBakeFrameShader.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\CustomEnvImplement.rlsl---------------
.
.
void main()
{
    accumulate(vec4(rl_InRay.color.x, rl_InRay.color.y, rl_InRay.color.z, 0.0));
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\CustomEnvImplement.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Debugging.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniformblock DebugProperties
{
    int ActiveDebugPixelX;
    int ActiveDebugPixelY;
    int InteractiveModeEnabled;
};

#define ERROR_CODE   1

#define ENABLE_DEBUGGING 0
#define ENABLE_INTERACTIVE_MODE 1

#if ENABLE_INTERACTIVE_MODE
bool InteractiveModeEnabled()
{
    return DebugProperties.InteractiveModeEnabled!=0;
}
#else
#define InteractiveModeEnabled() (false)
#endif

bool IsDebugRow(float offset)
{
    return abs(rl_FrameCoord.y - (float(DebugProperties.ActiveDebugPixelY)+0.5))<=offset;
}

bool IsDebugColumn(float offset)
{
    return abs(rl_FrameCoord.x - (float(DebugProperties.ActiveDebugPixelX)+0.5))<=offset;
}

bool IsDebugPixel(float offset)
{
    return (IsDebugRow(offset) && IsDebugColumn(offset));
}

bool DrawDebugCrosshair(float offset)
{
#if ENABLE_DEBUGGING
    if(IsDebugPixel(offset))
    {
        accumulate (vec3(1,0,0));
        return true;
    }

    if(IsDebugColumn(0.0))
        Accumulate(vec3(0,1,0));
    if(IsDebugRow(0.0))
        Accumulate(vec3(0,0,1));
#endif
    return false;

}


#if ENABLE_DEBUGGING

#define DEBUG_ALWAYS(_x, _y)     debug(_x,_y)
#define DEBUG(_x, _y)            if(IsDebugPixel(0.0))       debug(_x,_y)
#define CROSSHAIR_DEBUG(_x, _y)  if(DrawDebugCrosshair(0.0)) debug(_x, _y)


#else

#define DEBUG_ALWAYS(_x, _y)
#define DEBUG(_x, _y)
#define CROSSHAIR_DEBUG(_x, _y)

#endif
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Debugging.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\DefaultVertexShader.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

attribute vec3 Vertex;
attribute vec3 Normal;
attribute vec2 TexCoord0;
attribute vec2 TexCoord1;

normalized transformed varying vec3 NormalVarying;
varying vec2 TexCoord0Varying;
varying vec2 TexCoord1Varying;


void main()
{

    rl_Position = vec4(Vertex, 1.0);
    NormalVarying = Normal;
    TexCoord0Varying = TexCoord0;
    TexCoord1Varying = TexCoord1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\DefaultVertexShader.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Defines.rlsl---------------
.
.
/* Shared defines to be included from both rlsl and cpp files */

// Target aka unique buffer name
// (can't overlap between lightmaps and light probes)
#define GI_BUFFER                            0
#define AO_BUFFER                            1
#define ENV_BUFFER                           2
#define GI_DIRECT_BUFFER                     3
#define GI_SAMPLES_BUFFER                    4
#define DIRECT_SAMPLES_BUFFER                5
#define ENV_SAMPLES_BUFFER                   6
#define VALIDITY_BUFFER                      7
#define DIRECTIONAL_FROM_DIRECT_BUFFER       8
#define DIRECTIONAL_FROM_GI_BUFFER           9
#define SHADOW_MASK_BUFFER                  10
#define SHADOW_MASK_SAMPLE_BUFFER           11
#define PROBE_BUFFER                        12
#define PROBE_OCCLUSION_BUFFER              13
#define PROBE_VALIDITY_BUFFER               14
#define CUSTOM_BAKE_BUFFER                  15

// convergence bitmasks
#define PVR_CONVERGED_DIRECT    (1<<0)
#define PVR_CONVERGED_GI        (1<<1)
#define PVR_CONVERGED_ENV       (1<<2)

// limits
#ifndef PVR_MAX_ENVSAMPLES
#define PVR_MAX_ENVSAMPLES      16384
#endif
// a few light related constants
#define PVR_MAX_LIGHTS          32768       // maximum number of lights for the whole scene
#define PVR_MAX_LIGHT_REGIONS   8192        // maximum number of lightgrid cells
#define PVR_MAX_COOKIES         65536       // maximum number of cookie slices
// Reserve 4MB for cdfs. This roughly gives us 512 lights per light region if we got 8K regions, which should be good enough (tm).
// The conservative value obeying PVR_MAX_LIGHTS and PVR_MAX_LIGHT_REGIONS would require about 256MB which seems excessive.
#define PVR_MAX_CDFS            4202496
#define PVR_MAX_SHADOW_RAYS     4           // limit of how many shadow rays are shot per bounce excluding directional light shadows


//Keep in sync with CPPsharedCLincludes.h
#define PLM_MAX_DIR_LIGHTS      8           // limit of how many directional lights are supported
#define PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE 3
#define PLM_MAX_BOUNCE_FOR_CRANLEY_PATTERSON_ROTATION 4
#define PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION (PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE * PLM_MAX_BOUNCE_FOR_CRANLEY_PATTERSON_ROTATION)

//Max size of the golden samples (for cranley patterson rotation) map
//this is a square map with PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION in each texel
#define PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP 512

// needs to be kept in sync with "LightmapBake::InitializeDataAndTextures()" and Defines.rlsl (PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP and PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION)
//equals to 512*512*12
#define MAX_GOLDEN_SAMPLES 3145728

//keep in sync with CPPsharedCLincludes.h
#define PLM_USE_BLUE_NOISE_SAMPLING 1

#define PLM_BLUE_NOISE_TILE_SIZE 128
#define PLM_BLUE_NOISE_MAX_DIMENSIONS 256
#define PLM_BLUE_NOISE_MAX_SAMPLES 256
#define PLM_BLUE_NOISE_MAX_RANKING_DIMENSIONS 8
#define PLM_BLUE_NOISE_MAX_SCRAMBLING_DIMENSIONS 8

// equivalent PLM_BLUE_NOISE_MAX_DIMENSIONS * PLM_BLUE_NOISE_MAX_SAMPLES
#define PLM_BLUE_NOISE_SAMPLING_BUFFER_SIZE 65536

//equivalent to PLM_BLUE_NOISE_MAX_RANKING_DIMENSIONS * PLM_BLUE_NOISE_TILE_SIZE * PLM_BLUE_NOISE_TILE_SIZE * 9 (9 distributions for progressive sampling 1-256 samples)
#define PLM_BLUE_NOISE_RANKING_BUFFER_SIZE 1179648
#define PLM_BLUE_NOISE_SCRAMBLING_BUFFER_SIZE 1179648

#define PLM_MIN_PUSHOFF 0.0001f

// Update Standard shader's setup() when adding more ray classes.
// Warning: When adding a new ray class, make sure it's correctly handled by the RayClassVisibility enum in PVRHelpers.h
// Level of Detail 0
#define PVR_RAY_CLASS_GI        0
#define PVR_RAY_CLASS_OCC       1
// lod occlusion rays
#define PVR_RAY_CLASS_LOD_0     1 // not a typo that this is 1 - it's only a helper, never meant to be set as an actual ray class
#define PVR_RAY_CLASS_LOD_1     2
#define PVR_RAY_CLASS_LOD_2     3
#define PVR_RAY_CLASS_LOD_3     4
#define PVR_RAY_CLASS_LOD_4     5
#define PVR_RAY_CLASS_LOD_5     6
#define PVR_RAY_CLASS_LOD_6     7
// ray visibility only supports up to 8 bits, so this should never be set on a ray
// #define PVR_RAY_CLASS_LOD_7  8

#define PVR_RAY_TYPE_GI         0
#define PVR_RAY_TYPE_SHADOW     1
#define PVR_RAY_TYPE_ENV        2

#define PVR_LOD_0_BIT            1 // 1 << 0
#define PVR_LOD_6_BIT           64 // 1 << 6
#define PVR_LOD_7_BIT          128 // 1 << 7

#define PVR_LODMASK_SHIFT       24 // offset of the lodmask in lparam, we're using the top 8 bits
#define PVR_LODGROUP_MASK       ((1 << PVR_LODMASK_SHIFT)-1) // mask to used to extract the group Id from the lodParam
#define PVR_FLT_MAX             1e37
#define PVR_FLT_EPSILON         1e-19 // Small value that is unlikely to result in a denormalised product.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Defines.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\EnvImplement.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

vec3 DecodeRGBM( vec4 color )
{
    return (8.0 * color.a) * (color.rgb);
}


void main()
{
    if(rl_InRay.depth == 0 && (rl_InRay.renderTarget != PROBE_BUFFER) && rl_InRay.rayType != PVR_RAY_TYPE_ENV)
        accumulate(AO_BUFFER, vec3(1.0,1.0,1.0));

    // we need to write 1.0 into the alpha so dilation during compositing doesn't kill the contribution if no lights are present in the scene
    if (rl_InRay.rayType == PVR_RAY_TYPE_ENV)
        Accumulate(rl_InRay.renderTarget, vec4( rl_InRay.color.rgb * rl_InRay.weight * rl_InRay.albedo, 1.0), rl_InRay.probeDir, rl_InRay.lightmapMode);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\EnvImplement.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Environment.rlsl---------------
.
.
/*
    This file contains helper functions for importance sampling the environment/sky.
*/


// EnvironmentLightingSamples contains data used to importance sample the environment map.
uniformblock EnvironmentLightingSamples
{
    int   NumRaysIndirect;                          // number of environment rays shot for each subsequent intersection of a path
    int   NumSamples;                               // the actual number of valid samples in the Directions and WeightedIntensities arrays
    float EnvmapIntegral;                           // contains the integral of the environment over the sphere surface according to a given metric
    int   Flags;                                    // various flags
    vec4  Directions[PVR_MAX_ENVSAMPLES];           // contains a direction into the environment map
    vec4  WeightedIntensities[PVR_MAX_ENVSAMPLES];  // contains (sampleIntensity / EnvironmentPDF)
};

uniformblock SkyboxTextures
{
    sampler2D FrontTex;
    sampler2D BackTex;
    sampler2D LeftTex;
    sampler2D RightTex;
    sampler2D UpTex;
    sampler2D DownTex;
};

bool UseEnvironmentImportanceSampling()
{
    return (EnvironmentLightingSamples.Flags & 1) != 0;
}

bool SampleDirectEnvironment()
{
    return (EnvironmentLightingSamples.Flags & 2) == 0 && IntegratorSamples.maxBounces > 0;
}

bool SampleIndirectEnvironment()
{
    return (EnvironmentLightingSamples.Flags & 4) == 0;
}

int GetRaysPerEnvironmentIndirect()
{
    return EnvironmentLightingSamples.NumRaysIndirect;
}


vec3 GetSkyBoxColor(vec3 direction)
{
    vec2 skyTexCoord;

    vec3 tempDir = normalize(direction);
    direction = tempDir;

    vec3 absDir = abs(direction);

    vec4 texColor = vec4(1.0);

    //See if the X axis is dominant in the direction vector
    if (absDir.x > absDir.y && absDir.x > absDir.z) {
        if (direction.x > 0.0) {
            skyTexCoord = vec2(-direction.z / absDir.x, -direction.y / absDir.x) / vec2(2.0) + vec2(0.5);
            texColor = texture2D(SkyboxTextures.LeftTex, skyTexCoord);
        }
        else {
            skyTexCoord = vec2(direction.z / absDir.x, -direction.y / absDir.x) / vec2(2.0) + vec2(0.5);
            texColor = texture2D(SkyboxTextures.RightTex, skyTexCoord);
        }
    }
    else {
        if (absDir.y > absDir.z) {
            if (direction.y > 0.0) {
                skyTexCoord = vec2(direction.x / absDir.y, direction.z / absDir.y) / vec2(2.0) + vec2(0.5);
                texColor = texture2D(SkyboxTextures.UpTex, skyTexCoord);
            }
            else {
                skyTexCoord = vec2(direction.x / absDir.y, -direction.z / absDir.y) / vec2(2.0) + vec2(0.5);
                texColor = texture2D(SkyboxTextures.DownTex, skyTexCoord);
            }
        }
        else {
            if (direction.z > 0.0) {
                skyTexCoord = vec2(direction.x / absDir.z, -direction.y / absDir.z) / vec2(2.0) + vec2(0.5);
                texColor = texture2D(SkyboxTextures.FrontTex, skyTexCoord);
            }
            else {
                skyTexCoord = vec2(-direction.x / absDir.z, -direction.y / absDir.z) / vec2(2.0) + vec2(0.5);
                texColor = texture2D(SkyboxTextures.BackTex, skyTexCoord);
            }
        }
    }

    return texColor.xyz;
}

// sets up parameters for an environment ray
void CastEnvironment(int target, vec3 pos, vec3 dir, vec3 firstDir, vec3 albedo, vec3 intensity, float weight, int depth, int transDepth, LightmapMode lightmapMode, int lodParam, float lodT)
{
    createRay();
    rl_OutRay.origin           = pos.xyz;
    rl_OutRay.direction        = dir;
    rl_OutRay.color            = vec4(intensity, 0.0 );
    rl_OutRay.probeDir         = firstDir;
    rl_OutRay.renderTarget     = target;
    rl_OutRay.isOutgoing       = true;          // undocumented built-in boolean
    rl_OutRay.sampleIndex      = 0;             // unused
    rl_OutRay.depth            = depth;
    rl_OutRay.weight           = weight;
    rl_OutRay.albedo           = albedo;
    rl_OutRay.sameOriginCount  = 0;
    rl_OutRay.transmissionDepth= transDepth;
    rl_OutRay.lightmapMode     = lightmapMode;
    rl_OutRay.lodParam         = lodParam;
    rl_OutRay.rayType          = PVR_RAY_TYPE_ENV;
    rl_OutRay.rayClass         = MapLodParamToRayClass(PVR_RAY_TYPE_ENV, lodParam);
    rl_OutRay.lodOrigin        = rl_OutRay.origin;
    rl_OutRay.originalT        = rl_OutRay.maxT;
    if (lodParam == 0)
    {
        rl_OutRay.occlusionTest    = true;
        rl_OutRay.defaultPrimitive = GetEnvPrimitive();
    }
    else
    {
        rl_OutRay.maxT             = lodT;
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.defaultPrimitive = GetLodMissPrimitive();
    }
    emitRayWithoutDifferentials();
}

// importance sampling path related functions to estimate pdfs
float EnvironmentMetric(vec3 intensity)
{
    // use the max intensity as a metric. Keep in sync with EnvironmentMetric in .cpp
    return max(max(intensity.r, intensity.g), intensity.b);
}

// calculates the weight using a balanced heuristic
float EnvironmentHeuristic(float pdf1, float pdf2)
{
    float denom = pdf1 + pdf2;
    return denom > 0.0 ? (pdf1 / denom) : 0.0;
}

// importance sampling path for sampling the environment according to the one-sample model
void SurfaceSampleEnvironmentIS(int target, vec3 position, vec3 firstDir, vec3 interpNormal, vec3 geomNormal, vec3 albedo, vec3 rand, float weight, int depth, int transDepth, LightmapMode lightmapMode, int lodParam, float lodT, bool firstDirValid)
{
    // frame of reference for sampling hemisphere
    vec3 b1, b2;
    CreateOrthoNormalBasis(interpNormal, b1, b2);
    mat3 onb = mat3(b1, b2, interpNormal);

    vec3 direction, intensity;
    bool shootRay;

    // Use one random sample rule instead of estimating both pdfs.
    // Due to this the chosen path has its is weight multiplied by 2.0 as we're evenly drawing from the two pdfs.

    if (rand.z > 0.5)
    {
        int   sampleIndex = int(fract(rand.x) * float(EnvironmentLightingSamples.NumSamples)) % EnvironmentLightingSamples.NumSamples;
              direction   = EnvironmentLightingSamples.Directions[sampleIndex].xyz;
        float cosdir      = max( 0.0, dot(direction, onb[2]));
        float pdf_diffuse = cosdir / PI;
              intensity   = GetSkyBoxColor(direction);
        float metric      = EnvironmentMetric(intensity);
        float pdf_envmap  = metric / EnvironmentLightingSamples.EnvmapIntegral;
        float is_weight  = EnvironmentHeuristic(pdf_envmap, pdf_diffuse);

        // The final weight is "2.0 * is_weight * intensity / pdf_environment * cos(dir, N) / PI", but we have pre-calculated "intensity / pdf_environment" on the CPU already
        // The multiplication by 2.0 compensates for us not drawing from both densities, but randomly and evenly choosing one of them.
        vec3 weightedIntensity = EnvironmentLightingSamples.WeightedIntensities[sampleIndex].xyz;
             intensity         = 2.0 * is_weight * weightedIntensity * cosdir / PI;
             shootRay          = dot(direction, geomNormal) > 0.0 && cosdir > 0.0;
    }
    else
    {
              direction   = HemisphereCosineSample(rand.xy);                    // cosine weighted samples
        float pdf_diffuse = direction.z / PI;                                   // cosine weight
              direction   = onb * direction;
              intensity   = GetSkyBoxColor(direction);
        float metric      = EnvironmentMetric(intensity);
        float pdf_envmap  = metric / EnvironmentLightingSamples.EnvmapIntegral;
        float is_weight  = EnvironmentHeuristic( pdf_diffuse, pdf_envmap );

        // The final weight is "2.0 * is_weight * intensity / pdf_diffuse * cos * brdf" in which case pdf_diffuse and (cos) eliminate the cosine.
        // The remaining PI is eliminated by the diffuse BRDF's 1/PI normalization that we're already handling here
        // The multiplication by 2.0 compensates for us not drawing from both densities, but randomly and evenly choosing one of them.
              intensity   = 2.0 * is_weight * intensity;
              shootRay    = dot(direction, geomNormal) > 0.0;
    }

    // Sampling the hemisphere around the interpolated normal can generate directions below the geometric surface, so we're guarding against that
    if (shootRay)
        CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, intensity, weight, depth, transDepth, lightmapMode, lodParam, lodT);
}

// non-is path
void SurfaceSampleEnvironment(int target, vec3 position, vec3 firstDir, vec3 interpNormal, vec3 geomNormal, vec3 albedo, vec2 rand, float weight, int depth, int transDepth, LightmapMode lightmapMode, int lodParam, float lodT, bool firstDirValid)
{
    vec3 b1, b2;
    CreateOrthoNormalBasis(interpNormal, b1, b2);
    mat3 onb = mat3(b1, b2, interpNormal);

    // sample hemisphere
    vec3 direction = onb * HemisphereCosineSample(rand);

    if (dot(direction, geomNormal) > 0.0)
    {
        vec3 intensity = GetSkyBoxColor(direction);
        CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, intensity, weight, depth, transDepth, lightmapMode, lodParam, lodT);
    }
}

// importance sampling path for sampling the environment according to the one-sample model
void VolumeSampleEnvironmentIS(int target, vec3 position, vec3 firstDir, vec3 albedo, vec3 rand, float weight, int depth, int transDepth, bool firstDirValid)
{
    // Use one random sample rule instead of estimating both pdfs.
    // Due to this the chosen path has its importance sampling weight multiplied by 2.0 as we're evenly drawing from the two pdfs.
    if (rand.z > 0.5)
    {
        int   sampleIndex = int(rand.x * float(EnvironmentLightingSamples.NumSamples));
        vec3  direction   = EnvironmentLightingSamples.Directions[sampleIndex].xyz;
        float pdf_diffuse = 1.0 / (4.0 * PI);
        vec3  intensity   = GetSkyBoxColor(direction);
        float metric      = EnvironmentMetric(intensity);
        float pdf_envmap  = metric / EnvironmentLightingSamples.EnvmapIntegral;
        float is_weight  = EnvironmentHeuristic(pdf_envmap, pdf_diffuse);

        // The final weight is "2.0 * is_weight * intensity / pdf_environment", but we have pre-calculated "intensity / pdf_environment" on the CPU already
        // The multiplication by 2.0 compensates for us not drawing from both densities, but randomly and evenly choosing one of them.
        vec3 weightedIntensity = EnvironmentLightingSamples.WeightedIntensities[sampleIndex].xyz;
        CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, 2.0 * is_weight * weightedIntensity / PI, weight, depth, transDepth, 0, 0, PVR_FLT_MAX);
    }
    else
    {
        vec3  direction   = SphereSample(rand.xy);
        float pdf_diffuse = 1.0 / (4.0 * PI);
        vec3  intensity   = GetSkyBoxColor(direction);
        float metric      = EnvironmentMetric(intensity);
        float pdf_envmap  = metric / EnvironmentLightingSamples.EnvmapIntegral;
        float is_weight  = EnvironmentHeuristic(pdf_diffuse, pdf_envmap);

        // The final weight is "2.0 * is_weight * intensity / pdf_diffuse".
        // The multiplication by 2.0 compensates for us not drawing from both densities, but randomly and evenly choosing one of them.
        CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, 2.0 * is_weight * intensity * 4.0, weight, depth, transDepth, 0, 0, PVR_FLT_MAX);
    }
}

// non-is path
void VolumeSampleEnvironment(int target, vec3 position, vec3 firstDir, vec3 albedo, vec2 rand, float weight, int depth, int transDepth, bool firstDirValid)
{
    vec3  direction   = SphereSample(rand);
    vec3  intensity   = GetSkyBoxColor(direction);
    float pdf_diffuse = 1.0 / 4.0; // PI in denominator cancels out with SH
    CastEnvironment(target, position, direction, firstDirValid ? firstDir : direction, albedo, intensity, weight / pdf_diffuse, depth, transDepth, 0, 0, PVR_FLT_MAX);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Environment.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\GIBakeFrameShader.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

#define ENABLE_CULLING 1

#define DIRECT_MODE_DISABLED 0
#define DIRECT_MODE_ONLY 1
#define DIRECT_MODE_GI 2

uniform sampler2D  PositionsTex;
uniform sampler2D  InterpolatedNormalsTex;
uniform sampler2D  PlaneNormalsTex;
uniform sampler2D  DirectLighting;
uniform sampler2D  PrevComposite;
uniform sampler2D  ConvergenceMap;
uniform sampler2D  CullingMap;
uniform sampler2D  GISamplesMap;
uniform sampler2D  DirectSamplesMap;
uniform sampler2D  EnvSamplesMap;
uniform sampler2D  InstanceTransforms;
uniform sampler2D  InstanceProperties;

uniform int InstanceTransformsWidth;
uniform int InstanceTransformsHeight;
uniform int InstancePropertiesWidth;
uniform int InstancePropertiesHeight;
uniform int TransformOffset;
uniform int OutputRayCount;
uniform int DirectSamplesPerPass;
uniform int GISamplesPerPass;
uniform int EnvSamplesPerPass;
uniform int DirectMaxSamples;
uniform int GIMaxSamples;
uniform int EnvMaxSamples;
uniform float PushOff;
uniform int SupersamplingMultiplier;
uniform float OneOverSupersamplingMultiplier;
uniform LightmapMode OutputlightmapMode;
uniform int LodPass;
uniform int DoShadowMask;
uniform int MaxDirectLightsCount;


void setup()
{
    rl_OutputRayCount = OutputRayCount;
}

mat4 GetInstanceTransform(int instanceIndex, out mat4 inv)
{
    int kPixelsPerInstance = 8;
    int linearIdx = instanceIndex * kPixelsPerInstance;
    int y = int(linearIdx/InstanceTransformsWidth);
    int x = linearIdx - y*InstanceTransformsWidth;
    float xTex = float(x)+0.5;
    float yTex = (float(y)+0.5)/float(InstanceTransformsHeight);
    float w = float(InstanceTransformsWidth);

    vec2 uv1 = vec2(xTex/w, yTex);
    vec2 uv2 = vec2((xTex + 1.0)/w, yTex);
    vec2 uv3 = vec2((xTex + 2.0)/w, yTex);
    vec2 uv4 = vec2((xTex + 3.0)/w, yTex);

    vec4 r1 = texture2D(InstanceTransforms, uv1);
    vec4 r2 = texture2D(InstanceTransforms, uv2);
    vec4 r3 = texture2D(InstanceTransforms, uv3);
    vec4 r4 = texture2D(InstanceTransforms, uv4);

    // load the inverse to transform normals
    float inverse_offset = 4.0 / w;
    vec2 iuv1 = vec2(uv1.x + inverse_offset, uv1.y);
    vec2 iuv2 = vec2(uv2.x + inverse_offset, uv2.y);
    vec2 iuv3 = vec2(uv3.x + inverse_offset, uv3.y);
    vec2 iuv4 = vec2(uv4.x + inverse_offset, uv4.y);

    inv[0] = texture2D(InstanceTransforms, iuv1);
    inv[1] = texture2D(InstanceTransforms, iuv2);
    inv[2] = texture2D(InstanceTransforms, iuv3);
    inv[3] = texture2D(InstanceTransforms, iuv4);

    return mat4(r1,r2,r3,r4);
}

vec4 GetInstanceProperties(int instanceIndex)
{
    int y = int(instanceIndex / InstancePropertiesWidth);
    int x = instanceIndex - y*InstancePropertiesWidth;
    float xTex = (float(x) + 0.5) / float(InstancePropertiesWidth);
    float yTex = (float(y) + 0.5) / float(InstancePropertiesHeight);
    return texture2D(InstanceProperties, vec2(xTex, yTex));
}

bool GetReceiveShadows(vec4 instanceProperties)
{
    // Keep in sync with data generation in BakeContextManager::SetInstanceReceiveShadowsData
    return instanceProperties.x > 0.5;
}

int GetLodParams(vec4 instanceProperties, out int lodMask, out float lodT)
{
    lodMask = int(instanceProperties.z);
    lodMask = (lodMask & (-lodMask));
    lodT    = instanceProperties.w;
    return PackLodParam(int(instanceProperties.y), lodMask);
}

bool SkipLodInstance(int lodMask)
{
    return LodPass == 0 ? (lodMask == PVR_LOD_7_BIT) : (lodMask != PVR_LOD_7_BIT);
}

vec2 GetSampleUV (vec2 frameCoord, vec2 frameSize, int sampleStartIndex)
{
    int supersamplingMultiplierSquared = SupersamplingMultiplier * SupersamplingMultiplier;
    int sampleIndex = sampleStartIndex % supersamplingMultiplierSquared;
    int y = int(floor(float(sampleIndex) * OneOverSupersamplingMultiplier));
    int x = sampleIndex - y * SupersamplingMultiplier;

    return (frameCoord - vec2(0.5, 0.5) + (0.5 + vec2(x, y)) * OneOverSupersamplingMultiplier) / frameSize;
}

vec2 GetRandomSampleUV (vec2 frameCoord, vec2 frameSize, int sampleIndex)
{
    float cpShift = GetCranleyPattersonRotation1D(int(frameCoord.x), int(frameCoord.y), 0);
    float ssIDxRand = fract(SobolSample(sampleIndex, 2, 0) + cpShift);

    int ss = SupersamplingMultiplier * SupersamplingMultiplier;
    int ssIDxRandInt = int( floor(float(ss) * ssIDxRand) );

    ssIDxRandInt = ssIDxRandInt % ss;
    int y = int(floor(float(ssIDxRandInt) * OneOverSupersamplingMultiplier));
    int x = ssIDxRandInt - y * SupersamplingMultiplier;

    return (frameCoord - vec2(0.5, 0.5) + (0.5 + vec2(x, y)) * OneOverSupersamplingMultiplier) / frameSize;
}

// Grab the GBuffer data and transform to world space
bool GetGBufferDataWS(vec2 uv, out vec3 position, out vec3 smoothNormal, out vec3 planeNormal, out int instanceIndex)
{
    vec4 interpObjNormal = texture2D(InterpolatedNormalsTex, uv);

    if(interpObjNormal.w < 0.0)
        return false;

    vec4 planeObjNormal = texture2D(PlaneNormalsTex, uv);
    vec4 objPosition = texture2D(PositionsTex, uv);
    instanceIndex = int(floor(objPosition.w)) + TransformOffset;

    mat4 transform_inverse;
    mat4 transform = GetInstanceTransform(instanceIndex, transform_inverse);

    // have to multiply with the transposed inverse, so invert multiplication order
    smoothNormal = normalize(mat3(transform_inverse) * interpObjNormal.xyz);
    planeNormal = normalize(mat3(transform_inverse) * planeObjNormal.xyz);

    position = (vec4(objPosition.xyz, 1.0) * transform).xyz;

    return true;
}

// Some notes on how the sampling works:
// The entire path launched from a sample uses the same sobol index.
// When the ray hits a surface, we evaluate the sobol sequence with the same index but with an increased dimension.
void GISampling(int passRayCount, int currentRayCount, int numRays, LightmapMode lightmapMode)
{
    int passRays = 0;

    for(int i = 0; i < passRayCount; ++i)
    {
        int sampleIndex = currentRayCount + i;
        vec2 gbufferUV = GetRandomSampleUV (rl_FrameCoord.xy, rl_FrameSize.xy, sampleIndex);

        // Get GBuffer data
        vec3 position;
        vec3 smoothNormal;
        vec3 planeNormal;
        int instanceIndex;
        if (!GetGBufferDataWS(gbufferUV, position, smoothNormal, planeNormal, instanceIndex))
            break;

        // LOD handling
        vec4 instanceProperties = GetInstanceProperties(instanceIndex);

        int lodMask;
        float lodT;
        int lodParam = GetLodParams(instanceProperties, lodMask, lodT);

        if (SkipLodInstance(lodMask))
            break;

        vec3 positionPushedOff = position + planeNormal * PushOff;

        // frame of reference for sampling hemisphere
        vec3 b1;
        vec3 b2;
        CreateOrthoNormalBasis(smoothNormal, b1, b2);

        // sample hemisphere
        vec2 rnd;

#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleIndex < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            rnd = vec2( BlueNoiseSobolSample(sampleIndex, 0, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset),
                        BlueNoiseSobolSample(sampleIndex, 1, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset) );
        }
        else
#endif
        {
            vec2 cpShift = GetCranleyPattersonRotation2D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
            rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift);
        }

        vec3 hamDir = HemisphereCosineSample(rnd);
        hamDir = hamDir.x*b1 + hamDir.y*b2 + hamDir.z*smoothNormal;

        passRays++;

        float dotVal = dot(hamDir, planeNormal);
        if (dotVal <= 0.0 || isnan(dotVal))
            continue;

        createRay();
        rl_OutRay.origin           = positionPushedOff;
        rl_OutRay.direction        = hamDir;
        rl_OutRay.color            = vec4(0.0); // unused, because we're not shooting against lights
        rl_OutRay.probeDir         = normalize(hamDir);
        rl_OutRay.renderTarget     = GI_BUFFER;
        rl_OutRay.isOutgoing       = true;
        rl_OutRay.sampleIndex      = sampleIndex;
        rl_OutRay.rayType          = PVR_RAY_TYPE_GI;
        rl_OutRay.rayClass         = MapLodParamToRayClass(PVR_RAY_TYPE_GI, lodParam);
        rl_OutRay.depth            = 0;
        rl_OutRay.weight           = 1.0;
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.albedo           = vec3(1.0);
        rl_OutRay.sameOriginCount  = 0;
        rl_OutRay.transmissionDepth= 0;
        rl_OutRay.lightmapMode     = lightmapMode;
        rl_OutRay.lodParam         = lodParam;
        rl_OutRay.lodOrigin        = rl_OutRay.origin;
        rl_OutRay.originalT        = rl_OutRay.maxT;
        if (lodParam == 0)
        {
            rl_OutRay.defaultPrimitive = GetEnvPrimitive();
        }
        else
        {
            rl_OutRay.maxT             = lodT;
            rl_OutRay.defaultPrimitive = GetLodMissPrimitive();
        }
        emitRayWithoutDifferentials();
    }

    accumulate(GI_SAMPLES_BUFFER, float(passRays));
}



void DirectSampling(int rayBudget, int curDirectSamples, LightmapMode lightmapMode, bool shadowmask)
{
    float maxDirectRcp = 1.0 / float(DirectMaxSamples);
    int   convergedSamples = 0;
    int   startIndex  = curDirectSamples;
    int   globalIndex = curDirectSamples;


    while (rayBudget > 0 && convergedSamples < DirectMaxSamples)
    {
        int lightIndex = int(float(globalIndex) * maxDirectRcp);
        if (lightIndex >= MaxDirectLightsCount)
            break;

        int sampleIndex = globalIndex - lightIndex * DirectMaxSamples;

        vec2 cpShift = GetCranleyPattersonRotation2D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);

        vec2 gbufferUV = GetRandomSampleUV(rl_FrameCoord.xy, rl_FrameSize.xy, sampleIndex);

        // Get GBuffer data
        vec3 position;
        vec3 smoothNormal;
        vec3 planeNormal;
        int instanceIndex;
        if (!GetGBufferDataWS(gbufferUV, position, smoothNormal, planeNormal, instanceIndex))
            break;

        // LOD handling
        vec4 instanceProperties = GetInstanceProperties(instanceIndex);

        int lodMask;
        float lodT;
        int lodParam = GetLodParams(instanceProperties, lodMask, lodT);

        if (SkipLodInstance(lodMask))
            break;

        globalIndex++;
        vec3 positionPushedOff = position + planeNormal * PushOff;

        int totalNumLights = GetTotalNumLights(positionPushedOff);
        if (lightIndex >= totalNumLights)
        {
            convergedSamples += lightIndex == 0 ? 1 : 0;
            continue;
        }

    vec2 rnd;

#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleIndex < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            rnd = vec2( BlueNoiseSobolSample(sampleIndex, 0, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.direct_blue_noise_buffer_offset),
                        BlueNoiseSobolSample(sampleIndex, 1, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.direct_blue_noise_buffer_offset) );
        }
        else
#endif
        {
            vec2 cpShift = GetCranleyPattersonRotation2D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
            rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift);
        }


        bool receiveShadows = GetReceiveShadows(instanceProperties);
        int remaining = DoShadows(lightIndex, 1, positionPushedOff, smoothNormal, vec3(1.0), GI_DIRECT_BUFFER, rnd.xyy, vec3(0.0), lightmapMode, lodParam, lodT, 0, OCCLUSIONMODE_DIRECT, vec4(-1.0), 1.0, receiveShadows);
        if (shadowmask)
            DoShadows(lightIndex, 1, positionPushedOff, smoothNormal, vec3(1.0), SHADOW_MASK_BUFFER, rnd.xyy, vec3(0.0), LIGHTMAPMODE_NOTUSED, lodParam, lodT, 0, OCCLUSIONMODE_SHADOWMASK, vec4(-1.0), 1.0, receiveShadows);

        convergedSamples += remaining == 0 ? 1 : 0; // accumulate the sample if it converged during this iteration
        rayBudget--;
    }

    int accValue = globalIndex - startIndex;
    accumulate(GI_DIRECT_BUFFER, vec4(0.0, 0.0, 0.0, convergedSamples));
    accumulate(DIRECT_SAMPLES_BUFFER, float(accValue));
}


void EnvironmentSampling(int passRayCount, int currentRayCount, LightmapMode lightmapMode)
{
    if (!SampleDirectEnvironment())
    {
        accumulate(ENV_SAMPLES_BUFFER, float(passRayCount));
        return;
    }

    int  passRays = 0;
    bool useIS = UseEnvironmentImportanceSampling();

    for (int i = 0; i < passRayCount; ++i)
    {
        int sampleIndex = currentRayCount + i;

        // Get position and normal for a sample position
        vec3 position, interpNormal, geomNormal;
        vec3 rand;
        int  lodParam = 0;
        float lodT;
        {
            // sample gbuffer data
            vec2 gbufferUV = GetRandomSampleUV(rl_FrameCoord.xy, rl_FrameSize.xy, sampleIndex);
            int  instanceIndex;

            if (!GetGBufferDataWS(gbufferUV, position, interpNormal, geomNormal, instanceIndex))
                break;

            // LOD handling (need to do this per sample, as different instances could possibly by mapped to the same lightmap texel)
            vec4 instanceProperties = GetInstanceProperties(instanceIndex);

            int lodMask;
            lodParam = GetLodParams(instanceProperties, lodMask, lodT);

            if (SkipLodInstance(lodMask))
                break;

            position += geomNormal * PushOff;

            // create 3d random variable
#if PLM_USE_BLUE_NOISE_SAMPLING
            if(sampleIndex < PLM_BLUE_NOISE_MAX_SAMPLES)
            {
            rand = vec3( BlueNoiseSobolSample(sampleIndex, 0, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.env_blue_noise_buffer_offset),
                         BlueNoiseSobolSample(sampleIndex, 1, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.env_blue_noise_buffer_offset),
                         BlueNoiseSobolSample(sampleIndex, 2, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.env_blue_noise_buffer_offset) );
            }
            else
#endif
            {
                vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
                rand = fract(vec3(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0), SobolSample(sampleIndex, 2, 0)) + cpShift);
            }
        }

        if (useIS)
            SurfaceSampleEnvironmentIS(ENV_BUFFER, position, vec3(0.0), interpNormal, geomNormal, vec3(1.0, 1.0, 1.0), rand, 1.0, 0, 0, lightmapMode, lodParam, lodT, false);
        else
            SurfaceSampleEnvironment(ENV_BUFFER, position, vec3(0.0), interpNormal, geomNormal, vec3(1.0, 1.0, 1.0), rand.xy, 1.0, 0, 0, lightmapMode, lodParam, lodT, false);

        passRays++;
    }
    accumulate(ENV_SAMPLES_BUFFER, float(passRays));
}


void main()
{
    vec2  frameCoord  = rl_FrameCoord.xy / rl_FrameSize.xy;


#if ENABLE_CULLING
    vec4 cull = texture2D(CullingMap, frameCoord);
    if(cull.r <= 0.0)
        return;
#endif
    int curGISamples = int(texture2D(GISamplesMap, frameCoord).x);
    int curDirectSamples = int(texture2D(DirectSamplesMap, frameCoord).x);
    int curEnvSamples = int(texture2D(EnvSamplesMap, frameCoord).x);

    int conv = int(texture2D(ConvergenceMap, frameCoord).x * 255.0);
    // Check against midpoints between values defined in the convergence job.
    bool isDirectConverged = (conv & PVR_CONVERGED_DIRECT) != 0;
    bool isGIConverged     = (conv & PVR_CONVERGED_GI)     != 0;
    bool isEnvConverged    = (conv & PVR_CONVERGED_ENV)    != 0;

    if (!isGIConverged && GISamplesPerPass > 0)
    {
        // Avoid overshooting GI samples
        int clampedGIsamplesPerPass = min(max(0, GIMaxSamples - curGISamples), GISamplesPerPass);
        GISampling(clampedGIsamplesPerPass, curGISamples, GIMaxSamples, OutputlightmapMode);
    }

    if (!isDirectConverged && DirectSamplesPerPass > 0)
    {
        DirectSampling(DirectSamplesPerPass, curDirectSamples, OutputlightmapMode, DoShadowMask == 1);
    }

    if (!isEnvConverged && EnvSamplesPerPass > 0)
    {
        int clampedEnvSamplesPerPass = min(max(0, EnvMaxSamples - curEnvSamples), EnvSamplesPerPass);
        EnvironmentSampling(clampedEnvSamplesPerPass, curEnvSamples, OutputlightmapMode);

    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\GIBakeFrameShader.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Integrator.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniformblock IntegratorSamples {
    float goldenSamples[MAX_GOLDEN_SAMPLES];
    int sobolMatrices[SOBOL_MATRIX_SIZE];
    int maxBounces;
    int blueNoiseSamplingBuffer[PLM_BLUE_NOISE_SAMPLING_BUFFER_SIZE];
    int blueNoiseRankingBuffer[PLM_BLUE_NOISE_RANKING_BUFFER_SIZE];
    int blueNoiseScramblingBuffer[PLM_BLUE_NOISE_SCRAMBLING_BUFFER_SIZE];
    int gi_blue_noise_buffer_offset;
    int direct_blue_noise_buffer_offset;
    int env_blue_noise_buffer_offset;
    int minBounces;
};

float BlueNoiseSobolSample(int index, int dimension, int texel_x, int texel_y, int blue_noise_buffer_offset)
{
    // wrap arguments
    int pixel_i = texel_x % PLM_BLUE_NOISE_TILE_SIZE;
    int pixel_j = texel_y % PLM_BLUE_NOISE_TILE_SIZE;
    int sampleIndex = index % PLM_BLUE_NOISE_MAX_SAMPLES;
    int sampleDimension = dimension % PLM_BLUE_NOISE_MAX_DIMENSIONS;

    int rankedSampleIndex = sampleIndex ^ IntegratorSamples.blueNoiseRankingBuffer[blue_noise_buffer_offset + sampleDimension%PLM_BLUE_NOISE_MAX_RANKING_DIMENSIONS + (pixel_i + pixel_j * PLM_BLUE_NOISE_TILE_SIZE) * PLM_BLUE_NOISE_MAX_RANKING_DIMENSIONS];

    // fetch value in sequence
    int value = IntegratorSamples.blueNoiseSamplingBuffer[sampleDimension + (rankedSampleIndex * PLM_BLUE_NOISE_MAX_DIMENSIONS)];

    // If the dimension is optimized,
    //xor sequence value based on optimized scrambling
    value = value ^ IntegratorSamples.blueNoiseScramblingBuffer[blue_noise_buffer_offset +  (sampleDimension % PLM_BLUE_NOISE_MAX_SCRAMBLING_DIMENSIONS) + (pixel_i + pixel_j * PLM_BLUE_NOISE_TILE_SIZE) * PLM_BLUE_NOISE_MAX_SCRAMBLING_DIMENSIONS];
    // convert to float and return
    float v = float(value);
    v = (0.5 + v) / 256.0;
    return v;
}

// Sample Sobol sequence
#define MATSIZE 52
float SobolSample (int index, int dimension, int scramble)
{
    int result = scramble;
    for (int i = dimension * MATSIZE; index != 0; index >>= 1, ++i)
    {
        if ((index & 1) != 0)
            result ^= int(IntegratorSamples.sobolMatrices[i]);
    }
    float res = float(result) * 2.3283064365386963e-10; // (1.f / (1ULL << 32));
    return (res < 0.0 ? res + 1.0 : res);
}

vec3 GetCranleyPattersonRotation3D(int texel_x, int texel_y, int base_dimension)
{
    //We use a modulo on base_dimension+0, base_dimension+1 and base_dimension+2 to be sure the texel doesn't uses random numbers from another texels, leading to correlation issues.
    //It can happen if someone calls this function with a dimension that exceed what we have accounted for.
    //So far we accounted for PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION.

    //We also bound the texel index in a PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP*PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP square to avoid consuming too much memory with high res lightmaps
    int texel_index = (texel_x% PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP ) + (texel_y% PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP )*min(int(rl_FrameSize.x),PLM_MAX_SIZE_GOLDEN_SAMPLES_MAP);

    int dim0_rnd = texel_index * PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION + (base_dimension + 0) % PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION;
    int dim1_rnd = texel_index * PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION + (base_dimension + 1) % PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION;
    int dim2_rnd = texel_index * PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION + (base_dimension + 2) % PLM_MAX_NUM_CRANLEY_PATTERSON_ROTATION;

    return vec3(IntegratorSamples.goldenSamples[dim0_rnd], IntegratorSamples.goldenSamples[dim1_rnd], IntegratorSamples.goldenSamples[dim2_rnd]);
}

vec2 GetCranleyPattersonRotation2D(int texel_x, int texel_y, int base_dimension)
{
    return GetCranleyPattersonRotation3D(texel_x, texel_y, base_dimension).xy;
}

float GetCranleyPattersonRotation1D(int texel_x, int texel_y, int base_dimension)
{
    return GetCranleyPattersonRotation3D(texel_x, texel_y, base_dimension).x;
}

vec2 Rotate2D (float angle, vec2 point)
{
    float cosAng = cos (angle);
    float sinAng = sin (angle);
    return vec2 (point.x*cosAng - point.y*sinAng, point.y*cosAng + point.x*sinAng);
}

// Map sample on square to disk (http://psgraphics.blogspot.com/2011/01/improved-code-for-concentric-map.html)
vec2 MapSquareToDisk (vec2 uv)
{
    float phi;
    float r;

    float a = uv.x * 2.0 - 1.0;
    float b = uv.y * 2.0 - 1.0;

    if (a * a > b * b)
    {
        r = a;
        phi = KQUARTERPI * (b / a);
    }
    else
    {
        r = b;

        if (b == 0.0)
        {
            phi = KHALFPI;
        }
        else
        {
            phi = KHALFPI - KQUARTERPI * (a / b);
        }
    }

    return vec2(r * cos(phi), r * sin(phi));
}

vec3 HemisphereCosineSample (vec2 rnd)
{
    vec2 diskSample = MapSquareToDisk(rnd);
    return vec3(diskSample.x, diskSample.y, sqrt(1.0 - dot(diskSample,diskSample)));
}

vec3 SphereSample(vec2 rnd)
{
    float ct = 1.0 - 2.0 * rnd.y;
    float st = sqrt(1.0 - ct * ct);

    float phi = KTWOPI * rnd.x;
    float cp = cos(phi);
    float sp = sin(phi);

    return vec3 (cp * st, sp * st, ct);
}

// iHash can end up being up to INT_MAX.
// 1. Some usages were adding other non-negative values to it and then doing modulo operation. The modulo operator can return a negative value for a negative dividend
// and a positive divisor (well, it always does that unless the remainder is 0).
// 2. Similarly abs(INT_MIN) (where we can get INT_MIN from e.g. INT_MAX+1) gives INT_MIN again, as -INT_MIN can't be stored as an int in two's complement representation.
// In either case, the result of those operations couldn't be used as an index into an array.
// We could either:
// a. get the negative value conditionally back into the [0;divisor) range by adding the divisor;
// b. bring the result of iHash into a more sensible range first (by doing the modulo operation) and only then add other non-negative values to it.
// We should not:
// c. use abs() on the (possibly) nagative modulo result of 1. That doesn't behave nicely when the value jumps from INT_MAX to INT_MIN, because the result (that was
// monotonically increasing up until now) starts decreasing, so we would reuse some array items and miss some others when using the result of those operations as an array index.
// Option b. is recommended.
int GetScreenCoordHash(vec2 pixel)
{
    // combine the x and y into a 32-bit int
    int iHash = ((int(dFstrip(pixel.y)) & 0xffff) << 16) + (int(dFstrip(pixel.x)) & 0xffff);

    iHash -= (iHash << 6);
    iHash ^= (iHash >> 17);
    iHash -= (iHash << 9);
    iHash ^= (iHash << 4);
    iHash -= (iHash << 3);
    iHash ^= (iHash << 10);
    iHash ^= (iHash >> 15);

    iHash &= 0x7fffffff; //make sure it's not negative

    return iHash;
}

vec3 GetRotatedHemisphereSample (vec2 rndSq, float rnd)
{
    float rot = rnd * KTWOPI;
    vec3 hamDir = HemisphereCosineSample(rndSq);
    return vec3(Rotate2D(rot, hamDir.xy).xy, hamDir.z);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Integrator.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\LightImplement.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

void main()
{
    Accumulate(rl_InRay.renderTarget, rl_InRay.weight * rl_InRay.color * vec4(rl_InRay.albedo,1.0), rl_InRay.probeDir, rl_InRay.lightmapMode);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\LightImplement.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Lighting.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/


#define OcclusionMode int
#define OCCLUSIONMODE_DIRECT           0
#define OCCLUSIONMODE_SHADOWMASK       1
#define OCCLUSIONMODE_PROBEOCCLUSION   2
#define OCCLUSIONMODE_DIRECT_ON_BOUNCE 3 // allows to skip the check whether to do direct

//keep that enum in sync with enum AngularFalloffType in wmLight.h and LightmapBake.cpp
//it allows to switch between different falloff computation
//for spotlights
#define ANGULARFALLOFFTYPE_LUT                         0
#define ANGULARFALLOFFTYPE_ANALYTIC_AND_INNER_ANGLE    1


uniformblock Lights
{
    // Per light data
    // Bytes per light: 116 = 3 * sizeof(int) + 3 * sizeof(vec3) + 4 * sizeof(vec4) + 1 * sizeof(float)
    // MB for all lights: 3.6 = (116 * MAX_LIGHTS) / 1024^2
    int       LightTypes[PVR_MAX_LIGHTS];
    int       LightShadowMaskChannels[PVR_MAX_LIGHTS];
    int       LightFalloffIndex[PVR_MAX_LIGHTS];
    vec3      LightPositions[PVR_MAX_LIGHTS];
    vec3      LightDirections[PVR_MAX_LIGHTS];
    vec3      LightTangents[PVR_MAX_LIGHTS];
    vec4      LightProperties0[PVR_MAX_LIGHTS];
    vec4      LightProperties1[PVR_MAX_LIGHTS];
    vec4      LightProperties2[PVR_MAX_LIGHTS];
    vec4      LightColors[PVR_MAX_LIGHTS];
    float     LightPowerDistributions[PVR_MAX_LIGHTS];

    // Per region data
    ivec2     NumLights[PVR_MAX_LIGHT_REGIONS]; // x = number of lights, y = offset into the cdf array
    float     CumulativePowerDistributions[PVR_MAX_CDFS]; // first element in a region slice is the cdf sum

    // Global data
    int       MaxDirLights;
    vec3      SceneBoundsMin;
    vec3      SceneBoundsMax;
    vec3      GridRegionSize;
    ivec3     GridDims;
    int       GridLength;
    int       RaysPerSoftShadow;
    int       TotalLights;
    vec2      LightIndicesRes;
};

uniformblock LightInfo
{
    vec4      AmbientSH[7];
    float     AmbientIntensity;
    primitive LightPrimitive;
    primitive EnvPrimitive;
    primitive LodMissPrimitive;
    float     AngularFalloffTable[MAX_ANGULAR_FALLOFF_TABLE_LENGTH];
    int       AngularFalloffTableLength;
};

uniformblock FalloffInfo
{
    sampler2D LightFalloff;
    int       LightFalloffWidth;
    int       LightFalloffHeight;
};

uniformblock LightCookieInfo
{
    vec4    CookieAtlasHalfTexelSize;
    vec4    ScaleOffset[PVR_MAX_COOKIES];
    int     LightToScaleOffset[PVR_MAX_LIGHTS];
};

uniformblock AOInfo
{
    float aoMaxDistance;
    int aoEnabled;
};

uniform sampler2D LightIndices;
uniform sampler2D LightCookies;

float SampleFalloff(int falloffIndex, float normalizedSamplePosition)
{
    int y = min(falloffIndex, FalloffInfo.LightFalloffHeight-1);
    int sampleCount = FalloffInfo.LightFalloffWidth;
    float index = normalizedSamplePosition*float(sampleCount);

    // compute the index pair
    int loIndex = min(int(index), int(sampleCount - 1));
    int hiIndex = min(int(index) + 1, int(sampleCount - 1));
    float hiFraction = (index - float(loIndex));

    float yTex = (float(y) + 0.5) / float(FalloffInfo.LightFalloffHeight);
    float xTexLo = (float(loIndex) + 0.5) / float(FalloffInfo.LightFalloffWidth);
    float xTexHi = (float(hiIndex) + 0.5) / float(FalloffInfo.LightFalloffWidth);

    vec2 uv1 = vec2(xTexLo, yTex);
    vec2 uv2 = vec2(xTexHi, yTex);

    vec4 sampleLo = texture2D(FalloffInfo.LightFalloff, uv1);
    vec4 sampleHi = texture2D(FalloffInfo.LightFalloff, uv2);

    // do the lookup
    return (1.0 - hiFraction) * sampleLo.x + hiFraction * sampleHi.x;
}

// This code must be kept in sync with FalloffLUT.cpp::LookupFalloffLUT
float LookupAngularFalloffLUT(float angularScale)
{
    int sampleCount = LightInfo.AngularFalloffTableLength;

    //======================================
    // light distance falloff lookup:
    //   d = Max(0, distance - m_Radius) / (m_CutOff - m_Radius)
    //   index = (g_SampleCount - 1) / (1 + d * d * (g_SampleCount - 2))
    float tableDist = max(angularScale, 0.0);
    float index = float(sampleCount - 1) / (1.0 + tableDist * tableDist * float(sampleCount - 2));

    // compute the index pair
    int loIndex = min(int(index), int(sampleCount - 1));
    int hiIndex = min(int(index) + 1, int(sampleCount - 1));
    float hiFraction = (index - float(loIndex));

    // do the lookup
    return (1.0 - hiFraction) * LightInfo.AngularFalloffTable[loIndex] + hiFraction * LightInfo.AngularFalloffTable[hiIndex];
}

primitive GetLightPrimitive()
{
    return LightInfo.LightPrimitive;
}

primitive GetEnvPrimitive()
{
    return LightInfo.EnvPrimitive;
}

// LoD related routines
primitive GetLodMissPrimitive()
{
    return LightInfo.LodMissPrimitive;
}

int PackLodParam(int LODGroupId, int LODMask)
{
    return (LODGroupId < 0 || LODMask == PVR_LOD_0_BIT) ? 0 :
        ((LODGroupId & PVR_LODGROUP_MASK) | (LODMask << PVR_LODMASK_SHIFT));
}

void UnpackLodParam(int lodParam, out int groupId, out int mask)
{
    if (lodParam == 0)
    {
        groupId = -1;
        mask = PVR_LOD_0_BIT;
    }
    else
    {
        groupId = lodParam & PVR_LODGROUP_MASK;
        mask    = (lodParam >> PVR_LODMASK_SHIFT) & 0xff;
    }
}

int MapLodParamToRayClass(int rayType, int lodParam)
{
    if (lodParam == 0)
        return (rayType == PVR_RAY_TYPE_GI || rayType == PVR_RAY_TYPE_ENV) ? PVR_RAY_CLASS_GI : PVR_RAY_CLASS_OCC;

    int lodMask   = (lodParam >> PVR_LODMASK_SHIFT) & 0xff;
    int shift     = (lodMask == PVR_LOD_7_BIT) ? 2 : 1;
        lodMask >>= shift;
    int rayClass  = PVR_RAY_CLASS_LOD_0;
    do
    {
        rayClass++;
        lodMask >>= 1;
    }
    while (lodMask != 0);

    return rayClass;
}

void ReshootLodRay(vec3 origin, float originalT, int depth, int rayType)
{
    createRay();
    rl_OutRay.origin           = origin;
    rl_OutRay.maxT             = originalT;
    rl_OutRay.depth            = depth;
    rl_OutRay.rayType          = rayType;
    rl_OutRay.rayClass         = (rayType == PVR_RAY_TYPE_GI || rayType == PVR_RAY_TYPE_ENV) ? PVR_RAY_CLASS_GI : PVR_RAY_CLASS_OCC;
    rl_OutRay.defaultPrimitive = GetEnvPrimitive();

    if (rayType == PVR_RAY_TYPE_SHADOW)
    {
        rl_OutRay.defaultPrimitive = GetLightPrimitive();
    }

    emitRayWithoutDifferentials();
}

int GetLightIndex(int regionIdx, int lightIdx)
{
    vec2 uv = vec2(((float(lightIdx) + 0.5) / float(Lights.LightIndicesRes.x)), ((float(regionIdx) + 0.5) / float(Lights.LightIndicesRes.y)));

    float idx = texture2D(LightIndices, uv).x;

    // Integers up to 2^24 can be accurately represented as floats,
    // so a simple truncating conversion to int below is fine.
    return clamp(int(idx), 0, Lights.TotalLights - 1 + PLM_MAX_DIR_LIGHTS);
}

ivec3 GetRegionLocContaining(vec3 point)
{
    vec3 p = point-Lights.SceneBoundsMin;


    return ivec3(int(p.x/Lights.GridRegionSize.x), int(p.y/Lights.GridRegionSize.y), int(p.z/Lights.GridRegionSize.z));
}

int GetRegionContaining(vec3 point)
{
    return OpenRLCPPShared_GetRegionIdx(GetRegionLocContaining(point), Lights.GridDims);
}

int GetMaxDirLights()
{
    return Lights.MaxDirLights;
}

int GetNumLights(int regionIdx)
{
    return regionIdx < 0 ? 0 : Lights.NumLights[regionIdx].x;
}

int GetTotalNumLights(vec3 point)
{
    return GetMaxDirLights() + GetNumLights(GetRegionContaining(point));
}

void GetCdf(int regionIdx, out float cdfSumRcp, out int cdfOffset, out int cdfCount)
{
    cdfCount   = Lights.NumLights[regionIdx].x+1;
    int offset = Lights.NumLights[regionIdx].y;
    cdfSumRcp  = Lights.CumulativePowerDistributions[offset];
    cdfOffset  = offset + 1;
}

int GetRaysPerSoftShadow()
{
    return Lights.RaysPerSoftShadow;
}

vec3 GetLightPosition(int lightIdx)
{
    return Lights.LightPositions[lightIdx];
}
vec4 GetLightProperties0(int lightIdx)
{
    return Lights.LightProperties0[lightIdx];
}
vec4 GetLightProperties1(int lightIdx)
{
    return Lights.LightProperties1[lightIdx];
}

float GetShadowType(int lightIdx)
{
    return Lights.LightProperties0[lightIdx].w;

}
int  GetLightType(int lightIdx)
{
    return Lights.LightTypes[lightIdx];
}
int  GetShadowMaskChannel(int lightIdx)
{
    return Lights.LightShadowMaskChannels[lightIdx];
}

bool GetLightmapsDoDirect(int lightIdx)
{
    return Lights.LightProperties1[lightIdx].w != 0.0;
}

bool GetLightProbesDoDirect(int lightIdx)
{
    return Lights.LightProperties2[lightIdx].w != 0.0;
}

void GetJitteredLightVec(inout vec3 lightVec, int lightIdx, int rayIdx, vec3 lightOffset, mat3 lightBasis);

bool IsNormalValid(vec3 normal)
{
    return normal != vec3(0.0);
}

vec2 GetCookieSizesRcp(int lightIdx)
{
    return vec2(Lights.LightProperties1[lightIdx].y, Lights.LightProperties1[lightIdx].z); // only valid if this is a directional light
}

void ClampCookieUVs(inout vec2 uvs, vec2 scale)
{
    vec2 halfTexelSize = LightCookieInfo.CookieAtlasHalfTexelSize.xy / scale;
    uvs = clamp(uvs, halfTexelSize, vec2(1.0, 1.0) - halfTexelSize);

}

vec4 GetCookieScaleOffset(int cookieIndex, out bool tileCookie)
{
    vec4 scale_offset = LightCookieInfo.ScaleOffset[cookieIndex];
         tileCookie   = scale_offset.x < 0.0; // the sign bit is set in PVRJobLightCookies.cpp - look for it->tex.repeat
    return vec4(abs(scale_offset.x), scale_offset.y, scale_offset.z, scale_offset.w);
}

vec4 GetCookieScaleOffset(int cookieIndex)
{
    bool tileCookie;
    return GetCookieScaleOffset(cookieIndex, tileCookie);
}


// This function assumes the region contains lights. Check to make sure this is true before calling this function.
int PickLight(in int regionIdx, inout float rand, inout float weight)
{
    // Gather region information
    int   numLights    = GetNumLights(regionIdx);
    float numLightsRcp = 1.0 / float(numLights);
    // Gather cdf data for the region
    float cdfSumRcp;
    int   cdfOffset, cdfCount;
    GetCdf(regionIdx, cdfSumRcp, cdfOffset, cdfCount);

    int lightIdx = -1;
    if (rand >= 0.5) // choose power density
    {
        // do a binary search on the cdfs
        int count = cdfCount;
        int b = 0;
        int it = 0;
        int step = 0;
        // rescale used interval half to [0;1) for next event estimation
        rand = (rand - 0.5) * 2.0;

        while (count > 0)
        {
            it = b;
            step = count / 2;
            it += step;
            if (Lights.CumulativePowerDistributions[cdfOffset + it] < rand)
            {
                b = ++it;
                count -= step + 1;
            }
            else
            {
                count = step;
            }
        }

        int segmentIdx = max(b, 1);
            lightIdx   = GetLightIndex(regionIdx, segmentIdx - 1);
        // rescale the random variable again
            float seg_max = Lights.CumulativePowerDistributions[cdfOffset + segmentIdx];
            float seg_min = Lights.CumulativePowerDistributions[cdfOffset + segmentIdx - 1];
                  rand    = (rand - seg_min) / (seg_max - seg_min);
        /* original is balance heuristic calculation for reference:
         *   float pdf_equi   = numLightsRcp;
         *   float pdf_power  = Lights.LightPowerDistributions[lightIdx] / cdfSum;
         *   float is_weight = pdf_power / (pdf_equi + pdf_power);
         *   weight *= 2.0 * is_weight / pdf_power;
        */
    }
    else
    {
        // rescale used interval half to [0;1) for next event estimation
        rand     = rand * 2.0;
        lightIdx = int(rand * float(numLights));
        lightIdx = GetLightIndex(regionIdx, min(lightIdx, numLights-1));
        /* original is balance heuristic calculation for reference:
         *   float pdf_equi   = numLightsRcp;
         *   float pdf_power  = Lights.LightPowerDistributions[lightIdx] / cdfSum;
         *   float is_weight = pdf_equi / (pdf_equi + pdf_power);
         *   weight *= 2.0 * is_weight / pdf_equi;
         */
    }

    float pdf_equi = numLightsRcp;
    float pdf_power = Lights.LightPowerDistributions[lightIdx] * cdfSumRcp;
    weight *= 2.0 / (pdf_equi + pdf_power);

    return lightIdx;
}

bool CalculateDirectionalLightColor(int lightIdx, vec3 normal, vec3 position, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    bool hasNormal = IsNormalValid(normal);

    float dotVal = hasNormal ? dot(normal, Lights.LightDirections[lightIdx]) : 1.0;
    if (dotVal <= 0.0 || isnan(dotVal))
        return false;

    colorOut *= dotVal;
    lightVecOut = Lights.LightDirections[lightIdx];
    maxTOut = 1e27;

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec3 lightPos      = Lights.LightPositions[lightIdx];
        vec3 toLight       = position - lightPos;
        float dist         = dot(lightVecOut, toLight);
        vec3 projected     = (position + dist * lightVecOut) - lightPos;
        vec3 lightBitan_NZ = cross(Lights.LightDirections[lightIdx], Lights.LightTangents[lightIdx]);
        vec2 scales        = GetCookieSizesRcp(lightIdx);
        vec2 uvs           = vec2(dot(projected, scales.x * lightBitan_NZ),
                                  dot(projected, scales.y * Lights.LightTangents[lightIdx]));
             uvs           = uvs * 0.5 + 0.5;

        bool tileCookie;
        vec4 scale_offset  = GetCookieScaleOffset(cookieIdx, tileCookie);
        uvs = tileCookie ? fract(uvs) : uvs;
        bvec4 inrange = bvec4(uvs.x >= 0.0, uvs.x <= 1.0, uvs.y >= 0.0, uvs.y <= 1.0);
        if (all(inrange))
        {
            ClampCookieUVs(uvs, scale_offset.xy);
            cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
        }
    }

    colorOut *= cookieAttenuation;

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    return true;
}

vec3 vabs(vec3 vec)
{
    return vec3(abs(vec.x), abs(vec.y), abs(vec.z));
}

vec3 sampleCookie(vec3 dir, int lightIdx)
{
    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];

    if (cookieIdx < 0)
        return vec3(1.0, 1.0, 1.0);

    // rotate
    vec3 y = Lights.LightTangents[lightIdx];
    vec3 z = -Lights.LightDirections[lightIdx];
    vec3 x = cross(y, z);
    mat3 rot = mat3(x, y, z);
         dir = dir * rot; // rotate light vector into cubemap frame (transposing here instead of inverse, as rot is orthonormal)

    // find slice
    int slice = 0;

    vec2 uvs;
    vec3 absdir = vabs(dir);
    if (absdir.x >= absdir.y && absdir.x >= absdir.z)
    {
        slice = dir.x >= 0.0 ? 0 : 1;
        uvs = vec2(-dir.z / dir.x, -dir.y / absdir.x);
    }
    else if (absdir.y >= absdir.z)
    {
        slice = dir.y >= 0.0 ? 2 : 3;
        uvs = vec2(dir.x / absdir.y, dir.z / dir.y);
    }
    else
    {
        slice = dir.z >= 0.0 ? 4 : 5;
        uvs = vec2(dir.x / dir.z, -dir.y / absdir.z);
    }

         uvs               = uvs * 0.5 + 0.5;
         vec4 scale_offset = GetCookieScaleOffset(cookieIdx + slice);
         ClampCookieUVs(uvs, scale_offset.xy);
    vec3 cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;

    return cookieAttenuation;
}

bool CalculatePointLightColor(int lightIdx, vec3 shadingNormal, vec3 shadingPosWorld, inout vec3 colorOut, out vec3 lightPosLocalOut, inout float maxTOut, OcclusionMode occlusionMode, bool useCookie)
{
    // Retrieve the range and position from the light properties
    float lightRange = Lights.LightProperties0[lightIdx].x;
    vec3 lightPosWorld = Lights.LightPositions[lightIdx];

    // Calculate the position and distance of the light relative to the shading point
    vec3 lightPosLocal = lightPosWorld - shadingPosWorld;
    float lightDist = length(lightPosLocal);

    // If the shading point is outside the range of the light, we're done
    if (lightDist >= lightRange) { return false; }

    maxTOut = lightDist;

    // Distance is ~0. Just sample the falloff and we're done.
    if (lightDist < PVR_FLT_EPSILON)
    {
        colorOut *= SampleFalloff(Lights.LightFalloffIndex[lightIdx], 0.0);
        lightPosLocalOut = vec3(0.0, 0.0, 0.0);
    }
    else
    {
        // Normalise the local light position (needed by the directional accumulator)
        lightPosLocalOut = lightPosLocal / lightDist;

        // Evaluate the Lambertian BRDF and return if the light is below the horizon
        float cosTheta = IsNormalValid(shadingNormal) ? max(dot(shadingNormal, lightPosLocalOut), 0.0) : 1.0;
        if (cosTheta <= 0.0 || isnan(cosTheta)) { return false; }
        colorOut *= cosTheta;

        // Normalise the light distance to its range and evaluate the falloff
        float normLightDist = lightDist / lightRange;
        colorOut *= SampleFalloff(Lights.LightFalloffIndex[lightIdx], normLightDist);

        // If we're using cookies, evaluate them now
        if (useCookie)
        {
            colorOut *= sampleCookie(-lightPosLocal, lightIdx);
        }
    }

    // If we're using shadowmasks, accumulate them here. CalculateSpotLightColor also uses this function, so test the type.
    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK && GetLightType(lightIdx) == LIGHT_POINT)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    return true;
}


//Angle attenuation code from HDRP CommonLighting.hlsl
// Square the result to smoothen the function.
float HDRPSmoothAngleAttenuation(float cosFwd, float lightAngleScale, float lightAngleOffset)
{
   float attenuation = clamp(cosFwd * lightAngleScale + lightAngleOffset, 0.0, 1.0);
   return attenuation * attenuation;
}

bool CalculateSpotLightColor(int lightIdx, vec3 normal, vec3 position, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    if( CalculatePointLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode, false) == false )
      return false;

    float cosConeAng = Lights.LightProperties0[lightIdx].y;
    float invCosConeAng = 1.0 - cosConeAng;
    float cosInnerAngle =  Lights.LightProperties0[lightIdx].z;
    float dval = dot(lightVecOut, Lights.LightDirections[lightIdx]);

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec3 projected     = Lights.LightDirections[lightIdx] - lightVecOut / dval;
        vec3 lightBitan_NZ = cross(Lights.LightDirections[lightIdx], Lights.LightTangents[lightIdx]);
        float scale = cosConeAng / sqrt(1.0 - cosConeAng * cosConeAng);
        vec2 uvs = vec2(dot(projected, scale * lightBitan_NZ),
                        dot(projected, scale * Lights.LightTangents[lightIdx]));

        if (abs(uvs.x) > 1.0 || abs(uvs.y) > 1.0)
            return false;

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    //uses a LUT table to compute angular falloff to match legacy which uses a texture to compute angular faloff
    int angularFalloffType = int(Lights.LightProperties1[lightIdx].y);

    float angleLimit = (angularFalloffType != ANGULARFALLOFFTYPE_LUT || cookieIdx < 0) ? cosConeAng : 0.0;
    if (dval < angleLimit)
        return false;

    colorOut *= cookieAttenuation;

    if(angularFalloffType == ANGULARFALLOFFTYPE_LUT)
    {
        // builtin cookies completely control angular falloff
        if (cookieIdx < 0)
        {
            float angScale = (dval-cosConeAng)/invCosConeAng;
            float angFalloff = 1.0 - LookupAngularFalloffLUT (angScale);
            colorOut *= angFalloff;
        }
    }
    else//inner angle support AND Analyticfalloff
    {
        //There is no attenuation inside inner angle
        if(dval >= cosInnerAngle )
        {
            //nothing to do here
        }
        else//Otherwise match HDRP analytic formula for attenuation
        {
            float angleScale = 1.0 / max(0.0001, (cosInnerAngle - cosConeAng));
            float lightAngleOffset = -cosConeAng * angleScale;

            float angFalloff = HDRPSmoothAngleAttenuation(dval, angleScale, lightAngleOffset);
            colorOut *= angFalloff;
        }
    }

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }
    return true;
}

void CreateOrthoNormalBasis(vec3 n, out vec3 tangent, out vec3 bitangent);

#define PI 3.14159265359

vec3 SphQuadSample(vec3 s, vec3 ex, vec3 ey, vec3 o, float u, float v, inout float solidAngle)
{
    float exl = length(ex);
    float eyl = length(ey);
    // compute local reference system 'R'
    vec3 x = ex / exl;
    vec3 y = ey / eyl;
    vec3 z = cross(x, y);
    // compute rectangle coords in local reference system
    vec3 d = s - o;
    float z0 = dot(d, z);
    // flip 'z' to make it point against 'Q'
    if (z0 > 0.0) {
        z *= -1.0;
        z0 *= -1.0;
    }
    float z0sq = z0 * z0;
    float x0 = dot(d, x);
    float y0 = dot(d, y);
    float x1 = x0 + exl;
    float y1 = y0 + eyl;
    float y0sq = y0 * y0;
    float y1sq = y1 * y1;
    // create vectors to four vertices
    vec3 v00 = vec3(x0, y0, z0);
    vec3 v01 = vec3(x0, y1, z0);
    vec3 v10 = vec3(x1, y0, z0);
    vec3 v11 = vec3(x1, y1, z0);
    // compute normals to edges
    vec3 n0 = normalize(cross(v00, v10));
    vec3 n1 = normalize(cross(v10, v11));
    vec3 n2 = normalize(cross(v11, v01));
    vec3 n3 = normalize(cross(v01, v00));
    // compute internal angles (gamma_i)
    float g0 = acos(-dot(n0,n1));
    float g1 = acos(-dot(n1,n2));
    float g2 = acos(-dot(n2,n3));
    float g3 = acos(-dot(n3,n0));
    // compute predefined constants
    float b0 = n0.z;
    float b1 = n2.z;
    float b0sq = b0 * b0;
    float k = 2.0*PI - g2 - g3;
    // compute solid angle from internal angles
    float S = g0 + g1 - k;
    solidAngle = S;

    // 1. compute 'cu'
    float au = u * S + k;
    float fu = (cos(au) * b0 - b1) / sin(au);
    float cu = 1.0/sqrt(fu*fu + b0sq) * (fu>0.0 ? 1.0 : -1.0);
    cu = clamp(cu, -1.0, 1.0); // avoid NaNs
    // 2. compute 'xu'
    float xu = -(cu * z0) / sqrt(1.0 - cu*cu);
    xu = clamp(xu, x0, x1); // avoid Infs
    // 3. compute 'yv'
    float d_ = sqrt(xu*xu + z0sq);
    float h0 = y0 / sqrt(d_*d_ + y0sq);
    float h1 = y1 / sqrt(d_*d_ + y1sq);
    float hv = h0 + v * (h1-h0), hv2 = hv*hv;
    float eps = 0.0001;
    float yv = (hv2 < 1.0-eps) ? (hv*d_)/sqrt(1.0-hv2) : y1;

    // 4. transform (xu,yv,z0) to world coords
    return (o + xu*x + yv*y + z0*z);
}

bool SphQuadSampleDir(vec3 s, vec3 ex, vec3 ey, vec3 o, vec2 sq, out float solidAngle, out vec3 rayDir, out float rayMaxT)
{
    rayDir = SphQuadSample(s, ex, ey, o, sq.x, sq.y, solidAngle) - o;
    rayMaxT = length(rayDir);
    rayDir /= rayMaxT;

    return !isnan(solidAngle) && solidAngle > 0.0 && rayMaxT >= PVR_FLT_EPSILON;
}

//Do the lighting calculation for the provided position+normal
bool CalculateAreaLightColor(int lightIdx, vec3 normal, vec3 position, vec2 rnd, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    vec3 lightDir = normalize(Lights.LightDirections[lightIdx]);
    vec3 lightCenter = Lights.LightPositions[lightIdx];
    vec3 texelToLight = position-lightCenter;

    // light backfacing?
    if(dot(lightDir, texelToLight) > 0.0)
        return false;

    // range check
    float range = Lights.LightProperties0[lightIdx].x;
    float ttlDistSq = dot( texelToLight, texelToLight );
    if (ttlDistSq > (range*range))
        return false;

    float width = Lights.LightProperties1[lightIdx].y;
    float height = Lights.LightProperties1[lightIdx].z;

    // solid angle sampling
    vec3 lightTan = normalize(Lights.LightTangents[lightIdx]);
    vec3 lightBitan = cross(lightDir,lightTan);
    vec3 s = lightCenter - 0.5 * width * lightBitan- 0.5 * height * lightTan;

    float solidAngle, tempMaxTout;
    vec3 templightVecOut;
    if(!SphQuadSampleDir(s, lightTan * height, lightBitan * width, position, rnd, solidAngle, templightVecOut, tempMaxTout))
        return false;

    lightVecOut= templightVecOut;
    maxTOut = tempMaxTout;

    // evaluation (Note: we should  not do the division by width * height here)
    bool hasNormal = (normal != vec3(0.0));  // probes do not supply normals to this calculation
    float nDotL =  hasNormal ? max(0.0, dot(lightVecOut, normal)) : 1.0;

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec3 edge = position + lightVecOut * maxTOut - lightCenter;
        vec2 uvs = vec2(2.0 * dot(edge, -lightBitan) / width, 2.0 * dot(edge, lightTan) / height);

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    // Only accumulate samples when the sample on the rectangle is visible from this texel.
    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK && nDotL > 0.0)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    colorOut *= solidAngle * nDotL / PI * cookieAttenuation;
    return true;
}

//Do the lighting calculation for the provided position+normal
bool CalculateDiscLightColor(int lightIdx, vec3 normal, vec3 position, vec2 rnd, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    // check for early out
    vec3 lightDir = -normalize(Lights.LightDirections[lightIdx]); // we negate to undo negation in Wintermute/Scene.cpp
    vec3 lightCenter = Lights.LightPositions[lightIdx];
    vec3 texelToLight = position - lightCenter;  // account for area light size?

    // light backfacing?
    if(dot(lightDir, texelToLight) < 0.0)
        return false;

    // range check
    float range = Lights.LightProperties0[lightIdx].x;
    float ttlDistSq = dot( texelToLight, texelToLight );
    if (ttlDistSq > (range*range))
        return false;

    // Sample uniformly on 2d disc area
    float radius = Lights.LightProperties1[lightIdx].y;

    float rLocal = sqrt(rnd.x);
    float thetaLocal = 2.0 * PI * rnd.y;
    vec2 samplePointLocal = vec2(cos(thetaLocal), sin(thetaLocal)) * rLocal * radius;

    // Convert sample point to world space
    vec3 lightTan = normalize(Lights.LightTangents[lightIdx]);
    vec3 lineCross = cross(lightDir, lightTan);
    vec3 samplePointWorld = lightCenter + samplePointLocal.x * lightTan + samplePointLocal.y * lineCross;

    // Calc contribution etc.
    lightVecOut = samplePointWorld - position;
    maxTOut = length(lightVecOut);
    if (maxTOut < PVR_FLT_EPSILON)
        return false;

    lightVecOut /= maxTOut;
    bool hasNormal = (normal != vec3(0.0)); // probes do not supply normals to this calculation
    float nDotL = hasNormal ? max(0.0, dot(lightVecOut, normal)) : 1.0;

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        float scale = 1.0 / radius;
        vec3  edge  = position + lightVecOut * maxTOut - lightCenter;
        vec2  uvs   = vec2(dot(edge, lineCross) * scale, dot(edge, lightTan) * scale);

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    // Only accumulate samples when the sample on the rectangle is visible from this texel.
    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK && nDotL > 0.0)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    // * (Pi / Pi) removed from the expression below as it cancels out.
    colorOut *= cookieAttenuation * (nDotL * radius * radius * dot(lightDir, -lightVecOut)) / (maxTOut * maxTOut);
    return true;
}

bool CalculateBoxLightColor(int lightIdx, vec3 normal, vec3 position, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    bool hasNormal  = IsNormalValid(normal);
    vec3 toLight_NZ = Lights.LightDirections[lightIdx];
    vec3 lightPos   = Lights.LightPositions[lightIdx];

    float dotVal = hasNormal ? dot(normal, toLight_NZ) : 1.0;
    if (dotVal <= 0.0 || isnan(dotVal))
        return false;

    // check if the shading point is within range and in front
    float range         = Lights.LightProperties0[lightIdx].x;
    float projectedDist = dot(toLight_NZ, lightPos - position);
    if (projectedDist < 0.0 || projectedDist > range)
        return false;
    // check if the shading point is contained within the box
    vec3  lightTan_NZ   = Lights.LightTangents[lightIdx];
    vec3  lightBitan_NZ = cross(toLight_NZ, lightTan_NZ);
    vec3  pos2       = lightPos - toLight_NZ * projectedDist;
    vec3  edge       = position - pos2;
    float width      = abs(2.0 * dot(edge, lightBitan_NZ));
    float height     = abs(2.0 * dot(edge, lightTan_NZ));
    float box_width  = Lights.LightProperties1[lightIdx].y;
    float box_height = Lights.LightProperties1[lightIdx].z;
    if (width > box_width || height > box_height)
        return false;

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec2 uvs = vec2(2.0 * dot(edge, lightBitan_NZ) / box_width, 2.0 * dot(edge, lightTan_NZ) / box_height);

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    // box lights behave like directional lights, so we only
    // attenuate by the surface normal and light direction
    lightVecOut = toLight_NZ;
    colorOut   *= dotVal * cookieAttenuation;
    maxTOut     = projectedDist;

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    return true;
}

bool CalculatePyramidLightColor(int lightIdx, vec3 normal, vec3 position, inout vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    bool  hasNormal   = IsNormalValid(normal);
    vec3  lightPos    = Lights.LightPositions[lightIdx];
    vec3  lightDir_NZ = -Lights.LightDirections[lightIdx]; // we're actually passing in transform z of a right handed coordinate system, so flip the dir
    vec3  toLight     = lightPos - position;
    float range       = Lights.LightProperties0[lightIdx].x;

    float dist       = length(toLight);
    vec3  toLight_NZ = toLight / dist;

    // out of range or wrong side?
    float dotVal = hasNormal ? dot(normal, toLight_NZ) : 1.0;
    if (dist > range || dotVal <= 0.0 || isnan(dotVal))
        return false;

    float projectedDist = dot(-toLight, lightDir_NZ);
    if (projectedDist <= 0.0)
        return false;

    // check if the shading point is contained within the pyramid
    vec3  pos2 = lightPos + lightDir_NZ;
    vec3  pos3 = lightPos - (toLight / projectedDist);
    vec3  edge =  pos3 - pos2;

    vec3  lightTan_NZ    = Lights.LightTangents[lightIdx];
    vec3  lightBitan_NZ  = cross(lightTan_NZ, lightDir_NZ);
    float width          = abs(2.0 * dot(edge, lightBitan_NZ));
    float height         = abs(2.0 * dot(edge, lightTan_NZ));
    float pyramid_width  = Lights.LightProperties1[lightIdx].y;
    float pyramid_height = Lights.LightProperties1[lightIdx].z;

    if (width > pyramid_width || height > pyramid_height)
        return false;

    // process attenuation based on distance and angle
    float distFalloff = SampleFalloff(Lights.LightFalloffIndex[lightIdx], dist / range);

    // handle cookies
    int cookieIdx = LightCookieInfo.LightToScaleOffset[lightIdx];
    vec3 cookieAttenuation = vec3(1.0, 1.0, 1.0);

    if (cookieIdx >= 0)
    {
        vec2 uvs = vec2(2.0 * dot(edge, lightBitan_NZ) / pyramid_width, 2.0 * dot(edge, lightTan_NZ) / pyramid_height);

        uvs = uvs * 0.5 + 0.5;
        vec4 scale_offset = GetCookieScaleOffset(cookieIdx);
        ClampCookieUVs(uvs, scale_offset.xy);

        cookieAttenuation = texture2D(LightCookies, uvs * scale_offset.xy + scale_offset.zw).rgb;
    }

    // initialize outputs
    colorOut   *= distFalloff * dotVal * cookieAttenuation;
    lightVecOut = toLight_NZ;
    maxTOut     = dist;

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        int shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        vec4 samples = vec4(0.0, 0.0, 0.0, 0.0);
        samples[shadowMaskChannel] = 1.0;
        accumulate(SHADOW_MASK_SAMPLE_BUFFER, samples);
    }

    return true;
}


//Do the lighting calculation for the provided position+normal
bool CalculateLightColor(int lightIdx, vec3 normal, vec3 position, bool bounce, vec2 rnd, out vec3 colorOut, out vec3 lightVecOut, inout float maxTOut, OcclusionMode occlusionMode)
{
    if (bounce)
        colorOut = Lights.LightProperties2[lightIdx].xyz;
    else
        colorOut = Lights.LightColors[lightIdx].xyz;

    int lightType = GetLightType(lightIdx);

    if (lightType == LIGHT_DIRECTIONAL)
    {
        return CalculateDirectionalLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_POINT)
    {
        return CalculatePointLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode, true);
    }
    else if (lightType == LIGHT_SPOT)
    {
        return CalculateSpotLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_AREA)
    {
        return CalculateAreaLightColor(lightIdx, normal, position, rnd, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_DISC)
    {
        return CalculateDiscLightColor(lightIdx, normal, position, rnd, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_BOX)
    {
        return CalculateBoxLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode);
    }
    else if (lightType == LIGHT_PYRAMID)
    {
        return CalculatePyramidLightColor(lightIdx, normal, position, colorOut, lightVecOut, maxTOut, occlusionMode);
    }

    return false;
}

vec3 PointInCosLobe(vec2 uv)
{
    float theta = acos(sqrt(1. - uv.x));
    float phi   = 2. * 3.14159 * uv.y;
    return vec3(cos(phi) * sin(theta), sin(phi) * sin(theta), cos(theta));
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Lighting.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\LodMissShader.rlsl---------------
.
.


void setup()
{
    rl_OutputRayCount[PVR_RAY_CLASS_GI   ] = 0;
    rl_OutputRayCount[PVR_RAY_CLASS_OCC  ] = 0;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_1] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_2] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_3] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_4] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_5] = 1;
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_6] = 1;
}


void main()
{
    ReshootLodRay(rl_InRay.lodOrigin, rl_InRay.originalT, rl_InRay.depth, rl_InRay.rayType);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\LodMissShader.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\OpenRLCPPSharedIncludes.rlsl---------------
.
.
#pragma once

#if defined(UNITY_EDITOR)
#include "External/Wintermute/Vector.h"
#define SHARED_int3_type Wintermute::Vec3i
#define SHARED_CONST const
#define SHARED_INLINE inline
#else
#define SHARED_int3_type ivec3
#define SHARED_CONST in
#define SHARED_INLINE
#endif // UNITY_EDITOR

SHARED_INLINE int OpenRLCPPShared_GetRegionIdx(SHARED_CONST SHARED_int3_type loc, SHARED_CONST SHARED_int3_type gridDims)
{
    if (loc.x < 0 || loc.y < 0 || loc.z < 0 || loc.x >= gridDims.x || loc.y >= gridDims.y || loc.z >= gridDims.z)
        return -1;

    return loc.x + loc.y * gridDims.x + loc.z * gridDims.x * gridDims.y;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\OpenRLCPPSharedIncludes.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\ProbeBakeFrameShader.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniform sampler2D PositionsTex;
uniform sampler2D ProbeLightIndicesTexture;
uniform int OutputRayCount;
uniform int PassIdx;
uniform int GIMaxSamples;
uniform int GISamplesPerPass;
uniform int GISamplesSoFar;
uniform int DirectMaxSamples;
uniform int DirectSamplesPerPass;
uniform int DirectSamplesSoFar;
uniform int EnvironmentMaxSamples;
uniform int EnvironmentSamplesPerPass;
uniform int EnvironmentSamplesSoFar;
uniform int IgnoreDirectEnvironment;
uniform int DoDirect;

void setup()
{
    rl_OutputRayCount = OutputRayCount;
}

void ProbeSampling(vec3 pos, int samplesForPass, int samplesSoFar, int scramble)
{
    int sampleIndex = samplesSoFar;
    float weight = 4.0/float(GIMaxSamples);

    vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);

    for(int i = 0; i < samplesForPass; ++i, ++sampleIndex)
    {
        vec2 rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift.xy);
        vec3 direction = SphereSample(rnd);

        createRay();
        rl_OutRay.origin           = pos;
        rl_OutRay.direction        = direction;
        rl_OutRay.color            = vec4(0.0); // unused, because we're not shooting against lights
        rl_OutRay.probeDir         = normalize(direction);
        rl_OutRay.defaultPrimitive = GetEnvPrimitive();
        rl_OutRay.renderTarget     = PROBE_BUFFER;
        rl_OutRay.isOutgoing       = true;
        rl_OutRay.sampleIndex      = sampleIndex;
        rl_OutRay.rayType          = PVR_RAY_TYPE_GI;
        rl_OutRay.rayClass         = PVR_RAY_CLASS_GI;
        rl_OutRay.depth            = 0;
        rl_OutRay.weight           = weight;
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.albedo           = vec3(1.0);
        rl_OutRay.sameOriginCount  = 0;
        rl_OutRay.transmissionDepth= 0;
        rl_OutRay.lightmapMode     = LIGHTMAPMODE_NONDIRECTIONAL; // Not used with probe sampling.
        rl_OutRay.lodParam         = 0;
        rl_OutRay.lodOrigin        = rl_OutRay.origin;
        rl_OutRay.originalT        = rl_OutRay.maxT;
        emitRayWithoutDifferentials();
    }
}

void EnvironmentSampling(vec3 pos, int samplesForPass, int samplesSoFar)
{
    int   sampleIndex = samplesSoFar;
    float weight = 1.0/float(EnvironmentMaxSamples);

    vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);

    if (UseEnvironmentImportanceSampling())
    {
        for (int i = 0; i < samplesForPass; ++i, ++sampleIndex)
        {
            vec3 rand = fract(vec3(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0), SobolSample(sampleIndex, 2, 0)) + cpShift);
            VolumeSampleEnvironmentIS(PROBE_BUFFER, pos, vec3(0.0), vec3(1.0), rand, weight, 0, 0, false);
        }
    }
    else
    {
        for (int i = 0; i < samplesForPass; ++i, ++sampleIndex)
        {
            vec2 rand = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift.xy);
            VolumeSampleEnvironment(PROBE_BUFFER, pos, vec3(0.0), vec3(1.0), rand, weight, 0, 0, false);
        }
    }
}

void main()
{
    vec2  frameCoord  = rl_FrameCoord.xy / rl_FrameSize.xy;

    vec4 posTex = texture2D(PositionsTex, frameCoord);

    if(posTex.w <= 0.0)
        return;

    int scramble = GetScreenCoordHash(rl_FrameCoord.xy);

    if (DoDirect == 0)
    {
        if (GISamplesSoFar < GIMaxSamples)
        {
            int clampedGIsamplesPerPass = min (max(0, GIMaxSamples - GISamplesSoFar), GISamplesPerPass);
            ProbeSampling(posTex.xyz, clampedGIsamplesPerPass, GISamplesSoFar, scramble);
        }

        if (IgnoreDirectEnvironment == 0 && EnvironmentSamplesSoFar < EnvironmentMaxSamples && SampleDirectEnvironment())
        {
            int clampedEnvSamplesPerPass = min(max(0, EnvironmentMaxSamples - EnvironmentSamplesSoFar), EnvironmentSamplesPerPass);
            EnvironmentSampling(posTex.xyz, clampedEnvSamplesPerPass, EnvironmentSamplesSoFar);
        }
    }
    else
    {
        // Direct and probe occlusion are done in the first interation.
        if (DirectSamplesSoFar < DirectMaxSamples)
        {
            vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), 0);
            int clampedDirectsamplesPerPass = min (max(0, DirectMaxSamples - DirectSamplesSoFar), DirectSamplesPerPass);

            int sampleIndex = DirectSamplesSoFar;
            for (int i = 0; i < DirectSamplesPerPass; ++i, ++sampleIndex)
            {
                float weight = 1.0/float(DirectMaxSamples);
                vec2 rnd = fract(vec2(SobolSample(sampleIndex, 0, 0), SobolSample(sampleIndex, 1, 0)) + cpShift.xy);

                // Direct
                DoShadows(false, posTex.xyz, vec3(0.0), vec3(1.0), PROBE_BUFFER, rnd.xyy, vec3(0.0), LIGHTMAPMODE_NOTUSED, 0, PVR_FLT_MAX, 0, OCCLUSIONMODE_DIRECT, vec4(-1.0), weight, true);

                // Probe Occlusion
                vec4 lightIndices = texture2D(ProbeLightIndicesTexture, frameCoord);
                DoShadows(false, posTex.xyz, vec3(0.0), vec3(1.0), PROBE_OCCLUSION_BUFFER, rnd.xyy, vec3(0.0), LIGHTMAPMODE_NOTUSED, 0, PVR_FLT_MAX, 0, OCCLUSIONMODE_PROBEOCCLUSION, lightIndices, weight, true);
            }
        }
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\ProbeBakeFrameShader.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\RayAttributes.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

#define KPI                 3.14159265358979323846264338327950
#define KTWOPI              6.28318530717958646
#define KHALFPI             1.570796326794897
#define KQUARTERPI          0.7853981633974483


#define MAX_ANGULAR_FALLOFF_TABLE_LENGTH 128 // Needs to be kept in sync with PVRJobLoadShaders.cpp::gMaxAngularFalloffTableLength

// Keep in sync with wmLight.h
#define LIGHT_SPOT        0
#define LIGHT_DIRECTIONAL 1
#define LIGHT_POINT       2
#define LIGHT_AREA        3
#define LIGHT_DISC        4
#define LIGHT_PYRAMID     5
#define LIGHT_BOX         6

#define NO_SHADOW 0.0
#define HARD_SHADOW 1.0
#define SOFT_SHADOW 2.0


// FBO attachment index
// (can overlap between lightmaps and light probes)
// Lightmaps
// Same as unique buffer names above
// Light probes
#define PROBE_BUFFER_INDEX 0                // accumulateSH uses buffers [0;SHNUMCOEFFICIENTS-1]
#define PROBE_OCCLUSION_BUFFER_INDEX 9
// Custom bake
#define CUSTOM_BAKE_BUFFER_INDEX 0

int GetFBOAttachmentIndex(int target)
{
    if (target == PROBE_BUFFER)
        return PROBE_BUFFER_INDEX;
    else if (target == PROBE_OCCLUSION_BUFFER)
        return PROBE_OCCLUSION_BUFFER_INDEX;
    else if (target == CUSTOM_BAKE_BUFFER)
        return CUSTOM_BAKE_BUFFER_INDEX;

    // target < PROBE_BUFFER
    return target;
}

#define OutputType int
#define OUTPUTTYPE_LIGHTMAP     0
#define OUTPUTTYPE_LIGHTPROBES  1

OutputType GetOutputType(int target)
{
    if (target < PROBE_BUFFER)
        return OUTPUTTYPE_LIGHTMAP;
    else
        return OUTPUTTYPE_LIGHTPROBES;
}

#define SOBOL_MATRIX_SIZE 53248 // needs to be kept in sync with "External/qmc/SobolData.h [1024*52]"


#define LightmapMode int
#define LIGHTMAPMODE_NOTUSED        -1
#define LIGHTMAPMODE_NONDIRECTIONAL 0
#define LIGHTMAPMODE_DIRECTIONAL    1


rayattribute vec4 color;
rayattribute int renderTarget;
rayattribute float weight;
rayattribute int sampleIndex;
rayattribute vec3 probeDir;         // Used both for directionality and light probes.
rayattribute vec3 albedo;
rayattribute int sameOriginCount;
rayattribute LightmapMode lightmapMode;
rayattribute int transmissionDepth;
rayattribute int lodParam;
rayattribute int rayType;
rayattribute vec3 lodOrigin;
rayattribute float originalT;
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\RayAttributes.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Shadows.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

float sqr(float x) { return x*x; }

void EmitShadowRay(vec3, vec3, vec3, vec4, float, float, LightmapMode, int);

vec3 CalculateJitteredLightVec(int lightIdx, vec3 lightVec, vec3 position, float maxT, vec2 rnd)
{
    int lightType = GetLightType(lightIdx);

    if (lightType == LIGHT_AREA || lightType == LIGHT_DISC)
        return lightVec;

    float lightDist = maxT;
    if(lightDist == 1e27)
        lightDist = length(GetLightPosition(lightIdx) - position);

    if(lightType == LIGHT_DIRECTIONAL)
        lightDist = 1.0;

    vec3 lightOffset = lightVec * lightDist;

    float shadowRadius = GetLightProperties1(lightIdx).x;

    vec3 b1;
    vec3 b2;

    CreateOrthoNormalBasis(lightVec, b1, b2);
    mat3 lightBasis = mat3(b1.x, b1.y, b1.z, b2.x, b2.y, b2.z, lightVec.x, lightVec.y, lightVec.z);

    return GetJitteredLightVec(shadowRadius, rnd, lightOffset, lightBasis);
}

bool DoShadowsForLight(int lightIdx, vec3 position, vec3 normal, vec3 diffuse, int target, vec2 rnd, vec3 probeDir, LightmapMode lightmapMode, int lodParam, float lodT, bool bounce, OcclusionMode occlusionMode, vec4 lightIndices, float weight, bool receiveShadows)
{
    if (occlusionMode == OCCLUSIONMODE_DIRECT)
    {
        if (GetOutputType(target) == OUTPUTTYPE_LIGHTPROBES)
        {
            if (!GetLightProbesDoDirect(lightIdx))
                return false;
        }
        else // OUTPUTTYPE_LIGHTMAP
        {
            if (!GetLightmapsDoDirect(lightIdx))
                return false;
        }
    }

    int shadowMaskChannel;
    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        shadowMaskChannel = GetShadowMaskChannel(lightIdx);
        if (shadowMaskChannel < 0 || shadowMaskChannel > 3)
            return false;
    }

    int probeOcclusionChannel = -1;
    if (occlusionMode == OCCLUSIONMODE_PROBEOCCLUSION)
    {
        // Check if this probe wants to calculate occlusion for this light.
        if (int(lightIndices.x) == lightIdx)
            probeOcclusionChannel = 0;
        else if (int(lightIndices.y) == lightIdx)
            probeOcclusionChannel = 1;
        else if (int(lightIndices.z) == lightIdx)
            probeOcclusionChannel = 2;
        else if (int(lightIndices.w) == lightIdx)
            probeOcclusionChannel = 3;

        if (probeOcclusionChannel == -1)
            return false;
    }

    vec3 lightColor;
    vec3 lightVec;
    float maxT;
    float shadowType = GetShadowType(lightIdx);
    bool hasHardShadows = shadowType == HARD_SHADOW;

    if (!CalculateLightColor(lightIdx, normal, position, bounce, rnd, lightColor, lightVec, maxT, occlusionMode))
        return false;

    vec4 lambdiffuse = vec4(lightColor.rgb * diffuse, 0.0);

    if (occlusionMode == OCCLUSIONMODE_SHADOWMASK)
    {
        lambdiffuse = vec4(0.0, 0.0, 0.0, 0.0);
        lambdiffuse[shadowMaskChannel] = 1.0;
    }

    if (occlusionMode == OCCLUSIONMODE_PROBEOCCLUSION)
    {
        lambdiffuse = vec4(0.0, 0.0, 0.0, 0.0);
        lambdiffuse[probeOcclusionChannel] = 1.0;
    }

    bool hasProbeDir = probeDir != vec3(0.0);

    if (shadowType == NO_SHADOW || !receiveShadows || maxT < PVR_FLT_EPSILON)
    {
        vec3 probeDirOut = probeDir;
        if (!hasProbeDir)
            probeDirOut = lightVec;

        Accumulate(target, lambdiffuse * weight, probeDirOut, lightmapMode);
    }
    else
    {
        lightVec = CalculateJitteredLightVec(lightIdx, lightVec, position, maxT, rnd);

        vec3 probeDirOut = probeDir;
        if (!hasProbeDir)
            probeDirOut = lightVec;

        EmitShadowRay(position, lightVec, probeDirOut, lambdiffuse, maxT, weight, lightmapMode, lodParam, lodT, target);
    }

    return true;
}

bool DoShadowsForRegion(bool usePowerSampling, int regionIdx, vec3 position, vec3 normal, vec3 diffuse, int target, vec3 rnd, vec3 probeDir, LightmapMode lightmapMode, int lodParam, float lodT, int bounce, OcclusionMode occlusionMode, vec4 lightIndices, float weight, bool receiveShadows)
{
    bool didLight = false;
    // always test directional lights
    for (int i = 0, cnt = GetMaxDirLights(); i < cnt; ++i)
    {
        if (DoShadowsForLight(i, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, weight, receiveShadows))
            didLight = true;
    }

    if (regionIdx >= Lights.GridLength || regionIdx < 0)
        return didLight;

    int numLights = GetNumLights(regionIdx);

    if(numLights <= 0)
        return didLight;


    int  maxShadowRays = max(PVR_MAX_SHADOW_RAYS - bounce + 1, 1);

    if (numLights > maxShadowRays && usePowerSampling)
    {
        float origWeight = weight / float(maxShadowRays);
        float xi = rnd.z;
        for (int i = 0; i < maxShadowRays; ++i)
        {
            float rayWeight = origWeight;
            int lightIdx = PickLight(regionIdx, xi, rayWeight);
            if (lightIdx >= 0)
            {
                if (DoShadowsForLight(lightIdx, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, rayWeight, receiveShadows))
                    didLight = true;
            }
        }
    }
    else
    {
        for (int i = 0; i < numLights; ++i)
        {
            int lightIdx = GetLightIndex(regionIdx, i);
            if (DoShadowsForLight(lightIdx, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, weight, receiveShadows))
                didLight = true;
        }
    }

    return didLight;
}

bool DoShadows(bool usePowerSampling, vec3 surfPosition, vec3 surfNormal, vec3 surfDiffuseColor, int target, vec3 rnd, vec3 probeDir, LightmapMode lightmapMode, int lodParam, float lodT, int bounce, OcclusionMode occlusionMode, vec4 lightIndices, float weight, bool receiveShadows)
{
    bool didLight = DoShadowsForRegion(usePowerSampling, GetRegionContaining(surfPosition), surfPosition, surfNormal, surfDiffuseColor, target, rnd, probeDir, lightmapMode, lodParam, lodT, bounce, occlusionMode, lightIndices, weight, receiveShadows);
    return didLight;
}

int DoShadows(int startIndex, int count, vec3 position, vec3 normal, vec3 diffuse, int target, vec3 rnd, vec3 probeDir, LightmapMode lightmapMode, int lodParam, float lodT, int bounce, OcclusionMode occlusionMode, vec4 lightIndices, float weight, bool receiveShadows)
{
    bool didLight;
    int maxDirLights = GetMaxDirLights();
    int remainingDir = min(max( 0, maxDirLights - startIndex), count);
    for( int cnt = startIndex + remainingDir; startIndex < cnt; startIndex++, count-- )
    {
        if (DoShadowsForLight(startIndex, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, weight, receiveShadows))
            didLight = true;
    }
    startIndex -= maxDirLights;

    // any directional lights left?
    if (startIndex < 0)
        return 1;

    // any non-directional lights?
    int regionIdx = GetRegionContaining(position);
    if (regionIdx >= Lights.GridLength || regionIdx < 0)
        return 0;

    int numLights = GetNumLights(regionIdx);
    if (startIndex >= numLights)
        return 0;


    int remaining = min(numLights - startIndex, count);
    for( int cnt = startIndex + remaining; startIndex < cnt; startIndex++ )
    {
        int lightIdx = GetLightIndex(regionIdx, startIndex);
        if (DoShadowsForLight(lightIdx, position, normal, diffuse, target, rnd.xy, probeDir, lightmapMode, lodParam, lodT, bounce > 0, occlusionMode, lightIndices, weight, receiveShadows))
            didLight = true;
    }

    return numLights - startIndex;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Shadows.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\ShadowSampling.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

vec3 GetJitteredLightVec(float shadowRadius, vec2 rnd, vec3 lightOffset, mat3 lightBasis)
{
    vec2 diskSample = MapSquareToDisk (rnd);
    vec3 jitterOffset = vec3(shadowRadius * diskSample, 0.0);
    jitterOffset =  lightBasis * jitterOffset;;
    vec3 jitteredLightOffset = lightOffset + jitterOffset;
    return normalize(jitteredLightOffset);
}

void EmitShadowRay(vec3 origin, vec3 direction, vec3 probeDir, vec4 diffuse, float maxT, float weight, LightmapMode lightmapMode, int lodParam, float lodT, int target)
{
    createRay();
    rl_OutRay.origin              = origin;
    rl_OutRay.direction           = direction;
    rl_OutRay.color               = diffuse;
    rl_OutRay.probeDir            = probeDir;
    rl_OutRay.renderTarget        = target;
    rl_OutRay.isOutgoing          = true;       // ?
    rl_OutRay.sampleIndex         = 0;          // dummy, only used in the Standard.rlsl to decide on the next direction
    rl_OutRay.depth               = 0;
    rl_OutRay.weight              = weight;
    rl_OutRay.albedo              = vec3(1.0);
    rl_OutRay.maxT                = maxT;
    rl_OutRay.sameOriginCount     = 0;
    rl_OutRay.transmissionDepth   = 0;
    rl_OutRay.lightmapMode        = lightmapMode;
    rl_OutRay.lodParam            = lodParam;
    rl_OutRay.rayType             = PVR_RAY_TYPE_SHADOW;
    rl_OutRay.rayClass            = MapLodParamToRayClass(PVR_RAY_TYPE_SHADOW, lodParam);
    rl_OutRay.lodOrigin           = rl_OutRay.origin;
    rl_OutRay.originalT           = maxT;
    if (lodParam == 0)
    {
        rl_OutRay.occlusionTest    = true;
        rl_OutRay.defaultPrimitive = GetLightPrimitive();
    }
    else
    {
        rl_OutRay.maxT             = min(lodT, maxT);
        rl_OutRay.occlusionTest    = false;
        rl_OutRay.defaultPrimitive = GetLodMissPrimitive();
    }
    emitRayWithoutDifferentials();
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\ShadowSampling.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Standard.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniform sampler2D  Albedo;
uniform sampler2D  Emissive;
uniform sampler2D  Transmission;
uniform vec4       ST;
uniform vec4       TransmissionST;
uniform int        LightmapIndex;
uniform int        IsTransmissive;
uniform int        IsNegativelyScaled;
uniform int        IsDoubleSided;
uniform int        IsShadowCaster;
uniform int        LodParam;

#define MIN_INTERSECTION_DISTANCE 0.001
#define MIN_PUSHOFF_DISTANCE 0.0001 // Keep in sync with PLM_MIN_PUSHOFF

uniformblock PushOffInfo
{
    float pushOff;
};

void setup()
{
    // The output ray count for a given ray class is how many rays can be emitted by the shader when invoked by the ray class (i.e. the rl_InRay.rayClass)
    rl_OutputRayCount[PVR_RAY_CLASS_GI   ] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect(); // worst case: directional light shadow rays + 4 shadow rays + 1 GI ray + env rays
    rl_OutputRayCount[PVR_RAY_CLASS_OCC  ] = 1; // May potentially emit a ray due to transmission
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_1] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_2] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_3] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_4] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_5] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
    rl_OutputRayCount[PVR_RAY_CLASS_LOD_6] = GetMaxDirLights() + PVR_MAX_SHADOW_RAYS + 1 + GetRaysPerEnvironmentIndirect();
}

vec2 STTransform(vec2 uv)
{
    return (uv * ST.xy) + ST.zw;
}

vec2 TransmissionSTTransform(vec2 uv)
{
    return (uv * TransmissionST.xy) + TransmissionST.zw;
}

vec3 NextBounceDirection(vec2 rnd, vec3 normal)
{
    // next bounce
    vec3 hamDir = HemisphereCosineSample(rnd);

    vec3 b1;
    vec3 b2;
    CreateOrthoNormalBasis(normal, b1, b2);

    hamDir = hamDir.x*b1 + hamDir.y*b2 + hamDir.z*normal;

    return hamDir;
}

// This function should be used for ray generation where the depth attribute is not increased when generating a new ray
float GetAdjustedPushOff()
{
    return pow(2.0, float(rl_InRay.sameOriginCount)) * max(PushOffInfo.pushOff, MIN_PUSHOFF_DISTANCE);
}

void continueRay()
{
    // push off the ray and continue the path
    vec3  origin = vec3(rl_IntersectionPoint + (rl_InRay.direction * GetAdjustedPushOff()));
    float maxT   = max(0.0, rl_InRay.maxT - length(origin - rl_InRay.origin));
    // continue ray skipping this intersection
    createRay();
    rl_OutRay.origin = origin;
    rl_OutRay.maxT   = maxT;
    rl_OutRay.depth  = rl_InRay.depth;
    rl_OutRay.sameOriginCount = (rl_IntersectionT < MIN_INTERSECTION_DISTANCE) ? (rl_InRay.sameOriginCount + 1) : rl_InRay.sameOriginCount;
    emitRayWithoutDifferentials();
}

void main()
{
    int   lodParam        = rl_InRay.lodParam;
    bool  occlusionTest   = rl_InRay.rayType != PVR_RAY_TYPE_GI;
    int   sameOriginCount = rl_InRay.sameOriginCount;
    float maxT            = rl_InRay.maxT;

    // this ray was fired from a lod
    if (rl_InRay.lodParam != 0)
    {
        int rayGroupId, rayLodMask, instGroupId, instLodMask;
        bool shadowCaster = IsShadowCaster > 0;
        UnpackLodParam(rl_InRay.lodParam, rayGroupId, rayLodMask);
        UnpackLodParam(LodParam, instGroupId, instLodMask);

        bool occlusionRay  = rl_InRay.rayClass == PVR_RAY_CLASS_OCC;
        bool hitLod0       = (instLodMask & PVR_LOD_0_BIT) != 0;
        bool hitSameLod    = (instLodMask & rayLodMask) != 0;

        if (rayGroupId != instGroupId) // we hit a different lod group
        {
            if (!hitLod0) // we hit a higher lod of a different object, continue the ray
            {
                continueRay();
                return;
            }
            else if (occlusionTest && !occlusionRay && !shadowCaster)
            {
                // for lod rays we cannot terminate the path, as the object may not be a shadow caster. Continue the ray.
                continueRay();
                return;
            }

            lodParam = 0; // from now on treat this as a lod0 path
            sameOriginCount = 0;
            maxT = PVR_FLT_MAX;
        }
        else if(!hitSameLod ||                   // we hit an instance of our own lodgroup which is in a different lod layer, i.e. ray shot from lod1 hits lod0 from same group
               (occlusionTest && !shadowCaster)) // or this was an occlusion test, we hit an object in the same lod group but it's not flagged as a shadow caster
        {
            continueRay();
            return;
        }
        // self occlusion. As we bounce a lod ray again, we still need to use the max lod distance
        maxT = length(rl_InRay.origin - rl_InRay.lodOrigin) + rl_InRay.maxT;
    }

    // This is a workaround to avoid transparent hits getting stuck due to pushoff not working in very large scenes.
    if (rl_InRay.transmissionDepth > 100)
    {
        return;
    }

    // We draw a random number here for use in the sampling of translucency and we may reuse this random number
    // later we a diffuse interaction happens and the shader continues. Appropriate rescaling of the random
    // number is applied in this case.

    // Make sure we don't correlate due to reuse of dimensions, keep in sync with dimension picking below
    //matches GPU light mapper where increase the dimension each time we encounter a transmissive and we get the ray passing through it
    int   dim0 = rl_InRay.transmissionDepth + (rl_InRay.depth + 1) * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;

    float cpShift_transmission = GetCranleyPattersonRotation1D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), dim0);

    float rnd0 = fract(SobolSample(rl_InRay.sampleIndex, dim0, 0) + cpShift_transmission);

    // If a ray is intersecting a transmissive object (either from inside or outside)
    if (IsTransmissive > 0)
    {
        vec2 transmissionUV = TransmissionSTTransform(TexCoord0Varying.xy);
        vec4 transmission = texture2D(Transmission, transmissionUV);

        // NOTE: This is wrong! The probability of either reflecting or refracting a ray
        // should depend on the Fresnel of the material. However, since we do not support
        // any specularity in PVR there is currently no way to query this value, so for now
        // we use the transmission (texture) albedo.
        float probability = (transmission.x + transmission.y + transmission.z) / 3.0;
        if (probability > 0.0 && (rnd0 <= probability || occlusionTest))
        {
            createRay();
            rl_OutRay.direction = rl_InRay.direction;
            rl_OutRay.origin = vec3(rl_IntersectionPoint + (rl_InRay.direction * GetAdjustedPushOff()));

            rl_OutRay.color = rl_InRay.color;
            rl_OutRay.albedo = rl_InRay.albedo;

            if (occlusionTest)
                rl_OutRay.color *= vec4(transmission.xyz, 1.0);
            else
                rl_OutRay.albedo *= transmission.xyz;

            rl_OutRay.defaultPrimitive = rl_InRay.defaultPrimitive;
            rl_OutRay.depth = rl_InRay.depth;
            rl_OutRay.probeDir = rl_InRay.probeDir;
            rl_OutRay.renderTarget = rl_InRay.renderTarget;
            rl_OutRay.isOutgoing = rl_InRay.isOutgoing;
            rl_OutRay.sampleIndex = rl_InRay.sampleIndex;
            rl_OutRay.weight = rl_InRay.weight;
            rl_OutRay.maxT = max(0.0, rl_InRay.maxT - length(rl_OutRay.origin - rl_InRay.origin));
            rl_OutRay.sameOriginCount = (rl_IntersectionT < MIN_INTERSECTION_DISTANCE) ? sameOriginCount + 1 : sameOriginCount;
            rl_OutRay.transmissionDepth = rl_InRay.transmissionDepth + 1;
            rl_OutRay.lightmapMode = rl_InRay.lightmapMode;
            rl_OutRay.lodParam = lodParam;
            rl_OutRay.lodOrigin = rl_InRay.lodOrigin;
            rl_OutRay.originalT = rl_InRay.originalT;
            rl_OutRay.rayType = rl_InRay.rayType;
            if (lodParam == 0)
            {
                rl_OutRay.rayClass      = (rl_InRay.rayType == PVR_RAY_TYPE_GI || rl_InRay.rayType == PVR_RAY_TYPE_ENV) ? PVR_RAY_CLASS_GI : PVR_RAY_CLASS_OCC;
                rl_OutRay.occlusionTest = occlusionTest;
            }
            else
            {
                rl_OutRay.rayClass      = rl_InRay.rayClass;
                rl_OutRay.occlusionTest = rl_InRay.occlusionTest;
            }
            emitRayWithoutDifferentials();
            return;
        }
        // Rescale rnd to 0-1 range for reuse below for NEE (Realistic Ray Tracing by Peter Shirley).
        // Here we are guaranteed that rnd0 > probability and we want to rescale the rnd0 to fit into
        // the [0;1] range by "stretching" the interval [probability;1] and adjusting rnd0 accordingly.
        rnd0 = (rnd0 - probability) / (1.0 - probability);
    }

    // Shadow rays should not proceed beyond this point. Note that this shader is executed on intersections between occlusion rays and transmissive objects (RL_PRIMITIVE_IS_OCCLUDER set to false).
    if (occlusionTest)
        return;

    if(rl_IntersectionT > AOInfo.aoMaxDistance && rl_InRay.depth == 0 && (rl_InRay.renderTarget != PROBE_BUFFER))
        accumulate(AO_BUFFER, vec3(1.0,1.0,1.0));

    // check hit validity
    bool negativelyScaled = (IsNegativelyScaled > 0);
    bool doubleSided = (IsDoubleSided > 0);
    bool frontFacing = (negativelyScaled ? !rl_FrontFacing : rl_FrontFacing);

    if (!(frontFacing || doubleSided) && rl_InRay.depth == 0)
    {
        if (rl_InRay.renderTarget == CUSTOM_BAKE_BUFFER)
        {
            accumulate(vec4(0.0,0.0,0.0,1.0));
        }
        else if (rl_InRay.renderTarget != PROBE_BUFFER && rl_InRay.transmissionDepth == 0)
        {
            accumulate(VALIDITY_BUFFER, float(1.0));
            // accumulate -1 to sample buffer to discount this sample?
        }
        else if (rl_InRay.renderTarget == PROBE_BUFFER && rl_InRay.transmissionDepth == 0)
        {
            accumulate(PROBE_VALIDITY_BUFFER, float(1.0));
        }
    }

    // A custom bake should never proceed beyond this point, once a potential backface has been recorded we are done.
    if (rl_InRay.renderTarget == CUSTOM_BAKE_BUFFER)
        return;

    if((frontFacing || doubleSided) && rl_IsHit && IntegratorSamples.maxBounces > 0)
    {
        vec2 albedoUV = TexCoord1Varying.xy;

        // When intersecting backface we invert rl_GeometricNormal since this is the normal of the front face
        vec3 geometricNormal = (negativelyScaled ? -rl_GeometricNormal : rl_GeometricNormal); // account for negative scaling
        geometricNormal = (frontFacing ? geometricNormal : -geometricNormal); // account for backface intersection
        vec3 varyingNormal = (frontFacing ? NormalVarying : -NormalVarying); // account for backface intersection;

        vec3 intersectionPushedOff = vec3(rl_IntersectionPoint + (geometricNormal * PushOffInfo.pushOff));

        vec4 albedo = texture2D(Albedo, albedoUV);
        vec3 pathThroughput = albedo.xyz * rl_InRay.albedo.xyz;

        int base_dimension = (rl_InRay.depth+1)*PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;

        vec3 rnd;

#if PLM_USE_BLUE_NOISE_SAMPLING
            if(rl_InRay.sampleIndex < PLM_BLUE_NOISE_MAX_SAMPLES && rl_InRay.renderTarget == GI_BUFFER)
            {

            rnd = vec3( BlueNoiseSobolSample(rl_InRay.sampleIndex, base_dimension+0, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset),
                        BlueNoiseSobolSample(rl_InRay.sampleIndex, base_dimension+1, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset),
                        BlueNoiseSobolSample(rl_InRay.sampleIndex, base_dimension+2, int(rl_FrameCoord.x), int(rl_FrameCoord.y), IntegratorSamples.gi_blue_noise_buffer_offset) );
            }
            else
#endif
            {
                vec3 cpShift = GetCranleyPattersonRotation3D(int(rl_FrameCoord.x), int(rl_FrameCoord.y), base_dimension);
                rnd = fract(vec3(SobolSample(rl_InRay.sampleIndex, base_dimension, 0), SobolSample(rl_InRay.sampleIndex, base_dimension+1, 0), SobolSample(rl_InRay.sampleIndex, base_dimension+2, 0)) + cpShift);
            }


        // passing in false here reverts to sampling all lights in the lightgrid cell, but then rl_OutputRayCount needs to be adjusted accordingly
        DoShadows(true, intersectionPushedOff, varyingNormal, pathThroughput, rl_InRay.renderTarget, rnd, rl_InRay.probeDir, rl_InRay.lightmapMode, lodParam, maxT, rl_InRay.depth+1, OCCLUSIONMODE_DIRECT_ON_BOUNCE, vec4(-1.0), rl_InRay.weight, true);


        // add emissive
        vec4 emissive = texture2D(Emissive, albedoUV);
        Accumulate(rl_InRay.renderTarget, rl_InRay.weight * emissive * vec4(rl_InRay.albedo, 1.0), rl_InRay.probeDir, rl_InRay.lightmapMode);

        // Env importance sampling
        // The depth check prevents env light contribution at the last bounce of the path, preserving previous behavior
        if (SampleIndirectEnvironment() && (rl_InRay.depth + 1) < IntegratorSamples.maxBounces)
        {
            if (UseEnvironmentImportanceSampling())
                SurfaceSampleEnvironmentIS(rl_InRay.renderTarget, intersectionPushedOff, rl_InRay.probeDir, varyingNormal, geometricNormal, pathThroughput, rnd, rl_InRay.weight, rl_InRay.depth, rl_InRay.transmissionDepth + 1, rl_InRay.lightmapMode, lodParam, maxT, true);
            else
                SurfaceSampleEnvironment(rl_InRay.renderTarget, intersectionPushedOff, rl_InRay.probeDir, varyingNormal, geometricNormal, pathThroughput, rnd.xy, rl_InRay.weight, rl_InRay.depth, rl_InRay.transmissionDepth + 1, rl_InRay.lightmapMode, lodParam, maxT, true);
        }

        //Russian roulette
        bool russianRouletteContinuePath = true;
        if(rl_InRay.depth + 1 >= IntegratorSamples.minBounces)
        {
            float p = max(max(pathThroughput.x, pathThroughput.y), pathThroughput.z);
            if (p < rnd.z)
                russianRouletteContinuePath = false;

            pathThroughput = (1.0/p) * pathThroughput;
        }

        if(russianRouletteContinuePath && (rl_InRay.depth + 1) < IntegratorSamples.maxBounces)
        {
            // next bounce
            createRay();
            rl_OutRay.origin           = intersectionPushedOff;
            rl_OutRay.direction        = NextBounceDirection(rnd.xy, geometricNormal);
            rl_OutRay.color            = vec4(0.0); // unused, because we're not shooting against lights
            rl_OutRay.probeDir         = rl_InRay.probeDir;
            rl_OutRay.defaultPrimitive = rl_InRay.defaultPrimitive;
            rl_OutRay.renderTarget     = rl_InRay.renderTarget;
            rl_OutRay.isOutgoing       = true;
            rl_OutRay.sampleIndex      = rl_InRay.sampleIndex;
            rl_OutRay.rayClass         = rl_InRay.rayClass;
            rl_OutRay.depth            = rl_InRay.depth+1;
            rl_OutRay.weight           = rl_InRay.weight;
            rl_OutRay.occlusionTest    = false;
            rl_OutRay.albedo           = pathThroughput;
            rl_OutRay.sameOriginCount  = 0;
            rl_OutRay.transmissionDepth= 0;
            rl_OutRay.lightmapMode     = rl_InRay.lightmapMode;
            rl_OutRay.lodParam         = lodParam;
            rl_OutRay.maxT             = maxT;
            rl_OutRay.rayType          = rl_InRay.rayType;
            emitRayWithoutDifferentials();
        }
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Standard.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\TechniqueCommon.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

uniformblock TechniqueProperties
{
    int GridSize;
    int RaysPerPixel;
    int GridDim;
};

void CreateOrthoNormalBasis(vec3 n, out vec3 tangent, out vec3 bitangent)
{
    float sign = n.z >= 0.0 ? 1.0 : -1.0;
    float a    = -1.0 / (sign + n.z);
    float b    = n.x * n.y * a;

    tangent    = vec3(1.0 + sign * n.x * n.x * a, sign * b, -sign * n.x);
    bitangent  = vec3(b, sign + n.y * n.y * a, -n.y);
}

void pixarONB(vec3 n, inout vec3 tangent, inout vec3 bitangent)
{
    float sign = n.z >= 0.0 ? 1.0 : -1.0;
    float a = -1.0 / (sign + n.z);
    float b = n.x * n.y * a;

    tangent = vec3(1.0 + sign * n.x * n.x * a, sign * b, -sign * n.x);
    bitangent = vec3(b, sign + n.y * n.y * a, -n.y);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\TechniqueCommon.rlsl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Varyings.rlsl---------------
.
.
/*
    The contents of this file are provided under the terms described in the accompanying License.txt file. Use of this file in any way acknowledges acceptance of these terms.
    Copyright(c) 2010 - 2017, Imagination Technologies Limited and / or its affiliated group companies. All rights reserved.
*/

varying vec3 NormalVarying;
varying vec2 TexCoord0Varying;
varying vec2 TexCoord1Varying;
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\RLSL\Shaders\Varyings.rlsl---------------
.
.
