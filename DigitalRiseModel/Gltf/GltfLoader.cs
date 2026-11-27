using System.Buffers.Binary;
using System.Text.Json;
using AssetManagementBase;
using glTFLoader;
using glTFLoader.Schema;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using DigitalRiseModel.Vertices;
using GltfDocument = glTFLoader.Schema.Gltf;
using GltfImage = glTFLoader.Schema.Image;
using GltfMaterial = glTFLoader.Schema.Material;
using GltfMesh = glTFLoader.Schema.Mesh;
using GltfNode = glTFLoader.Schema.Node;
using GltfPrimitive = glTFLoader.Schema.MeshPrimitive;
using GltfSkin = glTFLoader.Schema.Skin;
using GltfTexture = glTFLoader.Schema.Texture;

namespace DigitalRiseModel.Gltf;

/// <summary>
/// Loads glTF 2.0 files into the DigitalRiseModel data model using the Khronos
/// <c>glTF2Loader</c> library.
/// </summary>
/// <remarks>
/// Animation clips are loaded as raw keyframes into <see cref="DrModel.Animations"/>; the
/// loader does not sample or apply them. Only triangle lists, triangle strips and triangle
/// fans are supported. Vertex buffers use a dynamic declaration generated from the
/// primitive's glTF attributes, including multiple texture coordinate and color sets.
/// </remarks>
internal static partial class GltfLoader
{
	internal static DrModel Load(
		AssetManager assetManager,
		GraphicsDevice graphicsDevice,
		string assetName,
		int? sceneIndex = null,
		ModelLoadFlags flags = ModelLoadFlags.None)
	{
		ArgumentNullException.ThrowIfNull(assetManager);
		ArgumentNullException.ThrowIfNull(graphicsDevice);
		ArgumentNullException.ThrowIfNull(assetName);

		using Stream stream = assetManager.Open(assetName);
		GltfDocument document = Interface.LoadModel(stream)
			?? throw new InvalidDataException("The glTF loader returned no document.");

		byte[] LoadExternalBuffer(string? uri)
		{
			if (string.IsNullOrEmpty(uri))
			{
				using Stream glbStream = assetManager.Open(assetName);
				return Interface.LoadBinaryBuffer(glbStream);
			}

			return assetManager.ReadAsByteArray(Uri.UnescapeDataString(uri));
		}

		byte[] GetBuffer(int bufferIndex)
		{
			return document.LoadBinaryBuffer(bufferIndex, uri => LoadExternalBuffer(uri));
		}

		var textureCache = new Dictionary<int, Texture2D?>();

		Texture2D? GetTexture(int textureIndex)
		{
			if (textureCache.TryGetValue(textureIndex, out Texture2D? cachedTexture))
			{
				return cachedTexture;
			}

			GltfTexture[] textures = document.Textures ?? [];

			if ((uint)textureIndex >= (uint)textures.Length)
			{
				throw new InvalidDataException(
					$"Texture index {textureIndex} is outside the texture collection.");
			}

			GltfTexture texture = textures[textureIndex];

			if (texture.Source is not int imageIndex)
			{
				textureCache[textureIndex] = null;
				return null;
			}

			GltfImage[] images = document.Images ?? [];

			if ((uint)imageIndex >= (uint)images.Length)
			{
				throw new InvalidDataException(
					$"Image index {imageIndex} is outside the image collection.");
			}

			GltfImage image = images[imageIndex];
			Texture2D result;

			if (!string.IsNullOrEmpty(image.Uri)
				&& !image.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			{
				result = assetManager.LoadTexture2D(
					graphicsDevice,
					Uri.UnescapeDataString(image.Uri));
			}
			else
			{
				using Stream imageStream = Interface.OpenImageFile(
					document,
					imageIndex,
					uri => LoadExternalBuffer(uri));

				using var imageData = new MemoryStream();
				imageStream.CopyTo(imageData);

				var imageManager = new AssetManager(
					new MemoryAssetAccessor(imageData.ToArray()),
					string.Empty);

				result = imageManager.LoadTexture2D(graphicsDevice, "image");
			}

			textureCache[textureIndex] = result;
			return result;
		}

		return Load(graphicsDevice, document, sceneIndex, flags, GetBuffer, GetTexture);
	}

	internal static DrModel Load(
		string path,
		int? sceneIndex = null,
		ModelLoadFlags flags = ModelLoadFlags.None)
	{
		ArgumentNullException.ThrowIfNull(path);

		GltfDocument document = Interface.LoadModel(path)
			?? throw new InvalidDataException("The glTF loader returned no document.");

		return Load(
			null,
			document,
			sceneIndex,
			flags,
			index => document.LoadBinaryBuffer(index, path),
			null);
	}

	private static DrModel Load(
		GraphicsDevice? graphicsDevice,
		GltfDocument document,
		int? sceneIndex,
		ModelLoadFlags flags,
		Func<int, byte[]> bufferProvider,
		Func<int, Texture2D?>? textureProvider)
	{
		GltfNode[] nodes = document.Nodes ?? [];
		var nodeBones = new DrModelBone?[nodes.Length];
		var nodeGlobalTransforms = new Dictionary<int, Matrix>();
		var bufferCache = new Dictionary<int, byte[]>();

		DrModelBone CreateBone(int nodeIndex, Matrix parentGlobal)
		{
			if ((uint)nodeIndex >= (uint)nodes.Length)
			{
				throw new InvalidDataException($"Node index {nodeIndex} is outside the node collection.");
			}

			if (nodeBones[nodeIndex] is not null)
			{
				throw new InvalidDataException(
					$"Node {nodeIndex} is referenced more than once or the node hierarchy contains a cycle.");
			}

			GltfNode node = nodes[nodeIndex];
			Matrix localTransform = GetNodeTransform(node);
			Matrix globalTransform = localTransform * parentGlobal;

			var bone = new DrModelBone(string.IsNullOrEmpty(node.Name) ? $"Node{nodeIndex}" : node.Name)
			{
				Transform = localTransform
			};

			nodeBones[nodeIndex] = bone;
			nodeGlobalTransforms[nodeIndex] = globalTransform;

			if (node.Children is { Length: > 0 })
			{
				bone.Children = node.Children
					.Select(childIndex => CreateBone(childIndex, globalTransform))
					.ToArray();
			}

			return bone;
		}

		int[] rootNodeIndices = GetRootNodeIndices(document, sceneIndex);
		var rootBone = new DrModelBone("Root");

		if (rootNodeIndices.Length > 0)
		{
			rootBone.Children = rootNodeIndices
				.Select(rootIndex => CreateBone(rootIndex, Matrix.Identity))
				.ToArray();
		}

		DrMaterial[] materials = ConvertMaterials(document, textureProvider);
		DrSkin?[] skins = ConvertSkins(document, nodeBones, bufferProvider, bufferCache);

		for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
		{
			if (nodes[nodeIndex].Mesh is not int meshIndex)
			{
				continue;
			}

			DrModelBone bone = nodeBones[nodeIndex]
				?? throw new InvalidDataException($"Node {nodeIndex} was not added to the model hierarchy.");

			DrSkin? skin = nodes[nodeIndex].Skin is int skinIndex
				? GetSkin(skins, skinIndex)
				: null;

			Matrix globalTransform = nodeGlobalTransforms[nodeIndex];

			// glTF uses counter-clockwise front faces when the node's global transform has
			// a positive determinant. MonoGame's CullCounterClockwise state uses clockwise
			// front faces, so reverse those triangles. Negative-determinant nodes are already
			// clockwise and need no conversion.
			bool flipWinding = globalTransform.Determinant() > 0f;

			bone.Mesh = ConvertMesh(
				graphicsDevice,
				document,
				meshIndex,
				materials,
				skin,
				flipWinding,
				flags,
				bufferProvider,
				bufferCache);
		}

		var model = new DrModel(rootBone)
		{
			Animations = ConvertAnimations(document, nodeBones, bufferProvider, bufferCache)
		};

		return model;
	}

	private static DrMaterial[] ConvertMaterials(
		GltfDocument document,
		Func<int, Texture2D?>? textureProvider)
	{
		GltfMaterial[] source = document.Materials ?? [];
		var materials = new DrMaterial[source.Length];

		for (int i = 0; i < source.Length; i++)
		{
			materials[i] = ConvertMaterial(source[i], i, textureProvider);
		}

		return materials;
	}

	private static DrMaterial ConvertMaterial(
		GltfMaterial material,
		int index,
		Func<int, Texture2D?>? textureProvider)
	{
		var result = new DrMaterial
		{
			Name = string.IsNullOrEmpty(material.Name) ? $"Material{index}" : material.Name,
			AlphaMode = ConvertAlphaMode(material.AlphaMode),
			AlphaCutoff = material.AlphaCutoff,
			DoubleSided = material.DoubleSided,
			EmissiveFactor = ToVector3(material.EmissiveFactor),
			EmissiveStrength = ReadEmissiveStrength(material)
		};

		if (material.PbrMetallicRoughness is { } pbr)
		{
			result.BaseColorFactor = ToVector4(pbr.BaseColorFactor);
			result.BaseColorTexture = ConvertTextureInfo(pbr.BaseColorTexture, textureProvider);
			result.MetallicFactor = pbr.MetallicFactor;
			result.RoughnessFactor = pbr.RoughnessFactor;
			result.MetallicRoughnessTexture = ConvertTextureInfo(pbr.MetallicRoughnessTexture, textureProvider);
		}

		result.NormalTexture = ConvertTextureInfo(
			material.NormalTexture,
			textureProvider,
			scale: material.NormalTexture?.Scale ?? 1f);

		result.OcclusionTexture = ConvertTextureInfo(
			material.OcclusionTexture,
			textureProvider,
			strength: material.OcclusionTexture?.Strength ?? 1f);

		result.EmissiveTexture = ConvertTextureInfo(material.EmissiveTexture, textureProvider);

		return result;
	}

	private static float ReadEmissiveStrength(GltfMaterial material)
	{
		if (material.Extensions is null
			|| !material.Extensions.TryGetValue(
				"KHR_materials_emissive_strength",
				out object? extension)
			|| extension is not JsonElement element
			|| element.ValueKind != JsonValueKind.Object
			|| !element.TryGetProperty(
				"emissiveStrength",
				out JsonElement strengthElement))
		{
			return 1f;
		}

		if (strengthElement.ValueKind != JsonValueKind.Number
			|| !strengthElement.TryGetSingle(out float strength)
			|| !float.IsFinite(strength)
			|| strength < 0f)
		{
			throw new InvalidDataException(
				"KHR_materials_emissive_strength has an invalid emissiveStrength value.");
		}

		return strength;
	}

	private static DrTextureInfo? ConvertTextureInfo(
		TextureInfo? info,
		Func<int, Texture2D?>? textureProvider,
		float scale = 1f,
		float strength = 1f)
	{
		if (info is null)
		{
			return null;
		}

		return new DrTextureInfo
		{
			TextureIndex = info.Index,
			TexCoord = info.TexCoord,
			Scale = scale,
			Strength = strength,
			Texture = textureProvider?.Invoke(info.Index)
		};
	}

	private static DrAlphaMode ConvertAlphaMode(GltfMaterial.AlphaModeEnum alphaMode)
	{
		return alphaMode.Value switch
		{
			"MASK" => DrAlphaMode.Mask,
			"BLEND" => DrAlphaMode.Blend,
			_ => DrAlphaMode.Opaque
		};
	}

	private static DrSkin?[] ConvertSkins(
		GltfDocument document,
		DrModelBone?[] nodeBones,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		GltfSkin[] source = document.Skins ?? [];
		var skins = new DrSkin?[source.Length];

		for (int i = 0; i < source.Length; i++)
		{
			GltfSkin skin = source[i];
			DrModelBone GetBone(int nodeIndex)
			{
				if ((uint)nodeIndex >= (uint)nodeBones.Length || nodeBones[nodeIndex] is null)
				{
					throw new InvalidDataException(
						$"Skin {i} references node {nodeIndex}, which is not part of the loaded scene.");
				}

				return nodeBones[nodeIndex]!;
			}

			int[] jointNodeIndices = skin.Joints ?? [];
			Matrix[] inverseBindMatrices;

			if (skin.InverseBindMatrices is int inverseBindAccessor)
			{
				inverseBindMatrices = ReadMatrices(document, inverseBindAccessor, bufferProvider, bufferCache);

				if (inverseBindMatrices.Length != jointNodeIndices.Length)
				{
					throw new InvalidDataException(
						$"Skin {i} has {jointNodeIndices.Length} joints but " +
						$"{inverseBindMatrices.Length} inverse bind matrices.");
				}
			}
			else
			{
				inverseBindMatrices = Enumerable
					.Repeat(Matrix.Identity, jointNodeIndices.Length)
					.ToArray();
			}

			var joints = new DrSkinJoint[jointNodeIndices.Length];

			for (int jointIndex = 0; jointIndex < jointNodeIndices.Length; jointIndex++)
			{
				DrModelBone bone = GetBone(jointNodeIndices[jointIndex]);
				joints[jointIndex] = new DrSkinJoint(bone, inverseBindMatrices[jointIndex]);
			}

			DrModelBone? skeleton = skin.Skeleton is int skeletonIndex
				? GetBone(skeletonIndex)
				: null;

			skins[i] = new DrSkin(i)
			{
				Name = string.IsNullOrEmpty(skin.Name) ? $"Skin{i}" : skin.Name,
				Skeleton = skeleton,
				Joints = joints
			};
		}

		return skins;
	}

	private static DrSkin GetSkin(DrSkin?[] skins, int index)
	{
		if ((uint)index >= (uint)skins.Length || skins[index] is null)
		{
			throw new InvalidDataException($"Skin index {index} is outside the skin collection.");
		}

		return skins[index]!;
	}

	private static DrModelMesh ConvertMesh(
		GraphicsDevice? graphicsDevice,
		GltfDocument document,
		int meshIndex,
		DrMaterial[] materials,
		DrSkin? skin,
		bool flipWinding,
		ModelLoadFlags flags,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		GltfMesh[] meshes = document.Meshes ?? [];

		if ((uint)meshIndex >= (uint)meshes.Length)
		{
			throw new InvalidDataException($"Mesh index {meshIndex} is outside the mesh collection.");
		}

		GltfMesh mesh = meshes[meshIndex];
		GltfPrimitive[] primitives = mesh.Primitives ?? [];
		var parts = new DrModelMeshPart[primitives.Length];

		for (int i = 0; i < primitives.Length; i++)
		{
			parts[i] = ConvertPrimitive(
				graphicsDevice,
				document,
				primitives[i],
				materials,
				skin,
				flipWinding,
				flags,
				bufferProvider,
				bufferCache);
		}

		return new DrModelMesh(parts)
		{
			Name = string.IsNullOrEmpty(mesh.Name) ? $"Mesh{meshIndex}" : mesh.Name
		};
	}

	private static DrModelMeshPart ConvertPrimitive(
		GraphicsDevice? graphicsDevice,
		GltfDocument document,
		GltfPrimitive primitive,
		DrMaterial[] materials,
		DrSkin? skin,
		bool flipWinding,
		ModelLoadFlags flags,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		Dictionary<string, int> attributes = primitive.Attributes
			?? throw new InvalidDataException("A mesh primitive has no attributes.");

		var vertexAttributes = new List<VertexAttribute>();
		int vertexCount = -1;

		foreach (KeyValuePair<string, int> attribute in attributes
			.OrderBy(pair => GetAttributeOrder(pair.Key))
			.ThenBy(pair => pair.Key, StringComparer.Ordinal))
		{
			if (!TryGetVertexUsage(
				attribute.Key,
				out VertexElementUsage usage,
				out int usageIndex,
				out int expectedComponents))
			{
				continue;
			}

			float[] values;
			int components;

			if (usage == VertexElementUsage.BlendIndices)
			{
				(uint[] rawValues, int rawComponents) = ReadUInts(
					document,
					attribute.Value,
					bufferProvider,
					bufferCache);

				components = rawComponents;
				values = new float[rawValues.Length];

				for (int i = 0; i < rawValues.Length; i++)
				{
					values[i] = rawValues[i];
				}
			}
			else
			{
				(values, components) = ReadFloats(document, attribute.Value, bufferProvider, bufferCache);
			}

			ValidateAttributeComponents(attribute.Key, usage, components, expectedComponents);

			int attributeVertexCount = values.Length / components;

			if (vertexCount < 0)
			{
				vertexCount = attributeVertexCount;
			}
			else if (vertexCount != attributeVertexCount)
			{
				throw new InvalidDataException("Vertex attributes have different vertex counts.");
			}

			vertexAttributes.Add(new VertexAttribute(
				attribute.Key,
				usage,
				usageIndex,
				components,
				values));
		}

		if (vertexCount < 0)
		{
			throw new InvalidDataException("A mesh primitive has no supported vertex attributes.");
		}

		VertexAttribute? positionAttribute = vertexAttributes.FirstOrDefault(
			attribute => attribute.Usage == VertexElementUsage.Position && attribute.UsageIndex == 0);

		if (positionAttribute is null)
		{
			throw new InvalidDataException("A mesh primitive has no POSITION attribute.");
		}

		if (flags.HasFlag(ModelLoadFlags.EnsureUVs)
			&& !vertexAttributes.Any(
				attribute => attribute.Usage == VertexElementUsage.TextureCoordinate
					&& attribute.UsageIndex == 0))
		{
			var zeroUVs = new float[vertexCount * 2];

			vertexAttributes.Add(new VertexAttribute(
				"TEXCOORD_0",
				VertexElementUsage.TextureCoordinate,
				0,
				2,
				zeroUVs));
		}

		if (flags.HasFlag(ModelLoadFlags.EnsureUV1)
			&& !vertexAttributes.Any(
				attribute => attribute.Usage == VertexElementUsage.TextureCoordinate
					&& attribute.UsageIndex == 1))
		{
			var zeroUVs = new float[vertexCount * 2];

			vertexAttributes.Add(new VertexAttribute(
				"TEXCOORD_1",
				VertexElementUsage.TextureCoordinate,
				1,
				2,
				zeroUVs));
		}

		uint[] sourceIndices;

		if (primitive.Indices is int indexAccessor)
		{
			(uint[] values, int components) = ReadUInts(document, indexAccessor, bufferProvider, bufferCache);
			RequireComponents(components, 1, "indices");
			sourceIndices = values;
		}
		else
		{
			sourceIndices = new uint[vertexCount];

			for (int i = 0; i < vertexCount; i++)
			{
				sourceIndices[i] = (uint)i;
			}
		}

		int[] indices = BuildTriangleList(sourceIndices, primitive.Mode);

		if (flipWinding)
		{
			ReverseWinding(indices);
		}

		foreach (int index in indices)
		{
			if ((uint)index >= (uint)vertexCount)
			{
				throw new InvalidDataException($"Index {index} is outside the vertex range 0..{vertexCount - 1}.");
			}
		}

		int stride = 0;
		var elements = new VertexElement[vertexAttributes.Count];

		for (int i = 0; i < vertexAttributes.Count; i++)
		{
			VertexAttribute attribute = vertexAttributes[i];
			elements[i] = new VertexElement(
				stride,
				GetVertexElementFormat(attribute.Components),
				attribute.Usage,
				attribute.UsageIndex);

			stride += attribute.Components * sizeof(float);
		}

		var declaration = new VertexDeclaration(stride, elements);
		var vertexData = new float[vertexCount * (stride / sizeof(float))];
		int dataIndex = 0;

		for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
		{
			foreach (VertexAttribute attribute in vertexAttributes)
			{
				int sourceOffset = vertexIndex * attribute.Components;

				for (int component = 0; component < attribute.Components; component++)
				{
					vertexData[dataIndex++] = attribute.Values[sourceOffset + component];
				}
			}
		}

		VertexBuffer? vertexBuffer = null;
		IndexBuffer? indexBuffer = null;

		if (graphicsDevice is not null)
		{
			vertexBuffer = new VertexBuffer(
				graphicsDevice,
				declaration,
				vertexCount,
				GetBufferUsage(flags));

			vertexBuffer.SetData(vertexData);

			bool useSixteenBitIndices = indices.Length == 0 || indices.Max() <= ushort.MaxValue;

			if (useSixteenBitIndices)
			{
				var indexData = new ushort[indices.Length];

				for (int i = 0; i < indices.Length; i++)
				{
					indexData[i] = (ushort)indices[i];
				}

				indexBuffer = new IndexBuffer(
					graphicsDevice,
					IndexElementSize.SixteenBits,
					indexData.Length,
					GetBufferUsage(flags));

				indexBuffer.SetData(indexData);
			}
			else
			{
				indexBuffer = new IndexBuffer(
					graphicsDevice,
					IndexElementSize.ThirtyTwoBits,
					indices.Length,
					GetBufferUsage(flags));

				indexBuffer.SetData(indices);
			}
		}

		DrMaterial? material = primitive.Material is int materialIndex
			? GetMaterial(materials, materialIndex)
			: null;

		int[]? partSourceIndices = null;
		VertexSkinned[]? partSourceVertices = null;

		if (flags.HasFlag(ModelLoadFlags.KeepSourceGeometry))
		{
			partSourceIndices = indices;
			partSourceVertices = CreateSourceVertices(vertexAttributes, vertexCount);
		}

		return new DrModelMeshPart
		{
			VertexBuffer = vertexBuffer,
			IndexBuffer = indexBuffer,
			SourceIndices = partSourceIndices,
			SourceVertices = partSourceVertices,
			VertexDeclaration = declaration,
			NumVertices = vertexCount,
			PrimitiveCount = indices.Length / 3,
			StartIndex = 0,
			VertexOffset = 0,
			BoundingSphere = ComputeBoundingSphere(
				positionAttribute.Values,
				vertexCount,
				positionAttribute.Components),
			Material = material,
			Skin = skin
		};
	}

	private static bool TryGetVertexUsage(
		string name,
		out VertexElementUsage usage,
		out int usageIndex,
		out int expectedComponents)
	{
		usage = default;
		usageIndex = 0;
		expectedComponents = 0;

		if (name == "POSITION")
		{
			usage = VertexElementUsage.Position;
			expectedComponents = 3;
			return true;
		}

		if (name == "NORMAL")
		{
			usage = VertexElementUsage.Normal;
			expectedComponents = 3;
			return true;
		}

		if (name == "TANGENT")
		{
			usage = VertexElementUsage.Tangent;
			expectedComponents = 4;
			return true;
		}

		if (TryGetAttributeIndex(name, "TEXCOORD_", out usageIndex))
		{
			usage = VertexElementUsage.TextureCoordinate;
			expectedComponents = 2;
			return true;
		}

		if (TryGetAttributeIndex(name, "COLOR_", out usageIndex))
		{
			usage = VertexElementUsage.Color;
			expectedComponents = 0;
			return true;
		}

		if (TryGetAttributeIndex(name, "JOINTS_", out usageIndex))
		{
			usage = VertexElementUsage.BlendIndices;
			expectedComponents = 4;
			return true;
		}

		if (TryGetAttributeIndex(name, "WEIGHTS_", out usageIndex))
		{
			usage = VertexElementUsage.BlendWeight;
			expectedComponents = 4;
			return true;
		}

		return false;
	}

	private static int GetAttributeOrder(string name)
	{
		if (name == "POSITION")
		{
			return 0;
		}

		if (name == "NORMAL")
		{
			return 1;
		}

		if (name == "TANGENT")
		{
			return 2;
		}

		if (TryGetAttributeIndex(name, "TEXCOORD_", out int texCoordIndex))
		{
			return 10 + texCoordIndex;
		}

		if (TryGetAttributeIndex(name, "COLOR_", out int colorIndex))
		{
			return 100 + colorIndex;
		}

		if (TryGetAttributeIndex(name, "JOINTS_", out int jointsIndex))
		{
			return 200 + jointsIndex;
		}

		if (TryGetAttributeIndex(name, "WEIGHTS_", out int weightsIndex))
		{
			return 300 + weightsIndex;
		}

		return 1000;
	}

	private static bool TryGetAttributeIndex(string name, string prefix, out int index)
	{
		index = -1;

		if (!name.StartsWith(prefix, StringComparison.Ordinal))
		{
			return false;
		}

		return int.TryParse(name.AsSpan(prefix.Length), out index) && index >= 0;
	}

	private static void ValidateAttributeComponents(
		string name,
		VertexElementUsage usage,
		int actual,
		int expected)
	{
		if (usage == VertexElementUsage.Color)
		{
			if (actual != 3 && actual != 4)
			{
				throw new InvalidDataException($"The {name} attribute must be VEC3 or VEC4.");
			}

			return;
		}

		if (actual != expected)
		{
			throw new InvalidDataException(
				$"The {name} attribute has {actual} components; expected {expected}.");
		}
	}

	private static VertexElementFormat GetVertexElementFormat(int components)
	{
		return components switch
		{
			1 => VertexElementFormat.Single,
			2 => VertexElementFormat.Vector2,
			3 => VertexElementFormat.Vector3,
			4 => VertexElementFormat.Vector4,
			_ => throw new InvalidDataException(
				$"A vertex attribute with {components} components cannot be represented.")
		};
	}

	private sealed class VertexAttribute
	{
		public VertexAttribute(
			string name,
			VertexElementUsage usage,
			int usageIndex,
			int components,
			float[] values)
		{
			Name = name;
			Usage = usage;
			UsageIndex = usageIndex;
			Components = components;
			Values = values;
		}

		public string Name { get; }

		public VertexElementUsage Usage { get; }

		public int UsageIndex { get; }

		public int Components { get; }

		public float[] Values { get; }
	}

	private static DrMaterial GetMaterial(DrMaterial[] materials, int index)
	{
		if ((uint)index >= (uint)materials.Length)
		{
			throw new InvalidDataException($"Material index {index} is outside the material collection.");
		}

		return materials[index];
	}

	private static Matrix GetNodeTransform(GltfNode node)
	{
		if (node.ShouldSerializeMatrix())
		{
			return ToMatrix(node.Matrix);
		}

		float[] translation = node.Translation;
		float[] rotation = node.Rotation;
		float[] scale = node.Scale;

		return Matrix.CreateScale(scale[0], scale[1], scale[2])
			* Matrix.CreateFromQuaternion(new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]))
			* Matrix.CreateTranslation(translation[0], translation[1], translation[2]);
	}

	private static Matrix ToMatrix(float[] values)
	{
		return new Matrix(
			values[0], values[1], values[2], values[3],
			values[4], values[5], values[6], values[7],
			values[8], values[9], values[10], values[11],
			values[12], values[13], values[14], values[15]);
	}

	private static int[] GetRootNodeIndices(GltfDocument document, int? sceneIndex)
	{
		if (document.Scenes is { Length: > 0 } scenes)
		{
			int index = sceneIndex ?? document.Scene ?? 0;

			if ((uint)index >= (uint)scenes.Length)
			{
				throw new ArgumentOutOfRangeException(
					nameof(sceneIndex),
					index,
					"The scene index is outside the scene collection.");
			}

			return scenes[index].Nodes ?? [];
		}

		if (sceneIndex.HasValue)
		{
			throw new InvalidDataException("A scene index was specified, but the file contains no scenes.");
		}

		GltfNode[] nodes = document.Nodes ?? [];
		var childNodes = new HashSet<int>();

		foreach (GltfNode node in nodes)
		{
			if (node.Children is null)
			{
				continue;
			}

			foreach (int child in node.Children)
			{
				childNodes.Add(child);
			}
		}

		return Enumerable
			.Range(0, nodes.Length)
			.Where(index => !childNodes.Contains(index))
			.ToArray();
	}

	private static (float[] Values, int Components) ReadFloats(
		GltfDocument document,
		int accessorIndex,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		Accessor accessor = GetAccessor(document, accessorIndex);
		BufferView view = GetBufferView(document, accessor);
		byte[] buffer = GetBuffer(view.Buffer, bufferProvider, bufferCache);
		int components = GetComponentCount(accessor.Type);
		int componentSize = GetComponentSize(accessor.ComponentType);
		int elementSize = components * componentSize;
		int stride = view.ByteStride ?? elementSize;

		if (stride < elementSize)
		{
			throw new InvalidDataException("A buffer view stride is smaller than its accessor element size.");
		}

		int start = view.ByteOffset + accessor.ByteOffset;
		int required = start + (accessor.Count - 1) * stride + elementSize;

		if (start < 0 || required > buffer.Length)
		{
			throw new InvalidDataException("An accessor reads outside its buffer.");
		}

		var values = new float[accessor.Count * components];

		for (int element = 0; element < accessor.Count; element++)
		{
			int elementOffset = start + element * stride;

			for (int component = 0; component < components; component++)
			{
				values[element * components + component] = ReadFloatComponent(
					buffer,
					elementOffset + component * componentSize,
					accessor.ComponentType,
					accessor.Normalized);
			}
		}

		return (values, components);
	}

	private static (uint[] Values, int Components) ReadUInts(
		GltfDocument document,
		int accessorIndex,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		Accessor accessor = GetAccessor(document, accessorIndex);
		BufferView view = GetBufferView(document, accessor);
		byte[] buffer = GetBuffer(view.Buffer, bufferProvider, bufferCache);
		int components = GetComponentCount(accessor.Type);
		int componentSize = GetComponentSize(accessor.ComponentType);
		int elementSize = components * componentSize;
		int stride = view.ByteStride ?? elementSize;

		if (stride < elementSize)
		{
			throw new InvalidDataException("A buffer view stride is smaller than its accessor element size.");
		}

		int start = view.ByteOffset + accessor.ByteOffset;
		int required = start + (accessor.Count - 1) * stride + elementSize;

		if (start < 0 || required > buffer.Length)
		{
			throw new InvalidDataException("An accessor reads outside its buffer.");
		}

		var values = new uint[accessor.Count * components];

		for (int element = 0; element < accessor.Count; element++)
		{
			int elementOffset = start + element * stride;

			for (int component = 0; component < components; component++)
			{
				values[element * components + component] = ReadUIntComponent(
					buffer,
					elementOffset + component * componentSize,
					accessor.ComponentType);
			}
		}

		return (values, components);
	}

	private static Matrix[] ReadMatrices(
		GltfDocument document,
		int accessorIndex,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		(float[] values, int components) = ReadFloats(document, accessorIndex, bufferProvider, bufferCache);
		RequireComponents(components, 16, "MAT4");

		var matrices = new Matrix[values.Length / 16];

		for (int i = 0; i < matrices.Length; i++)
		{
			int offset = i * 16;
			matrices[i] = new Matrix(
				values[offset], values[offset + 1], values[offset + 2], values[offset + 3],
				values[offset + 4], values[offset + 5], values[offset + 6], values[offset + 7],
				values[offset + 8], values[offset + 9], values[offset + 10], values[offset + 11],
				values[offset + 12], values[offset + 13], values[offset + 14], values[offset + 15]);
		}

		return matrices;
	}

	private static Accessor GetAccessor(GltfDocument document, int accessorIndex)
	{
		if (document.Accessors is null || (uint)accessorIndex >= (uint)document.Accessors.Length)
		{
			throw new InvalidDataException($"Accessor index {accessorIndex} is outside the accessor collection.");
		}

		Accessor accessor = document.Accessors[accessorIndex];

		if (accessor.BufferView is null)
		{
			throw new NotSupportedException("Accessors without a buffer view are not supported.");
		}

		if (accessor.Sparse is not null)
		{
			throw new NotSupportedException("Sparse accessors are not supported.");
		}

		return accessor;
	}

	private static BufferView GetBufferView(GltfDocument document, Accessor accessor)
	{
		int viewIndex = accessor.BufferView
			?? throw new InvalidDataException("The accessor has no buffer view.");

		if (document.BufferViews is null || (uint)viewIndex >= (uint)document.BufferViews.Length)
		{
			throw new InvalidDataException($"Buffer view index {viewIndex} is outside the buffer view collection.");
		}

		return document.BufferViews[viewIndex];
	}

	private static byte[] GetBuffer(
		int bufferIndex,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		if (bufferCache.TryGetValue(bufferIndex, out byte[]? buffer))
		{
			return buffer;
		}

		buffer = bufferProvider(bufferIndex);
		bufferCache[bufferIndex] = buffer;
		return buffer;
	}

	private static int GetComponentCount(Accessor.TypeEnum type)
	{
		return type.Value switch
		{
			"SCALAR" => 1,
			"VEC2" => 2,
			"VEC3" => 3,
			"VEC4" => 4,
			"MAT2" => 4,
			"MAT3" => 9,
			"MAT4" => 16,
			_ => throw new InvalidDataException($"Unknown accessor type '{type.Value}'.")
		};
	}

	private static int GetComponentSize(Accessor.ComponentTypeEnum componentType)
	{
		return componentType switch
		{
			Accessor.ComponentTypeEnum.BYTE => 1,
			Accessor.ComponentTypeEnum.UNSIGNED_BYTE => 1,
			Accessor.ComponentTypeEnum.SHORT => 2,
			Accessor.ComponentTypeEnum.UNSIGNED_SHORT => 2,
			Accessor.ComponentTypeEnum.UNSIGNED_INT => 4,
			Accessor.ComponentTypeEnum.FLOAT => 4,
			_ => throw new InvalidDataException($"Unknown accessor component type '{componentType}'.")
		};
	}

	private static float ReadFloatComponent(
		byte[] buffer,
		int offset,
		Accessor.ComponentTypeEnum componentType,
		bool normalized)
	{
		return componentType switch
		{
			Accessor.ComponentTypeEnum.BYTE => ReadNormalizedByte(buffer, offset, normalized),
			Accessor.ComponentTypeEnum.UNSIGNED_BYTE => ReadNormalizedUnsignedByte(buffer, offset, normalized),
			Accessor.ComponentTypeEnum.SHORT => ReadNormalizedShort(buffer, offset, normalized),
			Accessor.ComponentTypeEnum.UNSIGNED_SHORT => ReadNormalizedUnsignedShort(buffer, offset, normalized),
			Accessor.ComponentTypeEnum.UNSIGNED_INT =>
				BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset, 4)),
			Accessor.ComponentTypeEnum.FLOAT =>
				BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset, 4)),
			_ => throw new InvalidDataException($"Unknown accessor component type '{componentType}'.")
		};
	}

	private static uint ReadUIntComponent(
		byte[] buffer,
		int offset,
		Accessor.ComponentTypeEnum componentType)
	{
		return componentType switch
		{
			Accessor.ComponentTypeEnum.BYTE => ReadSignedByteAsUInt(buffer, offset),
			Accessor.ComponentTypeEnum.UNSIGNED_BYTE => buffer[offset],
			Accessor.ComponentTypeEnum.SHORT => ReadSignedShortAsUInt(buffer, offset),
			Accessor.ComponentTypeEnum.UNSIGNED_SHORT =>
				BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset, 2)),
			Accessor.ComponentTypeEnum.UNSIGNED_INT =>
				BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset, 4)),
			_ => throw new InvalidDataException(
				$"Accessor component type '{componentType}' cannot be read as an unsigned integer.")
		};
	}

	private static float ReadNormalizedByte(byte[] buffer, int offset, bool normalized)
	{
		sbyte value = (sbyte)buffer[offset];
		return normalized ? MathF.Max(value / 127f, -1f) : value;
	}

	private static float ReadNormalizedUnsignedByte(byte[] buffer, int offset, bool normalized)
	{
		byte value = buffer[offset];
		return normalized ? value / 255f : value;
	}

	private static float ReadNormalizedShort(byte[] buffer, int offset, bool normalized)
	{
		short value = BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(offset, 2));
		return normalized ? MathF.Max(value / 32767f, -1f) : value;
	}

	private static float ReadNormalizedUnsignedShort(byte[] buffer, int offset, bool normalized)
	{
		ushort value = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset, 2));
		return normalized ? value / 65535f : value;
	}

	private static uint ReadSignedByteAsUInt(byte[] buffer, int offset)
	{
		sbyte value = (sbyte)buffer[offset];
		return value < 0
			? throw new InvalidDataException("A signed component used as an unsigned integer is negative.")
			: (uint)value;
	}

	private static uint ReadSignedShortAsUInt(byte[] buffer, int offset)
	{
		short value = BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(offset, 2));
		return value < 0
			? throw new InvalidDataException("A signed component used as an unsigned integer is negative.")
			: (uint)value;
	}

	private static int[] BuildTriangleList(uint[] source, GltfPrimitive.ModeEnum mode)
	{
		switch (mode)
		{
			case GltfPrimitive.ModeEnum.TRIANGLES:
				if (source.Length % 3 != 0)
				{
					throw new InvalidDataException("A triangle list must contain a multiple of three indices.");
				}

				return ToInt32(source);

			case GltfPrimitive.ModeEnum.TRIANGLE_STRIP:
				{
					if (source.Length < 3)
					{
						return [];
					}

					var result = new int[(source.Length - 2) * 3];

					for (int i = 0; i < source.Length - 2; i++)
					{
						int offset = i * 3;

						if ((i & 1) == 0)
						{
							result[offset] = ToInt32(source[i]);
							result[offset + 1] = ToInt32(source[i + 1]);
							result[offset + 2] = ToInt32(source[i + 2]);
						}
						else
						{
							result[offset] = ToInt32(source[i + 1]);
							result[offset + 1] = ToInt32(source[i]);
							result[offset + 2] = ToInt32(source[i + 2]);
						}
					}

					return result;
				}

			case GltfPrimitive.ModeEnum.TRIANGLE_FAN:
				{
					if (source.Length < 3)
					{
						return [];
					}

					var result = new int[(source.Length - 2) * 3];

					for (int i = 1; i < source.Length - 1; i++)
					{
						int offset = (i - 1) * 3;
						result[offset] = ToInt32(source[0]);
						result[offset + 1] = ToInt32(source[i]);
						result[offset + 2] = ToInt32(source[i + 1]);
					}

					return result;
				}

			default:
				throw new NotSupportedException(
					$"The glTF primitive mode '{mode}' is not supported. Only triangle primitives are loaded.");
		}
	}

	private static VertexSkinned[] CreateSourceVertices(
		List<VertexAttribute> attributes,
		int vertexCount)
	{
		VertexAttribute position = FindAttribute(attributes, VertexElementUsage.Position)
			?? throw new InvalidDataException("A mesh primitive has no POSITION attribute.");
		VertexAttribute? normal = FindAttribute(attributes, VertexElementUsage.Normal);
		VertexAttribute? texCoord = FindAttribute(attributes, VertexElementUsage.TextureCoordinate);
		VertexAttribute? joints = FindAttribute(attributes, VertexElementUsage.BlendIndices);
		VertexAttribute? weights = FindAttribute(attributes, VertexElementUsage.BlendWeight);

		var result = new VertexSkinned[vertexCount];

		for (int i = 0; i < vertexCount; i++)
		{
			int positionOffset = i * position.Components;
			var positionValue = new Vector3(
				position.Values[positionOffset],
				position.Values[positionOffset + 1],
				position.Values[positionOffset + 2]);

			Vector3 normalValue = Vector3.Zero;
			if (normal is not null)
			{
				int normalOffset = i * normal.Components;
				normalValue = new Vector3(
					normal.Values[normalOffset],
					normal.Values[normalOffset + 1],
					normal.Values[normalOffset + 2]);
			}

			Vector2 texCoordValue = Vector2.Zero;
			if (texCoord is not null)
			{
				int texCoordOffset = i * texCoord.Components;
				texCoordValue = new Vector2(
					texCoord.Values[texCoordOffset],
					texCoord.Values[texCoordOffset + 1]);
			}

			var blendIndices = default(Byte4);
			if (joints is not null)
			{
				int jointsOffset = i * joints.Components;
				blendIndices = new Byte4(
					joints.Values[jointsOffset],
					joints.Values[jointsOffset + 1],
					joints.Values[jointsOffset + 2],
					joints.Values[jointsOffset + 3]);
			}

			Vector4 blendWeights = Vector4.Zero;
			if (weights is not null)
			{
				int weightsOffset = i * weights.Components;
				blendWeights = new Vector4(
					weights.Values[weightsOffset],
					weights.Values[weightsOffset + 1],
					weights.Values[weightsOffset + 2],
					weights.Values[weightsOffset + 3]);
			}

			result[i] = new VertexSkinned(
				positionValue,
				normalValue,
				texCoordValue,
				blendIndices,
				blendWeights);
		}

		return result;
	}

	private static VertexAttribute? FindAttribute(
		List<VertexAttribute> attributes,
		VertexElementUsage usage)
	{
		foreach (VertexAttribute attribute in attributes)
		{
			if (attribute.Usage == usage && attribute.UsageIndex == 0)
			{
				return attribute;
			}
		}

		return null;
	}

	internal static BufferUsage GetBufferUsage(ModelLoadFlags flags)
	{
		return flags.HasFlag(ModelLoadFlags.ReadableBuffers)
			? BufferUsage.None
			: BufferUsage.WriteOnly;
	}

	private static void ReverseWinding(int[] indices)
	{
		for (int i = 0; i + 2 < indices.Length; i += 3)
		{
			(indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
		}
	}

	private static int[] ToInt32(uint[] values)
	{
		var result = new int[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			result[i] = ToInt32(values[i]);
		}

		return result;
	}

	private static int ToInt32(uint value)
	{
		return value > int.MaxValue
			? throw new InvalidDataException($"Index {value} is too large.")
			: (int)value;
	}

	private static BoundingSphere ComputeBoundingSphere(
		float[] positions,
		int vertexCount,
		int components)
	{
		if (vertexCount == 0)
		{
			return default;
		}

		var min = new Vector3(float.MaxValue);
		var max = new Vector3(float.MinValue);

		for (int i = 0; i < vertexCount; i++)
		{
			int offset = i * components;
			var position = new Vector3(positions[offset], positions[offset + 1], positions[offset + 2]);
			min = Vector3.Min(min, position);
			max = Vector3.Max(max, position);
		}

		Vector3 center = (min + max) * 0.5f;
		float radiusSquared = 0f;

		for (int i = 0; i < vertexCount; i++)
		{
			int offset = i * components;
			var position = new Vector3(positions[offset], positions[offset + 1], positions[offset + 2]);
			float distanceSquared = Vector3.DistanceSquared(position, center);
			radiusSquared = MathF.Max(radiusSquared, distanceSquared);
		}

		return new BoundingSphere(center, MathF.Sqrt(radiusSquared));
	}

	private static Vector3 ToVector3(float[]? values)
	{
		return values is { Length: >= 3 }
			? new Vector3(values[0], values[1], values[2])
			: Vector3.Zero;
	}

	private static Vector4 ToVector4(float[]? values)
	{
		return values is { Length: >= 4 }
			? new Vector4(values[0], values[1], values[2], values[3])
			: Vector4.One;
	}

	private static void RequireComponents(int actual, int expected, string attributeName)
	{
		if (actual != expected)
		{
			throw new InvalidDataException(
				$"The {attributeName} attribute has {actual} components; expected {expected}.");
		}
	}

	private sealed class MemoryAssetAccessor : IAssetAccessor
	{
		private readonly byte[] _data;

		public MemoryAssetAccessor(byte[] data)
		{
			_data = data;
		}

		public string Name => "Memory";

		public bool Exists(string path)
		{
			return true;
		}

		public Stream Open(string path)
		{
			return new MemoryStream(_data, writable: false);
		}
	}
}
