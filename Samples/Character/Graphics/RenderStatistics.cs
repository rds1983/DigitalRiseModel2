namespace Character;

/// <summary>
/// Per-frame rendering statistics collected by <see cref="Renderer"/>.
/// </summary>
internal sealed class RenderStatistics
{
	/// <summary>
	/// Gets the number of draw calls issued in the current frame.
	/// </summary>
	public int DrawCalls { get; private set; }

	/// <summary>
	/// Gets the number of triangles submitted in the current frame.
	/// </summary>
	public int Triangles { get; private set; }

	/// <summary>
	/// Clears all counters. Called once at the start of a frame.
	/// </summary>
	public void Reset()
	{
		DrawCalls = 0;
		Triangles = 0;
	}

	/// <summary>
	/// Records one draw call and optionally adds the number of triangles it drew.
	/// </summary>
	public void AddDrawCall(int triangleCount = 0)
	{
		++DrawCalls;
		Triangles += triangleCount;
	}
}
