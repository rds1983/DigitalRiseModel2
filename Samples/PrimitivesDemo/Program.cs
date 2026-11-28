namespace PrimitivesDemo;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		using var game = new PrimitivesGame();
		game.Run();
	}
}
