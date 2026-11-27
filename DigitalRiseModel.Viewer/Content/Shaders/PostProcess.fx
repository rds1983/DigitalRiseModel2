Texture2D SceneTexture;
sampler2D SceneSampler = sampler_state
{
	Texture = <SceneTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

Texture2D BloomTexture;
sampler2D BloomSampler = sampler_state
{
	Texture = <BloomTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

float Exposure;
float BloomIntensity;
float2 TexelSize;
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

float4 BrightPassPS(VertexOutput input) : SV_TARGET
{
	float3 color = tex2D(SceneSampler, input.TexCoord).rgb;
	float brightness = max(color.r, max(color.g, color.b));
	float contribution = max(brightness - 1.0, 0.0) / max(brightness, 1e-4);
	return float4(color * contribution, 1.0);
}

float4 BlurHorizontalPS(VertexOutput input) : SV_TARGET
{
	float2 offset = float2(TexelSize.x, 0.0);
	float3 color = tex2D(SceneSampler, input.TexCoord).rgb * 0.4;
	color += (tex2D(SceneSampler, input.TexCoord + offset).rgb
		+ tex2D(SceneSampler, input.TexCoord - offset).rgb) * 0.24;
	color += (tex2D(SceneSampler, input.TexCoord + offset * 2.0).rgb
		+ tex2D(SceneSampler, input.TexCoord - offset * 2.0).rgb) * 0.06;
	return float4(color, 1.0);
}

float4 BlurVerticalPS(VertexOutput input) : SV_TARGET
{
	float2 offset = float2(0.0, TexelSize.y);
	float3 color = tex2D(SceneSampler, input.TexCoord).rgb * 0.4;
	color += (tex2D(SceneSampler, input.TexCoord + offset).rgb
		+ tex2D(SceneSampler, input.TexCoord - offset).rgb) * 0.24;
	color += (tex2D(SceneSampler, input.TexCoord + offset * 2.0).rgb
		+ tex2D(SceneSampler, input.TexCoord - offset * 2.0).rgb) * 0.06;
	return float4(color, 1.0);
}

float4 CompositePS(VertexOutput input) : SV_TARGET
{
	float3 color = tex2D(SceneSampler, input.TexCoord).rgb;

	if (DebugView == 0)
	{
		float3 bloom = tex2D(BloomSampler, input.TexCoord).rgb;
		color += bloom * BloomIntensity;
		color = ACESFilm(color * Exposure);
		return float4(pow(saturate(color), 1.0 / 2.2), 1.0);
	}
	else if (DebugView == 1 || DebugView == 6)
	{
		return float4(pow(saturate(color), 1.0 / 2.2), 1.0);
	}
	else if (DebugView >= 2 && DebugView <= 5)
	{
		return float4(saturate(color), 1.0);
	}
	else
	{
		return float4(pow(saturate(color * Exposure), 1.0 / 2.2), 1.0);
	}
}

technique BrightPass
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 BrightPassPS();
	}
}

technique BlurHorizontal
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 BlurHorizontalPS();
	}
}

technique BlurVertical
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 BlurVerticalPS();
	}
}

technique Composite
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 CompositePS();
	}
}
