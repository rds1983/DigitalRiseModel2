using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Tests;

public sealed class MeshPrimitivesTests
{
	private const float Tolerance = 1e-4f;

	[Fact]
	public void BuildPlane_DefaultSegments_ProducesTwoTriangles()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildPlane(builder, 2f, 4f, 1, 1);

		Assert.Equal(4, builder.VertexCount);
		Assert.Equal(6, builder.IndexCount);
	}

	[Fact]
	public void BuildPlane_Segmented_ProducesGridVertices()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildPlane(builder, 2f, 4f, 3, 2);

		Assert.Equal(4 * 3, builder.VertexCount);
		Assert.Equal(3 * 2 * 2 * 3, builder.IndexCount);
	}

	[Fact]
	public void BuildPlane_PositionsLieInXZPlaneAndFaceUp()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildPlane(builder, 2f, 4f, 2, 2);

		Assert.All(builder.Vertices, vertex => Assert.Equal(0f, vertex.Position.Y));
		Assert.All(builder.Vertices, vertex => Assert.Equal(Vector3.Up, vertex.Normal));

		BoundingSphere bounds = builder.ComputeBoundingSphere();
		AssertVector(new Vector3(0f, 0f, 0f), bounds.Center);
		AssertClose(MathF.Sqrt(1f + 4f), bounds.Radius);
	}

	[Fact]
	public void BuildBox_Default_ProducesTwentyFourVerticesAndTwelveTriangles()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildBox(builder, new Vector3(2f, 4f, 6f));

		Assert.Equal(24, builder.VertexCount);
		Assert.Equal(36, builder.IndexCount);
	}

	[Fact]
	public void BuildBox_NormalsAreUnitAndAxisAligned()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildBox(builder, new Vector3(2f, 4f, 6f));

		foreach (VertexPositionNormalTexture vertex in builder.Vertices)
		{
			AssertClose(1f, vertex.Normal.Length());
			Assert.Single(new[] { vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z }
				.Where(component => MathF.Abs(component) > 0.5f));
		}
	}

	[Fact]
	public void BuildBox_BoundsMatchSize()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildBox(builder, new Vector3(2f, 4f, 6f));

		BoundingSphere bounds = builder.ComputeBoundingSphere();
		AssertVector(Vector3.Zero, bounds.Center);
		AssertClose(MathF.Sqrt(1f + 4f + 9f), bounds.Radius);
	}

	[Fact]
	public void BuildSphere_VerticesLieOnRadiusAndNormalsPointOutward()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildSphere(builder, 2f, 16, 8);

		foreach (VertexPositionNormalTexture vertex in builder.Vertices)
		{
			AssertClose(2f, vertex.Position.Length());
			AssertClose(1f, vertex.Normal.Length());

			Vector3 expectedNormal = Vector3.Normalize(vertex.Position);
			AssertVector(expectedNormal, vertex.Normal);
		}

		BoundingSphere bounds = builder.ComputeBoundingSphere();
		AssertVector(Vector3.Zero, bounds.Center);
		AssertClose(2f, bounds.Radius);
	}

	[Fact]
	public void BuildSphere_PoleRowsDoNotProduceDegenerateTriangles()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildSphere(builder, 1f, 8, 4);

		AssertNoDegenerateTriangles(builder);
	}

	[Fact]
	public void BuildCylinder_BoundsMatchRadiusAndHeight()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildCylinder(builder, 2f, 4f, 16);

		BoundingSphere bounds = builder.ComputeBoundingSphere();
		AssertVector(Vector3.Zero, bounds.Center);
		AssertClose(MathF.Sqrt(4f + 4f), bounds.Radius);
	}

	[Fact]
	public void BuildCylinder_CapNormalsPointAlongY()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildCylinder(builder, 2f, 4f, 8);

		Assert.Contains(builder.Vertices, vertex => vertex.Normal == Vector3.Up);
		Assert.Contains(builder.Vertices, vertex => vertex.Normal == -Vector3.UnitY);
		AssertNoDegenerateTriangles(builder);
	}

	[Fact]
	public void BuildCone_HasApexAndBase()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildCone(builder, 2f, 4f, 16);

		float top = builder.Vertices.Max(vertex => vertex.Position.Y);
		float bottom = builder.Vertices.Min(vertex => vertex.Position.Y);

		AssertClose(2f, top);
		AssertClose(-2f, bottom);
		AssertNoDegenerateTriangles(builder);
	}

	[Fact]
	public void BuildCone_SideNormalsHavePositiveYComponent()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildCone(builder, 2f, 4f, 8);

		float baseY = builder.Vertices.Min(vertex => vertex.Position.Y);

		foreach (VertexPositionNormalTexture vertex in builder.Vertices)
		{
			if (vertex.Position.Y > baseY + Tolerance)
			{
				Assert.True(vertex.Normal.Y > 0f);
			}
		}
	}

	[Fact]
	public void BuildCapsule_BoundsMatchRadiusAndHeight()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildCapsule(builder, 1f, 4f, 16, 8);

		float top = builder.Vertices.Max(vertex => vertex.Position.Y);
		float bottom = builder.Vertices.Min(vertex => vertex.Position.Y);

		AssertClose(2f, top);
		AssertClose(-2f, bottom);

		BoundingSphere bounds = builder.ComputeBoundingSphere();
		AssertVector(Vector3.Zero, bounds.Center);
		AssertClose(2f, bounds.Radius);
		AssertNoDegenerateTriangles(builder);
	}

	[Fact]
	public void BuildCapsule_HeightEqualToDiameter_ProducesNoDuplicateRows()
	{
		var sphere = CreateBuilder();
		MeshPrimitives.BuildSphere(sphere, 1f, 8, 8);
		int sphereVertices = sphere.VertexCount;

		var capsule = CreateBuilder();
		MeshPrimitives.BuildCapsule(capsule, 1f, 2f, 8, 4);

		Assert.Equal(sphereVertices, capsule.VertexCount);
		AssertNoDegenerateTriangles(capsule);
	}

	[Fact]
	public void BuildTorus_BoundsMatchRadii()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildTorus(builder, 3f, 1f, 16, 8);

		BoundingSphere bounds = builder.ComputeBoundingSphere();
		AssertVector(Vector3.Zero, bounds.Center);
		AssertClose(4f, bounds.Radius);
	}

	[Fact]
	public void BuildTorus_NormalsAreUnit()
	{
		var builder = CreateBuilder();

		MeshPrimitives.BuildTorus(builder, 3f, 1f, 16, 8);

		foreach (VertexPositionNormalTexture vertex in builder.Vertices)
		{
			AssertClose(1f, vertex.Normal.Length());
		}
	}

	[Theory]
	[InlineData("plane")]
	[InlineData("box")]
	[InlineData("sphere")]
	[InlineData("cylinder")]
	[InlineData("cone")]
	[InlineData("capsule")]
	[InlineData("torus")]
	public void Build_ProducesClockwiseFrontFaces(string primitive)
	{
		var builder = CreateBuilder();

		Build(primitive, builder);

		AssertClockwise(builder);
	}

	[Fact]
	public void BuildPlane_NonPositiveSize_ThrowsArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildPlane(builder, 0f, 1f, 1, 1));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildPlane(builder, 1f, 0f, 1, 1));
	}

	[Fact]
	public void BuildPlane_SegmentsBelowOne_ThrowsArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildPlane(builder, 1f, 1f, 0, 1));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildPlane(builder, 1f, 1f, 1, 0));
	}

	[Fact]
	public void BuildBox_NonPositiveSize_ThrowsArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildBox(builder, Vector3.Zero));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildBox(builder, new Vector3(-1f, 1f, 1f)));
	}

	[Fact]
	public void BuildSphere_InvalidParameters_ThrowArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildSphere(builder, 0f, 8, 4));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildSphere(builder, 1f, 2, 4));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildSphere(builder, 1f, 8, 1));
	}

	[Fact]
	public void BuildCylinder_InvalidParameters_ThrowArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCylinder(builder, 0f, 2f, 8));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCylinder(builder, 1f, 0f, 8));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCylinder(builder, 1f, 2f, 2));
	}

	[Fact]
	public void BuildCone_InvalidParameters_ThrowArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCone(builder, 0f, 2f, 8));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCone(builder, 1f, 0f, 8));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCone(builder, 1f, 2f, 2));
	}

	[Fact]
	public void BuildCapsule_HeightLessThanDiameter_ThrowsArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCapsule(builder, 2f, 3f, 8, 4));
	}

	[Fact]
	public void BuildCapsule_InvalidParameters_ThrowArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCapsule(builder, 0f, 2f, 8, 4));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCapsule(builder, 1f, 2f, 2, 4));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildCapsule(builder, 1f, 2f, 8, 1));
	}

	[Fact]
	public void BuildTorus_InvalidParameters_ThrowArgumentOutOfRangeException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildTorus(builder, 0f, 1f, 8, 8));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildTorus(builder, 1f, 0f, 8, 8));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildTorus(builder, 2f, 1f, 2, 8));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => MeshPrimitives.BuildTorus(builder, 2f, 1f, 8, 2));
	}

	[Fact]
	public void Build_WithNullBuilder_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.BuildPlane(null!, 1f, 1f, 1, 1));
		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.BuildBox(null!, Vector3.One));
		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.BuildSphere(null!, 1f, 8, 4));
		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.BuildCylinder(null!, 1f, 1f, 8));
		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.BuildCone(null!, 1f, 1f, 8));
		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.BuildCapsule(null!, 1f, 2f, 8, 4));
		Assert.Throws<ArgumentNullException>(
			() => MeshPrimitives.BuildTorus(null!, 2f, 1f, 8, 8));
	}

	[Fact]
	public void CreateMeshPart_WithNullDevice_ThrowsArgumentNullException()
	{
		var builder = CreateBuilder();

		Assert.Throws<ArgumentNullException>(() => builder.CreateMeshPart(null!));
	}

	[Fact]
	public void ComputeBoundingSphere_NoVertices_ReturnsDefault()
	{
		var builder = CreateBuilder();

		BoundingSphere bounds = builder.ComputeBoundingSphere();

		Assert.Equal(Vector3.Zero, bounds.Center);
		Assert.Equal(0f, bounds.Radius);
	}

	[Fact]
	public void AddVertex_ReturnsSequentialIndices()
	{
		var builder = CreateBuilder();

		int first = builder.AddVertex(CreateVertex(Vector3.Zero));
		int second = builder.AddVertex(CreateVertex(Vector3.UnitX));

		Assert.Equal(0, first);
		Assert.Equal(1, second);
		Assert.Equal(2, builder.VertexCount);
	}

	[Fact]
	public void AddTriangle_ReversesWinding()
	{
		var builder = CreateBuilder();
		builder.AddVertex(CreateVertex(Vector3.Zero));
		builder.AddVertex(CreateVertex(Vector3.UnitX));
		builder.AddVertex(CreateVertex(Vector3.UnitY));

		builder.AddTriangle(0, 1, 2);

		Assert.Equal(new[] { 0, 2, 1 }, builder.Indices);
	}

	[Fact]
	public void AddQuad_ProducesTwoTrianglesInReversedWinding()
	{
		var builder = CreateBuilder();

		for (int i = 0; i < 4; i++)
		{
			builder.AddVertex(CreateVertex(Vector3.Zero));
		}

		builder.AddQuad(0, 1, 2, 3);

		Assert.Equal(new[] { 0, 2, 1, 0, 3, 2 }, builder.Indices);
	}

	[Fact]
	public void AddIndex_AppendsWithoutReordering()
	{
		var builder = CreateBuilder();
		builder.AddVertex(CreateVertex(Vector3.Zero));
		builder.AddVertex(CreateVertex(Vector3.UnitX));

		builder.AddIndex(1);
		builder.AddIndex(0);

		Assert.Equal(new[] { 1, 0 }, builder.Indices);
	}

	[Fact]
	public void Constructor_WithNullPositionSelector_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(
			() => new MeshBuilder<VertexPositionNormalTexture>(null!));
	}

	[Fact]
	public void AddTriangle_CullClockwise_KeepsCounterClockwiseOrder()
	{
		var builder = CreateBuilder(cullClockwise: true);
		builder.AddVertex(CreateVertex(Vector3.Zero));
		builder.AddVertex(CreateVertex(Vector3.UnitX));
		builder.AddVertex(CreateVertex(Vector3.UnitY));

		builder.AddTriangle(0, 1, 2);

		Assert.Equal(new[] { 0, 1, 2 }, builder.Indices);
	}

	[Fact]
	public void AddQuad_CullClockwise_KeepsCounterClockwiseOrder()
	{
		var builder = CreateBuilder(cullClockwise: true);

		for (int i = 0; i < 4; i++)
		{
			builder.AddVertex(CreateVertex(Vector3.Zero));
		}

		builder.AddQuad(0, 1, 2, 3);

		Assert.Equal(new[] { 0, 1, 2, 0, 2, 3 }, builder.Indices);
	}

	[Theory]
	[InlineData("plane")]
	[InlineData("box")]
	[InlineData("sphere")]
	[InlineData("cylinder")]
	[InlineData("cone")]
	[InlineData("capsule")]
	[InlineData("torus")]
	public void Build_CullClockwise_ProducesCounterClockwiseFrontFaces(string primitive)
	{
		var builder = CreateBuilder(cullClockwise: true);

		Build(primitive, builder);

		AssertCounterClockwise(builder);
	}

	private static MeshBuilder<VertexPositionNormalTexture> CreateBuilder(
		bool cullClockwise = false)
	{
		return new MeshBuilder<VertexPositionNormalTexture>(
			vertex => vertex.Position,
			cullClockwise);
	}

	private static VertexPositionNormalTexture CreateVertex(Vector3 position)
	{
		return new VertexPositionNormalTexture(position, Vector3.Up, Vector2.Zero);
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

	private static void AssertNoDegenerateTriangles(
		MeshBuilder<VertexPositionNormalTexture> builder)
	{
		for (int i = 0; i + 2 < builder.IndexCount; i += 3)
		{
			Vector3 a = builder.Vertices[builder.Indices[i]].Position;
			Vector3 b = builder.Vertices[builder.Indices[i + 1]].Position;
			Vector3 c = builder.Vertices[builder.Indices[i + 2]].Position;

			Assert.True(Vector3.Cross(b - a, c - a).LengthSquared() > 1e-8f);
		}
	}

	// MonoGame's default CullCounterClockwise state expects clockwise front faces, so the
	// geometric normal of each emitted triangle must point away from the vertex normals.
	private static void AssertClockwise(
		MeshBuilder<VertexPositionNormalTexture> builder)
	{
		for (int i = 0; i + 2 < builder.IndexCount; i += 3)
		{
			VertexPositionNormalTexture v0 = builder.Vertices[builder.Indices[i]];
			VertexPositionNormalTexture v1 = builder.Vertices[builder.Indices[i + 1]];
			VertexPositionNormalTexture v2 = builder.Vertices[builder.Indices[i + 2]];

			Vector3 geometricNormal = Vector3.Cross(
				v1.Position - v0.Position,
				v2.Position - v0.Position);

			Vector3 averageNormal = v0.Normal + v1.Normal + v2.Normal;
			float dot = Vector3.Dot(geometricNormal, averageNormal);
			int triangle = i / 3;

			Assert.True(
				dot <= Tolerance,
				$"Triangle {triangle} is wound counter-clockwise; dot was {dot}.");
		}
	}

	// For RasterizerState.CullClockwise the front face must be counter-clockwise, so the
	// geometric normal of each emitted triangle must point along the vertex normals.
	private static void AssertCounterClockwise(
		MeshBuilder<VertexPositionNormalTexture> builder)
	{
		for (int i = 0; i + 2 < builder.IndexCount; i += 3)
		{
			VertexPositionNormalTexture v0 = builder.Vertices[builder.Indices[i]];
			VertexPositionNormalTexture v1 = builder.Vertices[builder.Indices[i + 1]];
			VertexPositionNormalTexture v2 = builder.Vertices[builder.Indices[i + 2]];

			Vector3 geometricNormal = Vector3.Cross(
				v1.Position - v0.Position,
				v2.Position - v0.Position);

			Vector3 averageNormal = v0.Normal + v1.Normal + v2.Normal;
			float dot = Vector3.Dot(geometricNormal, averageNormal);
			int triangle = i / 3;

			Assert.True(
				dot >= -Tolerance,
				$"Triangle {triangle} is wound clockwise; dot was {dot}.");
		}
	}

	private static void AssertVector(Vector3 expected, Vector3 actual)
	{
		AssertClose(expected.X, actual.X);
		AssertClose(expected.Y, actual.Y);
		AssertClose(expected.Z, actual.Z);
	}

	private static void AssertClose(float expected, float actual)
	{
		Assert.InRange(actual, expected - Tolerance, expected + Tolerance);
	}
}
