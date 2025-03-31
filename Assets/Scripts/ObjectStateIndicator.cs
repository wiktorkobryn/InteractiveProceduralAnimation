using System;
using System.Linq;
using UnityEngine;

public class ObjectStateIndicator : StateIndicator, IObserver<int>
{
    public void OnValueChanged(int stateValue)
    {
        if(Current != stateValue)
            ChangeSprite(stateValue);
    }
}
