 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteEditor.uss---------------


.unity-imgui-container {
    flex: 1 0 0;
}
.moduleWindow {
    position: absolute;
}
.bottomRight {
    bottom: 16px;
    right: 16px;
}
.bottomLeft {
    bottom: 16px;
    left: 0;
}
.topLeft {
    top: 0;
    left: 0;
}
.topRight {
    top: 0;
    right: 16px;
}
.bottomRightFloating {
    bottom: 24px;
    right: 24px;
}
.bottomLeftFloating {
    bottom: 24px;
    left: 8px;
}
.topLeftFloating {
    top: 8px;
    left: 8px;
}
.topRightFloating {
    top: 8px;
    right: 24px;
}
#moduleViewElement{
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
}
#spriteFrameInspector{
    min-width: 330px;
    min-height: 170px;
    flex: 1 0 0;
}
#spriteFrameModuleInspector {
    min-width: 330px;
    min-height: 170px;
    flex: 1 0 0;
}

#spriteFrameModuleInspector Label{
    -unity-text-align: upper-left;
}

#spriteFrameModuleInspector #unity-text-input{
    -unity-text-align: upper-left;
    min-height : 18px;
}

.spriteFrameModuleInspectorField {
    flex-direction: row;
    min-height: 18px;
    padding-bottom: 2px;
    max-width : 320px;
    min-width : 320px;
 }

.spriteFrameModuleInspectorField > Label {
    width: 130px;
}

.spriteFrameModuleInspectorField > EnumField > .unity-label {
    width: 130px;
    min-width:0;
}


.spriteFrameModuleInspectorField > EnumField {
    flex: 1 0 0;
    margin-right: 4px;
}
.spriteFrameModuleInspectorField > #SpriteName {
    flex: 1 0 0;
    flex-direction : row;
}

#spriteName {
    flex : 1;
}

#spriteName > Label {
    width: 130px;
    min-width:0;
}

/* Sprite Editor Window Module Drop Down*/
#spriteEditorWindowModuleDropDown{
    flex: 0 0 auto;
    max-width: 120px;
    width: 120px;
    margin: 0 1px 1px 0;
    border-width: 0 0 0 0;
}

/* Module IMGUI Toolbar for backward compatibility*/
#spriteEditorWindowModuleToolbarIMGUI{
    height : 21px;
    flex: 1 1 0;
    margin-top : 0;
}

/* Module UI-Toolkit Toolbar */
#spriteEditorWindowModuleToolbarContainer{
    overflow: hidden;
    flex-direction: row;
}

/* Toolbar Apply and Revert button  */
#spriteEditorWindowApplyRevert{
    flex: 0 0 auto;
    overflow: hidden;
}

/* Toolbar Alpha And Zoom IMGUI  */
#spriteEditorWindowAlphaZoom{
    flex: 0 0 auto;
    width:170px;
    overflow: hidden;
}

#spriteEditorWindowMainView{
    flex: 1 0 0;
    overflow: hidden;
}

#polygonShapeWindow{
    width : 155px;
    min-height : 45px;
}
#polygonShapeWindowFrame{
    padding-top: 5px;
    padding-bottom: 5px;
    padding-left: 5px;
    padding-right: 5px;
    flex: 1 0 0;
    flex-direction : column;
}
#polygonShapeWindowFrame > .labelIntegerField{
    flex: 1 0 0;
    flex-direction: row;
}
#polygonShapeWindowFrame > .labelIntegerField > Label{
    margin-right: 0;
    margin-top: 4px;
    min-width:0;
    align-self:center;
}
#polygonShapeWindowFrame > .labelIntegerField > IntegerField{
    flex: 1 0 auto;
    margin-top: 4px;
    margin-bottom: 4px;
}
#polygonShapeWindowFrame > Button{
    height : 18px;
    align-self: flex-end;
}
#polygonShapeWindowFrame > #warning{
    flex-direction : row;
    border-color: #a2a2a2;
    border-left-width : 1px;
    border-right-width : 1px;
    border-top-width : 1px;
    border-bottom-width : 1px;
    margin-top: 4px;
    margin-left: 4px;
    margin-bottom: 4px;
    margin-right: 4px;
}
#polygonShapeWindowFrame > #warning > Image{
    background-image : resource("console.warnicon.png");
    width : 32px;
    height : 32px;
}
#polygonShapeWindowFrame > #warning > #warningLabel{
    white-space : normal;
    font-size : 9px;
    flex: 1 0 0;
}
.unity-composite-field__input{
    flex-shrink: 1;
    flex-grow: 1;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteEditor.uss---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteEditorToolbar.uss---------------


/* Toolbar */
#spriteEditorWindowToolbarContainer{
    width: 100%;
    flex-direction: row;
    height: 21px;
    border-bottom-width: 1px;
    border-bottom-color: var(--unity-colors-toolbar-border);
    background-color: var(--unity-colors-toolbar-background);
}

/* Sprite Editor Window Module Drop Down*/
#spriteEditorWindowModuleDropDown{
    flex: 0 0 auto;
    width: 120px;
    height:100%;
    margin: 0 0 0 0;
    border-width: 0 1px 0 0;
    border-color: var(--unity-colors-toolbar-border);
    background-color: var(--unity-colors-toolbar-background);
}

#spriteEditorWindowModuleDropDown .unity-base-field__input {
    background-color: var(--unity-colors-toolbar-background);
}

#spriteEditorWindowModuleDropDown PopupTextElement {
    text-overflow: ellipsis;
    background-color: var(--unity-colors-toolbar-background);
}

#spriteEditorWindowModuleDropDown .unity-base-popup-field__arrow {
    background-color: var(--unity-colors-toolbar-background);
}

/* Module IMGUI Toolbar for backward compatibility*/
#spriteEditorWindowModuleToolbarIMGUI{
    height : 21px;
    flex: 1 1 0;
    margin-top : 0;
    overflow: hidden;
}

/* Module UI-Toolkit Toolbar */
#spriteEditorWindowModuleToolbarContainer{
    overflow: hidden;
    flex-direction: row;
}

/* Toolbar Apply and Revert button  */
#spriteEditorWindowApplyRevert{
    flex: 0 0 auto;
    overflow: hidden;
}

/* Toolbar Alpha And Zoom IMGUI  */
#spriteEditorWindowAlphaZoom{
    flex: 0 0 auto;
    width:170px;
    overflow: hidden;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteEditorToolbar.uss---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteOutlineToolOverlayPanelStyle.uss---------------


/**********************************************************************************************************************/
/* SpriteOutlineToolOverlayPanel                                                                                      */
/**********************************************************************************************************************/

#SpriteOutlineToolOverlayPanel {
  height: 170px;
  width: 300px;
}

#unity-content-container {
    flex:1 0 auto;
}

.form-row {
    max-height : 20px;
    flex-direction: row;
    margin-left : 0;
    margin-right : 0;
    margin-top : 0;
    margin-bottom : 0;
    flex : 1 0 auto;
}

.form-row-space {
    height : 5px;
}

.form-editor {
    flex-direction: row;
    flex: 6;
}

.form-popup {
    margin-left : 0px;
    flex : 1 0 auto;
}

.form-popup .unity-enum-field,
.form-popup .unity-popup-field {
    flex : 1 0 auto;
}

.form-popup .unity-label {
    min-width : auto;
    flex : 4;
}

.form-popup .unity-enum-field__input,
.form-popup .unity-popup-field__input {
    flex : 6;
    min-width : auto;
}

.form-toggle {
    margin-left : 0px;
    flex : 1;
}

.form-toggle .unity-toggle__input {
    justify-content : flex-end;
}

.form-integerfield {
    margin-left : 0px;
    flex : 1;
}

.form-integerfield IntegerInput {
    margin-left : 0px;
    flex : 6;
}

.named-slider {
    flex: 3;
    margin-left : 0;
}

.named-slider > .unity-base-slider__label {
    padding-top: 0;
    min-width: 105px;
    width: 105px;
    max-width: 105px;
}

.named-slider > .unity-base-slider__input {
    min-width: 130px;
    width: 130px;
    max-width: 130px;
}

.slider-field {
    width: 41px;
}

.toggle-label
{
    min-width: 105px;
    width: 105px;
    max-width: 105px;
}

Label {
    flex: 4;
    margin-top : 2px;
    margin-bottom : 2px;
}

Slider {
    flex: 7;
    margin-top :2px;
    margin-right : 10px;
    margin-bottom :2px;
}

Button {
    flex : 1 0 auto;
    margin-left: 1px;
    margin-right: 1px;
    margin-top: 1px;
    margin-bottom: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-top-width: 1px;
    border-top-left-radius: 3px;
    border-top-right-radius: 3px;
    border-bottom-left-radius: 3px;
    border-bottom-right-radius: 3px;
    padding-left: 2px;
    padding-right: 2px;
    padding-bottom: 2px;
    padding-top: 2px;
}

Toggle {
    align-self : center;
    margin-bottom: 4px;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.sprite\Editor\UI\SpriteEditor\SpriteOutlineToolOverlayPanelStyle.uss---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\GridPaintPaletteWindow.uss---------------
.
.
.unity-imgui-container {
    flex: 0 0 0;
}
.bottomRight {
    bottom: 16px;
    right: 16px;
}
.bottomLeft {
    bottom: 16px;
    left: 0;
}
.topLeft {
    top: 0;
    left: 0;
}
.topRight {
    top: 0;
    right: 16px;
}
.bottomRightFloating {
    bottom: 24px;
    right: 24px;
}
.bottomLeftFloating {
    bottom: 24px;
    left: 8px;
}
.topLeftFloating {
    top: 8px;
    left: 8px;
}
.topRightFloating {
    top: 8px;
    right: 24px;
}

.unity-grid-paint-palette-window {
    background-color: var(--unity-colors-tab-background-checked);
}

.unity-tilepalette-activetargets {
    min-height: 30px;
    align-items: center;
    justify-content: center;
}

.unity-tilepalette-toolbar {
    margin-top: 4px;
    margin-bottom: 1px;
    align-items: center;
    justify-content: center;
}

.unity-toolbar-button {
    background-color: var(--unity-colors-button-background);
    border-width: 0;
    min-height: var(--toolbar-button-height);
    left: 0;
}
.unity-toolbar-button:hover {
    background-color: var(--unity-colors-button-background-hover);
}
.unity-toolbar-button:checked {
    background-color: var(--unity-overlay-buttons-on-color);
}

.unity-toolbar-button:active {
    background-color: var(--unity-colors-button-background-pressed);
}

.unity-toolbar-toggle:checked {
    background-color: var(--unity-overlay-buttons-on-color);
}

.unity-editor-toolbar__button-strip-element {
    left: 0;
    margin-right: 1px;
}

.unity-tilepalette-splitview .unity-tilepalette-element .unity-tilepalette-element-brushelement {
    visibility: visible;
    position: relative;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\GridPaintPaletteWindow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\GridPaintPaletteWindowDark.uss---------------
.
.
﻿.unity-grid-paint-palette-window {
    --unity-overlay-buttons-on-color: #46607C;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\GridPaintPaletteWindowDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\GridPaintPaletteWindowLight.uss---------------
.
.
﻿.unity-grid-paint-palette-window {
    --unity-overlay-buttons-on-color: #8CB9F3;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\GridPaintPaletteWindowLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\TilePaletteOverlay.uss---------------
.
.
#titleLabel {
    font-size: 19px;
    -unity-font-style: bold;
    padding-top: 0px;
    padding-left: 2px;
    padding-right: 2px;
    padding-bottom: 2px;
}

.unity-tilepalette-focus-dropdown {
    flex-grow: 1;
    flex-direction: row;
}

.unity-tilepalette-brushes-button{
    flex-grow: 1;
    flex-direction: row;
}

.unity-tilepalette-toolbar-strip {
    flex-grow: 1;
    flex-direction: row;
}

.unity-tilepalette-splitview {
    flex: 1;
    background-color: var(--unity-colors-inspector_titlebar-background);
}

.unity-tilepalette-splitview .unity-tilepalette-splitview-brushes {
    flex-grow: 1;
    flex-direction: column;
}

.unity-tilepalette-element {
    padding-top: 2px;
    padding-bottom: 5px;
    padding-left: 5px;
    padding-right: 5px;
    border-top-width: 1px;
    border-top-color: #000000;
    overflow: hidden;
    flex-grow: 1;
    background-color: var(--unity-colors-window-background);
}

.overlay--floating .unity-tilepalette-element-resizable {
    cursor: resize-up-left;
}

.unity-tilepalette-element-toolbar {
    flex-direction: row;
    overflow: hidden;
    min-height: 24px;
}

.unity-tilepalette-element-toolbar-right {
    overflow: hidden;
    flex-grow: 1;
    flex-direction: row-reverse;
}

.unity-tilepalette-clipboard-element {
    flex-grow: 1;
    overflow: hidden;
}

.unity-tilepalette-clipboard-firstuser-element {
    flex-grow: 1;
    overflow: hidden;
    justify-content: center;
}

.unity-tilepalette-clipboard-error-element {
    flex-grow: 1;
    overflow: hidden;
    justify-content: center;
}

.unity-tilepalette-clipboard-error-element .unity-label {
    align-self: center;
}

.unity-toolbar-overlay {
    flex-direction: row;
    align-items: center;
}

.unity-tilepalette-element .unity-tilepalette-element-brushelement {
    visibility: hidden;
    position: absolute;
}

.unity-tilepalette-brushes-icon {
    margin: 2px;
    min-width: 16px;
}

.unity-tilepalette-brushes-field__label {
    min-width: 40px;
    width: 40px;
    max-width: 40px;
}

.unity-tilepalette-brushes-field__input {
    min-width: 150px;
    width: 150px;
    max-width: 150px;
}

.unity-tilepalette-brushes-label {
    margin: 2px;
    padding-left: 3px;
    padding-right: 3px;
}

.unity-tilepalette-activepalette-icon {
    margin: 2px;
    width: 16px;
    height: 16px;
}

.unity-tilepalette-activepalettes-field__label {
    min-width: 50px;
    width: 50px;
    max-width: 50px;
}

.unity-tilepalette-activepalettes-field__input {
    min-width: 156px;
    flex-grow: 1;
}

.unity-tilepalette-activetargets {
    flex-direction: column;
}

.unity-tilepalette-activetargets-popup {
    flex-direction: row;
    border-width: 1px;
}

.unity-tilepalette-activetargets-info {
    flex-direction: row;
    border-width: 1px;
    border-radius: 2px;
    border-color: var(--unity-colors-helpbox-border);
    background-color: var(--unity-colors-helpbox-background);
    padding: 1px;
    margin: 1px;
}

.unity-tilepalette-activetargets-info > .unity-label {
    align-self: center;
}

.unity-tilepalette-activetargets-info__create {
    margin-left: 2px;
    margin-right: 2px;
    min-height: 16px;
    height: 16px;
    min-width: 16px;
    width: 16px;
    background-image: resource("console.infoicon.sml");
}

.unity-tilepalette-activetargets-field {
    margin-left: 1px;
    margin-right: 1px;
    margin-top: 0;
    margin-bottom: 0;
    align-items: center;
}

.unity-tilepalette-activetargets-field__label {
    min-width: 90px;
    width: 90px;
    max-width: 90px;
}

.unity-tilepalette-activetargets-icon {
    margin: 2px;
    width: 16px;
    height: 16px;
}

.unity-tilepalette-activetargets-field__input {
    min-width: 200px;
    width: 200px;
    padding-left: 3px;
    padding-right: 3px;
    border-top-left-radius: 2px;
    border-top-right-radius: 2px;
    border-bottom-left-radius: 2px;
    border-bottom-right-radius: 2px;
}

.unity-tilepalette-activetargets-field__warning {
    margin-left: 2px;
    margin-right: 2px;
    min-height: 16px;
    height: 16px;
    min-width: 16px;
    width: 16px;
    background-image: resource("console.warnicon.sml");
}

.unity-tilepalette-brushesdropdown-toggle {
    min-height: 16px;
    min-width: 16px;
}

.unity-tilepalette-brushinspector {
    flex-grow: 1;
}

.unity-overlay .unity-tilepalette-brushinspector {
    width: 290px;
    max-height: 400px;
}

.unity-tilepalette-brushinspectorpopup {
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-top-width: 1px;
}

.unity-tilepalette-brushinspectorpopup__horizontal {
    flex-direction: row;
}

.unity-tilepalette-brushinspectorpopup__right {
    overflow: hidden;
    flex-grow: 1;
    flex-direction: row-reverse;
}

.unity-tilepalette-brushinspectorpopup .unity-label {
    -unity-font-style: bold;
}

.unity-tilepalette-splitview-brushes-toolbar {
    min-height: 24px;
    flex-direction: row;
}

.unity-tilepalette-splitview-brushes-toolbar-right {
    overflow: hidden;
    flex-grow: 1;
    flex-direction: row-reverse;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle {
    border-width: 0px;
    padding-left: 12px;
    padding-right: 12px;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:checked {
    border-bottom-width: 2px;
    border-bottom-color: darkcyan;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle > .unity-text-element {
    -unity-text-align: middle-center;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle > .unity-label {
    padding-left: 2px;
    padding-right: 2px;
}

.unity-tilepalette-brushpick-item__icon {
    position: absolute;
    height: 16px;
    width: 16px;
    bottom: 0;
    right: 0;
    border-width: 1px;
}

.unity-tilepalette-brushpick-type__icon {
    height: 16px;
    width: 16px;
}

.unity-list-view {
    flex-grow: 1;
}

.unity-list-view .unity-tilepalette-brushpick-type {
    flex-grow: 1;
    flex-direction: row;
    align-items: center;
}

.unity-list-view .unity-tilepalette-brushpick-type .unity-label{
    margin-left: 2px;
}

.unity-list-view .unity-tilepalette-brushpick-item {
    flex-direction: row;
    align-items: center;
}

.unity-list-view .unity-tilepalette-brushpick-item .unity-label{
    margin-left: 4px;
}

.unity-list-view .unity-tilepalette-brushpick-item__icon {
    visibility: hidden;
}

.unity-grid-view__item .unity-image {
    flex-grow: 1;
    background-color: #525252;
}

.unity-grid-view__item--selected.unity-image {
    border-color: #3A72B0;
    border-width: 2px;
}

.unity-grid-view__item--selected.u2d-renameable-label {
    background-color: #3A72B0;
}

.unity-tilepalette-brushpick-view-toolbar {
    flex-direction: row;
    min-height: 24px;
}

.unity-tilepalette-brushpick {
    flex-grow: 1;
}

.overlay--floating .unity-tilepalette-brushpick {
    border-bottom-width: 5px;
    border-left-width: 5px;
    border-right-width: 5px;
}

.unity-tilepalette-brushpick > .unity-tilepalette-label-toolbar {
    border-width: 1px;
    min-height: 24px;
    max-height: 24px;
    flex-direction: row;
}

.unity-tilepalette-brushpick > .unity-tilepalette-label-toolbar > .unity-label {
    font-size: 12px;
    -unity-text-align: middle-left;
    margin-left: 2px;
}

.unity-overlay .unity-tilepalette-brushpick > .unity-tilepalette-label-toolbar .unity-toolbar-search-field {
    flex-shrink: 6;
}

.unity-overlay .unity-tilepalette-brushpick > .unity-tilepalette-label-toolbar .unity-toolbar-search-field > .unity-button {
    background-color: transparent;
    min-height: initial;
    left: initial;
    border-width: initial;
}

.unity-overlay .unity-tilepalette-brushpick .unity-scroller--horizontal {
    height: 0px;
    visibility: hidden;
}

.unity-tilepalette-brushpick .unity-search-field-base {
    flex-grow: 0.2;
    width: 160px;
}

.unity-tilepalette-brushpick-view-toolbar .unity-slider {
    flex-grow: 0.2;
}

.unity-tilepalette-brushpick-view-toolbar > .unity-toolbar-toggle {
    margin-top: 2px;
    margin-bottom: 2px;
    width: 20px;
    margin-left: 1px;
    margin-right: 1px;
}

.unity-tilepalette-brushpick .unity-tilepalette-brushpick-emptyview {
    flex-grow: 1;
    align-self: center;
    justify-content: center;
}

.unity-tilepalette-brushpick .unity-tilepalette-brushpick-emptyview .unity-label {
    margin: 2px;
    font-size: 16px;
    white-space: normal;
}

.unity-tilepalette-brushpick .unity-grid-view {
    flex-grow: 1;
}

.unity-tilepalette-brushpick-lastused {
    position: absolute;
    visibility: hidden;
}

.unity-overlay .unity-tilepalette-brushpick-type .unity-label {
    position: absolute;
    visibility: hidden;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\TilePaletteOverlay.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\TilePaletteOverlayDark.uss---------------
.
.
.unity-tilepalette-brushinspectorpopup
{
    border-left-color: rgb(112, 112, 112);
    border-right-color: rgb(112, 112, 112);
    border-top-color: rgb(112, 112, 112);
    border-bottom-color: rgb(112, 112, 112);
}

.unity-tilepalette-brushpick > .unity-tilepalette-label-toolbar {
    background-color: #414141;
    border-color: black;
}

.unity-tilepalette-brushpick .list-button
{
    background-image: resource('d_ListView');
}

.unity-tilepalette-brushpick .grid-button
{
    background-image: resource('d_GridView');
}

.unity-tilepalette-brushpick-item__icon {
    background-color: #3C3C3C;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle > .unity-text-element {
    color: #999999;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:hover > .unity-text-element{
    color: #D2D2D2;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:checked {
    border-bottom-width: 2px;
    border-bottom-color: #DEDEDE;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:checked > .unity-text-element {
    color: #DEDEDE;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:checked:hover > .unity-text-element {
    color: #FFFFFF;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\TilePaletteOverlayDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\TilePaletteOverlayLight.uss---------------
.
.
.unity-tilepalette-brushinspectorpopup
{
    border-left-color: rgb(130, 130, 130);
    border-right-color: rgb(130, 130, 130);
    border-top-color: rgb(130, 130, 130);
    border-bottom-color: rgb(130, 130, 130);
}

.unity-tilepalette-brushpick > .unity-tilepalette-label-toolbar {
    border-color: gray;
}

.unity-tilepalette-brushpick .list-button
{
    background-image: resource('ListView');
}

.unity-tilepalette-brushpick .grid-button
{
    background-image: resource('GridView');
}

.unity-tilepalette-brushpick-item__icon {
    background-color: #CBCBCB;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle > .unity-text-element {
    color: #616161;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:hover > .unity-text-element{
    color: #090909;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:checked {
    border-bottom-width: 2px;
    border-bottom-color: #373737;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:checked > .unity-text-element {
    color: #373737;
}

.unity-tilepalette-splitview-brushes-toolbar > .unity-toolbar-toggle:checked:hover > .unity-text-element {
    color: #090909;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\TilePaletteOverlayLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\External\GridView.uss---------------
.
.
.unity-grid-view__row {
    flex-direction: row;
    flex-grow: 0;
    flex-shrink: 0;
    flex-basis: auto;
    justify-content: space-around;
}

.unity-grid-view__item {
    border-color: rgba(11, 124, 213, 0);
    border-width: 3px;
    margin: 1px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\External\GridView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\External\RenameableLabel.uss---------------
.
.
.u2d-renameable-label {
    margin: 1px;
    padding: 1px;
    border-radius: 4px;
    min-height: 18px;
    height: 18px;
    max-height: 18px;
    flex-direction: row;
    flex-grow: 1;
    align-self: center;
}

.u2d-renameable-label > .unity-label {
    overflow: hidden;
    text-overflow: ellipsis;
    -unity-text-align: middle-center;
}

.u2d-renameable-label > .unity-text-field {
    margin: 0px;
    overflow: hidden;
    -unity-text-align: middle-center;
}

.u2d-renameable-label > .unity-text-field > .unity-text-field__input {
    padding: 0px;
    -unity-text-align: middle-center;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\UI\External\RenameableLabel.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.multiplayer.center\Editor\MultiplayerCenterWindow\UI\dark.uss---------------
.
.
/*
 * CSS variables specific to dark mode (pro skin)
 */
:root {
    --package-icon: url('Icons/d_Package.png');
    --questionnaire-icon: url('Icons/d_Questionnaire.png');
    --package-manager-icon: url('Icons/d_PackageManager.png');
    --package-installed-icon: url('Icons/d_PackageInstalled.png');
    --info-icon: resource('d__Help');
    --comment-color: dimgrey;
    --highlight-background-color: #2a2a2a;
    --colors-incompatible-background: #FFC107;
    --recommendation-badge-color: #69E39F;
    --card-poster-image-bg-color: #222222;
    --theme-slider-background-color:#5E5E5E; /* From Default common dark uss */
    --badge-color-grey:#C4C4C4; /* From Default common dark uss, feedback color */
    --pre-release-badge-color: #FFC107;
    --pre-release-badge-color-bg: #1D1E1F;
    --link-color: var(--unity-colors-label-text-focus);
    --tab-button-highlight-color: #DEDEDE;
    --onboarding-button-selected-text-color: #EEEEEE;
    --three-dot-icon: resource("UIBuilderPackageResources/Icons/Dark/Inspector/Status/Settings.png");
    --spinner-icon-big: url('Icons/d_Loading.png');
}.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.multiplayer.center\Editor\MultiplayerCenterWindow\UI\dark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.multiplayer.center\Editor\MultiplayerCenterWindow\UI\light.uss---------------
.
.
/*
 * CSS variables specific to light mode 
 */
:root {
    --package-icon: url('Icons/Package.png');
    --questionnaire-icon: url('Icons/Questionnaire.png');
    --package-manager-icon: url('Icons/PackageManager.png');
    --package-installed-icon: url('Icons/PackageInstalled.png');
    --info-icon: resource('_Help');
    --colors-incompatible-background: #C99700;
    --comment-color: darkgray;
    --highlight-background-color: #f2f2f2;
    --card-poster-image-bg-color: #555555;
    --recommendation-badge-color: #00876A;
    --theme-slider-background-color: #8F8F8F; /* From Default common light uss */
    --badge-color-grey:#555555; /* From Default common light uss, feedback color */
    --pre-release-badge-color: #C99700;
    --pre-release-badge-color-bg: #F0F0F0;
    --link-color: #0000f5;
    --tab-button-highlight-color: black;
    --onboarding-button-selected-text-color: white;
    --three-dot-icon: resource("UIBuilderPackageResources/Icons/Light/Inspector/Status/Settings.png");
    --spinner-icon-big: url('Icons/Loading.png');
}

.color-recommendation-badge {
   background-color: #eeeeee;
}.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.multiplayer.center\Editor\MultiplayerCenterWindow\UI\light.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.multiplayer.center\Editor\MultiplayerCenterWindow\UI\MultiplayerCenterWindow.uss---------------
.
.
#root {
    --dark-grey: var(--unity-colors-window-background);
    --standard-grey: var(--unity-colors-app_toolbar_button-background-hover);
    --standard-background-color: var(--unity-colors-window-background);
    --light-grey: var(--unity-colors-dropdown-background);
    --border-color: var(--unity-colors-dropdown-border);
    --card-background: var(--unity-colors-inspector_titlebar-border_accent);
    
    --doc-separator-color: #5D5D5D;
    
    --tab-button-bar-height: 32px;
    
    --bottom-bar-height: 56px;
    
    --card-width: 300px;
    --card-min-width: 200px;
    --card-poster-icon-width: 92px;
    --card-poster-icon-height: 64px;
    
    --checkmark-icon: url("Icons/Check.png");
    --questionnaire-empty-view-icon: url('Icons/EmptyViewIcon.png');
}

.color-recommendation-badge {
    color: var(--recommendation-badge-color);
    border-color: var(--recommendation-badge-color);
}

.icon{
    min-width: 16px;
    max-width: 16px;
    min-height: 16px;
    max-height: 16px;
}

Label{
    overflow: hidden;
    white-space: normal;
}

.view-headline{
    font-size: 14px;
    padding-bottom: 16px;
}

.recommendation-view-headline{
    font-size: 14px;
    padding-bottom: 16px;
    margin-left: 10px;
}

.icon-package-manager{
    background-image: var(--package-manager-icon);
}

.icon-questionmark {
    background-image: var(--info-icon);
}

.icon-three-dots {
    background-image: var(--three-dot-icon);
    -unity-background-scale-mode: scale-to-fit;
}

.icon-package-installed{
    margin-left: 4px;
    background-image: var(--package-installed-icon);
}

.icon-NGO{background-image:url('Icons/Ngo.png')}
.icon-N4E{background-image:url('Icons/N4E.png')}
.icon-LS{background-image:url('Icons/ClientHosted.png')}
.icon-DS{background-image:url('Icons/DedicatedServer.png')}
.icon-DA{background-image:url('Icons/DistributedAuthority.png')}
.icon-CustomNetcode{background-image:url('Icons/CustomNetcode.png')}
.icon-NoNetcode{background-image:url('Icons/NoNetcode.png')}
.icon-CloudCode{background-image:url('Icons/CloudCode.png')}

/*Utilities*/

.processing{
    background-image: var(--spinner-icon-big);
    rotate: 360000deg;
    transition-property: rotate;
    transition-timing-function: linear;
    transition-duration: 2000s;
    min-width: 64px;
    max-width: 64px;
    min-height: 64px;
    max-height: 64px;
    position: absolute;
    top: 50%; 
    left: 50%;
    translate: -50% -50%;
}

.flex-wrap{
    flex-wrap: wrap;
}

.color-grey {
    color: var(--badge-color-grey);
    opacity: 0.8;
    border-color: var(--badge-color-grey);
}

.highlight-background-color {
    background-color: var(--standard-background-color);
}

.next-step-button {
    padding: 6px;
    margin-top: 6px;
    min-height: 32px;
    max-height: 32px;
}

.packageIcon {
    background-image: var(--package-icon);
    -unity-background-scale-mode: scale-to-fit;
    margin-right: 4px;
    background-size: 16px 16px;
}

.questionnaireIcon {
    background-image: var(--questionnaire-icon);
    -unity-background-scale-mode: scale-to-fit;
    background-size: 14px 14px;
}

/* Questionnaire view  */

#questionnaire-view {
    margin: 10px;
    padding-bottom: 4px;
}


#questionnaire-view Label {
    padding-left: 0px;
    padding-right: 0px;
}

#questionnaire-view Toggle {
    margin-left: 0px;
    margin-right: 0px;
}

#advanced-questions > Toggle {
    -unity-font-style: bold;
}

/*question-view  One single question in question-view*/

.question-view{
    margin-bottom: 4px;
}

#advanced-questions .question-view > Label {
    margin-bottom: 2px;
}

.question-view Toggle {
    background-color: var(--dark-grey);
}

.question-view .question-text {
    margin: 5px;
    white-space: normal;
}

.question-view .unity-radio-button__label {
    left: 32px;
    white-space: nowrap;
    text-overflow: ellipsis;
    overflow: hidden;
}

.question-view .unity-radio-button__input {
    position: absolute;
    left: 16px;
}

.mandatory-question Label {
    padding-bottom: 4px;
    -unity-font-style: bold;
}

.question-section {
}

.question-section__no-scrollbar{
    align-content: flex-start;
    flex-shrink: 0;
}

/*bottom bar - holding the install button, spinning icon and the package count */
#bottom-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding-left: 15px;
    padding-right: 15px;
    max-height: var(--bottom-bar-height);
    min-height: var(--bottom-bar-height);
    flex-direction: row;
    border-width: 1px;
    background-color: var(--unity-colors-window-background);
    border-top-color: var(--border-color);
    font-size: 14px;
}

#bottom-bar #install-package-container {
    flex-direction: row;
}

EnumField, DropdownField {
    margin: 0px;
    min-height: 19px;
}

/* recommendation view*/

#main-sections-container{
    flex-direction: row;
    flex-wrap: wrap;
}

.main-section{
    background-color: var(--card-background);
    border-width: 1px;
    border-color: var(--border-color);
    padding-bottom: 16px;
    margin: 0px 8px 8px 0px;
    width: var(--card-width);
    min-width: var(--card-min-width);
    flex-grow: 1;
    flex-shrink: 1;
    padding-left: 16px;
    padding-right: 16px;
}

.main-section .recommendation-item{
    padding: 0px;
    margin-left: -4px;
    margin-right: -4px;

}

.main-section DropdownField{
    margin-left: 0;
    margin-right: 0;
    margin-bottom: 8px;
}

.main-section .unity-help-box{
    margin-top: 4px;
    margin-left: 0;
    margin-right: 0;
}

.sub-section{
    margin-top: 0px;
    margin-left: 0px;
    margin-right: 8px;
}

.subsection-headline{
    font-size: 12px;
    -unity-font-style: bold;
}

.subsection-headline > .unity-base-field{
    margin-top: 6px;
    margin-bottom: 0px;
}

/*recommendation section - foldout that holds recommendations*/
#card-headline{
    font-size: 12px;
    -unity-font-style: bold;
    margin-bottom: 4px;
}

#associated-features-headline{
    margin-top: 8px;
}

#card-poster-image{
    margin-right: -16px;
    margin-left: -16px;
    min-width: 100%;
    min-height: 92px;
    
    background-color: var(--card-poster-image-bg-color);
    margin-bottom: 16px;
    align-items: center;
    justify-content: center;
}

#card-poster-image >*{
    
    min-width: var(--card-poster-icon-width);
    max-width: var(--card-poster-icon-width);
    min-height: var(--card-poster-icon-height);
    max-height: var(--card-poster-icon-height);
}

#recommendation-view-section-container {
    flex-grow: 1;
    margin-left: 10px;
    margin-bottom: 10px;
}

/*recommendation item - One Item that a user can select or unselect within recommendation section*/

.recommendation-item {
    border-width: 1px;
    -unity-font-style: normal;
    background-color: var(--card-background);
    overflow: hidden;
    flex-grow: 0;
    flex-shrink: 1;
    min-height: 32px;
    margin-top: 4px;
    padding:4px;
}

.recommendation-item #sub-info-text {
    margin-left: 20px;
    -unity-font-style: italic;
    white-space: normal;
    text-overflow: ellipsis;
    overflow: hidden;
}

.recommendation-item #header {
 
    flex-direction: row;
    align-content: center;
    align-items: center;
    justify-content: space-between;
}

.recommendation-item-top-left-container {
    flex-direction: row;
    flex-grow: 1;
    flex-shrink: 1;
    align-items: center;
}

.recommendation-item-top-left-container Label {
    flex-shrink: 1;
}

.recommendation-item-top-right-container {
    max-width: 64px;
    flex-shrink: 0;
    flex-direction: row;
    justify-content: flex-end;
}

.badge {
    font-size: 9px;
    padding: 1px;
    margin-left: 4px;
    padding-left: 3px;
    padding-right: 3px;
    border-width: 1px;
    border-radius: 3px;
    white-space: nowrap;
    text-overflow: ellipsis;
    overflow: hidden;
    min-width: 24px;
    flex-shrink: 1;
}

.pre-release-badge {
    color: var(--pre-release-badge-color);
    background-color: var(--pre-release-badge-color-bg);
    border-color: var(--pre-release-badge-color);
}

/*Tab views - if we are sure we will not support 2022, we should merge this with TabView uss*/
#tab-view{
    height: 100%;
}

#tab-zone {
    flex-direction: row;
    justify-content: center;
    align-items: center;
    min-height: var(--tab-button-bar-height);
    max-height: var(--tab-button-bar-height);
}

.tabs-container {
    height: 100%;
}

.tab-content{
    height: 100%;
}

#recommendation-tab-container
{
    height: 100%;
}

.main-split-view {
    border-top-width: 1px;
    border-top-color: var(--border-color);
    height: 100%;
    margin-bottom: 0px;
}

.main-split-view-right{
    margin-top: 10px;
}

#unity-dragline-anchor{
    background-color: var(--border-color);
}

#unity-dragline-anchor:hover{
    background-color: white;
    opacity: 0.3;
}

.tab-button {
    margin: 0;
    padding-top: 4px;
    border-width: 0px;
    border-bottom-width: 2px;
    min-height: var(--tab-button-bar-height);
    flex-grow: 0;
    flex-shrink: 0;
    border-color: var(--dark-grey);
    background-color: var(--dark-grey);
    padding-left: 12px;
    padding-right: 12px;
    align-items: center;

}

.tab-button:hover {
    background-color: var(--standard-grey);
}

.tab-button.selected {
    border-bottom-width: 2px;
    border-bottom-color: var(--tab-button-highlight-color);
}

/* Getting started views */
.onboarding-categories{
    margin: 0px;
    align-items: flex-start;
}

.onboarding-categories .unity-button-group{
    flex-direction: column;
    align-self: stretch;
    margin: 0px;
}

.onboarding-category-button{
    margin: 0px;
    padding-left: 20px;
    -unity-text-align: middle-left;
    width: 100%;
    min-height: 28px;
    border-width: 0px;
    background-color: transparent;
    border-radius: 0px;
} 

.onboarding-category-button:hover {
    background-color: var(--standard-grey);
}

.onboarding-category-button:checked {
    background-color: var(--unity-colors-highlight-background);
    -unity-font-style: bold;
    color: var(--onboarding-button-selected-text-color);
}

.onboarding-section-category-container{
    padding-left: 12px;
    padding-right: 12px;
    margin-bottom: 24px;
}

.onboarding-section-mainbutton{
    max-height: 24px;
    align-self: flex-start;
    padding: 2px 4px 2px 4px;
    margin-left: 0;
    margin-top: 4px;
    margin-bottom: 16px;
}

/*onboarding section*/
.onboarding-section {
    background-color: var(--unity-colors-inspector_titlebar-border_accent);
    padding: 16px;
    margin-bottom: 8px;
}

.section-foldout {
    background-color: var(--card-background);
    padding: 16px;
    padding-top: 8px;
    margin-bottom: 8px;
}

.section-foldout .unity-foldout__checkmark {
    /* Use width instead of display: none, which results in the title being cut  */
    width: 0;
}

.section-foldout .unity-foldout__text {
    font-size: 14px;
    -unity-font-style: Bold;
    margin-bottom: 8px;
    margin-left: -6px;
}

.section-foldout .onboarding-section-short-description {
    margin-left: -16px;
}

.onboarding-section-title {
    font-size: 14px;
    -unity-font-style: Bold;
    margin-bottom: 8px;
}

.three-dot-button {
    background-color: transparent;
    border-width: 0px;
    padding: 0px;
    margin: 0px;
    width: 14px;
    height: 14px;
}

.three-dot-button:hover {
    background-color: var(--standard-grey);
}

.onboarding-section-short-description {
    max-width: 700px;
    overflow: hidden;
    white-space: normal;
}

.horizontal-container {
    flex-direction: row;
    justify-content: flex-start;
}

.flex-spacer {
    flex-grow: 1;
}

.doc-button {
    border-width: 0px;
    padding: 1px;
    margin-left: 0px;
    margin-right: 2px;
    background-color: var(--card-background);
    color: var(--link-color);
}

.doc-button:hover {
    background-color: var(--dark-grey);
}

#doc-button-separator {
    margin: 2px;
    color: var(--doc-separator-color);
}

.checkmark-icon {
    background-image: var(--checkmark-icon);
    -unity-background-scale-mode: scale-to-fit;
    background-size: 16px 16px;
    min-width: 16px;
    max-width: 16px;
    min-height: 16px;
    max-height: 16px;
}

/* Empty view which is shown when no recommendations are shown*/

#empty-view {
    flex-grow: 1;
    align-items: center;
    justify-content: center;
}

#empty-view-content{
    height: 100%;
    width: 75%;
    max-width: 400px;
    justify-content: center;
}

#empty-view-icon {
    min-height: 50%;
    background-image: var(--questionnaire-empty-view-icon);
    background-size: contain;
    margin-bottom: 8px;
}

#empty-view-message {
    flex-shrink: 0;
    flex-grow: 0;
    width: 100%;
    -unity-text-align: upper-left;
    overflow: hidden;
    white-space: normal;
}


.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.multiplayer.center\Editor\MultiplayerCenterWindow\UI\MultiplayerCenterWindow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\LookDev\DisplayWindow-PersonalSkin.uss---------------
.
.

/* ******************************* */
/* override for personal skin them */
/* ******************************* */
#environmentContainer > EnvironmentElement
{
    border-color: #999999;
}

#separator
{
    border-color: #999999;
}

.list-environment-overlay > ToolbarButton
{
    background-color: #CBCBCB;
}

.list-environment-overlay > ToolbarButton:hover
{
    background-color: #e8e8e8;
}

#inspector-header
{
    background-color: #CBCBCB;
    border-color: #999999;
}

#separator-line
{
    background-color: #CBCBCB;
}

Image.unity-list-view__item:selected
{
    border-color: #3A72B0;
}

#environmentContainer
{
    border-color: #999999;
}

#debugContainer
{
    border-color: #999999;
}

#debugToolbar
{
    border-color: #999999;
}

MultipleSourcePopupField > MultipleDifferentValue
{
    background-color: #DFDFDF;
}

MultipleSourcePopupField > MultipleDifferentValue:hover
{
    background-color: #e8e8e8;
}

#sunToBrightestButton:hover
{
    background-color: #e8e8e8;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\LookDev\DisplayWindow-PersonalSkin.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\LookDev\DisplayWindow.uss---------------
.
.
.container
{
    margin: 0px;
    flex: 1;
    flex-direction: row;
}

/* ///// */
/* VIEWS */
/* ///// */

/* override as later in document */
.verticalSplit
{
    flex-direction: column;
}

#viewContainer
{
    min-width: 50px;
    flex-shrink: 0;
}

#firstView,
#secondView
{
    flex: 0;
}

/* override as later in document */
.firstView > #firstView,
.secondView > #secondView
{
    flex: 1;
}


/* /////////// */
/* ENVIRONMENT */
/* /////////// */

#environmentContainer
{
    width: 0px;
    visibility: hidden;
    flex-direction: column-reverse;
    border-color: #232323;
    border-left-width: 1px;
}

#debugContainer
{
    width: 0px;
    visibility: hidden;
    border-color: #232323;
    border-left-width: 1px;
}

#environmentContainer > EnvironmentElement
{
    border-color: #232323;
    flex-shrink: 0;
}

.showEnvironmentPanel > #environmentContainer
{
    width: 255px;
    visibility: visible;
}

.showDebugPanel > #debugContainer
{
    width: 256px; /*219px;*/
    visibility: visible;
}

.unity-label
{
    min-width: 100px;
}

.unity-composite-field__field > .unity-base-field__label
{
    padding-left: 0px;
    min-width: 10px;
    max-width: 10px;
}

.unity-composite-field__field > .unity-base-field__input
{
    margin-left: 0px;
    min-width: 38px;
    max-width: 38px;
}

#unity-text-input
{
    min-width: 40px;
}

.list-environment
{
    flex: 1;
}

.list-environment-overlay
{
    position: absolute;
    bottom: 0px;
    padding-left: 1px;
    border-bottom-width: 0px;
    background-color: rgba(0,0,0,0);
}

#environmentListCreationToolbar
{
    padding-left: 1px;
}

#environmentListCreationToolbar > *
{
    flex: 1;
    -unity-text-align: middle-center;
}

#environmentListCreationToolbar > ObjectField > Label
{
    min-width: 60px;
    width: 60px;
}

#environmentListCreationToolbar > ToolbarButton
{
    min-width: 40px;
    width: 40px;
    flex: 0;
    border-left-width: 1px;
    border-right-width: 0px;
    margin-right: -1px;
}

ObjectFieldDisplay > Label
{
    -unity-text-align: middle-left;
}

ObjectFieldDisplay > Image
{
    min-width: 12px;
}

ToolbarButton
{
    border-left-width: 0px;
}

.list-environment-overlay > ToolbarButton
{
    border-width: 0px;
    border-right-width: 1px;
    width: 20px;
    min-width: 20px;
    -unity-text-align: middle-center;
    background-color: #3c3c3c;
    padding-left: 2px;
    padding-right: 2px;
}

.list-environment-overlay > ToolbarButton:hover
{
    background-color: #585858;
}

.list-environment-overlay > #duplicate
{
    border-right-width: 0px;
}

Image.unity-list-view__item
{
    width: 210px;
    margin: 15px;
    padding: 5px;
}

Image.unity-list-view__item:selected
{
    border-width: 2px;
    padding: 3px;
    border-color: #3d6091;
    background-color: rgba(0,0,0,0);
}

.sun-to-brightest-button
{
    padding-left: 4px;
}

#inspector-header
{
    flex-direction: row;
    border-bottom-width: 1px;
    border-color: #232323;
    padding-top: 5px;
    padding-bottom: 5px;
    padding-left: 3px;
    background-color: #3C3C3C;
}

#inspector-header > Image
{
    margin-top: 2px;
    margin-bottom: 2px;
}

#inspector-header > TextField
{
    flex: 1;
}

#inspector
{
    padding-bottom: 5px;
}

#separator-line
{
    background-color: #3C3C3C;
}

#separator
{
    flex: 1;
    width: 188px;
    border-bottom-width: 1px;
    border-color: #232323;
    height: 0;
    align-self: flex-end;
}

#sunToBrightestButton
{
    background-color: rgba(0,0,0,0);
    border-radius: 0px;
    border-width: 0px;
    padding: 0px;
    margin-right: 1px;
}

#sunToBrightestButton:hover
{
    background-color: #585858;
}

/* /////// */
/* /DEBUG/ */
/* /////// */

MultipleDifferentValue
{
    -unity-font-style: bold;
    font-size: 15px;
    -unity-text-align: middle-center;
    margin-bottom: 2px;
    height: 15px;
}

MultipleSourcePopupField > MultipleDifferentValue
{
    -unity-text-align: middle-left;
    width: 120px;
    background-color: #515151;
    position: absolute;
    left: 103px;
    bottom: 1px;
}

MultipleSourcePopupField > MultipleDifferentValue:hover
{
    background-color: #585858;
}

#debugToolbar
{
    margin-top: 16px;
    margin-bottom: 16px;
    flex-direction: row;
    align-self: center;
    border-bottom-width: 1px;
    border-top-width: 1px;
    border-color: #232323;
}

#debugToolbar > ToolbarToggle
{
    width: 40px;
    left: 0px;
}

#debugToolbar > ToolbarToggle > *
{
    justify-content: center;
}

/* /////// */
/* TOOLBAR */
/* /////// */

#toolbar
{
    flex-direction: row;
}

#toolbarRadio
{
    flex-direction: row;
}

.unity-toggle__input > .unity-image
{
    padding: 2px;
}

#tabsRadio
{
    width: 256px;
    min-width: 256px;
    max-width: 256px;
    flex: 1;
    flex-direction: row;
    -unity-text-align: middle-center;
}

#tabsRadio > ToolbarToggle
{
    flex: 1;
    left: 0px;
}

#tabsRadio > ToolbarToggle > * > Label
{
    flex: 1;
}

.unity-toolbar-toggle
{
    padding-top: 0px;
    padding-right: 0px;
    padding-bottom: 0px;
    padding-left: 0px;
    margin-left: 0px;
    margin-right: 0px;
    border-left-width: 0px;
}

#renderdoc-content
{
    flex: 1;
    flex-direction: row;
    max-width: 80px;
    flex-shrink: 0;
    min-width: 24px;
    border-left-width: 1px;
}

#renderdoc-content > Label
{
    -unity-text-align: middle-left;
    min-width: 0px;
    padding-top: 0px;
}

#renderdoc-content > Image
{
    flex-shrink: 0;
    min-width: 16px;
    min-height: 16px;
}

#cameraMenu
{
    flex-direction: row-reverse;
    padding: 0px;
}

#cameraButton
{
    border-radius: 0px;
    border-width: 0px;
    border-left-width: 1px;
    padding: 0px;
    padding-right: 4px;
    margin: 0px;
}

#cameraSeparator
{
    margin-top: 4px;
    margin-bottom: 4px;
    border-right-width: 1px;
}

#cameraButton > *
{
    margin: 2px;
}

/* /////////// */
/* DRAG'N'DROP */
/* /////////// */

#cursorFollower
{
    position: absolute;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\LookDev\DisplayWindow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\LookDev\Inspectors.uss---------------
.
.
Container
{
    border-top-width:1px;
    border-color:#000000;
    padding-top:10px;
    padding-bottom:10px;
}

Container.First
{
    border-top-width:0px;
}

Container.Selected > *
{
    margin-left:-5px;
    border-left-width:5px;
    border-color:#3d6091;
}

List > .Footer
{
    align-self:flex-end;
    flex-direction:row;
}

List > .Footer > *
{
    width:25px;
}



.unity-label
{
    min-width:90px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\LookDev\Inspectors.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\DefaultVolumeProfileEditor.uss---------------
.
.
.category-header {
    font-size: 12px;
    -unity-font-style: bold;
    margin-bottom: 2px;
    margin-top: 8px;
    margin-left: 16px;
}

.content-container {
    margin-bottom: 2px;
    margin-top: 2px;
    margin-left: 16px;
}

.search-field {
    width: auto;
}

/* Allow the imgui foldout headers inside ListViews to start from the left edge all the way to the right */
.unity-list-view {
    margin-left: -31px;
    margin-right: -6px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\DefaultVolumeProfileEditor.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\HeaderFoldout.uss---------------
.
.
.header-foldout
{
	border-width: 1px 0px 1px 0px;
	border-color: var(--unity-colors-inspector_titlebar-border);

	/* ensure border take all width */
	margin: 0px -6px 0px -31px;
	padding: 0px 0px 0px 0;
}

.header-foldout > Toggle
{
	/* ensure background take all width */
	margin: 0px 0px 0px 0px;
	padding: 0px 6px 0px 31px;
}

.header-foldout > Toggle Label
{
	-unity-font-style: bold;
	font-size: 13px;
	flex-grow: 1;
}

.header-foldout > Toggle Button
{
	background-color: transparent;
	border-width: 0px;
	margin: 1px 2px 1px 0px;
	padding: 0px 0px 0px 0px;
	border-radius: 0px;
}

.header-foldout > Toggle Button:hover
{
	background-color: var(--unity-colors-button-background-hover);
}

.header-foldout > Toggle Button:disabled
{
	display: none;
}

.header-foldout > Toggle Image
{
	min-width: 16px;
}

.header-foldout > #unity-content
{
	margin: 0px 5px 0px 47px;
}

.header-foldout #enable-checkbox
{
	margin: 2px 6px 3px 1px;
}

.header-foldout #enable-checkbox #unity-checkmark
{
	background-size: 80% 80%;
}

.header-foldout #header-foldout__icon
{
	margin-top: 2px;
	margin-right: 6px;
	height: 16px;
	width: 16px;
}.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\HeaderFoldout.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\HeaderFoldoutDark.uss---------------
.
.
.header-foldout
{
	border-color: #1f1f1f;
}

.header-foldout > Toggle
{
	background-color: #323232;
}.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\HeaderFoldoutDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\HeaderFoldoutLight.uss---------------
.
.
.header-foldout
{
	border-color: #999999;
}

.header-foldout > Toggle
{
	background-color: #d3d3d3;
}.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\HeaderFoldoutLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\HelpButton.uss---------------
.
.
.help-button {
    width: 16px;
    height: 16px;
    padding: 0 0 0 0;
    margin: 0 0 0 0;
    background-image: none;
    background-color: var(--unity-colors-window-background);
    border-width: 0;
    border-radius: 0;
}

.help-button:hover {
    background-color: var(--unity-colors-button-background-hover);
}

.help-button:active {
    background-color: var(--unity-colors-button-background-hover_pressed);
}

.help-button__image {
    background-image: none;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\HelpButton.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\RenderGraphViewer.uss---------------
.
.
:root {
    --header-container-height: 24px;
    --resource-column-width: 220px;
    --resource-list-icon-size: 16px;
    --resource-grid-icon-size: 16px;
    --pass-width: 26px;
    --pass-list-height: 180px;
    --pass-list-tilted-label-length: 200px;
    --pass-title-allowance-margin: 120px; /* used to reserve enough space so that rotated pass names stay visible */
    --pass-title-allowance-margin-with-scrollbar: 133px;
    --dependency-row-height: 30px;
    --dependency-block-height: 26px;
    --dependency-block-width: var(--pass-width);
    --hover-overlay-offset-top: 204px;

    --side-panel-width: 380px;
    --side-panel-pass-title-width: 330px; /* adjust if changing --side-panel-width */
    --side-panel-attachment-label-width: 298px; /* adjust if changing --side-panel-width */

    flex-grow: 1;
    flex-direction: column;
    min-width: 600px;
    background-color: var(--main-background-color);
}

#content-container {
    height: 100%;
    margin-left: 10px;
    flex-direction: row;
}

#main-container {
    flex-direction: column;
}

#panel-container {
    flex-direction: column;
    width: var(--side-panel-width);
    min-width: 280px;
    flex-shrink: 0;
    background-color: var(--side-panel-background-color);
}

/* Header */

#header-container {
    flex-direction: row;
    height: var(--header-container-height);
    min-height: 30px;
    align-items: center;
    justify-content: flex-start;
    border-width: 0px 0 1px 0;
    border-color: black;
}

#header-container .unity-base-popup-field__label {
    min-width: auto;
}

#header-container PopupTextElement {
    flex-grow: 0;
}

#capture-button {
    width: 28px;
    height: 28px;
    background-size: 16px;
    border-width: 0px 1px 0px 0;
    border-radius: 0;
    border-color: var(--unity-colors-app_toolbar-background);
    margin: 0;
    background-color: var(--main-background-color);
}

#current-graph-dropdown {
    max-width: 200px;
}

#current-execution-dropdown {
    max-width: 300px;
}

#header-container DropdownField .unity-base-popup-field__text {
    overflow: hidden;
    text-overflow: ellipsis;
}

/* Passes */

#pass-list-scroll-view {
    min-height: var(--pass-list-height);
    flex-direction: column;
}

#pass-list {
    margin-left: var(--resource-column-width);
    flex-direction: row;
    height: var(--pass-list-height);
    align-items: flex-end;
    flex-shrink: 0;
    padding-right: var(--pass-title-allowance-margin-with-scrollbar);
}

#pass-list-scroll-view #unity-content-container {
    flex-direction: row;
}

#pass-list-width-helper {
    width: var(--pass-title-allowance-margin);
}

.pass-list__item {
    position: absolute;
    min-width: var(--pass-width);
    width: var(--pass-width);
    justify-content: center;
    flex-direction: column;
}

.pass-list__item .pass-title {
    width: var(--pass-list-tilted-label-length);
    top: -50px;
    left: -17px;
    rotate: -45deg;
    margin-bottom: 10px;
    height: var(--pass-width);
}

.pass-list__item .pass-block {
    margin-top: 2px;
    height: 15px;
    border-width: 0.5px; /* 1px width looks too wide for some reason? */
    border-radius: 2px;
    border-color: var(--pass-block-border-color);
}

.pass-block--culled {
    background-color: var(--pass-block-color--culled);
}

.pass-block--async {
    background-color: var(--pass-block-color--async);
}


.pass-list__item .pass-merge-indicator {
    background-color: var(--merged-pass-accent-color);
    height: 3px;
    margin-bottom: 1px;
    margin-top: 3px;
    visibility: hidden;
}

.pass-block.pass--highlight {
    background-color: var(--pass-block-color--highlight);
}

.pass-block.pass--highlight-border {
    border-color: var(--pass-block-color--highlight);
}

.pass-title.pass--highlight {
    color: var(--pass-block-text-color--highlight);
    -unity-font-style: bold;
}

.pass-title.pass--hover-highlight {
    color: var(--pass-block-text-color--highlight);
    -unity-font-style: bold;
}

.pass-block.pass-compatibility-message-indicator {
    background-color: var(--native-pass-accent-color);
    border-color: var(--pass-block-border-color);
}

.pass-block.pass-compatibility-message-indicator--anim {
    /* compatible pass animation transitions */
    transition-property: background-color;
    transition-duration: 0.7s;
    transition-timing-function: ease-in-out;
}

.pass-block.pass-compatibility-message-indicator--compatible {
    background-color: var(--native-pass-accent-compatible-color);
}

.pass-block.pass-synchronization-message-indicator {
    background-color: var(--pass-block-color--async);
}

.pass-block.pass-block-script-link {
    /*-unity-background-scale-mode:scale-to-fit;*/
    border-width: 2px;
    margin: -1px;
    padding: 0;
}

#pass-list-corner-occluder {
    position: absolute;
    min-width: var(--resource-column-width);
    min-height: var(--pass-list-height);
    background-color: var(--main-background-color);
}

/* Resource container */

#resource-container {
    flex-direction: row;
    margin-top: 5px;
    height: 100%;
}

#resource-container ScrollView {
    flex-grow: 1;
}

/* Grid lines */

#grid-line-container {
    position: absolute;
}

.grid-line {
    position: absolute;
    border-color: var(--grid-line-color);
    border-left-width: 2px;
    width: 0px;
    flex-grow: 1;
}

.grid-line--highlight {
    border-color: var(--grid-line-color--hover);
}

/* Resource list */

#resource-list-scroll-view {
    flex-direction: column;
    margin-top: 6px;
    width: var(--resource-column-width);
    min-width: var(--resource-column-width);
    max-width: var(--resource-column-width);
    margin-right: 0;
    margin-bottom: 12px;
}

.resource-list__item {
    height: var(--dependency-row-height);
    min-width: var(--resource-column-width);
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
}

.resource-list__item VisualElement {
    flex-direction: row;
}

.resource-list__item Label {
    overflow: hidden;
    text-overflow: ellipsis;
    padding-left: 4px;
}

.resource-list__item .resource-icon-container {
    align-items: center;
    flex-direction: row;
    margin-right: 6px;
    flex: none;
}

.resource-icon {
    width: var(--resource-list-icon-size);
    height: var(--resource-list-icon-size);
}

#resource-grid .resource-icon {
    width: var(--resource-grid-icon-size);
    height: var(--resource-grid-icon-size);
    margin-top: 5px;
    align-self: center;
}

.resource-list__item--highlight {
    -unity-font-style: bold;
    color: var(--pass-block-color--highlight);
}

/* Resource grid */

#resource-grid-scroll-view .unity-scroll-view__content-container {
    margin: 0px;
}

.resource-list-padding-item {
    height: 13px;
}

#resource-grid {
    flex-direction: column;
    margin-top: 6px;
    margin-bottom: 6px;
    padding-right: var(--pass-title-allowance-margin);
}

#resource-grid-scroll-view.content-pan VisualElement {
    cursor: pan;
}

.resource-grid__row {
    height: var(--dependency-row-height);
    flex-direction: row;
}

.resource-helper-line {
    height: var(--dependency-row-height);
    flex-shrink: 0;
    -unity-background-image-tint-color: var(--resource-helper-line-color);
    background-image: url("../Icons/RenderGraphViewer/dash.png");
    background-repeat: repeat-x;
    background-size: 8px 8px;
    margin-top: 1px;
}

.resource-helper-line--highlight {
    -unity-background-image-tint-color: var(--resource-helper-line-color--hover);
}

.usage-range-block {
    margin-top: 2px;
    background-color: var(--usage-range-color);
    height: var(--dependency-block-height);
}

.usage-range-block--highlight {
    position: absolute;
    border-width: 1px;
    border-color: var(--pass-block-color--highlight);
    height: 27px;
    margin-top: 8px;
}

.dependency-block {
    position: absolute;
    margin-top: 2px;
    width: var(--dependency-block-width);
    min-width: var(--dependency-block-width);
    height: var(--dependency-block-height);
    background-color: var(--main-background-color);
}

.dependency-block-read {
    background-color: var(--resource-read-color);
}

.dependency-block-write {
    background-color: var(--resource-write-color);
}

.dependency-block-readwrite {
    /* foreground color is set in code when rendering the triangle */
    background-color: var(--resource-write-color);
}

#hover-overlay {
    /*display: none; /* for debugging */
    /*background-color: rgba(255, 0, 0, 0.2); /* for debugging */
    position: absolute;
}

#hover-overlay.content-pan {
    cursor: pan;
}

.resource-grid-focus-overlay {
    background-color: rgba(10, 10, 10, 0.2);
    position: absolute;
}

#empty-state-message {
    flex-direction: row;
    height: 100%;
    align-items: center;
    justify-content: center;
    -unity-text-align: middle-center;
}

#empty-state-message > TextElement {
    max-width: 260px;
}

/* Resource & pass list panel */

#panel-resource-list {
    flex-grow: 1;
    flex-shrink: 1;
    min-height: 18px;
    background-color: var(--side-panel-background-color);
}

#panel-resource-list-scroll-view {
    min-height: 30px;
}

#panel-pass-list {
    flex-grow: 0;
    flex-shrink: 1;
    min-height: 18px;
    border-bottom-width: 0;
    background-color: var(--side-panel-background-color);
}

#panel-pass-list-scroll-view {
    min-height: 30px;
}

#panel-container .header-foldout {
    margin: 0; /* Counteract built-in margins inside HeaderFoldout */
}

#panel-container .header-foldout > Toggle {
    padding: 0 0 0 8px; /* Counteract built-in margins inside HeaderFoldout */
}

#panel-container .header-foldout > #unity-content {
    margin: 0 5px 0 3px; /* Counteract built-in margins inside HeaderFoldout */
}

#empty-contents-message {
    flex-direction: row;
    height: 100%;
    align-items: center;
    justify-content: center;
    -unity-text-align: middle-center;
}

#panel-container .panel-list__item {
    margin-left: 6px;
    background-color: var(--side-panel-background-color);
    /* selection animation */
    transition-property: background-color;
    transition-duration: 0.7s;
    transition-timing-function: ease-in-out;
}

#panel-container .panel-list__item--selection-animation {
    background-color: var(--unity-colors-highlight-background-hover-lighter);
}

.panel-list__item .resource-icon-container {
    align-items: center;
    flex-direction: row;
    margin-right: 4px;
    flex: none;
}

.panel-list__item .resource-icon--imported {
    width: var(--resource-list-icon-size);
    height: var(--resource-list-icon-size);
}

.panel-list__item .resource-icon--global {
    width: var(--resource-list-icon-size);
    height: var(--resource-list-icon-size);
}

.panel-list__item > Label {
    -unity-font-style: normal;
    margin-top: 2px;
    color: var(--unity-colors-default-text);
}

.panel-list__item .unity-foldout__text {
    color: var(--unity-colors-default-text);
}

.panel-list__line-break {
    border-top-width: 2px;
    border-color: var(--side-panel-item-border-color);
    margin-left: -15px; /* counteract foldout indent */
    margin-top: 2px;
    margin-bottom: 4px;
}

ScrollView TextElement {
    margin-left: 4px;
}

.unity-foldout__text {
    color: var(--unity-colors-default-text);
}

.custom-foldout-arrow #unity-checkmark {
    background-image: resource("ArrowNavigationRight");
    width: 16px;
    height: 16px;
    rotate: 90deg;
}

.custom-foldout-arrow > Toggle > VisualElement:checked #unity-checkmark {
    rotate: 270deg;
    margin-top: 2px;
    flex-grow: 0;
    flex-shrink: 0;
}

.panel-search-field {
    margin-left: 6px;
    margin-top: 6px;
    width: 98%;
    height: 20px;
}

/* Resource List panel only */

.panel-resource-list__item {
    margin-bottom: 6px;
    border-radius: 4px;
    border-width: 1px;
    border-color: var(--side-panel-item-border-color);
    margin-top: 4px;
    margin-right: 4px;
    padding-top: 4px;
    padding-bottom: 6px;
    -unity-font-style: bold;
}

.panel-resource-list__item .resource-icon {
    margin-top: 1px;
    margin-left: 2px;
    margin-right: 6px;
    flex-grow: 0;
    flex-shrink: 0;
}

.panel-resource-list__item > Toggle > VisualElement {
    max-width: 100%
}

.panel-resource-list__item > Toggle > VisualElement > Label {
    overflow: hidden;
    flex-shrink: 1;
    text-overflow: ellipsis;
}

/* Pass List panel only */

.panel-pass-list__item {
    margin-top: 2px;
}

.panel-pass-list__item > Toggle > VisualElement {
    max-width: 100%
}
.panel-pass-list__item > Toggle > VisualElement > Label {
    overflow: hidden;
    flex-shrink: 1;
    text-overflow: ellipsis;
}

.panel-pass-list__item .sub-header-text {
    margin-top: 6px;
    margin-bottom: 2px;
    -unity-font-style: bold;
}

.info-foldout {
    border-radius: 4px;
    border-width: 1px;
    border-color: var(--side-panel-item-border-color);
    margin-top: 6px;
    margin-left: 4px;
    margin-right: 4px;
    padding-top: 4px;
    padding-bottom: 6px;
}

.info-foldout > Toggle > VisualElement {
    max-width: 100%;
}

.info-foldout > Toggle > VisualElement > Label {
    margin-left: 6px;
    flex-shrink: 1;
    flex-grow: 1;
    max-width: 100%;
    overflow: hidden;
    text-overflow: ellipsis;
}

.info-foldout > TextElement {
    -unity-font-style: normal;
    margin-right: 4px;
    margin-left: -6px;
    color: var(--unity-colors-default-text);
}

.info-foldout__secondary-text {
    margin-left: 0px;
    overflow: hidden;
    text-overflow: ellipsis;
    color: var(--side-panel-secondary-text-color);
}

.panel-pass-list__item > #unity-content {
    margin-bottom: 12px;
}


.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\RenderGraphViewer.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\RenderGraphViewerDark.uss---------------
.
.
/* Dark theme colors */
:root {
    --resource-read-color: #A9D136; /* keep in sync with kReadWriteBlockFillColorDark */
    --resource-write-color: #FF5D45;
    --merged-pass-accent-color: #395C94;
    --native-pass-accent-color: #395C94;
    --native-pass-accent-compatible-color: #62B0DE;
    --pass-block-color--async: #FFC107;
    --pass-block-border-color: var(--unity-colors-highlight-background-hover-lighter);
    --pass-block-color--highlight: white;
    --pass-block-text-color--highlight: white;
    --pass-block-color--culled: black;
    --grid-line-color: #454545;
    --grid-line-color--hover: white;
    --resource-helper-line-color: darkgray;
    --resource-helper-line-color--hover: white;
    --usage-range-color: #7D7D7D;
    --main-background-color: #313131;
    --side-panel-background-color: #383838;
    --side-panel-item-border-color: #666666;
    --side-panel-secondary-text-color: #808080;
}

#capture-button {
    background-image: url("../Icons/RenderGraphViewer/d_Refresh@2x.png");
}

#capture-button:hover {
    background-color: #424242;
}

#capture-button:active {
    background-color: #6A6A6A;
}

.resource-icon--imported {
    background-image: url("../Icons/RenderGraphViewer/d_Import@2x.png");
}

.resource-icon--global-dark {
    background-image: url("../Icons/RenderGraphViewer/d_Global@2x.png");
}

.resource-icon--global-light {
    background-image: url("../Icons/RenderGraphViewer/Global@2x.png");
}

.resource-icon--texture {
    background-image: url("../Icons/RenderGraphViewer/d_Texture@2x.png");
}

.resource-icon--buffer {
    background-image: url("../Icons/RenderGraphViewer/d_Buffer@2x.png");
}

.resource-icon--acceleration-structure {
    background-image: url("../Icons/RenderGraphViewer/d_AccelerationStructure@2x.png");
}

.resource-icon--fbfetch {
    background-image: url("../Icons/RenderGraphViewer/FramebufferFetch@2x.png");
}

.resource-icon--multiple-usage {
    background-image: url("../Icons/RenderGraphViewer/d_MultipleUsage.png");
}

.pass-block.pass-block-script-link {
    background-image: url("../Icons/RenderGraphViewer/d_ScriptLink@2x.png");
    background-color: #C4C4C4;
    border-color: #C4C4C4;
}

.custom-foldout-arrow #unity-checkmark {
    -unity-background-image-tint-color: #c0c0c0;
}

.custom-foldout-arrow > Toggle > VisualElement:hover #unity-checkmark {
    -unity-background-image-tint-color: white;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\RenderGraphViewerDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\RenderGraphViewerLight.uss---------------
.
.
/* Light theme colors */
:root {
    --resource-read-color: #679C33; /* keep in sync with kReadWriteBlockFillColorLight */
    --resource-write-color: #A83645;
    --merged-pass-accent-color: #475F78;
    --native-pass-accent-color: #475F78;
    --native-pass-accent-compatible-color: #62B0DE;
    --pass-block-color--async: #C99700;
    --pass-block-border-color: var(--unity-colors-highlight-background-hover-lighter);
    --pass-block-color--highlight: white;
    --pass-block-text-color--highlight: var(--unity-colors-default-text-hover);
    --pass-block-color--culled: rgb(30, 30, 30);
    --grid-line-color: var(--unity-colors-app_toolbar_button-background-hover);
    --grid-line-color--hover: white;
    --resource-helper-line-color: #b0b0b0;
    --resource-helper-line-color--hover: white;
    --usage-range-color: #979797;
    --main-background-color: #c8c8c8;
    --side-panel-background-color: #cbcbcb;
    --side-panel-item-border-color: #666666;
    --side-panel-secondary-text-color: #707070;
}

#capture-button {
    background-image: url("../Icons/RenderGraphViewer/Refresh@2x.png");
}

#capture-button:hover {
    background-color: #BBBBBB;
}

#capture-button:active {
    background-color: #656565;
}

.resource-icon--imported {
    background-image: url("../Icons/RenderGraphViewer/Import@2x.png");
}

.resource-icon--global-dark {
    background-image: url("../Icons/RenderGraphViewer/Global@2x.png");
}

.resource-icon--global-light {
    background-image: url("../Icons/RenderGraphViewer/d_Global@2x.png");
}

.resource-icon--texture {
    background-image: url("../Icons/RenderGraphViewer/Texture@2x.png");
}

.resource-icon--buffer {
    background-image: url("../Icons/RenderGraphViewer/Buffer@2x.png");
}

.resource-icon--acceleration-structure {
    background-image: url("../Icons/RenderGraphViewer/AccelerationStructure@2x.png");
}

.resource-icon--fbfetch {
    background-image: url("../Icons/RenderGraphViewer/d_FramebufferFetch@2x.png");
}

.resource-icon--multiple-usage {
    background-image: url("../Icons/RenderGraphViewer/MultipleUsage.png");
}

.resource-helper-line--highlight {
    background-size: 8px 20px; /* light theme needs a wider dashed line to be properly visible */
}

.pass-block.pass-block-script-link {
    background-image: url("../Icons/RenderGraphViewer/ScriptLink@2x.png");
    background-color: #555555;
    border-color: #555555;
}

.custom-foldout-arrow #unity-checkmark {
    -unity-background-image-tint-color: black;
}

.custom-foldout-arrow > Toggle > VisualElement:hover #unity-checkmark {
    -unity-background-image-tint-color: grey;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\RenderGraphViewerLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\RenderPipelineGlobalSettings.uss---------------
.
.
.srp-global-settings__container {
    margin-top: 1px;
}

.srp-global-settings__header-container {
    flex-direction: row;
    font-size: 20px;
    -unity-font-style: bold;
    margin-bottom: 4px;
    margin-left: 10px;
}

.srp-global-settings__help-button-container {
    align-items: flex-end;
    flex-grow: 1;
}

#srp-global-settings__help-button {
    margin: 2px;
}

.volume-profile-section {
    margin-left: 10px;
    margin-bottom: 10px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\RenderPipelineGlobalSettings.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\VolumeEditor.uss---------------
.
.
.volume-profile-header__container {
    margin: 4px -6px 2px -15px;
    border-top-width: 1px;
    border-bottom-width: 1px;
    border-color: #1f1f1f;
    padding-left: 13px;
    padding-top: 4px;
    padding-bottom: 4px;
    flex-direction: row;
}

#volume-profile-header__asset-icon {
    flex-grow: 1;
    background-color: rgba(0, 0, 0, 0);
    width: 44px;
    height: 44px;
    background-image: none;
}

.volume-profile-objectfield__container {
    flex-grow: 1;
}

.volume-profile-objectfield__container--column {
    flex-direction: column;
}

.volume-profile-objectfield__container--row {
    flex-direction: row;
}

.volume-profile-objectfield__container > ObjectField {
    height: 17px;
    flex-shrink: 1;
}

.volume-profile-objectfield__contextmenu {
    background-color: var(--unity-colors-window-background);
    border-width: 0 0 0 0;
    border-radius: 0 0 0 0;
    padding: 0 0 0 0;
    width: 16px;
    height: 16px;
    margin-left: 2px;
    margin-top: 1px;
}

.volume-profile-objectfield__contextmenu:hover {
    background-color: var(--unity-colors-button-background-hover);
}

.volume-profile-objectfield__contextmenu:active {
    background-color: var(--unity-colors-button-background-hover_pressed);
}

.volume-profile-objectfield {
    flex-grow: 1;
}

#volume-profile-new-button {
    width: 50px;
}

#volume-profile-instance-profile-label {
    -unity-font-style: bold;
    margin-left: 4px;
}

#volume-profile-component-container {
    margin-left: -15px;
    margin-top: 1px;
    margin-right: -3px;
}

#volume-profile-blend-distance {
    margin-left: 15px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\StyleSheets\VolumeEditor.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Samples~\Common\Scripts\Resources\SamplesSelectionUSS.uss---------------
.
.
.warningIcon {
    background-image: var(--unity-icons-console_entry_warn);
    margin-right: 5px;
    width: 32px;
    height: 32px;
    min-width: 32px;
    min-height: 32px;
}

.link-cursor {
    cursor: link;
}

.link {
}

Label {
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Samples~\Common\Scripts\Resources\SamplesSelectionUSS.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\Material\ShaderGraph\Resources\DiffusionProfileSlotControlView.uss---------------
.
.
DiffusionProfileSlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
     width: 100px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input{
    margin-top: 0px;
    margin-bottom: 0px;
    margin-left: 0px;
    margin-right: 0px;
}

.unity-object-field__object{
    overflow:hidden;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\Material\ShaderGraph\Resources\DiffusionProfileSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\FrameSettings.uss---------------
.
.
.frame-settings-field
{
	flex-grow: 1;
}

.frame-settings-field > Label
{
	overflow: hidden;
	text-overflow: ellipsis;
}

.frame-settings-override-checkbox
{
	margin: 2px 0px 3px 1px;
}

.frame-settings-override-checkbox #unity-checkmark
{
	background-size: 80% 80%;
}

.frame-settings-override-header
{
	flex-direction: row;
}

.frame-settings-override-header > Button
{
	background-color: transparent;
	border-width: 0px;
	font-size: 10px;
	padding-bottom: 0px;
	/* there is no var for disabled label color yet. See FrameSettingsLight.uss and FrameSettingsDark.uss */
}

.frame-settings-override-header > #All
{
	margin-right: 0px;
}

.frame-settings-override-header > #None
{
	margin-left: 0px;
}

.frame-settings-override-header > Button:hover
{
	color: var(--unity-colors-label-text);
}

.project-settings-section__content .frame-settings-header
{
	-unity-font-style: bold;
	padding-top: 5px;
}

.project-settings-section__content .frame-settings-section-header > Toggle
{
	background-color: var(--unity-colors-helpbox-background);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\FrameSettings.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\FrameSettingsDark.uss---------------
.
.
.frame-settings-override-header > Button
{
	/* there is no variable for disabled label yet */
	color: rgba(255, 255, 255, 0.5); /*from BuilderInspectorLight.uss*/
}.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\FrameSettingsDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\FrameSettingsLight.uss---------------
.
.
.frame-settings-override-header > Button
{
	/* there is no variable for disabled label yet */
	color: rgba(0, 0, 0, 0.5); /*from BuilderInspectorLight.uss*/
}.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\FrameSettingsLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\QualitySettings.uss---------------
.
.
/* ========= Quality Settings Panel =========  */

HDRPAssetHeaderEntry {
    flex-direction: row;
    align-items: center;
}

.unity-quality-entry-tag {
    border-bottom-width: 1px;
    border-top-width: 1px;
    border-right-width: 1px;
    border-left-width: 1px;
    border-top-left-radius: 2px;
    border-top-right-radius: 2px;
    border-bottom-left-radius: 2px;
    border-bottom-right-radius: 2px;
    margin: 0px 2px;
    padding: 0px 2px;
}

.unity-quality-header-list {
    margin: 5px;
    border-bottom-width: 1px;
    border-color: #1F1F1F;
    flex: 1 0 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\QualitySettings.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\QualitySettingsDark.uss---------------
.
.
.unity-list-view .odd {
    background-color: #3f3f3f;
}

/* We need to override the selected value in this case */
.unity-list-view .odd:selected {
    background-color: #3e5f96;
}

/* ========= Quality Settings Panel =========  */

.unity-quality-header-list {
    border-color: #1F1F1F;
}

.unity-quality-entry-tag {
    border-color: #1F1F1F;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\QualitySettingsDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\QualitySettingsLight.uss---------------
.
.
.unity-list-view .odd {
    background-color: #bcbcbc;
}

/* We need to override the selected value in this case */
.unity-list-view .odd:selected {
    background-color: #3e5f96;
}

/* ========= Quality Settings Panel =========  */

.unity-quality-header-list {
    border-color: #999999;
}

.unity-quality-entry-tag {
    border-color: #999999;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\QualitySettingsLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\ReorderableList.uss---------------
.
.
#ReorderableList
{
	padding: 2px 0px 0px 0px;
}

#ReorderableList > ScrollView
{
	border-top-left-radius: 0px;
	border-top-right-radius: 0px;
	border-color: var(--unity-colors-window-border);
}

#ReorderableList-header
{
	padding-left: 6px;
	-unity-font-style: bold;
	-unity-text-align: middle-left;
	border-top-left-radius: 3px;
	border-top-right-radius: 3px;
	border-width: 1px 1px 0px 1px;
	border-color: var(--unity-colors-window-border);
	background-color: var(--unity-colors-tab-background);
}

#ReorderableList-element
{
	-unity-text-align: middle-left;
	height: 18px;
}

#ReorderableList .unity-list-view__empty-label
{
	padding-left: 6px;
}.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\USS\ReorderableList.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\Wizard\USS\Formating.uss---------------
.
.
.h1 {
    -unity-font-style: bold;
    font-size: 16px;
    margin: 2px 5px 8px 9px;
}

.h2 {
    -unity-font-style: bold;
    margin-left: 7px;
}

.normal
{
    margin-left: 10px;
}

.normal-indent1
{
    margin-left: 24px;
}

.normal-indent2
{
    margin-left: 38px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\Wizard\USS\Formating.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\Wizard\USS\Wizard.uss---------------
.
.
#StatusOK, #StatusError, #StatusPending
{
    width: 12px;
    height: 12px;
    position: absolute;
    right: 10px;
}

#DefaultResourceFolder > Label, #NewScene > Label, #NewDXRScene > Label
{
    width: 165px;
}

#TestLabel
{
    width: 425px;
}

#Resolver
{
    position: absolute;
    right: 0px;
    width: 100px;
    margin-top: -4px;
}

#TestRow
{
    flex-direction:row;
    margin-top: 5px;
}

#FixAllWarning
{
    margin-bottom: -8px
}


.FixAllButton
{
    margin: 8px;
}

#OuterBox
{
    margin: 10px;
    margin-top: 0px;
    margin-bottom: 0px;
    border-radius: 3px;
}

#InnerBox
{
    padding: 10px;
    padding-top: 5px;
    border-radius: 3px;
    border-bottom-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-top-left-radius: 0px;
    border-top-right-radius: 0px;
}

.Radio
{
    margin: 0px;
    flex: 1;
    border-left-width: 0px;
    border-right-width: 1px;
    border-bottom-width: 1px;
}

.LastRadio
{
    border-right-width: 0px;
    margin-right: -1px;
}

.Radio > * > Label
{
    flex-grow: 1;
    -unity-text-align: middle-center;
}

#DefaultResourceFolder
{
    margin-right: 10px;
}

.RightAnchoredButton
{
    position: absolute;
    right: 0px;
    width: 114px;
    margin-top: 0px;
    margin-right: 10px;
}

.LargeButton
{
    margin-left: 20px;
    margin-right: 20px;
}

#HDRPVersionContainer
{
    flex-direction: row;
    margin-top: 10px;
    padding-top: 3px;
    padding-bottom: 3px;
    padding-left: 3px;
}

.ScopeBox
{
    border-width: 1px;
    border-radius: 3px;
    margin: 12px;
    padding: 10px;
    border-color: var(--unity-colors-inspector_titlebar-border);
}

.ScopeBoxLabel
{
    margin-top: -17px;
    align-self: center;
    width: 200px;
    font-size: 14px;
    -unity-font-style: bold;
    -unity-text-align: middle-center;
    background-color: var(--unity-colors-window-background);
}

/* spacing between categories */

#MainToolbar
{
    flex-direction: row-reverse;
}

#WizardCheckbox
{
    margin-top: 3px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.high-definition\Editor\Wizard\USS\Wizard.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\2D\LightBatchingDebugger\LightBatchingDebugger.uss---------------
.
.
:root {
    --border-color-custom: rgb(161, 154, 154);
}

.BatchList {

}

.BatchList.HeaderContainer {
    flex-direction: row;
    height: auto;
    background-color: var(--unity-colors-inspector_titlebar-background);
}

.BatchList.IndexColumn {
    margin-left: 5px;
    width: 65px;
}

.BatchList.ColorColumn {
    width: 3px;
}

.BatchList.NameColumn {
    flex-grow: 2;
    margin-left: 5px;
}

.BatchList.BatchContainer {
    flex-direction: row;
}

.BatchList.ColorColumn.Container {
    align-items: center;
}

.BatchList.ColorColumn.Splitter {
    margin-top: 5px;
    margin-bottom: 5px;
    margin-right: 5px;
}

.BatchList.IndexColumn.Header {
    margin-top: 5px;
    margin-bottom: 5px;
}

.BatchList.NameColumn.Header {
    margin-top: 5px;
    margin-bottom: 5px;
}

.BatchList.IndexColumn.Batch {
    margin-top: 5px;
}

.BatchList.NameColumn.Batch {
    margin-top: 5px;
    flex-direction: column;
}

.BatchList.List {
    flex-grow: 1;
}

.LayerNameLabel {
    margin-bottom: 5px;
}

.InfoView {
}

.InfoView.Content {
    border-bottom-width: 10px;
    white-space: normal;
}

.InfoView.Content.PillContainer {
    flex-direction: row;
    flex-wrap: wrap;
}

.InfoView.Content.Spacer {
    flex-grow: 1;
}

.InfoView.Footer {
    padding: 10px;
    white-space: normal;
}

.InfoView.Header {
    padding: 5px;
    align-items: flex-start;
    justify-content: space-around;
    background-color: var(--unity-colors-window-background);
}

.InfoView.Header.Bottom {
    border-bottom-color: var(--unity-colors-default-border);
}

.InfoView.Header.Top {
    border-top-color: var(--unity-colors-default-border);
}

.InfoScroller {
    flex-grow: 1;
    padding: 10px;
}

.MinSize {
    min-width: 160px;
    min-height: 160px;
}

.InfoContainer{
    min-width: 160px;
    flex-grow: 1;
    background-color: var(--unity-colors-default-background);
    justify-content: center;
}

.InitialPrompt {
    align-self: center;
}

.Pill {
    border-color: var(--border-color-custom);
    border-left-width: 1px;
    border-right-width: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
    border-top-left-radius: 10px;
    border-bottom-left-radius: 10px;
    border-top-right-radius: 10px;
    border-bottom-right-radius: 10px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\2D\LightBatchingDebugger\LightBatchingDebugger.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_editor.uss---------------
.
.
.iconButton {
    background-color: rgba(0,0,0,0);
}

.light :hover {
    background-color: #B2B2B2;
}

.dark :hover {
    background-color: #303030;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_editor.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget.uss---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget_main.uss---------------
.
.
.selected {
    color: white;
}

.not_selected {
    color: rgb(102, 102, 102);
}

.unity-list-view__empty-label {
    display: none;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Editor\Converter\converter_widget_main.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Resizable.uss---------------
.
.
ResizableElement
{
    position:absolute;
    top: 0;
    bottom: 0;
    right: 0;
    left: 0;
    flex-direction: row;
    align-items: stretch;
}

ResizableElement #right
{
    width: 10px;
    flex-direction: column;
    align-items: stretch;
}

ResizableElement #left
{
    width: 10px;
    flex-direction: column;
    align-items: stretch;
}
ResizableElement #middle
{
    flex:1 0 auto;
    flex-direction: column;
    align-items: stretch;
}


ResizableElement #left #top-left-resize
{
    height: 10px;
    cursor: resize-up-left;
}

ResizableElement #left #left-resize
{
    flex:1 0 auto;
    cursor: resize-horizontal;
}

ResizableElement #left #bottom-left-resize
{
    height: 10px;
    cursor: resize-up-right;
}

ResizableElement #middle #top-resize
{
    height: 10px;
    cursor: resize-vertical;
}

ResizableElement #middle #middle-center
{
    flex:1 0 auto;
}

ResizableElement #middle #bottom-resize
{
    height: 10px;
    cursor: resize-vertical;
}

ResizableElement #right #top-right-resize
{
    height: 10px;
    cursor: resize-up-right;
}

ResizableElement #right #right-resize
{
    flex:1 0 auto;
    cursor: resize-horizontal;
}

ResizableElement #right #bottom-right-resize
{
    height: 10px;
    cursor: resize-up-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Resizable.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Selectable.uss---------------
.
.
.selectable > #selection-border
{
    position: absolute;
    left:0;
    right:0;
    top:0;
    bottom:0;
}

.selectable:hover  > #selection-border{
    border-color: rgba(68,192,255, 0.5);
    border-top-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
}

.selectable:selected  > #selection-border
{
    border-color: #44C0FF;
    border-left-width: 1px;
    border-right-width: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
}

.selectable:selected:hover  > #selection-border
{
    border-left-width: 2px;
    border-right-width: 2px;
    border-top-width: 2px;
    border-bottom-width: 2px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Selectable.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\StickyNote.uss---------------
.
.
* .unity-label#title, * .unity-text-field#title-field,* .unity-label#contents, * .unity-text-field#contents-field
{
    -unity-font:resource("GraphView/DummyFont(LucidaGrande).ttf");
}

.sticky-note
{
    min-height: 100px;
    min-width: 80px;
    position:absolute;
    flex-direction:column;
    align-items:stretch;
    border-radius:0;
    margin-left: 0;
    margin-right: 0;
    margin-top: 0;
    margin-bottom: 0;
    border-left-width:0;
    border-top-width:0;
    border-bottom-width:0;
    border-right-width:0;
    padding-left:0;
    padding-right:0;
    padding-top:0;
    padding-bottom:0;
    border-radius: 2px;
}

.sticky-note #selection-border
{
    border-radius: 2px;
}

.sticky-note, * .unity-text-field , * .unity-text-field > .unity-base-text-field__input
{
    background-color: #fcd76e;
    padding-left:0;
    padding-right:0;
    padding-top:0;
    padding-bottom:0;
    font-size:20px;
}

.sticky-note, * .unity-text-field, .unity-base-text-field .unity-base-text-field__input
{
    border-left-width:0;
    border-top-width:0;
    border-bottom-width:0;
    border-right-width:0;
    unity-text-align:top-left;
}

.unity-text-field:hover, .unity-base-text-field .unity-base-text-field__input:hover
{
    border-left-width:0;
    border-top-width:0;
    border-bottom-width:0;
    border-right-width:0;
}

.sticky-note .resizer
{
    margin-bottom: 6px;
    margin-right: 6px;
}

.sticky-note *
{
    color:#584308;
}


/* Themes*/

.theme-black *
{
    color:#AB924B;
}

.theme-black > #node-border
{
    border-color:#AB924B;
    border-radius: 8px;
}
.sticky-note.theme-black, .theme-black .unity-text-field
{
    background-color: #362905;
}


* > #node-border
{
    flex:1 0 auto;
    flex-direction:column;
    align-items:stretch;
    border-left-width: 2px;
    border-top-width: 2px;
    border-bottom-width: 2px;
    border-right-width: 2px;
}


* #title, * #title-field
{
    font-size: 20px;
    white-space: normal;
}

* #contents
{
    margin-top:0;
    margin-bottom:0;
    padding-top:0;
    padding-bottom:0;
}

* #contents, * #contents *
{
    font-size: 11px;
    white-space: normal;
}

* #title
{
    margin-top:0;
    margin-bottom:0;
    padding-top:0;
    padding-bottom:0;
    white-space: normal;
}

* #title.empty
{
    height: 12px;
}

* #contents
{
    flex:1 0 auto;
}

* .unity-text-field
{
    position: absolute;
    left:0;
    right:0;
    top:0;
    bottom:0;
    margin-left:0;
    margin-right:0;
    margin-top:0;
    margin-bottom:0;
    padding-left: 0;
    padding-right: 0;
    padding-top: 0;
    padding-bottom: 0;
}

* .unity-text-field .unity-base-field__input
{
    background-image:none;
}

* .unity-label
{
    border-top-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    margin-left: 6px;
    margin-right: 6px;
    margin-top: 1px;
    margin-bottom: 1px;
    padding-left:0;
    padding-right:0;
    overflow:hidden;
}

.size-medium #title, .size-medium #title-field *
{
    font-size: 40px;
}
.size-medium #contents, .size-medium #contents *
{
    font-size: 24px;
}

.size-large #title, .size-large #title-field *
{
    font-size: 60px;
}
.size-large #contents, .size-large #contents *
{
    font-size: 36px;
}

.size-huge #title, .size-huge #title-field *
{
    font-size: 80px;
}

.size-huge #contents, .size-huge #contents *
{
    font-size: 56px;
}

* .unity-label:hover
{
    border-color: rgba(68,192,255, 0.5);
}

* .unity-text-field:hover
{
    background-image:none;
}

* .unity-text-field .unity-base-field__input:focus
{
    background-image:none;
}

* .unity-text-field .unity-base-field__input:focus:hover
{
    background-image:none;
}


/*

.sticky-note.theme-orange, .theme-orange TextField
{
    background-color:#FCD76E;
}

.sticky-note.theme-orange *
{
    color:#000000;
}

.sticky-note.theme-orange #node-border
{
    border-color:none;
}

.sticky-note.theme-red *
{
    color:#FF8B8B;
}
.sticky-note.theme-green *
{
    color:#6BE6B0;
}
.sticky-note.theme-blue *
{
    color:#8FC1DF;
}
.sticky-note.theme-teal *
{
    color:#84E4E7;
}
.sticky-note.theme-purple *
{
    color:#FBCBF4;
}
*/
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\StickyNote.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\ColorMode.uss---------------
.
.
/* Node Category Colors */
MaterialNodeView.Artistic #title {
    border-color:#DB773B;
}

MaterialNodeView.Channel #title {
    border-color:#97D13D;
}

MaterialNodeView.Input #title {
    border-color:#CB3022;
}

MaterialNodeView.Math #title {
    border-color:#4B92F3;
}

MaterialNodeView.Procedural #title {
    border-color:#9C4FFF;
}

MaterialNodeView.Utility #title {
    border-color:#AEAEAE;
}

MaterialNodeView.UV #title {
    border-color:#08D78B;
}

/* Precision Colors */
MaterialNodeView.Single #title {
    border-color:#4B92F3;
}

MaterialNodeView.Half #title {
    border-color:#CB3022;
}

MaterialNodeView.Graph #title {
    border-color:#30CB22;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\ColorMode.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\CustomSlotLabelField.uss---------------
.
.
TextField.modified {
    -unity-font-style: bold;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\CustomSlotLabelField.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\GraphEditorView.uss---------------
.
.
GraphEditorView {
    flex-direction: column;
    flex: 1;
}

GraphEditorView #TitleBar {

}

GraphEditorView > #content {
    flex: 1;
    flex-direction: row;
}

GraphEditorView > #content > #GraphView {
    flex: 1;
}

GraphEditorView > #content > #inspector {
    width: 400px;
}

.edge.fromMatrix4, .edge.fromMatrix3, .edge.fromMatrix2 {
    --edge-output-color: #8FC1DF;
}
.edge.toMatrix4, .edge.toMatrix3, .edge.toMatrix2 {
    --edge-input-color: #8FC1DF;
}

.edge.fromTexture2D, .edge.fromCubemap {
    --edge-output-color: #FF8B8B;
}
.edge.toTexture2D, .edge.toCubemap {
    --edge-input-color: #FF8B8B;
}

.edge.fromVector4 {
    --edge-output-color: #FBCBF4;
}
.edge.toVector4 {
    --edge-input-color: #FBCBF4;
}

.edge.fromVector3 {
    --edge-output-color: #F6FF9A;
}
.edge.toVector3 {
    --edge-input-color: #F6FF9A;
}

.edge.fromVector2 {
    --edge-output-color: #9AEF92;
}
.edge.toVector2 {
    --edge-input-color: #9AEF92;
}

.edge.fromVector1 {
    --edge-output-color: #84E4E7;
}
.edge.toVector1 {
    --edge-input-color: #84E4E7;
}

.edge.fromBoolean {
    --edge-output-color: #9481E6;
}
.edge.toBoolean {
    --edge-input-color: #9481E6;
}

#resizeBorderFrame > .resize {
    background-color: rgba(0, 0, 0, 0);
    position: absolute;
}

#resizeBorderFrame > .resize.vertical {
    cursor: resize-vertical;
    height: 10px;
    left: 10px;
    right: 10px;
    padding-top: 0;
    padding-bottom: 0;
    margin-top: 0;
    margin-bottom: 0;
}

#resizeBorderFrame > .resize.horizontal {
    cursor: resize-horizontal;
    width: 10px;
    top: 10px;
    bottom: 10px;
    padding-left: 0;
    padding-right: 0;
    margin-left: 0;
    margin-right: 0;
}

#resizeBorderFrame > .resize.diagonal {
    width: 10px;
    height: 6px;
}

#resizeBorderFrame > .resize.diagonal.top-left {
    cursor: resize-up-left;
    top: 0;
    left: 0;
}

#resizeBorderFrame > .resize.diagonal.top-right {
    cursor: resize-up-right;
    top: 0;
    right: 0;
}

#resizeBorderFrame > .resize.diagonal.bottom-left {
    cursor: resize-up-right;
    bottom: 0;
    left: 0;
}

#resizeBorderFrame > .resize.diagonal.bottom-right {
    cursor: resize-up-left;
    bottom: 0;
    right: 0;
}

#resizeBorderFrame > .resize.vertical.top {
    top: 0;
}

#resizeBorderFrame > .resize.vertical.bottom {
    bottom: 0;
}

#resizeBorderFrame > .resize.horzontal.left {
    left: 0;
}

#resizeBorderFrame > .resize.horizontal.right {
    right: 0;
}

.resizeBorderFrame {
    position: absolute;
    top: 0;
    right: 0;
    bottom: 0;
    left: 0;
}

.nodeEditor {
    border-color: rgb(79, 79, 79);
    border-bottom-width: 1px;
    padding-top: 10px;
}

NodeEditorHeaderView {
    padding-left: 16px;
    padding-right: 16px;
    padding-bottom: 10px;
    flex-direction: row;
}

NodeEditorHeaderView > #preType {
    margin-left: 10px;
}

NodeEditorHeaderView > #preType,
NodeEditorHeaderView > #postType,
NodeEditorHeaderView > #type {
    color: rgb(180, 180, 180);
}

NodeEditorHeaderView > #title {
    color: rgb(180, 180, 180);
    -unity-font-style: bold;
}

.nodeEditor > .section {
    padding-bottom: 10px;
}

.nodeEditor > .section.hidden {
    height: 0;
    padding-bottom: 0;
}

.nodeEditor > .section > .title {
    color: rgb(180, 180, 180);
    -unity-font-style: bold;
    padding-left: 16px;
    padding-right: 16px;
    padding-bottom: 2px;
}

.nodeEditor > .section > #slots {
    flex-direction: column;
    padding-left: 15px;
    padding-right: 15px;
}

.nodeEditor > .section#surfaceOptions {
    padding-left: 15px;
    padding-right: 15px;
}

IMGUISlotEditorView {
    flex-direction: column;
    padding-bottom: 2px;
}

ObjectControlView {
    flex-direction: row;
}

ObjectControlView > ObjectField {
    flex: 1;
}

PropertyControlView {
    padding-left: 8px;
    padding-right: 8px;
    padding-top: 4px;
    padding-bottom: 4px;
}

.stack-node {
    --separator-extent: 6;
}

/* TEMP STUFF THAT SHOULD ACTUALLY STAY IN GRAPHVIEW */

.unity-Doublefield-input {
    min-height: 15px;
    margin-left: 4px;
    margin-top: 2px;
    margin-right: 4px;
    margin-bottom: 2px;
    padding-left: 3px;
    padding-top: 1px;
    padding-right: 3px;
    padding-bottom: 2px;
    -unity-slice-left: 3;
    -unity-slice-top: 3;
    -unity-slice-right: 3;
    -unity-slice-bottom: 3;
    --unity-selection-color: rgba(61,128,223,166);
    cursor: text;
    color: #B4B4B4;
    background-image: resource("Builtin Skins/DarkSkin/Images/TextField.png");
    --unity-cursor-color:#B4B4B4;
}

.unity-Doublefield-input:focus {
    background-image: resource("Builtin Skins/DarkSkin/Images/TextField focused.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\GraphEditorView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\GraphSubWindow.uss---------------
.
.
.unity-label {
    padding: 5px 2px 2px;
    margin: 2px 4px;
}

.GraphSubWindow {
    position:absolute;
    border-left-width: 1px;
    border-top-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-radius: 5px;
    flex-direction: column;
    background-color: #2b2b2b;
    border-color: #191919;
    min-width: 100px;
    min-height: 100px;
    width: 200px;
    height: 200px;
}

.GraphSubWindow.windowed {
    position: relative;
    padding-top: 0;
    flex: auto;
    border-left-width: 0;
    border-top-width: 0;
    border-right-width: 0;
    border-bottom-width: 0;
    border-radius: 0;
    width: initial;
    height: initial;
}

.GraphSubWindow.windowed > .resizer {
    display: none;
}

.GraphSubWindow > .mainContainer {
    flex-direction: column;
    align-items: stretch;
}

.GraphSubWindow.scrollable > .mainContainer {
    position: absolute;
    top:0;
    left:0;
    right:0;
    bottom:0;
}

.GraphSubWindow > .mainContainer > #content {
    flex-direction: column;
    align-items: stretch;
}

.GraphSubWindow.scrollable > .mainContainer > #content {
    position: absolute;
    top:0;
    left:0;
    right:0;
    bottom:0;
    flex-direction: column;
    align-items: stretch;
}

.GraphSubWindow > .mainContainer > #content > ScrollView {
    flex: 1 0 0;
}

.GraphSubWindow > .mainContainer > #content > #contentContainer {
    min-height: auto;
    padding: 0 0 6px;
    flex-direction: column;
    flex-grow: 1;
    align-items: stretch;
}

.GraphSubWindow > #content > #header {
    font-size: 15px;
}

.GraphSubWindow > .mainContainer > #content > #header {
    overflow: hidden;
    flex-direction: row;
    align-items: stretch;
    background-color: #393939;
    border-bottom-width: 1px;
    border-color: #212121;
    border-top-right-radius: 4px;
    border-top-left-radius: 4px;
    padding-left: 1px;
    padding-top: 4px;
    padding-bottom: 2px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\GraphSubWindow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\HeatmapValuesEditor.uss---------------
.
.
.sg-heatmap__heat-field,
.sg-heatmap__subgraph-picker {
    margin-right: 0;
}

.sg-heatmap__node-label {
    flex-grow: 1;
    -unity-text-align: middle-left;
    margin-left: 4px;
}

.sg-heatmap__list,
.sg-heatmap__help-box {
    margin-bottom: 7px;
}

.sg-heatmap__list {
    max-height: none;
}

.sg-heatmap__help-box--hidden {
    display: none;
}

.unity-foldout__content {
    margin-left: 15px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\HeatmapValuesEditor.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\HelpBoxRow.uss---------------
.
.
HelpBoxRow > #container{
    flex-grow: 1;
    margin-left: 8px;
    margin-right: 8px;
    padding-left: 8px;
    padding-right: 8px;
    flex-direction: row;
}

HelpBoxRow > #container > #label {
    width : 20px;
    height : 20px;
    align-self: center;
    margin-right: 8px;
}

HelpBoxRow > #container > #content {
    flex-grow: 1;
    justify-content: center;
}

HelpBoxRow
{
    white-space: normal;
}

.help-box-row-style-info
{
    background-color: #474747;
}

.help-box-row-style-info #label
{
    background-image : resource("console.infoicon");
}

.help-box-row-style-warning
{
}

.help-box-row-style-warning #label
{
    background-image : resource("console.warnicon");
}

.help-box-row-style-error
{
}

.help-box-row-style-error #label
{
    background-image : resource("console.erroricon");
}

#message-warn
{
    color:#584308;
    white-space: normal;
}

#message-info
{
    color:#d2d2d2;
    white-space: normal;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\HelpBoxRow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\HlslFunctionView.uss---------------
.
.
HlslFunctionView {
    margin-top: 2;
    margin-bottom: 2;
    margin-left: 4;
    margin-right: 4;
}

HlslFunctionView > #Row {
    flex-direction: row;
    flex-grow: 1;
    margin-top: 2;
    margin-bottom: 2;
    margin-left: 4;
    margin-right: 4;
}

.unity-label {
    width: 80;
    margin-top: 0;
    margin-bottom: 0;
}

.unity-base-field {
    flex-direction: column;
    flex-grow: 1;
    margin-top: 0;
    margin-bottom: 0;
}

.unity-base-text-field__input {
    flex-direction: column;
    flex-grow: 1;
    -unity-text-align: upper-left;
    overflow: hidden;
}

.sg-hlsl-function-view__body {
    align-self: stretch;
    flex-shrink: 1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\HlslFunctionView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\InspectorView.uss---------------
.
.
.InspectorView {
    position:absolute;
    justify-content: flex-start;
    min-width: 100px;
    min-height: 100px;
    width: 300px;
    height: 275px;
}

.InspectorView > .mainContainer {
    flex-direction: column;
    align-items: stretch;
}

ScrollView {
    flex: 1 0 0;
}

.InspectorView > .mainContainer {
    border-left-width: 1px;
    border-top-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-radius: 5px;
    background-color: #2e2e2e;
    border-color: #191919;
    flex-direction: column;
    align-items: stretch;
    margin: 6px;
    flex-grow: 1;
}

.InspectorView > .mainContainer > #content {
    flex-grow: 1;
}

.InspectorView > .mainContainer > #content > #contentContainer {
    min-height: auto;
    padding: 6px;
    flex-direction: column;
    flex-grow: 1;
    align-items: stretch;
    padding-bottom: 15px;
}

.InspectorView > .mainContainer > #content > #header {
    overflow: hidden;
    flex-direction: row;
    justify-content: space-between;
    background-color: #393939;
    padding: 8px;
    border-top-left-radius: 6px;
    border-top-right-radius: 6px;
}

.InspectorView.scrollable > .mainContainer > #content > .unity-scroll-view > .unity-scroller--horizontal {
    background-color: #393939;
}

.InspectorView.scrollable > .mainContainer > #content > .unity-scroll-view > .unity-scroller--vertical {
    background-color: #393939;
}

#maxItemsMessageLabel {
    visibility: hidden;
    padding: 8px 4px 4px 4px;
    color: rgb(180, 180, 180);
    align-self: center;
    border-color: #1F1F1F;
}

#labelContainer {
    flex: 1 0 0;
    flex-direction: column;
    align-items: stretch;
}

#titleLabel {
    font-size: 14px;
    color: rgb(180, 180, 180);
    padding: 1px 2px 2px;
}

.MainFoldout {
    background-color: #383838;
    border-color: #1F1F1F;
    border-top-width: 1px;
}

.InspectorView #NodeSettingsContainer {
    padding-left: 3px;
    padding-right: 6px;
    width: 0;
    min-width: 100%;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\InspectorView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\MasterPreviewView.uss---------------
.
.
MasterPreviewView {
    flex-direction: column;
    position: absolute;
    right: 10px;
    bottom: 10px;
    width: 125px;
    height: 125px;
    min-width: 100px;
    min-height: 100px;
    background-color: rgb(79, 79, 79);
    justify-content: flex-start;
    border-radius: 6px;
    border-top-width: 1px;
    border-bottom-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-color: rgb(25,25,25);
}

MasterPreviewView > #top {
    flex-direction: row;
    justify-content: space-between;
    background-color: rgb(64, 64, 64);
    padding: 8px;
    border-top-left-radius: 6px;
    border-top-right-radius: 6px;
}

MasterPreviewView > #top > #title {
    overflow: hidden;
    font-size: 14px;
    color: rgb(180, 180, 180);
    padding: 1px 2px 2px;
}

MasterPreviewView > #middle {
    background-color: rgb(49, 49, 49);
    flex-grow: 1;
    flex-direction: row;
}

MasterPreviewView > #middle > #preview {
    flex-grow: 1;
    width: 100px;
    height: 100px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\MasterPreviewView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\MaterialGraph.uss---------------
.
.
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\MaterialGraph.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\MaterialGraphView.uss---------------
.
.
MaterialGraphView {
    background-color: #202020;
}

.subgraph{
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\MaterialGraphView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\MaterialNodeView.uss---------------
.
.
MaterialNodeView {
    overflow: visible;
}

MaterialNodeView.graphElement.node.MaterialNode {
    margin-top: 0;
    margin-bottom: 0;
    margin-left: 0;
    margin-right: 0;
}

MaterialNodeView.master {
    min-width: 200px;
}

MaterialNodeView.blockData {
    width: 200px;
}

MaterialNodeView.blockData > #portInputContainer {
    top: 6px;
}

MaterialNodeView #collapsible-area {
    width: 0;
    height: 0;
}

MaterialNodeView #previewFiller.expanded {
    width: 200px;
    padding-bottom: 200px;
}

MaterialNodeView #previewFiller,
MaterialNodeView #controls {
    background-color: rgba(63, 63, 63, 0.8);
}

MaterialNodeView #controls > #items {
    padding-top: 4px;
    padding-bottom: 4px;
}

MaterialNodeView #title {
    padding-top: 8px;
    border-bottom-width: 8px;
}

MaterialNodeView > #previewContainer {
    position: absolute;
    bottom: 4px;
    left: 4px;
    border-radius: 6px;
    padding-top: 6px;
}

MaterialNodeView > #previewContainer > #preview  {
    width: 200px;
    height: 200px;
    align-items:center;
}

MaterialNodeView > #previewContainer > #preview > #collapse {
    background-color: #000;
    border-color: #F0F0F0;
    width: 0;
    height: 0;
    opacity: 0;
    border-radius: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    margin-top: 4px;
    align-items:center;
    justify-content:center;
}


MaterialNodeView:hover > #previewContainer > #preview > #collapse {
    width: 20px;
    height: 20px;
    opacity: 0.6;
}

MaterialNodeView > #previewContainer > #preview > #collapse > #icon  {
    background-image : resource("GraphView/Nodes/PreviewCollapse.png");
    width: 16px;
    height: 16px;
}

MaterialNodeView > #previewContainer > #preview > #collapse:hover {
    opacity: 1.0;
}

MaterialNodeView #previewFiller > #expand {
    align-self: center;
    width: 56px;
    height: 16px;
    flex-direction: row;
    justify-content:center;
}

MaterialNodeView #previewFiller > #expand > #icon {
    background-image : resource("GraphView/Nodes/PreviewExpand.png");
    width: 16px;
    height: 16px;
}

MaterialNodeView #previewFiller.collapsed > #expand:hover {
    background-color: #2B2B2B;
}

MaterialNodeView #previewFiller.expanded > #expand {
    height: 0;
}

MaterialNodeView > #resize {
    background-image : resource("GraphView/Nodes/NodeChevronLeft.png");
    position: absolute;
    right: 5px;
    bottom: 5px;
    width: 10px;
    height: 10px;
    cursor: resize-up-left;
}

MaterialNodeView PortInputView {
    position: absolute;
    left: -224px;
}

MaterialNodeView > #settings-container {
    background-color : rgb(63, 63, 63);
}

MaterialNodeView.hovered #selection-border{
    background-color:rgba(68,192,255,0.4);
    border-color:rgba(68,192,255,1);
    border-left-width: 2px;
    border-right-width: 2px;
    border-top-width: 2px;
    border-bottom-width: 2px;
}

#settings-button {
    width: 16px;
    justify-content: center;
    padding-left: 8px;
}

#settings-button > #icon {
    width : 12px;
    height : 12px;
    align-self: center;
    visibility: hidden;
    background-image : resource("Icons/SettingsIcons");
}

.node:hover #settings-button > #icon {
    visibility: visible;
}

#settings-button:hover > #icon {
    align-self: center;
    background-color: #2B2B2B;
    background-image : resource("Icons/SettingsIcons_hover");
}

#settings-button.clicked > #icon{
    background-color: #2B2B2B;
    background-image : resource("Icons/SettingsIcons_hover");
    visibility: visible;
}

.node.collapsed > #node-border > #title > #button-container > #collapse-button > #icon {
    background-image: resource("GraphView/Nodes/NodeChevronLeft.png");
}

.node.expanded > #node-border > #title > #button-container > #collapse-button > #icon {
    background-image : resource("GraphView/Nodes/NodeChevronDown.png");
}

MaterialNodeView > #disabledOverlay {
    border-radius: 4;
    position: absolute;
    left: 4;
    right: 4;
    top: 4;
    bottom: 4;
    background-color: rgba(32, 32, 32, 0);
}

MaterialNodeView.disabled #disabledOverlay {
    background-color: rgba(32, 32, 32, 0.5);
}

MaterialNodeView.disabled:hover #disabledOverlay {
    background-color: rgba(32, 32, 32, 0.25);
}

MaterialNodeView.disabled:checked #disabledOverlay {
    background-color: rgba(32, 32, 32, 0.25);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\MaterialNodeView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\NodeSettings.uss---------------
.
.
NodeSettingsView {
    width: 362px;
    position: absolute;
    -unity-slice-top: 40;
    -unity-slice-left: 80;
    -unity-slice-right: 25;
    -unity-slice-bottom: 25;
    padding-top: 15px;
    padding-left: 4px;
    padding-right: 4px;
    padding-bottom: 4px;
    background-image: resource("Icons/Settings_Flyout_9slice");
}

NodeSettingsView > #mainContainer {
    padding-top: 4px;
    padding-left: 4px;
    padding-right: 4px;
    padding-bottom: 4px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\NodeSettings.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PixelCacheProfiler.uss---------------
.
.
PixelCacheProfilerView > #content > #title {
    -unity-font-style: bold;
}

PixelCacheProfilerView > #content > .row {
    flex-direction: row;
}

PixelCacheProfilerView > #content > .indented {
    padding-left: 8px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PixelCacheProfiler.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PortInputView.uss---------------
.
.
PortInputView {
    width: 232px;
    height: 22px;
    padding-top: 1px;
    flex-direction: row;
    justify-content: flex-end;
}

PortInputView > #container {
    background-color: rgba(63, 63, 63, 0.8);
    flex-direction: row;
    align-items: center;
    padding-left: 8px;
    margin-right: 12px;
    border-left-width: 1px;
    border-top-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-color: rgba(25, 25, 25, 0.8);
    border-radius: 2px;
}

PortInputView > #container > #disabledOverlay {
    position: absolute;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
    background-color: rgba(32, 32, 32, 0.0);
}

PortInputView.disabled > #container > #disabledOverlay {
    background-color: rgba(32, 32, 32, 0.75);
}

PortInputView > #container > #slot {
    width: 8px;
    height: 8px;
    background-color: #2B2B2B;
    border-color: #232323;
    border-top-width: 1px;
    border-bottom-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-radius: 4px;
    margin-left: 6px;
    margin-right: 6px;
    align-items: center;
    justify-content: center;
}

PortInputView > #edge {
    position: absolute;
    right: 0px;
    top: 10.5px;
    height: 2px;
    width: 20px;
    background-color: #ff0000;
}

PortInputView > #container > #slot > #dot {
    width: 4px;
    height: 4px;
    background-color: #ff0000;
    border-radius: 4px;
}

PortInputView.typeMatrix4 > #container > #slot > #dot,
PortInputView.typeMatrix3 > #container > #slot > #dot,
PortInputView.typeMatrix2 > #container > #slot > #dot {
    background-color: #8FC1DF;
}

PortInputView.typeMatrix4,
PortInputView.typeMatrix3,
PortInputView.typeMatrix2 {
    --edge-color: #8FC1DF;
}

PortInputView.typeTexture2D > #container > #slot > #dot,
PortInputView.typeTexture2DArray > #container > #slot > #dot,
PortInputView.typeTexture3D > #container > #slot > #dot,
PortInputView.typeCubemap > #container > #slot > #dot {
    background-color: #FF8B8B;
}

PortInputView.typeTexture2D,
PortInputView.typeTexture2DArray,
PortInputView.typeTexture3D,
PortInputView.typeCubemap {
    --edge-color: #FF8B8B;
}

PortInputView.typeVector4 > #container > #slot > #dot {
    background-color: #FBCBF4;
}

PortInputView.typeVector4 {
    --edge-color: #FBCBF4;
}

PortInputView.typeVector3 > #container > #slot > #dot {
    background-color: #F6FF9A;
}

PortInputView.typeVector3 {
    --edge-color: #F6FF9A;
}

PortInputView.typeVector2 > #container > #slot > #dot {
    background-color: #9AEF92;
}

PortInputView.typeVector2 {
    --edge-color: #9AEF92;
}

PortInputView.typeVector1 > #container > #slot > #dot {
    background-color: #84E4E7;
}

PortInputView.typeVector1 {
    --edge-color: #84E4E7;
}

PortInputView.typeBoolean > #container > #slot > #dot {
    background-color: #9481E6;
}

PortInputView.typeBoolean {
    --edge-color: #9481E6;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PortInputView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PropertyNameReferenceField.uss---------------
.
.
TextField.modified {
    -unity-font-style: bold;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PropertyNameReferenceField.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PropertyNodeView.uss---------------
.
.
PropertyNodeView.hovered #selection-border{
    background-color:rgba(68,192,255,0.4);
    border-color:rgba(68,192,255,1);
    border-left-width: 2px;
    border-right-width: 2px;
    border-top-width: 2px;
    border-bottom-width: 2px;
}

PropertyNodeView > #disabledOverlay {
    position: absolute;
    left: 4;
    right: 4;
    top: 4;
    bottom: 4;
    background-color: rgba(32, 32, 32, 0);
}

PropertyNodeView.disabled #disabledOverlay {
    background-color: rgba(32, 32, 32, 0.5);
}

PropertyNodeView.disabled:hover #disabledOverlay {
    background-color: rgba(32, 32, 32, 0.25);
}

PropertyNodeView.disabled:checked #disabledOverlay {
    background-color: rgba(32, 32, 32, 0.25);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PropertyNodeView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PropertyRow.uss---------------
.
.
PropertyRow EnumField,
PropertyRow PopupField,
PropertyRow TextField,
PropertyRow Label,
PropertyRow Toggle,
PropertyRow IntegerField,
PropertyRow ColorField,
PropertyRow DoubleField {
    margin-left: 0;
    margin-right: 0;
    -unity-text-align : middle-left;
}

PropertyRow > #container{
    flex-grow: 1;
    padding-left: 8px;
    padding-right: 16px;
    flex-direction: row;
}

PropertyRow > #container > #label {
    flex-grow: 2;
    flex-basis: 0;
    min-width: 184px;
    width: 92px;
    font-size: 12px;
    margin-right: 4px;
    justify-content: center;
}

PropertyRow > #container > #label > Label {
    margin-bottom: 3px;
    flex-wrap: wrap;
    text-overflow: ellipsis; /* Possible values: clip | ellipsis */
    -unity-text-overflow-position: end; /* Possible values: start | middle | end */
    /* Conditions */
    overflow: hidden;
    white-space: wrap;
}

PropertyRow > #container > #label > Label.modified {
    -unity-font-style: bold;
}

PropertyRow > #container > #content{
    flex-grow: 10;
    flex-basis: 0;
    height: auto;
    -unity-font-style: bold;
    min-width: 64px;
    width: 100px;
    justify-content: center;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PropertyRow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PropertySheet.uss---------------
.
.
PropertySheet {
    margin-top: 4px;
    margin-bottom: 4px;
    padding-left: 1px;
    padding-right: 1px;
    border-width: 0.5px;
    border-color: dimgray;
    border-radius: 5px;
}

PropertySheet > #content > #error {
    -unity-font-style: bold;
    max-height: 10px;
    margin-bottom: 15px;
}

PropertySheet > #content > #error > Label{
    -unity-font-style: bold;
    color: yellow;
}

PropertySheet > #content > #header
{
    top: 1px;
    border-color: dimgray;
    border-radius: 5px;
    background-color: #393939;
}

PropertySheet > #content > #header > Label{
    left: 2px;
    margin-top: 4px;
    font-size: 15px;
    color: #a4a4a4;
    -unity-font-style: bold;
}

PropertySheet > #content > #foldout {
    margin-left: 21px;
}

PropertySheet >.unity-label
{
    background-color: #1F1F1F;
    font-size: 11px;
    -unity-font-style: bold;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\PropertySheet.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\RedirectNode.uss---------------
.
.
.node {
    min-height: 0px;
    width: 56px;
    border-radius: 8px;
    background-color: rgba(63, 63, 63, 1.0);
    padding: 0px;
    border-width: 0px;
}

#node-border {
    margin: 0px;
    border-radius: 8px;
    /* The proper opacity of the node border is 0.8 but transparent
    border are not working properly so it's fully opaque.*/
    border-color: rgba(25,25,25,1.0);
    border-width: 0px;
}

#selection-border {
    border-width: 2px;
    border-radius: 8px;
    margin: 0px;
    position: absolute;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
}

#title {
    flex-direction: row;
    justify-content: space-between;
    background-color: rgba(63,63,63,0.0);
    border-color: rgba(0,0,0,0);
    height: 0;
}

#input {
    width: 50%;
}

#output {
    width: 50%;
}

#contents > #top > #input {
    padding: 0px;
}

#contents > #top > #output {
    padding: 0px;
}

#divider.horizontal {
    width: 0.0px;
    border-right-width: 1px;
}

.port.input {
    width: 100%;
    padding: 0px;
}

.port.output {
    width: 100%;
    padding: 0px;
}

.port.input > #connector {
    margin-left: 6px;
    margin-right: 0px;
}

.port.output > #connector {
    margin-left: 0px;
    margin-right: 6px;
}

.port.input > #type {
    visibility: hidden;
    padding: 0px;
    margin: 0px;
    width: 0%;
    font-size: 1px;
    color: rgba(0,0,0,0);
}

.port.output > #type {
    visibility: hidden;
    padding: 0px;
    margin: 0px;
    width: 0%;
    font-size: 1px;
    color: rgba(0,0,0,0);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\RedirectNode.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\ReorderableSlotListView.uss---------------
.
.
.unity-imgui-container {
    margin-top: 4;
    margin-bottom: 4;
    margin-left: 8;
    margin-right: 8;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\ReorderableSlotListView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\SGBlackboard.uss---------------
.
.
.unity-label {
    padding-top: 1px;
    padding-left: 2px;
    padding-right: 2px;
    padding-bottom: 2px;
    margin-left: 4px;
    margin-top: 2px;
    margin-right: 4px;
    margin-bottom: 2px;
}

#SGBlackboard {
    position:absolute;
    flex-direction: column;
    background-color: rgba(0,0,0,0);
    border-color: rgba(0,0,0,0);
    min-width: 100px;
    min-height: 100px;
    width: 200px;
    height: 400px;
}

#SGBlackboard.windowed {
    position: relative;
    padding-top: 0;
    flex: 1;
    border-left-width: 0;
    border-top-width: 0;
    border-right-width: 0;
    border-bottom-width: 0;
    border-radius: 0;
    width: initial;
    height: initial;
}

#SGBlackboard > #subTitleTextField {
    position: relative;
    font-size: 11px;
    margin-left: 0;
    left: 5;
    top: 5;
    width: auto;
    visibility: hidden;
}

#SGBlackboard.windowed > .resizer {
    display: none;
}

#SGBlackboard > #categoryDragIndicator {
    background-color: cornflowerblue;
    min-height: 4px;
    left:0;
    right:0;
    padding-top: 2px;
    padding-bottom: 2px;
}

#SGBlackboard:selected {
    border-color: #44C0FF;
}

#SGBlackboard > .mainContainer {
    border-left-width: 1px;
    border-top-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-radius: 5px;
    background-color: #2b2b2b;
    border-color: #191919;
    margin: 6px;
    flex-direction: column;
    align-items: stretch;
}

#SGBlackboard.scrollable > .mainContainer {
    position: absolute;
    top:0;
    left:0;
    right:0;
    bottom:0;
}

#SGBlackboard > .mainContainer > #content {
    flex-direction: column;
    align-items: stretch;
}

#SGBlackboard.scrollable > .mainContainer > #content {
    position: absolute;
    top:0;
    left:0;
    right:0;
    bottom:0;
    flex-direction: column;
    align-items: stretch;
}

#SGBlackboard.scrollable > .mainContainer > #content > #scrollBoundaryTop
{
    position: relative;
    align-content: flex-start;
    opacity: 0.1;
    min-height: 15;
    -unity-background-image-tint-color: aqua;
    background-color: lightgrey;
    border-bottom-right-radius: 15px;
    border-bottom-left-radius: 15px;
}

#SGBlackboard.scrollable > .mainContainer > #content > #scrollBoundaryBottom
{
    position: relative;
    align-content: flex-end;
    min-height: 15;
    opacity: 0.1;
    -unity-background-image-tint-color: aqua;
    background-color: lightgrey;
    border-top-right-radius: 15px;
    border-top-left-radius: 15px;
}

#SGBlackboard > .mainContainer > #content > ScrollView {
    flex: 1 0 0;
    align-content: center;
}

#SGBlackboard > .mainContainer > #content > #contentContainer {
    min-height: 50px;
    padding-bottom: 15px;
    flex-direction: column;
    align-items: stretch;
}

#SGBlackboard > .mainContainer > #content > #header {
    overflow: hidden;
    flex-direction: row;
    align-items: stretch;
    background-color: #393939;
    border-bottom-width: 1px;
    border-color: #212121;
    border-top-right-radius: 4px;
    border-top-left-radius: 4px;
    padding-left: 1px;
    padding-top: 4px;
    padding-bottom: 2px;
}

#SGBlackboard.windowed > .mainContainer > #content > #header {
    border-top-right-radius: 0;
    border-top-left-radius: 0;
}

#SGBlackboard > .mainContainer > #content > #header > #labelContainer {
    flex: 1 0 0;
    flex-direction: column;
    align-items: stretch;
}

#SGBlackboard > .mainContainer > #content > #header > #addButton {
    align-self:center;
    font-size: 20px;
    margin-top:15px;
    margin-bottom:3px;
    margin-left:4px;
    margin-right:4px;
    border-color: transparent;
    background-color: transparent;
}

#SGBlackboard > .mainContainer > #content > #header > #addButton:hover {
    background-color: #676767;
    /* theme-button-background-color */
    border-color: #222222;
    /* -theme-app-toolbar-button-border-color */
}

#SGBlackboard > .mainContainer > #content > #header > #addButton:hover:active {
    background-color: #747474;
    /* theme-button-background-color */
    border-color: #222222;
    /* -theme-app-toolbar-button-border-color */
}

#SGBlackboard > .mainContainer > #content > #header > #labelContainer > #titleLabel {
    font-size : 14px;
    color: #c1c1c1;
}

#SGBlackboard > .mainContainer > #content > #header > #labelContainer > #subTitleLabel {
    font-size: 11px;
    color: #606060;
}

#SGBlackboard.scrollable > .mainContainer > #content > .unity-scroll-view > .unity-scroller--horizontal {
    background-color: #393939;
}

#SGBlackboard.scrollable > .mainContainer > #content > .unity-scroll-view > .unity-scroller--vertical {
    background-color: #393939;
}

.blackboardCategory {
    padding: 2px;
    border-color: dimgray;
    border-width: 0.5px;
    border-radius: 3px;
}

.blackboardCategory  > .mainContainer > #categoryHeader {
    flex-direction: row;
    align-items: stretch;
}

.blackboardCategory > .mainContainer > #categoryHeader > #categoryTitleLabel {
    color: #606060;
    font-size: 11px;
}

.blackboardCategory > .mainContainer > #categoryHeader > #textField {
    position: absolute;
    top:0;
    left:0;
    right:0;
    bottom:0;
    -unity-text-align:middle-left;
    -unity-font-color: red;
    font-size: 11px;
}

.blackboardCategory > #dragIndicator {
    background-color: #44C0FF;
    position: absolute;
    min-height: 2px;
    height:4px;
    margin-bottom: 1;
}

.blackboardCategory.selected {
    border-color: cornflowerblue;
}

.blackboardCategory.unnamed {
    border-width: 0;
    border-radius: 0;
    border-color: black;
}
.blackboardCategory.hovered {
    border-color: lightskyblue;
}

#SGBlackboardRow {
    left: 1px;
    right: 1px;
    padding-left: 4px;
    padding-right: 8px;
}

#SGBlackboardRow  > .mainContainer > #root > #itemRow {
    flex-direction: row;
}

#SGBlackboardRow > .mainContainer > #root > #itemRow > #itemRowContentContainer {
    flex: 1 0 0;
    align-items: stretch;
}

#SGBlackboardRow > .mainContainer > #root > #itemRow > #itemRowContentContainer > #itemContainer {
    flex-direction: row;
    align-items: stretch;
}

#SGBlackboardRow > .mainContainer > #root > #itemRow > #expandButton {
    align-self: center;
    background-image: none;
    background-color: #2A2A2A;
    /* theme-input-background-color */
    border-left-width: 0;
    border-top-width: 0;
    border-right-width: 0;
    border-bottom-width: 0;
    margin: 0;
    padding: 0;
}

#SGBlackboardRow > .mainContainer > #root > #itemRow > #expandButton > #buttonImage {
    --unity-image : resource("GraphView/Nodes/NodeChevronRight.png");
    width: 12px;
    height: 12px;
}

#SGBlackboardRow.expanded > .mainContainer > #root > #itemRow > #expandButton > #buttonImage {
    --unity-image : resource("GraphView/Nodes/NodeChevronDown.png");
}

#SGBlackboardRow > .mainContainer > #root > #itemRow > #expandButton:hover > #buttonImage {
    background-color: #212121;
    border-radius: 1px;
}

#SGBlackboardRow > .mainContainer > #root > #propertyViewContainer {
}

#SGBlackboardRow.hovered #pill #selection-border
{
    background-color:rgba(68,192,255,0.4);
    border-color: cornflowerblue;
    border-left-width: 2px;
    border-top-width: 2px;
    border-right-width: 2px;
    border-bottom-width: 2px;
    left: 1px;
    right: 1px;
    top: 1px;
    bottom: 1px;
}

#SGBlackboardField {
    flex: 1 0 0;
    flex-direction: row;
    align-items: stretch;
}

#SGBlackboardField > .mainContainer {
    flex: 1 0 0;
    flex-direction: row;
    align-items: stretch;
}

#SGBlackboardField > .mainContainer > #contentItem {
    flex: 1 0 0;
    flex-direction: row;
    align-items: stretch;
}

#SGBlackboardField > .mainContainer > #textField {
    position: absolute;
    left:0;
    right:0;
    bottom:0;
    -unity-text-align:middle-left;
    -unity-font-color: red;
    font-size: 11px;
}

#SGBlackboardField > .mainContainer > #contentItem > #typeLabel {
    flex: 1 0 0;
    -unity-text-align:middle-right;
    color: #808080;
    font-size: 11px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\SGBlackboard.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\ShaderPort.uss---------------
.
.
/* THIS FILE IS FROM GRAPHVIEW BUT CONTAINS MODIFICATIONS */

ShaderPort {
    height: 24px;
    align-items: center;
    padding-left: 4px;
    padding-right: 4px;
    --port-color: rgb(200, 200, 200);
    --disabled-port-color: rgb(70, 70, 70);
}

ShaderPort.input {
    flex-direction: row;
}

ShaderPort.output {
    flex-direction: row-reverse;
}

ShaderPort > #connector {
    border-color: rgb(70, 70, 70);
    background-color: #212121;
    width: 8px;
    height: 8px;
    border-radius: 8px;
    align-items: center;
    justify-content: center;

    margin-left: 4px;
    margin-right: 4px;
    border-left-width: 1px;
    border-top-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
}

ShaderPort > #connector:hover {
    border-color: #f0f0f0
}

ShaderPort > #connector > #cap
{
    background-color: #212121;
    width: 4px;
    height: 4px;
    border-radius: 4px;
}

ShaderPort > #connector > #cap:hover
{
    background-color: #f0f0f0;
}

ShaderPort > #connector.portHighlight {
    border-color: #f0f0f0;
}

ShaderPort > #type {
    color: #c1c1c1;
    font-size: 11px;
    height: 16px;
    padding-left: 0;
    padding-right: 0;
    margin-left: 4px;
    margin-right: 4px;
    margin-top: 4px;
}

ShaderPort.input > #type {
    -unity-text-align: middle-left;
}

ShaderPort.output > #type {
    -unity-text-align:middle-right;
}

/*******************************/
/* ShaderPorts colors by types */
/*******************************/

ShaderPort.typeTexture {
    --port-color:#FF8B8B;
}

ShaderPort.typeTexture.inactive {
    --port-color:#7F5C5C;
}

ShaderPort.typeTexture2D {
    /* Same as typeTexture */
    --port-color:#FF8B8B;
}

ShaderPort.typeTexture2D.inactive {
    /* Same as typeTexture */
    --port-color:#7F5C5C;
}

ShaderPort.typeTexture2DArray {
    /* Same as typeTexture */
    --port-color:#FF8B8B;
}

ShaderPort.typeTexture2DArray.inactive {
    /* Same as typeTexture */
    --port-color:#7F5C5C;
}

ShaderPort.typeTexture3D {
    /* Same as typeTexture */
    --port-color:#FF8B8B;
}

ShaderPort.typeTexture3D.inactive {
    /* Same as typeTexture */
    --port-color:#7F5C5C;
}

ShaderPort.typeCubemap {
    /* Same as typeTexture */
    --port-color:#FF8B8B;
}

ShaderPort.typeCubemap.inactive {
    /* Same as typeTexture */
    --port-color:#7F5C5C;
}

ShaderPort.typeGraphScript {
    /* Todo: there is no such type in Unity atm */
    --port-color:#E681BA;
}

ShaderPort.typeGraphScript.inactive {
    /* Todo: there is no such type in Unity atm */
    --port-color:#73405D;
}

ShaderPort.typeFloat4 {
    --port-color:#FBCBF4;
}

ShaderPort.typeFloat4.inactive {
    --port-color:#7D657A;
}

ShaderPort.typeVector4 {
    /* Same as typeFloat4 */
    --port-color:#FBCBF4;
}

ShaderPort.typeVector4.inactive {
    /* Same as typeFloat4 */
    --port-color:#7D657A;
}

ShaderPort.typeQuaternion {
    /* Same as typeFloat4 */
    --port-color:#FBCBF4;
}

ShaderPort.typeQuaternion.inactive {
    /* Same as typeFloat4 */
    --port-color:#7D657A;
}

ShaderPort.typeColor {
    /* Same as typeFloat4 */
    --port-color:#FBCBF4;
}

ShaderPort.typeColor.inactive {
    /* Same as typeFloat4 */
    --port-color:#7D657A;
}

ShaderPort.typeInt {
    --port-color:#9481E6;
}

ShaderPort.typeInt.inactive {
    --port-color:#4A4073;
}

ShaderPort.typeInt32 {
    /* Same as typeInt */
    --port-color:#9481E6;
}

ShaderPort.typeInt32.inactive {
    /* Same as typeInt */
    --port-color:#4A4073;
}

/* TEMP STUFF THAT SHOULD ACTUALLY STAY IN GRAPHVIEW */
ShaderPort.typeInt64 {
    /* Same as typeInt */
    /* todo we might want to differentiate that from int32 */
    --port-color:#9481E6;
}

ShaderPort.typeInt64.inactive {
    /* Same as typeInt */
    /* todo we might want to differentiate that from int32 */
    --port-color:#4A4073;
}

ShaderPort.typeBoolean {
    --port-color:#9481E6;
}

ShaderPort.typeBoolean.inactive {
    --port-color:#4A4073;
}

ShaderPort.typeMatrix {
    --port-color:#8FC1DF;
}

ShaderPort.typeMatrix.inactive {
    --port-color:#47606F;
}

ShaderPort.typeMatrix4x4 {
    /* Same as typeMatrix */
    --port-color:#8FC1DF;
}

ShaderPort.typeMatrix4x4.inactive {
    /* Same as typeMatrix */
    --port-color:#47606F;
}

ShaderPort.typeGameObject {
    --port-color:#8FC1DF;
}

ShaderPort.typeGameObject.inactive {
    --port-color:#47606F;
}

ShaderPort.typeFloat {
    --port-color:#84E4E7;
}

ShaderPort.typeFloat.inactive {
    --port-color:#427273;
}

ShaderPort.typeFloat1 {
    /* Same as typeFloat */
    --port-color:#84E4E7;
}

ShaderPort.typeFloat1.inactive {
    /* Same as typeFloat */
    --port-color:#427273;
}

ShaderPort.typeSingle {
    /* Same as typeFloat */
    --port-color:#84E4E7;
}

ShaderPort.typeSingle.inactive {
    /* Same as typeFloat */
    --port-color:#427273;
}

ShaderPort.typeDouble {
    /* Same as typeFloat */
    /* todo we might want to differentiate that from float */
    --port-color:#84E4E7;
}

ShaderPort.typeDouble.inactive {
    /* Same as typeFloat */
    /* todo we might want to differentiate that from float */
    --port-color:#427273;
}

ShaderPort.typeFloat2 {
    --port-color:#9AEF92;
}

ShaderPort.typeFloat2.inactive {
    --port-color:#4D7749;
}

ShaderPort.typeVector2 {
    /* Same as typeFloat2 */
    --port-color:#9AEF92;
}

ShaderPort.typeVector2.inactive {
    /* Same as typeFloat2 */
    --port-color:#4D7749;
}

ShaderPort.typeComponent {
    --port-color:#C9F774;
}

ShaderPort.typeComponent.inactive {
    --port-color:#647B3A;
}

ShaderPort.typeFloat3 {
    --port-color:#F6FF9A;
}

ShaderPort.typeFloat3.inactive {
    --port-color:#7B7F4D;
}

ShaderPort.typeVector3 {
    /* Same as typeFloat3 */
    --port-color:#F6FF9A;
}

ShaderPort.typeVector3.inactive {
    /* Same as typeFloat3 */
    --port-color:#7B7F4D;
}

ShaderPort.typeString {
    --port-color:#FCD76E;
}

ShaderPort.typeString.inactive {
    --port-color:#7E6B37;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\ShaderPort.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\TabbedView.uss---------------
.
.
unity-tabbed-view {

}

.unity-tabbed-view__tabs-container {
    display: flex;
    background-color: #252525;
    flex-direction: row;
}

.unity-tabbed-view__content-container {
    flex-grow: 1;
    padding-left: 4;
    padding-top: 4;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\TabbedView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\TabButtonStyles.uss---------------
.
.
.unity-tab-button {

}

.unity-tab-button:hover {
    background-color: #303030;
}

.unity-tab-button__top-bar {
    height: 2px;
    border-top-left-radius: 2px;
    border-top-right-radius: 2px;
}

.unity-tab-button--active .unity-tab-button__top-bar {
    background-color: #3A79BB;
}

.unity-tab-button__content {
    flex-direction: row;
        height: 18px;
        padding: 0px 6px 2px 6px;
        align-items: center;
        overflow: hidden;
}

.unity-tab-button--active .unity-tab-button__content {
    background-color: #2E2E2E;
}

.unity-tab-button__content-icon {
    width: 12px;
    height: 12px;
    flex-shrink: 0;
}

.unity-tab-button__content-label {
    padding-left: 4px;
    padding-right: 4px;
    -unity-text-align: middle-left;
    flex-shrink: 1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\TabButtonStyles.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\BooleanSlotControlView.uss---------------
.
.
BooleanSlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
    width: 20px;
    margin-top: 1px;
    margin-bottom: 3px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\BooleanSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ChannelEnumControlView.uss---------------
.
.
ChannelEnumControlView {
    flex-direction: row;
}

ChannelEnumControlView > .unity-popup-field {
    width: 76px;
    margin-left: 0;
    margin-right: 8px;
    margin-top: 4px;
    margin-bottom: 4px;
}

ChannelEnumControlView > Label {
    max-width: 100px;
    width: 50px;
    margin-left: 8px;
    margin-right: 8px;
    -unity-text-align : middle-left;
    flex-grow: 1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ChannelEnumControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ChannelEnumMaskControlView.uss---------------
.
.
ChannelEnumMaskControlView {
    flex-direction: row;
    padding-left: 8px;
    padding-right: 8px;
    padding-top: 4px;
    padding-bottom: 4px;
    width: 200px;
}

ChannelEnumMaskControlView > IMGUIContainer {
    flex-direction: row;
    flex: 1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ChannelEnumMaskControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ChannelMixerControlView.uss---------------
.
.
ChannelMixerControlView {
    padding-left: 8px;
    padding-right: 8px;
    padding-top: 4px;
    padding-bottom: 4px;
}

ChannelMixerControlView > Label {
    margin-left: 0;
    margin-right: 0;
    cursor: slide-arrow;
    -unity-text-align : middle-left;
}

ChannelMixerControlView > #buttonPanel {
    flex-direction: row;
    flex-grow: 1;
    margin-bottom: 6px;
}

ChannelMixerControlView > #buttonPanel > Button {
    flex-grow: 1;
    margin-left: 1;
    margin-right: 1;
    align-items: center;
    border-left-width: 0;
    border-top-width: 0;
    border-right-width: 0;
    border-bottom-width: 1;
}

ChannelMixerControlView > #buttonPanel > Button > Label {
    flex-grow: 1;
    -unity-text-align : middle-left;
}

ChannelMixerControlView > #sliderPanel {
    flex-direction: row;
    flex-grow: 1;
}

ChannelMixerControlView > #sliderPanel > Label {
    -unity-text-align : middle-left;
    min-width: 20px;
}

ChannelMixerControlView > #sliderPanel > Slider {
    flex-grow: 1;
    overflow:visible;
}

ChannelMixerControlView > #sliderPanel > Slider > .unity-base-field__input {
}

ChannelMixerControlView > #sliderPanel > FloatField {
    margin-right: 0;
    padding-right: 0;
    width: 40px;
}

ChannelMixerControlView > #sliderPanel > FloatField > .unity-base-field__input {
    -unity-text-align : middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ChannelMixerControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ColorControlView.uss---------------
.
.
ColorControlView > ColorField {
    flex-direction: row;
}

ColorControlView > #enumPanel {
    flex-direction: row;
}

ColorControlView > ColorField {
    margin-left: 8px;
    margin-right: 8px;
    width: 184px;
}

ColorControlView > #enumPanel > EnumField {
    flex: 1;
    height: 19px;
}

ColorControlView > #enumPanel > EnumField > .unity-base-field__input{
    margin-left: 0;
    margin-right: 8px;
    margin-top: 4px;
    margin-bottom: 0;
}

ColorControlView > #enumPanel > Label {
    width: 100px;
    margin-left: 8px;
    margin-right: 8px;
    -unity-text-align: middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ColorControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ColorRGBASlotControlView.uss---------------
.
.
ColorRGBASlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
    width: 41px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input {
    margin-left: 0;
    margin-right: 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ColorRGBASlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ColorRGBSlotControlView.uss---------------
.
.
ColorRGBSlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
    width: 41px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input {
    margin-left: 0;
    margin-right: 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ColorRGBSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\CubemapSlotControlView.uss---------------
.
.
CubemapSlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
    width: 100px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-object-field .unity-base-field__input {
     margin-top: 0;
    margin-bottom: 0;
    margin-left: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}

.unity-object-field__object{
    overflow:hidden;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\CubemapSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\DielectricSpecularControlView.uss---------------
.
.
DielectricSpecularControlView {
    padding-left: 8px;
    padding-right: 8px;
}

DielectricSpecularControlView > #enumPanel {
    flex-direction: row;
    flex-grow: 1;
}

DielectricSpecularControlView > #enumPanel > Label {
    width: 60px;
    -unity-text-align : middle-left;
}

DielectricSpecularControlView > #enumPanel > EnumField {
    flex-grow: 1;
    margin-top: 4px;
    margin-bottom: 4px;
}

DielectricSpecularControlView > #enumPanel > EnumField .unity-base-field__input {
    flex-grow: 1;
    margin-left: 0;
    margin-right: 0;
}

DielectricSpecularControlView .unity-enum-field__text {
    margin-left: 5px;
    margin-right: 10px;
}

DielectricSpecularControlView > #sliderPanel {
    flex-direction: row;
    flex-grow: 1;
}

DielectricSpecularControlView > #sliderPanel > Label {
    width: 60px;
    -unity-text-align : middle-left;
}

DielectricSpecularControlView > #sliderPanel > Slider {
    flex-grow: 1;
    overflow:visible;
}

DielectricSpecularControlView > #sliderPanel > Slider .unity-base-field__input {
    overflow:visible;
}

DielectricSpecularControlView > #sliderPanel > FloatField {
    margin-right: 0;
    padding-right: 0;
    width: 40px;
}

DielectricSpecularControlView > #sliderPanel > FloatField > .unity-base-field__input{
    margin-left: 4px;
    margin-right: 0;
    -unity-text-align: middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\DielectricSpecularControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\EnumControlView.uss---------------
.
.
EnumControlView {
    flex-direction: row;
    align-items: center;
    margin-left: 8px;
    margin-right: 5px;
}

.unity-base-field {
    width: 80px;
    margin-top: 4px;
    margin-bottom: 4px;
}

.unity-base-field__input {
    margin-left: 0;
}

EnumControlView > Label {
    max-width: 140px;
    width: 30px;
    -unity-text-align: middle-left;
    flex-grow: 1;
    margin-right: 8px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\EnumControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\EnumConversionControlView.uss---------------
.
.
EnumConversionControlView {
    flex-direction: row;
    padding-left: 5px;
    padding-right: 5px;
    padding-top: 4px;
    padding-bottom: 4px;
}


.unity-base-field {
    width: 80px;
    flex:1;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input {
    flex:1;
    margin-left: 0;
    margin-right: 0;
    margin-top: 0;
    margin-bottom: 0;
}

EnumConversionControlView > Label {
    margin-left: 4px;
    margin-right: 4px;
    -unity-text-align : middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\EnumConversionControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\GradientControlView.uss---------------
.
.
GradientControlView > #gradientPanel {
    flex-direction: row;
}

GradientControlView > #gradientPanel > GradientField {
    margin-left: 8px;
    margin-right: 8px;
    width: 184px;
}

.unity-base-field {
    flex:1;
    margin-top: 1px;
    margin-bottom: 1px;
}

GradientControlView > #gradientPanel > Label {
    width: 100px;
    margin-left: 8px;
    margin-right: 8px;
    -unity-text-align : middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\GradientControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\GradientSlotControlView.uss---------------
.
.
GradientSlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
    width: 41px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input{
    margin-left: 0;
    margin-right: 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\GradientSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\IntegerControlView.uss---------------
.
.
IntegerControlView {
    flex-direction: row;
    padding-left: 8px;
    padding-right: 8px;
    padding-top: 4px;
    padding-bottom: 4px;
}

.unity-base-field {
    width: 100px;
    flex-grow: 1;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input{
    margin-left: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\IntegerControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\MultiFloatControlView.uss---------------
.
.
MultiFloatControlView {
    flex-direction: row;
    padding-left: 8px;
    padding-right: 8px;
    padding-top: 4px;
    padding-bottom: 4px;
}

MultiFloatControlView > #dummy > Label {
    margin-left: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}


.unity-base-field {
    min-width: 30px;
    flex-grow: 1;
    margin-top: 1px;
    margin-bottom: 1px;
    margin-left: 0;
    margin-right: 0;
    padding-left: 0;
    padding-right: 0;
    padding-top: 0;
    padding-bottom: 0;
}

.unity-base-field__input{
    margin-left: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\MultiFloatControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\MultiFloatSlotControlView.uss---------------
.
.
MultiFloatSlotControlView {
    flex-direction: row;
    align-items: center;
}

#dummy > Label {
    margin-left: 0;
    margin-right: 0;
    margin-bottom: 0;
    cursor: slide-arrow;
    -unity-text-align: middle-left;
}

.unity-base-field {
    width: 30px;
    margin-top: 1px;
    margin-bottom: 1px;
    -unity-text-align: middle-left;
}

.unity-base-field__input{
    margin-left: 0;
    margin-right: 0;
}

FloatField.unity-double-field {
    padding-left: 0;
    padding-right: 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\MultiFloatSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\MultiIntegerSlotControlView.uss---------------
.
.
MultiIntegerSlotControlView {
    flex-direction: row;
    align-items: center;
}

#dummy > Label {
    margin-left: 0;
    margin-right: 0;
    margin-bottom: 0;
    cursor: slide-arrow;
    -unity-text-align: middle-left;
}

.unity-base-field {
    width: 30px;
    margin-top: 1px;
    margin-bottom: 1px;
    -unity-text-align: middle-left;
}

.unity-base-field__input{
    margin-left: 0;
    margin-right: 0;
}

IntegerField.unity-integer-field {
    padding-left: 0;
    padding-right: 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\MultiIntegerSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\PopupControlView.uss---------------
.
.
PopupControlView {
    padding-top: 4px;
    padding-bottom: 4px;
    flex-direction: row;
}

PopupControlView > PopupField {
    width: 80px;
    flex: 1;
    margin-left: 0;
    margin-right: 8px;
}

PopupControlView > Label {
    width: 94px;
    margin-left: 8px;
    margin-right: 8px;
    -unity-text-align: middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\PopupControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ScreenPositionSlotControlView.uss---------------
.
.
.unity-base-field {
     width: 54px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input{
    margin-top: 0;
    margin-bottom: 0;
    margin-left: 0;
    margin-right: 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ScreenPositionSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\SliderControlView.uss---------------
.
.
SliderControlView {
    padding-left: 8px;
    padding-right: 8px;
    padding-top: 4px;
    padding-bottom: 4px;
}

SliderControlView > #SliderPanel {
    padding-bottom: 4px;
    flex-direction: row;
    flex-grow: 1;
}

.unity-slider,
.unity-slider .unity-base-field__input {
    margin-left: 0;
    margin-right: 0;
    flex-grow: 1;
    min-width: 164px;
    overflow:visible;
}

SliderControlView > #SliderPanel > FloatField {
    width: 40px;
    padding-right: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}

SliderControlView > #FieldsPanel {
    flex-direction: row;
    flex-grow: 1;
    margin-left: 0;
    margin-right: 0;
}

SliderControlView > #FieldsPanel > Label {
    -unity-text-align: middle-left;
    margin-right: 8px;
    padding-left: 4px;
}

SliderControlView > #FieldsPanel > FloatField {
    margin-left: 0;
    margin-right: 0;
    padding-right: 0;
    flex-direction: row;
    flex-grow: 1;
    width: 40px;
    -unity-text-align: middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\SliderControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\TextControlView.uss---------------
.
.
TextCotrolView {
    -unity-text-align: middle-left;

}

TextControlView > #container{
    flex-direction: row;
    flex-grow: 1;
    padding-left: 8px;
    padding-right: 16px;
    align-items: center;
}

TextCotrolView > #container > Label {
    flex-wrap: wrap;
    max-width: 94px;
    width: 30px;
    flex-grow: 1;

}

.unity-base-field {
    width: 60px;
    flex-grow: 1;
    margin-left: 20%;
    margin-right: 0;
    padding-right: 10;
}

.unity-base-field__input {
    margin-left: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\TextControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\Texture3DSlotControlView.uss---------------
.
.
Texture3DSlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
     width: 100px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input{
    margin-top: 0;
    margin-bottom: 0;
    margin-left: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}

.unity-object-field__object{
    overflow:hidden;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\Texture3DSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\TextureArraySlotControlView.uss---------------
.
.
TextureArraySlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
     width: 100px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input{
    margin-top: 0;
    margin-bottom: 0;
    margin-left: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}

.unity-object-field__object{
    overflow:hidden;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\TextureArraySlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\TextureSlotControlView.uss---------------
.
.
TextureSlotControlView {
    flex-direction: row;
    align-items: center;
}

.unity-base-field {
     width: 100px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input{
    margin-top: 0;
    margin-bottom: 0;
    margin-left: 0;
    margin-right: 0;
    -unity-text-align: middle-left;
}

.unity-object-field__object{
    overflow:hidden;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\TextureSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ToggleControlView.uss---------------
.
.
ToggleControlView > #togglePanel {
    padding-left: 8px;
    flex-direction: row;
    flex-grow: 1;
}

ToggleControlView > #togglePanel > Label {
    width: 100px;
    margin-left: 8px;
    margin-right: 8px;
    -unity-text-align : middle-left;
    flex-grow:1;
}

.unity-toggle{

    align-self: center;
}

.unity-toggle .unity-base-field__input{
    margin-right: 8px;
    margin-top: 4px;
    margin-bottom: 4px;
    align-self: center;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\ToggleControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\UVSlotControlView.uss---------------
.
.
.unity-base-field {
    width: 45px;
    margin-top: 1px;
    margin-bottom: 1px;
}

.unity-base-field__input{
    margin-top: 0;
    margin-bottom: 0;
    margin-left: 0;
    margin-right: 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.shadergraph\Editor\Resources\Styles\Controls\UVSlotControlView.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\ObjectPropertyRM.uss---------------
.
.
#PickIcon
{
    width: 12px;
    height: 12px;
    background-image: var(--unity-icons-picker);
}

#PickButton
{
    margin: 0px 1px 0px -21px;
    padding: 0;
    border-width: 0;
    border-bottom-left-radius: 0px;
    border-top-left-radius: 0px;
    width:16px;
    height:15px;
    justify-content: center;
    align-items: center;
    align-self: center;
    background-color: var(--unity-colors-object_field_button-background);
}

#PickLabel
{
    flex-grow: 1;
    flex-shrink: 1;
    margin: 1px 0;
}


#ValueIcon
{
    margin-left: 2px;
    position: absolute;
    align-self: center;
    width: 12px;
    height: 12px;
}

#PickLabel > TextInput
{
    margin-right: 4px;
    padding: 0 22px 0 18px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\ObjectPropertyRM.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\PropertyRM.uss---------------
.
.
.propertyrm
{
    flex-direction: row;
    align-items:center;
    align-self: stretch;
}

.unity-base-field {
    margin: 0;
    padding: 1px 0;
}

.propertyrm.expandable
{
    padding-left: 10px;
    background-size: 12px 12px;
    background-position-x: left;
    background-image:resource("Builtin Skins/DarkSkin/Images/IN foldout.png");
}

.propertyrm.expandable:hover
{
    background-image:resource("Builtin Skins/DarkSkin/Images/IN foldout focus.png");
}

.propertyrm.expandable:hover:active
{
    background-image:resource("Builtin Skins/DarkSkin/Images/IN foldout act.png");
}

.propertyrm.expandable.icon-expanded
{
    background-image:resource("IN foldout on.png");
}

.propertyrm.expandable.icon-expanded:hover
{
    background-image:resource("IN foldout focus on.png");
}
.propertyrm.expandable.icon-expanded:active
{
    background-image:resource("IN foldout act on.png");
}

.propertyrm.hasDepth {
    border-left-width: 1px;
    border-left-color: #4c4c4c;
}

.propertyrm.hasDepth > Label {
    padding-left: 20px;
 }

.propertyrm.expandable.hasDepth {
    background-position-x: 4px;
}

.propertyrm #spacebutton
{
    border-radius: 3px;
    width: 22px;
    height: 16px;
    margin-right: 8px;
    padding-left: 16px;
    font-size: 6px;
    -unity-text-align: middle-center;
    background-position: left;
    background-size: 14px 14px;
}

.propertyrm > Label
{
    margin-left: 4px;
    height: 16px;
    overflow:hidden;
    -unity-text-align:middle-left;
}

.propertyrm > VFXEnumField.fieldContainer .unity-enum-field__input
{
    height: 16px;
    margin-left: 8px;
    flex-grow: 1;
}

.propertyrm VFXMatrix4x4Field.fieldContainer
{
    flex:1 0 auto;
    flex-direction: column;
}

.propertyrm #spacebutton.World {
    background-image : url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_WorldSpace@2x.png");
}
.propertyrm VFXMatrix4x4Field #matrixContainer {
    flex-grow: 1;
}

.propertyrm #spacebutton.Local {
    background-image : url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_LocalSpace@2x.png");
}
.propertyrm VFXMatrix4x4Field #matrixLine {
    flex-grow: 1;
    flex-direction: row;
}

.propertyrm #spacebutton.None {
    background-image : url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_NoneSpace@2x.png");
}
.propertyrm VFXMatrix4x4Field Label {
    width: 18px;
    margin-left: 4px;
}

.propertyrm #spacebutton:hover {
    background-color: #515151;
    color: var(--unity-colors-default-text);
}

.propertyrm .unity-base-text-field *
{
    -unity-text-align: middle-left;
}

.propertyrm:disabled
{
    opacity: 1;
}

.propertyrm .label
{
    color:#888;
    flex:0 0 auto;
    padding-left: 4px;
    padding-right:0;
}

.propertyrm .label.first
{
    padding-left:0;
}

.propertyrm .fieldContainer
{
    flex-direction:row;
    flex:1 0 auto;
    height: auto;
    margin-top: 0;
    margin-bottom: 0;
}

.propertyrm .fieldContainer:disabled {
    opacity: 1;
}

.propertyrm .unity-base-field:disabled {
    opacity: 1;
}

.propertyrm .unity-base-field:disabled Label {
    cursor: arrow;
}

.propertyrm .unity-base-field:disabled .unity-base-field__input {
    opacity: 0.5;
}

.propertyrm .unity-enum-field
{
    flex:1 1;
    flex-basis: auto;
    padding: 1px 0;
}

.propertyrm  .maincontainer
{
    flex:1 1 auto;
    flex-direction:column;
    align-items:stretch;
}

.propertyrm .colordisplay
{
    flex:1 0 auto;
}

ColorPropertyRM
{
    flex-direction:row;
    height: 36px;
}

ColorPropertyRM .colorcontainer, Vector3PropertyRM .colorcontainer
{
    flex:1 0 auto;
    border-left-width: 1px;
    border-right-width: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
    border-color:#000;
    margin-top: 2px;
    margin-bottom: 2px;
}

ColorPropertyRM > .maincontainer > .fieldContainer
{
    flex:0 0 auto;
}

Vector3PropertyRM VFXColorField
{
    flex: 0 0 auto;
    margin-left: 4px;
}

Vector3PropertyRM.propertyrm VFXVector3Field.fieldContainer
{
    height: 20px;
    flex: 1 0 auto;
}

VFXVector3Field .unity-float-field {
    margin-left: 4px;
}


ColorPropertyRM .unity-float-field
{
    flex: 1 0 auto;
}

IntPropertyRM .unity-integer-field
{
    align-self:flex-end;
    margin-left:0;
    margin-right:0;
    margin-top:0;
    margin-bottom:0;
    flex:1 0 auto;
}

UintPropertyRM .unity-long-field
{
    align-self:flex-end;
    margin-left:0;
    margin-right:0;
    margin-top:0;
    margin-bottom:0;
    flex:1 0 auto;
}

CurvePropertyRM .unity-curve-field {
    flex: 1 0 auto;
    min-height: 18px;
}

CurvePropertyRM .unity-curve-field__input {
    flex: 1 1 auto;
    height: 17px;
    background-color: #2a2a2a;
    border-color: #212121;
    border-top-color: #0d0d0d;
    border-width: 1px;
    border-radius: 3px;
    margin-top: 0;
    padding: 0;
}

CurvePropertyRM .unity-curve-field:focus .unity-curve-field__input {
    border-color: var(--unity-colors-input_field-border-focus);
}

CurvePropertyRM
{
    flex:1 0 auto;
    min-width: 100px;
}

StringPropertyRM VFXStringField .unity-text-field
{
    flex-grow: 1;
    flex-shrink: 1;
    margin: 0 0 0 1px;
}

Matrix4x4PropertyRM.propertyrm > .fieldContainer
{
    height: auto;
    flex-direction: row;
}
Matrix4x4PropertyRM.propertyrm VFXMatrix4x4Field
{
    height: 72px;
}


.propertyrm #indeterminate
{
    flex:1 1 auto;
    padding-left: 3px;
    padding-right: 3px;
    padding-top: 0;
    padding-bottom: 0;
    cursor: text;
    overflow: visible;
    -unity-overflow-clip-box: content-box;
    flex : 1 1 auto;
    -unity-text-align: middle-left;
    background-color: rgb(42,42,42);
    border-top-width: 1px;
    border-color: rgb(13,13,13);
    border-radius: 2px;
    color: rgb(180,180,180);
}
.propertyrm VFXColorField #indeterminate
{
    border-top-width: 0px;
    border-radius: 0;
}

.propertyrm .sliderField #indeterminate
{
    flex:0 0 auto;
}

.ListPropertyRM > .ReorderableList
{
    margin-top: 4px;
    margin-left: 0;
    margin-right: 4px;
    flex: 1 1 auto;
}

HLSLPropertyRM VFXTextEditorField Button.propertyrm-button.unity-button
{
    flex-grow: 1;
    margin-top: 0;
    height: 17px;
}

DropdownField {
    margin: 0;
    flex: 1 1;
}
ColorPropertyRM
{
    height: auto;
}

VFXColorField
{
    margin-right: 10px;
    margin-bottom: 4px;
}
VFXColorField .colorcontainer
{
    margin-right: 24px;
}

.propertyrm .unity-base-field > Label {
    min-width: initial;
    padding-top: 0;
    align-self: center;
    overflow: hidden;
    padding: 0;
    padding-left: 4px;
    color: #888888;
    white-space: pre;
}

#settings .propertyrm .unity-base-field > Label {
    margin-left: 14px;
    margin-right: 0;
}

#settings .propertyrm .fieldContainer > Label {
    margin-left: 14px;
}

.fieldContainer #FieldParent {
    flex-basis: 1px;
}

.fieldContainer > Label {
    align-self: center;
}

.propertyrm.hasDepth > .unity-base-field {
    margin-left: 4px;
}

.fieldContainer .unity-base-text-field__input {
    width: 36px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\PropertyRM.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\PropertyRMDark.uss---------------
.
.
.propertyrm .unity-toggle.indeterminate #unity-checkmark {
    background-image: resource("StyleSheets/Northstar/Images/d_toggle_mixed_bg.png");
}

.propertyrm .unity-toggle.indeterminate:hover:active #unity-checkmark {
    background-image: resource("StyleSheets/Northstar/Images/d_toggle_mixed_bg_hover.png");
}

.propertyrm .unity-toggle.indeterminate:focus #unity-checkmark {
    background-image: resource("StyleSheets/Northstar/Images/d_toggle_mixed_bg_focus.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\PropertyRMDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\PropertyRMLight.uss---------------
.
.
.propertyrm #indeterminate
{
    background-image: resource("Builtin Skins/LightSkin/Images/TextField.png");
}
.propertyrm .unity-toggle.indeterminate {
    background-image: resource("Builtin Skins/LightSkin/Images/toggle mixed.png");
}

.propertyrm .unity-toggle.indeterminate:hover:active {
    background-image: resource("Builtin Skins/LightSkin/Images/toggle mixed act.png");
}

.propertyrm .unity-toggle.indeterminate:focus {
    background-image: resource("Builtin Skins/LightSkin/Images/toggle mixed focus.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\PropertyRMLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\Selectable.uss---------------
.
.
.selectable > #selection-border
{
    position: absolute;
    left:0;
    right:0;
    top:0;
    bottom:0;
}

.selectable:hover  > #selection-border{
    border-color: rgba(68,192,255, 0.5);
    border-top-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
}

.selectable:selected  > #selection-border
{
    border-color: #44C0FF;
    border-left-width: 1px;
    border-right-width: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
}

.selectable:selected:hover  > #selection-border
{
    border-left-width: 2px;
    border-right-width: 2px;
    border-top-width: 2px;
    border-bottom-width: 2px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\Selectable.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXAnchoredProfiler.uss---------------
.
.
.hot {
    color: #ff2e2e
}

.medium {
    color: #e3a98b;
}

.cold {
    color: #00ff47;
}

.VFXAnchoredProfiler.graphElement {
    font-size: 18px;
    margin: 10px 0 0 0;
    padding-left:0;
    padding-right:0;
}
.VFXAnchoredProfiler > #node
{
    width: 280px;
    background-color:rgba(37,37,37,0.5);
    border-radius: 8px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
}

.VFXAnchoredProfiler #header > #unity-content
{
    font-size: 12px;
    margin: 0;
    padding: 4px 4px 4px 6px;
    background-color: rgba(0,0,0,0.5);
    -unity-font-style: normal;
}

#header {
    background-color: rgba(100, 100, 100, 0.5);
    border-bottom-width: 1px;
    border-color: #212121;
    border-top-right-radius: 4px;
    border-top-left-radius: 4px;
    font-size: 16px;
    -unity-font-style: bold;
}

#header > Toggle {
    margin: 8px;
}

#header > Toggle #unity-checkmark {
    -unity-slice-left: 0;
    -unity-slice-top: 0;
    background-image : url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_debug.png");
}

.VFXAnchoredProfiler.collapsed #header > Toggle #unity-checkmark {
    width:18px;
    height:18px;
    margin:0;
}

#header > Toggle .unity-toggle__input > Label {
    display: none;
}

#header > Toggle .unity-toggle__input:checked > Label {
    display: flex;
}

#header > Toggle .unity-toggle__input:checked #unity-checkmark {
    background-image : resource("GraphView/Nodes/NodeChevronDown@2x.png");
}

#header > Toggle .unity-toggle__input {
    margin-right: 42px;
}
.VFXAnchoredProfiler #title #icon
{
    --unity-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Execution.png");
    margin-right: 6px;
}

.VFXAnchoredProfiler.graphElement #user-label
{
    border-top-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    margin-left: 12px;
    margin-right: 12px;
    margin-top: 1px;
    margin-bottom: 1px;
    padding-left:0;
    padding-right:0;
    color: #c4c4c4;
    min-height: 24px;
    white-space:normal;
}

.VFXAnchoredProfiler.graphElement #user-label:hover
{
    border-color: rgba(68,192,255, 0.5);
}

.VFXAnchoredProfiler ColorPropertyRM .fieldContainer > *
{
    width: 56px;
}

#lock-button {
    position: absolute;
    right: 0;
    top: 5px;
    width: 28px;
    height: 28px;
    flex-direction: column;
    justify-content: center;
    flex-grow: 0;
    opacity: 0.5;
    background-size: 16px 16px;
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/lock_unlocked.png");
}

#lock-button:hover {
    opacity: 1;
}

.VFXAnchoredProfiler.locked #lock-button
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/lock_locked.png");
}

/* Collapsed state */
.VFXAnchoredProfiler.collapsed > #node {
    width: auto;
}

.VFXAnchoredProfiler.collapsed #lock-button {
    display: none;
    background-size: 22px 22px;
    right: 6px;
    top: 6px;
    opacity:1;
}
.VFXAnchoredProfiler.collapsed.locked #lock-button {
    display: flex;
}
.VFXAnchoredProfiler.collapsed #header {
    border-radius: 4px;
}

.VFXAnchoredProfiler.collapsed.locked #header > Toggle > VisualElement {
    margin: 4px 32px 4px 4px;
}

.VFXAnchoredProfiler.collapsed #header > Toggle > VisualElement {
    margin: 4px 4px 4px 4px;
}
/*----------------*/

/* Hidden state */
.VFXAnchoredProfiler.hidden > #collapsed-node #node
{
    display: none;
}
/*----------------*/

.VFXAnchoredProfiler > #collapsed-node
{
    display: none;
}

VFXSystemProfilerUI.collapsed > #collapsed-node > #lock-icon
{
    display: none;
}

VFXSystemProfilerUI.collapsed > #collapsed-node
{
    width: 60px;
    height: 50px;
    display: flex;
    background-color: rgba(63,63,63,1.0);
    border-radius: 10px;
    flex-direction: row;
    padding: 5px;
    margin-top: 15px;
}

.VFXSystemProfiler > #node > #lock-button
{
    visibility: hidden;
}

.texture-slot {
    margin-left: 12px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXAnchoredProfiler.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXAttachPanel.uss---------------
.
.
#AttachButton
{
    margin-top: 4px;
    height: 18px;
}

#PickTitle
{
    -unity-text-align: middle-left;
    margin-left: 4px;
}

#PickLabel
{
    margin-right: 0;
}

#PickIcon
{
    width: 12px;
    height: 12px;
    background-image: var(--unity-icons-picker);
}

#PickButton
{
    margin: 0px 1px 0px -21px;
    padding: 0;
    border-width: 0;
    border-bottom-left-radius: 0px;
    border-top-left-radius: 0px;
    width:16px;
    height:15px;
    justify-content: center;
    align-items: center;
    align-self: center;
    background-color: var(--unity-colors-object_field_button-background);
}

#PickLabel
{
    width: 152px;
}

#PickLabel > TextInput
{
    margin-right: 4px;
    padding: 0 22px 0 18px;
}

#VFXIcon
{
    margin-left: 2px;
    position: absolute;
    align-self: center;
    width: 12px;
    height: 12px;
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/vfx_graph_icon_gray_dark.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXAttachPanel.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXBlackboard.uss---------------
.
.
VFXBlackboard.blackboard
{
    background-color: #292929;
    min-height: 200px;
    min-width: 250px;
}

/* This margin is to leave some space around the blackboard for the resizer element to grab the mouse */
VFXBlackboard.blackboard .mainContainer {
    margin: 4px;
}

VFXBlackboard.blackboard > .mainContainer > #content > #header {
    flex-direction: column;
    flex-grow: 1;
    flex-shrink: 0;
}

VFXBlackboard.blackboard > .mainContainer > #content > #header > #addButton {
    margin: 0 3px;
    background-color: rgba(0,0,0,0);
    border-color: rgba(0,0,0,0);
    background-image: none;
    font-size: 20px;
    align-self: flex-start;
    padding: 0 6px 3px 6px;
}

VFXBlackboard #vfx-properties
{
    margin-bottom: 7px;
}

/* Fixing some propertyrm that span outside of the blackboard */
VFXBlackboard .blackboardRow .unity-base-field
{
    align-self: stretch;
}

VFXBlackboard .blackboardRow Matrix4x4PropertyRM #fieldContainerParent
{
    align-self: stretch;
}


/* Tabs styles */
VFXBlackboard #tabsContainer
{
    background-color: #3c3c3c;
    flex-direction: row;
    margin-bottom: 4px;
}

VFXBlackboard #tabsContainer > #bottomBorder {
    border-color: black;
    border-bottom-width: 1px;
    position: absolute;
    height: 100%;
    width: 100%;
}

VFXBlackboard #tabsContainer > Toggle #unity-checkmark
{
    display: none;
}
VFXBlackboard #tabsContainer > Toggle
{
    margin: 0;
    width: 80px;
    height: 28px;
    margin-bottom: 0px;
    border-radius: 0;
    border-width: 0 0 1px 0;
    border-color: rgba(0, 0, 0, 0);
}

VFXBlackboard #tabsContainer > Toggle:hover
{
    border-color: #777777;
}

VFXBlackboard #tabsContainer > Toggle:checked
{
    border-color: #2C5D87;
}
/*-- End tabs styles --*/

VFXBlackboard #labelContainer TextField.unity-base-field {
    margin: 1px 4px 2px 4px;
}

VFXBlackboard .blackboardField #textField
{
    height: 23px;
    margin: 4px 0 4px 3px;
    flex-grow: 1;
}

VFXBlackboard .blackboardRow
{
    margin: 0;
    padding: 0 4px 0 30px;
}

VFXBlackboard .blackboardRowContainer:disabled
{
    opacity: 0.7;
}

VFXBlackboardCategory.graphElement
{
    margin: 0;
    border-radius: 0;
    border-top-width: 1px;
    border-top-color: #1a1a1a;
    padding: 0;
}

.sub-category VFXBlackboardCategory #icon {
    flex-shrink: 0;
    width: 16px;
    height: 16px;
    margin: 0 4px;
    background-image:resource("Icons/PackageManager/Dark/Folder.png");
}

VFXBlackboardCategory #header
{
    align-items: center;
    flex-direction: row;
    height: 30px;
    margin-left: 20px;
}

.sub-category VFXBlackboardCategory #header
{
    margin-left: 28px;
}

VFXBlackboardCategory #title
{
    height: 18px;
    margin-top: 1px;
    margin-bottom: 1px;
    padding: 0;
    flex-grow: 1;
    -unity-text-align: middle-left;
}

VFXBlackboardCategory #titleEdit
{
    height: 24px;
    margin-top: 1px;
    margin-bottom: 1px;
    padding: 0;
    flex-grow: 1;
    -unity-text-align: middle-left;
}

VFXBlackboard TreeView
{
    background-color: rgba(0, 0, 0, 0);
}

VFXBlackboard .no-category .blackboardRow
{
    /*margin: 1px 0 1px 0;*/
    padding: 0 4px 0 10px;
}

/* Center row vertically */
VFXBloackboard .blackboardRow, VFXBlackboard .blackboardRow .mainContainer, VFXBlackboard .blackboardRow #root, .blackboardRowContainer
{
    flex-grow: 1;
}

VFXBlackboard .blackboardRow #root
{
    justify-content: center;
}
/*-- End center vertically --*/

VFXBlackboard #SpecialIcon {
    position: absolute;
    right: 4px;
    top: 10px;
    width: 16px;
    height: 16px;
}

/*-- Put a lock icon on the right for built-in items --*/
VFXBlackboard .built-in #SpecialIcon
{
    background-image: var(--unity-icons-lock-checked);
}

/*-- Put a link icon on the right for imported items --*/
VFXBlackboard .sub-graph #SpecialIcon
{
    background-image: var(--unity-icons-link-checked);
}

VFXBlackboard #unity-tree-view__item-indent
{
    display: none;
}

VFXBlackboard #unity-tree-view__item-toggle
{
    left: 8px;
    margin: 8px 0;
    position: absolute;
}

VFXBlackboard .sub-category #unity-tree-view__item-toggle
{
    left: 16px;
}

VFXBlackboard .category .blackboardRowContainer
{
    background-color: #474747;
}

VFXBlackboard .collapsed.category .blackboardRowContainer
{
    /*margin-bottom: 4px;*/
}

VFXBlackboard .sub-category .blackboardRowContainer
{
    background-color: #323232;
}

VFXBlackboard .last.collapsed, .last.item
{
   /* margin-bottom: 8px;*/
}

/* Pill UI */
VFXBlackboard .blackboardRow .pill
{
    margin: 2px 4px;
}

VFXBlackboard .blackboardRow VFXBlackboardField .pill #node-border
{
    border-radius: 12px;
}

VFXBlackboard .blackboardRow .pill #selection-border
{
    display: none;
    background-image: none;
    margin: 0;
    border-width: 0;
}

VFXBlackboard .blackboardRow .pill #node-border
{
    background-color: #414141;
    background-image: none;
    border-width: 1px;
    margin: 0;
    padding: 1px 4px;
}

VFXBlackboard .blackboardRow.hovered .pill #node-border
{
    background-color: #955E27;
    border-color: #FF8000;
}

VFXBlackboard VFXBlackboardField.unused .pill.has-icon #contents > #top > #icon
{
    opacity:0.2;
}

VFXBlackboard VFXBlackboardField .pill #contents > #top > #icon,
VFXBlackboard VFXBlackboardField .pill.has-icon #contents > #top > #icon
{
    width: 7px;
    height: 7px;
    margin: 8px 4px;
}

VFXBlackboard VFXBlackboardField .pill #node-border
{
    border-radius: 12px;
    border-color: #242424;
}

VFXBlackboard .expanded VFXBlackboardField .pill #node-border
{
    border-radius: 12px 12px 0 0;
}
/*-- End of Pill UI --*/

VFXBlackboard .unity-collection-view__item--selected .blackboardRow,
VFXBlackboard .unity-collection-view__item--selected VFXBlackboardCategory
{
    border-radius: 0;
    background-color: #2c5d87;
}

/* BlackboardField UI */
VFXBlackboard .blackboardField #typeLabel
{
    color: #C1C1C1;
    flex-grow: 1;
    -unity-text-align: middle-right;
}

VFXBlackboard VFXBlackboardPropertyView
{
    padding: 4px;
    margin-top: -3px;
    margin-left: 16px;
    border-radius: 0 12px 12px 12px;
    border-width: 1px;
    border-color: #242424;
    background-color: #414141;
}

VFXBlackboard VFXBlackboardPropertyView .propertyrm
{
    padding: 2px 0;
    margin: 0 4px 0 4px;
}

VFXBlackboard VFXBlackboardPropertyView .propertyrm.hasDepth {
    border-width: 0;
}
/*-- End of BlackboardField UI --*/

/* VFX Attribute UI */
VFXBlackboard .separator Toggle {
    display: none;
}

VFXBlackboard .separator:checked {
    background-color: transparent;
}

VFXBlackboard .separator .blackboardRowContainer {
    color: var(--unity-colors-app_toolbar_button-background-checked);
    border-color: var(--unity-colors-app_toolbar_button-background-checked);
    border-bottom-width: 1px;
}

VFXBlackboard VFXBlackboardAttributeRow.blackboardRow.expanded .pill #node-border
{
    border-radius: 5px 5px 0 0;
}

VFXBlackboardAttributeField .pill.has-icon #contents > #top > #icon {
    flex-shrink: 0;
    width: 22px;
    height: 22px;
    margin: 0px 2px 1px 0px;
}

VFXBlackboard VFXBlackboardAttributeRow .pill #node-border
{
    border-radius: 5px;
}

VFXBlackboardAttributeView
{
    background-color: #414141;
    border-color: #242424;
    border-width: 1px;
    border-radius: 0 5px 5px 5px;
    padding-left: 16px;
    padding-top: 4px;
    padding-bottom: 4px;
    margin-top: -3px;
    margin-left: 16px;
    margin-bottom: 4px;
}

/* Disabled type enum field  (for built-in attributes) */
VFXBlackboardAttributeField #type:disabled > VisualElement
{
    border-width: 0;
    background-color: rgba(0, 0, 0, 0);
}

VFXBlackboardAttributeField #type:disabled TextElement
{
    -unity-text-align: middle-right;
}

VFXBlackboardAttributeField #type:disabled .unity-enum-field__arrow
{
    display: none;
}
/*-- End of disable type enum field --*/

VFXBlackboardAttributeRow #description,
VFXBlackboardAttributeRow #UsedBySubgraph
{
    white-space: normal;
    margin-top: 4px;
    min-height: 32px;
    flex-wrap: wrap;
    flex-direction: column;
}

VFXBlackboardAttributeRow #description TextElement,
VFXBlackboardAttributeRow #UsedBySubgraph TextElement {
    flex-shrink: 1;
}

VFXBlackboardAttributeRow #readonly Label,
VFXBlackboardAttributeRow #description Label,
VFXBlackboardAttributeRow #UsedBySubgraph Label,
VFXBlackboardAttributeRow #type Label,
VFXBlackboardAttributeRow #typeLabel Label {
    margin-left: 0;
    -unity-font-style: bold;
}

VFXBlackboardAttributeRow #description TextInput,
VFXBlackboardAttributeRow #UsedBySubgraph TextInput{
    align-self: stretch;
}

VFXBlackboardAttributeRow #readonly TextInput,
VFXBlackboardAttributeRow #typeLabel.read-only TextInput,
VFXBlackboardAttributeRow #description.read-only TextInput,
VFXBlackboardAttributeRow #UsedBySubgraph.read-only TextInput {
    background-color: rgba(0, 0, 0, 0);
    border-width: 0;
    -unity-font-style: italic;
}

VFXBlackboardAttributeRow #readonly TextElement,
VFXBlackboardAttributeRow #typeLabel.read-only TextElement {
    -unity-text-align: middle-right;
}
/*-- End of VFX Attribute UI --*/
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXBlackboard.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXBlock.uss---------------
.
.

#block-container .VFXNodeUI.node {
    flex:1 0 auto;
    align-items:stretch;
    border-width:1px;
    border-color:rgba(0,0,0,0);
    padding-top: 1px;
    padding-left:0;
    padding-right:0;
    padding-bottom: 0px;
    margin-left:0;
    margin-right:0;
    overflow:visible;
}
#block-container .VFXNodeUI.node.first
{
    padding-top:0;
}
#block-container .VFXNodeUI.node.first > #selection-border
{
    margin-top:0;
}

#block-container .VFXNodeUI.node > #selection-border
{
    margin-left:0;
    margin-right:0;
    margin-top: 2px;
    margin-bottom: 1px;
}

#block-container .VFXNodeUI.node #contents > #top > #input
{
    padding-bottom: 8px;
    flex: 1 1 auto;
}

VFXContextUI .VFXNodeUI.node #node-border #title
{
    flex-direction:row;
    align-items:center;
    justify-content:flex-start;
}

#block-container .VFXNodeUI.node #node-border
{
    border-radius:8px;
}

#block-container .VFXNodeUI.node.invalid #node-border
{
    border-color: #FF0000;
}

VFXContextUI #block-container .node.block-disabled #node-border .port #label
{
    opacity: 0.5;
}

VFXContextUI .VFXNodeUI.node.block-disabled #node-border #settings #label
{
    opacity: 0.5;
}

VFXContextUI .VFXNodeUI.node.block-disabled #node-border #title #title-label
{
    opacity: 0.5;
}

VFXContextUI .VFXNodeUI.node.block-disabled #node-border #title, VFXContextUI #block-container .node.block-disabled #node-border #contents
{
    background-color: rgba(41,41,41,0.8);
}

.VFXNodeUI.node
{
    margin-left:0;
    margin-right:0;
    margin-top:0;
    margin-bottom:0;
    flex-direction:row;
}

VFXBlockUI.node.cannot-expand.nosettings #collapse-button {
    display: none;
}

.VFXNodeUI.node > #node-border
{
    flex:1 1 auto;
    margin-left:0;
    margin-right:0;
    margin-top:0;
    margin-bottom:0;
}

VFXEditableDataAnchor.activationslot > BoolPropertyRM > Label
{
    display: none;
}

#block-container .activationslot.collapsed.VFXNodeUI.node #contents > #top > #input
{
    padding-top: 0px;
}

#block-container .activationslot.collapsed.VFXNodeUI.node #contents > #divider
{
    display: none;
}

#block-container .nosettings.VFXNodeUI.node #contents > #settings-divider
{
    display: none;
}

#block-container .collapsed.VFXNodeUI.node #contents > #top > #input > VFXEditableDataAnchor.activationslot
{
    margin-top: 1px;
}

VFXBlockUI BoolPropertyRM Label {
    margin-right: 3px;
}

#settings .propertyrm {
    height: 18px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXBlock.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXComponentBoard-bounds-list.uss---------------
.
.
#container
{
    flex-direction:row;
    align-items:center;
}

#system-field
{
    width: 40%;
    -unity-text-align: middle-center;
    white-space: normal;
    margin-top: 0px;
    margin-bottom: 0px;
    padding-left:4px;
    padding-bottom:0px;

}
#system-button
{
    -unity-text-align: middle-center;
    white-space: normal;
    margin-top: 0px;
    margin-bottom: 0px;
    padding-left:4px;
}

#divider {
    background-color: rgba(0,0,0,0);
    border-color: rgba(0,0,0,0);
    flex-grow:1;
}

#divider.horizontal {
    height: 0.05px;
    border-bottom-width: 1px;
}

EnumField {
    width: 94px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXComponentBoard-bounds-list.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXComponentBoard.uss---------------
.
.
VFXComponentBoard.graphElement
{
    position:Absolute;
    padding: 0;
    margin: 0;
    min-width: 286px;
    min-height: 100px;
}

VFXComponentBoard.graphElement > .mainContainer
{
    margin: 4px;
    flex: 1 1 auto;
    background-color: #2b2b2b;
    border-color: #191919;
}

#header {
    flex-shrink: 0;
    flex-direction: row;
    align-items: stretch;
    background-color: #393939;
    border-bottom-width: 1px;
    border-color: #212121;
    border-top-right-radius: 4px;
    border-top-left-radius: 4px;
    padding: 8px;
}

#header > #labelContainer > #titleLabel {
    font-size : 14px;
    color: #c1c1c1;
}

#header > #labelContainer #subTitleLabel {
    font-size: 12px;
    color: #808080;
    flex-grow: 1;
}

#labelContainer {
    flex-grow: 1;
}

#subtitle {
    flex-grow: 1;
    flex-direction: row;
}

#subTitle-icon
{
    margin-right: 4px;
}

#toolbar
{
    margin-top: 8px;
    flex-direction:row;
}

#toolbar .unity-button
{
    flex:1 0 auto;
    height: 24px;
    align-items:center;
    background-size: 16px 16px;
}

#toolbar .unity-button#stop
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Stop.png");
}
#toolbar .unity-button#play
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Pause.png");
}
#toolbar .unity-button#play.paused
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Play.png");
}
#toolbar .unity-button#step
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Step.png");
}
#toolbar .unity-button#restart
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Restart.png");
}

#play-rate-menu .unity-base-popup-field__text {
    -unity-text-align: middle-center;
}

#play-rate-container {
    flex-direction:row;
    align-items: center;
    padding-bottom: 8px;
}

.component-container {
    margin: 0;
    padding: 8px 8px;
    border-bottom-width: 1px;
    border-color: rgba(35,35,35,0.8);
}

.empty {
    border-width: 0;
}

#debug-box
{
    flex-grow: 1;
    background-color:#222;
    margin: auto;
    width: 90%;
    height: 150px;
    overflow:hidden;
    border-radius:6px;
}

#debug-settings-container
{
    flex:1 1 auto;
    flex-direction:row;
}

#debug-box-axis-container
{
    flex-direction:column;
    flex-grow: 1;
    width: 10%;
}

#debug-plot-area {
    flex-direction: row;
    width: 100%;
    margin-top: 7px;
    margin-bottom: 12px;
    padding-left:4px;
}

#debug-box-axis-100 {
    flex-grow: 1;
    -unity-text-align: upper-center;
}
#debug-box-axis-50 {
    flex-grow: 1;
    -unity-text-align: middle-center;
}
#debug-box-axis-0 {
    flex-grow: 1;
    -unity-text-align: lower-center;
}

#debug-system-stat-container
{
    margin-top: 4px;
}

#debug-system-stat-title, #debug-system-stat-title-name {
    font-size: 12px;
    flex-grow: 1;
    width: 20%;
    -unity-text-align: middle-center;
}

#debug-system-stat-entry-container
{
    flex-direction:row;
    align-items:center;
    flex-grow: 1;
    width:100%;
}

#debug-system-stat-entry-container > * {
    width: 25%;
}

#debug-system-stat-entry-container > Toggle {
    width: auto;
}

#play-rate-container #play-rate-slider
{
    flex:1 1 auto;
}

#play-rate-container #play-rate-field
{
    width: 40px;
}

.row {
    padding: 3px 0;
}

#bounds-actions-container {
    flex-direction:row;
    align-items: center;
}

#bounds-actions-container #bounds-label {
    flex-grow: 1;
}

#bounds-actions-container #record {
    height: 20px;
    width: 30px;
    background-size: 16px 16px;
}

#bounds-tool-container.is-recording {
    background-color: #833232;
}

#bounds-actions-container #record.show-recording {
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_Record.png");
}

VFXComponentBoardEventUI {
    flex-direction:row;
    align-items:center;
}

VFXComponentBoardEventUI #event-name {
    flex: 1;
}

#system-field {
    flex-grow: 1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXComponentBoard.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXContext.uss---------------
.
.
VFXContextUI.graphElement {
    font-size: 18px;
    flex-direction: column;
    margin-top:0;
    padding-left:0;
    padding-right:0;
    margin-left:0;
    margin-right:0;

}
VFXContextUI > #node-border
{
    width: 416px;
    background-color:rgba(29,29,29,0.8);
    border-radius: 8px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
    padding-top : 5px;
    padding-bottom : 5px;
    margin-top : 19px;
    margin-bottom : 19px;
    margin-left: 3px;
    margin-right: 3px;
}

VFXContextUI > #node-border.empty > #inside > #contents
{
    margin-top : 0;
    padding-top : 0;
    margin-bottom : 0;
    padding-bottom : 0;
}

/*  update */

VFXContextUI.outputTypenone > #node-border
{
    --end-color:#C24F30;
}


VFXContextUI.inputTypeparticle > #node-border
{
    --start-color:#BF9220;
}
VFXContextUI.outputTypeparticle > #node-border
{
    --end-color:#BF9220;
}

VFXFlowAnchor.port.EdgeConnector.typeparticle
{
    --port-color: #FFBC21;
}

VFXContextUI.inputTypeparticlestrip > #node-border
{
    --start-color:#BF8050;
}
VFXContextUI.outputTypeparticlestrip > #node-border
{
    --end-color:#BF8050;
}

VFXFlowAnchor.port.EdgeConnector.typeparticlestrip
{
    --port-color: #FFAA6B;
}

/*  output */


VFXContextUI.typemesh  > #node-border
{
    --start-color: #3b5e92;
    --end-color: #3b5e92;
}

VFXFlowAnchor.port.EdgeConnector.typemesh
{
    --port-color: #0763eb;
}
VFXContextUI.inputTypenone.outputTypenone > #node-border
{
    --start-color: #777;
    --end-color: #777;
}
/* spawner */

VFXContextUI.inputTypespawnevent  > #node-border,VFXContextUI.inputTypeevent  > #node-border
{
    --start-color: #17AB6A;
}
VFXContextUI.outputTypespawnevent  > #node-border,VFXContextUI.outputTypeevent  > #node-border
{
    --end-color: #17AB6A;
}

VFXContextUI.outputTypeoutputevent  > #node-border,VFXContextUI.outputTypeevent  > #node-border
{
    --end-color: #9ee556;
}

VFXFlowAnchor.port.EdgeConnector.typespawnevent, VFXFlowAnchor.port.EdgeConnector.typeevent
{
    --port-color: #00ff8f;
}

/* event */

VFXContextUI > #selection-border
{
    border-radius: 10px;
    margin-top: 18px;
    margin-bottom: 18px;
}
VFXContextUI #inside
{
    margin-left: 8px;
    margin-right: 8px;
}

VFXContextUI .extremity
{
    border-radius: 2px;
    justify-content:center;
    align-items:center;
    flex-direction:row;
}

VFXContextUI #HeaderContainer
{
    flex-direction:row;
    justify-content:center;
    align-items:center;
}

VFXContextUI #footer
{
    align-self: center;
    height: 16px;
    font-size: 11px;
    flex-direction:row;
    margin-bottom: 1px;
    padding-left: 18px;
    -unity-font-style: normal;
    --unity-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Execution.png");
    background-size: 16px 16px;
    background-position-x: left;
}

VFXContextUI #node-border > #inside > #title > #title-label
{
    -unity-text-align:middle-center;

    color:#c4c4c4;
    font-size: 18px;
    height: 24px;
}

VFXContextUI #title #icon
{
    --unity-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Execution.png");
    margin-right: 6px;
}

VFXContextUI #title #icon.Empty
{
    display: none;
}

VFXContextUI #header-space
{
    position:absolute;
    font-size: 11px;
    -unity-text-align: middle-center;
    border-width: 0.5px;
    border-radius: 4px;
    border-color: #3F3F3F;
    padding: 0 4px 0 20px;
    right: 0;
    top: 4px;
    width: 54px;
    height: 18px;
    background-size: 14px 14px;
    background-position-x: 4px;
}

VFXContextUI #header-space.World {
    background-image : url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_WorldSpace@2x.png");
}

VFXContextUI #header-space.Local {
    background-image : url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_LocalSpace@2x.png");
}

VFXContextUI #header-space.None {
    display: none;
}

VFXContextUI #header-space:hover {
    border-color: var(--unity-colors-default-text);
    background-color: #3F3F3F;
}

VFXContextUI > #node-border > #inside > #contents
{
    margin-top : 0;
    padding-top : 4px;
    border-radius: 6px;
    background-color: rgba(63,63,63,0.8);
    margin-left: 1px;
    margin-right: 1px;
    font-size: 11px;
}
VFXContextUI > #node-border > #inside > #contents > #settings
{
    background-color: rgba(0,0,0,0);
}

VFXContextUI #block-container {
    margin-top: 0;
    margin-bottom: 0;
    padding-top: 2px;
    padding-bottom: 2px;
}

.flow-container
{
    flex-direction: row;
    justify-content: center;
    padding-left: 0;
    padding-right: 0;
    position: Absolute;
    left: 0;
    right: 0;
    height: 28px;
}

#flow-outputs
{
    bottom: 0;
}
VFXContextUI #no-blocks
{
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-top-width: 1px;
    border-color: #393939;
    color: #393939;
    height: 24px;
    -unity-text-align: middle-center;
    border-radius: 8px;
}

VFXContextUI.graphElement #user-label
{
    border-top-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    margin-left: 12px;
    margin-right: 12px;
    margin-top: 1px;
    margin-bottom: 1px;
    padding-left:0;
    padding-right:0;
    color: #c4c4c4;
    min-height: 24px;
    white-space:normal;
}

#user-title
{
    flex-direction:column;
    align-items:stretch;
    min-height: 27px;
}

VFXContextUI.graphElement #user-label:hover
{
    border-color: rgba(68,192,255, 0.5);
}

TextField#user-title-textfield
{
    padding-left:0;
    padding-right:0;
    padding-top:0;
    padding-bottom:0;
    margin-left: 11px;
    margin-right:11px;
    margin-top: 1px;
    margin-bottom:1px;
    border-width: 1px;
    border-color: rgba(68,192,255, 0.5);
    background-color:rgba(0,0,0,0);
    min-height: 24px;
    white-space: normal;
    overflow: visible;
    height: auto;
    flex-direction: row;
}

TextField#user-title-textfield #unity-text-input
{
    padding: 0;
    margin: 0;
    border-width: 0 ;
    background-color:rgba(29,29,29,0.8);
    background-image:none;
    font-size: 18px;
    white-space: normal;
}

#user-label
{
    overflow: hidden;
    max-height: 120px;
}

VFXContextUI.inputTypespawnevent #user-label
{
    color: #00ff8f;
}
VFXContextUI.inputTypeparticle #user-label
{
    color: #FFBC21;
}
VFXContextUI.inputTypeparticlestrip #user-label
{
    color: #FFAA6B;
}

VFXContextUI.inputTypemesh #user-label
{
    color: #3b5e92;
}

#user-label.empty
{
    height : 4px;
    min-height:0;
}

VFXContextUI > #node-border > #inside > #contents
{
    background-color: rgba(45,45,45,0.8);
}

VFXContextUI .subtitle
{
    align-self:center;
    font-size:16px;
    color: #8F8F8F;
}

VFXContextUI .subtitle.empty
{
    display:None;
}

#title.extremity #spacer {
    display: none;
}

#settings {
    border-bottom-color: #242424;
    border-bottom-width: 1px;
}

VFXContextUI.nosettings #top {
    border-top-width: 0;
}

#contents #settings {
    border-top-width: 0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXContext.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXControls.uss---------------
.
.
/*FloatField {
    min-height: 15px;
    margin-left: 4px;
    margin-top: 2px;
    margin-right: 4px;
    margin-bottom: 2px;
    padding-left: 3px;
    padding-top: 1px;
    padding-right: 3px;
    padding-bottom: 2px;
    -unity-slice-left: 3;
    -unity-slice-top: 3;
    -unity-slice-right: 3;
    -unity-slice-bottom: 3;
    --unity-selection-color: rgba(61,128,223,166);
    cursor: text;
}*/

VFXColorField
{
    flex:1 0 auto;
    flex-direction:row;
    margin-left:0;
    margin-right:0;
}

VFXColorField #indeterminate
{
    color:#000;
}

.sliderField
{
    flex-direction:row;
}
.sliderField .unity-slider
{
    flex:1 1 auto;
    margin-right: 4px;
}
.sliderField #Field
{
    min-width: 40px;
    flex:1 0 auto;
    overflow: visible;
}
.sliderField #Field > .unity-base-field__input {
    min-width: 40px;
    flex:0 1 auto;
}

.sliderField #indeterminate
{
    width: 40px;
    flex:0 0 auto;
}

.sliderMinMaxField
{
    flex-direction:row;
}

.sliderMinMaxField .unity-min-max-slider
{
    flex:1 0 auto;
    overflow: visible;
}
.sliderMinMaxField #indeterminate
{
    width: 40px;
    flex:0 0 auto;
}

.PopupButton
{
    -unity-text-align:middle-left;
    height: 16px;
    -unity-slice-left: 6;
    -unity-slice-top: 4;
    -unity-slice-right: 14;
    -unity-slice-bottom: 4;
    color: #B4B4B4;
    padding-right: 14px;
    padding-left: 4px;
}

VFXEnumField .PopupButton
{
    padding-bottom: 4px;
}

VFXEnumField .PopupButton:hover
{
    color: #FFF;
}

VFXStringFieldPushButton
{
    height: 16px;
}

VFXStringFieldPushButton TextField
{
    width: 140px;
}

VFXStringFieldPushButton .unity-button
{
    flex-grow: 1;
    padding: 0 16px;
    margin: 0 3px;
}

VFXStringFieldPushButton > Label {
    min-width: 148px;
}

.unity-button.MiniDropDown
{
    -unity-slice-right: 12;
}

VFX32BitField {
    margin-left: 2px;
}

VFX32BitField.fieldContainer #bit-button {
    align-self: center;
    flex-grow: 1;
    height: 16px;
    margin-right: 1px;
    background-color: #514d4d;
}

VFX32BitField.fieldContainer #bit-button.first {
    border-radius: 2px 0 0 2px;
}

VFX32BitField.fieldContainer #bit-button.last {
    border-radius: 0 2px 2px 0;
}

VFX32BitField.fieldContainer #bit-button.bit-set {
    background-color: var(--unity-colors-highlight-background);
}

VFX32BitField.fieldContainer #bit-button:hover {
    background-color: var(--unity-colors-highlight-background-hover);
}

VFX32BitField > #tip
{
    position: absolute;
    left: 50%;
    right: 50%;
    -unity-text-align: middle-center;
    text-shadow: 1px 1px 2px #252525;
}

GradientField
{
    flex: 1 1 auto;
    flex-direction: row;
    margin-left:0;
    margin-right:0;
    border-width: 0;
}

GradientField * {
    border-width: 0;
    margin: 0;
}

GradientField .unity-gradient-field__input {
    border-width: 1px;
    border-radius: 3px;
    border-color: #212121;
    border-top-color: #0d0d0d;
}

GradientField:focus .unity-gradient-field__input {
    border-color: var(--unity-colors-input_field-border-focus);
}

GradientField .unity-gradient-field__background {
    margin: -1px;
}

ObjectField
{
    flex-grow: 1;
    flex-shrink: 1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXControls.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXControlsDark.uss---------------
.
.
.PopupButton
{
    background-image: resource("Builtin Skins/DarkSkin/Images/mini popup.png");
}
.PopupButton:hover:active
{
    background-image: resource("Builtin Skins/DarkSkin/Images/mini popup act.png");
}

.PopupButton:hover
{
    background-image: resource("Builtin Skins/DarkSkin/Images/mini popup focus.png");
}

.unity-button.MiniDropDown {
    background-image: resource("Builtin Skins/DarkSkin/Images/mini pulldown.png");
}

.unity-button.MiniDropDown:hover {
    background-image: resource("Builtin Skins/DarkSkin/Images/mini pulldown focus.png");
}

.unity-button.MiniDropDown:hover:active {
    background-image: resource("Builtin Skins/DarkSkin/Images/mini pulldown act.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXControlsDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXControlsLight.uss---------------
.
.
.PopupButton
{
    background-image: resource("Builtin Skins/LightSkin/Images/mini popup.png");
}
.PopupButton:hover:active
{
    background-image: resource("Builtin Skins/LightSkin/Images/mini popup act.png");
}

.PopupButton:focus
{
    background-image: resource("Builtin Skins/LightSkin/Images/mini popup focus.png");
}

.unity-button.MiniDropDown {
    background-image: resource("Builtin Skins/LightSkin/Images/mini pulldown.png");
}

.unity-button.MiniDropDown:hover {
    background-image: resource("Builtin Skins/LightSkin/Images/mini pulldown focus.png");
}

.unity-button.MiniDropDown:hover:active {
    background-image: resource("Builtin Skins/LightSkin/Images/mini pulldown act.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXControlsLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXDataAnchor.uss---------------
.
.
.VFXDataAnchor
{
    flex-direction: row;
    align-items: center;
    -unity-font-style: normal;
    color: #BBB;
    font-size: 11px;
    padding-bottom:0;
    height: auto;
    margin-bottom:0;
    margin-right: 4px;
}

.VFXDataAnchor #lineSpacer
{
    flex:1 0 auto;
}
.VFXDataAnchor #line
{
    align-self:stretch;
}


.VFXDataAnchor.port #type
{
    margin-left: 0;
    margin-right:0;
    padding-bottom:0;
    padding-left: 8px;
    margin-bottom:0;
}


.VFXDataAnchor.port #connector
{
    margin-right: 8px;
}

VFXContextUI #output .VFXDataAnchor.port #connector
{
    width: 8px;
    flex-shrink: 0;
    margin-right: 4px;
}


.superCollapsed .VFXDataAnchor.port #connector
{
    margin-left:0;
    position: relative;
    right: auto;
}


.VFXDataAnchor.hidden
{
    max-height:0;
    min-height:0;
    height:0;
}

.VFXDataAnchor.hidden #connector
{
    margin-top: -12px;
    border-left-width:0;
    border-right-width:0;
    border-top-width:0;
    border-bottom-width:0;
    background-color:rgba(0,0,0,0);
}

.VFXDataAnchor.hidden .propertyrm
{
    display:none;
}

.node #right > #output
{
    right: 0;
}

.VFXDataAnchor.Output
{
    flex-shrink: 0;
    padding-right: 0;
    margin-left: 0;
    flex-direction: row;
}

.VFXDataAnchor.Output #type {
    margin-right: 20px;
}

.VFXDataAnchor.Output #line {
    margin-left: 6px;
}

.VFXDataAnchor.Output #connector {
    position: absolute;
    right: -4px;
}

.activationslot.VFXDataAnchor .propertyrm {
    margin-left: 10px;
}
.activationslot.VFXDataAnchor .propertyrm > #icon {
    display: none;
}

.VFXDataAnchor.Output.hasDepth {
    border-left-width: 1px;
    margin-left: 9px;
    border-left-color: #4C4C4C;
}

.VFXDataAnchor.Output.expandable #type {
    padding-left: 16px;
    background-size: 12px 12px;
    background-position-x: left;
    background-image:resource("Builtin Skins/DarkSkin/Images/IN foldout.png");
}

.VFXDataAnchor.Output.expandable #type:hover
{
    background-image:resource("Builtin Skins/DarkSkin/Images/IN foldout focus.png");
}

.VFXDataAnchor.Output.expandable #type:hover:active
{
    background-image:resource("Builtin Skins/DarkSkin/Images/IN foldout act.png");
}

.VFXDataAnchor.Output.expandable.icon-expanded #type
{
    background-image:resource("IN foldout on.png");
}

.VFXDataAnchor.Output.expandable.icon-expanded #type:hover
{
    background-image:resource("IN foldout focus on.png");
}
.VFXDataAnchor.Output.expandable.icon-expanded #type:active
{
    background-image:resource("IN foldout act on.png");
}

VFXDataAnchor.Output #type
{
    color: rgb(153,153,153);
    -unity-text-align: middle-right;
    flex: 1 0 auto;
}

.VFXDataAnchor .propertyrm
{
    align-self: center;
    flex: 1 1 auto;
}

.VFXDataAnchor .space
{
    border-radius: 4px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
    margin-top: 4px;
    margin-bottom: 4px;
    padding-left: 4px;
    padding-right: 4px;
    padding-bottom: 2px;
    margin-right: 10px;
    border-color: rgb(153,153,153);
    color: rgb(153,153,153);
    -unity-text-align: middle-center;
}

.VFXOutputDataAnchor #icon
{
    width: 13px;
    height: 13px;
}

.VFXOutputDataAnchor.icon-expandable #icon
{
    background-image: resource("IN foldout@2x.png");
}

.VFXOutputDataAnchor.icon-expandable #icon:hover
{
    background-image: resource("IN foldout focus@2x.png");
}

.VFXOutputDataAnchor.icon-expandable #icon:hover:active
{
    background-image: resource("IN foldout act@2x.png");
}

.VFXOutputDataAnchor.icon-expandable.icon-expanded #icon
{
    background-image: resource("IN foldout on@2x.png");
}

.VFXOutputDataAnchor.icon-expandable.icon-expanded #icon:hover
{
    background-image: resource("IN foldout focus on@2x.png");
}

.VFXOutputDataAnchor.icon-expandable.icon-expanded #icon:hover:active
{
    background-image: resource("IN foldout act on@2x.png");
}

.VFXOutputDataAnchor #type
{
    color: #c4c4c4;
    margin-right: 12px;
    flex-grow: 1;
}
/*Default*/
.VFXDataAnchor.connected > #connector
{
    border-color: #787878;
}

TextElement:disabled
{
    opacity: 0.5;
}

VFXEditableDataAnchor.activationslot
{
    flex-grow: 0;
    position: absolute;
}

VFXEditableDataAnchor.activationslot BoolPropertyRM {
    margin-left: 4px;
}
VFXEditableDataAnchor.activationslot.subgraphblock #connector
{
    display: none;
}

VFXEditableDataAnchor.activationslot.subgraphblock > BoolPropertyRM
{
    margin-left: 12px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXDataAnchor.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXEdgeDragInfo.uss---------------
.
.
VFXEdgeDragInfo
{
    position:Absolute;
    max-height:200px;
    border-color:#881f1f;
    border-radius:8px;
    background-color:#282828;

    border-top-width:2px;
    border-bottom-width:2px;
    border-left-width:2px;
    border-right-width:2px;

    padding-left:8px;
    padding-right:8px;
    padding-top:8px;
    padding-bottom:8px;
}

VFXEdgeDragInfo #title
{
    font-size:11px;
    color:#BBB;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXEdgeDragInfo.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXFilterWindow.uss---------------
.
.
#Header {
    flex-direction: Row;
    background-color: var(--unity-colors-inspector_titlebar-background);
}

#SearchField {
    width: auto;
    margin: 12px 8px;
    flex-grow: 1;
    flex-shrink: 1;
}

/* List of variant toggle */
#ListVariantToggle {
    align-self: Center;
    width: 22px;
    height: 22px;
    background-size: 19px 19px;
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/sub-variant-hidden.png");
}

.dark #ListVariantToggle {
    -unity-background-image-tint-color: #C2C2C2;
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_sub-variant-hidden.png");
}

#ListVariantToggle:checked {
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/sub-variant-visible.png");
}

.dark #ListVariantToggle:checked {
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_sub-variant-visible.png");
}

/* Expand / collapse details panel toggle */
#CollapseButton {
    align-self: Center;
    width: 22px;
    height: 22px;
    margin-right: 8px;
    padding: 3px;
}

#ArrowImage {
    --unity-image: var(--unity-icons-arrow_left);
}

#Header Toggle {
    margin: 0 3px;
    border-radius: 2px;
}

#Header Toggle .unity-toggle__input {
    display: none;
}

#Header Toggle:hover {
    background-color: var(--unity-colors-button-background-hover);
}

#Header Toggle:active {
    background-color: var(--unity-colors-button-background-hover_pressed);
}

#CollapseButton:checked #ArrowImage {
    --unity-image: var(--unity-icons-arrow_right);
}

#CollapseButton #unity-checkmark {
    display: none;
}
/*--------------*/

#ListOfNodesPanel {
    flex-grow: 1;
    flex-shrink: 1;
    min-width: 200px;
}

#ListOfNodes {
    padding: 8px;
    min-width: 200px;
}

#DetailsPanel {
    min-width: 200px;
}

Label {
    flex-grow: 1;
    -unity-text-align: middle-left;
}

.unity-tree-view__item {
    height: 24px;
    background-color: rgba(0,0,0,0);
}

.unity-tree-view__item-toggle {
}

.unity-tree-view__item:hover {
    background-color: var(--unity-colors-highlight-background-hover);
}

.unity-tree-view__item:selected {
    background-color: var(--unity-colors-highlight-background);
}
 /* Node name and labels and highlight */
.node-name {
    margin-right: 4px;
    padding: 0;
    flex-grow: 0;
    color: var(--unity-colors-default-text);
}

.nodes-label-spacer {
    flex-grow: 1;
}

.node-name.setting {
    align-self: center;
    margin-left: 0;
    margin-right: 4px;
    flex-grow: 0;
    padding: 2px 3px;
    border-width: 0;
    border-radius: 3px;
    color: #B7B7B7;
    background-color: #323232;
}

.node-name.left-part {
    border-right-width: 0;
    border-bottom-right-radius: 0;
    border-top-right-radius: 0;
    padding-right: 0;
    margin-right: 0;
}

.node-name.middle-part {
    border-left-width: 0;
    border-right-width: 0;
    border-radius: 0;
    padding-left: 0;
    padding-right: 0;
    margin: 0;
}

.node-name.right-part {
    border-left-width: 0;
    border-bottom-left-radius: 0;
    border-top-left-radius: 0;
    padding-left: 0;
    margin-left: 0;
}

.unity-collection-view__item .node-name.highlighted {
    -unity-font-style: bold;
    color: #FACA61;
}
/* ---------- */

.treenode {
    flex-grow: 1;
    flex-direction: row;
}

/* Separator */
.unity-tree-view__item.separator {
    height: 36px;
}

.separator:hover, .separator:checked {
    background-color: rgba(0, 0, 0, 0);
}

.separator Label {
    flex-grow: 1;
    margin-top: 8px;
    margin-right: 4px;
    margin-bottom: 10px;
    color: var(--unity-colors-app_toolbar_button-background-checked);
    border-color: var(--unity-colors-app_toolbar_button-background-checked);
    border-bottom-width: 1px;
}

#ListOfVariants .separator Label {
    color: var(--unity-colors-default-text);
}
/* ---------- */

.category Image {
    width: 16px;
    height: 16px;
    align-self: center;
    margin-right: 4px;
    background-image: resource("Icons/Project.png");
}

.dark .category Image {
    background-image: resource("Icons/d_Project.png");
}

.category.favorite Image {
    margin-right: 4px;
    background-image: resource("Icons/Favorite_colored.png");
}

.dark .category.favorite Image {
    background-image: resource("Icons/d_Favorite_colored.png");
}

.treenode Button {
    align-self: center;
    width: 16px;
    height: 16px;
    border-width: 0;
    border-radius: 0;
    margin-right: 4px;
    background-color: rgba(0, 0, 0, 0);
}

/* Favorite button */
.treenode:hover #favoriteButton {
    -unity-background-image-tint-color: #A1A1A1;
    background-image: resource("Icons/Favorite.png");
}

.dark .treenode:hover #favoriteButton {
    background-image: resource("Icons/d_Favorite.png");
}

.treeleaf.favorite #favoriteButton, .treeleaf #favoriteButton:hover {
    -unity-background-image-tint-color: white;
    background-image: resource("Icons/Favorite_colored.png");
}

.dark .treeleaf.favorite #favoriteButton, .treeleaf #favoriteButton:hover {
    -unity-background-image-tint-color: white;
    background-image: resource("Icons/d_Favorite_colored.png");
}
/* ------------- */

#showDetailsPanelButton {
    width: 16px;
    height: 16px;
    -unity-background-image-tint-color: #858585;
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/NodeSearch-expand@2x.png");
}

.dark #showDetailsPanelButton {
    width: 16px;
    height: 16px;
    -unity-background-image-tint-color: #858585;
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_NodeSearch-expand@2x.png");
}

.treeleaf:hover #showDetailsPanelButton, .treeleaf:checked #showDetailsPanelButton {
    -unity-background-image-tint-color: #A1A1A1;
}

.treeleaf:hover #showDetailsPanelButton:hover {
    -unity-background-image-tint-color: white;
}

Label.category {
    align-self: center;
    height: 16px;
    padding-left: 20px;
    background-position-x: left;
    background-size: contain;
    -unity-background-image-tint-color: #505050;
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/Folder.png");
}

.dark Label.category {
    -unity-background-image-tint-color: #C2C2C2;
}

.favorite Label.category {
    background-position-x: left;
    background-size: contain;
    -unity-background-image-tint-color: white;
    background-image: resource("Icons/Favorite_colored.png");
}

.dark .favorite Label.category {
    background-image: resource("Icons/d_Favorite_colored.png");
}

/* Details panel */

#TitleAndDoc {
    height: 40px;
    padding: 5px;
    flex-grow: 0;
    align-items: flex-start;
    flex-direction: row;
    background-color: var(--unity-colors-inspector_titlebar-background);
}

#HelpButton {
    align-self: center;
    width: 22px;
    height: 22px;
    padding: 0;
    margin-top: 0;
    margin-bottom: 0;
    border-width: 0;
    border-radius: 3px;
    background-color: rgba(0, 0, 0, 0);
    background-image: resource("Icons/_Help@2x.png");
}

.dark #HelpButton {
    background-image: resource("Icons/d__Help@2x.png");
}

#HelpButton:hover {
    background-color: var(--unity-colors-button-background-hover);
}

#HelpButton:hover:disabled {
    background-color: initial;
}

#HelpButton.hidden {
    display: none;
}

#Title {
    align-self: center;
    margin-left: 8px;
    flex-grow: 1;
    font-size: 14px;
    -unity-font-style: bold;
}

#Description {
    flex-grow: 1;
    flex-wrap: wrap;
    white-space: normal;
}

#ListOfVariants {
    padding: 8px;
    display: none;
}

#CategoryLabel {
    margin: 12px 8px 0px 8px;
    flex-grow: 0;
    flex-wrap: wrap;
    white-space: normal;
}

#ColorFieldRow {
    margin-top: 16px;
    flex-direction: row;
}

#CategoryColorField {
    flex-grow: 1;
    margin: 8px;
}

#ResetButton {
    margin: 0 8px 1px 0;
    align-self: center;
    width: 20px;
    height: 20px;
    background-size: 80% 80%;
    background-image: resource("Icons/Refresh@2x.png");
}

.dark #ResetButton {
    background-image: resource("Icons/d_Refresh@2x.png");
}

#NoSubvariantLabel {
    margin-left: 8px;
    align-self: center;
    padding-left: 20px;
    flex-wrap: wrap;
    white-space: normal;
    background-position-x: left;
    background-size: 16px 16px;
    background-image: var(--unity-icons-console_entry_info_small);
}

#Resizer {
    position: absolute;
    right: 2px;
    bottom: 2px;
    width: 12px;
    height: 12px;
    cursor: resize-up-left;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXFilterWindow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXFlow.uss---------------
.
.
VFXFlowAnchor.port.EdgeConnector
{
    padding-left:0;
    padding-right:0;
    margin-left: 12px;
    margin-right: 12px;
    width: 100px;
    height: 28px;
    --port-color:rgb(220, 220, 220);
    flex-direction:column-reverse;
}

VFXFlowAnchor.port.EdgeConnector.output
{
    flex-direction:column;
}


VFXFlowAnchor.port.EdgeConnector #connector
{
    width: 12px;
    height: 12px;
    margin-left: 2px;
    margin-right: 2px;
}

VFXFlowAnchor.port.EdgeConnector #type
{
    position:Absolute;
    width: 42px;
    top: 1px;
    padding-left: 30px;
    margin-left:0;
    margin-right:0;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXFlow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXFlowEdge.uss---------------
.
.
VFXFlowEdge.edge
{
    --edge-width:4;
    border-color : rgba(146,146,146,255);
}

VFXFlowEdge.edge.selected
{
    border-color : rgba(240,240,240,255);
}

VFXFlowEdge.edge:hover
{
    --edge-width:6;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXFlowEdge.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXIconBadge.uss---------------
.
.
﻿#BadgesHolder
{
    top: -3px;
    right: -30px;
    flex-direction: column;
    position: absolute;
}

VFXOperatorUI.superCollapsed #BadgesHolder
{
    top: 6px;
}

VFXContextUI > #BadgesHolder {
    top: 32px;
}

VFXBlockUI.collapsed > #BadgesHolder > VFXIconBadge {
    position: absolute;
    right: 0px;
}

VFXIconBadge {
    margin-bottom: 0px;
    width: 24px;
    height: 24px;
    flex-direction: row;
}

VFXIconBadge #tip {
    align-self: center;
    scale: 1 -1;
    width: 9px;
    height: 7px;
    rotate: 90deg;
    margin-left: -9px;
    background-image : resource("GraphView/Badge/CommentTip.png");
}

VFXIconBadge.badge-error #tip {
    -unity-background-image-tint-color: #b10c0c;
}

VFXIconBadge.badge-perf #tip, VFXIconBadge.badge-warning #tip {
    -unity-background-image-tint-color: #ffc107;
}

VFXIconBadge.badge-error {
    background-image : resource("console.erroricon@2x");
}

VFXIconBadge.badge-perf, VFXIconBadge.badge-warning {
    background-image : resource("console.warnicon@2x");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXIconBadge.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXLabeledField.uss---------------
.
.

.cursor-slide-arrow {
    cursor: slide-arrow;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXLabeledField.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXNode.uss---------------
.
.
#divider.vertical {
    width: 0.01px;
    border-right-width: 1px;
}

#divider.vertical.hidden {
    width:0;
    border-right-width: 0;
}

.VFXNodeUI.node #node-border > #title
{
    justify-content:flex-start;
    border-radius: 6px 6px 0 0;
    border-bottom-width: 1px;
    border-bottom-color: #242424;
}

.VFXNodeUI.node #node-border > #title > Label {
    overflow:hidden;
}

VFXBlockUI.node #node-border > #title > Label.first {
    margin-left: 54px;
}

.VFXNodeUI.node #node-border > #title > #spacer {
    flex-grow: 1;
}

.VFXNodeUI #contents
{
    background-color: rgba(63,63,63,0.8);
    border-radius: 0 0 6px 6px;
}


.VFXNodeUI #settings
{
    border-bottom-color: rgba(35,35,35,0.8);
    border-bottom-width: 1px;
    margin-top: 8px;
    margin-bottom: 0px;
    padding-left: 8px;
    padding-right: 8px;
    padding-bottom: 8px;
    overflow:hidden;
}

.VFXNodeUI.collapsed #settings
{
    display:none;
    max-height:0;
    height:0;
}

.VFXNodeUI #settings.nosettings {
    display: none;
}

.node > #node-border > #title #collapse-button {
    width: 24px;
    height: 24px;
    margin-right: 4px;
    align-self: center;
    border-radius: 3px;
    opacity: 0.5;
    background-size: 16px 16px;
    background-image : url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/NodeChevronDown@2x.png");
}

.node > #node-border > #title #collapse-button:hover {
    opacity: 1;
    background-color: #2B2B2B;
}

.node > #node-border > #title #collapse-button:disabled {
    background-color: initial;
    opacity: 0.2;
}

.node.block-disabled > #node-border > #title #collapse-button:hover {
    opacity: 1;
    background-color: #383838;
}

.node.collapsed > #node-border > #title #collapse-button {
    rotate: 90deg;
}

.node.superCollapsed > #node-border > #title > #edit,.VFXNodeUI.superCollapsed > #node-border > #title > #collapse-button
{
    display: none;
}

.VFXNodeUI.node #contents > #top > #input
{
    padding-top: 6px;
    padding-bottom: 6px;
    flex: 1 1 auto;
    background-color: rgba(0,0,0,0.0);
}

.VFXNodeUI.node #contents > #top > #output
{
    padding-top: 6px;
    padding-bottom: 6px;
    flex: 0 1 auto;
    border-radius: 0 0 6px 0;
}

#line
{
    background-color:#4c4c4c;
}

.VFXNodeUI.hovered #selection-border
{
    border-width: 2px;
    background-color:rgba(255,128,0,0.4);
    border-color: rgba(255,128,0,1);
}

#title Label {
    align-self: center;
}

.setting {
    align-self: center;
    margin-left: 4px;
    margin-right: 4px;
    flex-grow: 0;
    padding: 1px 2px;
    border-width: 0;
    border-radius: 2px;
    color: #B7B7B7;
    background-color: #323232;
}

VFXContextUI #inside > #title > .setting {
    color: #B1B1B1;
    background-color: #272727;
}

VFXBlockUI #top, VFXOperatorUI #top {
    flex-grow: 1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXNode.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXNodeUI.uss---------------
.
.
#title
{
    flex-direction:row;
}

#selection-border {
    border-left-width:0;
    border-top-width:0;
    border-right-width:0;
    border-bottom-width:0;
    border-radius: 8px;
    margin-bottom: 1px;
    margin-left: 1px;
    margin-right: 1px;
    margin-top: 1px;
    position: absolute;
    left:0;
    right:0;
    top:0;
    bottom:0;
}

:hover > #selection-border {
    border-color: rgba(68,192,255, 0.5);
    border-top-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
}

:selected > #selection-border {
    border-color: #44C0FF;
    border-left-width: 1px;
    border-top-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
}

:selected:hover > #selection-border {
    border-color: #44C0FF;
    border-top-width: 2px;
    border-left-width: 2px;
    border-right-width: 2px;
    border-bottom-width: 2px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXNodeUI.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXOperator.uss---------------
.
.
VFXOperatorUI.node #middle
{
    flex:1 0 auto;
    background-color:#363636;
}

VFXOperatorUI #title #edit
{
    opacity: 0.6;
    width: 24px;
    height: 24px;
    align-self: center;
    border-width: 0;
    background-color: rgba(0, 0, 0, 0);
    background-size: 16px 16px;
    background-image: resource("d_SettingsIcon@2x.png");
}

VFXOperatorUI #title #edit:hover {
    opacity: 1;
    background-color: #2B2B2B;
}

VFXOperatorUI > #node-border #contents > #top > #output {
    flex: 1 1 auto;
}

VFXOperatorUI.node #node-border > #title > Label.first {
    margin-left: 16px;
}

VFXOperatorUI.node.superCollapsed #node-border > #title > Label.last {
    margin-right: 24px;
}

OperandInfo PopupField
{
    height: 16px;
}

OperandInfo .unity-text-element, OperandInfo .unity-text-field
{
    margin-left: 4px;
    flex: 1 1 auto;
}

#edit-container
{
    position:Absolute;
    top:0;
    bottom:0;
    right:0;
    left:0;
    background-color:#282828;
}

#edit-container.ReorderableList > #ListContainer
{
    flex:1 0 auto;
}

.VFXUniformOperatorEdit#edit-container
{
    justify-content:center;
}

.VFXUniformOperatorEdit#edit-container .unity-label
{
    margin-left: 8px;
    margin-right: 8px;
}

VFXOperatorUI.superCollapsed.VFXNodeUI.node .mainContainer > #middle
{
    width:0;
    height:0;
}

VFXOperatorUI.superCollapsed.VFXNodeUI.node #settings-divider, VFXOperatorUI.superCollapsed.VFXNodeUI.node #divider
{
    border-bottom-width:0;
    height:0;
}

.VFXNodeUI.superCollapsed
{
    height: 30px;
    width: auto;
}

.VFXNodeUI.VFXNodeUI.superCollapsed #settings
{
    margin-top: 0;
    margin-bottom: 0;
    display: none;
}

.VFXNodeUI.node.superCollapsed #contents
{
    background-color: rgba(0,0,0,0);
}

.VFXNodeUI.node.superCollapsed #node-border
{
    flex-grow: 1;
    overflow: hidden;
}

.VFXNodeUI.node.superCollapsed #node-border > #title
{
    border-radius: 6px;
    flex-grow: 1;
    position:Relative;
    left:0;
    right:0;
    top:0;
    bottom:0;
    height: auto;
    margin-bottom:0;
    border-bottom-width: 0;
}

.VFXNodeUI.node.superCollapsed #node-border > #title > .unity-label#title-label {
    margin-bottom: 3px;
    margin-top: 3px;
    margin-left: 20px;
    margin-right: 20px;
}

.VFXNodeUI.node.superCollapsed .mainContainer, .VFXNodeUI.superCollapsed #contents, .VFXNodeUI.superCollapsed #contents > #top
{
    position:Absolute;
    left:0;
    right:0;
    top:0;
    bottom:0;
}

.VFXNodeUI.superCollapsed #contents > #top > #input
{
    padding-top:0;
    padding-bottom:0;
    background-color:rgba(0,0,0,0);
    flex:1 0 auto;
}

.VFXNodeUI.superCollapsed #contents > #top > #divider
{
    width:0;
    border-right-width:0;
}

.VFXNodeUI.superCollapsed #contents > #top > #middle
{
    flex:0 0 auto;
}

.VFXNodeUI.superCollapsed #contents > #top > #output
{
    position:Absolute;
    top:0;
    bottom:0;
    right: 1px;
    flex-grow:0;
    flex:0 0 auto;
    width: 20px;
    background-color:rgba(0,0,0,0);
}

.VFXNodeUI.node.superCollapsed #right #output
{
    align-self:flex-end;
    width: 16px;
}

.VFXNodeUI.superCollapsed #type
{
    display:none;
}

.VFXNodeUI.superCollapsed #icon
{
    max-width:0;
}

.VFXNodeUI.superCollapsed .propertyrm
{
    min-width:0;
    max-width:0;
    overflow: hidden;
}

.VFXNodeUI.superCollapsed.node .mainContainer #left
{
    width: 120px;
}

.VFXNodeUI.superCollapsed.node .VFXDataAnchor
{
    position: Absolute;
    left: 0;
    right: 4px;
    width: 16px;
    top: 11px;
    height: 0;
    min-height: 0;
    overflow: hidden;
}

.VFXNodeUI.superCollapsed.node .VFXDataAnchor .propertyrm
{
    display:none;
}

.VFXNodeUI.superCollapsed.node .VFXDataAnchor.first
{
    top: auto;
    height: auto;
    bottom: 0;
    min-height: 20px;
}


.VFXNodeUI.superCollapsed.node .VFXDataAnchor.first #connector
{
    margin-top: 0;
    margin-right: 0;
}

.VFXNodeUI.superCollapsed.node VFXOutputDataAnchor.VFXDataAnchor
{
    width: auto;
}

.VFXNodeUI.superCollapsed.node .VFXDataAnchor #line
{
    background-color:rgba(0,0,0,0);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXOperator.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXPanelBadge.uss---------------
.
.
﻿#Badge
{
    flex-direction: row;
    background-color: rgba(40,40,40,1);
    height: 16px;
    border-radius: 2px;
    margin-right: 2px;
    margin-left: 2px;
    padding: 0 4px 0 14px;
    font-size: 10px;
    background-size: 10px 10px;
    background-position-x: 2px;
    -unity-text-align: middle-right;
}

#Badge.awake
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_Awake.png");
}
#Badge.sleep
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_Sleep.png");
}

#Badge.play
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_playBadge.png");
}
#Badge.pause
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_pauseBadge.png");
}

#Badge.visible
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_Visible.png");
}
#Badge.culled
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_Culled.png");
}

#Badge.stopped
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_stopBadge.png");
}

#Badge.position
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_Position.png");
}

#Badge.rotation
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_Rotation.png");
}

#Badge.age
{
    background-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_Age.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXPanelBadge.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXParameter.uss---------------
.
.

VFXParameterUI.node > #node-border > #title
{
    font-size: 11px;
    height: auto;
    padding-left: 8px;
}

VFXParameterUI.node.superCollapsed > #node-border > #title > #title-label
{
    margin-right:0;
    margin-left:0;
    margin-top:0;
    margin-bottom:0;
    padding-right:0;
    padding-left:0;
    padding-top:0;
    padding-bottom:0;
    font-size: 11px;
}

VFXParameterUI.node > #node-border > #title > #pill
{
    background-color: #303030;
    border-radius: 12px;
    flex-direction:row;
    padding-left: 8px;
    margin: 4px 8px 4px 0;
}

VFXParameterUI.node > #selection-border {
    border-radius: 12px;
}

VFXParameterUI.node.hovered > #selection-border
{
    background-color:rgba(255,128,0,0.4);
    border-color: rgba(255,128,0,1);
    border-width: 2px;
}

VFXParameterUI.node:hover > #selection-border
{
    border-color: #44C0FF;
    border-width: 1px;
}

VFXParameterUI.node > #node-border
{
    border-radius: 10px;
    background-color: #383838;
}

VFXParameterUI.node.superCollapsed > #node-border
{
    flex-direction: row;
}

VFXParameterUI.node.superCollapsed.output > #node-border
{
    flex-direction: row-reverse;
}

VFXParameterUI.node.superCollapsed > #selection-border
{
    border-radius: 12px;
}

VFXParameterUI.node.superCollapsed #pill {
    padding-right: 16px;
}

VFXParameterUI.node.superCollapsed > #node-border > #title
{
    background-color:rgba(0,0,0,0);
    margin-right: 16px;
}

VFXParameterUI.node.superCollapsed.output > #node-border > #title
{
    margin-left: 16px;
    margin-right: 0;
}

VFXParameterUI.node.superCollapsed > #node-border > #contents > #top > #output
{
    justify-content:space-around;
    background-color:rgba(0,0,0,0);
    align-items:flex-end;
}

VFXParameterUI.node.superCollapsed > #node-border > #contents > #top > #input
{
    justify-content:space-around;
    background-color:rgba(0,0,0,0);

}

VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Output,
VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Input
{
    border-width: 0;
    position:Absolute;
    top: 14px; /*hard coding the position so that edge are at the right height*/
}

VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Output #connector,
VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Input #connector {
    opacity: 0;
}

VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Output.first #connector,
VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Input.first #connector {
    opacity: 1;
}

VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Output #type,
VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Input #type,
VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Output #line,
VFXParameterUI.node.superCollapsed > #node-border > #contents .VFXDataAnchor.Input #line
{
    display: none;
}

VFXParameterUI.node > #node-border > #contents > #top
{
    flex:1 0 auto;
}

VFXParameterUI.node > #node-border > #contents #divider
{
    background-color:rgba(0,0,0,0);
    border-color:rgba(0,0,0,0);
    border-bottom-width:0;
}

VFXParameterUI.node > #node-border > #title > #pill > #exposed-icon
{
    align-self:center;
    width: 6px;
    height: 6px;
}

VFXParameterUI.node.exposed > #node-border > #title > #pill > #exposed-icon
{
    --unity-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/exposed-dot.png");
}

.node > #node-border > #title #collapse-button {
    margin: 2px 8px 2px 4px;
}

.node.superCollapsed > #node-border > #title #collapse-button {
    display: none;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXParameter.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXProfilingBoard.uss---------------
.
.
VFXProfilingBoard.graphElement
{
    position:Absolute;
    padding: 0;
    margin:0;
    min-width: 286px;
    min-height: 100px;
}

VFXProfilingBoard.graphElement > .mainContainer
{
    flex: 1 1 auto;
    background-color: #292929;
    border-color: #191919;
}

*
{
    font-size: 12px;
}

#divider {
    background-color: rgba(35,35,35,0.8);
    border-color: rgba(35,35,35,0.8);
}

#divider.horizontal {
    height: 0.05px;
    border-bottom-width: 1px;
}

VFXProfilingBoard .unity-scroll-view
{
    flex-direction:column;
    align-items:stretch;
    overflow:hidden;
    flex: 1 1 auto;
}

VFXProfilingBoard, #component-container
{
    flex-direction:column;
    align-items:stretch;
    overflow:hidden;
}

#header {
    flex-direction: row;
    align-items: stretch;
    background-color: #393939;
    border-bottom-width: 1px;
    border-color: #212121;
    border-top-right-radius: 4px;
    border-top-left-radius: 4px;
    padding-left: 8px;
    padding-top: 8px;
    padding-bottom: 8px;
}
#header > #labelContainer {
    flex:1 0 auto;
    flex-direction: column;
    align-items: stretch;
}
#header > #labelContainer > #titleContainer > #titleLabel {
    font-size : 14px;
    -unity-font-style: bold;
    color: #c1c1c1;
    flex-grow: 1;
}
#header > #labelContainer > #subtitle >#subTitleLabel {
    font-size: 11px;
    color: #606060;
    white-space: normal;
}

#attach, #select
{
    width: 60px;
}

#component-path
{
    flex:1;
}

#attach-container
{
    padding-top: 8px;
    padding-bottom: 8px;
}

#events-label
{
    padding-top: 8px;
}
#subtitle, #titleContainer
{
    flex-direction:row;
    flex:1 0 auto;
    align-items:center;
}

#subTitleLabel
{
    height: 16px;
    flex-grow: 1;
    -unity-text-align: lower-left;
    margin-right: 4px;
    overflow: hidden;
}

#subTitle-icon
{
    margin-right: 4px;
}

#title-icon
{
    margin-left: 4px;
    margin-right: 4px;
    width: 16px;
    height: 16px;
    --unity-image: url("project:///Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/d_debug.png");
}

.unity-foldout__toggle
{
    background-color: #474747;
    padding-top: 4px;
    padding-bottom: 4px;
    margin-left: 0;
    margin-right: 0;
    margin-bottom : 1px;
    height: 30px;
}

.unity-foldout__toggle__
{
    margin-left: 0;
    margin-right: 0;
}

.unity-foldout__input
{
    margin-left: 10px;
    margin-right: 0;
}

.unity-foldout__content
{
    margin-top: 4px;
    margin-bottom: 4px;
}


/*
.unity-foldout--depth-0
{
    margin-top: 4px;
    margin-bottom: 4px;
}
*/

#shortcut-windows
{
    flex-grow: 0;
    margin: 0 3px;
    height:16px ;
    background-color: rgba(0,0,0,0);
    border-color: rgba(0,0,0,0);
    background-image: resource("Builtin Skins/DarkSkin/Images/pane options.png");
    align-self: flex-end;
    border-radius: 1px;
}

#shortcut-windows:hover
{
    background-color:rgba(103,103,103,1);
}

#exec-time Label {
    flex-grow: 2;
}

.dynamic-label {
    flex-direction: row;
    padding: 2px 0;
}

.dynamic-label .main {
    flex-grow: 1;
}

.dynamic-label .dynamic {
    flex-grow: 0;
    align-self: flex-end;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXProfilingBoard.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXReorderableList.uss---------------
.
.
.ReorderableList
{
    background-color:#282828;
}

.ReorderableList > #ListContainer
{
    background-color:#383838;
    padding-top: 2px;
    padding-bottom: 4px;
}

.ReorderableList > * > *
{
    flex-direction:row;
    align-items:center;
}

.ReorderableList > * > *.selected
{
    background-color:#3d6091;
}

.ReorderableList > * > * > #DraggingHandle
{
    width: 16px;
    align-items:center;
    justify-content:center;
}

.ReorderableList > #Toolbar
{
    flex-direction:row;
    align-items:center;
    justify-content:flex-end;
}

.ReorderableList > #Toolbar > *
{
    height: 16px;
    width: 32px;
    justify-content:center;
}

.ReorderableList > #Toolbar > #Add
{
    border-bottom-left-radius: 4px;
}
.ReorderableList > #Toolbar > #Remove
{
    border-bottom-right-radius: 4px;
}

.ReorderableList > #Toolbar > * > #icon
{
    width: 16px;
    height: 16px;
}

#DraggingHandle #icon
{
    width: 10px;
    height: 6px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXReorderableList.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXReorderableListDark.uss---------------
.
.
.ReorderableList > #Toolbar > #Add > #icon
{
    background-image: resource("Icons/d_Toolbar Plus.png");
}
.ReorderableList > #Toolbar > #Remove > #icon
{
    background-image: resource("Icons/d_Toolbar Minus.png");
}

#DraggingHandle #icon
{
    background-image: resource("Builtin Skins/DarkSkin/Images/WindowBottomResize.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXReorderableListDark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXReorderableListLight.uss---------------
.
.
.ReorderableList > #Toolbar > #Add > #icon
{
    background-image: resource("Icons/Toolbar Plus.png");
}

.ReorderableList > #Toolbar > #Remove > #icon
{
    background-image: resource("Icons/Toolbar Minus.png");
}

#DraggingHandle #icon
{
    background-image: resource("Builtin Skins/LightSkin/Images/WindowBottomResize.png");
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXReorderableListLight.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXStickynote.uss---------------
.
.
VFXStickyNote .resizableElement > #right > #top-right-resize,
VFXStickyNote .resizableElement > #left >  #top-left-resize,
VFXStickyNote .resizableElement > #right >  #bottom-right-resize,
VFXStickyNote .resizableElement > #left >  #bottom-left-resize {
    width: 12px;
    height: 12px
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXStickynote.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXSystemBorder.uss---------------
.
.
VFXSystemBorder.graphElement
{
    --start-color:#17AB6A;
    --middle-color:#BF9220;
    --end-color:#C24F30;
    border-width: 6px;
    opacity:0.5;
    flex-direction:column;
    align-items:stretch;
    --layer:-400;
}

VFXSystemBorder.graphElement Label
{
    border-top-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-top-width: 1px;
    border-left-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    margin-left: 12px;
    margin-right: 12px;
    margin-top: 1px;
    margin-bottom: 1px;
    padding-left:0;
    padding-right:0;
}

VFXSystemBorder.graphElement Label:hover
{
    border-color: rgba(68,192,255, 0.5);
}


#title-field
{
    position: absolute;
}

#title, #title-field
{
    font-size: 48px;
    white-space: normal;
}

#title
{
    color:#17AB6A;
    opacity:0.75;
}

#title.empty
{
    height : 12px;
}

VFXSystemBorder.graphElement TextField#title-field
{
    padding-left:0;
    padding-right:0;
    padding-top:0;
    padding-bottom:0;
    margin-left:0;
    margin-right:0;
    margin-top:0;
    margin-bottom:0;

    position: absolute;
}

VFXSystemBorder.graphElement TextField#title-field #unity-text-input
{
    padding-left:0;
    padding-right:0;
    padding-top:0;
    padding-bottom:0;
    margin-left:0;
    margin-right:0;
    margin-top:0;
    margin-bottom:0;
    background-color:rgba(0,0,0,0);
    background-image:none;
    font-size: 48px;
    white-space: normal;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXSystemBorder.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXTemplateWindow.uss---------------
.
.
TreeView.remove-toggle Toggle {
    display: none;
}

#SplitPanel {
    flex-grow: 1;
    flex-direction: row;
    align-items: stretch;
    justify-content: flex-start;
    background-color: rgba(0, 0, 0, 0);
}

#SplitPanel .unity-two-pane-split-view__dragline-anchor {
    background-color: var(--unity-colors-default-border);
}

#ListOfTemplatesPanel {
    flex-grow: 1;
    flex-shrink: 0;
    min-width: 200px;
    background-color: var(--theme-view-background-color);

}

#ListOfTemplates {
    flex-grow: 1;
}

#ListOfTemplates .unity-scroll-view__content-viewport {
    margin-top: -3px;
}

#DetailsPanel {
    flex-grow: 1;
    flex-shrink: 1;
    min-width: 200px;
    background-color: var(--unity-theme-view-background-color-lighter);
}

#Screenshot {
    height: 50%;
}

#TitleAndDoc {
    flex-direction: row;
    background-color: var(--unity-colors-inspector_titlebar-background);
}

#HelpButton {
    padding: 0;
    margin-top: 0;
    margin-bottom: 0;
    align-self: center;
    border-width: 0;
    border-radius: 0;
    background-color: rgba(0, 0, 0, 0);
}

#HelpButton:hover {
    background-color: var(--unity-colors-toolbar_button-background-hover);
}

#Title {
    margin: 4px 0 4px 16px;
    flex-grow: 1;
    font-size: 14px;
    -unity-font-style: bold;
}

#Description {
    margin: 16px;
    flex-wrap: wrap;
    white-space: normal;
}

.vfxtemplate-section {
    height: auto;
}

.vfxtemplate-item {
    margin: 0 16px;
    height: auto;
}

.vfxtemplate-section Image {
    display: none;
}

.vfxtemplate-section #ItemRoot {
    margin-left: 0;
    border-radius: 0;
    border-width: 1px 0 1px 0;
    border-color: var(--unity-colors-app_toolbar-background);
    background-color: var(--unity-colors-app_toolbar-background);
}

.vfxtemplate-section #TemplateName {
    margin: 3px 0 3px 36px;
}

#ItemRoot {
    flex-grow: 1;
    flex-direction: row;
    flex-wrap: wrap;
    margin: 4px 0;
    margin-left: 0;
    border-width: 1px;
    border-radius: 4px;
    border-color: rgba(0, 0, 0, 0);
    background-color: var(--unity-colors-default-background);
}

.unity-tree-view__item {
    background-color: rgba(0, 0, 0, 0);
}

TreeView TemplateContainer {
    flex-grow: 1;
}

.vfxtemplate-item.unity-collection-view__item:hover #ItemRoot {
    border-color: var(--unity-colors-input_field-border-hover);
}

.vfxtemplate-item.unity-collection-view__item--selected #ItemRoot {
    border-color: var(--unity-colors-highlight-background);
}

.unity-tree-view__item-indent {
    display: none;
}

.unity-tree-view__item-toggle {
    position: absolute;
    left: 16px;
    top: 6px;
}

.unity-collection-view__item--selected .vfxtemplate-section #ItemRoot {
    border-color: var(--unity-colors-app_toolbar_button-border);
}

#TemplateIcon {
    margin-left: 16px;
    margin-right: 8px;
    margin-top: 2px;
    margin-bottom: 2px;
    align-self: center;
    width: 32px;
    height: 32px;
}

#TemplateName {
    justify-content: flex-start;
    white-space: normal;
    align-items: auto;
    margin-right: 16px;
    flex-grow: 1;
    flex-shrink: 1;
    align-self: center;
    color: var(--unity-colors-default-text);
}

#FooterPanel {
    height: 40px;
    flex-shrink: 0;
    flex-direction: row;
    justify-content: flex-end;
    align-items: flex-end;
    padding: 5px 8px;
    background-color: var(--theme-footer-bar-background-color);
    border-color: var(--unity-colors-default-border);
    border-top-width: 1px;
}

#FooterPanel Button {
    height: 24px;
    align-self: center;
}

#FooterPanel #InstallButton {
    position: absolute;
    left: 10px;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXTemplateWindow.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXTextEditor.uss---------------
.
.
#Save {
    align-self: flex-start;
}

#Close {
    border-width: 0px;
    padding-right: 0px;
}

#emptyMessage {
    flex-grow: 1;
    -unity-text-align: middle-center;
}

TextField {
    margin-top: 0px;
}

TextField > TextInput {
    padding-top: 8px;
    border-top-right-radius: 0px;
    border-top-left-radius: 0px;
}

Label {
    margin: 1px 3px 0px 3px;
    align-self: center;
}

ToolbarButton {
    padding-bottom: 2px;
}

Toolbar {
    height: auto;
    margin: 4px 3px 0px 3px;
    padding: 0px 3px 0px 3px;
    border-width: 1px 1px 0px 1px;
    border-top-right-radius: 3px;
    border-top-left-radius: 3px;
}

TemplateContainer {
    flex-grow: 1;
}
TemplateContainer ScrollView {
    flex-grow: 1;
}

.unity-scroll-view__content-container {
    flex-grow: 1;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXTextEditor.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXToolbar.uss---------------
.
.
.dropdown-button, .dropdown-arrow
{
    margin: 0 0 0 0;
    border-width: 0 0 0 1px;
    border-radius: 0;
}

.dropdown-button
{
    margin-right: 1px;
    flex-direction: row !important;
}

.dropdown-arrow
{
    width: 16px;
    border-width: 0 1px 0 0;
}

.dropdown-separator
{
    height: 10px;
    width: 2px;
    border-left-width: 1px;
    border-color: var(--unity-colors-default-border);
    opacity: 0.5;
    align-self: center;
}

.dropdown-arrow > VisualElement
{
    width: 15px;
    height: 16px;
    align-self: center;
    background-image: var(--unity-icons-dropdown);
}

.separator
{
    width: 6px;
}

.popup
{
    padding-top: 0px;
    border-width: 1px;
    border-color: var(--unity-colors-dropdown-border);
}

.indented
{
    padding-left: 16px;
}

.category
{
    margin-left: 4px;
    margin-top: 0px;
}

Box.section
{
    padding: 8px 0px 8px 0px;
}

Box.alternate
{
    background-color: var(--unity-colors-alternated_rows-background);
}

Button
{
    border-width: 0;
    border-radius: 0;
    background-color: rgba(0, 0, 0, 0);
    -unity-text-align: middle-left;
}

Button:hover, Toggle:hover
{
    background-color: var(--unity-colors-toolbar_button-background-hover);
}

Button:disabled
{
    opacity: 0.5;
}

Toggle Label
{
    margin-left: 4px;
}

#resyncMaterial
{
    display: none;
}

#BackButton
{
    justify-content: center;
    padding: 0 4px;
    border-left-width: 0;
}

#BackButton:hover
{
    background-color: var(--unity-colors-app_toolbar_button-background-hover);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXToolbar.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXTypeColor.uss---------------
.
.


.port.typeGradient
{
    --port-color: #FFAA6B;
}
.port.typetexture3d
{
    --port-color: #FF8B8B;
}
.port.typeTexture2DArray
{
    --port-color: #FF8B8B;
}
.port.typeCubemap
{
    --port-color: #FF8B8B;
}
.port.typeCubemapArray
{
    --port-color: #FF8B8B;
}
.port.typeboolean
{
    --port-color: #d9b3ff;
}
.port.typeuint32
{
    --port-color: #6e55dd;
}
.port.typeflipbook
{
    --port-color: #9481E6;
}
.port.typemesh
{
    --port-color : #7aa3ea;
}
.port.typeanimationcurve
{
    --port-color : #FFAA6B;
}
.port.typeGPUEvent
{
    --port-color : #17AB6A;
}
.port.typePosition
{
    --port-color: #FCD76E;
}
.port.typeVector
{
    --port-color: #FCD76E;
}
.port.typeDirectionType
{
    --port-color: #FCD76E;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXTypeColor.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXView-dark.uss---------------
.
.
.unity-toolbar.unity-disabled
{
    opacity: 1;
    background-color: #333;
}

.unity-toolbar.unity-disabled *
{
    background-color: rgba(0,0,0,0);
    color: #555;
    -unity-background-image-tint-color: #808080;
}

.unity-toolbar.unity-disabled > .unity-toolbar-toggle .unity-label
{
    color: #555;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXView-dark.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXView-light.uss---------------
.
.
.unity-toolbar > .unity-toolbar-toggle .unity-label
{
    color: #000;
}

.unity-toolbar.unity-disabled
{
    opacity: 1;
    background-color: #767676;
    -unity-background-image-tint-color: #808080;
}

.unity-toolbar.unity-disabled *
{
    background-color: rgba(0,0,0,0);
    color: #444;
}

.unity-toolbar.unity-disabled > .unity-toolbar-toggle .unity-label
{
    color: #444;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXView-light.uss---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXView.uss---------------
.
.
VFXView
{
    background-color:#1b1b1b;
    position: relative;
    flex-grow: 1;
}

.dragdisplay
{
    position:absolute;
    left:0;
    right: 0;
    height: 4px;
    margin-top: -2px;
    background-color: #44C0FF;
}

VFXView .unity-toolbar
{
    flex-wrap:wrap;
    height:auto;
}

#no-asset
{
    align-items:center;
    justify-content: center;
    flex-grow: 1;
}

#no-asset Label
{
    -unity-text-align: middle-center;
    font-size: 12px;
    white-space: normal;
    color: rgb(191, 191, 191);
}

#no-asset Button
{
    margin-top:3px;
}

ToolbarToggle
{
    border-right-width: 0px;
    justify-content:space-around;
    align-items: center;
}

ToolbarToggle .unity-image
{
    width: 16px;
    height: 16px;
}

EditorToolbarDropdown
{
    flex-direction: Row;
}

EditorToolbarDropdown:disabled, ToolbarToggle:disabled
{
    opacity: 0.5;
}

#lock-auto-attach
{
    padding: 1px 4px 0px 4px;
    align-self: center;
    align-items: center;
    border-left-width: 0;
    border-right-width: 1px;
    margin-left: 0;
}

#lock-auto-attach .unity-toggle__checkmark {
    background-image: var(--unity-icons-lock);
    width:16px;
    height:16px;
    margin-top: 2px;
}

#lock-auto-attach:checked
{
    background-color: var(--unity-colors-toolbar_button-background-checked);
}

#lock-auto-attach:checked .unity-toggle__checkmark {
    background-image: var(--unity-icons-lock-checked);
}

#attach-toolbar-button
{
    margin-left: 6px;
    border-right-width: 0;
}

#attach-toolbar-button.checked
{
    background-color: var(--unity-colors-toolbar_button-background-checked);
}

VFXView.graphView > Label.icon-badge__text {
    padding: 8px;
    max-width: 350px;
}

#lockedContainer {
    position: absolute;
    flex-grow: 1;
    width: 100%;
    height: 18px;
}

#lockedMessage {
    left: 172px;
    bottom: 0;
    -unity-text-align: upper-left;
    font-size: 16px;
    color: rgba(255, 255, 255, 0.75);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.visualeffectgraph\Editor\UIResources\uss\VFXView.uss---------------
.
.
