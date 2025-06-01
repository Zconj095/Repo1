 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\FBXMayaExport.mel---------------


while (1)
{	
	if (`filetest -r "!//UNITY_TEMP//!/CommandPipe"`)
	{
		$exportNormals = 0;
		$exportTangents = 0;
		$bakeCommand = "";
	
		// Parse settings
		$waitpipe = `fopen "!//UNITY_TEMP//!/CommandPipe" "r"`;
		$filename = `fgetline $waitpipe`;
		$fbxFilename = `fgetline $waitpipe`;
		
		for ($i = 0; $i < 3; ++$i) 
		{
			$cmd = `fgetline $waitpipe`;
			
			if ($cmd == "exportNormals\n")
				$exportNormals = 1;
			else if ($cmd == "exportTangents\n")
				$exportTangents = 1;
			else if (startsWith($cmd, "bake"))
				$bakeCommand = $cmd;			
		}
		
		fclose $waitpipe;
		
		if (endsWith ($filename, "\n"))
		{
			$filename = strip ($filename);
			$fbxFilename = strip ($fbxFilename);
			
			print "Starting maya loading and fbx conversion \n";
			
			if ( `file -q -exists $filename` )
			{				
				file -force -o $filename;
				
				if (`exists FBXResetExport`)
				{
					print "Resetting export options to the default values.\n";
					FBXResetExport(); 
				}
				
				if (getApplicationVersionAsFloat() >= 2013)
				{
					print "Setting FBX version to FBX201200.\n";
					// Ensure we are using a version of FBX that Unity can currently support
					FBXExportFileVersion FBX201200; 
				}

				if ($bakeCommand != "")
				{				
					if ( `exists FBXExportBakeComplexAnimation` )
					{						
						FBXExportBakeComplexAnimation -v true;

						if ($bakeCommand != "bake\n")
						{
							// Frame range was specified
							string $tokens[];
							$bakeCommand = substring($bakeCommand, 6, size($bakeCommand));
							$bakeCommand = strip ($bakeCommand);
							$numTokens = `tokenize $bakeCommand ":" $tokens`;
							
							if ( $numTokens == 2 )
							{
								int $start = $tokens[ 0 ];
								int $end = $tokens[ 1 ]; 
								print( "Exporting frame range " + $start + ":" + $end + "\n" );
								FBXExportBakeComplexStart -v $start;
								FBXExportBakeComplexEnd -v $end;
							} 
						}
					}
					else
					{
						$allObjects = stringArrayToString (`ls -dag`, " ");
						$cmd = "";
						
						if ($bakeCommand == "bake\n")
						{
							$cmd = ("bakeResults -simulation true -t \"" + `playbackOptions -query -minTime` + ":" + `playbackOptions -query -maxTime` + "\" ");
						}
						else
						{
							$bakeCommand = substring($bakeCommand, 6, size($bakeCommand));
							$bakeCommand = strip ($bakeCommand);
							$cmd = ("bakeResults -simulation true -t \"" + $bakeCommand + "\" ");
						}
						
						$cmd = ($cmd + "-hierarchy below -sampleBy 1 -disableImplicitControl true -preserveOutsideKeys true -sparseAnimCurveBake false -controlPoints false -shape false " + $allObjects);
						evalEcho($cmd);
					}					
				}

				FBXExportEmbeddedTextures -v false;
				FBXExportHardEdges -v $exportNormals;
				
				if (`exists FBXExportTangents`)
				{
					FBXExportTangents -v $exportTangents;
				}
								
				FBXExportApplyConstantKeyReducer -v false;
				// Disable constraints
				if (`exists FBXExportConstraint`)
				{
					FBXExportConstraint -v false;
				}
				if (`exists FBXExportConstraints`)
				{
					FBXExportConstraints -v false;
				}

				// Set up all the new export settings to sane values!
				if (`exists FBXExportUpAxis`)
				{
					FBXExportUpAxis Y;
				}
				if (`exists FBXExportAxisConversionMethod`)
				{
					FBXExportAxisConversionMethod convertAnimation;
				}
				if (`exists FBXExportConvertUnitString`) // This command should take precedence over the following
				{
					FBXExportConvertUnitString cm;
				}
				else if (`exists FBXConvertUnitString`) // This command is deprecated in 2011
				{
					FBXConvertUnitString cm;
				}
				if (`exists FBXExportScaleFactor`)
				{
					FBXExportScaleFactor 1;
				}				

				FBXExportShowUI -v false;

				print "Before fbx export\n";
				FBXExport -f $fbxFilename;
				print "after fbx export\n";
			}
			else
				print "Could not open Maya file.";

			sysFile -delete "!//UNITY_TEMP//!/CommandPipe";

			$donepipe = `fopen "!//UNITY_TEMP//!/SyncPipe" "w"`;
			fwrite $donepipe "Done";
			fclose $donepipe;
			print "Finished maya loading and fbx conversion \n";
		}
	}
}

#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\FBXMayaExport.mel---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\FBXMayaExport5.mel---------------


print "Starting maya loading and fbx conversion \n";
	
if (`file -q -exists "!//UNITY_MB_FILE//!"`)
{		
	file -o "!//UNITY_MB_FILE//!";
	FBXExportEmbeddedTextures -v false;
	// FBXExportHardEdges -v true;
	FBXExportApplyConstantKeyReducer -v false;
	FBXExportShowUI -v false;

	print "Before fbx export\n";
	FBXExport -f "!//UNITY_TEMP//!/ExportedFBXFile.fbx";
	print "after fbx export\n";
}
else
	print "Could not open Maya file.";

sysFile -delete "!//UNITY_TEMP//!/CommandPipe";

$donepipeKill = `fopen "!//UNITY_TEMP//!/SyncPipeKill" "w"`;
fwrite $donepipeKill "Done";
fclose $donepipeKill;


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\FBXMayaExport5.mel---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\FBXMayaMain.mel---------------


// Unload any plugins before exporting so we don't end up with the wrong version of fbx.
// unloadPlugin `pluginInfo -query -listPlugins`;

// We need to do the plugin loading in another script because
// the plugin needs to be loaded before we execute the script! (Names arent't found otherwise)
if (getApplicationVersionAsFloat() >= 8.0)
{
	// Use unqualified path to support Maya 2013, 2015 and future version
	loadPlugin "fbxmaya";
	eval ("source \"!//UNITY_TEMP//!/FBXMayaExport.mel\"");
}
else if (getApplicationVersionAsFloat() >= 7.0)
{
	loadPlugin "!//MAYA_PATH//!/Contents/MacOS/plug-ins/fbxmaya.lib";
	eval ("source \"!//UNITY_TEMP//!/FBXMayaExport.mel\"");
}
else if (getApplicationVersionAsFloat() >= 6.5)
{
	loadPlugin "!//UNITY_APP//!/Contents/Tools/fbxmaya65.lib";
	eval ("source \"!//UNITY_TEMP//!/FBXMayaExport.mel\"");
}
else if (getApplicationVersionAsFloat() >= 6.0) 
{
	loadPlugin "!//UNITY_APP//!/Contents/Tools/fbxmaya60.lib";
	eval ("source \"!//UNITY_TEMP//!/FBXMayaExport.mel\"");
}
else if (getApplicationVersionAsFloat() >= 5.0)
{
	loadPlugin "!//UNITY_APP//!/Contents/Tools/fbxmaya50.lib";
	eval ("source \"!//UNITY_TEMP//!/FBXMayaExport5.mel\"");
}
else if (getApplicationVersionAsFloat() >= 4.5) 
{
	loadPlugin "!//UNITY_APP//!/Contents/Tools/fbxmaya45.lib";
	eval ("source \"!//UNITY_TEMP//!/FBXMayaExport5.mel\"");
}

#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\FBXMayaMain.mel---------------


