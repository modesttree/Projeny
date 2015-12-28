using System.Collections;
using UnityEngine;

public class SphereColorChanger : MonoBehaviour
{
    public float Delay = 0.5f;

    public void Awake()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        var renderer = this.GetComponent<Renderer>();

        while (true)
        {
            var values = Random.onUnitSphere;
            renderer.material.color = Color.HSVToRGB(values.x, values.y, 1.0f);
            yield return new WaitForSeconds(Delay);
        }
    }
}

