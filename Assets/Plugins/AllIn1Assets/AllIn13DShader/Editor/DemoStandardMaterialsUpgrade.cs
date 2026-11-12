#if ALLIN1_URP
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AllIn1Assets.AllIn13DShader
{
	//The URP prompt converts these once, so an update that reimports them as Standard would leave them magenta.
	//Checked on every load instead, outside the config so no config is resolved at load
	public static class DemoStandardMaterialsUpgrade
	{
		//The Standard side of the demo's comparisons (Ellen, the knight and the skeleton), by GUID so finding them needs no config asset
		private static readonly string[] DEMO_STANDARD_MATERIAL_GUIDS =
		{
			"acdd86fe5859b874a9773404a741bc7e",
			"c08d5d79fff7e33418756220998c0682",
			"542617c268ce9584aa917cc7262f17d9",
			"2d719f2e7a48ccb409b3c3fec9d2a523",
			"2086fc2dcc2351246bf617f5c4fe54df",
			"e14f352c2f3d9a6429850d7ba4393b7a",
			"ecc0eb9577a09ca4090a071c5d6cf54c",
			"0ac4382594de48a468a669194f5d1e8f",
		};

		[InitializeOnLoadMethod]
		private static void ScheduleUpgrade()
		{
			if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary) { return; }

			EditorApplication.delayCall += UpgradeDemoStandardMaterials;
		}

		private static void UpgradeDemoStandardMaterials()
		{
			//A package import reloads the domain before its materials arrive
			if (EditorApplication.isCompiling || EditorApplication.isUpdating)
			{
				EditorApplication.delayCall += UpgradeDemoStandardMaterials;
				return;
			}

			if (BuildPipeline.isBuildingPlayer) { return; }
			if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) { return; }

			StandardUpgrader standardUpgrader = null;
			for (int i = 0; i < DEMO_STANDARD_MATERIAL_GUIDS.Length; i++)
			{
				string materialPath = AssetDatabase.GUIDToAssetPath(DEMO_STANDARD_MATERIAL_GUIDS[i]);
				if (string.IsNullOrEmpty(materialPath)) { continue; }

				Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
				if (mat == null || mat.shader == null || mat.shader.name != "Standard") { continue; }

				if (standardUpgrader == null) { standardUpgrader = new StandardUpgrader("Standard"); }

				standardUpgrader.Upgrade(mat, MaterialUpgrader.UpgradeFlags.None);
				EditorUtility.SetDirty(mat);
				AssetDatabase.SaveAssetIfDirty(mat);
			}
		}
	}
}
#endif
