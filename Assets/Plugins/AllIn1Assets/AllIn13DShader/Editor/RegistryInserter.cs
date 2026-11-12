using UnityEditor;

namespace AllIn1Assets.AllIn13DShader
{
	public static class RegistryInserter
	{
		//Declaring the plugin costs nothing: no asset is read, found or created here. The registry
		//resolves the config asset the first time something actually asks for it
		[InitializeOnLoadMethod]
		private static void InitializeOnLoad()
		{
			PluginRegistry.Register(PluginID.ALL_IN_1_3D_SHADER, typeof(AllIn13DShaderConfig));
		}
	}
}
