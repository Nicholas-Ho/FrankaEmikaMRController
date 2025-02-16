using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicLine : MonoBehaviour
{
    [HideInInspector]
    public GameObject ref1, ref2;
    private LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPosition(0, ref1.transform.position);
        lineRenderer.SetPosition(1, ref2.transform.position);
    }
}
