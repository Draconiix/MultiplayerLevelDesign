using AllIn1Assets.SharedExtensions;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AllIn1Assets.AllIn13DShader
{
	[CustomEditor(typeof(AllIn13DShaderComponent))]
	[CanEditMultipleObjects]
	public class AllIn13DShaderComponentCustomEditor : PluginComponentCustomEditor
	{
		protected override void DisplayNotValidComponentsDialog()
		{

		}

		//Only auto replace Unity default, built in, package and Standard shader materials. Any other custom material is adopted as is so it's never clobbered
		protected override bool ShouldAutoReplaceMaterial(Material currMaterial)
		{
			if (currMaterial == null) { return true; }
			if (pluginConfigGeneric.IsMaterialFromThisPlugin(currMaterial)) { return false; }

			bool isDefaultMaterial = currMaterial.name.Contains("Default");
			bool isStandardShader = currMaterial.shader != null && currMaterial.shader.name.Contains("Standard");

			//URP's defaults (Lit, ParticlesUnlit) are package materials with no "Default" in their names
			bool isBuiltInOrPackageMaterial = EditorUtils.IsAssetOutsideAssetsFolder(currMaterial);

			return isDefaultMaterial || isStandardShader || isBuiltInOrPackageMaterial;
		}

		//Every effect property back to 0 as in 2.74, with the keywords the material inspector would set for that value
		protected override void DeactivateAllEffects(Material mat)
		{
			AllIn13DShaderConfig config = (AllIn13DShaderConfig)pluginConfigGeneric;
			if (!config.IsMaterialFromThisPlugin(mat)) { return; }

			config.EnsureCollections();
			if (config.propertiesConfigCollection == null) { return; }

			//A baked variant has its keywords compiled in, there is nothing left to turn off
			AbstractMaterialInfo matInfo = AbstractMaterialInfo.CreateInstance(mat, config);
			if (matInfo.IsShaderVariant()) { return; }

			PropertiesConfig propertiesConfig = config.propertiesConfigCollection.FindPropertiesConfigByShader(mat.shader);
			if (propertiesConfig == null) { return; }

			List<AllIn13DEffectConfig> effects = propertiesConfig.GetAllEffects();
			for (int i = 0; i < effects.Count; i++)
			{
				AllIn13DEffectConfig effectConfig = effects[i];

				AllIn13DEffectConfig.DisableEffect(effectConfig, matInfo);

				//0 is the first option of an enum effect (None for Outline, which picks the shader below), so its keyword comes back on
				if (effectConfig.keywords.Count > 1)
				{
					matInfo.EnableKeyword(effectConfig.keywords[0].keyword);
				}

				if (mat.HasProperty(effectConfig.keywordPropertyName))
				{
					mat.SetFloat(effectConfig.keywordPropertyName, 0f);
				}
			}

			MaterialUtils.CheckMaterialShader(mat, config);
		}

		protected override void DrawExtraButtons()
		{
			if (GUILayout.Button("Apply Material To All Children"))
			{
				ExecuteActionAfterCheck(ApplyMaterialToAllChildren);
			}

			if (GUILayout.Button("Render Material To Image"))
			{
				ExecuteActionAfterCheck(RenderMaterialToImage);
			}
		}

		protected override void DrawExtraSections()
		{

		}

		private void ApplyMaterialToAllChildren()
		{
			for (int i = 0; i < targets.Length; i++)
			{
				AllIn13DShaderComponent allIn13DShaderComponent = (AllIn13DShaderComponent)targets[i];
				allIn13DShaderComponent.ApplyMaterialToChildren();

				EditorUtility.SetDirty(allIn13DShaderComponent);
			}

			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		}

		private void RenderMaterialToImage()
		{
			for (int i = 0; i < targets.Length; i++)
			{
				AllIn13DShaderComponent allIn13DShaderComponent = (AllIn13DShaderComponent)targets[i];
				RenderToImage(allIn13DShaderComponent);

				EditorUtility.SetDirty(allIn13DShaderComponent);
			}
		}

		public void RenderToImage(AllIn13DShaderComponent allIn13DShaderComponent)
		{
			AllIn13DShaderConfig config = (AllIn13DShaderConfig)pluginConfigGeneric;
			Texture tex = allIn13DShaderComponent.currMaterial.GetTexture("_MainTex");
			if (tex != null)
			{
				string folderPath = config.RenderImageSavePath;
				string fileName = allIn13DShaderComponent.gameObject.name + ".png";
				RenderMaterialToImageTool.RenderAndSaveTexture(allIn13DShaderComponent.currMaterial, tex, 4.0f, folderPath, fileName, config.GetPluginName());
			}
			else
			{
				EditorUtility.DisplayDialog("No valid target texture found",
					   "All In 1 3DShader component couldn't find a valid Main Texture in this GameObject (" +
					   allIn13DShaderComponent.gameObject.name +
					   "). This means that the material you are using has no Main Texture or that the texture couldn't be reached through the Renderer component you are using." +
					   " Please make sure to have a valid Main Texture in the Material", "Ok");
			}
		}

		protected override PluginID GetPluginID()
		{
			return PluginID.ALL_IN_1_3D_SHADER;
		}
	}
}