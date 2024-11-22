using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UrdfPositioning;

public class DataTest : MonoBehaviour
{
    private bool initialised = false;
    // Start is called before the first frame update
    void Start()
    {
        Initialise();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();
    }

    void Initialise()
    {
        transform.position = UrdfPositioner.robotOriginTransform.position;
        transform.rotation = UrdfPositioner.robotOriginTransform.rotation;
        initialised = true;
    }
}
