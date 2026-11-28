using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	/// <summary>
	/// Creates an axis-aligned box centered on the origin with per-face normals and texture
	/// coordinates.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <param name="size">The full extents along each axis. Every component must be greater than zero.</param>
	/// <param name="cullClockwise">
	/// If <see langword="true"/>, emits counter-clockwise front faces for
	/// <see cref="RasterizerState.CullClockwise"/> instead of the default clockwise front faces.
	/// </param>
	/// <returns>The mesh part containing 24 vertices and 12 triangles.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">A size component is not positive.</exception>
	public static DrModelMeshPart CreateBox(
		GraphicsDevice graphicsDevice,
		Vector3 size,
		bool cullClockwise = false)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		MeshBuilder<VertexPositionNormalTexture> builder = CreateBuilder(cullClockwise);
		BuildBox(builder, size);

		return builder.CreateMeshPart(graphicsDevice);
	}

	internal static void BuildBox(
		MeshBuilder<VertexPositionNormalTexture> builder,
		Vector3 size)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.X);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.Y);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.Z);

		float hx = size.X * 0.5f;
		float hy = size.Y * 0.5f;
		float hz = size.Z * 0.5f;

		AddQuadFace(
			builder,
			Vector3.UnitZ,
			new Vector3(-hx, -hy, hz),
			new Vector3(hx, -hy, hz),
			new Vector3(hx, hy, hz),
			new Vector3(-hx, hy, hz));

		AddQuadFace(
			builder,
			-Vector3.UnitZ,
			new Vector3(hx, -hy, -hz),
			new Vector3(-hx, -hy, -hz),
			new Vector3(-hx, hy, -hz),
			new Vector3(hx, hy, -hz));

		AddQuadFace(
			builder,
			Vector3.UnitX,
			new Vector3(hx, -hy, hz),
			new Vector3(hx, -hy, -hz),
			new Vector3(hx, hy, -hz),
			new Vector3(hx, hy, hz));

		AddQuadFace(
			builder,
			-Vector3.UnitX,
			new Vector3(-hx, -hy, -hz),
			new Vector3(-hx, -hy, hz),
			new Vector3(-hx, hy, hz),
			new Vector3(-hx, hy, -hz));

		AddQuadFace(
			builder,
			Vector3.UnitY,
			new Vector3(-hx, hy, hz),
			new Vector3(hx, hy, hz),
			new Vector3(hx, hy, -hz),
			new Vector3(-hx, hy, -hz));

		AddQuadFace(
			builder,
			-Vector3.UnitY,
			new Vector3(-hx, -hy, -hz),
			new Vector3(hx, -hy, -hz),
			new Vector3(hx, -hy, hz),
			new Vector3(-hx, -hy, hz));
	}

	private static void AddQuadFace(
		MeshBuilder<VertexPositionNormalTexture> builder,
		Vector3 normal,
		Vector3 bottomLeft,
		Vector3 bottomRight,
		Vector3 topRight,
		Vector3 topLeft)
	{
		int start = builder.VertexCount;

		builder.AddVertex(new VertexPositionNormalTexture(
			bottomLeft, normal, new Vector2(0f, 1f)));
		builder.AddVertex(new VertexPositionNormalTexture(
			bottomRight, normal, new Vector2(1f, 1f)));
		builder.AddVertex(new VertexPositionNormalTexture(
			topRight, normal, new Vector2(1f, 0f)));
		builder.AddVertex(new VertexPositionNormalTexture(
			topLeft, normal, new Vector2(0f, 0f)));

		builder.AddQuad(start, start + 1, start + 2, start + 3);
	}
}
