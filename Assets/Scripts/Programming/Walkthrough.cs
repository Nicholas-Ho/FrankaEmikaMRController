using UnityEngine;
using System.Collections.Generic;

public class WalkthroughManager : MonoBehaviour
{
    public Transform endTrackerTransform;
    public GameObject waypointPrefab;
    public GameObject dynamicLine;
    [HideInInspector]
    public List<Waypoint> waypoints =  new();
    private EndTracker endTracker;
    private Vector3 position { get => endTrackerTransform.position; set => endTrackerTransform.position = value; }
    private Quaternion rotation { get => endTrackerTransform.rotation; set => endTrackerTransform.rotation = value; }

    private static Stack<IWaypointCommand> commands = new();
    private static Stack<IWaypointCommand> undoneCommands = new();  // Stack for undone commands. Cleared on new command added.

    // Start is called before the first frame update
    void Start()
    {
        dynamicLine.SetActive(false);
        endTracker = endTrackerTransform.GetComponent<EndTracker>();
    }

    public void AddWaypoint()
    {
        if (!endTracker.connected) return ;
        AddCommand(new AppendWaypointCommand(position, rotation, this));
        // AddCommand(new AppendWaypointCommand(GameObject.FindWithTag("MainCamera").transform.position, Quaternion.identity, this));  // For testing
    }

    public void InsertWaypoint(int index)
    {
        if (!endTracker.connected) return ;
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
    private void AddCommand(IWaypointCommand command)
    {
        command.Execute();
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
