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

float4 FxaaPS(VertexOutput input) : SV_TARGET
{
	float2 texel = TexelSize;
	float3 lumaWeights = float3(0.299, 0.587, 0.114);

	float3 rgbM = tex2D(SceneSampler, input.TexCoord).rgb;
	float3 rgbNW = tex2D(SceneSampler, input.TexCoord + float2(-1.0, -1.0) * texel).rgb;
	float3 rgbNE = tex2D(SceneSampler, input.TexCoord + float2(1.0, -1.0) * texel).rgb;
	float3 rgbSW = tex2D(SceneSampler, input.TexCoord + float2(-1.0, 1.0) * texel).rgb;
	float3 rgbSE = tex2D(SceneSampler, input.TexCoord + float2(1.0, 1.0) * texel).rgb;

	float lumaM = dot(rgbM, lumaWeights);
	float lumaNW = dot(rgbNW, lumaWeights);
	float lumaNE = dot(rgbNE, lumaWeights);
	float lumaSW = dot(rgbSW, lumaWeights);
	float lumaSE = dot(rgbSE, lumaWeights);

	float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
	float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

	if (lumaMax - lumaMin < max(0.0312, lumaMax * 0.125))
	{
		return float4(rgbM, 1.0);
	}

	float2 direction;
	direction.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
	direction.y = (lumaNW + lumaSW) - (lumaNE + lumaSE);

	float directionReduce = max(
		(lumaNW + lumaNE + lumaSW + lumaSE) * 0.03125,
		1.0 / 128.0);
	float reciprocalMin = 1.0 / (min(abs(direction.x), abs(direction.y))
		+ directionReduce);
	direction = min(
		float2(8.0, 8.0),
		max(float2(-8.0, -8.0), direction * reciprocalMin)) * texel;

	float3 rgbA = 0.5 * (
		tex2D(SceneSampler, input.TexCoord + direction * (1.0 / 3.0 - 0.5)).rgb
		+ tex2D(SceneSampler, input.TexCoord + direction * (2.0 / 3.0 - 0.5)).rgb);
	float3 rgbB = rgbA * 0.5 + 0.25 * (
		tex2D(SceneSampler, input.TexCoord - direction * 0.5).rgb
		+ tex2D(SceneSampler, input.TexCoord + direction * 0.5).rgb);
	float lumaB = dot(rgbB, lumaWeights);

	return float4(
		(lumaB < lumaMin || lumaB > lumaMax) ? rgbA : rgbB,
		1.0);
}

technique Fxaa
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 FxaaPS();
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
