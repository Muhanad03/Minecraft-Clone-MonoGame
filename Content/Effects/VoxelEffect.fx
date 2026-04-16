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
float FogStart;
float FogEnd;
float Time;
texture BlockAtlas;

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

    return output;
}

float4 MainPS(VertexOutput input) : COLOR0
{
    float3 normal = normalize(input.WorldNormal);
    float3 viewDir = normalize(input.ViewDirection);
    float3 sunDir = normalize(-SunDirection);

    float diffuse = saturate(dot(normal, sunDir));
    float wrappedDiffuse = saturate(diffuse * 0.65 + 0.35);
    float backLight = pow(saturate(dot(normal, -sunDir)), 1.8) * 0.08;
    float topLight = saturate(normal.y * 0.5 + 0.5);
    float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 2.4);

    float worldVariation = sin(input.WorldPosition.x * 0.17 + Time * 0.1) * cos(input.WorldPosition.z * 0.14 - Time * 0.08);
    float variationMask = worldVariation * 0.035;
    float heightTint = saturate(input.WorldPosition.y / 24.0) * 0.08;

    float4 texColor = tex2D(AtlasSampler, input.TexCoord);
    float3 baseColor = saturate(texColor.rgb * input.Color.rgb + variationMask.xxx + float3(heightTint * 0.3, heightTint * 0.45, heightTint * 0.15));
    float3 ambient = AmbientColor * lerp(0.82, 1.12, topLight);
    float3 sunlight = SunColor * wrappedDiffuse * 0.72;
    float3 rimLight = lerp(HorizonColor, ZenithColor, topLight) * fresnel * 0.18;

    float3 litColor = baseColor * (ambient + sunlight) + rimLight + backLight.xxx;

    float distanceToCamera = distance(input.WorldPosition, CameraPosition);
    float fogFactor = saturate((distanceToCamera - FogStart) / max(FogEnd - FogStart, 0.001));
    fogFactor = fogFactor * fogFactor * (3.0 - 2.0 * fogFactor);

    float heightFog = saturate(1.0 - input.WorldPosition.y / 36.0);
    float horizonBlend = saturate(0.55 + normal.y * 0.45);
    float3 atmosphericFog = lerp(HorizonColor, ZenithColor, horizonBlend * 0.65 + 0.2);
    atmosphericFog = lerp(atmosphericFog, FogColor, 0.55 + heightFog * 0.2);

    float3 finalColor = lerp(litColor, atmosphericFog, fogFactor);
    finalColor = pow(saturate(finalColor), 1.0 / 1.1);

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
