using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Darwin.Mobile.Shared.Collections;

/// <summary>
/// Observable collection optimized for mobile list refreshes that replace or append multiple items at once.
/// </summary>
/// <typeparam name="T">Item type stored in the collection.</typeparam>
/// <remarks>
/// MAUI list controls are sensitive to repeated collection notifications because each notification can trigger
/// measuring, layout, and binding work. This collection keeps the standard <see cref="ObservableCollection{T}"/>
/// contract while allowing view models to batch server responses into a single UI notification.
/// </remarks>
public sealed class RangeObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Replaces the current collection contents with the provided items and emits one reset notification.
    /// </summary>
    /// <param name="items">New ordered collection contents.</param>
    public void ReplaceRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        RaiseResetNotifications();
    }

    /// <summary>
    /// Appends the provided items and emits one range add notification instead of one notification per item.
    /// </summary>
    /// <param name="items">Ordered items to append to the end of the collection.</param>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var newItems = items as IList<T> ?? items.ToList();
        if (newItems.Count == 0)
        {
            return;
        }

        var startIndex = Items.Count;
        foreach (var item in newItems)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)newItems, startIndex));
    }

    /// <summary>
    /// Clears the collection and emits one reset notification only when the collection was not already empty.
    /// </summary>
    public void ClearRange()
    {
        if (Items.Count == 0)
        {
            return;
        }

        Items.Clear();
        RaiseResetNotifications();
    }

    private void RaiseResetNotifications()
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
