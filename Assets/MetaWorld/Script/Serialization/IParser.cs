using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IParser<T>
{
    public bool TryParse(string str, out T obj);
    public T Parse(string str);
}
