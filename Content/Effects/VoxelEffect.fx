#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 CameraPosition;
float3 SunDirection;
float3 AmbientColor;
float3 SunColor;
float3 HorizonColor;
float3 ZenithColor;
float3 FogColor;
float3 UnderwaterFogColor;
float3 ShadowColor;
float4 TorchLights[16];
int TorchLightCount;
float DaylightFactor;
float FogStart;
float FogEnd;
float Time;
float UnderwaterFactor;
texture BlockAtlas;

float hash(float n)
{
    return frac(sin(n) * 43758.5453);
}

sampler AtlasSampler = sampler_state
{
    Texture = <BlockAtlas>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 WorldNormal : TEXCOORD1;
    float4 Color : COLOR0;
    float3 ViewDirection : TEXCOORD2;
    float2 TexCoord : TEXCOORD3;
    float3 FogTint : TEXCOORD4;
};

VertexOutput MainVS(VertexInput input)
{
    VertexOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    output.WorldPosition = worldPosition.xyz;
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)World));
    output.Color = input.Color;
    output.ViewDirection = normalize(CameraPosition - worldPosition.xyz);
    output.TexCoord = input.TexCoord;
    float skyBlend = saturate(output.WorldNormal.y * 0.5 + 0.5);
    output.FogTint = lerp(HorizonColor, ZenithColor, skyBlend * 0.6 + 0.2);

    return output;
}

float4 MainPS(VertexOutput input) : COLOR0
{
    float3 normal = normalize(input.WorldNormal);
    float3 viewDir = normalize(input.ViewDirection);
    float3 sunDir = normalize(-SunDirection);

    float diffuse = saturate(dot(normal, sunDir));
    float wrappedDiffuse = saturate(diffuse * 0.60 + 0.50);
    float backLight = pow(saturate(dot(normal, -sunDir)), 1.8) * 0.02;
    float topLight = saturate(normal.y * 0.5 + 0.5);
    float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 2.2);
    float horizonLight = saturate(1.0 - abs(normal.y)) * 0.035;

    float faceShade = normal.y > 0.5 ? 1.0 : (abs(normal.x) > 0.5 ? 0.86 : 0.93);

    float worldVariation = sin(input.WorldPosition.x * 0.17 + Time * 0.1) * cos(input.WorldPosition.z * 0.14 - Time * 0.08);
    float variationMask = worldVariation * 0.02;
    float heightTint = saturate(input.WorldPosition.y / 24.0) * 0.04;
    float texHash = hash(floor(input.WorldPosition.x) + floor(input.WorldPosition.y) * 31.0 + floor(input.WorldPosition.z) * 173.0);
    float grain = (texHash - 0.5) * 0.018;

    float4 texColor = tex2D(AtlasSampler, input.TexCoord);
    clip(texColor.a - 0.1);
    float3 baseColor = texColor.rgb * input.Color.rgb;
    baseColor = saturate(baseColor * faceShade + variationMask.xxx + grain.xxx + float3(heightTint * 0.18, heightTint * 0.22, heightTint * 0.08));
    float3 ambient = AmbientColor * lerp(1.02, 1.16, topLight);
    float3 sunlight = SunColor * wrappedDiffuse * 0.70;
    float3 rimLight = lerp(HorizonColor, ZenithColor, topLight) * fresnel * 0.03;
    float3 bounceLight = input.FogTint * horizonLight;
    float torchLight = 0.0;

    [unroll]
    for (int i = 0; i < 16; i++)
    {
        if (i >= TorchLightCount)
        {
            break;
        }

        float3 toTorch = TorchLights[i].xyz - input.WorldPosition;
        float distanceToTorch = length(toTorch);
        if (distanceToTorch > 9.0)
        {
            continue;
        }

        float attenuation = saturate(1.0 - distanceToTorch / 9.0);
        attenuation = attenuation * attenuation;
        torchLight = max(torchLight, attenuation);
    }

    float torchStrength = lerp(0.18, 2.25, 1.0 - DaylightFactor);
    float3 torchColor = float3(1.0, 0.78, 0.36) * torchLight * torchStrength;

    float3 litColor = baseColor * (ambient + sunlight + bounceLight + torchColor) + rimLight + backLight.xxx;

    float distanceToCamera = distance(input.WorldPosition, CameraPosition);
    float fogFactor = saturate((distanceToCamera - FogStart) / max(FogEnd - FogStart, 0.001));
    fogFactor = fogFactor * fogFactor * (3.0 - 2.0 * fogFactor);
    float distanceFade = saturate(1.0 - distanceToCamera / max(FogEnd, 0.001));
    distanceFade = lerp(0.82, 1.0, distanceFade);

    float heightFog = saturate(1.0 - input.WorldPosition.y / 36.0);
    float horizonBlend = saturate(viewDir.y * 0.5 + 0.5);
    float sunGlow = pow(max(0.0, dot(viewDir, sunDir)), 48.0) + pow(max(0.0, dot(viewDir, sunDir)), 4.0) * 0.25;
    float daylightFactor = saturate((SunDirection.y + 0.10) / 0.32);
    daylightFactor = daylightFactor * daylightFactor * (3.0 - 2.0 * daylightFactor);
    float3 daySky = lerp(float3(0.82, 0.90, 0.98), float3(0.28, 0.58, 0.94), horizonBlend);
    float3 nightSky = lerp(float3(0.04, 0.05, 0.09), float3(0.01, 0.02, 0.05), horizonBlend);
    float3 backgroundColor = lerp(nightSky, daySky, daylightFactor);
    backgroundColor += sunGlow * float3(1.0, 0.90, 0.62);
    float3 atmosphericFog = lerp(backgroundColor, FogColor, lerp(0.22, 0.10, daylightFactor) + heightFog * 0.06);
    atmosphericFog = lerp(atmosphericFog, UnderwaterFogColor, UnderwaterFactor);

    float3 finalColor = lerp(litColor * distanceFade, atmosphericFog, fogFactor);
    finalColor = lerp(finalColor, finalColor * float3(0.72, 0.86, 0.96), UnderwaterFactor * 0.55);
    finalColor = saturate(finalColor);
    finalColor = pow(finalColor, 1.0 / 1.12);

    return float4(finalColor, texColor.a * input.Color.a);
}

technique VoxelTerrain
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
