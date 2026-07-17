#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define DIRECTIONAL_LIGHT_LIMIT 4

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    int _AdditionalLightCount;
CBUFFER_END

struct DirectionalLightData
{
    float4 color, directionAndMask, shadowData;
};
StructuredBuffer<DirectionalLightData> _DirectionalLightData;

struct AdditionalLightData
{
    float4 color, position, directionAndMask, spotAngles, shadowData;
};
StructuredBuffer<AdditionalLightData> _AdditionalLightData;

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

int GetAdditionalLightCount()
{
    return _AdditionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData(float4 lightShadowData, ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = lightShadowData.x; // * shadowData.strength;
    data.tileIndex = lightShadowData.y + shadowData.cascadeIndex;
    data.normalBias = lightShadowData.z;
    data.maskChannel = lightShadowData.w;
    return data;
}

AdditionalShadowData GetAdditionalShadowData(float4 lightShadowData)
{
    AdditionalShadowData data;
    data.strength = lightShadowData.x;
    data.tileIndex = lightShadowData.y;
    data.isPoint = lightShadowData.z == 1.0;
    data.maskChannel = lightShadowData.w;
    data.lightDirectionWS = 0;
    data.lightPositionWS = 0;
    data.spotDirectionWS = 0;
    
    return data;
}

Light GetDirectionalLight(int index, Surface surface, ShadowData shadowData)
{
    DirectionalLightData data = _DirectionalLightData[index];
    Light light;
    light.color = data.color.xyz;
    light.direction = data.directionAndMask.xyz;
    DirectionalShadowData directionalShadowData = GetDirectionalShadowData(data.shadowData, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(directionalShadowData, shadowData, surface);
    //light.attenuation = shadowData.cascadeIndex * 0.25;
    return light;
}

Light GetAdditionalLight(int index, Surface surface, ShadowData shadowData)
{
    AdditionalLightData data = _AdditionalLightData[index];
    Light light;
    light.color = data.color.xyz;
    float3 position = data.position.xyz;
    float3 ray = position - surface.position;
    light.direction = normalize(ray);
    float distanceSqr = max(dot(ray, ray), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * data.position.w)));
    float3 spotDirection = data.directionAndMask.xyz;
    float spotAttenuation = Square(saturate(
        dot(spotDirection, light.direction) *
        data.spotAngles.x + data.spotAngles.y));
    AdditionalShadowData additionalShadowData = GetAdditionalShadowData(data.shadowData);
    additionalShadowData.lightPositionWS = position;
    additionalShadowData.lightDirectionWS = light.direction;
    additionalShadowData.spotDirectionWS = spotDirection;
    light.attenuation = GetAdditionalShadowAttenuation(additionalShadowData, shadowData, surface) *
        spotAttenuation * rangeAttenuation / distanceSqr;
    return light;
}



#endif