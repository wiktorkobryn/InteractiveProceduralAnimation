using System;
using System.Collections;
using UnityEngine;

public interface IObservable
{
    void NotifyObservers();
    void AddObserver(GameObject obj);
    void DeleteObserver(GameObject obj);
}
