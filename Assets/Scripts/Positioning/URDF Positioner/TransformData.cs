using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UrdfPositioning {
    public struct TransformData {
        public Vector3 position;
        public Quaternion rotation;

        public TransformData(Transform transform) {
            position = transform.position;
            rotation = transform.rotation;
        }
    }
}
