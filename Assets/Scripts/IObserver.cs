using System;
using System.Collections;
using UnityEngine;

public interface IObserver<T>
{
    void OnValueChanged(T value);
}
