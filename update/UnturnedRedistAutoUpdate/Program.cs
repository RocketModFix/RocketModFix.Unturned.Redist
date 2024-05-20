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
    private static bool DedicatedServer { get; set; }
    private static string AppId { get; set; }
    private static bool Force { get; set; }

    public static async Task<int> Main(string[] args)
    {
        var linux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        //linux = true;
        AssertPlatformSupported();

        string path;
#if DEBUG
        path = @"C:\Me\Git Repos\RocketModFix.Unturned.Redist";
#else
if (args.Length == 0)
{
    Console.WriteLine("Wrong usage. Specify path to the redist.");
    return 1;
}
path = args[0];
#endif

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

        DedicatedServer = !args.Any(x => x.Equals("--client", StringComparison.OrdinalIgnoreCase));
        Force = !args.Any(x => x.Equals("--force", StringComparison.OrdinalIgnoreCase));
        AppId = GetAppId().ToString();

        var archiveName = GetArchiveName();
        var downloadUrl = GetDownloadUrl(archiveName);

        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsByteArrayAsync();
        if (data.Length == 0)
        {
            Console.WriteLine("Downloaded data was empty");
            return 1;
        }

        var executableDirectory = Path.Combine(path, "steamcmd");
        Directory.CreateDirectory(executableDirectory);
        ExtractExecutableData(data, executableDirectory);

        var executablePath = GetExecutablePath(executableDirectory);
        if (File.Exists(executablePath) == false)
        {
            Console.WriteLine($"Executable cannot be found: {executablePath}");
            return 1;
        }

        if (linux)
        {
            MakeFullPermissionsForLinuxFile(executablePath);
        }

        try
        {
            var startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.FileName = executablePath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = $"+login anonymous +app_update {AppId} validate +quit";
            using var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += OutputHandler;
            process.ErrorDataReceived += OutputHandler;

            process.Start();
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occured while running app: \n{ex}");
            return 1;
        }

        var unturnedDirectory = Path.Combine(executableDirectory, "steamapps", "common", "U3DS");
        if (Directory.Exists(unturnedDirectory) == false)
        {
            Console.WriteLine($"Unturned Directory not found: \"{unturnedDirectory}\"");
            return 1;
        }
        var unturnedDataDirectoryName = GetUnturnedDataDirectoryName();
        var managedDirectory = Path.Combine(unturnedDirectory, unturnedDataDirectoryName, "Managed");
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

        Console.WriteLine($"Installed Unturned v{version} ({buildId})");

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
        string GetArchiveName()
        {
            if (windows)
            {
                return "steamcmd.zip";
            }
            else if (linux)
            {
                return "steamcmd_linux.tar.gz";
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
        string GetDownloadUrl(string fileName)
        {
            return $"https://steamcdn-a.akamaihd.net/client/installer/{fileName}";
        }
        string GetExecutablePath(string directory)
        {
            var extension = windows ? "exe" : "sh";
            return Path.Combine(directory, "steamcmd." + extension);
        }
        void ExtractExecutableData(byte[] bytes, string destination)
        {
            if (windows)
            {
                using var stream = new MemoryStream(bytes);
                using var zipArchive = new ZipArchive(stream);
                zipArchive.ExtractToDirectory(destination, true);
            }
            else if (linux)
            {
                using (var memoryStream = new MemoryStream(bytes))
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                using (var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8))
                {
                    tarArchive.ExtractContents(destination);
                }
            }
            else
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

    private static void MakeFullPermissionsForLinuxFile(string file)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = "+x " + file, // +x to add execute permission
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.OutputDataReceived += OutputHandler;
            process.ErrorDataReceived += OutputHandler;

            process.Start();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"error occured while setting chmod for {file}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static int GetAppId()
    {
        return DedicatedServer ? 1110390 : 304930;
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
    static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
    {
        Console.WriteLine(outLine.Data);
    }

}