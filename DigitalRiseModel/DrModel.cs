using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Represents a 3D model composed of multiple <see cref="DrModelMesh"/> objects which may be
/// moved independently through a hierarchy of <see cref="DrModelBone"/> objects.
/// </summary>
public sealed class DrModel
{
	private readonly List<DrModelBone> _bones;
	private readonly List<DrModelMesh> _meshes;

	/// <summary>
	/// Initializes a new instance of the <see cref="DrModel"/> class.
	/// </summary>
	/// <param name="bones">The bones of the model. Parent bones must appear before their children.</param>
	/// <param name="meshes">The meshes of the model.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="bones"/> or <paramref name="meshes"/> is <see langword="null"/>.
	/// </exception>
	public DrModel(IList<DrModelBone> bones, IList<DrModelMesh> meshes)
	{
		ArgumentNullException.ThrowIfNull(bones);
		ArgumentNullException.ThrowIfNull(meshes);

		_bones = new List<DrModelBone>(bones.Count);
		foreach (DrModelBone bone in bones)
		{
			ArgumentNullException.ThrowIfNull(bone);
			_bones.Add(bone);
		}

		_meshes = new List<DrModelMesh>(meshes.Count);
		foreach (DrModelMesh mesh in meshes)
		{
			ArgumentNullException.ThrowIfNull(mesh);
			_meshes.Add(mesh);
		}

		Bones = new DrModelBoneCollection(_bones);
		Meshes = new DrModelMeshCollection(_meshes);

		foreach (DrModelBone bone in _bones)
		{
			if (bone.Parent is null)
			{
				Root = bone;
				break;
			}
		}
	}

	/// <summary>
	/// Gets the bones of this model.
	/// </summary>
	public DrModelBoneCollection Bones { get; }

	/// <summary>
	/// Gets the meshes of this model.
	/// </summary>
	public DrModelMeshCollection Meshes { get; }

	/// <summary>
	/// Gets or sets the root bone of this model.
	/// </summary>
	public DrModelBone? Root { get; set; }

	/// <summary>
	/// Gets or sets an object that identifies or describes this model.
	/// </summary>
	public object? Tag { get; set; }

	/// <summary>
	/// Copies bone transforms relative to all parent bones of each bone to a destination array.
	/// </summary>
	/// <param name="destinationBoneTransforms">The array that receives the absolute bone transforms.</param>
	/// <exception cref="ArgumentNullException"><paramref name="destinationBoneTransforms"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="destinationBoneTransforms"/> is shorter than <see cref="Bones"/>.
	/// </exception>
	/// <exception cref="InvalidOperationException">A parent bone appears after its child in <see cref="Bones"/>.</exception>
	public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
	{
		ArgumentNullException.ThrowIfNull(destinationBoneTransforms);

		if (destinationBoneTransforms.Length < Bones.Count)
		{
			throw new ArgumentOutOfRangeException(
				nameof(destinationBoneTransforms),
				destinationBoneTransforms.Length,
				"The destination array must be at least as long as the bone collection.");
		}

		for (int i = 0; i < Bones.Count; i++)
		{
			DrModelBone bone = Bones[i];

			if (bone.Parent is null)
			{
				destinationBoneTransforms[i] = bone.Transform;
				continue;
			}

			int parentIndex = bone.Parent.Index;
			if ((uint)parentIndex >= (uint)i)
			{
				throw new InvalidOperationException(
					"Bones must be ordered so that a parent bone appears before its children.");
			}

			destinationBoneTransforms[i] = bone.Transform * destinationBoneTransforms[parentIndex];
		}
	}

	/// <summary>
	/// Copies bone transforms from a source array into this model.
	/// </summary>
	/// <param name="sourceBoneTransforms">The source bone transforms.</param>
	/// <exception cref="ArgumentNullException"><paramref name="sourceBoneTransforms"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="sourceBoneTransforms"/> is shorter than <see cref="Bones"/>.
	/// </exception>
	public void CopyBoneTransformsFrom(Matrix[] sourceBoneTransforms)
	{
		ArgumentNullException.ThrowIfNull(sourceBoneTransforms);

		if (sourceBoneTransforms.Length < Bones.Count)
		{
			throw new ArgumentOutOfRangeException(
				nameof(sourceBoneTransforms),
				sourceBoneTransforms.Length,
				"The source array must be at least as long as the bone collection.");
		}

		for (int i = 0; i < Bones.Count; i++)
		{
			Bones[i].Transform = sourceBoneTransforms[i];
		}
	}

	/// <summary>
	/// Copies bone transforms from this model to a destination array.
	/// </summary>
	/// <param name="destinationBoneTransforms">The array that receives the bone transforms.</param>
	/// <exception cref="ArgumentNullException"><paramref name="destinationBoneTransforms"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="destinationBoneTransforms"/> is shorter than <see cref="Bones"/>.
	/// </exception>
	public void CopyBoneTransformsTo(Matrix[] destinationBoneTransforms)
	{
		ArgumentNullException.ThrowIfNull(destinationBoneTransforms);

		if (destinationBoneTransforms.Length < Bones.Count)
		{
			throw new ArgumentOutOfRangeException(
				nameof(destinationBoneTransforms),
				destinationBoneTransforms.Length,
				"The destination array must be at least as long as the bone collection.");
		}

		for (int i = 0; i < Bones.Count; i++)
		{
			destinationBoneTransforms[i] = Bones[i].Transform;
		}
	}
}
