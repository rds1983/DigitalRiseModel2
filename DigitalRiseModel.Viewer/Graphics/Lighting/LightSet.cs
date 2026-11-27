using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Viewer.Graphics.Lighting;

/// <summary>
/// Directional lights and hemisphere ambient colors used by the PBR shader.
/// </summary>
internal sealed class LightSet
{
	/// <summary>
	/// Maximum number of directional lights packed into shader constants.
	/// </summary>
	public const int MaxDirectionalLights = 4;

	private readonly List<DirectionalLight> _directionalLights = [];

	/// <summary>
	/// Gets the directional lights in the order they are packed into the shader.
	/// </summary>
	public IReadOnlyList<DirectionalLight> DirectionalLights => _directionalLights;

	/// <summary>
	/// Gets or sets the linear ambient color for surfaces facing up.
	/// </summary>
	public Vector3 SkyColor { get; set; } = new(0.18f, 0.22f, 0.30f);

	/// <summary>
	/// Gets or sets the linear ambient color for surfaces facing down.
	/// </summary>
	public Vector3 GroundColor { get; set; } = new(0.04f, 0.035f, 0.03f);

	/// <summary>
	/// Adds a directional light.
	/// </summary>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="light"/> is <see langword="null"/>.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// The set already contains <see cref="MaxDirectionalLights"/> lights.
	/// </exception>
	public void Add(DirectionalLight light)
	{
		ArgumentNullException.ThrowIfNull(light);

		if (_directionalLights.Count >= MaxDirectionalLights)
		{
			throw new InvalidOperationException(
				$"A light set can contain at most {MaxDirectionalLights} directional lights.");
		}

		_directionalLights.Add(light);
	}

	/// <summary>
	/// Creates the fixed three-point studio rig used for model viewing.
	/// </summary>
	public static LightSet CreateStudio()
	{
		var lights = new LightSet
		{
			SkyColor = new Vector3(0.14f, 0.17f, 0.23f),
			GroundColor = new Vector3(0.03f, 0.028f, 0.026f)
		};

		lights.Add(new DirectionalLight
		{
			Direction = Vector3.Normalize(new Vector3(0.45f, 0.8f, 0.65f)),
			Color = new Vector3(1f, 0.95f, 0.88f),
			Intensity = 1.8f
		});

		lights.Add(new DirectionalLight
		{
			Direction = Vector3.Normalize(new Vector3(-0.75f, 0.35f, 0.4f)),
			Color = new Vector3(0.55f, 0.65f, 0.9f),
			Intensity = 0.55f
		});

		lights.Add(new DirectionalLight
		{
			Direction = Vector3.Normalize(new Vector3(0.1f, 0.45f, -0.9f)),
			Color = new Vector3(0.75f, 0.82f, 1f),
			Intensity = 0.9f
		});

		return lights;
	}
}
