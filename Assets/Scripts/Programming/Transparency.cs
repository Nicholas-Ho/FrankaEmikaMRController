using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transparency : MonoBehaviour
{    
    public float transparency = 0.25f;
    private bool set = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!set)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.material.color = new Color(
                    renderer.material.color.r,
                    renderer.material.color.g,
                    renderer.material.color.b,
                    transparency
                );
            }
            set = true;
        }
    }
}
