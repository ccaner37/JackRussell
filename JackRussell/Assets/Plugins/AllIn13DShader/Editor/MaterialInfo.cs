using UnityEngine;
using UnityEngine.Rendering;

namespace AllIn13DShader
{
	public class MaterialInfo
	{
		public LocalKeyword[] enabledKeywords;
		public Material mat;

		public MaterialInfo(Material mat)
		{
			this.mat = mat;

			RefreshKeywords();
		}

		public void RefreshKeywords()
		{
			this.enabledKeywords = mat.enabledKeywords;
		}

		public bool IsKeywordEnabled(string keyword)
		{
			bool res = false;

			for (int i = 0; i < enabledKeywords.Length; i++)
			{
				if (enabledKeywords[i].name == keyword)
				{
					res = true;
					break;
				}
			}

			return res;
		}

		public void EnableKeyword(string keyword)
		{
			mat.EnableKeyword(keyword);

			RefreshKeywords();
		}

		public void DisableKeyword(string keyword)
		{
			mat.DisableKeyword(keyword);

			RefreshKeywords();
		}
	}
}