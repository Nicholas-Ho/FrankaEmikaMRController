using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using UrdfPositioning;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class WalkthroughManager : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/robot_current_pose";
    public string pubTopic = "/target_data";
    public GameObject waypointPrefab;
    public GameObject dynamicLinePrefab;
    private List<GameObject> waypointObjects =  new List<GameObject>();
    private List<GameObject> lineObjects = new List<GameObject>();
    private bool initialised = false;
    private bool connected = false;
    private bool zeroSpring = false;
    private Vector3 position = Vector3.zero;

    // Persistent across scenes
    [HideInInspector]
    public static List<Vector3> waypoints =  new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {
        Initialise();

        // For reloading the programming scene
        foreach (Vector3 waypointPos in waypoints) {
            ConstructWaypoint(waypointPos);
        }
        waypoints = new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();

        // Set spring constant to near zero
        if (initialised && connected && !zeroSpring) {
            // [x, y, z, spring_k, damper_k]
            Float64MultiArrayMsg target_data = new Float64MultiArrayMsg();
            target_data.data = new double[]{0, 0, 0, 0.0001, 5};
            ros.Publish(pubTopic, target_data);
            zeroSpring = true;
        }
    }

    void Initialise()
    {
        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PoseStampedMsg>(subTopic, SubscribeCallback);
        ros.RegisterPublisher<Float64MultiArrayMsg>(pubTopic);
        initialised = true;
    }

    void SubscribeCallback(PoseStampedMsg msg)
    {
        // Keep track of position for retrieval
        Vector3 _position = new Vector3(
            (float)-msg.pose.position.y,  // Note: Swapped around x and y
            (float)msg.pose.position.z,
            (float)msg.pose.position.x);
        position = UrdfPositioner.TransformFromRobotSpace(_position);
        connected = true;
    }

    public void AddWaypoint()
    {
        if (!initialised) return ;

        // Add waypoint object
        ConstructWaypoint(position);
    }

    private void ConstructWaypoint(Vector3 waypointPos)
    {
        // Add waypoint object
        GameObject waypoint = Instantiate(waypointPrefab, waypointPos, Quaternion.identity);
        waypoint.transform.SetParent(transform);
        waypoint.GetComponent<Waypoint>().SetIndex(waypointObjects.Count);

        // Add delete callback
        waypoint.GetComponentInChildren<ProximityButton>().callback.AddListener(
            (BaseEventData eventData) => { Debug.Log(waypoint.GetComponent<Waypoint>().GetIndex()); DeleteWaypoint(waypoint.GetComponent<Waypoint>().GetIndex()); });

        // If there is already a waypoint, draw a dynamic line
        if (waypointObjects.Count > 0) {
            GameObject line = Instantiate(dynamicLinePrefab);
            line.transform.SetParent(transform);
            DynamicLine dynamicLine = line.GetComponent<DynamicLine>();
            dynamicLine.ref1 = waypointObjects[waypointObjects.Count-1];
            dynamicLine.ref2 = waypoint;
            lineObjects.Add(line);
        }

        // Add waypoint to list
        waypointObjects.Add(waypoint);
    }

    private void DeleteWaypoint(int index)
    {
        if (index >= waypointObjects.Count) {
            Debug.LogWarning("Index out-of-range. No waypoints deleted.");
            return ;
        }

        // Shift waypoints one index forward
        for (int i=index; i<(waypointObjects.Count-1); i++) {
            waypointObjects[i].transform.position = waypointObjects[i+1].transform.position;
            waypointObjects[i].transform.rotation = waypointObjects[i+1].transform.rotation;
            waypointObjects[i].GetComponentInChildren<ProximityButton>().ResetState();
        }

        // Remove last waypoint and line
        if (lineObjects.Count > 0) {
            GameObject lastLine = lineObjects[lineObjects.Count-1];
            lineObjects.RemoveAt(lineObjects.Count-1);
            Destroy(lastLine);
        }
        GameObject lastWaypoint = waypointObjects[waypointObjects.Count-1];
        waypointObjects.RemoveAt(waypointObjects.Count-1);
        Destroy(lastWaypoint);
    }

    public void BeginExecutionPhase()
    {
        if (!initialised) return ;
        waypoints = new List<Vector3>();
        foreach (GameObject waypoint in waypointObjects) {
            waypoints.Add(waypoint.transform.position);
        }

        SceneManager.LoadSceneAsync(2, LoadSceneMode.Single);
    }
}
