#include "PbrCommon.fxh"

float4x4 Bones[72];

struct VertexInput
{
	float4 Position : POSITION;
	float3 Normal : NORMAL;
	float2 TexCoord : TEXCOORD0;
	int4 BlendIndices : BLENDINDICES0;
	float4 BlendWeights : BLENDWEIGHT0;
};

struct VertexOutput
{
	float4 Position : SV_Position;
	float3 WorldPosition : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float2 TexCoord : TEXCOORD2;
};

VertexOutput MainVS(VertexInput input)
{
	float4x4 skinning = 0;

	skinning += Bones[input.BlendIndices.x] * input.BlendWeights.x;
	skinning += Bones[input.BlendIndices.y] * input.BlendWeights.y;
	skinning += Bones[input.BlendIndices.z] * input.BlendWeights.z;
	skinning += Bones[input.BlendIndices.w] * input.BlendWeights.w;

	float4 skinnedPosition = mul(input.Position, skinning);
	float3 skinnedNormal = mul(input.Normal, (float3x3)skinning);
	float4 worldPosition = mul(skinnedPosition, World);

	VertexOutput output;
	output.Position = mul(skinnedPosition, WorldViewProjection);
	output.WorldPosition = worldPosition.xyz;
	output.Normal = mul(skinnedNormal, (float3x3)World);
	output.TexCoord = input.TexCoord;
	return output;
}

float4 MainPS(VertexOutput input, float facing : VFACE) : SV_Target
{
	return ShadeSurface(input.WorldPosition, input.Normal, input.TexCoord, facing);
}

technique
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}
