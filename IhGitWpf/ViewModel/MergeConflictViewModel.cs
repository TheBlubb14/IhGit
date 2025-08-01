﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Linq;

namespace IhGitWpf.ViewModel;

public partial class MergeConflictViewModel : ObservableObject, IDisposable
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

    private static readonly string[] motd = ["I think you have conflicts", "We have some conflicts", "Resolve your conflicts"];
    public string MessageOfTheDay => motd[DateTime.Now.Day % motd.Length];


    public MergeConflictViewModel()
    {
        OnItemsChanging(null, _items);
    }

    private int notResolvedItemsCount => Items.Count(x => !x.IsResolved);
    public string ItemsDescription => $"{notResolvedItemsCount} conflicted file{(notResolvedItemsCount != 1 ? "s" : "")}";

    private bool CanContinue()
        => Items.All(i => i.IsResolved);

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        DialogHost.CloseDialogCommand.Execute(true, null);
    }

    [RelayCommand]
    private void Refresh()
    {
        DialogHost.CloseDialogCommand.Execute(null, null);
    }

    public void Dispose()
    {
        foreach (var item in Items)
        {
            item.PropertyChanged -= ItemsChanged;
            item.Dispose();
        }
    }
}

