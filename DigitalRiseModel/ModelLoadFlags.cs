namespace DigitalRiseModel;

/// <summary>
/// Controls how model GPU resources are created during loading.
/// </summary>
[Flags]
public enum ModelLoadFlags
{
	/// <summary>
	/// Use default loading behavior. Buffers are created as write-only for rendering.
	/// </summary>
	None = 0,

	/// <summary>
	/// Create vertex and index buffers with read access enabled so their data can be
	/// retrieved through <c>GetData</c>. Intended for debugging and testing.
	/// </summary>
	ReadableBuffers = 1,

	/// <summary>
	/// Keep a CPU-side copy of each mesh part's source geometry in
	/// <see cref="DrModelMeshPart.SourceIndices"/> and
	/// <see cref="DrModelMeshPart.SourceVertices"/>. Intended for debugging and testing.
	/// </summary>
	KeepSourceGeometry = 2
}
