using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PrimitivesDemo;

/// <summary>
/// Demonstrates building a scene with <see cref="MeshPrimitives"/>: solid parts, line
/// wireframes, materials, both winding conventions and per-bone animation.
/// </summary>
internal sealed class PrimitivesGame : Game
{
	private readonly GraphicsDeviceManager _graphics;
	private Renderer? _renderer;
	private DrModel? _model;
	private DrModelInstance? _instance;

	private BoundingSphere _bounds;
	private Vector3 _target;
	private float _cameraDistance = 12f;
	private float _yaw = MathHelper.PiOver4;
	private float _pitch = -0.5f;
	private float _spin;
	private bool _paused;
	private MouseState _previousMouse;

	public PrimitivesGame()
	{
		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1280,
			PreferredBackBufferHeight = 720,
			SynchronizeWithVerticalRetrace = true
		};

		IsMouseVisible = true;
		Window.AllowUserResizing = true;
		Window.Title = "MeshPrimitives demo - drag: orbit, wheel: zoom, space: pause, Esc: exit";
	}

	protected override void LoadContent()
	{
		_model = PrimitiveScene.Build(GraphicsDevice);
		_instance = new DrModelInstance(_model);
		_renderer = new Renderer(GraphicsDevice, _instance);

		ResetCamera();
		PrintScene();
		_previousMouse = Mouse.GetState();
	}

	protected override void Update(GameTime gameTime)
	{
		KeyboardState keyboard = Keyboard.GetState();

		if (keyboard.IsKeyDown(Keys.Escape))
		{
			Exit();
		}

		if (keyboard.IsKeyDown(Keys.Space))
		{
			_paused = !_paused;
		}

		if (!_paused)
		{
			_spin += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.6f;
		}

		MouseState mouse = Mouse.GetState();

		if (mouse.LeftButton == ButtonState.Pressed)
		{
			_yaw -= (mouse.X - _previousMouse.X) * 0.01f;
			_pitch = Math.Clamp(_pitch + (mouse.Y - _previousMouse.Y) * 0.01f, -1.4f, 1.4f);
		}

		int scrollDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

		if (scrollDelta != 0)
		{
			_cameraDistance *= MathF.Pow(0.9f, scrollDelta / 120f);
			_cameraDistance = Math.Clamp(_cameraDistance, 2f, 60f);
		}

		_previousMouse = mouse;

		ApplySpin();

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(new Color(24, 28, 36));

		if (_renderer is not null)
		{
			Matrix view = Matrix.CreateLookAt(GetCameraPosition(), _target, Vector3.Up);
			Matrix projection = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.PiOver4,
				GraphicsDevice.Viewport.AspectRatio,
				0.1f,
				200f);

			_renderer.BeginFrame(view, projection);

			try
			{
				_renderer.DrawScene();
			}
			finally
			{
				_renderer.EndFrame();
			}
		}

		base.Draw(gameTime);
	}

	protected override void UnloadContent()
	{
		_renderer?.Dispose();
		_renderer = null;

		if (_model is not null)
		{
			foreach (DrModelMesh mesh in _model.Meshes)
			{
				mesh.Dispose();
			}
		}

		_model = null;
		_instance = null;

		base.UnloadContent();
	}

	private void ApplySpin()
	{
		if (_instance is null)
		{
			return;
		}

		// Rotate every mesh bone around its own origin. The pose lives on the instance, so the
		// shared model data is untouched.
		for (int i = 0; i < _instance.Model.Bones.Length; i++)
		{
			DrModelBone bone = _instance.Model.Bones[i];

			if (bone.Mesh is null)
			{
				continue;
			}

			Vector3 pivot = bone.Transform.Translation;
			float direction = i % 2 == 0 ? 1f : -0.65f;

			_instance.SetLocalTransform(
				i,
				Matrix.CreateTranslation(-pivot)
				* Matrix.CreateRotationY(_spin * direction)
				* Matrix.CreateTranslation(pivot));
		}
	}

	private void ResetCamera()
	{
		_bounds = _renderer?.ComputeBounds() ?? default;

		if (_bounds.Radius <= 0f)
		{
			_bounds = new BoundingSphere(Vector3.Zero, 6f);
		}

		_target = _bounds.Center;
		_cameraDistance = _bounds.Radius * 1.8f;
	}

	private Vector3 GetCameraPosition()
	{
		float cosPitch = MathF.Cos(_pitch);

		var offset = new Vector3(
			_cameraDistance * cosPitch * MathF.Sin(_yaw),
			_cameraDistance * MathF.Sin(_pitch),
			_cameraDistance * cosPitch * MathF.Cos(_yaw));

		return _target + offset;
	}

	private void PrintScene()
	{
		Console.WriteLine("MeshPrimitives demo");
		Console.WriteLine("Drag: orbit   Wheel: zoom   Space: pause   Esc: exit");
		Console.WriteLine();

		foreach (DrModelMesh mesh in _model!.Meshes)
		{
			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				Console.WriteLine(
					$"{mesh.Name,-16} {part.PrimitiveType,-12} " +
					$"vertices={part.NumVertices,5} primitives={part.PrimitiveCount,5} " +
					$"radius={part.BoundingSphere.Radius:0.###}");
			}
		}
	}
}
