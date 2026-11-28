using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

/// <summary>
/// Creates common mesh primitives as <see cref="DrModelMeshPart"/> objects.
/// </summary>
/// <remarks>
/// <para>
/// Every primitive uses the <see cref="VertexPositionNormalTexture"/> layout: a position,
/// a unit normal and a texture coordinate. Skinning, tangent, vertex color and second
/// texture-coordinate data are not produced.
/// </para>
/// <para>
/// By default front faces are clockwise, matching MonoGame's default
/// <see cref="RasterizerState.CullCounterClockwise"/> state and the winding produced by
/// <see cref="DrModelAssetManagerExtensions.LoadDrModel"/>. Pass
/// <c>cullClockwise: true</c> to emit counter-clockwise front faces for
/// <see cref="RasterizerState.CullClockwise"/> instead. Primitives are centered on the
/// origin and use Y as their up axis.
/// </para>
/// <para>
/// Each public factory requires a <see cref="GraphicsDevice"/>, creates write-only vertex
/// and index buffers, and selects 16-bit or 32-bit indices automatically. The returned part
/// has <see cref="DrModelMeshPart.Material"/> unset; assign a material through the caller's
/// renderer.
/// </para>
/// </remarks>
public static partial class MeshPrimitives
{
	private const int MinSlices = 3;
	private const int MinStacks = 2;

	private static MeshBuilder<VertexPositionNormalTexture> CreateBuilder(bool cullClockwise)
	{
		return new MeshBuilder<VertexPositionNormalTexture>(
			vertex => vertex.Position,
			cullClockwise);
	}
}
