using System.Diagnostics;
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
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace DigitalRiseModel.Viewer;

internal sealed class ViewerGame : Game
{
	private readonly string _modelPath;
	private readonly string? _screenshotPath;
	private readonly ShaderDebugView _debugView;
	private readonly float _exposure;
	private readonly float _bloom;
	private readonly bool _fxaa;

	private AssetManager? _assetManager;
	private DrModel? _model;
	private DrModelInstance? _instance;
	private AnimationController? _animation;
	private Renderer? _renderer;
	private Desktop? _desktop;
	private AnimationPanel? _animationPanel;
	private GraphicsSettingsPanel? _graphicsSettingsPanel;
	private ContentManager? _content;
	private KeyboardState _previousKeyboard;
	private bool _screenshotCaptured;
	private int _drawFrame;
	private Label? _statsLabel;
	private long _lastFrameTimestamp;
	private double _smoothedFrameTimeMs;

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
		string? screenshotPath = null,
		ShaderDebugView debugView = ShaderDebugView.Shaded,
		float exposure = 1f,
		float bloom = 0f,
		bool fxaa = false)
	{
		_modelPath = modelPath;
		_screenshotPath = screenshotPath;
		_debugView = debugView;
		_exposure = exposure;
		_bloom = bloom;
		_fxaa = fxaa;
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
		_model = _assetManager.LoadDrModel(
			GraphicsDevice,
			fileName,
			ModelLoadFlags.EnsureUVs | ModelLoadFlags.EnsureUV1);
		_instance = new DrModelInstance(_model);
		_animation = new AnimationController(_instance);

		_content = new ContentManager(Services, "Content");
		_renderer = new Renderer(GraphicsDevice, _instance, _content);
		_renderer.Settings.DebugView = _debugView;
		_renderer.Settings.Exposure = _exposure;
		_renderer.Settings.BloomIntensity = _bloom;
		_renderer.Settings.EnableFxaa = _fxaa;

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
				// Linear background; the post-process pass applies ACES and gamma.
				_renderer.Clear(new Color(4, 5, 6));
				_renderer.DrawModel();
			}
			finally
			{
				_renderer.EndFrame();
			}
		}

		UpdateStats();
		_desktop?.Render();

		if (_screenshotPath is not null && !_screenshotCaptured && ++_drawFrame >= 3)
		{
			_screenshotCaptured = true;
			SaveScreenshot(_screenshotPath);
			Exit();
		}

		base.Draw(gameTime);
	}

	private void UpdateStats()
	{
		long now = Stopwatch.GetTimestamp();

		if (_lastFrameTimestamp != 0)
		{
			double frameTimeMs = Stopwatch
				.GetElapsedTime(_lastFrameTimestamp, now)
				.TotalMilliseconds;
			_smoothedFrameTimeMs = _smoothedFrameTimeMs <= 0
				? frameTimeMs
				: _smoothedFrameTimeMs * 0.9 + frameTimeMs * 0.1;
		}

		_lastFrameTimestamp = now;

		if (_statsLabel is null)
		{
			return;
		}

		int drawCalls = _renderer?.Statistics.DrawCalls ?? 0;
		int triangles = _renderer?.Statistics.Triangles ?? 0;
		double fps = _smoothedFrameTimeMs > 0
			? 1000.0 / _smoothedFrameTimeMs
			: 0.0;

		_statsLabel.Text = FormattableString.Invariant(
			$"{fps:0.0} FPS  {_smoothedFrameTimeMs:0.0} ms\nDraw calls: {drawCalls}\nTriangles: {triangles:N0}");
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

		_content?.Dispose();
		_content = null;

		_animationPanel = null;
		_graphicsSettingsPanel = null;
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

		_graphicsSettingsPanel = new GraphicsSettingsPanel(_renderer!.Settings)
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(8)
		};
		root.Widgets.Add(_graphicsSettingsPanel);

		_statsLabel = new Label
		{
			Text = "0.0 FPS  0.0 ms\nDraw calls: 0\nTriangles: 0"
		};

		var statsPanel = new VerticalStackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(8),
			Padding = new Thickness(8),
			Background = new SolidBrush("Black")
		};
		statsPanel.Widgets.Add(_statsLabel);
		root.Widgets.Add(statsPanel);

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
