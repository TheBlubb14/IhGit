using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IhGitWpf.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace IhGitWpf.ViewModel;

public partial class MergeConflict : ObservableObject
{
    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _path;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OpenWithDefaultProgramCommand), nameof(ShowInExplorerCommand), nameof(OpenCommand))]
    private string? _fullPath;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Description), nameof(IsResolved), nameof(DescriptionColor), nameof(OpenButtonVisible), nameof(ResolveButtonVisible))]
    private int _numberOfConflicts;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Description), nameof(OpenButtonVisible), nameof(ResolveButtonVisible))]
    private bool _deletedOnRemote;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Description))]
    private string? _remoteName;

    public bool IsResolved => NumberOfConflicts == 0;

    public bool OpenButtonVisible => !IsResolved && !DeletedOnRemote;

    public bool ResolveButtonVisible => !IsResolved && DeletedOnRemote;

    public MergeConflictAction DeletedOnRemoteAction { get; private set; }

    public SolidColorBrush DescriptionColor => NumberOfConflicts > 0 ? _warningBrush : _okBrush;

    public SolidColorBrush WarningBrush { get; } = _warningBrush;
    public SolidColorBrush OkBrush { get; } = _okBrush;
    private static readonly SolidColorBrush _warningBrush = new((Color)ColorConverter.ConvertFromString("#ff6a34"));
    private static readonly SolidColorBrush _okBrush = new((Color)ColorConverter.ConvertFromString("#28a745"));

    public string Description
    {
        get
        {
            if (IsResolved && DeletedOnRemote)
            {
                return DeletedOnRemoteAction switch
                {
                    MergeConflictAction.DoNotIncludeFile => "File will not be included in the merge",
                    MergeConflictAction.UseModifiedFile => "Modified file will be used",
                    _ => "Unknown"
                };
            }

            if (DeletedOnRemote)
            {
                return $"File does not exist on {RemoteName}";
            }

            return NumberOfConflicts switch
            {
                0 => "No conflicts remaining",
                1 => "1 conflict",
                _ => $"{NumberOfConflicts} conflicts"
            };
        }
    }

    public override string ToString()
        => Name ?? "";

    [RelayCommand(CanExecute = nameof(CanOpenWithDefaultProgram))]
    private void Open()
    {
        NumberOfConflicts = 0; // Simulate resolving the conflict

        Process.Start(new ProcessStartInfo(Environment.ExpandEnvironmentVariables(Settings.Default.ExternalEditorPath))
        {
            Arguments = $"\"{FullPath}\"",
            WorkingDirectory = System.IO.Path.GetDirectoryName(FullPath) ?? "",
            UseShellExecute = false
        });
    }

    [RelayCommand(CanExecute = nameof(CanOpenWithDefaultProgram))]
    private void OpenWithDefaultProgram()
    {
        Process.Start(new ProcessStartInfo(FullPath!)
        {
            UseShellExecute = true
        });
    }

    private bool CanOpenWithDefaultProgram()
        => !string.IsNullOrEmpty(FullPath) && File.Exists(FullPath);

    [RelayCommand(CanExecute = nameof(CanShowInExplorer))]
    private void ShowInExplorer()
    {
        var directory = System.IO.Path.GetDirectoryName(FullPath);
        if (directory is null)
            return;

        Process.Start(new ProcessStartInfo(directory)
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{FullPath}\"",
            UseShellExecute = true
        });
    }

    private bool CanShowInExplorer()
        => !string.IsNullOrEmpty(FullPath) && Directory.Exists(System.IO.Path.GetDirectoryName(FullPath));

    [RelayCommand]
    private void Redo()
    {
        NumberOfConflicts++; // Simulate reintroducing the conflict
    }

    [RelayCommand]
    private void DoNotIncludeFile()
    {
        DeletedOnRemoteAction = MergeConflictAction.DoNotIncludeFile;
        NumberOfConflicts = 0;
    }

    [RelayCommand]
    private void UseModifiedFile()
    {
        DeletedOnRemoteAction = MergeConflictAction.UseModifiedFile;
        NumberOfConflicts = 0;
    }
}

public enum MergeConflictAction
{
    None,
    DoNotIncludeFile,
    UseModifiedFile,
}