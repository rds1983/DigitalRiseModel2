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
}
