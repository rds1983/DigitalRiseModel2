using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	/// <summary>
	/// Creates a capsule aligned with the Y axis, centered on the origin, made from a
	/// cylinder and two hemispherical caps.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="radius">The radius of the cylinder and caps. Must be greater than zero.</param>
	/// <param name="height">
	/// The total height along Y, including both caps. Must be at least <c>2 * radius</c>.
	/// </param>
	/// <param name="slices">The number of radial subdivisions. Must be at least 3.</param>
	/// <param name="stacks">The number of subdivisions per hemispherical cap. Must be at least 2.</param>
	/// <param name="cullClockwise">
	/// If <see langword="true"/>, emits counter-clockwise front faces for
	/// <see cref="RasterizerState.CullClockwise"/> instead of the default clockwise front faces.
	/// </param>
	/// <returns>The mesh part containing the capsule geometry.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="radius"/> is not positive, <paramref name="slices"/> is less than 3,
	/// <paramref name="stacks"/> is less than 2 or <paramref name="height"/> is less than
	/// <c>2 * radius</c>.
	/// </exception>
	public static DrModelMeshPart CreateCapsule(
		GraphicsDevice graphicsDevice,
		float radius,
		float height,
		int slices = 32,
		int stacks = 16,
		bool cullClockwise = false)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		MeshBuilder<VertexPositionNormalTexture> builder = CreateBuilder(cullClockwise);
		BuildCapsule(builder, radius, height, slices, stacks);

		return builder.CreateMeshPart(graphicsDevice);
	}

	internal static void BuildCapsule(
		MeshBuilder<VertexPositionNormalTexture> builder,
		float radius,
		float height,
		int slices,
		int stacks)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, radius * 2f);
		ArgumentOutOfRangeException.ThrowIfLessThan(slices, MinSlices);
		ArgumentOutOfRangeException.ThrowIfLessThan(stacks, MinStacks);

		float halfHeight = height * 0.5f;
		float cylinderHalfHeight = halfHeight - radius;
		var rows = new List<LatheRow>(2 * stacks + 2);

		for (int j = 0; j <= stacks; j++)
		{
			float theta = MathF.PI * 0.5f * j / stacks;
			float sin = MathF.Sin(theta);
			float cos = MathF.Cos(theta);

			rows.Add(new LatheRow(
				cylinderHalfHeight + radius * cos,
				radius * sin,
				sin,
				cos));
		}

		// When the caps meet directly, skip the duplicate equator row of the bottom hemisphere.
		int firstBottomRow = cylinderHalfHeight > 0f ? 0 : 1;

		for (int j = firstBottomRow; j <= stacks; j++)
		{
			float theta = MathF.PI * 0.5f + MathF.PI * 0.5f * j / stacks;
			float sin = MathF.Sin(theta);
			float cos = MathF.Cos(theta);

			rows.Add(new LatheRow(
				-cylinderHalfHeight + radius * cos,
				radius * sin,
				sin,
				cos));
		}

		AddLathe(builder, rows, slices);
	}
}
