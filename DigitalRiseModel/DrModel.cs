using DigitalRiseModel.Animation;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Represents a 3D model composed of multiple <see cref="DrModelMesh"/> objects which may be
/// moved independently through a hierarchy of <see cref="DrModelBone"/> objects.
/// </summary>
public sealed class DrModel
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DrModel"/> class by traversing the
	/// bone hierarchy starting at <paramref name="root"/>.
	/// </summary>
	/// <param name="root">The root bone of the model.</param>
	/// <param name="autoAssignIndices">
	/// If <see langword="true"/>, assigns sequential indices to the bones in traversal order.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
	/// <exception cref="InvalidOperationException">
	/// The hierarchy contains a cycle or references the same bone more than once.
	/// </exception>
	public DrModel(DrModelBone root, bool autoAssignIndices = true)
	{
		ArgumentNullException.ThrowIfNull(root);

		var bones = new List<DrModelBone>();
		var meshes = new List<DrModelMesh>();
		var visited = new HashSet<DrModelBone>();

		CollectBones(root, bones, meshes, visited, autoAssignIndices);

		Bones = bones.ToArray();
		Meshes = meshes.ToArray();
		Root = root;
	}

	/// <summary>
	/// Gets the bones of this model.
	/// </summary>
	public DrModelBone[] Bones { get; }

	/// <summary>
	/// Gets the meshes of this model.
	/// </summary>
	public DrModelMesh[] Meshes { get; }

	/// <summary>
	/// Gets or sets the root bone of this model.
	/// </summary>
	public DrModelBone? Root { get; set; }

	/// <summary>
	/// Gets or sets the animation clips of this model, keyed by clip name.
	/// </summary>
	public Dictionary<string, AnimationClip> Animations { get; set; } = new(StringComparer.Ordinal);

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

		if (destinationBoneTransforms.Length < Bones.Length)
		{
			throw new ArgumentOutOfRangeException(
				nameof(destinationBoneTransforms),
				destinationBoneTransforms.Length,
				"The destination array must be at least as long as the bone collection.");
		}

		for (int i = 0; i < Bones.Length; i++)
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

		if (sourceBoneTransforms.Length < Bones.Length)
		{
			throw new ArgumentOutOfRangeException(
				nameof(sourceBoneTransforms),
				sourceBoneTransforms.Length,
				"The source array must be at least as long as the bone collection.");
		}

		for (int i = 0; i < Bones.Length; i++)
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

		if (destinationBoneTransforms.Length < Bones.Length)
		{
			throw new ArgumentOutOfRangeException(
				nameof(destinationBoneTransforms),
				destinationBoneTransforms.Length,
				"The destination array must be at least as long as the bone collection.");
		}

		for (int i = 0; i < Bones.Length; i++)
		{
			destinationBoneTransforms[i] = Bones[i].Transform;
		}
	}

	private static void CollectBones(
		DrModelBone bone,
		List<DrModelBone> bones,
		List<DrModelMesh> meshes,
		HashSet<DrModelBone> visited,
		bool assignIndices)
	{
		if (!visited.Add(bone))
		{
			throw new InvalidOperationException(
				"The bone hierarchy contains a cycle or references the same bone more than once.");
		}

		if (assignIndices)
		{
			bone.Index = bones.Count;
		}

		bones.Add(bone);

		if (bone.Mesh is not null)
		{
			meshes.Add(bone.Mesh);
		}

		foreach (DrModelBone child in bone.Children)
		{
			CollectBones(child, bones, meshes, visited, assignIndices);
		}
	}
}
