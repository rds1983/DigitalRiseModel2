#include "PbrCommon.fxh"

struct VertexInput
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
};

struct VertexOutput
{
	float4 Position : SV_Position;
	float3 WorldPosition : TEXCOORD0;
	float2 TexCoord : TEXCOORD1;
};

VertexOutput MainVS(VertexInput input)
{
	VertexOutput output;
	float4 worldPosition = mul(input.Position, World);

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = worldPosition.xyz;
	output.TexCoord = input.TexCoord;
	return output;
}

float4 MainPS(VertexOutput input, float facing : VFACE) : SV_Target
{
	return ShadeSurface(input.WorldPosition, float3(0.0, 1.0, 0.0), input.TexCoord, facing);
}

technique
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}
