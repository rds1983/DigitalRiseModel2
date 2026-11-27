using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Represents one instance of a <see cref="DrModel"/> with its own local and absolute
/// bone transforms.
/// </summary>
public sealed class DrModelInstance
{
	private readonly Matrix[] _boneTransforms;
	private readonly Matrix[] _absoluteBoneTransforms;
	private bool _absoluteBoneTransformsDirty;
	private BoundingSphere _boundingSphere;
	private bool _boundingSphereDirty = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="DrModelInstance"/> class and copies
	/// the model's current bone transforms as the initial local pose.
	/// </summary>
	/// <param name="model">The model to create an instance of.</param>
	/// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
	public DrModelInstance(DrModel model)
	{
		ArgumentNullException.ThrowIfNull(model);

		Model = model;
		_boneTransforms = new Matrix[model.Bones.Length];
		_absoluteBoneTransforms = new Matrix[model.Bones.Length];
		_absoluteBoneTransformsDirty = true;

		for (int i = 0; i < model.Bones.Length; i++)
		{
			_boneTransforms[i] = model.Bones[i].Transform;
		}
	}

	/// <summary>
	/// Gets the model wrapped by this instance.
	/// </summary>
	public DrModel Model { get; }

	/// <summary>
	/// Gets the number of bones in this instance.
	/// </summary>
	public int BoneCount => _boneTransforms.Length;

	/// <summary>
	/// Gets the local transform of the bone at <paramref name="boneIndex"/>.
	/// </summary>
	/// <param name="boneIndex">The bone index in <see cref="DrModel.Bones"/>.</param>
	/// <returns>The local transform of the bone, relative to its parent.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="boneIndex"/> is negative or outside the bone collection.
	/// </exception>
	public Matrix GetLocalTransform(int boneIndex)
	{
		ValidateBoneIndex(boneIndex);
		return _boneTransforms[boneIndex];
	}

	/// <summary>
	/// Sets the local transform of the bone at <paramref name="boneIndex"/> and marks
	/// <see cref="AbsoluteBoneTransforms"/> as dirty.
	/// </summary>
	/// <param name="boneIndex">The bone index in <see cref="DrModel.Bones"/>.</param>
	/// <param name="matrix">The local transform, relative to the bone's parent.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="boneIndex"/> is negative or outside the bone collection.
	/// </exception>
	public void SetLocalTransform(int boneIndex, Matrix matrix)
	{
		ValidateBoneIndex(boneIndex);
		_boneTransforms[boneIndex] = matrix;
		_absoluteBoneTransformsDirty = true;
		_boundingSphereDirty = true;
	}

	/// <summary>
	/// Gets the absolute bone transforms. The array is indexed like
	/// <see cref="DrModel.Bones"/> and is recalculated when queried after a local
	/// transform change. Do not modify the returned array.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// A parent bone appears after its child in <see cref="DrModel.Bones"/>.
	/// </exception>
	public Matrix[] AbsoluteBoneTransforms
	{
		get
		{
			if (_absoluteBoneTransformsDirty)
			{
				UpdateAbsoluteBoneTransforms();
				_absoluteBoneTransformsDirty = false;
			}

			return _absoluteBoneTransforms;
		}
	}

	/// <summary>
	/// Gets the bounding sphere of the model instance in its current pose. Mesh bounding
	/// spheres are transformed by their parent bones' absolute transforms and merged.
	/// The sphere is recalculated when queried after a local transform change.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// A parent bone appears after its child in <see cref="DrModel.Bones"/>.
	/// </exception>
	public BoundingSphere BoundingSphere
	{
		get
		{
			if (_boundingSphereDirty)
			{
				_boundingSphere = CalculateBoundingSphere();
				_boundingSphereDirty = false;
			}

			return _boundingSphere;
		}
	}

	private void ValidateBoneIndex(int boneIndex)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(boneIndex);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
			boneIndex,
			_boneTransforms.Length);
	}

	private BoundingSphere CalculateBoundingSphere()
	{
		Matrix[] absoluteBoneTransforms = AbsoluteBoneTransforms;
		BoundingSphere result = default;
		bool hasBounds = false;

		foreach (DrModelMesh mesh in Model.Meshes)
		{
			Matrix meshTransform = mesh.ParentBone is { } parentBone
				&& (uint)parentBone.Index < (uint)absoluteBoneTransforms.Length
				? absoluteBoneTransforms[parentBone.Index]
				: Matrix.Identity;

			BoundingSphere sphere = mesh.BoundingSphere.Transform(meshTransform);

			result = hasBounds
				? BoundingSphere.CreateMerged(result, sphere)
				: sphere;
			hasBounds = true;
		}

		return result;
	}

	private void UpdateAbsoluteBoneTransforms()
	{
		for (int i = 0; i < _boneTransforms.Length; i++)
		{
			DrModelBone? parent = Model.Bones[i].Parent;

			if (parent is null)
			{
				_absoluteBoneTransforms[i] = _boneTransforms[i];
				continue;
			}

			int parentIndex = parent.Index;

			if ((uint)parentIndex >= (uint)i)
			{
				throw new InvalidOperationException(
					"Bones must be ordered so that a parent bone appears before its children.");
			}

			_absoluteBoneTransforms[i] =
				_boneTransforms[i] * _absoluteBoneTransforms[parentIndex];
		}
	}
}
