using UnityEditor;
using UnityEngine;

namespace AllIn13DShader
{ 
	public class EditorEventManager : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
		{
#if ALLIN13DSHADER_URP
			URPConfigurator.CheckURPRemoved(deletedAssets, didDomainReload);
#endif
			GlobalConfiguration.InitIfNeeded();
			PropertiesConfigCollection propertiesConfigCollection = PropertiesConfigCreator.InitIfNeeded(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, didDomainReload);
			//GlobalConfiguration.SetPropertiesConfigCollection(propertiesConfigCollection);
			GlobalConfiguration.SetupShadersReferences(); 

#if ALLIN13DSHADER_URP
			URPConfigurator.AllAssetProcessed();
			AllIn13DShaderWindow.AllAssetProcessed();  
#endif
		}
	}
} 