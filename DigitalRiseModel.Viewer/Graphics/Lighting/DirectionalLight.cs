using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Viewer.Graphics.Lighting;

/// <summary>
/// A directional light used by the viewer's PBR shader.
/// </summary>
internal sealed class DirectionalLight
{
	/// <summary>
	/// Gets or sets the unit vector pointing from the surface toward the light.
	/// </summary>
	public Vector3 Direction { get; set; } = Vector3.UnitY;

	/// <summary>
	/// Gets or sets the linear RGB color of the light.
	/// </summary>
	public Vector3 Color { get; set; } = Vector3.One;

	/// <summary>
	/// Gets or sets the intensity multiplier. A value of zero disables the light.
	/// </summary>
	public float Intensity { get; set; } = 1f;
}
