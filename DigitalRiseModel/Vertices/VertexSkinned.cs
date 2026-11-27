using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace DigitalRiseModel.Vertices;

/// <summary>
/// Vertex layout containing position, normal, texture coordinate and skinning data.
/// </summary>
public struct VertexSkinned : IVertexType
{
	/// <summary>
	/// The vertex declaration for this format.
	/// </summary>
	public static readonly VertexDeclaration VertexDeclaration = new(
		52,
		new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
		new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
		new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
		new VertexElement(32, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0),
		new VertexElement(36, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0));

	/// <summary>
	/// The vertex position.
	/// </summary>
	public Vector3 Position;

	/// <summary>
	/// The vertex normal.
	/// </summary>
	public Vector3 Normal;

	/// <summary>
	/// The first texture coordinate set.
	/// </summary>
	public Vector2 TextureCoordinate;

	/// <summary>
	/// The four joint indices used for skinning.
	/// </summary>
	public Byte4 BlendIndices;

	/// <summary>
	/// The four joint blend weights used for skinning.
	/// </summary>
	public Vector4 BlendWeights;

	/// <summary>
	/// Initializes a new instance of the <see cref="VertexSkinned"/> structure.
	/// </summary>
	public VertexSkinned(
		Vector3 position,
		Vector3 normal,
		Vector2 textureCoordinate,
		Byte4 blendIndices,
		Vector4 blendWeights)
	{
		Position = position;
		Normal = normal;
		TextureCoordinate = textureCoordinate;
		BlendIndices = blendIndices;
		BlendWeights = blendWeights;
	}

	/// <inheritdoc />
	VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}
