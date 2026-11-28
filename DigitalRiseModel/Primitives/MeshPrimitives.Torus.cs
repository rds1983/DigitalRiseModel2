using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	/// <summary>
	/// Creates a torus lying in the XZ plane and centered on the origin.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="majorRadius">
	/// The distance from the origin to the center of the tube. Must be greater than zero.
	/// </param>
	/// <param name="minorRadius">The tube radius. Must be greater than zero.</param>
	/// <param name="majorSlices">The number of subdivisions around the Y axis. Must be at least 3.</param>
	/// <param name="minorSlices">The number of subdivisions around the tube. Must be at least 3.</param>
	/// <param name="cullClockwise">
	/// If <see langword="true"/>, emits counter-clockwise front faces for
	/// <see cref="RasterizerState.CullClockwise"/> instead of the default clockwise front faces.
	/// </param>
	/// <returns>The mesh part containing the torus geometry.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A radius is not positive or a slice count is less than 3.
	/// </exception>
	public static DrModelMeshPart CreateTorus(
		GraphicsDevice graphicsDevice,
		float majorRadius,
		float minorRadius,
		int majorSlices = 32,
		int minorSlices = 16,
		bool cullClockwise = false)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		MeshBuilder<VertexPositionNormalTexture> builder = CreateBuilder(cullClockwise);
		BuildTorus(builder, majorRadius, minorRadius, majorSlices, minorSlices);

		return builder.CreateMeshPart(graphicsDevice);
	}

	/// <summary>
	/// Creates a torus lying in the XZ plane and centered on the origin as a
	/// <see cref="PrimitiveType.LineList"/> wireframe.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="majorRadius">
	/// The distance from the origin to the center of the tube. Must be greater than zero.
	/// </param>
	/// <param name="minorRadius">The tube radius. Must be greater than zero.</param>
	/// <param name="majorSlices">The number of subdivisions around the Y axis. Must be at least 3.</param>
	/// <param name="minorSlices">The number of subdivisions around the tube. Must be at least 3.</param>
	/// <returns>
	/// The mesh part containing the torus edges. It uses the <see cref="VertexPosition"/> layout
	/// and must be drawn with <see cref="PrimitiveType.LineList"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A radius is not positive or a slice count is less than 3.
	/// </exception>
	public static DrModelMeshPart CreateTorusLines(
		GraphicsDevice graphicsDevice,
		float majorRadius,
		float minorRadius,
		int majorSlices = 32,
		int minorSlices = 16)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		return CreateLinePart(
			graphicsDevice,
			builder => BuildTorus(builder, majorRadius, minorRadius, majorSlices, minorSlices));
	}

	internal static void BuildTorus(
		MeshBuilder<VertexPositionNormalTexture> builder,
		float majorRadius,
		float minorRadius,
		int majorSlices,
		int minorSlices)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(majorRadius);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minorRadius);
		ArgumentOutOfRangeException.ThrowIfLessThan(majorSlices, MinSlices);
		ArgumentOutOfRangeException.ThrowIfLessThan(minorSlices, MinSlices);

		int start = builder.VertexCount;
		int rowSize = minorSlices + 1;

		for (int i = 0; i <= majorSlices; i++)
		{
			float u = MathF.Tau * i / majorSlices;
			float cosU = MathF.Cos(u);
			float sinU = MathF.Sin(u);

			for (int j = 0; j <= minorSlices; j++)
			{
				float v = MathF.Tau * j / minorSlices;
				float cosV = MathF.Cos(v);
				float sinV = MathF.Sin(v);
				float ring = majorRadius + minorRadius * cosV;

				var position = new Vector3(ring * cosU, minorRadius * sinV, ring * sinU);
				var normal = new Vector3(cosV * cosU, sinV, cosV * sinU);

				builder.AddVertex(new VertexPositionNormalTexture(
					position,
					normal,
					new Vector2((float)i / majorSlices, (float)j / minorSlices)));
			}
		}

		for (int i = 0; i < majorSlices; i++)
		{
			for (int j = 0; j < minorSlices; j++)
			{
				int a = start + i * rowSize + j;
				int b = a + 1;
				int c = a + rowSize + 1;
				int d = a + rowSize;

				builder.AddQuad(a, b, c, d);
			}
		}
	}
}
