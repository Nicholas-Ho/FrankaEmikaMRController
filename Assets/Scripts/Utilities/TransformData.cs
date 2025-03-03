using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TransformData {
    public Vector3 position;
    public Quaternion rotation;

    public TransformData(Transform transform) {
        position = transform.position;
        rotation = transform.rotation;
    }

    public TransformData(Vector3 pos, Quaternion rot) {
        position = pos;
        rotation = rot;
    }
}