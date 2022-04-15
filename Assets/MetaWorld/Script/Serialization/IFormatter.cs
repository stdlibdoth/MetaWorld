using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFormatter<T>
{
    public string Format(T data);
}
