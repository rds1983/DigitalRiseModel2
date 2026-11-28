using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	/// <summary>
	/// Creates a cone aligned with the Y axis, centered on the origin, with the apex at the
	/// top and a flat base.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="radius">The base radius. Must be greater than zero.</param>
	/// <param name="height">The full height along Y. Must be greater than zero.</param>
	/// <param name="slices">The number of radial subdivisions. Must be at least 3.</param>
	/// <param name="cullClockwise">
	/// If <see langword="true"/>, emits counter-clockwise front faces for
	/// <see cref="RasterizerState.CullClockwise"/> instead of the default clockwise front faces.
	/// </param>
	/// <returns>The mesh part containing the cone geometry.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A size is not positive or <paramref name="slices"/> is less than 3.
	/// </exception>
	public static DrModelMeshPart CreateCone(
		GraphicsDevice graphicsDevice,
		float radius,
		float height,
		int slices = 32,
		bool cullClockwise = false)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		MeshBuilder<VertexPositionNormalTexture> builder = CreateBuilder(cullClockwise);
		BuildCone(builder, radius, height, slices);

		return builder.CreateMeshPart(graphicsDevice);
	}

	internal static void BuildCone(
		MeshBuilder<VertexPositionNormalTexture> builder,
		float radius,
		float height,
		int slices)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentOutOfRangeException.ThrowIfLessThan(slices, MinSlices);

		float halfHeight = height * 0.5f;

		// The lateral normal is proportional to (height * cos, radius, height * sin).
		LatheRow[] rows =
		[
			new LatheRow(halfHeight, 0f, height, radius),
			new LatheRow(-halfHeight, radius, height, radius)
		];

		AddLathe(builder, rows, slices);
		AddCap(builder, -halfHeight, radius, -1f, slices);
	}
}
