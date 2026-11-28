using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	private static DrModelMeshPart CreateLinePart(
		GraphicsDevice graphicsDevice,
		Action<MeshBuilder<VertexPositionNormalTexture>> build)
	{
		// Build the solid geometry first and keep its unique triangle edges. This keeps the
		// line version aligned with the solid version, including the tessellation parameters.
		var solid = new MeshBuilder<VertexPositionNormalTexture>(vertex => vertex.Position);
		build(solid);

		var builder = new MeshBuilder<VertexPosition>(
			vertex => vertex.Position,
			primitiveType: PrimitiveType.LineList);
		AddWireframe(builder, solid.Vertices, solid.Indices, solid.QuadDiagonals);

		return builder.CreateMeshPart(graphicsDevice);
	}

	internal static void AddWireframe(
		MeshBuilder<VertexPosition> builder,
		IReadOnlyList<VertexPositionNormalTexture> sourceVertices,
		IReadOnlyList<int> sourceIndices,
		IReadOnlyCollection<(int A, int B)>? quadDiagonals)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(sourceVertices);
		ArgumentNullException.ThrowIfNull(sourceIndices);

		float scale = GetPositionScale(sourceVertices);
		var positionIndices = new Dictionary<(int X, int Y, int Z), int>();
		var weldedIndices = new int[sourceVertices.Count];

		for (int i = 0; i < sourceVertices.Count; i++)
		{
			weldedIndices[i] = -1;
		}

		int GetVertexIndex(int sourceIndex)
		{
			if ((uint)sourceIndex >= (uint)weldedIndices.Length)
			{
				return -1;
			}

			if (weldedIndices[sourceIndex] >= 0)
			{
				return weldedIndices[sourceIndex];
			}

			Vector3 position = sourceVertices[sourceIndex].Position;
			var key = (
				Quantize(position.X, scale),
				Quantize(position.Y, scale),
				Quantize(position.Z, scale));

			if (!positionIndices.TryGetValue(key, out int index))
			{
				index = builder.AddVertex(new VertexPosition(position));
				positionIndices[key] = index;
			}

			weldedIndices[sourceIndex] = index;
			return index;
		}

		// Diagonals are expressed in source indices; translate them to welded indices so they
		// can be skipped in the edge set.
		var skipped = new HashSet<(int A, int B)>();

		if (quadDiagonals is not null)
		{
			foreach ((int a, int c) in quadDiagonals)
			{
				int weldedA = GetVertexIndex(a);
				int weldedC = GetVertexIndex(c);

				if (weldedA >= 0 && weldedC >= 0 && weldedA != weldedC)
				{
					skipped.Add(weldedA < weldedC ? (weldedA, weldedC) : (weldedC, weldedA));
				}
			}
		}

		var edges = new HashSet<(int A, int B)>();

		for (int i = 0; i + 2 < sourceIndices.Count; i += 3)
		{
			int a = GetVertexIndex(sourceIndices[i]);
			int b = GetVertexIndex(sourceIndices[i + 1]);
			int c = GetVertexIndex(sourceIndices[i + 2]);

			AddEdge(builder, edges, skipped, a, b);
			AddEdge(builder, edges, skipped, b, c);
			AddEdge(builder, edges, skipped, c, a);
		}
	}

	private static void AddEdge(
		MeshBuilder<VertexPosition> builder,
		HashSet<(int A, int B)> edges,
		HashSet<(int A, int B)> skipped,
		int a,
		int b)
	{
		if (a < 0 || b < 0 || a == b)
		{
			return;
		}

		(int A, int B) key = a < b ? (a, b) : (b, a);

		if (skipped.Contains(key))
		{
			return;
		}

		if (edges.Add(key))
		{
			builder.AddLine(a, b);
		}
	}

	// Quantizes positions to roughly 1e-5 of the largest coordinate so that vertices which are
	// geometrically the same point but carry different normals or texture coordinates (for
	// example a box corner shared by three faces) collapse to a single line vertex.
	private static float GetPositionScale(
		IReadOnlyList<VertexPositionNormalTexture> vertices)
	{
		float max = 0f;

		foreach (VertexPositionNormalTexture vertex in vertices)
		{
			max = MathF.Max(max, MathF.Max(
				MathF.Abs(vertex.Position.X),
				MathF.Max(MathF.Abs(vertex.Position.Y), MathF.Abs(vertex.Position.Z))));
		}

		return max > 1e-20f && float.IsFinite(max)
			? 1e5f / max
			: 1f;
	}

	private static int Quantize(float value, float scale)
	{
		return (int)MathF.Round(value * scale);
	}
}
