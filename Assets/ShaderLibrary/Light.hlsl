#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define DIRECTIONAL_LIGHT_LIMIT 4
#define ADDITIONAL_LIGHT_LIMIT 64

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[DIRECTIONAL_LIGHT_LIMIT];
    float4 _DirectionalLightDirections[DIRECTIONAL_LIGHT_LIMIT];
    float4 _DirectionalLightShadowData[DIRECTIONAL_LIGHT_LIMIT];
    int _AdditionalLightCount;
    float4 _AdditionalLightColors[ADDITIONAL_LIGHT_LIMIT];
    float4 _AdditionalLightPositions[ADDITIONAL_LIGHT_LIMIT];
    float4 _AdditionalLightDirections[ADDITIONAL_LIGHT_LIMIT];
    float4 _AdditionalLightSpotAngles[ADDITIONAL_LIGHT_LIMIT];
    float4 _AdditionalLightShadowData[ADDITIONAL_LIGHT_LIMIT];
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

int GetAdditionalLightCount()
{
    return min(_AdditionalLightCount, ADDITIONAL_LIGHT_LIMIT);
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

AdditionalShadowData GetAdditionalShadowData(int lightIndex)
{
    AdditionalShadowData data;
    data.strength = _AdditionalLightShadowData[lightIndex].x;
    data.tileIndex = _AdditionalLightShadowData[lightIndex].y;
    data.isPoint = _AdditionalLightShadowData[lightIndex].z == 1.0;
    data.maskChannel = _AdditionalLightShadowData[lightIndex].w;
    data.lightDirectionWS = 0;
    data.lightPositionWS = 0;
    data.spotDirectionWS = 0;
    
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

Light GetAdditionalLight(int index, Surface surface, ShadowData shadowData)
{
    Light light;
    light.color = _AdditionalLightColors[index].xyz;
    float3 position = _AdditionalLightPositions[index].xyz;
    float3 ray = position - surface.position;
    light.direction = normalize(ray);
    float distanceSqr = max(dot(ray, ray), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _AdditionalLightPositions[index].w)));
    float4 spotAngles = _AdditionalLightSpotAngles[index];
    float3 spotDirection = _AdditionalLightDirections[index].xyz;
    float spotAttenuation = Square(saturate(
        dot(spotDirection, light.direction) *
        spotAngles.x + spotAngles.y));
    AdditionalShadowData additionalShadowData = GetAdditionalShadowData(index);
    additionalShadowData.lightPositionWS = position;
    additionalShadowData.lightDirectionWS = light.direction;
    additionalShadowData.spotDirectionWS = spotDirection;
    light.attenuation = GetAdditionalShadowAttenuation(additionalShadowData, shadowData, surface) *
        spotAttenuation * rangeAttenuation / distanceSqr;
    return light;
}



#endif