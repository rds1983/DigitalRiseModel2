using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

/// <summary>
/// Accumulates vertices and indices for a single mesh part and uploads them to a
/// <see cref="GraphicsDevice"/>.
/// </summary>
/// <typeparam name="T">
/// The vertex type. It must describe its own layout through <see cref="IVertexType"/>.
/// </typeparam>
internal sealed class MeshBuilder<T> where T : struct, IVertexType
{
	private readonly List<T> _vertices = [];
	private readonly List<int> _indices = [];
	private readonly Func<T, Vector3> _positionSelector;
	private readonly bool _cullClockwise;

	/// <summary>
	/// Initializes a new instance of the <see cref="MeshBuilder{T}"/> class.
	/// </summary>
	/// <param name="positionSelector">
	/// Extracts the position from a vertex. It is used to compute the bounding sphere when the
	/// part is created.
	/// </param>
	/// <param name="cullClockwise">
	/// If <see langword="false"/>, triangles are emitted clockwise so they are front-facing under
	/// <see cref="RasterizerState.CullCounterClockwise"/>. If <see langword="true"/>, triangles
	/// are emitted counter-clockwise so they are front-facing under
	/// <see cref="RasterizerState.CullClockwise"/>.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="positionSelector"/> is <see langword="null"/>.</exception>
	public MeshBuilder(Func<T, Vector3> positionSelector, bool cullClockwise = false)
	{
		ArgumentNullException.ThrowIfNull(positionSelector);

		_positionSelector = positionSelector;
		_cullClockwise = cullClockwise;
	}

	/// <summary>
	/// Gets the number of vertices added so far.
	/// </summary>
	public int VertexCount => _vertices.Count;

	/// <summary>
	/// Gets the number of indices added so far.
	/// </summary>
	public int IndexCount => _indices.Count;

	/// <summary>
	/// Gets the vertices added so far.
	/// </summary>
	public IReadOnlyList<T> Vertices => _vertices;

	/// <summary>
	/// Gets the indices added so far.
	/// </summary>
	public IReadOnlyList<int> Indices => _indices;

	/// <summary>
	/// Appends a vertex and returns its index.
	/// </summary>
	/// <param name="vertex">The vertex to append.</param>
	/// <returns>The zero-based index of the appended vertex.</returns>
	public int AddVertex(T vertex)
	{
		_vertices.Add(vertex);
		return _vertices.Count - 1;
	}

	/// <summary>
	/// Appends a raw index without changing its order.
	/// </summary>
	/// <param name="index">The vertex index to append.</param>
	public void AddIndex(int index)
	{
		_indices.Add(index);
	}

	/// <summary>
	/// Appends one triangle.
	/// </summary>
	/// <param name="a">The first vertex index.</param>
	/// <param name="b">The second vertex index.</param>
	/// <param name="c">The third vertex index.</param>
	/// <remarks>
	/// The indices are given in counter-clockwise order as seen from the front face. Unless the
	/// builder was created for <see cref="RasterizerState.CullClockwise"/>, the order is reversed
	/// so that the triangle is front-facing under <see cref="RasterizerState.CullCounterClockwise"/>.
	/// </remarks>
	public void AddTriangle(int a, int b, int c)
	{
		if (_cullClockwise)
		{
			_indices.Add(a);
			_indices.Add(b);
			_indices.Add(c);
			return;
		}

		_indices.Add(a);
		_indices.Add(c);
		_indices.Add(b);
	}

	/// <summary>
	/// Appends two triangles that form a quad.
	/// </summary>
	/// <param name="a">The first vertex index.</param>
	/// <param name="b">The second vertex index.</param>
	/// <param name="c">The third vertex index.</param>
	/// <param name="d">The fourth vertex index.</param>
	/// <remarks>
	/// Indices are given in counter-clockwise order and are emitted like
	/// <see cref="AddTriangle"/>.
	/// </remarks>
	public void AddQuad(int a, int b, int c, int d)
	{
		AddTriangle(a, b, c);
		AddTriangle(a, c, d);
	}

	/// <summary>
	/// Computes a bounding sphere that encloses every appended vertex position.
	/// </summary>
	/// <returns>
	/// The bounding sphere, or <see langword="default"/> when no vertices have been added.
	/// </returns>
	public BoundingSphere ComputeBoundingSphere()
	{
		if (_vertices.Count == 0)
		{
			return default;
		}

		var min = new Vector3(float.MaxValue);
		var max = new Vector3(float.MinValue);

		foreach (T vertex in _vertices)
		{
			Vector3 position = _positionSelector(vertex);
			min = Vector3.Min(min, position);
			max = Vector3.Max(max, position);
		}

		Vector3 center = (min + max) * 0.5f;
		float radiusSquared = 0f;

		foreach (T vertex in _vertices)
		{
			float distanceSquared = Vector3.DistanceSquared(_positionSelector(vertex), center);
			radiusSquared = MathF.Max(radiusSquared, distanceSquared);
		}

		return new BoundingSphere(center, MathF.Sqrt(radiusSquared));
	}

	/// <summary>
	/// Creates a triangle-list <see cref="DrModelMeshPart"/> whose buffers contain the
	/// accumulated geometry.
	/// </summary>
	/// <param name="graphicsDevice">The device used to create the vertex and index buffers.</param>
	/// <returns>The created mesh part.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
	public DrModelMeshPart CreateMeshPart(GraphicsDevice graphicsDevice)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);

		T[] vertices = _vertices.ToArray();
		VertexDeclaration declaration = default(T).VertexDeclaration;

		var vertexBuffer = new VertexBuffer(
			graphicsDevice,
			declaration,
			vertices.Length,
			BufferUsage.WriteOnly);
		vertexBuffer.SetData(vertices);

		IndexBuffer indexBuffer;

		if (vertices.Length <= ushort.MaxValue + 1)
		{
			var indices = new ushort[_indices.Count];

			for (int i = 0; i < indices.Length; i++)
			{
				indices[i] = (ushort)_indices[i];
			}

			indexBuffer = new IndexBuffer(
				graphicsDevice,
				IndexElementSize.SixteenBits,
				indices.Length,
				BufferUsage.WriteOnly);
			indexBuffer.SetData(indices);
		}
		else
		{
			indexBuffer = new IndexBuffer(
				graphicsDevice,
				IndexElementSize.ThirtyTwoBits,
				_indices.Count,
				BufferUsage.WriteOnly);
			indexBuffer.SetData(_indices.ToArray());
		}

		return new DrModelMeshPart
		{
			VertexBuffer = vertexBuffer,
			IndexBuffer = indexBuffer,
			VertexDeclaration = declaration,
			NumVertices = vertices.Length,
			PrimitiveCount = _indices.Count / 3,
			StartIndex = 0,
			VertexOffset = 0,
			BoundingSphere = ComputeBoundingSphere()
		};
	}
}
