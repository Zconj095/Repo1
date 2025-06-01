 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\14.0\Microsoft.Common.props---------------


<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
	<PropertyGroup>
		<ImportByWildcardBeforeMicrosoftCommonProps Condition="'$(ImportByWildcardBeforeMicrosoftCommonProps)' == ''">true</ImportByWildcardBeforeMicrosoftCommonProps>
		<ImportByWildcardAfterMicrosoftCommonProps Condition="'$(ImportByWildcardAfterMicrosoftCommonProps)' == ''">true</ImportByWildcardAfterMicrosoftCommonProps>
	</PropertyGroup>

	<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportBefore\*"
		Condition="'$(ImportByWildcardBeforeMicrosoftCommonProps)' == 'true' and Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportBefore')"/>

	<PropertyGroup>
		<MicrosoftCommonPropsHasBeenImported>true</MicrosoftCommonPropsHasBeenImported>
	</PropertyGroup>

	<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportAfter\*"
		Condition="'$(ImportByWildcardAfterMicrosoftCommonProps)' == 'true' and Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportAfter')"/>
</Project>


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\14.0\Microsoft.Common.props---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\14.0\Imports\Microsoft.Common.props\ImportBefore\Microsoft.NuGet.ImportBefore.props---------------
.
.
<!--
***********************************************************************************************
Microsoft.NuGet.ImportBefore.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved. 
***********************************************************************************************
-->
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <NuGetProps Condition="'$(NuGetProps)'==''">$(MSBuildExtensionsPath)\Microsoft\NuGet\Microsoft.NuGet.props</NuGetProps>
  </PropertyGroup>
  <Import Condition="Exists('$(NuGetProps)') and '$(SkipImportNuGetProps)' != 'true'" Project="$(NuGetProps)" />
</Project>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\14.0\Imports\Microsoft.Common.props\ImportBefore\Microsoft.NuGet.ImportBefore.props---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\Current\Imports\Microsoft.Common.props\ImportBefore\Microsoft.NuGet.ImportBefore.props---------------
.
.
<!--
***********************************************************************************************
Microsoft.NuGet.ImportBefore.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved. 
***********************************************************************************************
-->
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <NuGetProps Condition="'$(NuGetProps)'==''">$(MSBuildExtensionsPath)\Microsoft\NuGet\Microsoft.NuGet.props</NuGetProps>
  </PropertyGroup>
  <Import Condition="Exists('$(NuGetProps)') and '$(SkipImportNuGetProps)' != 'true'" Project="$(NuGetProps)" />
</Project>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\Current\Imports\Microsoft.Common.props\ImportBefore\Microsoft.NuGet.ImportBefore.props---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\Microsoft\NuGet\Microsoft.NuGet.props---------------
.
.
<!--
***********************************************************************************************
Microsoft.NuGet.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved. 
***********************************************************************************************
-->
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildProjectDirectory)\$(MSBuildProjectName).nuget.props" Condition="Exists('$(MSBuildProjectDirectory)\$(MSBuildProjectName).nuget.props') AND '$(IncludeNuGetImports)' != 'false'" />
</Project>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\Microsoft\NuGet\Microsoft.NuGet.props---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\Microsoft\Portable\Microsoft.Portable.Core.props---------------
.
.
<!--
***********************************************************************************************
Microsoft.Portable.Core.props

Contains common properties that are shared by all portable library projects regardless of version.

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

***********************************************************************************************
-->
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

    <Import Project="VisualStudio\v$(VisualStudioVersion)\Microsoft.Portable.CurrentVersion.props" Condition="Exists('VisualStudio\v$(VisualStudioVersion)\Microsoft.Portable.CurrentVersion.props')"/>

    <PropertyGroup>
        <TargetPlatformIdentifier>Portable</TargetPlatformIdentifier>
        <TargetFrameworkIdentifier>.NETPortable</TargetFrameworkIdentifier>
        <TargetFrameworkMonikerDisplayName>.NET Portable Subset</TargetFrameworkMonikerDisplayName>

        <!-- Automatically reference all assemblies in the target framework -->
        <ImplicitlyExpandTargetFramework Condition="'$(ImplicitlyExpandTargetFramework)' == '' AND '$(PortableNuGetMode)' != 'true'">true</ImplicitlyExpandTargetFramework>

    </PropertyGroup>

    <!-- Redefine AssemblySearchPaths to exclude {AssemblyFolders} and {GAC}, these represent .NET-specific locations -->
    <PropertyGroup>
        <AssemblySearchPaths Condition="'$(AssemblySearchPaths)' == ''">
            {CandidateAssemblyFiles};
            $(ReferencePath);
            {HintPathFromItem};
            {TargetFrameworkDirectory};
            {Registry:$(FrameworkRegistryBase),$(TargetFrameworkVersion),$(AssemblyFoldersSuffix)$(AssemblyFoldersExConditions)};
            {RawFileName};
            $(OutDir)
        </AssemblySearchPaths>
    </PropertyGroup>

</Project>
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\xbuild\Microsoft\Portable\Microsoft.Portable.Core.props---------------
.
.
