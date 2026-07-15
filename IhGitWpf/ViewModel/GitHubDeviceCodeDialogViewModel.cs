using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace IhGitWpf.ViewModel;

public partial class GitHubDeviceCodeDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string deviceCode = null!;

    [ObservableProperty]
    private string url = null!;

    [RelayCommand]
    private void OpenUrl() => MainViewModel.OpenUrl(Url);

    [RelayCommand]
    private void CopyCode(string input)
    {
        try
        {
            Clipboard.SetText(input);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy code to clipboard: {Environment.NewLine}{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
