using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.FrankaExampleControllers;
using UrdfPositioning;
using System.Collections.Generic;

public class WalkthroughManager : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/robot_current_pose";
    public string pubTopic = "/target_data";
    public GameObject waypointPrefab;
    public GameObject dynamicLine;
    [HideInInspector]
    public List<Waypoint> waypoints =  new();
    private bool initialised = false;
    private bool connected = false;
    private bool active = true;
    private float zeroSpringWait = 0f;
    private readonly float zeroSpringWaitThresh = 1.0f;
    private Vector3 position = Vector3.zero;
    private Quaternion rotation = Quaternion.identity;

    private static Stack<IWaypointCommand> commands = new Stack<IWaypointCommand>();
    private static Stack<IWaypointCommand> undoneCommands = new Stack<IWaypointCommand>();  // Stack for undone commands. Cleared on new command added.

    // Start is called before the first frame update
    void Start()
    {
        Initialise();
        dynamicLine.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();

        // Set spring constant to zero
        if (initialised && connected && active && zeroSpringWait < zeroSpringWaitThresh) {
            // [x, y, z, spring_k, damper_k]
            TargetPoseMsg msg = new();
            msg.pose.position = new PointMsg();
            msg.pose.orientation = new QuaternionMsg
            {
                x = 0,
                y = 0,
                z = 0
            };
            msg.cartesian_stiffness = 0;
            msg.rotational_stiffness = 0;

            ros.Publish(pubTopic, msg);
            zeroSpringWait += Time.deltaTime;
        }
    }

    void Initialise()
    {
        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PoseStampedMsg>(subTopic, SubscribeCallback);
        ros.RegisterPublisher<TargetPoseMsg>(pubTopic);
        initialised = true;
    }

    void SubscribeCallback(PoseStampedMsg msg)
    {
        position = UrdfPositioner.VectorFromRobotSpace(msg.pose.position.From<FLU>());
        rotation = UrdfPositioner.RotateFromRobotSpace(msg.pose.orientation.From<FLU>());
        connected = true;
    }

    // public void OnDestroy()
    // {
    //     ros.Unsubscribe(subTopic);
    // }

    public void ActivateWalkthroughMode()
    {
        active = true;
        zeroSpringWait = 0;
    }

    public void DeactivateWalkthroughMode() => active = false;

    public void AddWaypoint()
    {
        if (!initialised) return ;

        // Add waypoint object at the robot position
        AddCommand(new AppendWaypointCommand(position, rotation, this));
        // AddCommand(new AppendWaypointCommand(new Vector3(0,1,0), Quaternion.identity, this));  // For testing
    }

    public void InsertWaypoint(int index)
    {
        AddCommand(new InsertWaypointCommand(index, position, rotation, this));
    }

    public void InsertWaypointAtPosition(int index, Vector3 insertPos, Quaternion insertRot)
    {
        AddCommand(new InsertWaypointCommand(index, insertPos, insertRot, this));
    }

    public void DeleteWaypoint(int index)
    {
        AddCommand(new DeleteWaypointCommand(index, this));
    }

    public void MoveWaypoint(int index, Vector3 startPos, Quaternion startRot, Vector3 endPos, Quaternion endRot)
    {
        AddCommand(new MoveWaypointCommand(index, startPos, startRot, endPos, endRot, this));
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
