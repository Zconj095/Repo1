 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\PolygonChangeShapeWindow.uxml---------------


<UXML xmlns:ui= "UnityEngine.Experimental.UIElements"  xmlns:uie= "UnityEditor.Experimental.UIElements">
  <ui:VisualElement name = "polygonShapeWindow" class = "moduleWindow topLeft">
    <ui:Box name = "polygonShapeWindowFrame">
      <uie:IntegerField name = "labelIntegerField" class = "labelIntegerField" label = "Sides" value = "0"/>
      <ui:VisualElement name = "warning" >
        <ui:Image name = "icon"/>
        <ui:Label name = "warningLabel" text= "Sides can only be either 0 or anything between 3 and 128"/>
      </ui:VisualElement>
      <ui:Button name = "changeButton" text= "Change" />
    </ui:Box>
  </ui:VisualElement>
</UXML>


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\PolygonChangeShapeWindow.uxml---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteFrameModuleInspector.uxml---------------


<UXML xmlns:ui = "UnityEngine.Experimental.UIElements" xmlns:uie = "UnityEditor.Experimental.UIElements">
    <ui:PopupWindow name = "spriteFrameModuleInspector" text = "Sprite">
        <ui:VisualElement name = "name" class = "spriteFrameModuleInspectorField">
          <ui:TextField name = "spriteName" label = "Name" value = "Square"/>
        </ui:VisualElement>
        <ui:VisualElement name = "position">
            <ui:VisualElement class = "spriteFrameModuleInspectorField unity-composite-field unity-composite-field--multi-line unity-base-field">
                <ui:Label text = "Position" />
                <ui:VisualElement class = "unity-composite-field__input unity-base-field__input">
                  <ui:VisualElement name = "positionXY" class = "unity-composite-field__field-group">
                    <uie:IntegerField name = "positionX" class = "unity-composite-field__field" label = "X"/>
                      <uie:IntegerField name = "positionY" class = "unity-composite-field__field" label = "Y"/>
                  </ui:VisualElement>
                  <ui:VisualElement name = "positionWH" class = "unity-composite-field__field-group">
                      <uie:IntegerField name = "positionW" class = "unity-composite-field__field" label = "W"/>
                      <uie:IntegerField name = "positionH" class = "unity-composite-field__field" label = "H"/>
                  </ui:VisualElement>
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name = "border">
            <ui:VisualElement class = "spriteFrameModuleInspectorField unity-composite-field unity-composite-field--multi-line unity-base-field">
                <ui:Label text = "Border" />
                <ui:VisualElement class = "unity-composite-field__input unity-base-field__input">
                  <ui:VisualElement name = "borderLT" class = "unity-composite-field__field-group">
                      <uie:IntegerField name = "borderL" class = "unity-composite-field__field" label = "L"/>
                      <uie:IntegerField name = "borderT" class = "unity-composite-field__field" label = "T"/>
                  </ui:VisualElement>
                  <ui:VisualElement name = "borderRB" class = "unity-composite-field__field-group">
                      <uie:IntegerField name = "borderR" class = "unity-composite-field__field" label = "R"/>
                      <uie:IntegerField name = "borderB" class = "unity-composite-field__field" label = "B"/>
                  </ui:VisualElement>
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name = "pivot" class = "spriteFrameModuleInspectorField">
            <uie:EnumField name = "pivotField" label = "Pivot" class="unity-enum-field"/>
        </ui:VisualElement>
        <ui:VisualElement name = "pivotUnitMode" class = "spriteFrameModuleInspectorField">
            <uie:EnumField name = "pivotUnitModeField" label ="Pivot Unit Mode" class="unity-enum-field"/>
        </ui:VisualElement>
        <ui:VisualElement name = "customPivot" class = "spriteFrameModuleInspectorField unity-composite-field unity-base-field">
            <ui:Label text = "Custom Pivot" />
          <ui:VisualElement name = "customPivotField" class = "unity-composite-field__input">
            <uie:FloatField name = "customPivotX" class = "unity-composite-field__field" label = "X"/>
            <uie:FloatField name = "customPivotY" class = "unity-composite-field__field" label = "Y"/>
          </ui:VisualElement>
        </ui:VisualElement>
    </ui:PopupWindow>
</UXML>


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteFrameModuleInspector.uxml---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteOutlineToolOverlayPanel.uxml---------------


<?xml version="1.0" encoding="utf-8"?>
<UXML xmlns:ui="UnityEngine.UIElements" xmlns:eui="UnityEditor.UIElements" xmlns:aui="UnityEditor.U2D.Sprites.SpriteEditorTool">
  <aui:SpriteOutlineToolOverlayPanel name="SpriteOutlineToolOverlayPanel" picking-mode="Ignore">
    <ui:PopupWindow name = "spriteOutlineTool" text = "Outline Tool">
      <ui:VisualElement name="Content">
        <ui:VisualElement class="form-row">
          <ui:VisualElement class="form-editor">
            <ui:Slider name="OutlineDetailSlider" class="named-slider" direction="Horizontal" low-value="0" high-value="1" label="Outline Detail" tooltip="Accuracy of the generated outline. Small values will produce simpler outlines. Large values will produce denser outlines that fit to the Sprite better." />
            <eui:FloatField name="OutlineDetailField" class="slider-field" value="0" />
          </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement class="form-row">
          <ui:VisualElement class="form-editor">
            <ui:SliderInt name="AlphaToleranceSlider" class="named-slider" direction="Horizontal" low-value="0" high-value="254" label="Alpha Tolerance" tooltip="Pixels with alpha value smaller than tolerance will be considered transparent during outline detection." />
            <eui:IntegerField name="AlphaToleranceField" class="slider-field" value="0" />
          </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement class="form-row">
          <ui:Label name="Snap" tooltip="Snap points to nearest pixel." text="Snap" class = "toggle-label"/>
          <ui:Toggle name="SnapToggle" class="form-editor" value="true"/>
        </ui:VisualElement>
        <ui:VisualElement class="form-row" name ="OptimizeOutlineGroup" style="visibility:Hidden">
          <ui:Label name="OptimizeOutline" tooltip="Initialize weights automatically for the generated geometry" text="Optimize Outline" />
          <ui:Toggle name="OptimizeOutlineToggle" class="form-editor" value="true" />
        </ui:VisualElement>

        <ui:VisualElement name="GenerateRow" class="form-row">
          <ui:Button name="GenerateButton" text="Generate For Selected" tooltip="Generate new outline based on mesh detail value. If no SpriteRect is selected, it will generate outlines for all the Sprites that does not have a custom outline."/>
          <ui:Toggle name="ForceGenerateToggle" tooltip="Regenerate outline even if the Sprite/s already have one." />
        </ui:VisualElement>
        <ui:VisualElement name="CopyPasteRow" class="form-row">
          <ui:Button name="CopyButton" text="Copy" tooltip="Copy outline from Sprite"/>
          <ui:Button name="PasteButton" text="Paste" tooltip="Paste outline to Sprite"/>
          <ui:Button name="PasteAllButton" text="Paste All" tooltip="Paste outline to all Sprites"/>
        </ui:VisualElement>
        <ui:VisualElement name="PasteAlternateRow" class="form-row">
          <ui:Label name="PasteAlternateLabel"/>
          <ui:Button name="PasteAlternateButton" text="Paste" tooltip="Paste outline to Sprite"/>
          <ui:Button name="PasteAlternateAllButton" text="Paste All" tooltip="Paste outline to all Sprites"/>
        </ui:VisualElement>
      </ui:VisualElement>
    </ui:PopupWindow>
  </aui:SpriteOutlineToolOverlayPanel>
</UXML>


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteOutlineToolOverlayPanel.uxml---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\UXML\DefaultVolumeProfileEditor.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xmlns:uib="Unity.UI.Builder">
    <Style src="project://database/Packages/com.unity.render-pipelines.core/Editor/StyleSheets/DefaultVolumeProfileEditor.uss"/>
    <ui:VisualElement class="content-container">
        <uie:ToolbarSearchField class="search-field"/>
        <ui:HelpBox message-type="Info" name="volume-override-info-box"/>
    </ui:VisualElement>
    <ui:VisualElement name="component-list"/>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\UXML\DefaultVolumeProfileEditor.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\UXML\RenderGraphViewer.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xmlns:r="UnityEditor.Rendering" editor-extension-mode="True">
    <Style src="project://database/Packages/com.unity.render-pipelines.core/Editor/StyleSheets/RenderGraphViewer.uss" />
    <ui:VisualElement name="header-container">
        <ui:Button name="capture-button" tooltip="Capture"/>
        <ui:DropdownField name="current-graph-dropdown" label="Graph" />
        <ui:DropdownField name="current-execution-dropdown" label="Camera" />
        <uie:EnumFlagsField label="Pass Filter" name="pass-filter-field"/>
        <uie:EnumFlagsField label="Resource Filter" name="resource-filter-field"/>
    </ui:VisualElement>

    <ui:TwoPaneSplitView name="content-container" orientation="Horizontal">
        <ui:VisualElement name="main-container">
            <ui:ScrollView name="pass-list-scroll-view">
                <ui:VisualElement name="pass-list"/>
                <ui:VisualElement name="pass-list-width-helper" pickingMode="Ignore" />
            </ui:ScrollView>
            <ui:VisualElement name="pass-list-corner-occluder"/>
            <ui:VisualElement name="resource-container">
                <ui:ScrollView name="resource-list-scroll-view"/>
                <ui:ScrollView name="resource-grid-scroll-view">
                    <ui:VisualElement name="resource-grid"/>
                    <ui:VisualElement name="grid-line-container"/>
                    <ui:VisualElement name="hover-overlay"/>
                </ui:ScrollView>
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:TwoPaneSplitView name="panel-container" orientation="Vertical">
            <r:HeaderFoldout text="Resource List" name="panel-resource-list">
                <uie:ToolbarSearchField name="resource-search-field" class="panel-search-field"/>
                <ui:ScrollView name="panel-resource-list-scroll-view"/>
            </r:HeaderFoldout>
            <r:HeaderFoldout text="Pass List" name="panel-pass-list">
                <uie:ToolbarSearchField name="pass-search-field" class="panel-search-field"/>
                <ui:ScrollView name="panel-pass-list-scroll-view"/>
            </r:HeaderFoldout>
        </ui:TwoPaneSplitView>
    </ui:TwoPaneSplitView>
    <ui:VisualElement name="empty-state-message">
        <ui:TextElement/>
    </ui:VisualElement>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\UXML\RenderGraphViewer.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\UXML\RenderPipelineGlobalSettings.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xmlns:uib="Unity.UI.Builder">
    <Style src="project://database/Packages/com.unity.render-pipelines.core/Editor/StyleSheets/RenderPipelineGlobalSettings.uss"/>
    <Style src="project://database/Packages/com.unity.render-pipelines.core/Editor/StyleSheets/HelpButton.uss"/>
    <ui:ScrollView horizontal-scroller-visibility="Hidden">
        <ui:VisualElement class="srp-global-settings__container">
            <ui:VisualElement class="srp-global-settings__header-container">
                <ui:Label name="srp-global-settings__header-label" text="SRP Global Settings"/>
                <ui:VisualElement class="srp-global-settings__help-button-container">
                    <ui:Button name="srp-global-settings__help-button" class="iconButton unity-icon-button help-button">
                        <ui:Image name="srp-global-settings__help-button-image" class="unity-icon-button help-button__image"/>
                    </ui:Button>
                </ui:VisualElement>
            </ui:VisualElement>
            <ui:VisualElement name="srp-global-settings__content-container"/>
        </ui:VisualElement>
    </ui:ScrollView>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\UXML\RenderPipelineGlobalSettings.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\UXML\VolumeEditor.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" uib="Unity.UI.Builder" editor-extension-mode="True">
    <Style src="project://database/Packages/com.unity.render-pipelines.core/Editor/StyleSheets/VolumeEditor.uss?fileID=7433441132597879392&amp;guid=cd410e8cdc0119a46a753f48e79c8d07&amp;type=3#VolumeEditor" />
    <uie:PropertyField
            binding-path="blendDistance"
            name="volume-profile-blend-distance"
            label="Blend Distance"
            tooltip="Sets the outer distance to start blending from. A value of 0 means no blending and Unity applies the Volume overrides immediately upon entry." />
    <ui:VisualElement name="collider-fixme-box__container"/>
    <uie:PropertyField
            binding-path="weight"
            label="Weight"
            tooltip="Sets the total weight of this Volume in the Scene. 0 means no effect and 1 means full effect." />
    <uie:PropertyField
            binding-path="priority"
            name="volume-profile-priority"
            label="Priority"
            tooltip="A value which determines which Volume is being used when Volumes have an equal amount of influence on the Scene. Volumes with a higher priority will override lower ones." />
    <ui:VisualElement class="volume-profile-header__container">
        <ui:Image name="volume-profile-header__asset-icon" style="flex-grow: 0;" />
        <ui:VisualElement class="volume-profile-objectfield__container volume-profile-objectfield__container--column">
            <ui:VisualElement class="volume-profile-objectfield__container volume-profile-objectfield__container--row">
                <uie:ObjectField binding-path="sharedProfile" class="volume-profile-objectfield" />
                <ui:Button class="volume-profile-objectfield__contextmenu iconButton unity-icon-button">
                    <ui:Image name="volume-profile-objectfield__contextmenu-image" class="unity-icon-button help-button__image" />
                </ui:Button>
            </ui:VisualElement>
            <ui:Button text="New" tooltip="Create a new profile." name="volume-profile-new-button" />
            <ui:Label text="Instance Profile" name="volume-profile-instance-profile-label" />
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="volume-profile-component-container" />
    <ui:HelpBox name="volume-profile-empty-helpbox" text="Please select or create a new Volume Profile to begin applying overrides to the scene." message-type="Info"/>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\UXML\VolumeEditor.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Samples~\Common\Scripts\Resources\SamplesSelectionUXML.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <Style src="project://database/Assets/Samples/Core%20RP%20Library/Common/Scripts/Resources/SamplesSelectionUSS.uss?fileID=7433441132597879392&amp;guid=dace8ee3f59c99149ad4c1db64b635fe&amp;type=3#SamplesSelectionUSS" />
    <ui:ScrollView horizontal-scroller-visibility="Hidden">
        <ui:Button text="Open in Window" name="OpenInWindowButton" enabled="true" style="margin-top: 15px; margin-bottom: 0; width: 100%; align-self: center; display: none;" />
        <ui:VisualElement name="RequiredSettingsBox" style="flex-grow: 1; top: auto; left: auto; bottom: auto; right: auto; margin-left: 0; margin-right: 0; margin-top: 15px; margin-bottom: 15px; padding-left: 5px; padding-right: 5px; padding-top: 5px; padding-bottom: 5px; align-self: flex-start; border-left-color: rgba(0, 0, 0, 0.25); border-right-color: rgba(0, 0, 0, 0.25); border-top-color: rgba(0, 0, 0, 0.25); border-bottom-color: rgba(0, 0, 0, 0.25); flex-direction: column; flex-wrap: nowrap; border-left-width: 1px; border-right-width: 1px; border-top-width: 1px; border-bottom-width: 1px; border-top-left-radius: 5px; border-bottom-left-radius: 5px; border-top-right-radius: 5px; border-bottom-right-radius: 5px; background-color: rgba(255, 255, 255, 0.1); justify-content: space-around; width: 100%;">
            <ui:VisualElement style="flex-grow: 1; flex-direction: row; margin-bottom: 5px;">
                <ui:VisualElement class="warningIcon" />
                <ui:Label text="The following settings are required for the samples to display properly:" style="white-space: normal; justify-content: center; align-self: center; width: auto; margin-right: 39px;" />
            </ui:VisualElement>
            <ui:VisualElement name="RequiredSettingsList" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0);">
                <ui:Button text="Button" name="PreviewButton" class="requiredSettingButton" style="border-left-color: rgba(0, 0, 0, 0.25); border-right-color: rgba(0, 0, 0, 0.25); border-top-color: rgba(0, 0, 0, 0.25); border-bottom-color: rgba(0, 0, 0, 0.25); margin-left: 0; padding-left: 12px;" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:Label tabindex="-1" text="Samples Selection" display-tooltip-when-elided="true" view-data-key="Headline" binding-path="headline" name="headline" style="-unity-font-style: bold; color: rgb(184, 226, 229); -unity-text-align: middle-center; font-size: 22px; white-space: normal; height: auto; width: auto; margin-bottom: 15px; margin-top: 15px; justify-content: center; align-self: center; flex-wrap: wrap; flex-direction: column; align-items: auto; margin-left: 0; margin-right: 0; padding-left: 0; padding-right: 0;" />
        <ui:VisualElement name="intro" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); flex-direction: column; flex-wrap: nowrap; padding-top: 0; padding-bottom: 0; padding-left: 0; padding-right: 0; height: auto; align-self: stretch; justify-content: center; align-items: stretch; width: 100%; margin-left: 0; margin-right: 0; margin-top: 15px; margin-bottom: 15px;" />
        <ui:VisualElement name="SamplesSelection" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0.16); margin-right: 0; -unity-background-scale-mode: scale-to-fit; width: 100%; margin-left: 0; align-self: flex-start; align-items: stretch; justify-content: space-around; padding-right: 10px; padding-left: 10px; padding-top: 10px; padding-bottom: 9px; -unity-text-align: upper-left; white-space: normal; margin-top: 15px; margin-bottom: 15px; border-top-left-radius: 5px; border-top-right-radius: 5px; border-bottom-right-radius: 5px; border-bottom-left-radius: 5px; border-left-color: rgba(0, 0, 0, 0.25); border-right-color: rgba(0, 0, 0, 0.25); border-top-color: rgba(0, 0, 0, 0.25); border-bottom-color: rgba(0, 0, 0, 0.25); border-left-width: 1px; border-right-width: 1px; border-top-width: 1px; border-bottom-width: 1px;">
            <ui:VisualElement name="selectionButtons" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); flex-direction: row; justify-content: flex-start; margin-left: 0; margin-right: 0; margin-top: 0; align-self: auto; width: 100%; align-items: auto;">
                <ui:Label text="Samples" style="-unity-font-style: bold; align-self: center; margin-left: 0; margin-right: 0; padding-right: 0; padding-left: 0; align-items: stretch; justify-content: flex-start;" />
                <ui:DropdownField index="-1" choices="System.Collections.Generic.List`1[System.String]" name="SampleDropDown" tooltip="Select the Sample to Instantiate" style="opacity: 1; display: flex; visibility: visible; overflow: hidden; height: 18px; width: 50%; justify-content: space-evenly; align-items: flex-end; flex-direction: row; align-self: center; -unity-text-align: middle-center; padding-left: 0; padding-right: 0; padding-top: 0; padding-bottom: 0; margin-left: 5px; margin-right: 8px; margin-top: 0; margin-bottom: 0; text-overflow: ellipsis; min-height: auto; max-width: none; min-width: auto;" />
                <ui:Button text="&lt;&lt;" name="switchBack" style="height: 18px; margin-left: 0; margin-right: 5px; margin-bottom: 0; padding-left: 0; padding-bottom: 0; padding-right: 0; padding-top: 0; min-height: auto; width: auto; justify-content: flex-start; align-self: center; align-items: stretch; margin-top: 0; border-left-width: 0; border-right-width: 0; border-top-width: 0; border-bottom-width: 0; -unity-text-align: middle-center; white-space: nowrap; max-width: none; max-height: none; min-width: 20px; font-size: 10px; -unity-font-style: bold;" />
                <ui:Button text="&gt;&gt;" name="switchForward" style="height: 18px; margin-left: 0; margin-right: 0; margin-bottom: 0; padding-left: 0; padding-bottom: 0; padding-right: 0; padding-top: 0; min-height: auto; width: auto; justify-content: flex-start; align-self: center; align-items: stretch; margin-top: 0; border-left-width: 0; border-right-width: 0; border-top-width: 0; border-bottom-width: 0; -unity-text-align: middle-center; white-space: nowrap; font-size: 10px; -unity-font-style: bold; min-width: 20px;" />
            </ui:VisualElement>
            <ui:VisualElement name="sampleInfosContainer" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); min-width: auto; min-height: auto; height: auto; padding-left: 0; padding-bottom: 0; padding-top: 0; padding-right: 0; justify-content: flex-start; width: 100%; margin-left: 0; margin-right: 0; margin-top: 20px; margin-bottom: 20px; max-height: none; align-self: center;" />
            <ui:Button text="Select Object" display-tooltip-when-elided="true" name="SelectSampleBtn" style="width: 100%; align-self: center; padding-left: 0; margin-left: 0; margin-right: 0; margin-top: 0; margin-bottom: 0; padding-right: 0; padding-top: 0; padding-bottom: 0; height: 18px; white-space: nowrap; justify-content: flex-start;" />
        </ui:VisualElement>
    </ui:ScrollView>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Samples~\Common\Scripts\Resources\SamplesSelectionUXML.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\Tools\Resources\ColorCheckerUI.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xsi="http://www.w3.org/2001/XMLSchema-instance" engine="UnityEngine.UIElements" editor="UnityEditor.UIElements" noNamespaceSchemaLocation="../../../UIElementsSchema/UIElements.xsd" editor-extension-mode="True">
    <ui:VisualElement name="margin" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); width: auto; height: auto; max-height: 0; margin-top: 10px; margin-bottom: 10px;" />
    <ui:DropdownField index="-1" choices="System.Collections.Generic.List`1[System.String]" name="ModesDropdown" binding-path="Mode" />
    <ui:Label text="Label" name="Info" style="margin-bottom: 8px; margin-top: 4px; margin-left: 6px; margin-right: 6px; white-space: normal; -unity-font-style: italic;" />
    <ui:VisualElement name="textureMode" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0);">
        <uie:ObjectField label="Texture" allow-scene-objects="true" type="UnityEngine.Texture2D, UnityEngine.CoreModule" binding-path="userTexture" name="userTexture" tooltip="Base Texture, lit." style="height: auto; padding-bottom: 5px; width: 60%;" />
        <ui:VisualElement style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); justify-content: flex-start; align-items: stretch; align-self: flex-start; margin-left: 0; margin-right: 0; flex-direction: row; flex-wrap: nowrap; width: 100%; height: auto;">
            <uie:ObjectField label="Unlit Texture" allow-scene-objects="true" type="UnityEngine.Texture2D, UnityEngine.CoreModule" binding-path="userTextureRaw" name="rawTexture" tooltip="Comparison texture, unlit." style="width: 60%; height: 19px; align-self: flex-start;" />
            <ui:Toggle label="Pre Exposure " name="unlitTextureExposure" binding-path="unlitTextureExposure" value="true" tooltip="Make the texture values adapt to exposure. Uncheck this when using raw values." style="width: 40%; height: auto; justify-content: space-around; align-items: stretch; align-self: flex-start; flex-direction: row; padding-right: 0; padding-left: 20px; padding-top: 2px;" />
        </ui:VisualElement>
        <ui:Slider picking-mode="Ignore" label="Slicer" value="0" high-value="1" name="textureSlice" tooltip="Slice the color checker between the texture and the unlit raw texture. Both are still affected by post-processes." binding-path="textureSlice" show-input-field="true" style="padding-top: 10px;" />
    </ui:VisualElement>
    <ui:VisualElement name="colorfields" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); flex-direction: column-reverse;" />
    <ui:VisualElement name="customControls" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0);">
        <ui:Button tabindex="-1" text="Reset Values" display-tooltip-when-elided="true" name="resetBtn" tooltip="Reset the color fields to default values." />
        <ui:SliderInt picking-mode="Ignore" label="Material Fields" value="6" high-value="12" name="materialFieldsCount" show-input-field="true" low-value="1" binding-path="materialFieldsCount" tooltip="Number of materials displayed. Each row represents a material with varying smoothness." />
        <ui:VisualElement name="colorfieldsCtrl" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); height: auto; min-width: 100%; min-height: auto; max-height: initial;">
            <ui:SliderInt picking-mode="Ignore" value="24" high-value="64" low-value="1" show-input-field="true" inverted="false" label="Color Fields" name="fieldCount" binding-path="fieldCount" tooltip="Number of colors displayed." />
            <ui:SliderInt picking-mode="Ignore" label="Fields per Row" value="6" high-value="16" name="fieldsPerRow" show-input-field="true" binding-path="fieldsPerRow" low-value="1" tooltip="Number of colors per row." />
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="materialElement" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); display: none;">
        <ui:VisualElement name="materialRow" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); flex-direction: row; align-items: flex-start; justify-content: flex-start; align-self: stretch; max-height: 22px;">
            <ui:Label tabindex="-1" text="Gold" display-tooltip-when-elided="true" style="align-self: center; margin-right: 3px;" />
            <uie:ColorField value="#FFE29BFF" show-alpha="false" show-eye-dropper="false" focusable="true" style="width: 15%; margin-right: 50px; visibility: visible; overflow: hidden; display: flex;" />
            <ui:Label tabindex="-1" text="is Metal" display-tooltip-when-elided="true" style="align-self: center; align-items: auto; margin-right: 3px;" />
            <ui:Toggle value="true" style="justify-content: flex-start; align-items: center; align-self: center; flex-direction: row; -unity-text-align: upper-right; white-space: normal; text-overflow: clip;" />
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="margin" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); width: auto; height: auto; max-height: 0; margin-top: 10px; margin-bottom: 10px;" />
    <ui:VisualElement name="colorfieldsAdjustments" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0);">
        <ui:Slider picking-mode="Ignore" label="Fields Margin" value="0.08" high-value="1" binding-path="gridThickness" show-input-field="true" name="fieldsMargin" tooltip="Controls the size of each fields. " />
        <ui:VisualElement name="gradientElement" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); flex-direction: row; flex-wrap: nowrap; align-items: auto; justify-content: flex-start; align-self: auto; height: auto; min-height: auto; max-height: 18px;">
            <ui:Toggle label="Add Gradient " binding-path="addGradient" name="gradientToggle" tooltip="Add a gradient at the bottom of the color checker. " style="justify-content: flex-start; align-self: auto; align-items: flex-start; margin-right: 25px; width: auto;" />
            <uie:ColorField value="#E9E9E3FF" show-alpha="false" name="gradientA" binding-path="gradientA" show-eye-dropper="true" style="height: 18px; width: 16%; align-self: auto;" />
            <uie:ColorField value="#131416FF" show-alpha="false" show-eye-dropper="true" name="gradientB" binding-path="gradientB" style="width: 16%; height: 18px;" />
            <ui:FloatField value="2.2" binding-path="gradientPower" name="gradientPower" tooltip="Power applied to the blend value used to create the gradient." style="width: 10%; justify-content: flex-start; align-items: flex-start; align-self: flex-start;" />
        </ui:VisualElement>
        <ui:Toggle label="Sphere Mode" name="sphereModeToggle" binding-path="sphereMode" tooltip="Instantiates spheres for each field." />
        <ui:Toggle label="Compare to Unlit" binding-path="unlitCompare" name="unlit" tooltip="Split the fields into lit and pre-exposed unlit values, which is useful for calibration. Please note that the post-process still applies to both sides." style="flex-direction: row;" />
    </ui:VisualElement>
    <ui:VisualElement name="margin" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); width: auto; height: auto; max-height: 0; margin-top: 10px; margin-bottom: 10px;" />
    <ui:Button text="Move To View" name="moveToViewButton" tooltip="Move and align the color checker within the scene view." />
    <ui:Label text="Object not saved in build." style="font-size: 10px; -unity-text-align: upper-right;" />
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\Tools\Resources\ColorCheckerUI.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\2D\LightBatchingDebugger\LayerBatch.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xsi="http://www.w3.org/2001/XMLSchema-instance" engine="UnityEngine.UIElements" editor="UnityEditor.UIElements" noNamespaceSchemaLocation="../../UIElementsSchema/UIElements.xsd" editor-extension-mode="False">
    <Style src="project://database/Assets/LightBatchingDebugger/LightBatchingDebugger.uss?fileID=7433441132597879392&amp;guid=fd5cb3dd3de574f1585db92b6689f86a&amp;type=3#LightBatchingDebugger" />

    <ui:VisualElement class="BatchList BatchContainer">

        <ui:Label name="BatchIndex"         class="BatchList IndexColumn Batch" />
        <ui:VisualElement name="BatchColor" class="BatchList ColorColumn" />
        <ui:VisualElement name="LayerNames" class="BatchList NameColumn Batch" />

    </ui:VisualElement>

</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\2D\LightBatchingDebugger\LayerBatch.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\2D\LightBatchingDebugger\LightBatchingDebugger.uxml---------------
.
.
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="ParentElement" style="flex-direction:column">
    <Style src="LightBatchingDebugger.uss" />

        <Box class="InfoView Header Bottom">
            <Label name="InfoTitle"/>
        </Box>

        <TwoPaneSplitView fixed-pane-initial-dimension="200">
            <!--Left Split View Layer Batch Info-->
            <VisualElement class="MinSize">
                <Box class="BatchList HeaderContainer">
                    <Label class="BatchList IndexColumn Header" text="Batch ID"/>
                    <VisualElement class="BatchList ColorColumn Container">
                        <Label text="|" class="BatchList ColorColumn Splitter"/>
                    </VisualElement>
                    <Label class="BatchList NameColumn Header" text="Layer Names"/>
                </Box>
                <ListView name="BatchList" class="BatchList List"/>
            </VisualElement>
            <!--Right Split View Lights and Shadows-->
            <VisualElement name="InfoContainer" class="InfoContainer">
                <Label name="InitialPrompt" text="Open Game View and select a batch to begin." class="InitialPrompt"/>
                <TwoPaneSplitView name="InfoView" fixed-pane-initial-dimension="500" orientation="Vertical">
                    <!--Light Split View Top-->
                    <VisualElement class="MinSize">
                        <Box class="BatchList HeaderContainer">
                            <Label name="LightHeader" class="BatchList IndexColumn Header"/>
                        </Box>
                        <ScrollView class="InfoScroller">
                            <Label name="LightLabel1" class="InfoView Content"/>
                            <VisualElement name="LightBubble1" class="InfoView Content PillContainer"/>
                            <Label name="LightLabel2" class="InfoView Content"/>
                            <VisualElement name="LightBubble2" class="InfoView Content PillContainer"/>
                        </ScrollView>
                    </VisualElement>
                    <!--Shadow Caster Split View Bottom-->
                    <VisualElement class="MinSize">
                        <Box class="BatchList HeaderContainer">
                            <Label name="ShadowHeader" class="BatchList IndexColumn Header"/>
                        </Box>
                        <ScrollView class="InfoScroller">
                            <Label name="ShadowLabel1" class="InfoView Content"/>
                            <VisualElement name="ShadowBubble1" class="InfoView Content PillContainer"/>
                            <Label name="ShadowLabel2" class="InfoView Content"/>
                            <VisualElement name="ShadowBubble2" class="InfoView Content PillContainer"/>
                        </ScrollView>
                    </VisualElement>
                </TwoPaneSplitView>
            </VisualElement>
        </TwoPaneSplitView>

        <Box class="InfoView Header Top">
            <Label name="InfoTitle2"/>
        </Box>
    </VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\2D\LightBatchingDebugger\LightBatchingDebugger.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_editor.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <Style src="project://database/Packages/com.unity.render-pipelines.universal/Editor/Converter/converter_editor.uss?fileID=7433441132597879392&amp;guid=0356b27d63ed56a4c9b6fa5b729c89f8&amp;type=3#converter_editor" />
    <ui:VisualElement name="singleConverterVE" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); display: none; padding-left: 19px; padding-right: 19px; padding-top: 20px; padding-bottom: 20px;">
        <ui:VisualElement name="backButtonVE" style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); flex-direction: row; height: 0; flex-shrink: 1; max-height: 22px; display: flex; min-height: 22px; margin-bottom: 10px;">
            <ui:Button tabindex="-1" text="&lt; Back" display-tooltip-when-elided="true" name="backButton" style="width: 110px; margin-left: 3px; display: flex;" />
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="converterEditorMainVE" style="max-width: 100%; min-width: 620px; flex-grow: 1; padding-left: 19px; padding-right: 19px; padding-top: 15px; width: auto; padding-bottom: 20px;">
        <ui:VisualElement name="topInformationVE" style="flex-grow: initial; background-color: rgba(0, 0, 0, 0); height: 146px; flex-shrink: initial; width: auto; min-height: 130px; max-height: 130px;">
            <ui:VisualElement style="flex-direction: row; padding-left: 2px;">
                <ui:DropdownField index="-1" name="conversionsDropDown" style="height: 27px; width: 208px; display: flex; margin-left: 0;" />
                <ui:VisualElement style="flex-grow: 1;" />
                <ui:Button name="containerHelpButton" class="iconButton unity-icon-button" style="width: 16px; height: 16px; padding-left: 0; padding-right: 0; padding-top: 0; padding-bottom: 0; margin-left: 0; margin-right: 0; margin-top: 0;">
                    <ui:Image name="containerHelpImage" class="unity-icon-button" style="background-image: none; flex-grow: 1;" />
                </ui:Button>
            </ui:VisualElement>
            <ui:TextElement text="The Render Pipeline Converter converts project elements from the Built-in Render Pipeline to URP." name="conversionInfo" style="height: 46px; width: 540px; flex-shrink: 0; margin-top: 3px; padding-left: 4px; padding-top: 9px;" />
            <ui:HelpBox message-type="Error" text="This process makes irreversible changes to the project. Back up your project before proceeding." style="flex-grow: 0; padding-top: 2px; padding-bottom: 2px; margin-top: 1px; margin-bottom: 1px;" />
            <ui:HelpBox message-type="Info" text="Click the converters below to see more information." style="flex-grow: 0; padding-top: 2px; padding-bottom: 2px; margin-top: 1px; margin-bottom: 1px;" />
        </ui:VisualElement>
        <ui:ScrollView scroll-deceleration-rate="0,135" elasticity="0,1" name="convertersScrollView" horizontal-scroller-visibility="Hidden" vertical-scroller-visibility="Auto" style="flex-grow: 1; flex-shrink: 1; max-width: none; width: auto; padding-right: 0; padding-left: 0; border-left-width: 0; border-right-width: 0; border-top-width: 0; border-bottom-width: 0; border-top-left-radius: 0; border-bottom-left-radius: 0; border-top-right-radius: 0; border-bottom-right-radius: 0; border-left-color: rgba(0, 0, 0, 0); border-right-color: rgba(0, 0, 0, 0); border-top-color: rgba(0, 0, 0, 0); border-bottom-color: rgba(0, 0, 0, 0); padding-top: 10px;" />
        <ui:VisualElement name="bottomButtonVE" style="flex-direction: row-reverse; flex-shrink: 0; padding-left: 0; padding-right: 0; margin-top: 6px; width: auto; align-items: stretch; flex-grow: 0;">
            <ui:VisualElement style="flex-grow: 0; background-color: rgba(0, 0, 0, 0); min-width: auto; min-height: auto; flex-shrink: 0; width: auto; height: auto;">
                <ui:VisualElement style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); flex-direction: row; flex-shrink: 0;">
                    <ui:Label tabindex="-1" display-tooltip-when-elided="true" style="flex-grow: 1;" />
                    <ui:Button text="Initialize Converters" name="initializeButton" tooltip="Click to initialize all the selected converters." style="flex-direction: column; margin-bottom: 1px; min-width: auto; width: 130px; display: none;" />
                    <ui:Button text=" Convert Assets" name="convertButton" style="flex-direction: column; margin-bottom: 1px; width: 130px; min-width: auto; display: none;" />
                </ui:VisualElement>
                <ui:Button tabindex="-1" text="Initialize And Convert" display-tooltip-when-elided="true" name="initializeAndConvert" style="width: 140px;" />
            </ui:VisualElement>
            <ui:Label style="flex-grow: 1;" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_editor.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <ui:VisualElement name="converterTopVisualElement" style="flex-grow: 1; height: 250px; width: 606px; flex-shrink: 0; border-bottom-width: 0; border-top-width: 0; border-bottom-color: rgb(0, 0, 0); margin-bottom: 20px; padding-bottom: 0; margin-top: 4px; padding-right: 0; border-left-width: 0; border-right-width: 0; border-left-color: rgb(0, 0, 0); border-right-color: rgb(0, 0, 0); border-top-color: rgb(0, 0, 0);">
        <ui:Button tabindex="-1" text="&lt; Back" display-tooltip-when-elided="true" name="backButton" style="align-items: flex-start; width: 75px;" />
        <ui:VisualElement style="height: 24px; width: 613px; flex-direction: row; flex-grow: 0; margin-left: 0; margin-right: 0; margin-top: 0; margin-bottom: 0; flex-shrink: 0; padding-bottom: 0; padding-left: 2px; border-top-width: 0; border-top-color: rgb(0, 0, 0); padding-top: 11px;">
            <ui:Label name="converterName" text="Name Of The Converter" style="width: 143px; -unity-text-align: middle-left; flex-grow: 1; flex-direction: column; max-height: 20%; height: 20px; min-height: 20px; padding-top: 3px; -unity-font-style: bold; padding-left: 4px;" />
            <ui:Label name="converterStats" style="flex-grow: 0; -unity-text-align: middle-right; -unity-font-style: bold; padding-right: 20px;" />
        </ui:VisualElement>
        <ui:VisualElement style="height: 40px; width: 596px; flex-direction: row; flex-grow: 0; flex-shrink: 1; padding-right: 0; padding-left: 2px; padding-top: 6px; overflow: hidden;">
            <ui:Label name="converterStatus" style="-unity-text-align: middle-left; height: 20px;" />
            <ui:Label text="info" name="converterInfo" style="-unity-text-align: middle-left; flex-grow: 1; height: 40px; flex-wrap: nowrap; overflow: visible; white-space: normal; padding-top: 0;" />
            <ui:Label name="converterTime" style="-unity-text-align: middle-left; -unity-font-style: bold; height: 20px;" />
        </ui:VisualElement>
        <ui:VisualElement style="flex-direction: row; height: 19px; flex-grow: 0; margin-top: 0; margin-bottom: 0; padding-right: 14px; width: 608px; flex-shrink: 1;">
            <ui:Label text="&#10;" style="flex-grow: 1;" />
            <ui:Image name="pendingImage" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px;" />
            <ui:Label name="pendingLabel" />
            <ui:Image name="warningImage" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px;" />
            <ui:Label name="warningLabel" />
            <ui:Image name="errorImage" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px;" />
            <ui:Label name="errorLabel" />
            <ui:Image name="successImage" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px;" />
            <ui:Label name="successLabel" style="flex-grow: 0; flex-shrink: 0;" />
        </ui:VisualElement>
        <ui:ListView focusable="true" name="converterItems" show-alternating-row-backgrounds="All" text="Info" style="flex-grow: 1; flex-shrink: 0; height: 100px; width: 602px; padding-bottom: 0; margin-bottom: 0; margin-top: 0;" />
    </ui:VisualElement>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget_item.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <ui:BindableElement style="flex-grow: 1; flex-direction: row; align-items: center; flex-shrink: 0; padding-right: 20px;">
        <ui:Toggle value="true" name="converterItemActive" binding-path="isActive" />
        <ui:Label name="converterItemName" text="name" style="flex-grow: 0; width: 200px; overflow: hidden; padding-left: 4px;" />
        <ui:Label name="converterItemInfo" style="visibility: hidden;" />
        <ui:Label name="converterItemPath" text="path..." style="flex-grow: 0; flex-wrap: nowrap; white-space: nowrap; width: auto; overflow: hidden; padding-left: 21px; flex-shrink: 1;" />
        <ui:Label display-tooltip-when-elided="true" style="flex-grow: 1;" />
        <ui:Image name="converterItemStatusIcon" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px; justify-content: center; flex-grow: 0; width: 16px; height: 16px;" />
        <ui:Image name="converterItemHelpIcon" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px; justify-content: center; flex-grow: 0; width: 16px; height: 16px;" />
    </ui:BindableElement>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget_item.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget_main.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <Style src="project://database/Packages/com.unity.render-pipelines.universal/Editor/Converter/converter_widget_main.uss?fileID=7433441132597879392&amp;guid=012f251bbb5d36246a87499f75dd8b24&amp;type=3#converter_widget_main" />
    <ui:VisualElement name="converterTopVisualElement" class="unity-button" style="flex-grow: 1; height: auto; width: auto; flex-shrink: 0; border-bottom-width: 1px; border-top-width: 1px; border-bottom-color: rgb(60, 60, 60); margin-bottom: 0; padding-bottom: 12px; margin-top: 0; padding-right: 0; border-left-width: 1px; border-right-width: 1px; border-left-color: rgb(60, 60, 60); border-right-color: rgb(60, 60, 60); border-top-color: rgb(60, 60, 60); padding-top: 6px; padding-left: 16px; max-height: 120px; min-height: 120px;">
        <ui:VisualElement style="height: 40px; width: auto; flex-direction: row; flex-grow: 0; margin-left: 0; margin-right: 0; margin-top: 0; margin-bottom: 0; flex-shrink: 0; padding-bottom: 0; padding-left: 2px; border-top-width: 0; border-top-color: rgb(0, 0, 0); padding-top: 11px;">
            <ui:Toggle name="converterEnabled" value="false" tooltip="Enabling this checkbox adds this converter to the initialization and conversion list." style="flex-grow: 0; height: 24px; margin-top: 0; margin-bottom: 0;" />
            <ui:Label name="converterName" text="Name Of The Converter" style="width: auto; -unity-text-align: middle-left; flex-grow: 1; flex-direction: column; max-height: 20%; height: 20px; min-height: 20px; padding-top: 3px; -unity-font-style: bold; padding-left: 4px; cursor: initial;" />
            <ui:Label name="converterStats" style="flex-grow: 0; -unity-text-align: middle-right; -unity-font-style: bold; padding-right: 16px;" />
        </ui:VisualElement>
        <ui:VisualElement style="flex-grow: 1; background-color: rgba(0, 0, 0, 0); flex-direction: row; min-width: auto; min-height: auto; padding-right: 15px; padding-left: 4px;">
            <ui:Image name="converterStateInfoIcon" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px;" />
            <ui:Label tabindex="-1" text="Converter Disabled" display-tooltip-when-elided="true" name="converterStateInfoL" style="max-width: initial; margin-bottom: 4px; margin-left: 5px; flex-grow: 0;" />
            <ui:Label tabindex="-1" display-tooltip-when-elided="true" style="flex-grow: 1;" />
            <ui:Label name="pendingLabel" style="padding-left: 0; -unity-text-align: upper-center; padding-right: 0;" />
            <ui:Image name="pendingImage" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px; visibility: visible; padding-right: 0; margin-right: 8px;" />
            <ui:Label name="warningLabel" style="padding-left: 0; -unity-text-align: upper-center; padding-right: 0;" />
            <ui:Image name="warningImage" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px; padding-right: 0; margin-right: 8px;" />
            <ui:Label name="errorLabel" style="padding-left: 0; -unity-text-align: upper-center; padding-right: 0;" />
            <ui:Image name="errorImage" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px; padding-right: 0; margin-right: 8px;" />
            <ui:Label name="successLabel" style="flex-grow: 0; flex-shrink: 0; padding-left: 0; -unity-text-align: upper-center; padding-right: 0;" />
            <ui:Image name="successImage" style="max-width: 16px; max-height: 16px; min-width: 16px; min-height: 16px;" />
        </ui:VisualElement>
        <ui:VisualElement style="height: 40px; width: auto; flex-direction: row; flex-grow: 0; flex-shrink: 1; padding-right: 0; padding-left: 2px; padding-top: 6px; overflow: hidden;">
            <ui:Label name="converterStatus" style="-unity-text-align: middle-left; height: 20px;" />
            <ui:Label text="info" name="converterInfo" style="-unity-text-align: middle-left; flex-grow: 1; height: 23px; flex-wrap: nowrap; overflow: visible; white-space: normal; padding-top: 0; width: auto; margin-top: 0; margin-right: 10px; margin-left: 0;" />
            <ui:Label name="converterTime" style="-unity-text-align: middle-left; -unity-font-style: bold; height: 20px;" />
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="informationVE" style="flex-direction: row; height: 19px; flex-grow: 0; margin-top: 0; margin-bottom: 0; padding-right: 14px; width: auto; flex-shrink: 1; visibility: visible; display: none; padding-left: 2px;">
        <ui:VisualElement name="allNoneVE" style="flex-grow: initial; background-color: rgba(0, 0, 0, 0); flex-direction: row; margin-top: 1px; margin-left: 4px; display: flex;">
            <ui:Label tabindex="-1" text="ALL" display-tooltip-when-elided="true" name="all" class="not_selected" style="width: 0; max-width: 26px; min-width: 26px; flex-grow: initial;" />
            <ui:Label tabindex="-1" text="NONE" display-tooltip-when-elided="true" name="none" class="not_selected" style="max-width: 36px; min-width: 36px;" />
        </ui:VisualElement>
        <ui:Label text="&#10;" style="flex-grow: 1;" />
    </ui:VisualElement>
    <ui:ListView focusable="true" name="converterItems" show-alternating-row-backgrounds="All" text="Info" style="flex-grow: 1; flex-shrink: 1; height: auto; width: auto; padding-bottom: 0; margin-bottom: 0; margin-top: 0; max-height: initial; min-height: auto; display: none; padding-left: 10px;" />
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget_main.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shaderanalysis\Editor\Viewer\Toolbar.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xsi="http://www.w3.org/2001/XMLSchema-instance" engine="UnityEngine.UIElements" editor="UnityEditor.UIElements" noNamespaceSchemaLocation="../../../../UIElementsSchema/UIElements.xsd" editor-extension-mode="True">
    <uie:Toolbar>
        <uie:ObjectField label="Compute Shader" name="ShaderField" />
        <ui:VisualElement name="ComputeShaderFilterSettings" style="flex-grow: 0.5; flex-direction: row;" />
        <ui:VisualElement name="ShaderFilterSettings" style="flex-grow: 1;" />
        <ui:VisualElement name="MaterialFilterSettings" style="flex-grow: 1;" />
        <uie:ToolbarSpacer style="flex-direction: row; flex-grow: 1; flex-shrink: 0;" />
        <ui:DropdownField label="Platform" choices="PS4,PS5" name="PlatformField" />
        <uie:ToolbarSearchField placeholder-text="Search In Code" name="CodeSearch" style="width: 200px;" />
        <uie:ToolbarButton text="Analyze" name="Analyze" style="white-space: nowrap; -unity-font-style: bold; right: auto; left: 0;" />
    </uie:Toolbar>
    <ui:TabView>
        <ui:Tab label="VGPR Code Analysis" name="VGPRCodeAnalysisTab" />
        <ui:Tab label="VGPR Assembly Analysis" name="VGPRAssemblyAnalysisTab" />
        <ui:Tab label="SGPR Code Analysis" name="SGPRCodeAnalysisTab" />
        <ui:Tab label="SGPR Assembly Analysis" name="SGPRAssemblyAnalysisTab" />
        <ui:Tab label="CBuffer Read Order" name="CBufferReadOrderTab" />
    </ui:TabView>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shaderanalysis\Editor\Viewer\Toolbar.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\GraphInspector.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:sg="UnityEditor.ShaderGraph.Drawing">
    <ui:VisualElement name="content" picking-mode="Ignore">
        <ui:VisualElement name="header" picking-mode="Ignore">
            <ui:VisualElement name="labelContainer" picking-mode="Ignore">
                <ui:Label name="titleLabel" text="" />
                <ui:Label name="subTitleLabel" text="" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:ScrollView class="unity-scroll-view unity-scroll-view--scroll unity-scroll-view--vertical" name ="scrollView"/>
        <ui:VisualElement name="contentContainer" picking-mode="Ignore" />
            <TabbedView name="GraphInspectorView" >
                <TabButton name="NodeSettingsButton" text="Node Settings" target="NodeSettingsContainer" />
                <TabButton name="GraphSettingsButton" text="Graph Settings" target="GraphSettingsContainer" />
                <ui:VisualElement name="GraphSettingsContainer" picking-mode="Ignore" />
                <ui:VisualElement name="NodeSettingsContainer" picking-mode="Ignore">
                    <ui:Label name="maxItemsMessageLabel" picking-mode="Ignore" text ="Max of 20 visible items reached" />
                </ui:VisualElement>
            </TabbedView>
    </ui:VisualElement>
    <sg:ResizableElement pickingMode="Ignore" resizeRestriction="FlexDirection"/>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\GraphInspector.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\GraphSubWindow.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:sg="UnityEditor.ShaderGraph.Drawing">
    <ui:VisualElement name="content" picking-mode="Ignore">
        <ui:VisualElement name="header" picking-mode="Ignore">
            <ui:VisualElement name="labelContainer" picking-mode="Ignore">
                <ui:Label name="titleLabel" text="" />
                <ui:Label name="subTitleLabel" text="" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name="contentContainer" picking-mode="Ignore" />
    </ui:VisualElement>
    <sg:ResizableElement pickingMode="Ignore" resizeRestriction="FlexDirection"/>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\GraphSubWindow.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\HeatmapValuesEditor.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">
    <ui:VisualElement view-data-key="sg-heatmap-editor" class="sg-heatmap">
        <ui:HelpBox
                class="sg-heatmap__help-box"
                message-type="Info"
                text="To use a custom data source for the Heatmap color mode, assign the Heatmap Values Asset in Edit > Project Settings > ShaderGraph.">
        </ui:HelpBox>

        <ui:ListView
                name="colors-list"
                class="sg-heatmap__list"
                view-data-key="colors-list"
                header-title="Categories"
                show-foldout-header="true"
                fixed-item-height="20"
                reorderable="true"
                reorder-mode="Animated"
                show-border="true"
                show-add-remove-footer="true"/>

        <ui:HelpBox
                name="refresh-nodes-hint"
                class="sg-heatmap__help-box"
                message-type="Info"
                text="This project contains nodes that are not listed in this Heatmap Values Asset. Refresh the node list to generate entries for them.">
            <ui:Button name="refresh-nodes-button" text="Refresh Node List"/>
        </ui:HelpBox>

        <ui:MultiColumnListView
                name="subgraph-list"
                class="sg-heatmap__list"
                view-data-key="subgraph-list"
                header-title="Subgraphs"
                fixed-item-height="20"
                show-border="true"
                show-foldout-header="true"
                selection-type="Multiple"
                show-add-remove-footer="true">
            <ui:Columns primary-column-name="subgraph" reorderable="false">
                <ui:Column name="subgraph" title="Subgraph" width="250" optional="false"/>
                <ui:Column name="value" title="Category" width="80" optional="false"/>
            </ui:Columns>
        </ui:MultiColumnListView>

        <ui:MultiColumnListView
                name="nodes-list"
                class="sg-heatmap__list"
                view-data-key="nodes-list"
                header-title="Nodes"
                fixed-item-height="20"
                show-border="true"
                show-foldout-header="true"
                selection-type="Multiple"
                show-bound-collection-size="false">
            <ui:Columns primary-column-name="node" reorderable="false">
                <ui:Column name="node" title="Node" width="250" optional="false"/>
                <ui:Column name="value" title="Category" width="80" optional="false"/>
            </ui:Columns>
        </ui:MultiColumnListView>
    </ui:VisualElement>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\HeatmapValuesEditor.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\NodeSettings.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="mainContainer">
        <ui:VisualElement name="contentContainer">
        </ui:VisualElement>
    </ui:VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\NodeSettings.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\PixelCacheProfiler.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="content">
        <Style path="Styles/PixelCacheProfiler"/>
        <ui:Label name="title" text="Pixel Cache Profiler"/>
        <ui:VisualElement class="row">
            <ui:Label text="Total pixel caches: "/>
            <ui:Label name="totalLabel" text="-"/>
        </ui:VisualElement>
        <ui:VisualElement class="indented row">
            <ui:Label text="Node contents: "/>
            <ui:Label name="totalNodeContentsLabel" text="-"/>
        </ui:VisualElement>
        <ui:VisualElement class="indented row">
            <ui:Label text="Node previews: "/>
            <ui:Label name="totalPreviewsLabel" text="-"/>
        </ui:VisualElement>
        <ui:VisualElement class="indented row">
            <ui:Label text="Inline inputs: "/>
            <ui:Label name="totalInlinesLabel" text="-"/>
        </ui:VisualElement>

        <ui:VisualElement class="row">
            <ui:Label text="Dirty pixel caches: "/>
            <ui:Label name="dirtyLabel" text="-"/>
        </ui:VisualElement>
        <ui:VisualElement class="indented row">
            <ui:Label text="Node contents: "/>
            <ui:Label name="dirtyNodeContentsLabel" text="-"/>
        </ui:VisualElement>
        <ui:VisualElement class="indented row">
            <ui:Label text="Node previews: "/>
            <ui:Label name="dirtyPreviewsLabel" text="-"/>
        </ui:VisualElement>
        <ui:VisualElement class="indented row">
            <ui:Label text="Inline inputs: "/>
            <ui:Label name="dirtyInlinesLabel" text="-"/>
        </ui:VisualElement>
    </ui:VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\PixelCacheProfiler.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Resizable.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="left" pickingMode="Ignore" >
        <ui:VisualElement name="top-left-resize"/>
        <ui:VisualElement name="left-resize"/>
        <ui:VisualElement name="bottom-left-resize"/>
    </ui:VisualElement>
    <ui:VisualElement name="middle" pickingMode="Ignore" >
        <ui:VisualElement name="top-resize"/>
        <ui:VisualElement name="middle-center" pickingMode="Ignore" />
        <ui:VisualElement name="bottom-resize"/>
    </ui:VisualElement>
    <ui:VisualElement name="right"  pickingMode="Ignore" >
        <ui:VisualElement name="top-right-resize"/>
        <ui:VisualElement name="right-resize"/>
        <ui:VisualElement name="bottom-right-resize"/>
    </ui:VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Resizable.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\StickyNote.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements" xmlns:sg="UnityEditor.ShaderGraph.Drawing">
  <ui:VisualElement name="node-border" pickingMode="Ignore" >
    <ui:Label name="title" />
    <ui:TextField name="title-field" />
    <ui:Label name="contents">
      <ui:TextField name="contents-field" />
    </ui:Label>
  </ui:VisualElement>
  <ui:VisualElement name="selection-border" pickingMode="Ignore" />
  <sg:ResizableElement pickingMode="Ignore" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\StickyNote.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\TabButton.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements"
      xmlns:ed="UnityEditor.UIElements">

        <ui:VisualElement class="unity-tab-button__top-bar" picking-mode="Ignore"/>
        <ui:VisualElement class="unity-tab-button__content" picking-mode="Ignore">
            <ui:Label name="Label" class="unity-tab-button__content-label" picking-mode="Ignore"/>
        </ui:VisualElement>

</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\TabButton.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Blackboard\SGBlackboard.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements" xmlns:sg="UnityEditor.ShaderGraph.Drawing">
    <ui:VisualElement name="content" pickingMode="Ignore">
        <ui:VisualElement name="header" pickingMode="Ignore">
            <ui:VisualElement name="labelContainer" pickingMode="Ignore">
                <ui:Label name="titleLabel" text="" />
                <ui:Label name="subTitleLabel" text="Blackboard">
                    <ui:TextField name="subTitleTextField" text="Blackboard" />
                </ui:Label>
            </ui:VisualElement>
            <ui:Button name="addButton" text="+" />
        </ui:VisualElement>
        <ui:VisualElement name="scrollBoundaryTop" pickingMode="Position" />
            <ui:ScrollView class="unity-scroll-view unity-scroll-view--scroll unity-scroll-view--vertical-horizontal" name ="scrollView"/>
                <ui:VisualElement name="contentContainer" pickingMode="Ignore" />
        <ui:VisualElement name="scrollBoundaryBottom" pickingMode="Position"/>
    </ui:VisualElement>
    <sg:ResizableElement pickingMode="Ignore" resizeRestriction="FlexDirection"/>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Blackboard\SGBlackboard.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Blackboard\SGBlackboardCategory.uxml---------------
.
.
<?xml version="1.0" encoding="Windows-1252"?>
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="categoryHeader" pickingMode="Ignore">
        <ui:Foldout name="categoryTitleFoldout" pickingMode="Ignore" text=""/>
        <ui:Label name="categoryTitleLabel" pickingMode="Ignore" text="Text"/>
        <ui:TextField name="textField" text="" />
    </ui:VisualElement>
    <ui:VisualElement name="rowsContainer" pickingMode="Ignore" />
        <ui:VisualElement name="dragIndicator" pickingMode="Ignore" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Blackboard\SGBlackboardCategory.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Blackboard\SGBlackboardField.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements"
    xmlns:gv="UnityEditor.Experimental.GraphView">
    <ui:VisualElement name="contentItem" picking-mode="Ignore">
        <gv:Pill name="pill" picking-mode="Ignore" />
        <ui:Label name="typeLabel" text = "" picking-mode="Ignore" />
    </ui:VisualElement>
    <ui:TextField name="textField" text="" />
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Blackboard\SGBlackboardField.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Blackboard\SGBlackboardRow.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="root" pickingMode="Ignore">
        <ui:VisualElement name="itemRow" pickingMode="Ignore">
            <ui:Button name="expandButton" text = "">
                <ui:Image name="buttonImage" />
            </ui:Button>
            <ui:VisualElement name="itemRowContentContainer" pickingMode="Ignore">
                <ui:VisualElement name="itemContainer" pickingMode="Ignore" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name="propertyViewContainer"/>
    </ui:VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\UXML\Blackboard\SGBlackboardRow.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXAnchoredProfiler.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements">
<ui:VisualElement name="node" pickingMode="Ignore" >
    <ui:Foldout name="header" picking-mode="Ignore" />
    <ui:VisualElement name="lock-button" />
</ui:VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXAnchoredProfiler.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXAttachPanel.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="container" pickingMode="Ignore">
        <ui:Button name="AttachButton" text="Attach" />
        <ui:VisualElement style="flex-direction:Row;margin-top:4px;" pickingMode="Ignore">
            <ui:Label name="PickTitle" text="Select a target" pickingMode="Ignore" />
            <ui:TextField name="PickLabel" pickingMode="Ignore">
                <ui:VisualElement name="VFXIcon" />
                <ui:Button name="PickButton" tooltip="Click here to pick a VFX in the scene">
                    <ui:VisualElement name="PickIcon" />
                </ui:Button>
            </ui:TextField>
        </ui:VisualElement>
    </ui:VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXAttachPanel.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXBlackboardCategory.uxml---------------
.
.
<?xml version="1.0" encoding="Windows-1252"?>
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="header" pickingMode="Ignore">
        <ui:VisualElement name="icon" pickingMode="Ignore" />
        <ui:Label name="title" pickingMode="Ignore" />
        <ui:TextField name="titleEdit" style="display:none" pickingMode="Position" />
    </ui:VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXBlackboardCategory.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXCompileDropdownPanel.uxml---------------
.
.
<?xml version="1.0" encoding="Windows-1252"?>
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:Box>
        <ui:Toggle name="autoCompile" text="Auto Compile" tooltip="Toggles auto-compilation" style="margin-top:4px" />
    </ui:Box>
    <ui:Box>
        <ui:Toggle name="autoReinit" text="Auto Reinit" tooltip="Toggles auto-reinit of attached component when values are changed in Spawner or Init."/>
    </ui:Box>
    <ui:Box style="flex-direction:row">
        <ui:Slider name="prewarmTime" label="Prewarm Time" tooltip="Specifies the duration of the prewarm used during authoring in seconds." showInputField="true" style="margin-bottom:4px;flex-grow:1" direction="Horizontal" />
        <ui:FloatField name="prewarmTimeField" style="width :30px"/>
    </ui:Box>
    <ui:Box>
        <ui:Toggle name="runtimeMode" text="Runtime Mode" tooltip="Forces optimized compilation" />
	</ui:Box>
    <ui:Box>
		<ui:Toggle name="shaderDebugSymbols" text="Shader Debug Symbols" tooltip="Forces debug symbols with generated shaders (Automatically on when globally set in VFX preferences)" />
    </ui:Box>
	<ui:Box>
        <ui:Toggle name="shaderValidation" text="Shader Validation" tooltip="Performs a forced Shader compilation when the effect recompiles" />
    </ui:Box>
    <ui:Box>
        <ui:Button name="resyncMaterial" text="Resync Material" tooltip="Forces VFX to synchronize the underlying internal VFX material of the system with the generated shader" />
    </ui:Box>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXCompileDropdownPanel.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXComponentBoard-bounds-list.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements" xmlns:vfx="UnityEditor.VFX.UI">
  <vfx:VFXComponentBoardBoundsSystemUI name="container" pickingMode="Ignore">
    <vfx:VFXBoundsRecorderField name="system-field">
      <ui:Button name="system-button"/>
    </vfx:VFXBoundsRecorderField>
    <ui:VisualElement name="divider" class="horizontal"/>
<!--    <vfx:VFXEnumField name="bounds-mode"/>-->
  </vfx:VFXComponentBoardBoundsSystemUI>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXComponentBoard-bounds-list.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXComponentBoard-event.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements" xmlns:vfx="UnityEditor.VFX.UI">
  <vfx:VFXComponentBoardEventUI name="container" pickingMode="Ignore">
    <!--<ui:Label text="Event" />-->
    <ui:TextField name="event-name" />
    <ui:Button name="event-send" text="Send" tooltip="Send event to VFX asset" />
  </vfx:VFXComponentBoardEventUI>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXComponentBoard-event.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXComponentBoard.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xmlns:gv="UnityEditor.Experimental.GraphView" xmlns:vfx="UnityEditor.VFX.UI">
    <ui:VisualElement class="mainContainer">
        <ui:VisualElement name="header" pickingMode="Ignore">
            <ui:VisualElement name="labelContainer" pickingMode="Ignore">
                <ui:Label name="titleLabel" text="VFX Control" tooltip="Control the attached VFX GameObject"/>
                <ui:VisualElement name="subtitle" pickingMode="Ignore">
                    <ui:Label name="subTitleLabel" />
                    <ui:Image name="subTitle-icon" />
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:ScrollView class="stretchContentWidth">
            <ui:VisualElement name="component-container">
                <ui:VisualElement name="toolbar" pickingMode="Ignore" >
                    <ui:Button name="stop" tooltip="Stop VFX simulation" />
                    <ui:Button name="play" tooltip="Play VFX simulation" />
                    <ui:Button name="step" tooltip="Next VFX simulation frame" />
                    <ui:Button name="restart" tooltip="Restart VFX simulation" />
                </ui:VisualElement>
                <ui:VisualElement name="play-rate-container" pickingMode="Ignore" class="component-container" >
                    <ui:Label text="Play Rate" />
                    <ui:Slider name="play-rate-slider" direction="Horizontal" tooltip="VFX simulation play rate" />
                    <uie:IntegerField name="play-rate-field"  tooltip="VFX simulation play rate" />
                    <ui:DropdownField name="play-rate-menu" class="MiniDropDown" tooltip="Select a predefined VFX simulation play rates" />
                </ui:VisualElement>
                <ui:VisualElement name="bounds-tool-container" pickingMode="Ignore" class="component-container">
                    <ui:VisualElement name="bounds-actions-container" pickingMode="Ignore" class="row">
                        <ui:Label name="bounds-label" text="Bounds Recording"/>
                        <ui:Button name="record"  tooltip="Start recording VFX bounding box" />
                        <ui:Button name="apply-bounds-button" text="Apply Bounds" tooltip="Apply recorded VFX bounding box"/>
                    </ui:VisualElement>
                    <vfx:VFXBoundsSelector name="system-bounds-container" class="row" />
                </ui:VisualElement>
                <ui:VisualElement name="events-tool-container" pickingMode="Ignore" class="component-container">
                    <ui:Label name="events-label" text="Events"/>
                    <ui:VisualElement name="on-play-stop" pickingMode="Ignore" style="flex-direction:row">
                        <ui:Button name="on-play-button" text="OnPlay" style="flex:1" tooltip="Send 'OnPlay' event" />
                        <ui:Button name="on-stop-button" text="OnStop" style="flex:1" tooltip="Send 'OnStop' event" />
                    </ui:VisualElement>
                    <ui:VisualElement name="events-container" />
                </ui:VisualElement>
                <ui:VisualElement name="debug-tool-container" pickingMode="Ignore" class="component-container">
                    <ui:Label name="debug-label" text="Debug"/>
                    <ui:VisualElement name="debug-container" pickingMode="Ignore">
                        <ui:Button name="debug-modes" text="Debug modes" class="DropDown" tooltip="Select a debugging chart" />
                        <ui:VisualElement name="debug-modes-container" />
                    </ui:VisualElement>
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:ScrollView>
    </ui:VisualElement>
    <gv:ResizableElement pickingMode="Ignore" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXComponentBoard.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXContext.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements" xmlns:vfx="UnityEditor.VFX.UI">
  <vfx:VFXContextBorder name="node-border" pickingMode="Ignore" >
    <ui:VisualElement name="inside" pickingMode="Ignore" >
        <ui:VisualElement name="title" pickingMode="Ignore" class="extremity">
          <ui:Image name="icon" text=""/>
          <ui:Label name="title-label" text="" />
          <ui:Label name="header-space" text="" />
        </ui:VisualElement>
        <ui:Label name="subtitle" pickingMode="Ignore" text="" class="subtitle" />
        <ui:VisualElement name="user-title" pickingMode="Ignore">
            <ui:Label name="user-label" text="" tooltip="Double click to edit"/>
            <ui:TextField name="user-title-textfield" multiline="true"/>
        </ui:VisualElement>
        <ui:VisualElement name="contents" pickingMode="Ignore">
          <ui:VisualElement name="settings"/>
          <ui:VisualElement name="top" pickingMode="Ignore">
            <ui:VisualElement name="input" pickingMode="Ignore" />
            <ui:VisualElement name="divider" class="vertical"/>
            <ui:VisualElement name="output" pickingMode="Ignore" />
          </ui:VisualElement>
          <ui:VisualElement name="collapsible-area" pickingMode="Ignore">
            <ui:VisualElement name="divider" class="horizontal"/>
            <ui:VisualElement name="extension" pickingMode="Ignore" />
          </ui:VisualElement>
        </ui:VisualElement>

        <ui:VisualElement name="block-container" pickingMode="Ignore">
          <ui:Label name="no-blocks" text="Press space to add blocks" pickingMode="Ignore" />
        </ui:VisualElement>

        <ui:Label name="footer" pickingMode="Ignore"  class="extremity" />
      </ui:VisualElement>
    </vfx:VFXContextBorder>

  <ui:VisualElement name="selection-border" pickingMode="Ignore" />
  <ui:VisualElement name="flow-inputs" pickingMode="Ignore" class="flow-container input" />
  <ui:VisualElement name="flow-outputs" pickingMode="Ignore" class="flow-container output" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXContext.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXCreateFromTemplateDropDownPanel.uxml---------------
.
.
<?xml version="1.0" encoding="Windows-1252"?>
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:Button name="createNew" text="Create from template" tooltip="Create a new VFX asset starting from a template" style="margin-top:8px" />
    <ui:Button name="insert" text="Insert template" tooltip="Insert a template into the current VFX asset" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXCreateFromTemplateDropDownPanel.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXEdgeDragInfo.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.Experimental.UIElements" xmlns:vfx="UnityEditor.VFX.UI">
    <ui:Label name="title" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXEdgeDragInfo.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXFilterWindow.uxml---------------
.
.
<ui:UXML
        xmlns:ui="UnityEngine.UIElements"
        xmlns:uie="UnityEditor.UIElements"
        editor-extension-mode="False">
    <ui:TwoPaneSplitView name="SplitPanel"
                         fixedPaneIndex="0"
                         fixed-pane-initial-dimension="350"
                         orientation="Horizontal">
        <ui:VisualElement name="ListOfNodesPanel">
            <ui:VisualElement name="Header">
                <uie:ToolbarSearchField name="SearchField"
                                        tooltip="Start with a '+' character to temporarily include sub-variants in search results"
                                        />
                <ui:Toggle name="ListVariantToggle"
                           tooltip="Toggle sub-variants visibility in search results and favorites"
                           />
                <ui:Toggle name="CollapseButton"
                           tooltip="Show details panel"
                           >
                        <ui:Image name="ArrowImage" />
                </ui:Toggle>
            </ui:VisualElement>
            <ui:TreeView name="ListOfNodes"
                         selectionType="Single"
                         virtualizationMethod="FixedHeight"
                         fixedItemHeight = "24"
                         />
        </ui:VisualElement>
        <ui:VisualElement name="DetailsPanel">
            <ui:VisualElement name="TitleAndDoc">
                <ui:Label name="Title" />
                <ui:Button name="HelpButton"
                           tooltip="Open node documentation in a browser"
                           />
            </ui:VisualElement>
            <ui:TreeView name="ListOfVariants"
                         selectionType="Single"
                         virtualizationMethod="FixedHeight"
                         fixedItemHeight = "24"
                         />
            <ui:VisualElement name="ColorFieldRow">
                <uie:ColorField name="CategoryColorField"
                                show-eye-dropper="false"
                                label="Set Category Color"
                                value="#FFFFFF"
                                />
                <ui:Button name="ResetButton"
                           tooltip="Reset color to default"
                           />
            </ui:VisualElement>
            <ui:Label name="NoSubvariantLabel"
                      text="No variant available for this node"
                      />
        </ui:VisualElement>
    </ui:TwoPaneSplitView>
    <ui:VisualElement name="Resizer" />
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXFilterWindow.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXHelpDropdownPanel.uxml---------------
.
.
<?xml version="1.0" encoding="Windows-1252"?>
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:Box class="section">
        <ui:Label text="Install Additional Samples" class="category" />
        <ui:Button name="installSamples" text="VFX Graph Additions" tooltip="Installs pre built VFX examples" class="indented" />
        <ui:Button name="graphAddition" text="Output Event Helpers" tooltip="Installs helper scripts to add behaviors on VFX events" class="indented" />
        <ui:Button name="learningSamples" text="Learning Samples" tooltip="Installs learning VFX templates" class="indented" />
    </ui:Box>
    <ui:Box class="section alternate">
        <ui:Label text="Documentation" class="category" />
        <ui:Label text="&lt;a href=&quot;https://unity.com/visual-effect-graph&quot;&gt;VFX Graph Home&lt;/a&gt;" tooltip="Open Visual Effect Graph home page" class="indented" />
        <ui:Label text="&lt;a href=&quot;https://discussions.unity.com/tags/c/unity-engine/52/visual-effects-graph&quot;&gt;VFX Graph Discussions&lt;/a&gt;" tooltip="Open Visual Effect Graph Discussions" class="indented" />
    </ui:Box>
    <ui:Box class="section">
        <ui:Label text="Examples and Resources" class="category" />
        <ui:Label text="&lt;a href=&quot;https://github.com/Unity-Technologies/SpaceshipDemo&quot;&gt;[Github] Spaceship Demo&lt;/a&gt;" tooltip="Open Spaceship Demo repository" class="indented" />
        <ui:Label text="&lt;a href=&quot;https://github.com/Unity-Technologies/VisualEffectGraph-Samples&quot;&gt;[Github] VFX Graph Samples&lt;/a&gt;" tooltip="Open VFX Graph Samples repository" class="indented" />
    </ui:Box>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXHelpDropdownPanel.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXNode.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements">
  <ui:VisualElement name="node-border" pickingMode="Ignore" >
    <ui:VisualElement name="title" pickingMode="Ignore" >
      <ui:Label name="title-label" text="" />
      <ui:VisualElement name="collapse-button" text="">
        <ui:VisualElement name="icon" text=""/>
      </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="contents" pickingMode="Ignore">
      <ui:VisualElement name="settings"/>
      <ui:VisualElement name="top" pickingMode="Ignore">
        <ui:VisualElement name="input" pickingMode="Ignore" />
        <ui:VisualElement name="output" pickingMode="Ignore" />
      </ui:VisualElement>
      <ui:VisualElement name="collapsible-area" pickingMode="Ignore">
        <ui:VisualElement name="divider" class="horizontal"/>
        <ui:VisualElement name="extension" pickingMode="Ignore" />
      </ui:VisualElement>
    </ui:VisualElement>
  </ui:VisualElement>
  <ui:VisualElement name="selection-border" pickingMode="Ignore" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXNode.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXParameter.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.UIElements">
  <ui:VisualElement name="node-border" pickingMode="Ignore" >
    <ui:VisualElement name="title" pickingMode="Ignore" >
      <ui:VisualElement name="pill" >
        <ui:Image name="exposed-icon" pickingMode="Ignore" />
        <ui:Label name="title-label" pickingMode="Ignore" />
        <ui:VisualElement name="collapse-button" text="" />
      </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="contents" pickingMode="Ignore">
      <ui:VisualElement name="top" pickingMode="Ignore">
        <ui:VisualElement name="input" pickingMode="Ignore" />
        <ui:VisualElement name="output" pickingMode="Ignore" />
      </ui:VisualElement>
    </ui:VisualElement>
  </ui:VisualElement>
  <ui:VisualElement name="selection-border" pickingMode="Ignore" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXParameter.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXProfilingBoard.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xmlns:gv="UnityEditor.Experimental.GraphView" vfx="UnityEditor.VFX.UI" editor-extension-mode="False">
    <ui:VisualElement class="mainContainer">
        <ui:VisualElement name="header" picking-mode="Ignore">
            <ui:VisualElement name="labelContainer" picking-mode="Ignore">
                <ui:VisualElement name="titleContainer" picking-mode="Ignore">
                    <ui:Image name="title-icon" />
                    <ui:Label name="titleLabel" text="Graph Debug Information"/>
                    <ui:Button name="shortcut-windows"/>
                </ui:VisualElement>
                <ui:VisualElement name="subtitle" picking-mode="Ignore">
                    <ui:Label name="subTitleLabel" />
                    <ui:Image name="subTitle-icon" />
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:ScrollView class="stretchContentWidth">
            <ui:VisualElement name="component-container">
                <ui:VisualElement picking-mode="Ignore">
                    <ui:VisualElement name="general-info-container"/>
                    <ui:Foldout name="cpu-timings-foldout" text="CPU Information" />
                    <ui:Foldout name="gpu-timings-foldout" text="GPU Information" />
                    <ui:Foldout name="texture-info-foldout" text="Texture Usage" />
                    <ui:Foldout name="heatmap-foldout" text="Heatmap Parameters" />
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:ScrollView>
    </ui:VisualElement>
    <gv:ResizableElement pickingMode="Ignore" />
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXProfilingBoard.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXSaveDropDownPanel.uxml---------------
.
.
<?xml version="1.0" encoding="Windows-1252"?>
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:Button name="saveAs" text="Save as..." tooltip="Save as..." style="margin-top:8px" />
    <ui:Button name="showInInspector" text="Show in Inspector" tooltip="Shows currently opened VFX asset in the Inspector" />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXSaveDropDownPanel.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXSystemBorder.uxml---------------
.
.
<UXML xmlns:ui="UnityEngine.Experimental.UIElements" xmlns:vfx="UnityEditor.VFX.UI">
    <ui:Label name="title" />
    <ui:TextField name="title-field" multiline="true"/>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXSystemBorder.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTemplateItem.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <ui:VisualElement name="ItemRoot">
        <ui:Image name="TemplateIcon" />
        <ui:Label name="TemplateName"
                  tabindex="-1"
                  text="Label"
                  display-tooltip-when-elided="true"
        />
    </ui:VisualElement>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTemplateItem.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTemplateSection.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <ui:VisualElement name="ItemRoot">
        <ui:Image name="TemplateIcon" />
        <ui:Label name="TemplateName"
                  tabindex="-1"
                  text="Label"
                  display-tooltip-when-elided="true"
        />
    </ui:VisualElement>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTemplateSection.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTemplateWindow.uxml---------------
.
.
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <ui:TwoPaneSplitView name="SplitPanel"
        fixedPaneIndex="0"
        fixed-pane-initial-dimension="400"
        orientation="Horizontal">
        <ui:VisualElement name="ListOfTemplatesPanel">
            <ui:TreeView name="ListOfTemplates" />
        </ui:VisualElement>
        <ui:VisualElement name="DetailsPanel">
            <ui:Image name="Screenshot" />
            <ui:VisualElement name="TitleAndDoc">
                <ui:Label name="Title"
                          tabindex="-1"
                          text="Title"
                          display-tooltip-when-elided="true"
                          />
                <ui:Button name="HelpButton"
                           tooltip="Open VFX Templates documentation in a browser">
                    <ui:Image name="HelpImage" />
                </ui:Button>
            </ui:VisualElement>
            <ui:ScrollView
                    horizontal-scroller-visibility="Hidden">
                <ui:Label name="Description"
                          tabindex="-1"
                          text="Description"
                          display-tooltip-when-elided="true"
                          />
            </ui:ScrollView>
        </ui:VisualElement>
    </ui:TwoPaneSplitView>
    <ui:VisualElement name="FooterPanel">
        <ui:Button name="InstallButton"
                   text="Install learning templates"
                   tooltip="Imports learning templates featuring visual effects and examples that showcase the functionalities and capabilities of the VFX Graph"
                   />
        <ui:Button name="CancelButton"
                   text="Cancel"
                   display-tooltip-when-elided="true"
                   />
        <ui:Button name="CreateButton"
                   text="Create"
                   display-tooltip-when-elided="true"
                   />
    </ui:VisualElement>
</ui:UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTemplateWindow.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTextEditor.uxml---------------
.
.
<UXML
    xmlns:ui="UnityEngine.UIElements"
    xmlns:uie="UnityEditor.UIElements">
    <ui:ScrollView vertical-scroller-visibility="AlwaysVisible">
        <ui:Label name="emptyMessage" text="Click on the edit button in a Custom HLSL node to open some code." style="display:none" />
        <ui:VisualElement name="container" pickingMode="Ignore" />
    </ui:ScrollView>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTextEditor.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTextEditorArea.uxml---------------
.
.
<UXML
    xmlns:ui="UnityEngine.UIElements"
    xmlns:uie="UnityEditor.UIElements">
    <ui:VisualElement name="container" pickingMode="Ignore">
        <uie:Toolbar>
            <ui:Label name="Label" />
            <uie:ToolbarButton name="Save" tooltip="Save changes" />
            <ui:VisualElement style="flex-grow:1" />
            <uie:ToolbarButton name="Close" tooltip="Close" />
        </uie:Toolbar>
        <ui:TextField name="TextEditor"
                      multiline="True"
                      pickingMode="Ignore">
        </ui:TextField>
    </ui:VisualElement>
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXTextEditorArea.uxml---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXVCSDropDownPanel.uxml---------------
.
.
<?xml version="1.0" encoding="Windows-1252"?>
<UXML xmlns:ui="UnityEngine.UIElements">
    <ui:Button name="getlatest" text="Get Latest" style="margin-top:8px" />
    <ui:Button name="submit" text="Submit" />
    <ui:Button name="revert" text="Revert..." />
</UXML>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uxml\VFXVCSDropDownPanel.uxml---------------
.
.
