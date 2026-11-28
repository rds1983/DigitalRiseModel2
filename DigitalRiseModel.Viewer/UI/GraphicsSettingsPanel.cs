using DigitalRiseModel.Viewer.Graphics.Shaders;
using Myra.Graphics2D.UI;

namespace DigitalRiseModel.Viewer.UI
{
	internal partial class GraphicsSettingsPanel
	{
		private readonly ShaderSettings _settings;

		public GraphicsSettingsPanel(ShaderSettings settings)
		{
			ArgumentNullException.ThrowIfNull(settings);

			_settings = settings;
			BuildUI();

			_bloomSlider.Minimum = 0f;
			_bloomSlider.Maximum = MathF.Max(2f, settings.BloomIntensity);
			_exposureSlider.Minimum = 0.1f;
			_exposureSlider.Maximum = MathF.Max(4f, settings.Exposure);
			_fxaaCheck.IsChecked = settings.EnableFxaa;
			_bloomSlider.Value = settings.BloomIntensity;
			_exposureSlider.Value = settings.Exposure;
			_boundingSpheresCheck.IsChecked = settings.ShowBoundingSpheres;
			_skeletonCheck.IsChecked = settings.ShowSkeleton;
			UpdateLabels();

			_fxaaCheck.IsCheckedChanged += (_, _) =>
				_settings.EnableFxaa = _fxaaCheck.IsChecked;
			_bloomSlider.ValueChangedByUser += (_, args) =>
				SetBloom(args.NewValue);
			_exposureSlider.ValueChangedByUser += (_, args) =>
				SetExposure(args.NewValue);
			_boundingSpheresCheck.IsCheckedChanged += (_, _) =>
				_settings.ShowBoundingSpheres = _boundingSpheresCheck.IsChecked;
			_skeletonCheck.IsCheckedChanged += (_, _) =>
				_settings.ShowSkeleton = _skeletonCheck.IsChecked;
		}

		private void SetBloom(float value)
		{
			_settings.BloomIntensity = value;
			UpdateLabels();
		}

		private void SetExposure(float value)
		{
			_settings.Exposure = value;
			UpdateLabels();
		}

		private void UpdateLabels()
		{
			_bloomLabel.Text = FormattableString.Invariant(
				$"{_settings.BloomIntensity:F2}");
			_exposureLabel.Text = FormattableString.Invariant(
				$"{_settings.Exposure:F2}");
		}
	}
}
