Texture2D SceneTexture;
sampler2D SceneSampler = sampler_state
{
	Texture = <SceneTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

float Exposure;
int DebugView;

struct VertexInput
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
};

struct VertexOutput
{
	float4 Position : SV_POSITION;
	float2 TexCoord : TEXCOORD0;
};

VertexOutput MainVS(VertexInput input)
{
	VertexOutput output;
	output.Position = input.Position;
	output.TexCoord = input.TexCoord;
	return output;
}

float3 ACESFilm(float3 color)
{
	return saturate((color * (2.51 * color + 0.03))
		/ (color * (2.43 * color + 0.59) + 0.14));
}

float4 MainPS(VertexOutput input) : SV_TARGET
{
	float3 color = tex2D(SceneSampler, input.TexCoord).rgb;

	if (DebugView == 1 || DebugView == 6)
	{
		return float4(pow(saturate(color), 1.0 / 2.2), 1.0);
	}
	else if (DebugView >= 2 && DebugView <= 5)
	{
		return float4(saturate(color), 1.0);
	}
	else if (DebugView >= 7)
	{
		return float4(pow(saturate(color * Exposure), 1.0 / 2.2), 1.0);
	}

	color = ACESFilm(color * Exposure);
	return float4(pow(saturate(color), 1.0 / 2.2), 1.0);
}

technique
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}
