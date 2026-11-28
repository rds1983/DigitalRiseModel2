using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Viewer.Graphics.Lighting;

/// <summary>
/// Packs directional lights into shader constant arrays.
/// </summary>
internal static class LightPacker
{
	/// <summary>
	/// Packs the lights into the supplied arrays and returns the active light count.
	/// Directions are normalized, intensity is stored in <c>W</c>, and color in RGB.
	/// </summary>
	public static int Pack(
		LightSet lights,
		Vector4[] directions,
		Vector4[] colors)
	{
		ArgumentNullException.ThrowIfNull(lights);
		ArgumentNullException.ThrowIfNull(directions);
		ArgumentNullException.ThrowIfNull(colors);

		if (directions.Length < LightSet.MaxDirectionalLights)
		{
			throw new ArgumentException(
				$"The direction array must hold at least {LightSet.MaxDirectionalLights} entries.",
				nameof(directions));
		}

		if (colors.Length < LightSet.MaxDirectionalLights)
		{
			throw new ArgumentException(
				$"The color array must hold at least {LightSet.MaxDirectionalLights} entries.",
				nameof(colors));
		}

		int count = 0;

		foreach (DirectionalLight light in lights.DirectionalLights)
		{
			if (count >= LightSet.MaxDirectionalLights)
			{
				break;
			}

			if (light.Intensity <= 0f)
			{
				continue;
			}

			Vector3 direction = light.Direction;

			if (direction.LengthSquared() < 1e-8f)
			{
				continue;
			}

			direction.Normalize();
			directions[count] = new Vector4(direction, light.Intensity);
			colors[count] = new Vector4(light.Color, 0f);
			++count;
		}

		for (int i = count; i < LightSet.MaxDirectionalLights; ++i)
		{
			directions[i] = Vector4.Zero;
			colors[i] = Vector4.Zero;
		}

		return count;
	}
}
