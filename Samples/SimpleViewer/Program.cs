using AssetManagementBase;

namespace SimpleViewer;

internal static class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		if (args.Length != 1)
		{
			Console.Error.WriteLine("Usage: SimpleViewer <path-to-gltf-or-glb>");
			return 1;
		}

		string path = Path.GetFullPath(args[0]);

		if (!File.Exists(path))
		{
			Console.Error.WriteLine($"File not found: {path}");
			return 1;
		}

		AMBConfiguration.Logger = Console.WriteLine;

		using var game = new ViewerGame(path);
		game.Run();
		return 0;
	}
}
