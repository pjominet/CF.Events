using NUglify;

namespace CF.Events.AssetCompiler;

public static class Program
{
    public static int Main(string[] args)
    {
        var (targetDir, folders) = AssetProcessor.ParseArguments(args);
        return AssetProcessor.ProcessAssets(targetDir, folders, Console.Out, Console.Error);
    }
}

public static class AssetProcessor
{
    private static readonly string[] DefaultFolders = ["css", "js"];

    public static (string TargetDir, List<string> Folders) ParseArguments(string[] args)
    {
        string? targetDir = null;
        var folders = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.Equals("--target", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                    targetDir = args[++i];
            }
            else if (arg.StartsWith("--target=", StringComparison.OrdinalIgnoreCase))
            {
                var equalsIndex = arg.IndexOf('=');
                targetDir = arg[(equalsIndex + 1)..];
            }
            else if (arg.Equals("--folders", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("-f", StringComparison.OrdinalIgnoreCase))
            {
                while (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    i++;
                    var split = args[i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    folders.AddRange(split);
                }
            }
            else if (arg.StartsWith("--folders=", StringComparison.OrdinalIgnoreCase) ||
                     arg.StartsWith("-f=", StringComparison.OrdinalIgnoreCase))
            {
                var equalsIndex = arg.IndexOf('=');
                var val = arg[(equalsIndex + 1)..];
                var split = val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                folders.AddRange(split);
            }
            else if (!arg.StartsWith('-'))
            {
                if (targetDir is null)
                    targetDir = arg;
                else
                {
                    var split = arg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    folders.AddRange(split);
                }
            }
        }

        var resolvedTargetDir = !string.IsNullOrWhiteSpace(targetDir)
            ? Path.GetFullPath(targetDir)
            : Directory.GetCurrentDirectory();

        var resolvedFolders = folders.Count > 0
            ? folders.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : DefaultFolders.ToList();

        return (resolvedTargetDir, resolvedFolders);
    }

    public static int ProcessAssets(string targetDir, IEnumerable<string> folders, TextWriter? stdout = null, TextWriter? stderr = null)
    {
        stdout ??= TextWriter.Null;
        stderr ??= TextWriter.Null;

        var webRootDir = Path.Combine(targetDir, "wwwroot");

        if (!Directory.Exists(webRootDir))
        {
            stderr.WriteLine($"Error: wwwroot directory not found in '{targetDir}'.");
            return 1;
        }

        stdout.WriteLine($"Processing static assets in {webRootDir}...");

        foreach (var folder in folders)
        {
            if (string.IsNullOrWhiteSpace(folder)) continue;

            var cleanFolder = folder.Trim().TrimStart('/', '\\');
            var folderDir = Path.IsPathRooted(folder) ? folder : Path.Combine(webRootDir, cleanFolder);

            if (!Directory.Exists(folderDir))
            {
                stdout.WriteLine($"Directory '{folderDir}' not found, skipping.");
                continue;
            }

            // 1. Minify CSS files
            foreach (var file in Directory.GetFiles(folderDir, "*.css", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase)) continue;

                var content = File.ReadAllText(file);
                var result = Uglify.Css(content);
                if (result.HasErrors)
                {
                    stderr.WriteLine($"Warning: Error minifying CSS file '{file}':");
                    foreach (var err in result.Errors)
                    {
                        stderr.WriteLine($"  - {err}");
                    }
                }

                var minFile = Path.ChangeExtension(file, ".min.css");
                var minCode = string.IsNullOrWhiteSpace(result.Code) ? content : result.Code;
                File.WriteAllText(minFile, minCode);
                stdout.WriteLine($"Minified CSS: {Path.GetFileName(file)} -> {Path.GetFileName(minFile)}");
            }

            // 2. Minify JS files
            foreach (var file in Directory.GetFiles(folderDir, "*.js", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase)) continue;

                var content = File.ReadAllText(file);
                var result = Uglify.Js(content);
                if (result.HasErrors)
                {
                    stderr.WriteLine($"Warning: Error minifying JS file '{file}':");
                    foreach (var err in result.Errors)
                    {
                        stderr.WriteLine($"  - {err}");
                    }
                }

                var minFile = Path.ChangeExtension(file, ".min.js");
                var minCode = string.IsNullOrWhiteSpace(result.Code) ? content : result.Code;
                File.WriteAllText(minFile, minCode);
                stdout.WriteLine($"Minified JS: {Path.GetFileName(file)} -> {Path.GetFileName(minFile)}");
            }
        }

        stdout.WriteLine("Asset compilation completed successfully.");
        return 0;
    }
}
