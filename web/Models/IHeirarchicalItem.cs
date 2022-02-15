using System;
using System.Collections.Generic;

namespace Models
{
    public interface IHeirarchicalItem<T>
    {
        T[] Children { get; set;}
        T Parent { get; set; }
    }
}