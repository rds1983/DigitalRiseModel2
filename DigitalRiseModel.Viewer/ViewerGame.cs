using AssetManagementBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DigitalRiseModel.Viewer;

internal sealed class ViewerGame : Game
{
	private readonly string _modelPath;
	private readonly List<Drawable> _drawables = [];
	private Matrix[] _absoluteBoneTransforms = [];

	private AssetManager? _assetManager;
	private DrModel? _model;
	private Texture2D? _whiteTexture;
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

	public ViewerGame(string modelPath)
	{
		_modelPath = modelPath;

		_ = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1280,
			PreferredBackBufferHeight = 720,
			SynchronizeWithVerticalRetrace = true
		};

		IsMouseVisible = true;
		Window.AllowUserResizing = true;
		Window.Title = $"DigitalRiseModel.Viewer - {Path.GetFileName(modelPath)}";
	}

	protected override void LoadContent()
	{
		string fullPath = Path.GetFullPath(_modelPath);
		string directory = Path.GetDirectoryName(fullPath)
			?? throw new InvalidOperationException("The model path has no directory.");
		string fileName = Path.GetFileName(fullPath);

		_assetManager = AssetManager.CreateFileAssetManager(directory);
		_model = _assetManager.LoadDrModel(GraphicsDevice, fileName);
		_absoluteBoneTransforms = new Matrix[_model.Bones.Length];

		if (_model.Bones.Length > 0)
		{
			_model.CopyAbsoluteBoneTransformsTo(_absoluteBoneTransforms);
		}

		_whiteTexture = new Texture2D(GraphicsDevice, 1, 1);
		_whiteTexture.SetData([Color.White]);

		CreateDrawables();
		ResetCamera();
		_previousMouse = Mouse.GetState();
	}

	protected override void Update(GameTime gameTime)
	{
		KeyboardState keyboard = Keyboard.GetState();

		if (keyboard.IsKeyDown(Keys.Escape))
		{
			Exit();
		}

		MouseState mouse = Mouse.GetState();

		if (mouse.LeftButton == ButtonState.Pressed)
		{
			int dx = mouse.X - _previousMouse.X;
			int dy = mouse.Y - _previousMouse.Y;

			_yaw -= dx * 0.01f;
			_pitch = Math.Clamp(_pitch + dy * 0.01f, -1.4f, 1.4f);
		}

		int scrollDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

		if (scrollDelta != 0)
		{
			_cameraDistance *= MathF.Pow(0.9f, scrollDelta / 120f);
			_cameraDistance = Math.Clamp(_cameraDistance, _minCameraDistance, _maxCameraDistance);
		}

		_previousMouse = mouse;

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(new Color(28, 32, 40));

		if (_model is null || _model.Bones.Length == 0)
		{
			base.Draw(gameTime);
			return;
		}

		_model.CopyAbsoluteBoneTransformsTo(_absoluteBoneTransforms);

		Matrix view = Matrix.CreateLookAt(GetCameraPosition(), _target, Vector3.Up);
		Matrix projection = Matrix.CreatePerspectiveFieldOfView(
			MathHelper.PiOver4,
			GraphicsDevice.Viewport.AspectRatio,
			_nearPlane,
			_farPlane);

		foreach (Drawable drawable in _drawables)
		{
			DrModelMeshPart part = drawable.Part;

			if (part.VertexBuffer is null || part.IndexBuffer is null)
			{
				continue;
			}

			DrMaterial? material = part.Material;
			bool doubleSided = material?.DoubleSided == true;
			bool alphaBlend = material?.AlphaMode == DrAlphaMode.Blend;

			GraphicsDevice.RasterizerState = doubleSided
				? RasterizerState.CullNone
				: RasterizerState.CullCounterClockwise;
			GraphicsDevice.BlendState = alphaBlend
				? BlendState.AlphaBlend
				: BlendState.Opaque;
			GraphicsDevice.DepthStencilState = DepthStencilState.Default;
			GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

			if (drawable.IsSkinned)
			{
				var effect = (SkinnedEffect)drawable.Effect;
				effect.World = Matrix.Identity;
				effect.View = view;
				effect.Projection = projection;
			}
			else
			{
				var effect = (BasicEffect)drawable.Effect;
				effect.World = GetMeshWorld(part);
				effect.View = view;
				effect.Projection = projection;
			}

			GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
			GraphicsDevice.Indices = part.IndexBuffer;

			foreach (EffectPass pass in drawable.Effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				GraphicsDevice.DrawIndexedPrimitives(
					PrimitiveType.TriangleList,
					part.VertexOffset,
					part.StartIndex,
					part.PrimitiveCount);
			}
		}

		base.Draw(gameTime);
	}

	protected override void UnloadContent()
	{
		foreach (Drawable drawable in _drawables)
		{
			drawable.Effect.Dispose();
		}

		_drawables.Clear();

		if (_model is not null)
		{
			foreach (DrModelMesh mesh in _model.Meshes)
			{
				mesh.Dispose();
			}
		}

		_whiteTexture?.Dispose();
		_assetManager?.Unload();

		base.UnloadContent();
	}

	private void CreateDrawables()
	{
		if (_model is null)
		{
			return;
		}

		foreach (DrModelMesh mesh in _model.Meshes)
		{
			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				if (part.VertexBuffer is null || part.IndexBuffer is null)
				{
					continue;
				}

				_drawables.Add(CreateDrawable(part));
			}
		}
	}

	private Drawable CreateDrawable(DrModelMeshPart part)
	{
		Texture2D? texture = part.Material?.BaseColorTexture?.Texture;
		Vector4 color = part.Material?.BaseColorFactor ?? Vector4.One;
		VertexDeclaration declaration = part.VertexDeclaration
			?? part.VertexBuffer!.VertexDeclaration;

		bool hasNormals = HasElement(declaration, VertexElementUsage.Normal);
		Matrix[]? boneTransforms = CreateBoneTransforms(part);
		bool isSkinned = boneTransforms is not null
			&& HasElement(declaration, VertexElementUsage.BlendIndices)
			&& HasElement(declaration, VertexElementUsage.BlendWeight);

		if (isSkinned)
		{
			var effect = new SkinnedEffect(GraphicsDevice)
			{
				DiffuseColor = new Vector3(color.X, color.Y, color.Z),
				Alpha = color.W,
				Texture = texture ?? _whiteTexture!,
				PreferPerPixelLighting = true,
				World = Matrix.Identity
			};

			effect.SetBoneTransforms(boneTransforms!);
			return new Drawable(part, effect, true);
		}

		var basicEffect = new BasicEffect(GraphicsDevice)
		{
			DiffuseColor = new Vector3(color.X, color.Y, color.Z),
			Alpha = color.W,
			LightingEnabled = hasNormals,
			TextureEnabled = texture is not null,
			Texture = texture,
			PreferPerPixelLighting = true
		};

		return new Drawable(part, basicEffect, false);
	}

	private Matrix[]? CreateBoneTransforms(DrModelMeshPart part)
	{
		DrSkin? skin = part.Skin;

		if (skin is null
			|| skin.Joints.Length == 0
			|| skin.Joints.Length > SkinnedEffect.MaxBones
			|| skin.InverseBindMatrices.Length != skin.Joints.Length)
		{
			return null;
		}

		var transforms = new Matrix[skin.Joints.Length];

		for (int i = 0; i < transforms.Length; i++)
		{
			int boneIndex = skin.Joints[i].Index;

			if ((uint)boneIndex >= (uint)_absoluteBoneTransforms.Length)
			{
				return null;
			}

			transforms[i] = skin.InverseBindMatrices[i] * _absoluteBoneTransforms[boneIndex];
		}

		return transforms;
	}

	private void ResetCamera()
	{
		_bounds = ComputeBounds();

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

	private BoundingSphere ComputeBounds()
	{
		BoundingSphere result = default;
		bool hasBounds = false;

		if (_model is null)
		{
			return result;
		}

		foreach (DrModelMesh mesh in _model.Meshes)
		{
			Matrix meshTransform = mesh.ParentBone is { } parentBone
				&& (uint)parentBone.Index < (uint)_absoluteBoneTransforms.Length
				? _absoluteBoneTransforms[parentBone.Index]
				: Matrix.Identity;

			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				BoundingSphere sphere = part.BoundingSphere.Transform(meshTransform);
				result = hasBounds
					? BoundingSphere.CreateMerged(result, sphere)
					: sphere;
				hasBounds = true;
			}
		}

		return result;
	}

	private Matrix GetMeshWorld(DrModelMeshPart part)
	{
		DrModelBone? parentBone = part.Mesh?.ParentBone;

		return parentBone is not null
			&& (uint)parentBone.Index < (uint)_absoluteBoneTransforms.Length
			? _absoluteBoneTransforms[parentBone.Index]
			: Matrix.Identity;
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

	private static bool HasElement(VertexDeclaration declaration, VertexElementUsage usage)
	{
		foreach (VertexElement element in declaration.GetVertexElements())
		{
			if (element.VertexElementUsage == usage)
			{
				return true;
			}
		}

		return false;
	}

	private sealed class Drawable
	{
		public Drawable(DrModelMeshPart part, Effect effect, bool isSkinned)
		{
			Part = part;
			Effect = effect;
			IsSkinned = isSkinned;
		}

		public DrModelMeshPart Part { get; }

		public Effect Effect { get; }

		public bool IsSkinned { get; }
	}
}
