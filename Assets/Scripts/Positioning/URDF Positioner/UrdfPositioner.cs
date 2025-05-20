using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UrdfPositioning {
    public delegate void TransformDataCallback(TransformData data);
    enum PositionState {
        None,
        Ray,
        Gizmo,
        Confirm,
        Fixed
    }

    public class UrdfPositioner : MonoBehaviour
    {
        public GameObject urdfModel;
        public UrdfRayPositioner rayPositioner = new();
        public UrdfGizmoPositioner gizmoPositioner = new();
        public SpatialAnchorManager anchorManager = new();

        private PositionState state = PositionState.None;

        private const string playerPrefsAnchorKey = "FrankEmikaAnchorID";

        // Persistent across scenes
        [HideInInspector]
        public static TransformData robotOriginTransform;
        private static Quaternion inverseOriginQuaternion;
        private static bool invertedQuaternion = false;
        public static Vector3 VectorFromRobotSpace(Vector3 vector)
        {
            return robotOriginTransform.rotation * vector +
                robotOriginTransform.position;
        }

        public static Vector3 VectorToRobotSpace(Vector3 vector)
        {
            if (!invertedQuaternion)
            {
                inverseOriginQuaternion = Quaternion.Inverse(robotOriginTransform.rotation);
                invertedQuaternion = true;
            }
            return inverseOriginQuaternion * (vector - robotOriginTransform.position);
        }

        public static Quaternion RotateFromRobotSpace(Quaternion rotation)
        {
            return robotOriginTransform.rotation * rotation;
        }

        public static Quaternion RotateToRobotSpace(Quaternion rotation)
        {
            if (!invertedQuaternion)
            {
                inverseOriginQuaternion = Quaternion.Inverse(robotOriginTransform.rotation);
                invertedQuaternion = true;
            }
            return inverseOriginQuaternion * rotation;
        }

        // Start is called before the first frame update
        void Start()
        {
            LoadAnchor();
        }

        // Update is called once per frame
        void Update()
        {
            if (state == PositionState.Ray)
            {
                rayPositioner.Update();
            }
            else if (state == PositionState.Gizmo)
            {
                gizmoPositioner.Update();
            }
        }

        public void StartRayState()
        {
            _ = anchorManager.EraseAnchor(playerPrefsAnchorKey);
            state = PositionState.Ray;
            rayPositioner.Initialise(urdfModel, StartGizmoState);
        }

        public void StartGizmoState(TransformData data)
        {
            state = PositionState.Gizmo;
            gizmoPositioner.Initialise(urdfModel, data, StartFixedState);
        }

        public void StartFixedState(TransformData data)
        {
            state = PositionState.Fixed;
            robotOriginTransform = data;

            urdfModel.SetActive(false);
            anchorManager.CreateAndSaveAnchor(data, playerPrefsAnchorKey, () => { LoadNextScene(); });
        }

        public void StartConfirmState(TransformData data)
        {
            state = PositionState.Confirm;
            robotOriginTransform = data;
            urdfModel.transform.SetPositionAndRotation(data.position, data.rotation);
        }

        public void LoadNextScene() { SceneManager.LoadSceneAsync(1, LoadSceneMode.Single); }

        public void HandSelect()
        {
            if (state == PositionState.Ray)
            {
                rayPositioner.HandSelect();
            }
            else if (state == PositionState.Gizmo)
            {
                gizmoPositioner.HandSelect();
            }
            else if (state == PositionState.Confirm)
            {
                urdfModel.SetActive(false);
                LoadNextScene();
            }
        }

        private void LoadAnchor()
        {
            _ = anchorManager.LoadAnchor(playerPrefsAnchorKey,
            // Successful load
            (success, anchor) =>
            {
                if (success)
                {
                    // Successful localise
                    OVRSpatialAnchor anchorObject = new GameObject().AddComponent<OVRSpatialAnchor>();
                    anchor.BindTo(anchorObject);
                    TransformData data = new()
                    {
                        position = anchorObject.transform.position,
                        rotation = anchorObject.transform.rotation
                    };
                    Destroy(anchorObject.gameObject);
                    Debug.Log("Anchor loaded");
                    StartConfirmState(data);
                }
                else
                {
                    // Unsuccessful localise
                    Debug.LogWarning("Unable to localise anchor");
                    StartRayState();
                }
            },
            // Unsuccessful load
            () =>
            {
                Debug.LogWarning("Failed to load anchor");
                StartRayState();
            }
        );
        }
    }
}
