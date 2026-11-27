using AssetManagementBase;

namespace DigitalRiseModel.Viewer;

internal static class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		string? modelPath = null;
		bool shaderTest = false;

		foreach (string arg in args)
		{
			switch (arg)
			{
				case "--shader-test":
					shaderTest = true;
					break;
				case "--help":
				case "-h":
					PrintUsage();
					return 0;
				default:
					if (arg.StartsWith("--", StringComparison.Ordinal))
					{
						Console.Error.WriteLine($"Unknown option: {arg}");
						PrintUsage();
						return 1;
					}

					if (modelPath is not null)
					{
						Console.Error.WriteLine("Only one model path may be specified.");
						PrintUsage();
						return 1;
					}

					modelPath = arg;
					break;
			}
		}

		if (modelPath is null)
		{
			PrintUsage();
			return 1;
		}

		string path = Path.GetFullPath(modelPath);

		if (!File.Exists(path))
		{
			Console.Error.WriteLine($"File not found: {path}");
			return 1;
		}

		AMBConfiguration.Logger = Console.WriteLine;

		using var game = new ViewerGame(path, shaderTest);
		game.Run();
		return 0;
	}

	private static void PrintUsage()
	{
		Console.Error.WriteLine("Usage: drm-viewer <path-to-gltf-or-glb> [--shader-test]");
	}
}
