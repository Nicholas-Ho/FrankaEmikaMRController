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
    [HideInInspector]
    public List<GameObject> waypointObjects =  new List<GameObject>();
    [HideInInspector]
    public List<GameObject> lineObjects = new List<GameObject>();
    private bool initialised = false;
    private bool connected = false;
    private float zeroSpringWait = 0f;
    private float zeroSpringWaitThresh = 1.0f;
    private Vector3 position = Vector3.zero;

    private static Stack<IWaypointCommand> commands = new Stack<IWaypointCommand>();
    private static Stack<IWaypointCommand> undoneCommands = new Stack<IWaypointCommand>();  // Stack for undone commands. Cleared on new command added.

    // Persistent across scenes
    [HideInInspector]
    public static List<Vector3> waypoints =  new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {
        Initialise();

        // For reloading the programming scene
        foreach (Vector3 waypointPos in waypoints) {
            AddCommand(new AppendWaypointCommand(waypointPos, this), true);
        }
        waypoints = new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();

        // Set spring constant to near zero
        if (initialised && connected && zeroSpringWait < zeroSpringWaitThresh) {
            // [x, y, z, spring_k, damper_k]
            Float64MultiArrayMsg target_data = new Float64MultiArrayMsg();
            target_data.data = new double[]{0, 0, 0, 0.0001, 5};
            ros.Publish(pubTopic, target_data);
            zeroSpringWait += Time.deltaTime;
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

    public void OnDestroy()
    {
        ros.Unsubscribe(subTopic);
    }

    public void AddWaypoint()
    {
        if (!initialised) return ;

        // Add waypoint object at the robot position
        AddCommand(new AppendWaypointCommand(position, this));
        // AddCommand(new AppendWaypointCommand(new Vector3(0,1,0), this));  // For testing
    }

    public void InsertWaypoint(int index, Vector3 position)
    {
        AddCommand(new InsertWaypointCommand(index, position, this));
    }

    public void DeleteWaypoint(int index)
    {
        AddCommand(new DeleteWaypointCommand(index, this));
    }

    public void MoveWaypoint(int index, Vector3 startPos, Quaternion startRot, Vector3 endPos, Quaternion endRot)
    {
        AddCommand(new MoveWaypointCommand(index, startPos, startRot, endPos, endRot, this));
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


    // Commands infrastructure
    private void AddCommand(IWaypointCommand command, bool skipStack = false)
    {
        command.Execute();
        if (skipStack) return ;  // Option to skip using the command stack
        commands.Push(command);
        if (undoneCommands.Count > 0) undoneCommands.Clear();
    }

    public void UndoCommand()
    {
        if (commands.Count == 0) return ;
        IWaypointCommand command = commands.Peek();
        command.Unexecute();
        commands.Pop();
        undoneCommands.Push(command);
    }

    public void RedoCommand()
    {
        if (undoneCommands.Count == 0) return ;
        IWaypointCommand command = undoneCommands.Peek();
        command.Execute();
        undoneCommands.Pop();
        commands.Push(command);
    }
}
