#include "PbrCommon.fxh"

struct VertexInput
{
	float4 Position : POSITION;
	float3 Normal : NORMAL;
	float4 Tangent : TANGENT;
	float2 TexCoord : TEXCOORD0;
};

struct VertexOutput
{
	float4 Position : SV_Position;
	float3 WorldPosition : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float3 Tangent : TEXCOORD2;
	float3 Bitangent : TEXCOORD3;
	float2 TexCoord : TEXCOORD4;
};

VertexOutput MainVS(VertexInput input)
{
	VertexOutput output;
	float4 worldPosition = mul(input.Position, World);
	float3 normal = mul(input.Normal, (float3x3)World);
	float3 tangent = mul(input.Tangent.xyz, (float3x3)World);

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = worldPosition.xyz;
	output.Normal = normal;
	output.Tangent = tangent;
	output.Bitangent = cross(normal, tangent) * input.Tangent.w;
	output.TexCoord = input.TexCoord;
	return output;
}

float4 MainPS(VertexOutput input, float facing : VFACE) : SV_Target
{
	float3 normal = normalize(input.Normal);

	if (HasNormalTexture > 0.5)
	{
		float3 sampledNormal = tex2D(NormalSampler, input.TexCoord).xyz * 2.0 - 1.0;
		sampledNormal.xy *= NormalScale;
		float3 tangent = normalize(input.Tangent);
		float3 bitangent = normalize(input.Bitangent);
		float3x3 tangentSpace = float3x3(tangent, bitangent, normal);
		normal = normalize(mul(sampledNormal, tangentSpace));
	}

	return ShadeSurface(input.WorldPosition, normal, input.TexCoord, facing);
}

technique
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}
