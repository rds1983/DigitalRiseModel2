using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

/// <summary>
/// Describes sampler settings from a glTF sampler.
/// </summary>
public sealed class DrTextureSampler
{
	/// <summary>
	/// Gets or sets the horizontal wrapping mode.
	/// </summary>
	public TextureAddressMode AddressU { get; set; } = TextureAddressMode.Wrap;

	/// <summary>
	/// Gets or sets the vertical wrapping mode.
	/// </summary>
	public TextureAddressMode AddressV { get; set; } = TextureAddressMode.Wrap;

	/// <summary>
	/// Gets or sets whether nearest-neighbor filtering is requested.
	/// The viewer approximates this with texel-center sampling.
	/// </summary>
	public bool PointFiltering { get; set; }
}
