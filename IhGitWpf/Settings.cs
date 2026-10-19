using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace IhGitWpf;

internal class Settings
{
    public string? GitHubToken { get; set; }
    public string? UserName { get; set; } = "TheBlubb14";
    public string? RepoPath { get; set; } = @"C:\Dev\Projects\GitHub\paxcontrol";
    public string? Password { get; set; }
    public int MinSupportMinorVersion { get; set; } = 16;
    public int MinSupportMajorVersion { get; set; } = 4;
    public bool AddToMergeQueue { get; set; } = true;
    public string? ExternalEditorPath { get; set; } = @"%localappdata%/Programs/Microsoft VS Code/Code.exe";
    public string? GitHubOAuth { get; set; }

    public static Settings Default { get; }

    static Settings()
    {
        var settingsFilePath = GetSettingsFilePath();
        if (File.Exists(settingsFilePath))
        {
            try
            {
                Default = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFilePath), jsonSerializerOptions) ?? new Settings();
                return;
            }
            catch { }
        }

        var files = Directory.GetFiles(
            Path.GetDirectoryName(settingsFilePath) ?? string.Empty,
            "user.config",
            new EnumerationOptions() { MaxRecursionDepth = 3, RecurseSubdirectories = true });

        Default = new Settings();
        var firstLoaded = false;
        foreach (var file in files.OrderByDescending(f => File.GetCreationTime(f)))
        {
            if (!firstLoaded)
            {
                var elements = XDocument.Load(file).Root?.Elements("userSettings").Elements("IhGitWpf.Properties.Settings").Elements("setting");
                if (elements != null)
                {
                    foreach (var setting in elements)
                    {
                        var name = setting.Attribute("name")?.Value;
                        var value = setting.Element("value")?.Value;
                        switch (name)
                        {
                            case nameof(GitHubToken):
                                Default.GitHubToken = value;
                                break;
                            case nameof(UserName):
                                Default.UserName = value;
                                break;
                            case nameof(RepoPath):
                                Default.RepoPath = value;
                                break;
                            case nameof(Password):
                                Default.Password = value;
                                break;
                            case nameof(MinSupportMinorVersion):
                                if (int.TryParse(value, out int minSupportMinorVersion))
                                    Default.MinSupportMinorVersion = minSupportMinorVersion;
                                break;
                            case nameof(MinSupportMajorVersion):
                                if (int.TryParse(value, out int minSupportMajorVersion))
                                    Default.MinSupportMajorVersion = minSupportMajorVersion;
                                break;
                            case nameof(AddToMergeQueue):
                                if (bool.TryParse(value, out bool addToMergeQueue))
                                    Default.AddToMergeQueue = addToMergeQueue;
                                break;
                            case nameof(ExternalEditorPath):
                                Default.ExternalEditorPath = value;
                                break;
                            case nameof(GitHubOAuth):
                                Default.GitHubOAuth = value;
                                break;
                        }
                    }
                }
            }

            firstLoaded = true;

            // TODO: Maybe in the future, delete the old files/folders
            //File.Delete(file);
        }
    }

    private static string GetSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsDirectory = Path.Combine(appDataPath, "IhGitWpf");
        Directory.CreateDirectory(settingsDirectory);
        return Path.Combine(settingsDirectory, "settings.json");
    }

    private static JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, jsonSerializerOptions);
        File.WriteAllText(GetSettingsFilePath(), json);
    }
}
