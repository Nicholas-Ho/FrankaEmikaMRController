using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using UrdfPositioning;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class WalkthroughManager : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/vmc_controller/robot_current_pose";
    public string pubTopic = "/vmc_controller/equilibrium_pose";
    public GameObject waypointPrefab;
    private List<GameObject> waypointObjects =  new List<GameObject>();
    private bool initialised = false;
    private Vector3 position = Vector3.zero;

    // Persistent across scenes
    [HideInInspector]
    public static List<Vector3> waypoints =  new List<Vector3>();

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
        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PoseStampedMsg>(subTopic, SubscribeCallback);
        ros.RegisterPublisher<PoseStampedMsg>(pubTopic);
        initialised = true;
    }

    void SubscribeCallback(PoseStampedMsg msg)
    {
        PublishPose(msg.pose.position.x,
                    msg.pose.position.y,
                    msg.pose.position.z,
                    msg.pose.orientation);

        // Keep track of position for retrieval
        Vector3 _position = new Vector3(
            (float)-msg.pose.position.y,  // Note: Swapped around x and y
            (float)msg.pose.position.z,
            (float)msg.pose.position.x);
        position = UrdfPositioner.TransformFromRobotSpace(_position);
    }

    void PublishPose(double x, double y, double z, QuaternionMsg orientation)
    {
        PoseStampedMsg msg = new PoseStampedMsg();
        msg.pose.position.x = x;
        msg.pose.position.y = y;
        msg.pose.position.z = z;
        msg.pose.orientation = orientation;  // Not dealing with orientation for now
        msg.header.frame_id = "panda_link0";

        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
        msg.header.stamp.sec = (uint)t.TotalSeconds;

        ros.Publish(pubTopic, msg);
    }

    public void AddWaypoint()
    {
        if (!initialised) return ;
        GameObject waypoint = Instantiate(waypointPrefab, position, Quaternion.identity);
        waypoint.GetComponent<Waypoint>().SetIndex(waypointObjects.Count);
        waypointObjects.Add(waypoint);
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
