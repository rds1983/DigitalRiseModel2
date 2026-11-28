using AssetManagementBase;
using Character.UI;
using DigitalRiseModel;
using DigitalRiseModel.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Character;

/// <summary>
/// Character movement sample. WASD drives the character across a flat field, the mouse turns
/// the character, and the camera follows behind its back. Movement switches between the
/// <c>Idle</c> and <c>Run</c> clips.
/// </summary>
internal sealed class CharacterGame : Game
{
	private const string IdleClip = "Idle";
	private const string RunClip = "Run";

	private const float TurnPerPixel = 0.005f;
	private const float CameraDistance = 4.5f;
	private const float CameraHeight = 2.2f;

	private readonly GraphicsDeviceManager _graphics;

	private AssetManager? _assetManager;
	private Scene? _scene;
	private AnimationController? _animation;
	private Renderer? _renderer;
	private Desktop? _desktop;
	private MouseState _previousMouse;
	private string _currentClip = string.Empty;

	public CharacterGame()
	{
		MyraEnvironment.Game = this;

		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1280,
			PreferredBackBufferHeight = 720,
			SynchronizeWithVerticalRetrace = true
		};

		IsMouseVisible = true;
		Window.AllowUserResizing = true;
		Window.Title = "Character - WASD: move, mouse: turn, Esc: exit";
	}

	protected override void LoadContent()
	{
		string modelPath = Path.Combine(AppContext.BaseDirectory, "Content", "mixamo.gltf");
		string directory = Path.GetDirectoryName(modelPath)
			?? throw new InvalidOperationException("The model path has no directory.");

		_assetManager = AssetManager.CreateFileAssetManager(directory);
		DrModel model = _assetManager.LoadDrModel(GraphicsDevice, Path.GetFileName(modelPath));

		_scene = new Scene(GraphicsDevice, model);
		_animation = new AnimationController(_scene.Instance);
		_renderer = new Renderer(GraphicsDevice);

		CreateUI();
		PlayClip(IdleClip);
		_previousMouse = Mouse.GetState();
	}

	protected override void Update(GameTime gameTime)
	{
		if (_scene is null)
		{
			return;
		}

		KeyboardState keyboard = Keyboard.GetState();

		if (keyboard.IsKeyDown(Keys.Escape))
		{
			Exit();
		}

		float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

		if (!IsMouseOverGui())
		{
			TurnFromMouse();
		}

		_scene.Move(GetMoveInput(keyboard), deltaSeconds);
		PlayClip(_scene.IsMoving ? RunClip : IdleClip);

		_animation?.Update(gameTime.ElapsedGameTime);

		// The clip drives local transforms, so movement is applied afterwards.
		_scene.ApplyTransform();

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(new Color(24, 28, 36));

		if (_scene is not null && _renderer is not null)
		{
			_renderer.SetCamera(GetView(), GetProjection());
			DrawScene(_scene, _renderer);
		}

		_desktop?.Render();

		base.Draw(gameTime);
	}

	private static void DrawScene(Scene scene, Renderer renderer)
	{
		renderer.DrawPart(scene.Field, Matrix.Identity, instance: null);

		Matrix[] absoluteBoneTransforms = scene.Instance.AbsoluteBoneTransforms;

		foreach (DrModelMesh mesh in scene.Model.Meshes)
		{
			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				Matrix world = GetMeshWorld(part, absoluteBoneTransforms);
				renderer.DrawPart(part, world, scene.Instance);
			}
		}
	}

	private static Matrix GetMeshWorld(
		DrModelMeshPart part,
		Matrix[] absoluteBoneTransforms)
	{
		DrModelBone? parentBone = part.Mesh?.ParentBone;

		return parentBone is not null
			&& (uint)parentBone.Index < (uint)absoluteBoneTransforms.Length
			? absoluteBoneTransforms[parentBone.Index]
			: Matrix.Identity;
	}

	protected override void UnloadContent()
	{
		_desktop?.Dispose();
		_desktop = null;

		_renderer?.Dispose();
		_renderer = null;

		_scene?.Dispose();
		_scene = null;

		_animation = null;

		_assetManager?.Unload();

		base.UnloadContent();
	}

	private void CreateUI()
	{
		_desktop = new Desktop();

		var root = new Panel();
		var controlsPanel = new ControlsPanel
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(8)
		};

		root.Widgets.Add(controlsPanel);
		_desktop.Root = root;
	}

	private bool IsMouseOverGui()
	{
		return _desktop?.IsMouseOverGUI == true;
	}

	/// <summary>
	/// Horizontal mouse motion turns the character. The cursor is re-centred each frame so it
	/// cannot leave the window.
	/// </summary>
	private void TurnFromMouse()
	{
		MouseState mouse = Mouse.GetState();
		int centerX = GraphicsDevice.Viewport.Width / 2;
		int centerY = GraphicsDevice.Viewport.Height / 2;

		if (mouse.X != centerX || mouse.Y != centerY)
		{
			_scene?.Turn(-(mouse.X - _previousMouse.X) * TurnPerPixel);
			Mouse.SetPosition(centerX, centerY);
		}

		_previousMouse = Mouse.GetState();
	}

	private static Vector2 GetMoveInput(KeyboardState keyboard)
	{
		var input = Vector2.Zero;

		if (keyboard.IsKeyDown(Keys.W))
		{
			input.Y += 1f;
		}

		if (keyboard.IsKeyDown(Keys.S))
		{
			input.Y -= 1f;
		}

		if (keyboard.IsKeyDown(Keys.A))
		{
			input.X -= 1f;
		}

		if (keyboard.IsKeyDown(Keys.D))
		{
			input.X += 1f;
		}

		return input;
	}

	private void PlayClip(string clipName)
	{
		if (_animation is null || _currentClip == clipName)
		{
			return;
		}

		if (_animation.Play(clipName))
		{
			_currentClip = clipName;
		}
	}

	/// <summary>
	/// The camera sits behind the character's back and looks slightly past it.
	/// </summary>
	private Matrix GetView()
	{
		Vector3 position = _scene?.Position ?? Vector3.Zero;
		Vector3 forward = _scene?.Forward ?? Vector3.UnitZ;

		Vector3 cameraPosition = position
			- forward * CameraDistance
			+ Vector3.Up * CameraHeight;
		Vector3 target = position + Vector3.Up * 1.1f + forward * 1.5f;

		return Matrix.CreateLookAt(cameraPosition, target, Vector3.Up);
	}

	private Matrix GetProjection()
	{
		return Matrix.CreatePerspectiveFieldOfView(
			MathHelper.PiOver4,
			GraphicsDevice.Viewport.AspectRatio,
			0.1f,
			200f);
	}
}
