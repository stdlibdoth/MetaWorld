using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISerializer<T>
{
    public string Serialize(T obj);
}
