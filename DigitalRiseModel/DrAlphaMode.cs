namespace DigitalRiseModel;

/// <summary>
/// Defines how the alpha channel of a material is interpreted.
/// </summary>
public enum DrAlphaMode
{
	/// <summary>The alpha channel is ignored and the material is opaque.</summary>
	Opaque,

	/// <summary>Fragments below the alpha cutoff are discarded.</summary>
	Mask,

	/// <summary>Fragments are blended using their alpha value.</summary>
	Blend
}
