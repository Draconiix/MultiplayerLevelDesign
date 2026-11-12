
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using AllIn1Assets.SharedExtensions;

#if ALLIN1_URP
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if !UNITY_6000_0_OR_NEWER
using UnityEngine.Experimental.Rendering.Universal;
#endif
using System.Reflection;
#endif

namespace AllIn1Assets.AllIn13DShader
{
	public class AllIn13DShaderConfig : Plugin3DConfig
	{
		private const string PLUGIN_NAME = "AllIn13DShader";
		private const string VERSION = "2.8";

		private const string OUTLINE_PASS_NAME = "OutlinePass";
		private const string OUTLINE_OPAQUE_RENDER_FEATURE_NAME = "Render Feature - Outline Opaque";
		private const string OUTLINE_TRANSPARENT_RENDER_FEATURE_NAME = "Render Feature - Outline Transparent";

		private const string FOLDER_NAME_STANDARD_EXAMPLES = "StandardExamples";

		private const PluginID PLUGIN_ID = PluginID.ALL_IN_1_3D_SHADER;

		private static string[] SHADER_NAMES =
		{
			"AllIn13DShader",
			"AllIn13DShader_NoShadowCaster",
			"AllIn13DShaderOutline",
			"AllIn13DShaderOutline_NoShadowCaster"
		};

		private static string[] SHADER_TEMPLATES =
		{
			"AllIn13DShader_NoShadowCaster_Template",
			"AllIn13DShaderOutline_NoShadowCaster_Template",
			"AllIn13DShaderOutline_Template",
		};

		#region FOLDER PATHS

		private const string FOLDER_NAME_BAKED_SHADERS_DEMO = "Baked Shaders Demo";

		public string DemoShadersBakedFolderPath
		{
			get
			{
				string res = EditorUtils.CombineAssetPath(DemoFolderPath, FOLDER_NAME_BAKED_SHADERS_DEMO);
				return res;
			}
		}

		public string DemoMaterialsFolderPath => EditorUtils.CombineAssetPath(DemoFolderPath, GlobalConstants.FOLDER_NAME_MATERIALS, FOLDER_NAME_STANDARD_EXAMPLES);

		#endregion

		//Runs from the asset postprocessor, which also fires during a player build, so it cannot throw
		public void CheckBakedShadersFolder(string folderPath, PropertiesConfig propertiesConfig)
		{
			string normalizedFolderPath = EditorUtils.NormalizeAssetPath(folderPath);

			try
			{
				//Only a scan, so never create anything here. Creating assets from the postprocessor
				//chain is unsupported, and a missing folder simply means there is nothing to find
				if (!EditorUtils.AssetFolderExists(normalizedFolderPath))
				{
					return;
				}

				string[] files = Directory.GetFiles(normalizedFolderPath, "*.shader", SearchOption.AllDirectories);

				for (int i = 0; i < files.Length; i++)
				{
					//Directory.GetFiles returns platform separators, AssetDatabase wants forward slashes
					string filePath = EditorUtils.NormalizeAssetPath(files[i]);

					Shader shader = (Shader)AssetDatabase.LoadAssetAtPath(filePath, typeof(Shader));
					if (shader == null)
					{
						continue;
					}

					EffectsProfile effectProfile = effectsProfileCollection.FindEffectProfileByShader(shader);

					if (effectProfile == null)
					{
						EffectProfileCreatorFromShaderVariant.Create(shader, filePath, propertiesConfig, effectsProfileCollection);
					}
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[AllIn1] Could not scan the baked shaders folder '{normalizedFolderPath}'. " +
					$"Set a valid folder in the Save Paths tab of the plugin window. Details: {e.Message}");
			}
		}

		public void CheckBakedShadersFolder(PropertiesConfig propertiesConfig)
		{
			string shaderVariantsFolderPath = ShaderVariantCreator.GetShaderVariantsFolderPath(this);
			CheckBakedShadersFolder(shaderVariantsFolderPath, propertiesConfig);

			//The demo folder is optional, it is stripped in some installs, so it is not created on demand
			if (EditorUtils.AssetFolderExists(DemoShadersBakedFolderPath))
			{
				CheckBakedShadersFolder(DemoShadersBakedFolderPath, propertiesConfig);
			}
		}

		protected override void RefreshShadersReferences()
		{
			shStandard = Shader.Find("AllIn13DShader/" + SHADER_NAMES[0]);
			shStandardNoShadowCaster = Shader.Find("AllIn13DShader/" + SHADER_NAMES[1]);
			shOutline = Shader.Find("AllIn13DShader/" + SHADER_NAMES[2]);
			shOutlineNoShadowCaster = Shader.Find("AllIn13DShader/" + SHADER_NAMES[3]);

			AllIn1Logger.LogWarning($"RefreshShadersReferences(AllIn13DShaderConfig) # shStandard: {shStandard}");
		}

		public override string[] GetShaderNames()
		{
			return SHADER_NAMES;
		}

		public override string[] GetShaderTemplates()
		{
			return SHADER_TEMPLATES;
		}

		protected override void AdditionalActionsOverEffectsProfiles(PropertiesConfig propertiesConfig)
		{
			base.AdditionalActionsOverEffectsProfiles(propertiesConfig);

			CheckBakedShadersFolder(propertiesConfig);
		}

		public override string GetVersion()
		{
			return VERSION;
		}

		public override string GetPluginName()
		{
			return PLUGIN_NAME;
		}

		public override PluginID GetPluginID()
		{
			return PLUGIN_ID;
		}

		public override PropertiesConfigCollection CreatePropertiesConfigSO()
		{
			PropertiesConfigCollection res = ScriptableObject.CreateInstance<AllIn13DShaderPropertiesConfigSO>();
			return res;
		}

		public override Shader GetShaderAfterMaterialRemoval()
		{
			Shader res = Shader.Find(ConstantsRuntime.STANDARD_SHADER_NAME);
			return res;
		}

		public override bool IsWritableFolder(string folderPath)
		{
			return false;
		}

		public override bool IsDerivedFromAllIn13DShader()
		{
			return true;
		}

		public override string GetRenderPipelineDocURL()
		{
			return $"{GlobalConstants.URL_DOCS_SITE}/3d-shader/urp-and-post-processing-setup";
		}

#if ALLIN1_URP
		public override void ConfigureRenderPipeline(UniversalRendererData universalRendererData, RenderPipelineAsset renderPipelineAsset)
		{
			ConvertMaterialsFolder();
			ConfigureRenderPipeline(renderPipelineAsset);
		}

		public override void ConvertMaterialsFolder()
		{
			string dirPath = DemoMaterialsFolderPath;

			StandardUpgrader standardUpgrader = new StandardUpgrader("Standard");
			DirectoryInfo dir = new DirectoryInfo(dirPath);

			List<string> materialsPathsToConvert = new List<string>();
			FileInfo[] files = dir.GetFiles("*.mat");
			for (int i = 0; i < files.Length; i++)
			{
				string materialPath = EditorUtils.CombineAssetPath(dirPath, files[i].Name);
				materialsPathsToConvert.Add(materialPath);
			}

			for (int i = 0; i < materialsPathsToConvert.Count; i++)
			{
				Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialsPathsToConvert[i]);

				standardUpgrader.Upgrade(mat, MaterialUpgrader.UpgradeFlags.None);
			}
		}


		public static void ConfigureRenderPipeline(RenderPipelineAsset renderPipelineAsset)
		{
			UniversalRenderPipelineAsset pipeline = renderPipelineAsset as UniversalRenderPipelineAsset;
			ScriptableRenderer scriptableRenderer = pipeline.GetRenderer(0);

			FieldInfo rendererDataListFieldInfo = GetUrpPrivateField(typeof(UniversalRenderPipelineAsset), "m_RendererDataList");
			if (rendererDataListFieldInfo == null) return;
			ScriptableRendererData[] scriptableRendererDatas = rendererDataListFieldInfo.GetValue(pipeline) as ScriptableRendererData[];

			UniversalRendererData universalRendererData = scriptableRendererDatas[0] as UniversalRendererData;

			if (universalRendererData != null)
			{
				Undo.RecordObject(universalRendererData, "");

				universalRendererData.depthPrimingMode = DepthPrimingMode.Disabled;


				CreateRenderFeatureOutline(universalRendererData: universalRendererData, renderFeatureName: OUTLINE_OPAQUE_RENDER_FEATURE_NAME,
					renderQueueType: RenderQueueType.Opaque, renderPassEvent: RenderPassEvent.AfterRenderingOpaques);

				CreateRenderFeatureOutline(universalRendererData: universalRendererData, renderFeatureName: OUTLINE_TRANSPARENT_RENDER_FEATURE_NAME,
					renderQueueType: RenderQueueType.Transparent, renderPassEvent: RenderPassEvent.AfterRenderingTransparents);

				EditorUtility.SetDirty(universalRendererData);
				EditorUtility.SetDirty(pipeline);
				AssetDatabase.SaveAssets();
			}
		}

		private static void CreateRenderFeatureOutline(UniversalRendererData universalRendererData,
			string renderFeatureName, RenderQueueType renderQueueType, RenderPassEvent renderPassEvent)
		{
			RenderObjects outlineRenderFeature = null;
			bool outlineRenderFeatureFound = false;

			List<ScriptableRendererFeature> rendererFeatures = universalRendererData.rendererFeatures;
			foreach (ScriptableRendererFeature scriptableRendererFeature in rendererFeatures)
			{
				if (scriptableRendererFeature is RenderObjects)
				{
					RenderObjects renderObjects = (RenderObjects)scriptableRendererFeature;
					string[] passNames = renderObjects.settings.filterSettings.PassNames;

					for (int i = 0; i < passNames.Length; i++)
					{
						if (passNames[i] == OUTLINE_PASS_NAME && renderObjects.settings.filterSettings.RenderQueueType == renderQueueType)
						{
							outlineRenderFeature = renderObjects;
							outlineRenderFeatureFound = true;
						}
					}

					if (outlineRenderFeatureFound)
					{
						break;
					}
				}
			}

			if (!outlineRenderFeatureFound)
			{
				outlineRenderFeature = RenderObjects.CreateInstance<RenderObjects>();
				outlineRenderFeature.name = renderFeatureName;

				rendererFeatures.Add(outlineRenderFeature);

				FieldInfo fieldInfoRenderFeaturesMap = GetUrpPrivateField(typeof(UniversalRendererData), "m_RendererFeatureMap");
				if (fieldInfoRenderFeaturesMap == null) return;
				List<long> renderFeaturesMapList = fieldInfoRenderFeaturesMap.GetValue(universalRendererData) as List<long>;

				string guid;
				long localID;
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(outlineRenderFeature, out guid, out localID);
				renderFeaturesMapList.Add(localID);

				AssetDatabase.AddObjectToAsset(outlineRenderFeature, universalRendererData);
				EditorUtility.SetDirty(outlineRenderFeature);
			}

			outlineRenderFeature.name = renderFeatureName;
			outlineRenderFeature.settings.filterSettings.RenderQueueType = renderQueueType;
			outlineRenderFeature.settings.filterSettings.LayerMask = ~0;
			outlineRenderFeature.settings.Event = renderPassEvent;
			outlineRenderFeature.settings.filterSettings.PassNames = new string[] { OUTLINE_PASS_NAME };
			outlineRenderFeature.SetActive(true);



			EditorUtility.SetDirty(outlineRenderFeature);
			EditorUtility.SetDirty(universalRendererData);

			FieldInfo fieldInfoRendererFeatures = GetUrpPrivateField(typeof(UniversalRendererData), "m_RendererFeatures");
			if (fieldInfoRendererFeatures == null) return;
			fieldInfoRendererFeatures.SetValue(universalRendererData, rendererFeatures);
		}

		//URP has no public API for adding renderer features, so a URP upgrade can rename these and silently break the outline setup
		public static FieldInfo GetUrpPrivateField(System.Type type, string fieldName)
		{
			FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (fieldInfo == null)
			{
				Debug.LogError($"AllIn13DShader: URP field {type.Name}.{fieldName} was not found in this URP version, outline render features could not be configured. Add two RenderObjects features for the {OUTLINE_PASS_NAME} pass manually");
			}
			return fieldInfo;
		}
#endif
	}
}