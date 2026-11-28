using System.Diagnostics;

namespace DigitalRiseModel.Viewer.Graphics;

/// <summary>
/// Watches shader sources, rebuilds the viewer content with MGCB and requests an
/// effect reload. Only active when the repository content sources are available.
/// </summary>
internal sealed class ShaderHotReload : IDisposable
{
	private const int DebounceMilliseconds = 400;

	private readonly FileSystemWatcher _watcher;
	private readonly Timer _timer;
	private readonly string _projectRoot;
	private readonly string _contentSourceDirectory;
	private readonly string _contentFile;
	private readonly string _contentBuildOutputDirectory;
	private readonly string _appContentDirectory;
	private readonly string _intermediateDirectory;

	private int _rebuildInProgress;
	private int _reloadRequested;
	private bool _disposed;

	public ShaderHotReload()
	{
		string? projectRoot = FindProjectRoot(AppContext.BaseDirectory);

		if (projectRoot is null)
		{
			throw new InvalidOperationException(
				"Could not locate DigitalRiseModel.Viewer/Content/Content.mgcb.");
		}

		_projectRoot = projectRoot;
		_contentSourceDirectory = Path.Combine(
			projectRoot,
			"DigitalRiseModel.Viewer",
			"Content");
		_contentFile = Path.Combine(_contentSourceDirectory, "Content.mgcb");
		_contentBuildOutputDirectory = Path.Combine(
			_contentSourceDirectory,
			"bin",
			"DesktopGL",
			"Content");
		_appContentDirectory = Path.Combine(AppContext.BaseDirectory, "Content");
		_intermediateDirectory = Path.Combine(
			_contentSourceDirectory,
			"obj",
			"DesktopGL",
			"net8.0",
			"Content");

		string shaderDirectory = Path.Combine(_contentSourceDirectory, "Shaders");

		_watcher = new FileSystemWatcher(shaderDirectory)
		{
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.LastWrite
				| NotifyFilters.FileName
				| NotifyFilters.Size
		};
		_watcher.Changed += OnShaderChanged;
		_watcher.Created += OnShaderChanged;
		_watcher.Deleted += OnShaderChanged;
		_watcher.Renamed += OnShaderRenamed;
		_watcher.EnableRaisingEvents = true;

		_timer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
	}

	public bool ConsumeReloadRequest()
	{
		return Interlocked.Exchange(ref _reloadRequested, 0) != 0;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_watcher.EnableRaisingEvents = false;
		_watcher.Dispose();
		_timer.Dispose();
	}

	private void OnShaderChanged(object sender, FileSystemEventArgs e)
	{
		if (IsShaderSource(e.FullPath))
		{
			ScheduleRebuild();
		}
	}

	private void OnShaderRenamed(object sender, RenamedEventArgs e)
	{
		if (IsShaderSource(e.FullPath) || IsShaderSource(e.OldFullPath))
		{
			ScheduleRebuild();
		}
	}

	private static bool IsShaderSource(string path)
	{
		string extension = Path.GetExtension(path);

		return extension.Equals(".fx", StringComparison.OrdinalIgnoreCase)
			|| extension.Equals(".fxh", StringComparison.OrdinalIgnoreCase);
	}

	private void ScheduleRebuild()
	{
		if (_disposed)
		{
			return;
		}

		_timer.Change(DebounceMilliseconds, Timeout.Infinite);
	}

	private void OnDebounceElapsed(object? state)
	{
		if (_disposed || Interlocked.Exchange(ref _rebuildInProgress, 1) != 0)
		{
			return;
		}

		try
		{
			if (RebuildContent())
			{
				Interlocked.Exchange(ref _reloadRequested, 1);
			}
		}
		finally
		{
			Interlocked.Exchange(ref _rebuildInProgress, 0);
		}
	}

	private bool RebuildContent()
	{
		Console.WriteLine("Shader change detected. Rebuilding content...");

		var startInfo = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = _projectRoot,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		startInfo.ArgumentList.Add("tool");
		startInfo.ArgumentList.Add("run");
		startInfo.ArgumentList.Add("mgcb");
		startInfo.ArgumentList.Add("/@:" + _contentFile);
		startInfo.ArgumentList.Add("/platform:DesktopGL");
		startInfo.ArgumentList.Add("/outputDir:" + _contentBuildOutputDirectory);
		startInfo.ArgumentList.Add("/intermediateDir:" + _intermediateDirectory);
		startInfo.ArgumentList.Add("/workingDir:" + _contentSourceDirectory);

		try
		{
			using Process process = Process.Start(startInfo)
				?? throw new InvalidOperationException(
					"Could not start the content builder.");

			Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
			Task<string> errorTask = process.StandardError.ReadToEndAsync();
			process.WaitForExit();

			string output = outputTask.GetAwaiter().GetResult();
			string error = errorTask.GetAwaiter().GetResult();

			if (process.ExitCode == 0)
			{
				if (!CopyContentToApp())
				{
					return false;
				}

				Console.WriteLine("Shader rebuild succeeded. Reloading effects.");
				return true;
			}

			Console.WriteLine(
				$"Shader rebuild failed with exit code {process.ExitCode}.");
			LogMessage(output);
			LogMessage(error);
			return false;
		}
		catch (Exception exception)
		{
			Console.WriteLine($"Shader rebuild failed: {exception.Message}");
			return false;
		}
	}

	private bool CopyContentToApp()
	{
		try
		{
			foreach (string source in Directory.GetFiles(
				_contentBuildOutputDirectory,
				"*",
				SearchOption.AllDirectories))
			{
				string relativePath = Path.GetRelativePath(
					_contentBuildOutputDirectory,
					source);
				string destination = Path.Combine(
					_appContentDirectory,
					relativePath);
				Directory.CreateDirectory(
					Path.GetDirectoryName(destination)!);
				File.Copy(source, destination, true);
			}

			return true;
		}
		catch (Exception exception)
		{
			Console.WriteLine(
				$"Could not copy rebuilt content: {exception.Message}");
			return false;
		}
	}

	private static void LogMessage(string message)
	{
		if (!string.IsNullOrWhiteSpace(message))
		{
			Console.WriteLine(message.Trim());
		}
	}

	private static string? FindProjectRoot(string startDirectory)
	{
		var directory = new DirectoryInfo(startDirectory);

		while (directory is not null)
		{
			string candidate = Path.Combine(
				directory.FullName,
				"DigitalRiseModel.Viewer",
				"Content",
				"Content.mgcb");

			if (File.Exists(candidate))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		return null;
	}
}
