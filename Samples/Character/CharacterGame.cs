using System.Diagnostics;
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
	private const float PitchPerPixel = 0.004f;
	private const float MinPitch = -0.2f;
	private const float MaxPitch = 1f;
	private const float CameraDistance = 4.5f;
	private const float CameraHeight = 1.2f;

	private readonly GraphicsDeviceManager _graphics;

	private AssetManager? _assetManager;
	private Scene? _scene;
	private AnimationController? _animation;
	private Renderer? _renderer;
	private Desktop? _desktop;
	private MouseState _previousMouse;
	private string _currentClip = string.Empty;
	private StatsPanel? _statsPanel;
	private ControlsPanel? _controlsPanel;
	private long _lastFrameTimestamp;
	private double _smoothedFrameTimeMs;
	private float _cameraPitch = 0.25f;
	private bool _mouseLocked;
	private bool _escapeWasDown;

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
			if (!_escapeWasDown)
			{
				ToggleMouseLock();
			}

			_escapeWasDown = true;
		}
		else
		{
			_escapeWasDown = false;
		}

		float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

		if (!IsMouseOverGui() && _mouseLocked)
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
			_renderer.BeginFrame(GetView(), GetProjection());
			_scene.Draw(_renderer);
			_renderer.EndFrame();
		}

		UpdateStats();
		_desktop?.Render();

		base.Draw(gameTime);
	}

	private void UpdateStats()
	{
		long now = Stopwatch.GetTimestamp();

		if (_lastFrameTimestamp != 0)
		{
			double frameTimeMs = Stopwatch.GetElapsedTime(_lastFrameTimestamp, now)
				.TotalMilliseconds;
			_smoothedFrameTimeMs = _smoothedFrameTimeMs <= 0
				? frameTimeMs
				: _smoothedFrameTimeMs * 0.9 + frameTimeMs * 0.1;
		}

		_lastFrameTimestamp = now;

		if (_statsPanel is null)
		{
			return;
		}

		int drawCalls = _renderer?.Statistics.DrawCalls ?? 0;
		int triangles = _renderer?.Statistics.Triangles ?? 0;
		double fps = _smoothedFrameTimeMs > 0 ? 1000.0 / _smoothedFrameTimeMs : 0.0;

		_statsPanel.SetValues(fps, _smoothedFrameTimeMs, drawCalls, triangles);
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
		_controlsPanel = new ControlsPanel
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(8)
		};

		root.Widgets.Add(_controlsPanel);

		_statsPanel = new StatsPanel
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(8)
		};
		root.Widgets.Add(_statsPanel);

		_desktop.Root = root;
	}

	private bool IsMouseOverGui()
	{
		return _desktop?.IsMouseOverGUI == true;
	}

	/// <summary>
	/// Toggles mouse lock. While locked, the cursor is hidden and re-centred each frame so
	/// horizontal motion turns the character and vertical motion pitches the camera.
	/// </summary>
	private void ToggleMouseLock()
	{
		_mouseLocked = !_mouseLocked;
		IsMouseVisible = !_mouseLocked;
		_controlsPanel?.SetMouseLocked(_mouseLocked);

		if (_mouseLocked)
		{
			Mouse.SetPosition(
				GraphicsDevice.Viewport.Width / 2,
				GraphicsDevice.Viewport.Height / 2);
			_previousMouse = Mouse.GetState();
		}
	}

	/// <summary>
	/// Mouse-look while locked. Horizontal motion turns the character, vertical motion pitches
	/// the camera around it like a third-person game. The cursor is re-centred every frame.
	/// </summary>
	private void TurnFromMouse()
	{
		MouseState mouse = Mouse.GetState();
		int centerX = GraphicsDevice.Viewport.Width / 2;
		int centerY = GraphicsDevice.Viewport.Height / 2;

		int deltaX = mouse.X - _previousMouse.X;
		int deltaY = mouse.Y - _previousMouse.Y;

		if (deltaX != 0)
		{
			_scene?.Turn(-deltaX * TurnPerPixel);
		}

		if (deltaY != 0)
		{
			_cameraPitch = Math.Clamp(
				_cameraPitch + deltaY * PitchPerPixel,
				MinPitch,
				MaxPitch);
		}

		Mouse.SetPosition(centerX, centerY);
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
	/// The camera orbits behind the character's back. Pitch lifts it above or below the shoulder
	/// line while the look direction stays aimed at the character.
	/// </summary>
	private Matrix GetView()
	{
		Vector3 position = _scene?.Position ?? Vector3.Zero;
		Vector3 forward = _scene?.Forward ?? Vector3.UnitZ;

		Vector3 target = position + Vector3.Up * CameraHeight;

		// Orbit behind the character, lifting the camera as pitch grows.
		Vector3 right = Vector3.Cross(forward, Vector3.Up);
		Vector3 offset = Vector3.Transform(
			-forward * CameraDistance,
			Matrix.CreateFromAxisAngle(right, -_cameraPitch));
		Vector3 cameraPosition = target + offset;

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
