using System;
using System.Collections.Generic;
using System.Linq;

namespace Models;

public interface IHeirarchicalItem<T>
{
    T[] Children { get; set;}
    T Parent { get; set; }
}

public static class HeirarchitalItemExtensions
{
    public static void PopulateParentage<T>(this IHeirarchicalItem<T>[] items, T parent)
        where T : IHeirarchicalItem<T>
    {
        foreach (var item in items.OfType<T>())
        {
            if (item.Parent == null)
            {
                item.Parent = parent;
            }

            var children = item.Children.OfType<IHeirarchicalItem<T>>().ToArray();
            if (children.Any())
            {
                PopulateParentage<T>(children, item);
            }
        }
    }
}