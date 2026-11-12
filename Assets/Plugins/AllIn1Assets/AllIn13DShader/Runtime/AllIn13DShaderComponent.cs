using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AllIn1Assets.AllIn13DShader
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("AllIn13DShader/AddAllIn13DShader")]
	public class AllIn13DShaderComponent : PluginComponent
	{

		public Renderer currRenderer;
		public Material currMaterial
		{
			get
			{
				return currRenderer.sharedMaterial;
			}
			set
			{
				currRenderer.sharedMaterial = value;
			}
		}

		public override void AwakeImpl()
		{

		}

		public override Material GetCurrentMaterial()
		{
			return currMaterial;
		}

		protected override void OnEnableRuntime()
		{
			
		}

		protected override void OnDisableRuntime()
		{
			
		}

		public override void SetCurrentMaterial(Material mat)
		{
			currMaterial = mat;
		}

#if UNITY_EDITOR

		protected override void OnEnableEditor()
		{
			
		}

		protected override void OnDisableEditor()
		{
			
		}

		public override void ResetImpl()
		{

		}

		public override void InitMaterial(Material mat)
		{

		}

		public override void RemoveChildComponents()
		{

		}

		public void ApplyMaterialToChildren()
		{
			ApplyMaterialRecursively(transform, currMaterial);
		}

		private void ApplyMaterialRecursively(Transform tr, Material mat)
		{
			bool existsMeshRenderer = tr.TryGetComponent<Renderer>(out currRenderer);

			if (existsMeshRenderer)
			{
				currRenderer.sharedMaterial = mat;
			}

			int childCount = tr.childCount;
			for (int i = 0; i < childCount; i++)
			{
				ApplyMaterialRecursively(tr.GetChild(i), mat);
			}
		}

		public override bool CheckValidComponent()
		{
			bool res = true;

			bool dirty = false;
			if (currRenderer == null || currMaterial == null)
			{
				res = res && TryGetComponent<Renderer>(out currRenderer);
				dirty = true;
			}

			if (dirty && res)
			{
				currRenderer = GetComponent<Renderer>();

				EditorUtility.SetDirty(this);

				if (currMaterial != null)
				{
					EditorUtility.SetDirty(currMaterial);
				}
			}

			return res;
		}
#endif
	}
}