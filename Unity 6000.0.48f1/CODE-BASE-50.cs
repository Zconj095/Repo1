 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\Server\node_modules\node-addon-api\common.gypi---------------


{
  'variables': {
    'NAPI_VERSION%': "<!(node -p \"process.versions.napi\")",
    'disable_deprecated': "<!(node -p \"process.env['npm_config_disable_deprecated']\")"
  },
  'conditions': [
    ['NAPI_VERSION!=""', { 'defines': ['NAPI_VERSION=<@(NAPI_VERSION)'] } ],
    ['disable_deprecated=="true"', {
      'defines': ['NODE_ADDON_API_DISABLE_DEPRECATED']
    }],
    ['OS=="mac"', {
      'cflags+': ['-fvisibility=hidden'],
      'xcode_settings': {
        'OTHER_CFLAGS': ['-fvisibility=hidden']
      }
    }]
  ],
  'include_dirs': ["<!(node -p \"require('../').include_dir\")"],
  'cflags': [ '-Werror', '-Wall', '-Wextra', '-Wpedantic', '-Wunused-parameter' ],
  'cflags_cc': [ '-Werror', '-Wall', '-Wextra', '-Wpedantic', '-Wunused-parameter' ]
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\Server\node_modules\node-addon-api\common.gypi---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\Server\node_modules\node-addon-api\except.gypi---------------


{
  'defines': [ 'NAPI_CPP_EXCEPTIONS' ],
  'cflags!': [ '-fno-exceptions' ],
  'cflags_cc!': [ '-fno-exceptions' ],
  'conditions': [
    ["OS=='win'", {
      "defines": [
        "_HAS_EXCEPTIONS=1"
      ],
      "msvs_settings": {
        "VCCLCompilerTool": {
          "ExceptionHandling": 1,
          'EnablePREfast': 'true',
        },
      },
    }],
    ["OS=='mac'", {
      'xcode_settings': {
        'GCC_ENABLE_CPP_EXCEPTIONS': 'YES',
        'CLANG_CXX_LIBRARY': 'libc++',
        'MACOSX_DEPLOYMENT_TARGET': '10.7',
      },
    }],
  ],
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\Server\node_modules\node-addon-api\except.gypi---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\Server\node_modules\node-addon-api\node_api.gyp---------------


{
  'targets': [
    {
      'target_name': 'nothing',
      'type': 'static_library',
      'sources': [ 'nothing.c' ]
    }
  ]
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\Server\node_modules\node-addon-api\node_api.gyp---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\Server\node_modules\node-addon-api\noexcept.gypi---------------


{
  'defines': [ 'NAPI_DISABLE_CPP_EXCEPTIONS' ],
  'cflags': [ '-fno-exceptions' ],
  'cflags_cc': [ '-fno-exceptions' ],
  'conditions': [
    ["OS=='win'", {
      # _HAS_EXCEPTIONS is already defined and set to 0 in common.gypi
      #"defines": [
      #  "_HAS_EXCEPTIONS=0"
      #],
      "msvs_settings": {
        "VCCLCompilerTool": {
          'ExceptionHandling': 0,
          'EnablePREfast': 'true',
        },
      },
    }],
    ["OS=='mac'", {
      'xcode_settings': {
        'CLANG_CXX_LIBRARY': 'libc++',
        'MACOSX_DEPLOYMENT_TARGET': '10.7',
        'GCC_ENABLE_CPP_EXCEPTIONS': 'NO',
      },
    }],
  ],
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\Server\node_modules\node-addon-api\noexcept.gypi---------------


