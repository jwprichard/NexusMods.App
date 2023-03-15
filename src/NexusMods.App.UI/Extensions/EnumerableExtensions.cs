﻿using System.Collections.ObjectModel;

namespace NexusMods.App.UI.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Creates a new <see cref="ObservableCollection{T}"/> from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        return new ObservableCollection<T>(source);
    }

    /// <summary>
    /// Creates a new <see cref="ReadOnlyObservableCollection{T}"/> from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ReadOnlyObservableCollection<T> ToReadOnlyObservableCollection<T>(this IEnumerable<T> source)
    {
        return new ReadOnlyObservableCollection<T>(source.ToObservableCollection());
    }

}
