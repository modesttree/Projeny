using System.Collections;
using UnityEngine;

public class ShapeMover : MonoBehaviour
{
    public float Speed = 1;
    public float Amplitude = 1;
    public Vector3 Direction;

    public void Awake()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        float theta = 0;

        while (true)
        {
            transform.position = Direction * Amplitude * Mathf.Sin(theta);
            theta += Speed * Time.deltaTime;
            yield return null;
        }
    }
}
