using System;
using System.Collections;
using System.Collections.Generic;
using Meta.WitAi;
using Oculus.Platform;
using UnityEngine;

namespace UrdfPositioning {
    [Serializable]
    public class UrdfRayPositioner
    {
        public LineRenderer visibleRay;
        public float maxRayLength = 2;
        public string layerName = "Room";

        private GameObject urdfModel;
        private LayerMask collisionLayerMask;
        private TransformDataCallback finaliseTransform;  // Used when position of model is confirmed.

        // To call during Start
        public void Initialise(GameObject model, TransformDataCallback callback)
        {
            urdfModel = model;
            urdfModel.SetActive(false);
            visibleRay.enabled = false;
            collisionLayerMask = LayerMask.GetMask(layerName);
            finaliseTransform = callback;  // For finalising the position and moving on to the next state.
        }

        // To call every Update
        public void Update()
        {
            bool cast = (
                OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) &&
                OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch)
            );
            urdfModel.SetActive(true);
            // visibleRay.enabled = cast;
            if (cast) {
                Vector3 controllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
                Vector3 controllerForward = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) *
                    Vector3.forward;

                // Cast ray
                RaycastHit hit;
                float rayLength = maxRayLength;
                if (Physics.Raycast(controllerPos, controllerForward, out hit, maxRayLength, collisionLayerMask)) {
                    rayLength = hit.distance;
                    urdfModel.transform.position = hit.point;
                    urdfModel.transform.up = hit.normal;
                } else {
                    urdfModel.SetActive(false);  // Set false here to allow urdfModel to update
                }

                // Render visible ray
                Vector3 endPoint = controllerPos + rayLength * controllerForward;
                visibleRay.positionCount = 2;
                visibleRay.SetPosition(0, controllerPos);
                visibleRay.SetPosition(1, endPoint);

                // Finalise position on "B" button press
                if (OVRInput.GetDown(OVRInput.RawButton.B)) {
                    TransformData data = new TransformData(urdfModel.transform);
                    finaliseTransform(data);
                    visibleRay.enabled = false;
                }
            } else {
                urdfModel.SetActive(false);  // Set false here to allow urdfModel to update
            }
        }
    }
}