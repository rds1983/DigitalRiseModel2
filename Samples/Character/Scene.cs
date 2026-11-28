using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character;

/// <summary>
/// Holds everything the sample draws: the field built with <see cref="MeshPrimitives"/>, the
/// skinned character, and the state that describes where the character is and how it moves.
/// </summary>
internal sealed class Scene : IDisposable
{
	public const float FieldSize = 40f;

	private const float MoveSpeed = 4f;
	private const float FieldMargin = 0.5f;

	// The Mixamo character stands with its back to +Z after the loader's winding fix.
	private static readonly Vector3 ModelForward = Vector3.UnitZ;

	/// <summary>
	/// The character's facing angle around the Y axis, in radians.
	/// </summary>
	private float _facing;

	public Scene(GraphicsDevice device, DrModel characterModel)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(characterModel);

		Model = characterModel;
		Instance = new DrModelInstance(characterModel);

		// The field is a wireframe grid so the ground reads as an outline without hiding the
		// character behind a solid plane.
		Field = MeshPrimitives.CreatePlaneLines(device, FieldSize, FieldSize, segmentsX: 40, segmentsZ: 40);
		Field.Material = new DrMaterial
		{
			Name = "Field",
			BaseColorFactor = new Vector4(0.35f, 0.55f, 0.35f, 1f)
		};
	}

	/// <summary>
	/// Gets the character model.
	/// </summary>
	public DrModel Model { get; }

	/// <summary>
	/// Gets the character instance whose pose is animated.
	/// </summary>
	public DrModelInstance Instance { get; }

	/// <summary>
	/// Gets the field mesh part.
	/// </summary>
	public DrModelMeshPart Field { get; }

	/// <summary>
	/// Gets the character's position on the field.
	/// </summary>
	public Vector3 Position { get; private set; }

	/// <summary>
	/// Gets the character's facing direction in world space.
	/// </summary>
	public Vector3 Forward => Vector3.TransformNormal(ModelForward, Matrix.CreateRotationY(_facing));

	/// <summary>
	/// Gets a value indicating whether the character moved this frame.
	/// </summary>
	public bool IsMoving { get; private set; }

	/// <summary>
	/// Draws the field and the character through the renderer. The renderer must have a frame
	/// open, that is, it must be called between <see cref="Renderer.BeginFrame"/> and
	/// <see cref="Renderer.EndFrame"/>.
	/// </summary>
	public void Draw(Renderer renderer)
	{
		ArgumentNullException.ThrowIfNull(renderer);

		renderer.DrawPart(Field, Matrix.Identity);
		renderer.DrawInstance(Instance);
	}

	/// <summary>
	/// Turns the character by the given angle in radians.
	/// </summary>
	public void Turn(float angle)
	{
		_facing += angle;
	}

	/// <summary>
	/// Moves the character relative to its facing. The input has Y for forward and X for strafe
	/// right.
	/// </summary>
	public void Move(Vector2 input, float deltaSeconds)
	{
		IsMoving = input != Vector2.Zero;

		if (!IsMoving)
		{
			return;
		}

		input.Normalize();

		Vector3 forward = Forward;
		Vector3 right = Vector3.Cross(Vector3.Up, forward);

		Position += (forward * input.Y + right * input.X) * (MoveSpeed * deltaSeconds);

		float limit = FieldSize * 0.5f - FieldMargin;
		Position = new Vector3(
			Math.Clamp(Position.X, -limit, limit),
			0f,
			Math.Clamp(Position.Z, -limit, limit));
	}

	/// <summary>
	/// Writes the character's position and facing into the root bone. Call after the animation
	/// controller samples the clip so movement and animation compose.
	/// </summary>
	public void ApplyTransform()
	{
		if (Model.Root is not { } root)
		{
			return;
		}

		Matrix transform =
			Matrix.CreateRotationY(_facing)
			* Matrix.CreateTranslation(Position);

		Instance.SetLocalTransform(root.Index, transform);
	}

	public void Dispose()
	{
		Field.Dispose();

		foreach (DrModelMesh mesh in Model.Meshes)
		{
			mesh.Dispose();
		}
	}
}
