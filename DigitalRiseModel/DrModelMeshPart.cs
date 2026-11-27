using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

/// <summary>
/// Represents a batch of geometry information. A <see cref="DrModelMeshPart"/>
/// is a subdivision of a <see cref="DrModelMesh"/>, typically split by material.
/// </summary>
public sealed class DrModelMeshPart : IDisposable
{
	private BoundingSphere _boundingSphere;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="DrModelMeshPart"/> class.
	/// </summary>
	public DrModelMeshPart()
	{
	}

	/// <summary>
	/// Gets or sets the bounding sphere that encompasses this mesh part. Changing the
	/// value invalidates the owning mesh's cached bounding sphere.
	/// </summary>
	public BoundingSphere BoundingSphere
	{
		get => _boundingSphere;
		set
		{
			_boundingSphere = value;
			Mesh?.InvalidateBoundingSphere();
		}
	}

	/// <summary>
	/// Gets or sets the material used by this mesh part.
	/// </summary>
	public DrMaterial? Material { get; set; }

	/// <summary>
	/// Gets the mesh that owns this part.
	/// </summary>
	public DrModelMesh? Mesh { get; internal set; }

	/// <summary>
	/// Gets or sets the skinning data used by this mesh part.
	/// </summary>
	public DrSkin? Skin { get; set; }

	/// <summary>
	/// Gets or sets the index buffer for this mesh part.
	/// </summary>
	public IndexBuffer? IndexBuffer { get; set; }

	/// <summary>
	/// Gets or sets the source indices when the model was loaded with
	/// <see cref="ModelLoadFlags.KeepSourceGeometry"/>.
	/// </summary>
	public int[]? SourceIndices { get; set; }

	/// <summary>
	/// Gets or sets the source vertices when the model was loaded with
	/// <see cref="ModelLoadFlags.KeepSourceGeometry"/>.
	/// </summary>
	public VertexPositionColorNormalTexture[]? SourceVertices { get; set; }

	/// <summary>
	/// Gets or sets the vertex declaration that describes the vertex layout.
	/// </summary>
	public VertexDeclaration? VertexDeclaration { get; set; }

	/// <summary>
	/// Gets or sets the number of vertices referenced by this mesh part.
	/// </summary>
	public int NumVertices { get; set; }

	/// <summary>
	/// Gets or sets the number of primitives to render.
	/// </summary>
	public int PrimitiveCount { get; set; }

	/// <summary>
	/// Gets or sets the location in the index buffer at which to start reading indices.
	/// </summary>
	public int StartIndex { get; set; }

	/// <summary>
	/// Gets or sets an object that identifies this mesh part.
	/// </summary>
	public object? Tag { get; set; }

	/// <summary>
	/// Gets or sets the vertex buffer for this mesh part.
	/// </summary>
	public VertexBuffer? VertexBuffer { get; set; }

	/// <summary>
	/// Gets or sets the offset, in vertices, from the start of the vertex buffer.
	/// </summary>
	public int VertexOffset { get; set; }

	/// <summary>
	/// Disposes the vertex and index buffers assigned to this part and clears its
	/// <see cref="Mesh"/> reference. Calling this method more than once has no effect.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		VertexBuffer?.Dispose();
		VertexBuffer = null;

		IndexBuffer?.Dispose();
		IndexBuffer = null;

		Mesh = null;
		_disposed = true;
	}
}
