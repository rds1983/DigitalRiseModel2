#include "PbrCommon.fxh"

struct VertexInput
{
	float4 Position : POSITION;
	float3 Normal : NORMAL;
	float2 TexCoord : TEXCOORD0;
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
	VertexOutput output;
	float4 worldPosition = mul(input.Position, World);

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = worldPosition.xyz;
	output.Normal = mul(input.Normal, (float3x3)World);
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
