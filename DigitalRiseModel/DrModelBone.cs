using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Represents bone data for a model. A bone stores a transform relative to its parent
/// and can contain child bones.
/// </summary>
public sealed class DrModelBone
{
	private readonly List<DrModelBone> _children = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="DrModelBone"/> class.
	/// </summary>
	public DrModelBone()
	{
		Children = new DrModelBoneCollection(_children);
	}

	/// <summary>
	/// Gets the child bones of this bone.
	/// </summary>
	public DrModelBoneCollection Children { get; }

	/// <summary>
	/// Gets or sets the index of this bone in the owning <see cref="DrModel.Bones"/> collection.
	/// </summary>
	public int Index { get; set; }

	/// <summary>
	/// Gets or sets the name of this bone.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the parent bone, or <see langword="null"/> if this is a root bone.
	/// </summary>
	public DrModelBone? Parent { get; set; }

	/// <summary>
	/// Gets or sets the transform of this bone relative to its parent.
	/// </summary>
	public Matrix Transform { get; set; } = Matrix.Identity;

	/// <summary>
	/// Adds a child bone to this bone and sets its <see cref="Parent"/> to this bone.
	/// </summary>
	/// <param name="bone">The bone to add.</param>
	/// <exception cref="ArgumentNullException"><paramref name="bone"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException"><paramref name="bone"/> is this bone or one of its ancestors.</exception>
	/// <exception cref="InvalidOperationException"><paramref name="bone"/> is already a child of this bone.</exception>
	public void AddChild(DrModelBone bone)
	{
		ArgumentNullException.ThrowIfNull(bone);

		if (ReferenceEquals(bone, this))
		{
			throw new ArgumentException("A bone cannot be its own child.", nameof(bone));
		}

		for (DrModelBone? ancestor = this; ancestor is not null; ancestor = ancestor.Parent)
		{
			if (ReferenceEquals(ancestor, bone))
			{
				throw new ArgumentException("Adding this bone would create a cycle in the bone hierarchy.", nameof(bone));
			}
		}

		if (_children.Contains(bone))
		{
			throw new InvalidOperationException("The bone is already a child of this bone.");
		}

		bone.Parent = this;
		_children.Add(bone);
	}
}
