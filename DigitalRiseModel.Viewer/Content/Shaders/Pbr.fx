float4x4 WorldViewProjection;
float4 BaseColorFactor;

struct VertexInput
{
	float4 Position : POSITION;
};

struct VertexOutput
{
	float4 Position : SV_Position;
};

VertexOutput MainVS(VertexInput input)
{
	VertexOutput output;
	output.Position = mul(input.Position, WorldViewProjection);
	return output;
}

float4 MainPS(VertexOutput input) : SV_Target
{
	return BaseColorFactor;
}

technique
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}
