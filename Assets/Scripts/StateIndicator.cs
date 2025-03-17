using System;
using System.Linq;
using UnityEngine;

public class StateIndicator : MonoBehaviour
{
    public Sprite[] indicators;
    public int Current { get; private set; } = 0;
    private SpriteRenderer spriteRenderer;

    public Transform mainCamera;

    public void ChangeSprite(int index)
    {
        if (index < indicators.Count())
        {
            spriteRenderer.sprite = indicators[index];
            Current = index;
        }
    }

    public void NextSprite()
    {
        Current = (Current + 1) % indicators.Count();
        spriteRenderer.sprite = indicators[Current];
    }

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = indicators[0];

        mainCamera = Camera.main.transform;
    }

    private void Update()
    {
        Billboard();
    }

    public void Billboard()
    {
        transform.LookAt(mainCamera.position, Vector3.up);
    }
}
