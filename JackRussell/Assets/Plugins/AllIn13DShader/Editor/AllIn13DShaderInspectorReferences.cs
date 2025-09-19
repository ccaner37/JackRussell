using UnityEditor;
using UnityEngine;

namespace AllIn13DShader
{
	public class AllIn13DShaderInspectorReferences
	{
		public MaterialProperty[] matProperties;
		public string[] oldKeyWords;

		//public Material[] targetMats;
		public MaterialInfo[] targetMatInfos;

		public Material materialWithDefaultValues;

		public MaterialEditor editorMat;

		//Styles
		private const int bigFontSize = 16, smallFontSize = 11;
		public GUIStyle propertiesStyle, bigLabelStyle, smallLabelStyle, toggleButtonStyle, tabButtonStyle;

		//Outline Effect
		public AllIn13DEffectConfig outlineEffectConfig;

		//Cast Shadows Effect
		public AllIn13DEffectConfig castShadowsEffectConfig;



		public AllIn13DShaderInspectorReferences()
		{
			propertiesStyle = new GUIStyle(EditorStyles.helpBox);
			propertiesStyle.margin = new RectOffset(0, 0, 0, 0);

			bigLabelStyle = new GUIStyle(EditorStyles.boldLabel);
			bigLabelStyle.fontSize = bigFontSize;

			smallLabelStyle = new GUIStyle(EditorStyles.boldLabel);
			smallLabelStyle.fontSize = smallFontSize;

			toggleButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, richText = true };

			tabButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 10 };
		}

		public void Setup(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			this.editorMat = materialEditor;

			if (this.targetMatInfos == null)
			{
				this.targetMatInfos = new MaterialInfo[materialEditor.targets.Length];
				for (int i = 0; i < materialEditor.targets.Length; i++)
				{
					Material mat = (Material)materialEditor.targets[i];
					targetMatInfos[i] = new MaterialInfo(mat);
				}

				materialWithDefaultValues = new Material(targetMatInfos[0].mat.shader);
			}

			this.matProperties = properties;
		}

		public void SetOutlineEffect(PropertiesConfig propertiesConfig)
		{
			this.outlineEffectConfig = propertiesConfig.FindEffectConfigByID("OUTLINETYPE");
		}

		public void SetCastShadowsEffect(PropertiesConfig propertiesConfig)
		{
			this.castShadowsEffectConfig = propertiesConfig.FindEffectConfigByID("CAST_SHADOWS_ON");
		}

		public void SetMaterialsDirty()
		{
			for(int i = 0; i < targetMatInfos.Length; i++)
			{
				EditorUtility.SetDirty(targetMatInfos[i].mat);
			}
		}

		public Shader GetShader()
		{
			return targetMatInfos[0].mat.shader;
		}

		public bool IsKeywordEnabled(string keyword)
		{
			bool res = true;

			for(int i = 0; i < targetMatInfos.Length; i++)
			{
				res = res && targetMatInfos[i].IsKeywordEnabled(keyword);
			}

			return res;
		}

		public void RefreshMaterialKeywords()
		{
			for(int i = 0; i < this.targetMatInfos.Length; i++)
			{
				this.targetMatInfos[i].RefreshKeywords();
			}
		}
	}
}