#ifndef ALLIN13DSHADER_LIGHT_INCLUDED
#define ALLIN13DSHADER_LIGHT_INCLUDED


#if ALLIN1_USE_FORWARD_PLUS
    #define LIGHT_LOOP_BEGIN_ALLIN13D(lightCount, data) { \
    uint lightIndex; \
    ClusterIterator _urp_internal_clusterIterator = ClusterInit(data.normalizedScreenSpaceUV, data.vertexWS, 0); \
    [loop] while (ClusterNext(_urp_internal_clusterIterator, lightIndex)) { \
        lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT; \
        ALLIN1_FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
    #define LIGHT_LOOP_END_ALLIN13D } }
#else
    #define LIGHT_LOOP_BEGIN_ALLIN13D(lightCount, data) \
	if(lightCount > 0) { \
    for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex) {
    #define LIGHT_LOOP_END_ALLIN13D } }
#endif

float GetMainLightIntensity()
{
	return length(GetMainLightColorRGB());
}

//Toon Ramp trusts the shadow map from this NdotL on: its step sits at the terminator, where the shadow map aliases into a jagged band
#define TOON_RAMP_SHADOW_FADE 0.2

//The wrap models take the main light's shadow on NdotL; Toon Ramp scales its ramp coordinate instead, so full shadow reads the ramp's dark end
float ShadowedRawNdotL(float rawNdotL, float shadow)
{
#if defined(_LIGHTMODEL_TOONRAMP)
	shadow = lerp(1.0, shadow, SafeSmoothstep(0.0, TOON_RAMP_SHADOW_FADE, rawNdotL));
	float res = (rawNdotL + 1.0) * shadow - 1.0;
#else
	float res = rawNdotL * shadow;
#endif
	return res;
}

#if defined(_CUSTOM_SHADOW_COLOR_ON) && !defined(FORWARD_ADD_PASS)
#define SHADOW_COLOR_TERMINATOR_WIDTH 0.1
//A main light dimmer than this fades its shadow color out, so a sun dimmed to 0 doesn't drop the color in one frame
#define SHADOW_COLOR_LIGHT_FADE 0.05

AllIn1LightData FullyShadowedLightData(AllIn1LightData lightData)
{
	AllIn1LightData res = lightData;
	res.realtimeShadow = 0.0;
	res.shadowColor = MainLightShadowColor(0.0);
	return res;
}

//The main light's unshadowed light model term, as DiffuseTerm computes it
float LightModelTerm(float rawNdotL)
{
	float NdotL = saturate(rawNdotL);
	float res = NdotL;
#if defined(_LIGHTMODEL_TOON)
	res = SafeSmoothstep(ACCESS_PROP_FLOAT(_ToonCutoff), ACCESS_PROP_FLOAT(_ToonCutoff) + ACCESS_PROP_FLOAT(_ToonSmoothness), NdotL);
	#if defined(_SHADINGMODEL_PBR)
	res *= NdotL;
	#endif
#elif defined(_LIGHTMODEL_TOONRAMP)
	res = GetLuminance(SAMPLE_TEX2D_LOD(_ToonRamp, float4((rawNdotL * 0.5) + 0.5, 0, 0, 0)).rgb);
#elif defined(_LIGHTMODEL_HALFLAMBERT)
	float halfLambertTerm = (NdotL * ACCESS_PROP_FLOAT(_HalfLambertWrap)) + (1 - ACCESS_PROP_FLOAT(_HalfLambertWrap));
	res = halfLambertTerm * halfLambertTerm;
#elif defined(_LIGHTMODEL_FAKEGI)
	res = (NdotL * ACCESS_PROP_FLOAT(_HardnessFakeGI)) + 1.0 - ACCESS_PROP_FLOAT(_HardnessFakeGI);
#endif
	return res;
}

//How lit the main light leaves a pixel: 0 as dark as the light model gets, 1 facing the light unshadowed
float LitFraction(float rawNdotL, float shadow)
{
	float darkest = LightModelTerm(-1.0);
	float range = LightModelTerm(1.0) - darkest;
#if defined(_LIGHTMODEL_HALFLAMBERT) || defined(_LIGHTMODEL_FAKEGI) || defined(_LIGHTMODEL_TOONRAMP)
	float term = LightModelTerm(ShadowedRawNdotL(rawNdotL, shadow));
#else
	float term = LightModelTerm(rawNdotL) * shadow;
#endif
	float res = range > 0.0001 ? saturate((term - darkest) / SafeDivisor(range)) : 1.0;
	return res;
}

//x: weight of the fully shadowed light, y: weight of the light added on top of it, z: how lit the main light leaves the pixel, w: how strongly the main light shines
float4 CustomShadowColorWeights(AllIn1LightData lightData, float rawNdotL)
{
	float shadow = lightData.realtimeShadow;
	float termShadow = lightData.realtimeShadow;
	bool hasMainLight = length(lightData.lightColor.rgb) * lightData.distanceAttenuation > 0.0;
#if defined(_RECEIVE_SHADOWS_ON) && !defined(_LIGHTMODEL_FASTLIGHTING) && !defined(SUBTRACTIVE_LIGHTING)
	//Faces turned away from the light ignore the shadow map: they count as shadowed where the light model leaves them dark, and as lit where it still wraps light onto them
	float litFactor = saturate(LightModelTerm(rawNdotL) / SHADOW_COLOR_TERMINATOR_WIDTH);
	float shadowMapInfluence = SafeSmoothstep(0.0, SHADOW_COLOR_TERMINATOR_WIDTH, rawNdotL);
	float lightModelShadow = lerp(litFactor, min(shadow, litFactor), shadowMapInfluence);
	shadow = hasMainLight ? lightModelShadow : shadow;
	termShadow = hasMainLight ? lerp(1.0, termShadow, shadowMapInfluence) : termShadow;
#endif
	float visibleShadow = rawNdotL > 0.0 ? lightData.realtimeShadow : shadow;

	float lit = LitFraction(rawNdotL, termShadow) * visibleShadow;
#if defined(SUBTRACTIVE_LIGHTING)
	lit = lightData.realtimeShadow;
#endif

	float4 res;
	res.x = saturate(shadow + 1.0 - global_shadowColor.a);
	res.y = saturate(visibleShadow + 1.0 - global_shadowColor.a);
	res.z = hasMainLight ? lit : lightData.realtimeShadow;
	res.w = saturate(max(lightData.lightColor.r, max(lightData.lightColor.g, lightData.lightColor.b)) / SHADOW_COLOR_LIGHT_FADE);
	return res;
}

//Weight of the shadow color: full where the main light leaves the pixel as dark as full shadow, none once the lighting alone is as bright as the color
float ShadowColorFloor(float litFraction, float lightStrength, float3 lighting, float3 litDiffuse, float3 litColor)
{
	//Measured on the lighting too, so a model darker than its term (PBR at grazing angles) can't dip below the color
	float litDiffuseLuminance = GetLuminance(litDiffuse);
	litFraction = litDiffuseLuminance > 0.0001 ? min(litFraction, GetLuminance(lighting) / SafeDivisor(litDiffuseLuminance)) : litFraction;

	//Saturated colors look brighter than their luminance, so their strongest channel counts too
	float luminanceReach = GetLuminance(global_shadowColor.rgb) / max(GetLuminance(litColor), 0.0001);
	float channelReach = max(global_shadowColor.r, max(global_shadowColor.g, global_shadowColor.b)) / max(max(litColor.r, max(litColor.g, litColor.b)), 0.0001);
	float reach = saturate(max(luminanceReach, channelReach));
	float res = global_shadowColor.a * lightStrength * (1.0 - saturate(litFraction / max(reach, 0.0001)));
	return res;
}
#endif

#if defined(_SHADINGMODEL_PBR) || defined(_SPECULARMODEL_ANISOTROPIC) || defined(_SPECULARMODEL_ANISOTROPICTOON)
	#include "../ShaderLibrary/AllIn13DShader_BRDF.hlsl"
#endif

float3 GetBitangentWS(float4 tangentOS, float3 tangentWS, float3 normalWS)
{
    float tangentSign = tangentOS.w * unity_WorldTransformParams.w;
    float3 res = cross(normalWS, tangentWS) * tangentSign;
    return res;
}

float3 DiffuseTerm(AllIn1LightData lightData, float3 normal, float isAdditionalLight)
{
	float rawNdotL = dot(normal, lightData.lightDir);
	float3 lightModel = 0;
	float3 lightColor = lightData.lightColor.rgb * lightData.distanceAttenuation * lightData.shadowColor.rgb;

#if !defined(FORWARD_ADD_PASS)
	#if defined(_LIGHTMODEL_HALFLAMBERT) || defined(_LIGHTMODEL_FAKEGI) || defined(_LIGHTMODEL_TOONRAMP)
		lightColor = isAdditionalLight > 0 ? lightColor : lightData.lightColor.rgb;
		rawNdotL = isAdditionalLight > 0 ? rawNdotL : ShadowedRawNdotL(rawNdotL, GetLuminance(lightData.shadowColor.rgb));
	#endif
#endif

#if defined(_LIGHTMODEL_CLASSIC)
	float NdotL = saturate(rawNdotL);
	lightModel = NdotL;

#elif defined(_LIGHTMODEL_TOON)
	float NdotL = saturate(rawNdotL);
	lightModel = SafeSmoothstep(ACCESS_PROP_FLOAT(_ToonCutoff), ACCESS_PROP_FLOAT(_ToonCutoff) + ACCESS_PROP_FLOAT(_ToonSmoothness), NdotL); 

#elif defined(_LIGHTMODEL_TOONRAMP)
	float NdotL = (rawNdotL * 0.5) + 0.5;
	lightModel = SAMPLE_TEX2D_LOD(_ToonRamp, float4(NdotL, 0, 0, 0)).rgb;

#elif defined(_LIGHTMODEL_HALFLAMBERT)
	float NdotL = saturate(rawNdotL);
	float halfLambertTerm = (NdotL * ACCESS_PROP_FLOAT(_HalfLambertWrap)) + (1 - ACCESS_PROP_FLOAT(_HalfLambertWrap));
	lightModel = halfLambertTerm * halfLambertTerm;

#elif defined(_LIGHTMODEL_FAKEGI)
	float NdotL = saturate(rawNdotL);
	lightModel = (NdotL * ACCESS_PROP_FLOAT(_HardnessFakeGI)) + 1.0 - ACCESS_PROP_FLOAT(_HardnessFakeGI);

#elif defined(_LIGHTMODEL_FASTLIGHTING)
	lightModel = saturate(rawNdotL);

#endif
	
	float3 res = lightModel * lightColor;

    return res;
}

#ifdef SPECULAR_ON

float3 SpecularTerm(float3 objectColor, AllIn1LightData lightData, 
		float3 normalWS, float3 tangentWS, float3 bitangentWS,
		float3 viewDirWS, float glossiness, float2 mainUV, float4 specularTex)
{
	float3 lightColor = lightData.lightColor.rgb * lightData.distanceAttenuation * lightData.shadowColor.rgb;
    float3 reflectionDir = reflect(-lightData.lightDir, normalWS);
    float rawVdotL = dot(viewDirWS, reflectionDir);
	
    float3 specularModel = 0;
	

#if defined(_SPECULARMODEL_CLASSIC) || defined(_SPECULARMODEL_TOON)
	float3 halfVector = normalize(viewDirWS + lightData.lightDir);
	float NdotH = saturate(dot(normalWS, halfVector));
	float specularIntensity = pow(NdotH, glossiness);
	
#elif defined(_SPECULARMODEL_ANISOTROPIC) || defined(_SPECULARMODEL_ANISOTROPICTOON)
	float3 H = normalize(viewDirWS + lightData.lightDir);

	float TdotH = saturate(dot(tangentWS, H));
	float BdotH = saturate(dot(bitangentWS, H));

	float TdotV = saturate(dot(tangentWS, viewDirWS));
	float BdotV = saturate(dot(bitangentWS, viewDirWS));

	float TdotL = saturate(dot(tangentWS, lightData.lightDir));
	float BdotL = saturate(dot(bitangentWS, lightData.lightDir));

	float NdotH = saturate(dot(normalWS, H));
	float NdotL = saturate(dot(normalWS, lightData.lightDir));
	float NdotV = saturate(dot(normalWS, viewDirWS));

	float VdotH = saturate(dot(viewDirWS, H));

	float anisotropy = ACCESS_PROP_FLOAT(_Anisotropy);
	float anisoShininess = ACCESS_PROP_FLOAT(_AnisoShininess);
	float at = max((1 - anisoShininess) * (1.0 + anisotropy), 0.001);
	float ab = max((1 - anisoShininess) * (1.0 - anisotropy), 0.001);

	float3 specularIntensity = SpecularAnisoTerm(
		at, ab, 
		float3(1.0, 1.0, 1.0), 
		NdotH, NdotV, NdotL,
		TdotV, BdotV, 
		TdotL, BdotL, 
		TdotH, BdotH,
		H, tangentWS.xyz, bitangentWS.xyz) * NdotL;

	specularIntensity = saturate(specularIntensity);
#endif

#if defined(_SPECULARMODEL_ANISOTROPICTOON) || defined(_SPECULARMODEL_TOON)
	specularIntensity = SafeSmoothstep(ACCESS_PROP_FLOAT(_SpecularToonCutoff), ACCESS_PROP_FLOAT(_SpecularToonCutoff) + ACCESS_PROP_FLOAT(_SpecularToonSmoothness), specularIntensity);
#endif

	specularModel = specularIntensity * lightColor;
	float3 res = specularModel * ACCESS_PROP_FLOAT(_SpecularAtten) * specularTex.r;
	
	return res;
}

float3 SpecularTerm_Basic(float3 objectColor, AllIn1LightData lightData, 
		float3 normalWS, float3 tangentWS, float3 bitangentWS, float3 viewDirWS, float2 mainUV, float4 specularTex)
{
	float3 res = SpecularTerm(objectColor, lightData,
		normalWS, tangentWS, bitangentWS,
		viewDirWS, ACCESS_PROP_FLOAT(_Shininess) * ACCESS_PROP_FLOAT(_Shininess), mainUV, specularTex);
	return res;
}

#endif

float3 RimTermOperation(float3 normalWS, float3 viewDirWS, float minRim, float maxRim, float rimAttenuation, float3 rimColor)
{
	float NdotV = saturate(dot(normalWS, viewDirWS));
	float rimIntensity = (1 - NdotV);
	rimIntensity = SafeSmoothstep(minRim, maxRim, rimIntensity) * rimAttenuation;

	float3 res = rimIntensity * rimColor;
	return res;
}

float3 DirectLighting(float3 objectColor, EffectsData effectsData, AllIn1LightData lightData, float4 specularTex, float isAdditionalLight)
{
	float3 res = 0;

	float3 diffuseTerm = DiffuseTerm(lightData, effectsData.normalWS, isAdditionalLight) * objectColor;
	
#if defined(_CUSTOM_SHADOW_COLOR_ON) && !defined(FORWARD_ADD_PASS)
	if (isAdditionalLight == 0)
	{
		float3 shadowedDiffuseTerm = DiffuseTerm(FullyShadowedLightData(lightData), effectsData.normalWS, 0) * objectColor;
		float4 shadowColorT = CustomShadowColorWeights(lightData, dot(effectsData.normalWS, lightData.lightDir));
		diffuseTerm = shadowedDiffuseTerm * shadowColorT.x + (diffuseTerm - shadowedDiffuseTerm) * shadowColorT.y;

		float3 litColor = lightData.lightColor.rgb * lightData.distanceAttenuation * objectColor;
		diffuseTerm += global_shadowColor.rgb * ShadowColorFloor(shadowColorT.z, shadowColorT.w, diffuseTerm, litColor, litColor);
	}
#endif
	
	float3 specularTerm = 0;
#ifdef SPECULAR_ON
	
	float NdotL = saturate(dot(effectsData.normalWS, lightData.lightDir));

    specularTerm = SpecularTerm_Basic(objectColor, lightData,
		effectsData.normalWS, effectsData.tangentWS, effectsData.bitangentWS, 
		effectsData.viewDirWS, effectsData.mainUV, specularTex) * NdotL;
#endif
	
	res = diffuseTerm + specularTerm;

	return res;
}

float3 DirectLighting(float3 objectColor, float2 ssaoFactor, EffectsData effectsData, AllIn1GI gi)
{
	AllIn1LightData mainLightData = GetMainLightData(effectsData.vertexWS, effectsData, gi);


	float3 res = objectColor;
	float4 specularTex = float4(1, 1, 1, 1);

#ifdef ALLIN1_USE_LIGHT_LAYERS
	uint meshRenderingLayers = AllIn1GetMeshRenderingLayer();
	res = 0;
	if (IsMatchingLightLayer(mainLightData.layerMask, meshRenderingLayers))
	{
		res = objectColor;
#endif

#ifdef SPECULAR_ON
	specularTex = SAMPLE_TEX2D(_SpecularMap, effectsData.mainUV);
#endif

	res = DirectLighting(objectColor, effectsData, mainLightData, specularTex, 0);

#ifdef ALLIN1_USE_LIGHT_LAYERS
		}
#endif



#if defined(ADDITIONAL_LIGHT_LOOP) && !defined(_LIGHTMODEL_FASTLIGHTING)
	// Additional light loop for non-main directional lights. This block is specific to Forward+.
	#if USE_CLUSTER_LIGHT_LOOP
	UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
	{
		AllIn1LightData additionalLightData = GetPointLightData(lightIndex, effectsData.vertexWS, effectsData.normalWS, effectsData, gi);
		#ifdef ALLIN1_USE_LIGHT_LAYERS
		if (IsMatchingLightLayer(additionalLightData.layerMask, meshRenderingLayers))
		{
		#endif
			res += DirectLighting(objectColor, effectsData, additionalLightData, specularTex, 1);
		#ifdef ALLIN1_USE_LIGHT_LAYERS
		}
		#endif
	}
	#endif
	
	uint numAdditionalLights = NUM_ADDITIONAL_LIGHTS;
	LIGHT_LOOP_BEGIN_ALLIN13D(numAdditionalLights, effectsData)
		AllIn1LightData additionalLightData = GetPointLightData(lightIndex, effectsData.vertexWS, effectsData.normalWS, effectsData, gi);
	#ifdef ALLIN1_USE_LIGHT_LAYERS
		if (IsMatchingLightLayer(additionalLightData.layerMask, meshRenderingLayers))
		{
	#endif
		res += DirectLighting(objectColor, effectsData, additionalLightData, specularTex, 1);
	#ifdef ALLIN1_USE_LIGHT_LAYERS
		}
	#endif

	LIGHT_LOOP_END_ALLIN13D
#endif

	res *= ssaoFactor.x;

	return res;
}

float3 IndirectLighting_Basic(float3 objectColor, float2 ssaoFactor, EffectsData effectsData, AllIn1GI gi)
{
	//float3 ambientColor = GetAmbientColor(effectsData);

	float3 ao = 1.0;

	float3 reflections = 0;
#ifdef REFLECTIONS_ON
	reflections = GetSkyColor(effectsData.vertexWS, effectsData.normalizedScreenSpaceUV, effectsData.normalWS, effectsData.viewDirWS, 1.0);
#endif
	
	float3 indirectLighting = /*(ambientColor + lightmap)*/gi.diffuse * objectColor;
	indirectLighting += reflections;

	indirectLighting *= ssaoFactor.y;

	return indirectLighting;
}

float3 CalculateLighting_Basic(float3 objectColor, float alpha, EffectsData effectsData, AllIn1GI gi)
{
	float2 ssaoFactor = GetSSAO(effectsData.normalizedScreenSpaceUV.xy, alpha); 

	float3 directLighting = DirectLighting(objectColor, ssaoFactor, effectsData, gi);
	
	
	float3 indirectLighting = 0;	
	//We add IndirectLighting only once
#ifndef FORWARD_ADD_PASS
	indirectLighting = IndirectLighting_Basic(objectColor, ssaoFactor, effectsData, gi);
	
	#ifdef _AOMAP_ON
		float3 aoMapValue = GetAOMapTerm(effectsData.mainUV);
		indirectLighting *= aoMapValue;
	#endif
#endif

	float3 res = directLighting + indirectLighting;

	return res;
}

#ifdef _SHADINGMODEL_PBR
float3 CalculateLighting_MetallicWorkflow(
	float3 vertexWS, 
	float3 normalWS, float3 tangentWS, float3 bitangentWS, 
	float3 objectColor, float alpha,
	float3 shadows, float3 ambientCol, float3 viewDirWS, 
	float2 mainUV, FragmentData fragmentData, EffectsData effectsData, AllIn1GI gi)
{
	float3 res = CalculateLighting_PBR(objectColor, alpha, effectsData, gi);

	return res;
}

#endif

float3 CalculateLighting(float3 vertexWS, 
	float3 normalWS, float3 tangentWS, float3 bitangentWS, 
	float3 objectColor, float alpha,
	float shadows, float3 ambientCol, float3 viewDirWS, 
	float2 mainUV,
	float3 lightColor, float3 lightDir,
	FragmentData fragmentData, float lightAtten, EffectsData effectsData, 
	AllIn1GI gi)
{
#ifdef _CUSTOM_SHADOW_COLOR_ON
	float3 shadowColor = lerp(1.0, global_shadowColor, 1 - shadows);
#else
	float3 shadowColor = shadows;
#endif
	
	float3 res = 0;
#ifdef _LIGHTMODEL_FASTLIGHTING
	res = CalculateLighting_Basic(objectColor, alpha, effectsData, gi);
#else
	#ifdef _SHADINGMODEL_PBR
		res = CalculateLighting_MetallicWorkflow(
			vertexWS, 
			normalWS, tangentWS, bitangentWS, 
			objectColor, alpha, 
			shadowColor, ambientCol, viewDirWS, 
			mainUV, fragmentData, effectsData, gi);
	#else
		res = CalculateLighting_Basic(objectColor, alpha, effectsData, gi);
	#endif
#endif
	
	return res;
}

#endif