#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define DIRECTIONAL_LIGHT_LIMIT 4

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[DIRECTIONAL_LIGHT_LIMIT];
    float4 _DirectionalLightDirections[DIRECTIONAL_LIGHT_LIMIT];
    float4 _DirectionalLightShadowData[DIRECTIONAL_LIGHT_LIMIT];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

int GetDirectionalLightCount()
{
    return min(_DirectionalLightCount, DIRECTIONAL_LIGHT_LIMIT);
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x; // * shadowData.strength;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.maskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}

Light GetDirectionalLight(int index, Surface surface, ShadowData shadowData)
{
    Light light;
    light.color = _DirectionalLightColors[index].xyz;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShadowData directionalShadowData = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(directionalShadowData, shadowData, surface);
    //light.attenuation = shadowData.cascadeIndex * 0.25;
    return light;
}



#endif