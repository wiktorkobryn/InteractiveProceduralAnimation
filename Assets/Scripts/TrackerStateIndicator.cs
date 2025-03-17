using System;
using System.Linq;
using UnityEngine;

public class TrackerStateIndicator : StateIndicator, IObserver<TrackerState>
{
    public void OnValueChanged(TrackerState value)
    {
        ChangeSprite((int)value);
    }
}
