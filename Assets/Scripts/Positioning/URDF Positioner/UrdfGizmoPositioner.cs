using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UrdfPositioning {
    [Serializable]
    public class UrdfGizmoPositioner
    {
        public GameObject gizmoSet;

        private GameObject urdfModel;
        private TransformDataCallback finaliseTransform;  // Used when position of model is confirmed.

        public void Initialise(GameObject model, TransformData data, TransformDataCallback callback) {
            urdfModel = model;
            urdfModel.transform.position = data.position;
            urdfModel.transform.rotation = data.rotation;
            urdfModel.SetActive(true);

            gizmoSet.SetActive(true);
            gizmoSet.transform.position = data.position;
            urdfModel.transform.parent = gizmoSet.GetComponent<Repositioner>().objectHolder;

            finaliseTransform = callback;
        }

        // To call every Update
        public void Update()
        {
            // Finalise position on "B" button press
            if (OVRInput.GetDown(OVRInput.RawButton.B)) {
                TransformData data = new TransformData(urdfModel.transform);
                urdfModel.transform.parent = null;
                gizmoSet.SetActive(false);
                finaliseTransform(data);
            }
        }
    }
}