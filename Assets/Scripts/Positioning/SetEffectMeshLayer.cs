using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using Unity.VisualScripting;
using UnityEngine;

public class SetEffectMeshLayer : MonoBehaviour
{
    public string layerName = "Room";

    // Awake to ensure that layer is changed before meshes are created.
    void Awake()
    {
        gameObject.GetComponent<EffectMesh>().Layer = LayerMask.NameToLayer(layerName);
    }
}
