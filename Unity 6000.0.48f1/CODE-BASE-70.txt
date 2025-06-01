 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\default.mode---------------


default = {
    builtin = true
}

profiler = {
    builtin = true
    label = "Profiler"

    capabilities = {
        reset_menus = true
        layout_switching = false
        layout_window_menu = false
        status_bar_extra_features = false
    }

    pane_types = [
        "ConsoleWindow"
        "ProfilerWindow"
    ]

    menus = [
        { name = "File" children = [
            { name = "Open profile data file..." command_id = "Profiler/OpenProfileData" }
            { name = "Save to profile data file..." command_id = "Profiler/SaveProfileData" }
            null
            { name = "Exit" }
        ]}
        { name = "Edit" children = [
            { name = "Undo" }
            { name = "Redo" }
            null
            { name = "Select All" }
            { name = "Deselect All" }
            { name = "Invert Selection" }
            null
            { name = "Cut" }
            { name = "Copy" }
            { name = "Paste" }
            null
            { name = "Delete" }
            null
            { name = "Frame Selected in Window under Cursor" }
            { name = "Find" }
            null
            { name = "Record" command_id = "Profiler/Record"  }
            { name = "Deep Profiling" command_id = "Profiler/EnableDeepProfiling"  }
        ]}
        { name = "Window" children = [
            { name = "Next Window" }
            { name = "Previous Window" }
            null
            { name = "General"  priority = 2000 children = [
                { name = "Console" }
            ]}
            { name = "Analysis"  priority = 2100 children = [
                { name = "Profiler" }
                { name = "Memory Profiler" }
                { name = "Profile Analyzer" }
            ] }
        ]}
        { name = "Help" children = [
            { name = "About Unity" }
            null
            { name = "Unity Manual" }
            { name = "Scripting Reference" }
            null
            { name = "Release Notes" }
            { name = "Software Licenses" }
            { name = "Package Manager Licenses" }
            { name = "Report a Bug..." }
        ] }
    ]
}

safe_mode = {
    builtin = true
    label = "Safe Mode"

    pane_types = [
        "ConsoleWindow"
        "InspectorWindow"
        "ProjectBrowser"
    ]

    menus = [
        { name = "File" children = [
            { name = "New Project..." }
            { name = "Open Project..." }
            { name = "Save Project" }
            null
            { name = "Exit" platform="windows"}
            { name = "Exit" platform="linux"}
            { name = "Close" platform="osx"}

        ]}
        { name = "Edit" children = [
            { name = "Undo" }
            { name = "Redo" }
            null
            { name = "Copy" }
            { name = "Paste" }
            null
            { name = "Select All" }
            null
            { name = "Delete" }
            null
            { name = "Project Settings..." }
            { name = "Preferences..." platform="windows"}
            { name = "Preferences..." platform="linux"}
        ]}
        { name = "Assets" children = [
            { name = "Create" children = [
                { name = "Folder" }
                null
                { name = "Scripting/MonoBehaviour Script" }
                { name = "Scripting/Empty C# Script" }
                { name = "Scripting/Assembly Definition" }
                { name = "Scripting/Assembly Definition Reference" }
            ]}
            { name = "Version Control" children = "*" }
            { name = "Show in Explorer" platform="windows" }
            { name = "Reveal in Finder" platform="osx" }
            { name = "Open Containing Folder" platform="linux" }
            { name = "Delete" }
            { name = "Rename" }
            { name = "Copy Path" }
            null
            { name = "View in Package Manager" }
            null
            { name = "Refresh" }
            null
            { name = "Open C# Project" menu_item_id = "Assets/Open C# Project" }
        ]}
        { name = "Packages" children = [
            { name = "Reset Package Database" }
        ]}
        { name = "Safe Mode" children = [
            { name = "Exit Safe Mode" command_id = "SafeMode/Exit" }
        ]}
        { name = "Window" children = [
            { name = "Next Window" }
            { name = "Previous Window" }
            null
            { name = "Package Manager" }
            null
            { name = "Asset Management" children = [
                { name = "Version Control" }
            ]}
            null
            { name = "General" children = [
                { name = "Console" }
                { name = "Project" }
                { name = "Inspector" }
            ]}
        ]}
        { name = "Help" children = "*" }
    ]

    context_menus = {
        Assets = [
            { name = "Create" children = [
                { name = "Folder" }
                null
                { name = "Scripting/C# Script" }
                { name = "Scripting/Assembly Definition" }
                { name = "Scripting/Assembly Definition Reference" }
            ]}
            { name = "Version Control" children = "*" }
            { name = "Show in Explorer" platform="windows" }
            { name = "Reveal in Finder" platform="osx" }
            { name = "Open Containing Folder" platform="linux" }
            { name = "Delete" }
            { name = "Rename" }
            { name = "Copy Path" }
            null
            { name = "View in Package Manager" }
            null
            { name = "Refresh" }
            null
            { name = "Open C# Project" menu_item_id = "Assets/Open C# Project" }
        ]
    }

    layout = {
        restore_saved_layout = false
        top_view = { class_name = "SafeModeToolbarWindow" size = 62 }
        min_width = 1024
        min_height = 768
        center_view = {
            vertical = true
            children = [
                {
                    horizontal = true
                    children = [
                        { tabs = true children = [{ class_name = "ConsoleWindow"}] size = 0.8 }
                        { tabs = true children = [{ class_name = "InspectorWindow"}] size = 0.2 }
                    ]
                }
                {
                    size = 0.3
                    tabs = true
                    children = [{ class_name = "ProjectBrowser"}]
                }
            ]
        }
    }

    capabilities = {
        remember = false                    // Never save this mode as the current
        safe_mode = true                    // Used to disable some features
        main_toolbar = false
        layout_switching = true             // Update the layout when switching to safe mode.
        layout_window_menu = false
        status_bar_extra_features = false
        allow_asset_creation = false
    }
}




#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\default.mode---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.accessibility\package.ModuleCompilationTrigger---------------




#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.accessibility\package.ModuleCompilationTrigger---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.ai\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.ai\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.amd\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.amd\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.androidjni\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.androidjni\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.animation\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.animation\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.assetbundle\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.assetbundle\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.audio\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.audio\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.cloth\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.cloth\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.director\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.director\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.hierarchycore\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.hierarchycore\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.imageconversion\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.imageconversion\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.imgui\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.imgui\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.jsonserialize\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.jsonserialize\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.nvidia\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.nvidia\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.particlesystem\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.particlesystem\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.physics\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.physics\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.physics2d\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.physics2d\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.screencapture\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.screencapture\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.subsystems\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.subsystems\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.terrain\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.terrain\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.terrainphysics\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.terrainphysics\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.tilemap\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.tilemap\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.ui\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.ui\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.uielements\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.uielements\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.umbra\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.umbra\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unityanalytics\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unityanalytics\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequest\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequest\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequestassetbundle\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequestassetbundle\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequestaudio\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequestaudio\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequesttexture\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequesttexture\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequestwww\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.unitywebrequestwww\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.vehicles\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.vehicles\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.video\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.video\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.vr\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.vr\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.wind\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.wind\package.ModuleCompilationTrigger---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.xr\package.ModuleCompilationTrigger---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.modules.xr\package.ModuleCompilationTrigger---------------
.
.
