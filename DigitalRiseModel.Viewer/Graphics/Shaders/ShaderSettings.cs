namespace DigitalRiseModel.Viewer.Graphics.Shaders;

internal sealed class ShaderSettings
{
	public ShaderDebugView DebugView { get; set; }

	public float Exposure { get; set; } = 1f;

	/// <summary>
	/// Gets or sets the bloom contribution added before tone mapping.
	/// Zero disables bloom.
	/// </summary>
	public float BloomIntensity { get; set; }

	/// <summary>
	/// Gets or sets whether FXAA is applied to the shaded output.
	/// </summary>
	public bool EnableFxaa { get; set; } = true;

	/// <summary>
	/// Gets or sets whether mesh part bounding spheres are drawn as wireframes.
	/// </summary>
	public bool ShowBoundingSpheres { get; set; }

	/// <summary>
	/// Gets or sets whether the bone hierarchy is drawn as a skeleton overlay.
	/// </summary>
	public bool ShowSkeleton { get; set; }
}
