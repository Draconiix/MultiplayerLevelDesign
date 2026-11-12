#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System.IO;
using UnityEngine;

#if ALLIN1_BIRP && UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#elif ALLIN1_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace AllIn1Assets.AllIn13DShader
{
	[ExecuteInEditMode]
	public class PostProcessConfigurator : MonoBehaviour
	{
		private Camera cam;

#if ALLIN1_BIRP && UNITY_POST_PROCESSING_STACK_V2
		public PostProcessProfile postProcessProfileBIRP;
#elif ALLIN1_URP
		public VolumeProfile postProcessProfileURP;
#endif

		private void Awake()
		{
			cam = GetComponent<Camera>();

			if(cam == null)
			{
				Debug.LogError("Camera not found in this object!", gameObject);
				return;
			}

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
#if ALLIN1_BIRP && UNITY_POST_PROCESSING_STACK_V2
			ConfigurePostprocessBIRP();
#elif ALLIN1_URP
				ConfigurePostprocessURP();
#endif
			}
#endif
		}

#if UNITY_EDITOR
	#if ALLIN1_BIRP && UNITY_POST_PROCESSING_STACK_V2
		private void ConfigurePostprocessBIRP()
		{
			PostProcessLayer postProcessLayer;
			if (!cam.gameObject.TryGetComponent<PostProcessLayer>(out postProcessLayer))
			{
				postProcessLayer = cam.gameObject.AddComponent<PostProcessLayer>();
			}

			PostProcessVolume volume;
			if(!cam.gameObject.TryGetComponent<PostProcessVolume>(out volume))
			{
				volume = cam.gameObject.AddComponent<PostProcessVolume>();
			}
			
			postProcessLayer.volumeTrigger = cam.transform;

			if(postProcessProfileBIRP == null)
			{
				postProcessProfileBIRP = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(GetProfilePath("3DShaderPP.asset"));
			}

			postProcessLayer.volumeLayer = ~0;
			volume.profile = postProcessProfileBIRP;
			volume.isGlobal = true;

			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		}
#endif

#if ALLIN1_URP
		private void ConfigurePostprocessURP()
		{
			Volume volume;
			if (!cam.gameObject.TryGetComponent<Volume>(out volume))
			{
				volume = cam.gameObject.AddComponent<Volume>();
			}

			if(postProcessProfileURP == null)
			{
				postProcessProfileURP = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetProfilePath("3DShaderPP_URP.asset"));
			}

			volume.sharedProfile = (VolumeProfile)postProcessProfileURP;
			EditorUtility.SetDirty(volume);

			UniversalAdditionalCameraData cameraData;
			if(!cam.gameObject.TryGetComponent<UniversalAdditionalCameraData>(out cameraData))
			{
				cameraData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
			}
			cameraData.renderPostProcessing = true;

			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		}
#endif

		//Relative to this script (Demo/Scripts), so the profiles are found wherever the plugin folder sits. Runtime assembly, so EditorUtils is out of reach
		private string GetProfilePath(string profileFileName)
		{
			string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(this));
			if (string.IsNullOrEmpty(scriptPath)) { return string.Empty; }

			string demoFolder = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath));
			string res = Path.Combine(demoFolder, "PostProcessingProfile", profileFileName).Replace('\\', '/');
			return res;
		}
#endif
	}
}