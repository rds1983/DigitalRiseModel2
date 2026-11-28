namespace DigitalRiseModel.Viewer.Graphics;

/// <summary>
/// Per-frame rendering statistics. Both the scene renderer and the post-processor
/// update the same instance so draw calls include model and full-screen passes.
/// </summary>
internal sealed class RenderStatistics
{
	/// <summary>
	/// Number of draw calls issued in the current frame.
	/// </summary>
	public int DrawCalls { get; private set; }

	/// <summary>
	/// Number of model triangles submitted in the current frame. Full-screen
	/// post-processing passes do not contribute triangles.
	/// </summary>
	public int Triangles { get; private set; }

	/// <summary>
	/// Clears all counters. Call once at the start of a frame.
	/// </summary>
	public void Reset()
	{
		DrawCalls = 0;
		Triangles = 0;
	}

	/// <summary>
	/// Records one draw call and optionally adds the number of triangles it drew.
	/// </summary>
	/// <param name="triangleCount">
	/// Number of triangles in the draw, or zero for full-screen passes.
	/// </param>
	public void AddDrawCall(int triangleCount = 0)
	{
		++DrawCalls;
		Triangles += triangleCount;
	}
}
