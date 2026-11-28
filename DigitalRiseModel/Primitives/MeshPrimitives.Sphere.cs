using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	/// <summary>
	/// Creates a UV sphere centered on the origin.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="radius">The sphere radius. Must be greater than zero.</param>
	/// <param name="slices">The number of longitudinal subdivisions. Must be at least 3.</param>
	/// <param name="stacks">The number of latitudinal subdivisions. Must be at least 2.</param>
	/// <param name="cullClockwise">
	/// If <see langword="true"/>, emits counter-clockwise front faces for
	/// <see cref="RasterizerState.CullClockwise"/> instead of the default clockwise front faces.
	/// </param>
	/// <returns>The mesh part containing the sphere geometry.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="radius"/> is not positive, <paramref name="slices"/> is less than 3 or
	/// <paramref name="stacks"/> is less than 2.
	/// </exception>
	public static DrModelMeshPart CreateSphere(
		GraphicsDevice graphicsDevice,
		float radius,
		int slices = 32,
		int stacks = 16,
		bool cullClockwise = false)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		MeshBuilder<VertexPositionNormalTexture> builder = CreateBuilder(cullClockwise);
		BuildSphere(builder, radius, slices, stacks);

		return builder.CreateMeshPart(graphicsDevice);
	}

	/// <summary>
	/// Creates a UV sphere centered on the origin as a <see cref="PrimitiveType.LineList"/>
	/// wireframe.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="radius">The sphere radius. Must be greater than zero.</param>
	/// <param name="slices">The number of longitudinal subdivisions. Must be at least 3.</param>
	/// <param name="stacks">The number of latitudinal subdivisions. Must be at least 2.</param>
	/// <returns>
	/// The mesh part containing the sphere edges. It uses the <see cref="VertexPosition"/> layout
	/// and must be drawn with <see cref="PrimitiveType.LineList"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="radius"/> is not positive, <paramref name="slices"/> is less than 3 or
	/// <paramref name="stacks"/> is less than 2.
	/// </exception>
	public static DrModelMeshPart CreateSphereLines(
		GraphicsDevice graphicsDevice,
		float radius,
		int slices = 32,
		int stacks = 16)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		return CreateLinePart(
			graphicsDevice,
			builder => BuildSphere(builder, radius, slices, stacks));
	}

	internal static void BuildSphere(
		MeshBuilder<VertexPositionNormalTexture> builder,
		float radius,
		int slices,
		int stacks)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
		ArgumentOutOfRangeException.ThrowIfLessThan(slices, MinSlices);
		ArgumentOutOfRangeException.ThrowIfLessThan(stacks, MinStacks);

		var rows = new LatheRow[stacks + 1];

		for (int j = 0; j <= stacks; j++)
		{
			float theta = MathF.PI * j / stacks;
			float sin = MathF.Sin(theta);
			float cos = MathF.Cos(theta);

			rows[j] = new LatheRow(radius * cos, radius * sin, sin, cos);
		}

		AddLathe(builder, rows, slices);
	}
}
