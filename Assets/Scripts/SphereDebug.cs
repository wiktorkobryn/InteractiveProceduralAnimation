using UnityEngine;

public class SphereDebug : MonoBehaviour
{
    public float radius = 0.06f;
    public Color color = Color.yellow;

    private void OnValidate()
    {
        //
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, radius);
    }
}
