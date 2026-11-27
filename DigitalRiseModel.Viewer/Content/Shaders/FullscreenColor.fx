struct VertexInput
{
	float4 Position : POSITION;
	float4 Color : COLOR0;
};

struct VertexOutput
{
	float4 Position : SV_Position;
	float4 Color : COLOR0;
};

VertexOutput MainVS(VertexInput input)
{
	VertexOutput output;
	output.Position = input.Position;
	output.Color = input.Color;
	return output;
}

float4 MainPS(VertexOutput input) : SV_Target
{
	return input.Color;
}

technique
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}
