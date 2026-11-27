using System.Runtime.CompilerServices;
using AssetManagementBase;
using DigitalRiseModel;
using DigitalRiseModel.Gltf;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

		Assert.NotNull(part.Skin);
		Assert.Single(part.Skin!.Joints);
		Assert.Single(part.Skin.InverseBindMatrices);
		Assert.Equal(Matrix.Identity, part.Skin.InverseBindMatrices[0]);

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

		Assert.Equal(3, model.Bones.Length);
		Assert.Same(model, cached);
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

	private static DrModelMeshPart LoadFirstPart(string fileName)
	{
		DrModel model = GltfLoader.Load(TestDataPath(fileName));
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
