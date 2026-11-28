using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PrimitivesDemo;

/// <summary>
/// Builds the demo scene out of <see cref="MeshPrimitives"/>. Each shape gets its own bone so
/// it can spin independently, and every shape is shown twice: a solid, lit part and a line
/// wireframe offset behind it.
/// </summary>
internal static class PrimitiveScene
{
	public static DrModel Build(GraphicsDevice device)
	{
		ArgumentNullException.ThrowIfNull(device);

		var root = new DrModelBone("Root");
		var children = new List<DrModelBone>();

		AddShape(
			children,
			"Plane",
			new Vector3(-4.5f, 0f, 0f),
			MeshPrimitives.CreatePlane(device, 2f, 2f, 4, 4),
			MeshPrimitives.CreatePlaneLines(device, 2f, 2f, 4, 4),
			new Color(90, 150, 220));

		AddShape(
			children,
			"Box",
			new Vector3(-1.5f, 0f, 0f),
			MeshPrimitives.CreateBox(device, new Vector3(2f, 2f, 2f)),
			MeshPrimitives.CreateBoxLines(device, new Vector3(2f, 2f, 2f)),
			new Color(230, 140, 60));

		AddShape(
			children,
			"Sphere",
			new Vector3(1.5f, 0f, 0f),
			// Built with counter-clockwise front faces and tagged with the matching rasterizer
			// state, demonstrating the cullClockwise option.
			MeshPrimitives.CreateSphere(device, 1f, 32, 16, cullClockwise: true),
			MeshPrimitives.CreateSphereLines(device, 1f, 16, 8),
			new Color(80, 200, 130),
			RasterizerState.CullClockwise);

		AddShape(
			children,
			"Cylinder",
			new Vector3(4.5f, 0f, 0f),
			MeshPrimitives.CreateCylinder(device, 0.8f, 2f, 32),
			MeshPrimitives.CreateCylinderLines(device, 0.8f, 2f, 16),
			new Color(200, 90, 160));

		AddShape(
			children,
			"Cone",
			new Vector3(-4.5f, 0f, 3f),
			MeshPrimitives.CreateCone(device, 0.9f, 2f, 32),
			MeshPrimitives.CreateConeLines(device, 0.9f, 2f, 16),
			new Color(240, 210, 80));

		AddShape(
			children,
			"Capsule",
			new Vector3(-1.5f, 0f, 3f),
			MeshPrimitives.CreateCapsule(device, 0.6f, 2.4f, 32, 12),
			MeshPrimitives.CreateCapsuleLines(device, 0.6f, 2.4f, 16, 6),
			new Color(120, 190, 240));

		AddShape(
			children,
			"Torus",
			new Vector3(1.5f, 0f, 3f),
			MeshPrimitives.CreateTorus(device, 0.9f, 0.35f, 40, 20),
			MeshPrimitives.CreateTorusLines(device, 0.9f, 0.35f, 20, 10),
			new Color(220, 110, 110));

		root.Children = children.ToArray();

		return new DrModel(root);
	}

	private static void AddShape(
		List<DrModelBone> children,
		string name,
		Vector3 offset,
		DrModelMeshPart solidPart,
		DrModelMeshPart linePart,
		Color color,
		RasterizerState? rasterizerState = null)
	{
		solidPart.Material = new DrMaterial
		{
			Name = $"{name}Solid",
			BaseColorFactor = color.ToVector4(),
			MetallicFactor = 0f,
			RoughnessFactor = 0.65f
		};

		linePart.Material = new DrMaterial
		{
			Name = $"{name}Lines",
			BaseColorFactor = Color.Lerp(color, Color.White, 0.35f).ToVector4(),
			Unlit = true
		};

		// The renderer reads Tag to pick the rasterizer state for the solid part, so a part
		// built with cullClockwise: true draws with CullClockwise.
		solidPart.Tag = rasterizerState;

		// The wireframe sits just behind the solid so it reads as an outline.
		var lineShift = Matrix.CreateTranslation(0f, 0f, 0.002f);

		var solidBone = new DrModelBone($"{name}Solid")
		{
			Transform = Matrix.CreateTranslation(offset),
			Mesh = new DrModelMesh([solidPart]) { Name = $"{name}Solid" }
		};

		var lineBone = new DrModelBone($"{name}Lines")
		{
			Transform = Matrix.CreateTranslation(offset) * lineShift,
			Mesh = new DrModelMesh([linePart]) { Name = $"{name}Lines" }
		};

		children.Add(solidBone);
		children.Add(lineBone);
	}
}
