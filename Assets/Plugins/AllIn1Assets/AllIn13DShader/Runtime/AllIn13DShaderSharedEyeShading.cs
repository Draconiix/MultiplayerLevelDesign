using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AllIn1Assets.AllIn13DShader
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("AllIn13DShader/VR Shared Eye Shading")]
	public class AllIn13DShaderSharedEyeShading : MonoBehaviour
	{
		[Tooltip("Both eyes shade specular, reflections, Fresnel, Rim and Highlights from one point between them, so they see the same result. Off: each eye shades from its own position.")]
		public bool shareShadingBetweenEyes = true;

		private static readonly int SharedEyePositionID = Shader.PropertyToID("global_sharedEyePosition");
		private static readonly List<AllIn13DShaderSharedEyeShading> activeInstances = new List<AllIn13DShaderSharedEyeShading>();
		private static readonly List<Camera> renderingCameras = new List<Camera>();

		private Camera cam;

		private void OnEnable()
		{
			cam = GetComponent<Camera>();

			if (activeInstances.Count == 0)
			{
				RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
				RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
				Camera.onPreRender += BeginCamera;
				Camera.onPostRender += EndCamera;
			}
			activeInstances.Add(this);
		}

		private void OnDisable()
		{
			activeInstances.Remove(this);

			if (activeInstances.Count == 0)
			{
				RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
				RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
				Camera.onPreRender -= BeginCamera;
				Camera.onPostRender -= EndCamera;
				renderingCameras.Clear();
				Shader.SetGlobalVector(SharedEyePositionID, Vector4.zero);
			}
		}

		private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
		{
			BeginCamera(renderingCamera);
		}

		private static void OnEndCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
		{
			EndCamera(renderingCamera);
		}

		// A camera rendered inside another one (a mirror, a planar reflection) gets its own value, then the outer camera's comes back
		private static void BeginCamera(Camera renderingCamera)
		{
			renderingCameras.Add(renderingCamera);
			Apply(renderingCamera);
		}

		private static void EndCamera(Camera renderingCamera)
		{
			int index = renderingCameras.LastIndexOf(renderingCamera);
			if (index >= 0)
			{
				renderingCameras.RemoveAt(index);
			}

			Apply(renderingCameras.Count > 0 ? renderingCameras[renderingCameras.Count - 1] : null);
		}

		// Cameras without the component, like the scene view and probes, get zero: the regular per eye shading
		private static void Apply(Camera renderingCamera)
		{
			Vector4 sharedEyePosition = Vector4.zero;

			for (int i = 0; renderingCamera != null && i < activeInstances.Count; i++)
			{
				AllIn13DShaderSharedEyeShading instance = activeInstances[i];
				if (instance.cam == renderingCamera && instance.shareShadingBetweenEyes && !renderingCamera.orthographic)
				{
					Vector3 headPosition = renderingCamera.transform.position;
					sharedEyePosition = new Vector4(headPosition.x, headPosition.y, headPosition.z, 1f);
					break;
				}
			}

			Shader.SetGlobalVector(SharedEyePositionID, sharedEyePosition);
		}
	}
}
