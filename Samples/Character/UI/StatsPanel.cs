using Myra.Graphics2D.UI.Styles;

namespace Character.UI
{
	public partial class StatsPanel
	{
		public StatsPanel()
		{
			BuildUI();
		}

		/// <summary>
		/// Updates the counters shown by the panel.
		/// </summary>
		public void SetValues(double fps, double frameTimeMs, int drawCalls, int triangles)
		{
			_fpsLabel.Text = FormattableString.Invariant($"{fps:0.0} FPS  {frameTimeMs:0.0} ms");
			_drawCallsLabel.Text = FormattableString.Invariant($"Draw calls: {drawCalls}");
			_trianglesLabel.Text = FormattableString.Invariant($"Triangles: {triangles:N0}");
		}
	}
}
