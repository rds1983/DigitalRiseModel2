using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	/// <summary>
	/// Creates a cylinder aligned with the Y axis, centered on the origin, with flat caps.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="radius">The radius. Must be greater than zero.</param>
	/// <param name="height">The full height along Y. Must be greater than zero.</param>
	/// <param name="slices">The number of radial subdivisions. Must be at least 3.</param>
	/// <param name="cullClockwise">
	/// If <see langword="true"/>, emits counter-clockwise front faces for
	/// <see cref="RasterizerState.CullClockwise"/> instead of the default clockwise front faces.
	/// </param>
	/// <returns>The mesh part containing the cylinder geometry.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A size is not positive or <paramref name="slices"/> is less than 3.
	/// </exception>
	public static DrModelMeshPart CreateCylinder(
		GraphicsDevice graphicsDevice,
		float radius,
		float height,
		int slices = 32,
		bool cullClockwise = false)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		MeshBuilder<VertexPositionNormalTexture> builder = CreateBuilder(cullClockwise);
		BuildCylinder(builder, radius, height, slices);

		return builder.CreateMeshPart(graphicsDevice);
	}

	/// <summary>
	/// Creates a cylinder aligned with the Y axis, centered on the origin, as a
	/// <see cref="PrimitiveType.LineList"/> wireframe with flat caps.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="radius">The radius. Must be greater than zero.</param>
	/// <param name="height">The full height along Y. Must be greater than zero.</param>
	/// <param name="slices">The number of radial subdivisions. Must be at least 3.</param>
	/// <returns>
	/// The mesh part containing the cylinder edges. It uses the <see cref="VertexPosition"/>
	/// layout and must be drawn with <see cref="PrimitiveType.LineList"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A size is not positive or <paramref name="slices"/> is less than 3.
	/// </exception>
	public static DrModelMeshPart CreateCylinderLines(
		GraphicsDevice graphicsDevice,
		float radius,
		float height,
		int slices = 32)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		return CreateLinePart(
			graphicsDevice,
			builder => BuildCylinder(builder, radius, height, slices));
	}

	internal static void BuildCylinder(
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

		LatheRow[] rows =
		[
			new LatheRow(halfHeight, radius, 1f, 0f),
			new LatheRow(-halfHeight, radius, 1f, 0f)
		];

		AddLathe(builder, rows, slices);
		AddCap(builder, halfHeight, radius, 1f, slices);
		AddCap(builder, -halfHeight, radius, -1f, slices);
	}
}
