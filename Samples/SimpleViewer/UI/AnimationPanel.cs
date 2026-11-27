using DigitalRiseModel;
using DigitalRiseModel.Animation;
using Myra.Graphics2D.UI;

namespace SimpleViewer.UI
{
	public partial class AnimationPanel
	{
		private readonly AnimationController _animation;
		private readonly string[] _animationNames;
		private readonly Label _playPauseLabel;

		public AnimationPanel(DrModel model, AnimationController animation)
		{
			ArgumentNullException.ThrowIfNull(model);
			ArgumentNullException.ThrowIfNull(animation);

			_animation = animation;
			_animationNames = model.Animations.Keys.ToArray();

			BuildUI();

			_playPauseLabel = (Label)_playPauseButton.Content;

			foreach (string name in _animationNames)
			{
				_animationCombo.Widgets.Add(new Label { Text = name });
			}

			_animationCombo.SelectedIndexChanged += (_, _) => PlaySelectedAnimation();
			_playPauseButton.Click += (_, _) => TogglePlayback();
			_stopButton.Click += (_, _) => StopPlayback();
			_loopButton.IsCheckedChanged += (_, _) => _animation.Loop = _loopButton.IsChecked;
			_playBackwardsButton.IsCheckedChanged += (_, _) =>
				SetPlayBackwards(_playBackwardsButton.IsChecked);
			_speedSlider.ValueChangedByUser += (_, args) => SetSpeed(args.NewValue);
			_timeSlider.ValueChangedByUser += (_, args) =>
				Seek(TimeSpan.FromSeconds(args.NewValue));

			_loopButton.IsChecked = _animation.Loop;
			_playBackwardsButton.IsChecked = _animation.PlayBackwards;
			_speedSlider.Value = _animation.Speed;

			if (_animationNames.Length > 0)
			{
				_animationCombo.SelectedIndex = 0;
			}
			else
			{
				Visible = false;
			}

			Update();
		}

		public void Update()
		{
			TimeSpan duration = _animation.Duration;
			float maximum = (float)duration.TotalSeconds;

			_timeSlider.Maximum = maximum > 0f ? maximum : 1f;
			_timeSlider.Value = (float)_animation.Time.TotalSeconds;
			_timeSlider.Enabled = _animation.CurrentClip is not null;
			_stopButton.Enabled = _animation.CurrentClip is not null;
			_timeLabel.Text = $"{_animation.Time.TotalSeconds:F2} / {duration.TotalSeconds:F2}";
			_playPauseLabel.Text = _animation.IsPlaying ? "Pause" : "Play";

			if (MathF.Abs(_speedSlider.Value - _animation.Speed) > 0.001f)
			{
				_speedSlider.Value = _animation.Speed;
			}

			_speedLabel.Text = $"{_animation.Speed:F2}x";

			if (_playBackwardsButton.IsChecked != _animation.PlayBackwards)
			{
				_playBackwardsButton.IsChecked = _animation.PlayBackwards;
			}
		}

		private void PlaySelectedAnimation()
		{
			int index = _animationCombo.SelectedIndex ?? -1;

			if (index < 0 || index >= _animationNames.Length)
			{
				return;
			}

			_animation.Loop = _loopButton.IsChecked;
			_animation.PlayBackwards = _playBackwardsButton.IsChecked;
			_animation.Speed = _speedSlider.Value;
			_animation.Play(_animationNames[index]);
			Update();
		}

		private void TogglePlayback()
		{
			if (_animation.CurrentClip is null)
			{
				PlaySelectedAnimation();
				return;
			}

			if (_animation.IsPlaying)
			{
				_animation.Pause();
			}
			else
			{
				_animation.Resume();
			}

			Update();
		}

		private void StopPlayback()
		{
			_animation.Stop();
			Update();
		}

		private void SetPlayBackwards(bool playBackwards)
		{
			_animation.PlayBackwards = playBackwards;

			if (playBackwards && _animation.Time <= TimeSpan.Zero)
			{
				_animation.Seek(_animation.Duration);
			}
			else if (!playBackwards && _animation.Time >= _animation.Duration)
			{
				_animation.Seek(TimeSpan.Zero);
			}

			Update();
		}

		private void SetSpeed(float speed)
		{
			_animation.Speed = speed;
			Update();
		}

		private void Seek(TimeSpan time)
		{
			_animation.Seek(time);
			Update();
		}
	}
}
