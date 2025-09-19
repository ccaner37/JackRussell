using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AllIn13DShader
{
	public static class EffectPropertyDrawer
	{
		public static void DrawMainProperty(int globalEffectIndex, AllIn13DEffectConfig effectConfig, AllIn13DShaderInspectorReferences references)
		{
			EditorGUILayout.BeginHorizontal();

			string label = $"{globalEffectIndex}. {effectConfig.displayName}";
			
			switch (effectConfig.effectConfigType)
			{
				case EffectConfigType.EFFECT_TOGGLE:
					DrawMainPropertyToggle(label, effectConfig, references);
					break;
				case EffectConfigType.EFFECT_ENUM:
					DrawMainPropertyEnum(label, effectConfig, references);
					break;
			}

			EditorGUILayout.EndHorizontal();
		}

		public static void DrawMainPropertyToggle(string label, AllIn13DEffectConfig effectConfig, AllIn13DShaderInspectorReferences references)
		{
			bool isEffectEnabled = AllIn13DEffectConfig.IsEffectEnabled(effectConfig, references);
			
			EditorGUI.BeginChangeCheck();

			string tooltip = effectConfig.keywords[0].keyword + " (C#)";
			GUIContent guiContent = new GUIContent(label, tooltip);

			bool effectEnabledInAllMaterials = true;
			bool effectDisabledInAllMaterials = true;
			for (int i = 0; i < references.targetMatInfos.Length; i++)
			{
				bool effectEnabledThisMat = AllIn13DEffectConfig.IsEffectEnabled(effectConfig, references.targetMatInfos[i]);
				
				effectEnabledInAllMaterials = effectEnabledInAllMaterials && effectEnabledThisMat;
				effectDisabledInAllMaterials = effectDisabledInAllMaterials && !effectEnabledThisMat;
			}

			bool mixedValue = (!(effectEnabledInAllMaterials || effectDisabledInAllMaterials)) && references.targetMatInfos.Length > 1;
			string style = mixedValue ? "ToggleMixed" : "Toggle";

			isEffectEnabled = GUILayout.Toggle(isEffectEnabled, guiContent, style);
			if (EditorGUI.EndChangeCheck())
			{
				for(int i = 0; i < references.targetMatInfos.Length; i++)
				{
					MaterialInfo matInfo = references.targetMatInfos[i];

					if (isEffectEnabled)
					{
						AllIn13DEffectConfig.EnableEffect(effectConfig, references, matInfo);
					}
					else
					{
						AllIn13DEffectConfig.DisableEffect(effectConfig, matInfo);
					}
				}

				references.matProperties[effectConfig.keywordPropertyIndex].floatValue = isEffectEnabled ? 1f : 0f;

				EditorUtils.SetDirtyCurrentScene();

				references.SetMaterialsDirty();
			}
		}

		public static void DrawMainPropertyEnum(string label, AllIn13DEffectConfig effectConfig, AllIn13DShaderInspectorReferences references)
		{
			int selectedIndex = 0;
			//bool isEffectEnabled = AllIn13DEffectConfig.IsEffectEnabled(effectConfig, ref selectedIndex, references);


			bool sameEnumValueInAllMaterials = true;
			for(int i = 0; i < references.targetMatInfos.Length; i++)
			{
				int enumIdx = -1;
				MaterialInfo matInfo = references.targetMatInfos[i];
				AllIn13DEffectConfig.IsEffectEnabled(effectConfig, ref enumIdx, matInfo);

				if(i == 0)
				{
					selectedIndex = enumIdx;
				}
				else
				{
					sameEnumValueInAllMaterials = sameEnumValueInAllMaterials && (enumIdx == selectedIndex);
				}
			}

			

			EditorGUI.BeginChangeCheck();

			string tooltip = effectConfig.keywords[selectedIndex].keyword + " (C#)";
			GUIContent guiContent = new GUIContent(label, tooltip);
			
			EditorGUI.showMixedValue = !sameEnumValueInAllMaterials;
			selectedIndex = EditorGUILayout.Popup(guiContent, selectedIndex, effectConfig.keywordsDisplayNames);
			EditorGUI.showMixedValue = false;

			if (EditorGUI.EndChangeCheck())
			{
				for (int i = 0; i < references.targetMatInfos.Length; i++)
				{
					MaterialInfo matInfo = references.targetMatInfos[i];

					if (selectedIndex >= 0)
					{
						AllIn13DEffectConfig.EnableEffectByIndex(effectConfig, selectedIndex, references, matInfo);
					}
					else
					{
						AllIn13DEffectConfig.DisableEffect(effectConfig, matInfo);
					}
				}

				references.matProperties[effectConfig.keywordPropertyIndex].floatValue = selectedIndex;

				EditorUtils.SetDirtyCurrentScene();

				references.SetMaterialsDirty();
			}
		}

		public static void DrawProperty(int propertyIndex, string labelPrefix, bool allowReset, bool isKeywordProperty, AllIn13DShaderInspectorReferences references)
		{
			MaterialProperty targetProperty = references.matProperties[propertyIndex];
			DrawProperty(targetProperty, labelPrefix, allowReset, isKeywordProperty, references);
		}

		public static void DrawProperty(EffectProperty effectProperty, string labelPrefix, bool allowReset, AllIn13DShaderInspectorReferences references)
		{
			DrawProperty(effectProperty.propertyIndex, labelPrefix, effectProperty.allowReset, effectProperty.IsPropertyWithKeywords(), references);
		}

		public static void DrawProperty(EffectProperty effectProperty, AllIn13DShaderInspectorReferences references)
		{
			DrawProperty(propertyIndex: effectProperty.propertyIndex, isKeywordProperty: effectProperty.IsPropertyWithKeywords(),  references: references);
		}

		public static void DrawProperty(int propertyIndex, bool isKeywordProperty, AllIn13DShaderInspectorReferences references)
		{
			DrawProperty(propertyIndex, string.Empty, true, isKeywordProperty, references);
		}

		public static void DrawProperty(MaterialProperty materialProperty, AllIn13DShaderInspectorReferences references)
		{
			DrawProperty(materialProperty, false, references);
		}

		public static void DrawProperty(MaterialProperty materialProperty, bool isKeywordProperty, AllIn13DShaderInspectorReferences references)
		{
			DrawProperty(materialProperty, string.Empty, true, isKeywordProperty, references);
		}

		public static void DrawProperty(MaterialProperty materialProperty, bool allowReset, bool isKeywordProperty, AllIn13DShaderInspectorReferences references)
		{
			DrawProperty(materialProperty, string.Empty, allowReset, isKeywordProperty, references);
		}

		public static void DrawProperty(MaterialProperty materialProperty, string labelPrefix, bool allowReset, bool isKeywordProperty, 
			AllIn13DShaderInspectorReferences references)
		{
			DrawProperty(
				materialProperty: materialProperty, 
				labelPrefix: labelPrefix, 
				displayName: materialProperty.displayName,
				allowReset: allowReset, 
				isKeywordProperty: isKeywordProperty,
				references: references);
		}

		public static void DrawProperty(MaterialProperty materialProperty, string labelPrefix, string displayName, bool allowReset, bool isKeywordProperty, 
			AllIn13DShaderInspectorReferences references)
		{
			string label = $"{labelPrefix} {displayName}";
			string tooltip = materialProperty.name + "(C#)";


			EditorGUILayout.BeginHorizontal();

			DrawProperty(materialProperty, label, tooltip, isKeywordProperty, references);
			if (allowReset)
			{
				DrawResetButton(materialProperty, references);
			}

			EditorGUILayout.EndHorizontal();
		}

		public static void DrawProperty(MaterialProperty targetProperty, string label, string tooltip, bool isKeywordProperty, 
			AllIn13DShaderInspectorReferences references)
		{
			GUIContent propertyLabel = new GUIContent();
			propertyLabel.text = label;
			propertyLabel.tooltip = tooltip;

			EditorGUI.BeginChangeCheck();
			references.editorMat.ShaderProperty(targetProperty, propertyLabel);
			if (EditorGUI.EndChangeCheck())
			{
				if (isKeywordProperty)
				{
					references.RefreshMaterialKeywords();
				}
			}
		}

		public static void DrawResetButton(MaterialProperty targetProperty, AllIn13DShaderInspectorReferences references)
		{
			GUIContent resetButtonLabel = new GUIContent();
			resetButtonLabel.text = "R";
			resetButtonLabel.tooltip = "Resets to default value";
			if (GUILayout.Button(resetButtonLabel, GUILayout.Width(20)))
			{
				for (int i = 0; i < references.targetMatInfos.Length; i++)
				{
					MaterialInfo matInfo = references.targetMatInfos[i];
					AllIn13DEffectConfig.ResetProperty(targetProperty, references, matInfo);
				}
			}
		}
	}
}