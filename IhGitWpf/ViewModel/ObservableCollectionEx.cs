using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;

namespace IhGitWpf.ViewModel;

public class ObservableCollectionEx<T> : ObservableCollection<T>, INotifyPropertyChanged where T : INotifyPropertyChanged
{
    public ObservableCollectionEx() : base()
    {
        Init();
    }

    public ObservableCollectionEx(IEnumerable<T> collection) : base(collection)
    {
        Init();
    }

    private void Init()
    {
        CollectionChanged += OnCollectionChanged;
        foreach (var item in Items)
        {
            if (item is not null)
                item.PropertyChanged += ItemOnPropertyChanged;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (T item in e.NewItems)
            {
                if (item is not null)
                    item.PropertyChanged += ItemOnPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (T item in e.OldItems)
            {
                if (item is not null)
                    item.PropertyChanged -= ItemOnPropertyChanged;
            }
        }
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
