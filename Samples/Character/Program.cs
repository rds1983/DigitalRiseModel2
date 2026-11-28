namespace Character;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		using var game = new CharacterGame();
		game.Run();
	}
}
