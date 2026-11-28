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
	private readonly Dictionary<int, SkinInfo> _skinInfos = [];
	private bool _absoluteBoneTransformsDirty = true;
	private bool _skinTransformsDirty = true;
	private bool _boundingSphereDirty = true;
	private BoundingSphere _boundingSphere;

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

		for (int i = 0; i < model.Bones.Length; i++)
		{
			_boneTransforms[i] = model.Bones[i].Transform;
		}

		foreach (DrModelMesh mesh in model.Meshes)
		{
			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				if (part.Skin is { } skin)
				{
					_skinInfos.TryAdd(skin.Index, new SkinInfo(skin));
				}
			}
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
		_skinTransformsDirty = true;
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
			EnsureAbsoluteBoneTransforms();
			return _absoluteBoneTransforms;
		}
	}

	/// <summary>
	/// Gets the bounding sphere of the model instance in its current pose. Static mesh
	/// bounding spheres are transformed by their parent bones' absolute transforms and
	/// merged. Skinned meshes use their own bounds because glTF skinning ignores the
	/// mesh node transform. The sphere is recalculated when queried after a local
	/// transform change.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// A parent bone appears after its child in <see cref="DrModel.Bones"/>.
	/// </exception>
	public BoundingSphere BoundingSphere
	{
		get
		{
			EnsureAbsoluteBoneTransforms();

			if (_boundingSphereDirty)
			{
				_boundingSphere = CalculateBoundingSphere();
				_boundingSphereDirty = false;
			}

			return _boundingSphere;
		}
	}

	/// <summary>
	/// Gets the current skinning transforms for the skin with the specified index.
	/// Each array element is the joint's inverse bind matrix composed with the bone's
	/// absolute transform in the instance's current pose.
	/// </summary>
	/// <param name="skinIndex">The skin index in the model's skin collection.</param>
	/// <returns>The skinning transforms, one per joint. Do not modify the returned array.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// The model does not contain a skin with the specified index.
	/// </exception>
	public Matrix[] GetSkinTransforms(int skinIndex)
	{
		if (!_skinInfos.TryGetValue(skinIndex, out SkinInfo? skinInfo))
		{
			throw new ArgumentOutOfRangeException(
				nameof(skinIndex),
				skinIndex,
				"The model has no skin with this index.");
		}

		EnsureAbsoluteBoneTransforms();

		if (_skinTransformsDirty)
		{
			UpdateSkinTransforms();
			_skinTransformsDirty = false;
		}

		return skinInfo.Transforms;
	}

	private void ValidateBoneIndex(int boneIndex)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(boneIndex);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
			boneIndex,
			_boneTransforms.Length);
	}

	private void EnsureAbsoluteBoneTransforms()
	{
		if (!_absoluteBoneTransformsDirty)
		{
			return;
		}

		UpdateAbsoluteBoneTransforms();
		_absoluteBoneTransformsDirty = false;
	}

	private void UpdateSkinTransforms()
	{
		foreach (SkinInfo skinInfo in _skinInfos.Values)
		{
			DrSkin skin = skinInfo.Skin;

			for (int i = 0; i < skin.Joints.Length; i++)
			{
				DrSkinJoint joint = skin.Joints[i];
				skinInfo.Transforms[i] = joint.InverseBindMatrix
					* _absoluteBoneTransforms[joint.Bone.Index];
			}
		}
	}

	private BoundingSphere CalculateBoundingSphere()
	{
		BoundingSphere result = default;
		bool hasBounds = false;

		foreach (DrModelMesh mesh in Model.Meshes)
		{
			Matrix meshTransform = Matrix.Identity;

			if (!IsSkinned(mesh)
				&& mesh.ParentBone is { } parentBone
				&& (uint)parentBone.Index < (uint)_absoluteBoneTransforms.Length)
			{
				meshTransform = _absoluteBoneTransforms[parentBone.Index];
			}

			BoundingSphere sphere = mesh.BoundingSphere.Transform(meshTransform);

			result = hasBounds
				? BoundingSphere.CreateMerged(result, sphere)
				: sphere;
			hasBounds = true;
		}

		return result;
	}

	private static bool IsSkinned(DrModelMesh mesh)
	{
		foreach (DrModelMeshPart part in mesh.MeshParts)
		{
			if (part.Skin is not null)
			{
				return true;
			}
		}

		return false;
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

	private class SkinInfo
	{
		public SkinInfo(DrSkin skin)
		{
			Skin = skin ?? throw new ArgumentNullException(nameof(skin));
			Transforms = new Matrix[skin.Joints.Length];
		}

		public DrSkin Skin { get; }

		public Matrix[] Transforms { get; }
	}
}
