using System.Runtime.CompilerServices;
using AssetManagementBase;
using DigitalRiseModel;
using DigitalRiseModel.Animation;
using DigitalRiseModel.Gltf;
using DigitalRiseModel.Vertices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace DigitalRiseModel.Tests;

public sealed class GltfLoaderTests
{
	[Fact]
	public void Load_TriangleGltf_UsesDynamicVertexLayout()
	{
		DrModelMeshPart part = LoadFirstPart("triangle.gltf");

		Assert.Equal(3, part.NumVertices);
		Assert.Equal(1, part.PrimitiveCount);
		Assert.Null(part.VertexBuffer);

		VertexDeclaration declaration = Assert.IsType<VertexDeclaration>(part.VertexDeclaration);
		VertexElement position = Assert.Single(declaration.GetVertexElements());
		Assert.Equal(VertexElementUsage.Position, position.VertexElementUsage);
		Assert.Equal(0, position.UsageIndex);
		Assert.Equal(VertexElementFormat.Vector3, position.VertexElementFormat);
		Assert.Equal(0, position.Offset);
		Assert.Equal(12, declaration.VertexStride);
	}

	[Fact]
	public void Load_TwoUvGltf_PreservesAllTextureCoordinates()
	{
		DrModelMeshPart part = LoadFirstPart("two-uvs.gltf");

		VertexDeclaration declaration = Assert.IsType<VertexDeclaration>(part.VertexDeclaration);
		VertexElement[] texCoords = declaration
			.GetVertexElements()
			.Where(element => element.VertexElementUsage == VertexElementUsage.TextureCoordinate)
			.OrderBy(element => element.UsageIndex)
			.ToArray();

		Assert.Equal(2, texCoords.Length);
		Assert.Equal(0, texCoords[0].UsageIndex);
		Assert.Equal(1, texCoords[1].UsageIndex);
		Assert.Equal(VertexElementFormat.Vector2, texCoords[0].VertexElementFormat);
		Assert.Equal(40, declaration.VertexStride);
	}

	[Fact]
	public void Load_SkinnedGltf_LoadsSkinAndJointAttributes()
	{
		DrModelMeshPart part = LoadFirstPart("skinned.gltf");

		Assert.Equal(0, part.Skin!.Index);
		DrSkinJoint joint = Assert.Single(part.Skin.Joints);
		Assert.Equal("Joint", joint.Bone.Name);
		Assert.Equal(Matrix.Identity, joint.InverseBindMatrix);

		VertexElement[] elements = part.VertexDeclaration!.GetVertexElements();
		Assert.Contains(elements, element => element.VertexElementUsage == VertexElementUsage.BlendIndices);
		Assert.Contains(elements, element => element.VertexElementUsage == VertexElementUsage.BlendWeight);
	}

	[Fact]
	public void Load_MaterialsGltf_LoadsMaterialsAndParts()
	{
		DrModel model = GltfLoader.Load(TestDataPath("materials.gltf"));
		DrModelMesh mesh = Assert.Single(model.Meshes);

		Assert.Equal(2, mesh.MeshParts.Length);

		DrMaterial red = Assert.IsType<DrMaterial>(mesh.MeshParts[0].Material);
		Assert.Equal("RedMask", red.Name);
		Assert.Equal(new Vector4(1, 0, 0, 1), red.BaseColorFactor);
		Assert.Equal(DrAlphaMode.Mask, red.AlphaMode);
		Assert.Equal(0.3f, red.AlphaCutoff, 5);
		Assert.True(red.DoubleSided);
		Assert.Equal(0.25f, red.MetallicFactor, 3);
		Assert.Equal(0.75f, red.RoughnessFactor, 3);
		Assert.NotNull(red.BaseColorTexture);
		Assert.Equal(0, red.BaseColorTexture!.TextureIndex);
		Assert.Equal(1, red.BaseColorTexture.TexCoord);
		Assert.Null(red.BaseColorTexture.Texture);
		Assert.NotNull(red.NormalTexture);
		Assert.Equal(0.5f, red.NormalTexture!.Scale, 5);
		Assert.NotNull(red.OcclusionTexture);
		Assert.Equal(0.8f, red.OcclusionTexture!.Strength, 5);
		Assert.Equal(new Vector3(0.1f, 0.2f, 0.3f), red.EmissiveFactor);

		DrMaterial blue = Assert.IsType<DrMaterial>(mesh.MeshParts[1].Material);
		Assert.Equal("Blue", blue.Name);
	}

	[Fact]
	public void Load_TriangleStrip_ConvertsToTwoTriangles()
	{
		DrModelMeshPart part = LoadFirstPart("triangle-strip.gltf");

		Assert.Equal(4, part.NumVertices);
		Assert.Equal(2, part.PrimitiveCount);
	}

	[Fact]
	public void Load_KeepSourceGeometry_PopulatesSourceData()
	{
		DrModel model = GltfLoader.Load(
			TestDataPath("two-uvs.gltf"),
			flags: ModelLoadFlags.KeepSourceGeometry);
		DrModelMeshPart part = Assert.Single(Assert.Single(model.Meshes).MeshParts);

		Assert.NotNull(part.SourceIndices);
		Assert.NotNull(part.SourceVertices);
		Assert.Equal(3, part.SourceIndices!.Length);
		Assert.Equal(3, part.SourceVertices!.Length);

		VertexSkinned vertex = part.SourceVertices[1];
		Assert.Equal(new Vector3(1, 0, 0), vertex.Position);
		Assert.Equal(Vector3.UnitZ, vertex.Normal);
		Assert.Equal(new Vector2(1, 0), vertex.TextureCoordinate);
		Assert.Equal(new Vector4(0, 0, 0, 0), vertex.BlendWeights);

		// Positive determinant: glTF CCW is normalized to MonoGame's CW convention.
		Assert.Equal(new[] { 0, 2, 1 }, part.SourceIndices);
	}

	[Fact]
	public void Load_KeepSourceGeometry_PopulatesSkinningData()
	{
		DrModelMeshPart part = LoadFirstPart(
			"skinned.gltf",
			ModelLoadFlags.KeepSourceGeometry);

		Assert.NotNull(part.SourceVertices);
		VertexSkinned vertex = part.SourceVertices![0];
		Assert.Equal(default(Byte4), vertex.BlendIndices);
		Assert.Equal(new Vector4(1, 0, 0, 0), vertex.BlendWeights);
	}

	[Fact]
	public void Load_PositiveDeterminant_ReversesSourceIndices()
	{
		DrModelMeshPart part = LoadFirstPart(
			"triangle.gltf",
			ModelLoadFlags.KeepSourceGeometry);

		Assert.Equal(new[] { 0, 2, 1 }, part.SourceIndices);
	}

	[Fact]
	public void Load_NegativeDeterminant_KeepsSourceIndices()
	{
		DrModelMeshPart part = LoadFirstPart(
			"triangle-mirrored.gltf",
			ModelLoadFlags.KeepSourceGeometry);

		Assert.Equal(new[] { 0, 1, 2 }, part.SourceIndices);
	}

	[Fact]
	public void Load_EnsureUVs_AddsZeroTextureCoordinateChannel()
	{
		DrModelMeshPart part = LoadFirstPart(
			"triangle.gltf",
			ModelLoadFlags.EnsureUVs);

		VertexElement uv = Assert.Single(
			part.VertexDeclaration!.GetVertexElements(),
			element => element.VertexElementUsage == VertexElementUsage.TextureCoordinate);

		Assert.Equal(0, uv.UsageIndex);
		Assert.Equal(VertexElementFormat.Vector2, uv.VertexElementFormat);
	}

	[Fact]
	public void Load_WithoutEnsureUVs_DoesNotAddTextureCoordinateChannel()
	{
		DrModelMeshPart part = LoadFirstPart("triangle.gltf");

		Assert.DoesNotContain(
			part.VertexDeclaration!.GetVertexElements(),
			element => element.VertexElementUsage == VertexElementUsage.TextureCoordinate);
	}

	[Fact]
	public void Load_EnsureUVsWithSourceGeometry_ReturnsZeroUVs()
	{
		DrModelMeshPart part = LoadFirstPart(
			"triangle.gltf",
			ModelLoadFlags.EnsureUVs | ModelLoadFlags.KeepSourceGeometry);

		Assert.All(part.SourceVertices!, vertex => Assert.Equal(Vector2.Zero, vertex.TextureCoordinate));
	}

	[Fact]
	public void Load_WithoutKeepSourceGeometry_LeavesSourceDataNull()
	{
		DrModel model = GltfLoader.Load(TestDataPath("triangle.gltf"));
		DrModelMeshPart part = Assert.Single(Assert.Single(model.Meshes).MeshParts);

		Assert.Null(part.SourceIndices);
		Assert.Null(part.SourceVertices);
	}

	[Fact]
	public void Load_Glb_LoadsDataOnly()
	{
		DrModelMeshPart part = LoadFirstPart("triangle.glb");

		Assert.Equal(3, part.NumVertices);
		Assert.Equal(1, part.PrimitiveCount);
		Assert.Null(part.VertexBuffer);
	}

	[Fact]
	public void Load_MinimalGltf_BuildsBoneHierarchy()
	{
		string path = Path.Combine(Path.GetTempPath(), $"dr-model-{Guid.NewGuid():N}.gltf");

		try
		{
			File.WriteAllText(
				path,
				"""
				{
				  "asset": { "version": "2.0" },
				  "scene": 0,
				  "scenes": [ { "nodes": [ 0 ] } ],
				  "nodes": [
				    { "name": "Root", "children": [ 1 ] },
				    { "name": "Child", "translation": [ 0, 1, 0 ] }
				  ]
				}
				""");

			DrModel model = GltfLoader.Load(path);

			Assert.Equal(3, model.Bones.Length);
			Assert.Equal("Root", model.Bones[1].Name);
			Assert.Equal("Child", model.Bones[2].Name);
			Assert.Equal(1f, model.Bones[2].Transform.Translation.Y);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void Load_NodeMatrix_UsesRowVectorConvention()
	{
		string path = Path.Combine(Path.GetTempPath(), $"dr-model-{Guid.NewGuid():N}.gltf");

		try
		{
			File.WriteAllText(
				path,
				"""
				{
				  "asset": { "version": "2.0" },
				  "scene": 0,
				  "scenes": [ { "nodes": [ 0 ] } ],
				  "nodes": [
				    {
				      "name": "Translated",
				      "matrix": [ 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 2, 3, 1 ]
				    }
				  ]
				}
				""");

			DrModel model = GltfLoader.Load(path);

			Assert.Equal(new Vector3(1, 2, 3), model.Bones[1].Transform.Translation);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void LoadDrModel_AssetManager_LoadsAndCachesModel()
	{
		var graphicsDevice = (GraphicsDevice)RuntimeHelpers.GetUninitializedObject(typeof(GraphicsDevice));
		AssetManager assets = AssetManager.CreateFileAssetManager(TestDataDirectory());

		DrModel model = assets.LoadDrModel(graphicsDevice, "empty-scene.gltf");
		DrModel cached = assets.LoadDrModel(graphicsDevice, "empty-scene.gltf");
		DrModel readable = assets.LoadDrModel(
			graphicsDevice,
			"empty-scene.gltf",
			ModelLoadFlags.ReadableBuffers);

		Assert.Equal(3, model.Bones.Length);
		Assert.Equal(3, readable.Bones.Length);
		Assert.Same(model, cached);
		Assert.NotSame(model, readable);
	}

	[Fact]
	public void LoadDrModel_NullAssetManager_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(
			() => DrModelAssetManagerExtensions.LoadDrModel(null!, null!, "model.gltf"));
	}

	[Fact]
	public void LoadDrModel_NullGraphicsDevice_ThrowsArgumentNullException()
	{
		AssetManager assetManager = AssetManager.CreateFileAssetManager(AppContext.BaseDirectory);

		Assert.Throws<ArgumentNullException>(() => assetManager.LoadDrModel(null!, "model.gltf"));
	}

	[Fact]
	public void Load_NullPath_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => GltfLoader.Load((string)null!));
	}

	[Fact]
	public void Load_AnimatedGltf_LoadsClip()
	{
		DrModel model = GltfLoader.Load(TestDataPath("animated-translation.gltf"));

		AnimationClip clip = Assert.Single(model.Animations).Value;
		Assert.Equal("Move", clip.Name);
		Assert.Equal(1f, clip.Duration);
		Assert.Equal("Move", Assert.Single(model.Animations).Key);
	}

	[Fact]
	public void Load_AnimatedGltf_BakesTranslationKeyframes()
	{
		DrModel model = GltfLoader.Load(TestDataPath("animated-translation.gltf"));
		AnimationClip clip = model.Animations["Move"];
		AnimationChannel channel = Assert.Single(clip.Channels);

		Assert.Equal("Bone", model.Bones[channel.BoneIndex].Name);
		Assert.Equal(AnimationInterpolation.Linear, channel.TranslationInterpolation);
		Assert.Equal(AnimationInterpolation.Linear, channel.RotationInterpolation);
		Assert.Equal(AnimationInterpolation.Linear, channel.ScaleInterpolation);
		Assert.Equal(2, channel.Keyframes.Length);

		Assert.Equal(TimeSpan.Zero, channel.Keyframes[0].Time);
		Assert.Equal(
			new SrtTransform(Vector3.Zero, Quaternion.Identity, Vector3.One),
			channel.Keyframes[0].Pose);

		Assert.Equal(TimeSpan.FromSeconds(1), channel.Keyframes[1].Time);
		Assert.Equal(
			new SrtTransform(new Vector3(1, 2, 3), Quaternion.Identity, Vector3.One),
			channel.Keyframes[1].Pose);
	}

	[Fact]
	public void Load_AnimatedGltf_ResamplesMixedTracksAtUnionOfTimes()
	{
		DrModel model = GltfLoader.Load(TestDataPath("animated-resampled.gltf"));
		AnimationClip clip = model.Animations["Mixed"];
		AnimationChannel channel = Assert.Single(clip.Channels);

		Assert.Equal(3f, clip.Duration);
		Assert.Equal(AnimationInterpolation.Step, channel.TranslationInterpolation);
		Assert.Equal(AnimationInterpolation.Linear, channel.RotationInterpolation);
		Assert.Equal(AnimationInterpolation.Linear, channel.ScaleInterpolation);
		Assert.Equal(4, channel.Keyframes.Length);

		Assert.Equal(TimeSpan.Zero, channel.Keyframes[0].Time);
		Assert.Equal(Vector3.Zero, channel.Keyframes[0].Pose.Translation);
		Assert.Equal(Vector3.One, channel.Keyframes[0].Pose.Scale);

		Assert.Equal(TimeSpan.FromSeconds(1), channel.Keyframes[1].Time);
		Assert.Equal(Vector3.Zero, channel.Keyframes[1].Pose.Translation);
		Assert.Equal(Vector3.One, channel.Keyframes[1].Pose.Scale);

		Assert.Equal(TimeSpan.FromSeconds(2), channel.Keyframes[2].Time);
		Assert.Equal(new Vector3(2, 0, 0), channel.Keyframes[2].Pose.Translation);
		Assert.Equal(new Vector3(2, 2, 2), channel.Keyframes[2].Pose.Scale);

		Assert.Equal(TimeSpan.FromSeconds(3), channel.Keyframes[3].Time);
		Assert.Equal(new Vector3(2, 0, 0), channel.Keyframes[3].Pose.Translation);
		Assert.Equal(new Vector3(3, 3, 3), channel.Keyframes[3].Pose.Scale);
	}

	[Fact]
	public void Load_WithoutAnimations_ReturnsEmptyDictionary()
	{
		DrModel model = GltfLoader.Load(TestDataPath("triangle.gltf"));

		Assert.Empty(model.Animations);
	}

	[Fact]
	public void Load_DuplicateAnimationNames_GeneratesUniqueKeys()
	{
		DrModel model = GltfLoader.Load(TestDataPath("animated-duplicate-names.gltf"));

		Assert.Equal(2, model.Animations.Count);
		Assert.True(model.Animations.ContainsKey("Move"));
		Assert.True(model.Animations.ContainsKey("Move_1"));

		AnimationChannel first = Assert.Single(model.Animations["Move"].Channels);
		AnimationChannel second = Assert.Single(model.Animations["Move_1"].Channels);
		Assert.Equal("BoneA", model.Bones[first.BoneIndex].Name);
		Assert.Equal("BoneB", model.Bones[second.BoneIndex].Name);
	}

	private static DrModelMeshPart LoadFirstPart(
		string fileName,
		ModelLoadFlags flags = ModelLoadFlags.None)
	{
		DrModel model = GltfLoader.Load(TestDataPath(fileName), flags: flags);
		DrModelMesh mesh = Assert.Single(model.Meshes);
		return Assert.Single(mesh.MeshParts);
	}

	private static string TestDataPath(string fileName)
	{
		return Path.Combine(TestDataDirectory(), fileName);
	}

	private static string TestDataDirectory()
	{
		return Path.Combine(AppContext.BaseDirectory, "TestData");
	}
}
