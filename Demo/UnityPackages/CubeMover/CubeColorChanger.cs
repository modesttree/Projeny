using System.Collections;
using UnityEngine;

public class CubeColorChanger : MonoBehaviour
{
    public float Speed = 1;

    public void Awake()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        float theta1 = Random.Range(0, 1);
        float theta2 = Random.Range(0, 1);

        var renderer = this.GetComponent<Renderer>();

        while (true)
        {
            renderer.material.color = Color.HSVToRGB(
                0.5f * (1 + Mathf.Sin(theta1)), 0.5f * (1 + Mathf.Sin(theta2)), 0.5f);

            theta1 += Speed * Time.deltaTime;
            theta2 += Speed * Time.deltaTime;

            yield return null;
        }
    }
}

