using Myra.Graphics2D.UI.Styles;

namespace Character.UI
{
	public partial class ControlsPanel
	{
		public ControlsPanel()
		{
			BuildUI();
		}

		/// <summary>
		/// Shows whether the mouse is currently locked to the window.
		/// </summary>
		public void SetMouseLocked(bool locked)
		{
			_mouseLockLabel.Text = locked ? "Mouse: locked" : "Mouse: unlocked";
		}
	}
}
