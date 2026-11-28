using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

public static partial class MeshPrimitives
{
	private static void AddLathe(
		MeshBuilder<VertexPositionNormalTexture> builder,
		IReadOnlyList<LatheRow> rows,
		int slices)
	{
		int start = builder.VertexCount;
		int rowSize = slices + 1;
		int rowCount = rows.Count;

		float maxRadius = 0f;

		foreach (LatheRow row in rows)
		{
			maxRadius = MathF.Max(maxRadius, MathF.Abs(row.Radius));
		}

		// Rows computed from an angle at the poles have a radius near zero rather than
		// exactly zero, so detect them with a relative tolerance.
		float poleTolerance = maxRadius * 1e-4f;

		for (int r = 0; r < rowCount; r++)
		{
			LatheRow row = rows[r];
			float v = (float)r / (rowCount - 1);

			for (int i = 0; i <= slices; i++)
			{
				float phi = MathF.Tau * i / slices;
				float cos = MathF.Cos(phi);
				float sin = MathF.Sin(phi);

				var position = new Vector3(row.Radius * cos, row.Y, row.Radius * sin);
				var normal = Vector3.Normalize(new Vector3(
					row.NormalRadial * cos,
					row.NormalY,
					row.NormalRadial * sin));

				builder.AddVertex(new VertexPositionNormalTexture(
					position,
					normal,
					new Vector2((float)i / slices, v)));
			}
		}

		for (int r = 0; r < rowCount - 1; r++)
		{
			int topStart = start + r * rowSize;
			int bottomStart = topStart + rowSize;
			bool topPole = MathF.Abs(rows[r].Radius) <= poleTolerance;
			bool bottomPole = MathF.Abs(rows[r + 1].Radius) <= poleTolerance;

			for (int i = 0; i < slices; i++)
			{
				int a = topStart + i;
				int b = topStart + i + 1;
				int c = bottomStart + i + 1;
				int d = bottomStart + i;

				if (topPole)
				{
					if (!bottomPole)
					{
						builder.AddTriangle(a, c, d);
					}
				}
				else if (bottomPole)
				{
					builder.AddTriangle(a, b, c);
				}
				else
				{
					builder.AddQuad(a, b, c, d);
				}
			}
		}
	}

	private static void AddCap(
		MeshBuilder<VertexPositionNormalTexture> builder,
		float y,
		float radius,
		float normalY,
		int slices)
	{
		var normal = new Vector3(0f, normalY, 0f);
		int center = builder.AddVertex(new VertexPositionNormalTexture(
			new Vector3(0f, y, 0f),
			normal,
			new Vector2(0.5f, 0.5f)));

		int ringStart = builder.VertexCount;

		for (int i = 0; i <= slices; i++)
		{
			float phi = MathF.Tau * i / slices;
			float cos = MathF.Cos(phi);
			float sin = MathF.Sin(phi);

			builder.AddVertex(new VertexPositionNormalTexture(
				new Vector3(radius * cos, y, radius * sin),
				normal,
				new Vector2(0.5f + 0.5f * cos, 0.5f + 0.5f * sin)));
		}

		for (int i = 0; i < slices; i++)
		{
			int p0 = ringStart + i;
			int p1 = ringStart + i + 1;

			if (normalY > 0f)
			{
				builder.AddTriangle(center, p1, p0);
			}
			else
			{
				builder.AddTriangle(center, p0, p1);
			}
		}
	}

	private readonly struct LatheRow
	{
		public LatheRow(float y, float radius, float normalRadial, float normalY)
		{
			Y = y;
			Radius = radius;
			NormalRadial = normalRadial;
			NormalY = normalY;
		}

		public float Y { get; }

		public float Radius { get; }

		public float NormalRadial { get; }

		public float NormalY { get; }
	}
}
