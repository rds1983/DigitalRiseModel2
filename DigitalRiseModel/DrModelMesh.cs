using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Represents a mesh that is part of a <see cref="DrModel"/>. A mesh can be moved
/// independently through its <see cref="ParentBone"/> and is composed of one or more
/// <see cref="DrModelMeshPart"/> objects.
/// </summary>
public sealed class DrModelMesh : IDisposable
{
	private DrModelMeshPart[] _meshParts = [];
	private BoundingSphere? _boundingSphere;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="DrModelMesh"/> class with the specified parts.
	/// </summary>
	/// <param name="meshParts">The parts that compose this mesh.</param>
	/// <exception cref="ArgumentNullException"><paramref name="meshParts"/> is <see langword="null"/>.</exception>
	public DrModelMesh(DrModelMeshPart[] meshParts)
	{
		MeshParts = meshParts;
	}

	/// <summary>
	/// Gets the bounding sphere that encompasses this mesh. The sphere is cached and
	/// recalculated by merging the parts' bounding spheres when the cache is invalidated.
	/// </summary>
	public BoundingSphere BoundingSphere
	{
		get
		{
			_boundingSphere ??= CalculateBoundingSphere();
			return _boundingSphere.Value;
		}
	}

	/// <summary>
	/// Gets or sets the parts that compose this mesh. The setter clears the
	/// <see cref="DrModelMeshPart.Mesh"/> reference of the old parts and assigns this
	/// mesh to the new parts.
	/// </summary>
	public DrModelMeshPart[] MeshParts
	{
		get => _meshParts;
		set
		{
			ArgumentNullException.ThrowIfNull(value);

			if (!ReferenceEquals(_meshParts, value))
			{
				foreach (DrModelMeshPart part in _meshParts)
				{
					if (ReferenceEquals(part.Mesh, this))
					{
						part.Mesh = null;
					}
				}

				foreach (DrModelMeshPart part in value)
				{
					ArgumentNullException.ThrowIfNull(part);
					part.Mesh = this;
				}

				_meshParts = value;
			}

			_boundingSphere = null;
		}
	}

	/// <summary>
	/// Gets or sets the name of this mesh.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the bone that controls the position of this mesh.
	/// </summary>
	public DrModelBone? ParentBone { get; set; }

	/// <summary>
	/// Gets or sets an object that identifies this mesh.
	/// </summary>
	public object? Tag { get; set; }

	/// <summary>
	/// Disposes all mesh parts and their buffers, clears the parts array and clears
	/// each part's <see cref="DrModelMeshPart.Mesh"/> reference.
	/// Calling this method more than once has no effect.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		foreach (DrModelMeshPart part in _meshParts)
		{
			part.Dispose();
		}

		MeshParts = [];

		_disposed = true;
	}

	internal void InvalidateBoundingSphere()
	{
		_boundingSphere = null;
	}

	private BoundingSphere CalculateBoundingSphere()
	{
		if (_meshParts.Length == 0)
		{
			return default;
		}

		BoundingSphere result = _meshParts[0].BoundingSphere;

		for (int i = 1; i < _meshParts.Length; i++)
		{
			result = BoundingSphere.CreateMerged(result, _meshParts[i].BoundingSphere);
		}

		return result;
	}
}
