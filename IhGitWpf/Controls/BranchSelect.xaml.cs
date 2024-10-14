using IhGitWpf.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace IhGitWpf.Controls;

public partial class BranchSelect : UserControl
{
    public ObservableCollectionEx<BranchVersion> Versions
    {
        get => (ObservableCollectionEx<BranchVersion>)GetValue(VersionsProperty);
        set => SetValue(VersionsProperty, value);
    }

    public static readonly DependencyProperty VersionsProperty =
        DependencyProperty.Register(nameof(Versions), typeof(ObservableCollectionEx<BranchVersion>), typeof(BranchSelect), new PropertyMetadata(empty));

    private static readonly ObservableCollectionEx<BranchVersion> empty = [];

    public BranchSelect()
    {
        InitializeComponent();
    }
}
