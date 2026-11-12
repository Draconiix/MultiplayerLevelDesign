using AllIn1Assets.SharedExtensions;

namespace AllIn1Assets.AllIn13DShader
{
	public class AllIn13DShaderGradientDrawer : AllIn1AbstractShaderGradientDrawer
	{
		public override Plugin3DConfig GetPluginConfig()
		{
			Plugin3DConfig res = PluginRegistry.GetPluginByID<Plugin3DConfig>(PluginID.ALL_IN_1_3D_SHADER);
			return res;
		}
	}
}