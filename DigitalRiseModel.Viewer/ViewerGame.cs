using AssetManagementBase;
using DigitalRiseModel.Animation;
using DigitalRiseModel.Viewer.Graphics;
using DigitalRiseModel.Viewer.Graphics.Shaders;
using DigitalRiseModel.Viewer.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace DigitalRiseModel.Viewer;

internal sealed class ViewerGame : Game
{
	private readonly string _modelPath;
	private readonly bool _usePbr;
	private readonly string? _screenshotPath;
	private readonly ShaderDebugView _debugView;
	private readonly float _exposure;

	private AssetManager? _assetManager;
	private DrModel? _model;
	private DrModelInstance? _instance;
	private AnimationController? _animation;
	private Renderer? _renderer;
	private Desktop? _desktop;
	private AnimationPanel? _animationPanel;
	private ContentManager? _content;
	private FullscreenColorEffect? _shaderSmokeTest;
	private KeyboardState _previousKeyboard;
	private bool _showShaderSmokeTest;
	private bool _screenshotCaptured;
	private int _drawFrame;

	private BoundingSphere _bounds;
	private Vector3 _target;
	private float _cameraDistance = 1f;
	private float _minCameraDistance = 0.0001f;
	private float _maxCameraDistance = 100000f;
	private float _nearPlane = 0.01f;
	private float _farPlane = 10000f;
	private float _yaw = MathHelper.PiOver4;
	private float _pitch = -0.25f;
	private MouseState _previousMouse;

	public ViewerGame(
		string modelPath,
		bool shaderTest = false,
		bool usePbr = false,
		string? screenshotPath = null,
		ShaderDebugView debugView = ShaderDebugView.Shaded,
		float exposure = 1f)
	{
		_modelPath = modelPath;
		_showShaderSmokeTest = shaderTest;
		_usePbr = usePbr;
		_screenshotPath = screenshotPath;
		_debugView = debugView;
		_exposure = exposure;
		MyraEnvironment.Game = this;

		_ = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1280,
			PreferredBackBufferHeight = 720,
			SynchronizeWithVerticalRetrace = true
		};

		IsMouseVisible = true;
		Window.AllowUserResizing = true;
		Window.Title = $"drm-viewer - {Path.GetFileName(modelPath)}";
	}

	protected override void LoadContent()
	{
		string fullPath = Path.GetFullPath(_modelPath);
		string directory = Path.GetDirectoryName(fullPath)
			?? throw new InvalidOperationException("The model path has no directory.");
		string fileName = Path.GetFileName(fullPath);

		_assetManager = AssetManager.CreateFileAssetManager(directory);
		_model = _assetManager.LoadDrModel(GraphicsDevice, fileName);
		_instance = new DrModelInstance(_model);
		_animation = new AnimationController(_instance);

		_content = new ContentManager(Services, "Content");
		_renderer = new Renderer(GraphicsDevice, _instance, _content, _usePbr);
		_renderer.Settings.DebugView = _debugView;
		_renderer.Settings.Exposure = _exposure;
		_shaderSmokeTest = new FullscreenColorEffect(GraphicsDevice, _content);

		CreateUI();
		ResetCamera();
		UpdateWindowTitle();
		_previousMouse = Mouse.GetState();
	}

	protected override void Update(GameTime gameTime)
	{
		_animation?.Update(gameTime.ElapsedGameTime);
		_animationPanel?.Update();

		KeyboardState keyboard = Keyboard.GetState();

		if (keyboard.IsKeyDown(Keys.Escape))
		{
			Exit();
		}

		if (keyboard.IsKeyDown(Keys.F2) && _previousKeyboard.IsKeyUp(Keys.F2))
		{
			_showShaderSmokeTest = !_showShaderSmokeTest;
		}

		if (_renderer is not null
			&& keyboard.IsKeyDown(Keys.F3)
			&& _previousKeyboard.IsKeyUp(Keys.F3))
		{
			_renderer.Settings.DebugView = NextDebugView(_renderer.Settings.DebugView);
			UpdateWindowTitle();
		}

		MouseState mouse = Mouse.GetState();

		if (mouse.LeftButton == ButtonState.Pressed && !IsMouseOverGui())
		{
			int dx = mouse.X - _previousMouse.X;
			int dy = mouse.Y - _previousMouse.Y;

			_yaw -= dx * 0.01f;
			_pitch = Math.Clamp(_pitch + dy * 0.01f, -1.4f, 1.4f);
		}

		int scrollDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

		if (scrollDelta != 0 && !IsMouseOverGui())
		{
			_cameraDistance *= MathF.Pow(0.9f, scrollDelta / 120f);
			_cameraDistance = Math.Clamp(_cameraDistance, _minCameraDistance, _maxCameraDistance);
		}

		_previousMouse = mouse;
		_previousKeyboard = keyboard;

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		if (_renderer is not null)
		{
			Matrix view = Matrix.CreateLookAt(GetCameraPosition(), _target, Vector3.Up);
			Matrix projection = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.PiOver4,
				GraphicsDevice.Viewport.AspectRatio,
				_nearPlane,
				_farPlane);

			_renderer.BeginFrame(view, projection);
			try
			{
				_renderer.Clear(new Color(28, 32, 40));
				_renderer.DrawModel();
			}
			finally
			{
				_renderer.EndFrame();
			}
		}

		if (_showShaderSmokeTest)
		{
			_shaderSmokeTest?.Draw();
		}

		_desktop?.Render();

		if (_screenshotPath is not null && !_screenshotCaptured && ++_drawFrame >= 3)
		{
			_screenshotCaptured = true;
			SaveScreenshot(_screenshotPath);
			Exit();
		}

		base.Draw(gameTime);
	}

	private void SaveScreenshot(string path)
	{
		int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
		int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
		var pixels = new Color[width * height];
		GraphicsDevice.GetBackBufferData(pixels);

		using var texture = new Texture2D(GraphicsDevice, width, height);
		texture.SetData(pixels);

		string fullPath = Path.GetFullPath(path);
		string? directory = Path.GetDirectoryName(fullPath);

		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		using var stream = File.Create(fullPath);
		texture.SaveAsPng(stream, width, height);
	}

	protected override void UnloadContent()
	{
		_desktop?.Dispose();
		_desktop = null;

		_renderer?.Dispose();
		_renderer = null;

		_shaderSmokeTest?.Dispose();
		_shaderSmokeTest = null;

		_content?.Dispose();
		_content = null;

		_animationPanel = null;
		_animation = null;
		_instance = null;

		if (_model is not null)
		{
			foreach (DrModelMesh mesh in _model.Meshes)
			{
				mesh.Dispose();
			}
		}

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

		if (_model!.Animations.Count > 0)
		{
			_animationPanel = new AnimationPanel(_model, _animation!)
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(8)
			};
			root.Widgets.Add(_animationPanel);
		}

		_desktop.Root = root;
	}

	private bool IsMouseOverGui()
	{
		return _desktop?.IsMouseOverGUI == true;
	}

	private void UpdateWindowTitle()
	{
		ShaderDebugView debugView = _renderer?.Settings.DebugView ?? ShaderDebugView.Shaded;
		string suffix = debugView == ShaderDebugView.Shaded
			? string.Empty
			: $" [{debugView}]";

		Window.Title = $"drm-viewer - {Path.GetFileName(_modelPath)}{suffix}";
	}

	private static ShaderDebugView NextDebugView(ShaderDebugView view)
	{
		ShaderDebugView[] values = Enum.GetValues<ShaderDebugView>();
		int index = Array.IndexOf(values, view);
		return values[(index + 1) % values.Length];
	}

	private void ResetCamera()
	{
		_bounds = _renderer?.ComputeBounds() ?? default;

		if (_bounds.Radius <= 0f)
		{
			_bounds = new BoundingSphere(Vector3.Zero, 1f);
		}

		float radius = MathF.Max(_bounds.Radius, 0.0001f);
		float verticalFov = MathHelper.PiOver4;
		float aspect = GraphicsDevice.Viewport.AspectRatio;
		float horizontalFov = aspect > 0f
			? 2f * MathF.Atan(MathF.Tan(verticalFov * 0.5f) * aspect)
			: verticalFov;
		float limitingFov = MathF.Min(verticalFov, horizontalFov);

		_target = _bounds.Center;
		_cameraDistance = radius / MathF.Sin(limitingFov * 0.5f) * 1.2f;
		_minCameraDistance = MathF.Max(radius * 0.01f, 0.0001f);
		_maxCameraDistance = MathF.Max(radius * 1000f, 1f);
		_nearPlane = MathF.Max(_cameraDistance * 0.0005f, 0.00001f);
		_farPlane = MathF.Max(_cameraDistance * 100f, radius * 100f);
		_yaw = MathHelper.PiOver4;
		_pitch = -0.25f;
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
}
