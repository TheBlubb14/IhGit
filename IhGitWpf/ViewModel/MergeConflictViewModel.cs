using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Linq;

namespace IhGitWpf.ViewModel;

public partial class MergeConflictViewModel : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ItemsDescription)), NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private ObservableCollectionEx<MergeConflict> _items = [];

    partial void OnItemsChanging(ObservableCollectionEx<MergeConflict>? oldValue, ObservableCollectionEx<MergeConflict> newValue)
    {
        if (oldValue is not null)
        {
            ((INotifyPropertyChanged)oldValue).PropertyChanged -= ItemsChanged;
        }

        if (newValue is not null)
        {
            ((INotifyPropertyChanged)newValue).PropertyChanged += ItemsChanged;
        }
    }

    private void ItemsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MergeConflict.IsResolved))
        {
            ContinueCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(ItemsDescription));
        }
    }

    public MergeConflictViewModel()
    {
        OnItemsChanging(null, _items);
    }

    private int notResolvedItemsCount => Items.Count(x => !x.IsResolved);
    public string ItemsDescription => $"{notResolvedItemsCount} conflicted file{(notResolvedItemsCount != 1 ? "s" : "")}";

    [RelayCommand]
    private void OpenItem(MergeConflict item)
    {

    }

    private bool CanContinue()
        => Items.All(i => i.IsResolved);

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        DialogHost.CloseDialogCommand.Execute(this, null);
    }
}

