using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Represents bone data for a model. A bone stores a transform relative to its parent
/// and can contain child bones.
/// </summary>
public sealed class DrModelBone
{
	private DrModelBone[] _children = [];
	private DrModelMesh? _mesh;

	/// <summary>
	/// Initializes a new instance of the <see cref="DrModelBone"/> class.
	/// </summary>
	/// <param name="name">The name of the bone.</param>
	/// <param name="mesh">The mesh associated with this bone, if any.</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
	public DrModelBone(string name, DrModelMesh? mesh = null)
	{
		ArgumentNullException.ThrowIfNull(name);

		Name = name;
		Mesh = mesh;
	}

	/// <summary>
	/// Gets or sets the child bones of this bone. The setter clears the
	/// <see cref="Parent"/> reference of the old children and assigns this bone as the
	/// parent of the new children.
	/// </summary>
	public DrModelBone[] Children
	{
		get => _children;
		set
		{
			ArgumentNullException.ThrowIfNull(value);

			if (ReferenceEquals(_children, value))
			{
				return;
			}

			foreach (DrModelBone child in _children)
			{
				if (ReferenceEquals(child.Parent, this))
				{
					child.Parent = null;
				}
			}

			foreach (DrModelBone child in value)
			{
				ArgumentNullException.ThrowIfNull(child);
				child.Parent = this;
			}

			_children = value;
		}
	}

	/// <summary>
	/// Gets or sets the index of this bone in the owning <see cref="DrModel.Bones"/> collection.
	/// </summary>
	public int Index { get; set; }

	/// <summary>
	/// Gets or sets the mesh associated with this bone, if any. The setter keeps
	/// <see cref="DrModelMesh.ParentBone"/> synchronized.
	/// </summary>
	public DrModelMesh? Mesh
	{
		get => _mesh;
		set
		{
			if (ReferenceEquals(_mesh, value))
			{
				return;
			}

			if (_mesh is not null && ReferenceEquals(_mesh.ParentBone, this))
			{
				_mesh.ParentBone = null;
			}

			if (value?.ParentBone is { } oldParent && !ReferenceEquals(oldParent, this))
			{
				oldParent.Mesh = null;
			}

			_mesh = value;

			if (_mesh is not null)
			{
				_mesh.ParentBone = this;
			}
		}
	}

	/// <summary>
	/// Gets or sets the name of this bone.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets the parent bone, or <see langword="null"/> if this is a root bone.
	/// </summary>
	public DrModelBone? Parent { get; internal set; }

	/// <summary>
	/// Gets or sets an object that identifies this bone.
	/// </summary>
	public object? Tag { get; set; }

	/// <summary>
	/// Gets or sets the transform of this bone relative to its parent.
	/// </summary>
	public Matrix Transform { get; set; } = Matrix.Identity;
}
