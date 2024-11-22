using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UrdfPositioning {
    public delegate void TransformDataCallback(TransformData data);
    enum PositionState {
        Ray,
        Gizmo,
        Fixed
    }

    public class UrdfPositioner : MonoBehaviour
    {
        public GameObject urdfModel;
        public UrdfRayPositioner rayPositioner = new UrdfRayPositioner();
        public UrdfGizmoPositioner gizmoPositioner = new UrdfGizmoPositioner();

        private PositionState state = PositionState.Ray;

        // Persistent across scenes
        [HideInInspector]
        public static TransformData robotOriginTransform;
        private static Quaternion inverseOriginQuaternion;
        private static bool invertedQuaternion = false;
        public static Vector3 TransformFromRobotSpace(Vector3 vector) {
            return robotOriginTransform.rotation * vector +
                robotOriginTransform.position;
        }

        public static Vector3 TransformToRobotSpace(Vector3 vector) {
            if (!invertedQuaternion) {
                inverseOriginQuaternion = Quaternion.Inverse(robotOriginTransform.rotation);
                invertedQuaternion = true;
            }
            return inverseOriginQuaternion * (vector - robotOriginTransform.position);
        }

        // Start is called before the first frame update
        void Start()
        {
            rayPositioner.Initialise(urdfModel, StartGizmoState);
        }

        // Update is called once per frame
        void Update()
        {
            if (state == PositionState.Ray) {
                rayPositioner.Update();
            } else if (state == PositionState.Gizmo) {
                gizmoPositioner.Update();
            }
        }

        public void StartGizmoState(TransformData data) {
            state = PositionState.Gizmo;
            gizmoPositioner.Initialise(urdfModel, data, StartFixedState);
        }

        public void StartFixedState(TransformData data) {
            state = PositionState.Fixed;
            urdfModel.SetActive(false);
            robotOriginTransform = data;

            SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
        }
    }
}
