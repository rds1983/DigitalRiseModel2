using System.Globalization;
using AssetManagementBase;
using DigitalRiseModel.Gltf;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

/// <summary>
/// Provides <see cref="AssetManager"/> extensions for loading DigitalRiseModel assets.
/// </summary>
public static class DrModelAssetManagerExtensions
{
	private static readonly AssetLoader<DrModel> Loader = LoadModel;

	/// <summary>
	/// Loads a glTF or GLB model through an <see cref="AssetManager"/>.
	/// Textures referenced by the model are loaded relative to the model asset and stored in
	/// <see cref="DrTextureInfo.Texture"/>.
	/// </summary>
	/// <param name="assetManager">The asset manager.</param>
	/// <param name="graphicsDevice">The graphics device used to create buffers and textures.</param>
	/// <param name="path">The model path, resolved by the asset manager.</param>
	/// <param name="sceneIndex">The scene to load, or <see langword="null"/> to use the default scene.</param>
	/// <returns>The loaded model.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="assetManager"/>, <paramref name="graphicsDevice"/> or <paramref name="path"/>
	/// is <see langword="null"/>.
	/// </exception>
	public static DrModel LoadDrModel(
		this AssetManager assetManager,
		GraphicsDevice graphicsDevice,
		string path,
		int? sceneIndex = null)
	{
		ArgumentNullException.ThrowIfNull(assetManager);
		ArgumentNullException.ThrowIfNull(graphicsDevice);
		ArgumentNullException.ThrowIfNull(path);

		var settings = new DrModelLoadingSettings(sceneIndex);

		return assetManager.UseLoader(
			Loader,
			path,
			settings,
			(graphicsDevice, sceneIndex));
	}

	private static DrModel LoadModel(
		AssetManager manager,
		string assetName,
		IAssetSettings? settings,
		object? tag)
	{
		(GraphicsDevice graphicsDevice, int? sceneIndex) = ((GraphicsDevice, int?))tag!;

		return GltfLoader.Load(manager, graphicsDevice, assetName, sceneIndex);
	}

	private sealed class DrModelLoadingSettings : IAssetSettings
	{
		private readonly int? _sceneIndex;

		public DrModelLoadingSettings(int? sceneIndex)
		{
			_sceneIndex = sceneIndex;
		}

		public string BuildKey()
		{
			return _sceneIndex?.ToString(CultureInfo.InvariantCulture) ?? "default";
		}
	}
}
