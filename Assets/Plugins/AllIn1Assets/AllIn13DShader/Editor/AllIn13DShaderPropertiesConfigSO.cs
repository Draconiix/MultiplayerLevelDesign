using AllIn1Assets.SharedExtensions;
using UnityEngine;

namespace AllIn1Assets.AllIn13DShader
{
	public class AllIn13DShaderPropertiesConfigSO : PropertiesConfigCollection
	{
		public PropertiesConfig propertiesConfig;

		public override void AddConfig(PropertiesConfig config)
		{
			this.propertiesConfig = config;
		}

		public override PropertiesConfig FindPropertiesConfigByShader(Shader shader)
		{
			return propertiesConfig;
		}

		public override PropertiesConfig GetPropertiesConfig()
		{
			return propertiesConfig;
		}

		public override bool IsAllIn3DShaderMaterial()
		{
			return true;
		}
	}
}