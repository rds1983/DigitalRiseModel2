using System.Globalization;
using AssetManagementBase;
using DigitalRiseModel.Viewer.Graphics.Shaders;

namespace DigitalRiseModel.Viewer;

internal static class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		string? modelPath = null;
		string? screenshotPath = null;
		ShaderDebugView debugView = ShaderDebugView.Shaded;
		float exposure = 1f;
		float bloom = 0f;
		bool fxaa = false;

		for (int i = 0; i < args.Length; i++)
		{
			string arg = args[i];

			switch (arg)
			{
				case "--screenshot":
					if (++i >= args.Length)
					{
						Console.Error.WriteLine("--screenshot requires a file path.");
						PrintUsage();
						return 1;
					}

					screenshotPath = args[i];
					break;
				case "--debug-view":
					if (++i >= args.Length
						|| !Enum.TryParse(args[i], true, out debugView))
					{
						Console.Error.WriteLine(
							"--debug-view requires a view name: shaded, basecolor, metallic, roughness, normal, occlusion, emissive, diffuse, specular, ibldiffuse, iblspecular.");
						PrintUsage();
						return 1;
					}

					break;
				case "--exposure":
					if (++i >= args.Length
						|| !float.TryParse(
							args[i],
							NumberStyles.Float,
							CultureInfo.InvariantCulture,
							out exposure)
						|| exposure <= 0f)
					{
						Console.Error.WriteLine("--exposure requires a positive number.");
						PrintUsage();
						return 1;
					}

					break;
				case "--bloom":
					if (++i >= args.Length
						|| !float.TryParse(
							args[i],
							NumberStyles.Float,
							CultureInfo.InvariantCulture,
							out bloom)
						|| bloom < 0f)
					{
						Console.Error.WriteLine("--bloom requires a non-negative number.");
						PrintUsage();
						return 1;
					}

					break;
				case "--fxaa":
					fxaa = true;
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

		using var game = new ViewerGame(
			path,
			screenshotPath,
			debugView,
			exposure,
			bloom,
			fxaa);
		game.Run();
		return 0;
	}

	private static void PrintUsage()
	{
		Console.Error.WriteLine(
			"Usage: drm-viewer <path-to-gltf-or-glb> " +
			"[--debug-view <name>] [--exposure <value>] [--bloom <intensity>] " +
			"[--fxaa] [--screenshot <file.png>]");
	}
}
