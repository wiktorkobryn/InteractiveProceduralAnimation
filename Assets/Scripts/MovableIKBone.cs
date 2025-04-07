using System;
using UnityEngine;

[Serializable]
public class MovableIKBone
{
    public Transform targetIK, homeMarker;
    public bool CanMove { get; set; } = false;
}
