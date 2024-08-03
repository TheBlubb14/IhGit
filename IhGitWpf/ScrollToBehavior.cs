using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IhGitWpf;

/// <summary>
/// A behavior for <see cref="ListView"/> which automatically scrolls to the given direction
/// </summary>
public sealed class ScrollToBehavior : Behavior<ListView>
{
    /// <summary>
    /// State of the automatic scrolling
    /// </summary>
    public bool Enabled
    {
        get => (bool)GetValue(EnabledProperty);
        set => SetValue(EnabledProperty, value);
    }

    /// <summary>
    /// Using a DependencyProperty as the backing store for <see cref="Enabled"/>
    /// </summary>
    public static readonly System.Windows.DependencyProperty EnabledProperty =
        System.Windows.DependencyProperty.Register("Enabled", typeof(bool), typeof(ScrollToBehavior), new PropertyMetadata(true));

    /// <summary>
    /// The direction the <see cref="ListView"/> should scroll to
    /// </summary>
    public ScrollTo ScrollMode
    {
        get => (ScrollTo)GetValue(ScrollProperty);
        set => SetValue(ScrollProperty, value);
    }

    /// <summary>
    /// Using a DependencyProperty as the backing store for <see cref="ScrollMode"/>
    /// </summary>
    public static readonly System.Windows.DependencyProperty ScrollProperty =
        System.Windows.DependencyProperty.Register("Scroll", typeof(ScrollTo), typeof(ScrollToBehavior), new PropertyMetadata(ScrollTo.Bottom));

    //protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    //{
    //    base.OnPropertyChanged(e);

    //    if (e.Property == ListView.ItemsSourceProperty)
    //    {
    //        if (e.OldValue is INotifyCollectionChanged oldCollection)
    //            oldCollection.CollectionChanged -= Collection_CollectionChanged;

    //        if (e.NewValue is INotifyCollectionChanged newCollection)
    //            newCollection.CollectionChanged += Collection_CollectionChanged;
    //    }
    //}

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        if (AssociatedObject.ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += Collection_CollectionChanged;
            Collection_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

        }

        base.OnDetaching();
    }

    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        if (AssociatedObject.ItemsSource is INotifyCollectionChanged collection)
            collection.CollectionChanged -= Collection_CollectionChanged;
    }

    private void Collection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!Enabled)
            return;

        if (VisualTreeHelper.GetChild(AssociatedObject, 0) is ScrollViewer scrollViewer)
        {
            switch (ScrollMode)
            {
                case ScrollTo.Top:
                    scrollViewer.ScrollToTop();
                    break;

                case ScrollTo.Bottom:
                    scrollViewer.ScrollToBottom();
                    break;

                case ScrollTo.End:
                    scrollViewer.ScrollToEnd();
                    break;

                case ScrollTo.Home:
                    scrollViewer.ScrollToHome();
                    break;

                case ScrollTo.LeftEnd:
                    scrollViewer.ScrollToLeftEnd();
                    break;

                case ScrollTo.RightEnd:
                    scrollViewer.ScrollToRightEnd();
                    break;
            }
        }
    }

    /// <summary>
    /// The directions for the <see cref="ScrollToBehavior"/>
    /// </summary>
    public enum ScrollTo
    {
        /// <summary>
        /// Scrolls vertically to the beginning of the <see cref="ScrollViewer"/> content
        /// </summary>
        Top,

        /// <summary>
        /// Scrolls vertically to the end of the <see cref="ScrollViewer"/> content
        /// </summary>
        Bottom,

        /// <summary>
        ///  Scrolls to both the vertical and horizontal end points of the <see cref="ScrollViewer"/> content
        /// </summary>
        End,

        /// <summary>
        /// Scrolls vertically to the beginning of the <see cref="ScrollViewer"/> content
        /// </summary>
        Home,

        /// <summary>
        /// Scrolls horizontally to the beginning of the <see cref="ScrollViewer"/> content
        /// </summary>
        LeftEnd,

        /// <summary>
        /// Scrolls horizontally to the end of the <see cref="ScrollViewer"/> content
        /// </summary>
        RightEnd,
    }
}
