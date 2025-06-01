 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\GLSLSupport.glslinc---------------


#ifndef GLSL_SUPPORT_INCLUDED
#define GLSL_SUPPORT_INCLUDED

// Automatically included in raw GLSL (GLSLPROGRAM) shader snippets, to map from some of the legacy OpenGL
// variable names to uniform names used by Unity.

#ifdef GL_FRAGMENT_PRECISION_HIGH
    precision highp float;
#else
    precision mediump float;
#endif

uniform mat4 unity_ObjectToWorld;
uniform mat4 unity_WorldToObject;
uniform mat4 unity_MatrixVP;
uniform mat4 unity_MatrixV;
uniform mat4 unity_MatrixInvV;
uniform mat4 glstate_matrix_projection;

#define gl_ModelViewProjectionMatrix        (unity_MatrixVP * unity_ObjectToWorld)
#define gl_ModelViewMatrix                  (unity_MatrixV * unity_ObjectToWorld)
#define gl_ModelViewMatrixTranspose         (transpose(unity_MatrixV * unity_ObjectToWorld))
#define gl_ModelViewMatrixInverseTranspose  (transpose(unity_WorldToObject * unity_MatrixInvV))
#define gl_NormalMatrix                     (transpose(mat3(unity_WorldToObject * unity_MatrixInvV)))
#define gl_ProjectionMatrix                 glstate_matrix_projection

#if __VERSION__ < 120
#ifndef UNITY_GLSL_STRIP_TRANSPOSE
mat3 transpose(mat3 mtx)
{
    vec3 c0 = mtx[0];
    vec3 c1 = mtx[1];
    vec3 c2 = mtx[2];

    return mat3(
        vec3(c0.x, c1.x, c2.x),
        vec3(c0.y, c1.y, c2.y),
        vec3(c0.z, c1.z, c2.z)
    );
}
mat4 transpose(mat4 mtx)
{
    vec4 c0 = mtx[0];
    vec4 c1 = mtx[1];
    vec4 c2 = mtx[2];
    vec4 c3 = mtx[3];

    return mat4(
        vec4(c0.x, c1.x, c2.x, c3.x),
        vec4(c0.y, c1.y, c2.y, c3.y),
        vec4(c0.z, c1.z, c2.z, c3.z),
        vec4(c0.w, c1.w, c2.w, c3.w)
    );
}
#endif
#endif // __VERSION__ < 120

#endif // GLSL_SUPPORT_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\GLSLSupport.glslinc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityCG.glslinc---------------


#ifndef UNITY_CG_INCLUDED
#define UNITY_CG_INCLUDED

// -------------------------------------------------------------------
// Common functions

float saturate(float x)
{
    return max(0.0, min(1.0, x));
}


// -------------------------------------------------------------------
//  builtin values exposed from Unity

// Time values from Unity
uniform vec4 _Time;
uniform vec4 _SinTime;
uniform vec4 _CosTime;

// x = 1 or -1 (-1 if projection is flipped)
// y = near plane
// z = far plane
// w = 1/far plane
uniform vec4 _ProjectionParams;

// x = width
// y = height
// z = 1 + 1.0/width
// w = 1 + 1.0/height
uniform vec4 _ScreenParams;

uniform vec3 _WorldSpaceCameraPos;
uniform vec4 _WorldSpaceLightPos0;

uniform vec4 _LightPositionRange; // xyz = pos, w = 1/range

// -------------------------------------------------------------------
//  helper functions and macros used in many standard shaders

#if defined DIRECTIONAL || defined DIRECTIONAL_COOKIE
#define USING_DIRECTIONAL_LIGHT
#endif

#if defined DIRECTIONAL || defined DIRECTIONAL_COOKIE || defined POINT || defined SPOT || defined POINT_NOATT || defined POINT_COOKIE
#define USING_LIGHT_MULTI_COMPILE
#endif


#ifdef VERTEX

// Computes world space light direction
vec3 WorldSpaceLightDir( vec4 v )
{
    vec3 worldPos = (unity_ObjectToWorld * v).xyz;
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

// Computes object space light direction
vec3 ObjSpaceLightDir( vec4 v )
{
    vec3 objSpaceLightPos = (unity_WorldToObject * _WorldSpaceLightPos0).xyz;
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

// Computes world space view direction
vec3 WorldSpaceViewDir( vec4 v )
{
    return _WorldSpaceCameraPos.xyz - (unity_ObjectToWorld * v).xyz;
}

// Computes object space view direction
vec3 ObjSpaceViewDir( vec4 v )
{
    vec3 objSpaceCameraPos = (unity_WorldToObject * vec4(_WorldSpaceCameraPos.xyz, 1.0)).xyz;
    return objSpaceCameraPos - v.xyz;
}

// Declares 3x3 matrix 'rotation', filled with tangent space basis
// Do not use multiline define here, nVidia OpenGL drivers are buggy in parsing that.
#define TANGENT_SPACE_ROTATION vec3 binormal = cross( gl_Normal.xyz, Tangent.xyz ) * Tangent.w; mat3 rotation = mat3( Tangent.x, binormal.x, gl_Normal.x, Tangent.y, binormal.y, gl_Normal.y, Tangent.z, binormal.z, gl_Normal.z );


// Transforms float2 UV by scale/bias property (new method)
// GLSL ES does not support ## concat operator so we also provide macro that expects xxx_ST
#define TRANSFORM_TEX_ST(tex,namest) (tex.xy * namest.xy + namest.zw)
#ifndef GL_ES
    #define TRANSFORM_TEX(tex,name) TRANSFORM_TEX_ST(tex, name##_ST)
#endif

// Deprecated. Used to transform 4D UV by a fixed function texture matrix. Now just returns the passed UV.
#define TRANSFORM_UV(idx) (gl_TexCoord[0].xy)

#endif // VERTEX



// Calculates UV offset for parallax bump mapping
vec2 ParallaxOffset( float h, float height, vec3 viewDir )
{
    h = h * height - height/2.0;
    vec3 v = normalize(viewDir);
    v.z += 0.42;
    return h * (v.xy / v.z);
}


// Converts color to luminance (grayscale)
float Luminance( vec3 c )
{
    return dot( c, vec3(0.22, 0.707, 0.071) );
}


#endif


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityCG.glslinc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStereoExtensions.glslinc---------------


#ifndef GLSL_STEREO_EXTENSIONS_INCLUDED
#define GLSL_STEREO_EXTENSIONS_INCLUDED

#ifdef STEREO_MULTIVIEW_ON
    #extension GL_OVR_multiview2 : require
#endif

#ifdef STEREO_INSTANCING_ON
    #extension GL_NV_viewport_array2 : enable
    #extension GL_AMD_vertex_shader_layer : enable
    #extension GL_ARB_fragment_layer_viewport : enable
#endif

#endif // GLSL_STEREO_EXTENSIONS_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStereoExtensions.glslinc---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStereoSupport.glslinc---------------


#ifndef GLSL_STEREO_SETUP_INCLUDED
#define GLSL_STEREO_SETUP_INCLUDED

#if defined(STEREO_MULTIVIEW_ON) || defined(STEREO_INSTANCING_ON)
    layout(std140) uniform UnityStereoGlobals {
        mat4 unity_StereoMatrixP[2];
        mat4 unity_StereoMatrixV[2];
        mat4 unity_StereoMatrixInvV[2];
        mat4 unity_StereoMatrixVP[2];
        mat4 unity_StereoCameraProjection[2];
        mat4 unity_StereoCameraInvProjection[2];
        mat4 unity_StereoWorldToCamera[2];
        mat4 unity_StereoCameraToWorld[2];
        vec3 unity_StereoWorldSpaceCameraPos[2];
        vec4 unity_StereoScaleOffset[2];
    };
#endif

#ifdef VERTEX
    #ifdef STEREO_MULTIVIEW_ON
        layout(num_views = 2) in;
    #endif

    uniform int unity_StereoEyeIndex;

    int SetupStereoEyeIndex()
    {
        int eyeIndex = unity_StereoEyeIndex;

        #if defined(STEREO_MULTIVIEW_ON)
            eyeIndex = int(gl_ViewID_OVR);
        #elif defined(STEREO_INSTANCING_ON)
            eyeIndex = int(gl_InstanceID & 1);
            gl_Layer = eyeIndex;
        #endif

        return eyeIndex;
    }

    mat4 GetStereoMatrixVP(int eyeIndex)
    {
        mat4 stereoVP = unity_MatrixVP;

        #if defined(STEREO_MULTIVIEW_ON) || defined(STEREO_INSTANCING_ON)
            stereoVP = unity_StereoMatrixVP[eyeIndex];
        #endif

        return stereoVP;
    }
#endif

#endif // GLSL_STEREO_SETUP_INCLUDED


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\CGIncludes\UnityStereoSupport.glslinc---------------


