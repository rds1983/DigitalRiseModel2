using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	/// <summary>
	/// Creates a rectangle in the XZ plane facing +Y, centered on the origin.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="width">The size along the X axis. Must be greater than zero.</param>
	/// <param name="depth">The size along the Z axis. Must be greater than zero.</param>
	/// <param name="segmentsX">The number of subdivisions along X. Must be at least 1.</param>
	/// <param name="segmentsZ">The number of subdivisions along Z. Must be at least 1.</param>
	/// <param name="cullClockwise">
	/// If <see langword="true"/>, emits counter-clockwise front faces for
	/// <see cref="RasterizerState.CullClockwise"/> instead of the default clockwise front faces.
	/// </param>
	/// <returns>The mesh part containing the plane geometry.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A size is not positive or a segment count is less than 1.
	/// </exception>
	public static DrModelMeshPart CreatePlane(
		GraphicsDevice graphicsDevice,
		float width,
		float depth,
		int segmentsX = 1,
		int segmentsZ = 1,
		bool cullClockwise = false)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		MeshBuilder<VertexPositionNormalTexture> builder = CreateBuilder(cullClockwise);
		BuildPlane(builder, width, depth, segmentsX, segmentsZ);

		return builder.CreateMeshPart(graphicsDevice);
	}

	internal static void BuildPlane(
		MeshBuilder<VertexPositionNormalTexture> builder,
		float width,
		float depth,
		int segmentsX,
		int segmentsZ)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);
		ArgumentOutOfRangeException.ThrowIfLessThan(segmentsX, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(segmentsZ, 1);

		int start = builder.VertexCount;

		for (int z = 0; z <= segmentsZ; z++)
		{
			float v = (float)z / segmentsZ;

			for (int x = 0; x <= segmentsX; x++)
			{
				float u = (float)x / segmentsX;
				var position = new Vector3(
					(u - 0.5f) * width,
					0f,
					(v - 0.5f) * depth);

				builder.AddVertex(new VertexPositionNormalTexture(
					position,
					Vector3.Up,
					new Vector2(u, v)));
			}
		}

		int rowSize = segmentsX + 1;

		for (int z = 0; z < segmentsZ; z++)
		{
			for (int x = 0; x < segmentsX; x++)
			{
				int a = start + z * rowSize + x;
				int b = a + 1;
				int c = a + rowSize + 1;
				int d = a + rowSize;

				builder.AddQuad(a, d, c, b);
			}
		}
	}
}
