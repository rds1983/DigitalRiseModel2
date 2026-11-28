using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Tests;

public sealed class MeshPrimitivesLinesTests
{
	private const float Tolerance = 1e-4f;

	[Fact]
	public void AddWireframe_EmptySource_ProducesEmptyBuilder()
	{
		var builder = CreateLineBuilder();

		MeshPrimitives.AddWireframe(builder, [], [], null);

		Assert.Equal(0, builder.VertexCount);
		Assert.Equal(0, builder.IndexCount);
	}

	[Fact]
	public void AddWireframe_SingleTriangle_ProducesThreeEdges()
	{
		var source = new[]
		{
			CreateSourceVertex(Vector3.Zero),
			CreateSourceVertex(Vector3.UnitX),
			CreateSourceVertex(Vector3.UnitY)
		};
		var builder = CreateLineBuilder();

		MeshPrimitives.AddWireframe(builder, source, new[] { 0, 1, 2 }, null);

		Assert.Equal(3, builder.VertexCount);
		Assert.Equal(6, builder.IndexCount);
		Assert.Equal(3, builder.IndexCount / 2);
	}

	[Fact]
	public void AddWireframe_TwoTrianglesSharingAnEdge_DeduplicatesTheSharedEdge()
	{
		// A quad split into two triangles shares one diagonal edge.
		var source = new[]
		{
			CreateSourceVertex(Vector3.Zero),
			CreateSourceVertex(Vector3.UnitX),
			CreateSourceVertex(new Vector3(1f, 1f, 0f)),
			CreateSourceVertex(Vector3.UnitY)
		};
		var builder = CreateLineBuilder();

		MeshPrimitives.AddWireframe(builder, source, new[] { 0, 1, 2, 0, 2, 3 }, null);

		Assert.Equal(4, builder.VertexCount);
		Assert.Equal(5, builder.IndexCount / 2);
	}

	[Fact]
	public void AddWireframe_QuadDiagonals_AreSkipped()
	{
		// Same quad as above, but the diagonal 0-2 is reported as an internal quad diagonal.
		var source = new[]
		{
			CreateSourceVertex(Vector3.Zero),
			CreateSourceVertex(Vector3.UnitX),
			CreateSourceVertex(new Vector3(1f, 1f, 0f)),
			CreateSourceVertex(Vector3.UnitY)
		};
		var builder = CreateLineBuilder();

		MeshPrimitives.AddWireframe(
			builder,
			source,
			new[] { 0, 1, 2, 0, 2, 3 },
			new[] { (0, 2) });

		Assert.Equal(4, builder.VertexCount);
		Assert.Equal(4, builder.IndexCount / 2);
	}

	[Fact]
	public void AddWireframe_QuadDiagonals_WeldsSharedPositions()
	{
		var source = new[]
		{
			CreateSourceVertex(Vector3.Zero),
			CreateSourceVertex(Vector3.UnitX),
			CreateSourceVertex(new Vector3(1f, 1f, 0f)),
			CreateSourceVertex(Vector3.UnitY)
		};
		var builder = CreateLineBuilder();

		MeshPrimitives.AddWireframe(
			builder,
			source,
			new[] { 0, 1, 2, 0, 2, 3 },
			new[] { (0, 2) });

		Assert.Single(builder.Vertices.Where(vertex => vertex.Position == Vector3.Zero));
	}

	[Fact]
	public void AddWireframe_WeldsSharedPositionsWithDifferentNormals()
	{
		// Two triangles meet at a corner expressed with different normals, like a box corner.
		var source = new[]
		{
			CreateSourceVertex(Vector3.Zero),
			CreateSourceVertex(Vector3.UnitX),
			CreateSourceVertex(Vector3.UnitY),
			CreateSourceVertex(new Vector3(0f, 0f, 1f))
		};
		var builder = CreateLineBuilder();

		MeshPrimitives.AddWireframe(builder, source, new[] { 0, 1, 2, 3, 1, 2 }, null);

		Assert.Contains(builder.Vertices, vertex => vertex.Position == Vector3.Zero);
		Assert.Single(
			builder.Vertices.Where(vertex => vertex.Position == Vector3.Zero));
	}

	[Fact]
	public void AddWireframe_DegenerateTriangle_ProducesNoZeroLengthLines()
	{
		var source = new[]
		{
			CreateSourceVertex(Vector3.Zero),
			CreateSourceVertex(Vector3.Zero),
			CreateSourceVertex(Vector3.UnitX)
		};
		var builder = CreateLineBuilder();

		MeshPrimitives.AddWireframe(builder, source, new[] { 0, 1, 2 }, null);

		for (int i = 0; i + 1 < builder.IndexCount; i += 2)
		{
			Vector3 a = builder.Vertices[builder.Indices[i]].Position;
			Vector3 b = builder.Vertices[builder.Indices[i + 1]].Position;

			Assert.True(Vector3.DistanceSquared(a, b) > 1e-10f);
		}
	}

	[Fact]
	public void AddWireframe_NullBuilder_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.AddWireframe(null!, [], [], null));
	}

	[Fact]
	public void AddWireframe_NullVertices_ThrowsArgumentNullException()
	{
		var builder = CreateLineBuilder();

		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.AddWireframe(builder, null!, [], null));
	}

	[Fact]
	public void AddWireframe_NullIndices_ThrowsArgumentNullException()
	{
		var builder = CreateLineBuilder();

		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.AddWireframe(builder, [], null!, null));
	}

	[Fact]
	public void AddLine_AppendsIndicesInOrder()
	{
		var builder = CreateLineBuilder();
		builder.AddVertex(new VertexPosition(Vector3.Zero));
		builder.AddVertex(new VertexPosition(Vector3.UnitX));

		builder.AddLine(1, 0);

		Assert.Equal(new[] { 1, 0 }, builder.Indices);
	}

	[Fact]
	public void CreateMeshPart_LineListPart_UsesLinePrimitiveCount()
	{
		var builder = CreateLineBuilder();
		builder.AddVertex(new VertexPosition(Vector3.Zero));
		builder.AddVertex(new VertexPosition(Vector3.UnitX));
		builder.AddVertex(new VertexPosition(Vector3.UnitY));
		builder.AddLine(0, 1);
		builder.AddLine(1, 2);

		Assert.Equal(PrimitiveType.LineList, builder.PrimitiveType);
		Assert.Equal(2, builder.IndexCount / 2);
	}

	[Fact]
	public void CreateMeshPart_DefaultBuilder_UsesTriangleList()
	{
		var builder = new MeshBuilder<VertexPositionNormalTexture>(
			vertex => vertex.Position);

		Assert.Equal(PrimitiveType.TriangleList, builder.PrimitiveType);
	}

	[Fact]
	public void MeshPrimitives_BoxLines_ProducesTwelveEdges()
	{
		var solid = BuildSolid(builder => MeshPrimitives.BuildBox(builder, Vector3.One));
		var lines = BuildWireframe(solid);

		Assert.Equal(8, lines.VertexCount);
		Assert.Equal(12, lines.IndexCount / 2);
	}

	[Fact]
	public void MeshPrimitives_PlaneLines_SingleSegment_ProducesFourEdges()
	{
		var solid = BuildSolid(builder => MeshPrimitives.BuildPlane(builder, 2f, 2f, 1, 1));
		var lines = BuildWireframe(solid);

		Assert.Equal(4, lines.VertexCount);
		Assert.Equal(4, lines.IndexCount / 2);
	}

	[Fact]
	public void MeshPrimitives_LineParts_MatchSolidBounds()
	{
		MeshBuilder<VertexPositionNormalTexture> sphere =
			BuildSolid(builder => MeshPrimitives.BuildSphere(builder, 2f, 16, 8));
		MeshBuilder<VertexPosition> lines = BuildWireframe(sphere);

		BoundingSphere solidBounds = sphere.ComputeBoundingSphere();
		BoundingSphere lineBounds = lines.ComputeBoundingSphere();

		Assert.Equal(solidBounds.Center.X, lineBounds.Center.X, Tolerance);
		Assert.Equal(solidBounds.Center.Y, lineBounds.Center.Y, Tolerance);
		Assert.Equal(solidBounds.Center.Z, lineBounds.Center.Z, Tolerance);
		Assert.Equal(solidBounds.Radius, lineBounds.Radius, Tolerance);
	}

	[Theory]
	[InlineData("plane")]
	[InlineData("box")]
	[InlineData("sphere")]
	[InlineData("cylinder")]
	[InlineData("cone")]
	[InlineData("capsule")]
	[InlineData("torus")]
	public void MeshPrimitives_LineParts_StayOnSolidSurface(string primitive)
	{
		MeshBuilder<VertexPositionNormalTexture> solid = BuildSolid(builder => Build(primitive, builder));
		MeshBuilder<VertexPosition> lines = BuildWireframe(solid);

		Assert.True(lines.VertexCount > 0);
		Assert.True(lines.IndexCount > 0);

		foreach (VertexPosition vertex in lines.Vertices)
		{
			Assert.Contains(
				solid.Vertices,
				candidate => Vector3.DistanceSquared(candidate.Position, vertex.Position) < 1e-8f);
		}
	}

	[Fact]
	public void MeshPrimitives_SphereLines_SkipsQuadDiagonals()
	{
		MeshBuilder<VertexPositionNormalTexture> solid =
			BuildSolid(builder => MeshPrimitives.BuildSphere(builder, 1f, 8, 4));
		MeshBuilder<VertexPosition> withDiagonals = BuildWireframe(solid, includeDiagonals: false);
		MeshBuilder<VertexPosition> withoutSkipping = BuildWireframe(solid, includeDiagonals: true);

		Assert.True(solid.QuadDiagonals.Count > 0);
		Assert.True(withoutSkipping.IndexCount > withDiagonals.IndexCount);
	}

	private static MeshBuilder<VertexPosition> CreateLineBuilder()
	{
		return new MeshBuilder<VertexPosition>(
			vertex => vertex.Position,
			primitiveType: PrimitiveType.LineList);
	}

	private static VertexPositionNormalTexture CreateSourceVertex(Vector3 position)
	{
		return new VertexPositionNormalTexture(position, Vector3.Up, Vector2.Zero);
	}

	private static MeshBuilder<VertexPositionNormalTexture> BuildSolid(
		Action<MeshBuilder<VertexPositionNormalTexture>> build)
	{
		var builder = new MeshBuilder<VertexPositionNormalTexture>(vertex => vertex.Position);
		build(builder);
		return builder;
	}

	private static MeshBuilder<VertexPosition> BuildWireframe(
		MeshBuilder<VertexPositionNormalTexture> solid,
		bool includeDiagonals = false)
	{
		var builder = CreateLineBuilder();
		MeshPrimitives.AddWireframe(
			builder,
			solid.Vertices,
			solid.Indices,
			includeDiagonals ? null : solid.QuadDiagonals);
		return builder;
	}

	private static void Build(
		string primitive,
		MeshBuilder<VertexPositionNormalTexture> builder)
	{
		switch (primitive)
		{
			case "plane":
				MeshPrimitives.BuildPlane(builder, 2f, 2f, 2, 2);
				break;

			case "box":
				MeshPrimitives.BuildBox(builder, new Vector3(2f, 2f, 2f));
				break;

			case "sphere":
				MeshPrimitives.BuildSphere(builder, 1f, 8, 4);
				break;

			case "cylinder":
				MeshPrimitives.BuildCylinder(builder, 1f, 2f, 8);
				break;

			case "cone":
				MeshPrimitives.BuildCone(builder, 1f, 2f, 8);
				break;

			case "capsule":
				MeshPrimitives.BuildCapsule(builder, 1f, 3f, 8, 4);
				break;

			case "torus":
				MeshPrimitives.BuildTorus(builder, 2f, 0.5f, 8, 6);
				break;

			default:
				throw new ArgumentException("Unknown primitive.", nameof(primitive));
		}
	}
}
