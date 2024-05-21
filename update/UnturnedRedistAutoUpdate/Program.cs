using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Tar;
using ValveKeyValue;

internal class Program
{
    private static string AppId { get; set; }
    private static bool Force { get; set; }

    public static async Task<int> Main(string[] args)
    {
        var linux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        AssertPlatformSupported();

        string path;
        if (args.Length < 3)
        {
            Console.WriteLine("Wrong usage. Correct usage: UnturnedRedistAutoUpdate.exe <path> <app_id> [args]");
            return 1;
        }
        path = args[0];
        AppId = args[1];
        Force = !args.Any(x => x.Equals("--force", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(AppId))
        {
            Console.WriteLine("AppId is not specified.");
            return 1;
        }
        if (Path.Exists(path) == false)
        {
            Console.WriteLine($"Path doesn't exists: \"{path}\".");
            return 1;
        }
        var redistPath = Path.Combine(path, "redist");
        if (Path.Exists(redistPath) == false)
        {
            Console.WriteLine($"Redist path doesn't exists: \"{redistPath}\".");
            return 1;
        }
        var nuspecFilePath = Directory.GetFiles(redistPath, "*.nuspec").FirstOrDefault();
        if (nuspecFilePath == null)
        {
            Console.WriteLine($".nuspec file cannot be found in redist folder: \"{redistPath}\".");
            return 1;
        }
        if (File.Exists(nuspecFilePath) == false)
        {
            Console.WriteLine($".nuspec file doesn't exists in redist folder: \"{redistPath}\".");
            return 1;
        }

        Console.WriteLine("Preparing to run tool...");

        var executableDirectory = Path.Combine(path, "steamcmd");
        if (Directory.Exists(executableDirectory) == false)
        {
            Console.WriteLine($"executable Directory not found: \"{executableDirectory}\"");
            return 1;
        }
        var steamappsDirectory = Path.Combine(executableDirectory, "steamapps");
        if (Directory.Exists(steamappsDirectory) == false)
        {
            Console.WriteLine($"steamapps Directory not found: \"{steamappsDirectory}\"");
            return 1;
        }
        var commonDirectory = Path.Combine(steamappsDirectory, "common");
        if (Directory.Exists(commonDirectory) == false)
        {
            Console.WriteLine($"common Directory not found: \"{commonDirectory}\"");
            return 1;
        }
        var unturnedDirectory = GetUnturnedDirectory(commonDirectory);
        if (unturnedDirectory == null || Directory.Exists(unturnedDirectory) == false)
        {
            Console.WriteLine($"Unturned Directory not found: \"{unturnedDirectory}\"");
            return 1;
        }
        Console.WriteLine("unturnedDirectory: " + string.Join(", ", Directory.GetDirectories(unturnedDirectory)));

        var unturnedDataDirectoryName = GetUnturnedDataDirectoryName();
        var unturnedDataPath = Path.Combine(unturnedDirectory, unturnedDataDirectoryName);
        if (Directory.Exists(unturnedDataPath) == false)
        {
            Console.WriteLine($"Unturned Data Directory not found: \"{unturnedDataPath}\"");
            return 1;
        }
        var managedDirectory = Path.Combine(unturnedDataPath, "Managed");
        if (Directory.Exists(managedDirectory) == false)
        {
            Console.WriteLine($"Unturned Managed Directory not found: \"{managedDirectory}\"");
            return 1;
        }
        const string statusFileName = "Status.json";
        var statusFilePath = Path.Combine(unturnedDirectory, statusFileName);
        if (File.Exists(statusFilePath) == false)
        {
            throw new FileNotFoundException("Required file is not found", statusFilePath);
        }
        var (version, buildId) = await GetInfo(unturnedDirectory, executableDirectory, AppId);

        Console.WriteLine($"Found Unturned v{version} ({buildId})");

        var doc = XDocument.Load(nuspecFilePath);
        XNamespace ns = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";
        var versionElement = doc.Root.Element(ns + "metadata").Element(ns + "version");
        if (versionElement != null)
        {
            if (version == versionElement.Value)
            {
                Console.WriteLine("Unturned Version is the same as in nuspec, it means new version is not detected, skipping...");
                return 1;
            }
            versionElement.Value = version;
        }
        else
        {
            Console.WriteLine("Version element not found in nuspec file!");
            return 1;
        }

        doc.Save(nuspecFilePath);

        UpdateRedist(managedDirectory);

        var forcedNote = Force ? " [Forced]" : "";

        await File.WriteAllTextAsync(Path.Combine(path, ".commit"),
            $"{DateTime.UtcNow:dd MMMM yyyy} - Version {version} ({buildId})" + forcedNote);

        return 0;

        void AssertPlatformSupported()
        {
            if (!(linux || windows))
            {
                throw new PlatformNotSupportedException();
            }
        }
        string GetUnturnedDataDirectoryName()
        {
            if (linux)
            {
                return "Unturned_Headless_Data";
            }
            else if (windows)
            {
                return "Unturned_Data";
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
        void UpdateRedist(string unturnedManagedDirectory)
        {
            var managedFiles = new DirectoryInfo(unturnedManagedDirectory).GetFiles();
            if (managedFiles.Length == 0)
            {
                throw new InvalidOperationException($"{nameof(managedFiles)} was empty");
            }

            foreach (var fileInfo in managedFiles)
            {
                var redistFilePath = Path.Combine(path, fileInfo.Name);
                if (File.Exists(redistFilePath))
                {
                    fileInfo.CopyTo(redistFilePath, true);
                }
            }
        }
    }

    private static string? GetUnturnedDirectory(string commonDirectory)
    {
        var unturnedDirectory = Path.Combine(commonDirectory, "Unturned");
        if (Directory.Exists(unturnedDirectory))
        {
            return unturnedDirectory;
        }
        unturnedDirectory = Path.Combine(commonDirectory, "U3DS");
        if (Directory.Exists(unturnedDirectory))
        {
            return unturnedDirectory;
        }
        return null;
    }

    private static async Task<(string version, string buildId)> GetInfo(string unturnedDirectory, string executablePath, string appId)
    {
        var node = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(unturnedDirectory, "Status.json")))!["Game"]!;
        var version = $"3.{node["Major_Version"]}.{node["Minor_Version"]}.{node["Patch_Version"]}";

        var appmanifestFileName = $"appmanifest_{appId}.acf";
        var appdataPath = Path.Combine(executablePath, "steamapps", appmanifestFileName);
        if (!File.Exists(appdataPath))
        {
            throw new FileNotFoundException("Required file is not found", appmanifestFileName);
        }

        await using var file = File.OpenRead(appdataPath);
        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        var obj = kv.Deserialize(file);

        var buildId1 = obj["buildid"].ToString();
        return (version, buildId1);
    }
}