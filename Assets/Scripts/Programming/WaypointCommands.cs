using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// Command structs for use with WalkthroughManager. Allows undo and redo metacommands
interface IWaypointCommand { void Execute(); void Unexecute(); }

struct AppendWaypointCommand : IWaypointCommand
{
    private Vector3 position;
    private Quaternion rotation;
    private WalkthroughManager manager;
    public AppendWaypointCommand(Vector3 pos, Quaternion rot, WalkthroughManager wm) {
        position = pos;
        rotation = rot;
        manager = wm;
    }

    public void Execute()
    {
        WaypointCommandUtilities.AppendWaypoint(position, rotation, manager);
    }

    public void Unexecute()
    {
        WaypointCommandUtilities.PopLastWaypoint(manager);
    }
}

struct InsertWaypointCommand : IWaypointCommand
{
    private int index;
    private Vector3 position;
    private Quaternion rotation;
    private WalkthroughManager manager;
    public InsertWaypointCommand(int i, Vector3 pos, Quaternion rot, WalkthroughManager wm) {
        index = i;
        position = pos;
        rotation = rot;
        manager = wm;
    }

    public void Execute()
    {
        WaypointCommandUtilities.InsertWaypointAtIndex(index, position, rotation, manager);
    }

    public void Unexecute()
    {
        WaypointCommandUtilities.DeleteWaypointAtIndex(index, ref position, ref rotation, manager);
    }
}

struct DeleteWaypointCommand : IWaypointCommand
{
    private int index;
    private Vector3 position;
    private Quaternion rotation;
    private WalkthroughManager manager;
    public DeleteWaypointCommand(int i, WalkthroughManager wm) {
        index = i;
        manager = wm;

        // Defaults
        position = Vector3.zero;
        rotation = Quaternion.identity;
    }

    public void Execute()
    {
        WaypointCommandUtilities.DeleteWaypointAtIndex(index, ref position, ref rotation, manager);
    }

    public void Unexecute()
    {
        WaypointCommandUtilities.InsertWaypointAtIndex(index, position, rotation, manager);
    }
}

struct MoveWaypointCommand : IWaypointCommand
{
    private int index;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 endPosition;
    private Quaternion endRotation;
    private WalkthroughManager manager;
    public MoveWaypointCommand(int i, Vector3 startPos, Quaternion startRot, Vector3 endPos, Quaternion endRot, WalkthroughManager wm) {
        index = i;
        manager = wm;
        startPosition = startPos;
        startRotation = startRot;
        endPosition = endPos;
        endRotation = endRot;
    }
    public void Execute()
    {
        manager.waypointObjects[index].GetComponent<Waypoint>().SetWaypointTransform(endPosition, endRotation);
    }

    public void Unexecute()
    {
        manager.waypointObjects[index].GetComponent<Waypoint>().SetWaypointTransform(startPosition, startRotation);
    }
}

struct WaypointCommandUtilities
{
    public static void AppendWaypoint(Vector3 position, Quaternion rotation, WalkthroughManager manager)
    {
        // Add waypoint object
        GameObject waypoint = UnityEngine.Object.Instantiate(manager.waypointPrefab);
        waypoint.transform.SetParent(manager.transform);

        // Set index and callbacks for delete and move
        Waypoint waypointComponent = waypoint.GetComponent<Waypoint>();
        waypointComponent.SetIndex(manager.waypointObjects.Count);
        waypointComponent.SetButtonCallback(
            (eventData) => { manager.DeleteWaypoint(waypointComponent.GetIndex()); });
        waypointComponent.SetMoveCallback(
            (eventData) => {
                manager.MoveWaypoint(waypointComponent.GetIndex(),
                                     waypointComponent.transformer.startMovePosition,
                                     waypointComponent.transformer.startMoveRotation,
                                     waypointComponent.transformer.endMovePosition,
                                     waypointComponent.transformer.endMoveRotation);
            });
        waypointComponent.SetWaypointTransform(position, rotation);

        // If there is already a waypoint, draw a dynamic line
        if (manager.waypointObjects.Count > 0) {
            GameObject line = UnityEngine.Object.Instantiate(manager.dynamicLinePrefab);
            line.transform.SetParent(manager.transform);
            DynamicLine lineComponent = line.GetComponent<DynamicLine>();
            lineComponent.ref1 = manager.waypointObjects[^1].transform;
            lineComponent.ref2 = waypoint.transform;
            lineComponent.SetButtonCallback(
                (eventData) => {
                    manager.InsertWaypoint(waypointComponent.GetIndex());
                });
            manager.lineObjects.Add(line);
        }

        // Add waypoint to list
        manager.waypointObjects.Add(waypoint);
    }

    public static void PopLastWaypoint(WalkthroughManager manager)
    {
        // Remove last waypoint and line
        if (manager.lineObjects.Count > 0) {
            GameObject lastLine = manager.lineObjects[^1];
            manager.lineObjects.RemoveAt(manager.lineObjects.Count-1);
            UnityEngine.Object.Destroy(lastLine);
        }
        GameObject lastWaypoint = manager.waypointObjects[^1];
        manager.waypointObjects.RemoveAt(manager.waypointObjects.Count-1);
        UnityEngine.Object.Destroy(lastWaypoint);
    }

    public static void InsertWaypointAtIndex(int index,
                                             Vector3 position,
                                             Quaternion rotation,
                                             WalkthroughManager manager)
    {
        AppendWaypoint(Vector3.zero, Quaternion.identity, manager);

        // Shift waypoints one index backward
        for (int i=manager.waypointObjects.Count-1; i>index; i--) {
            manager.waypointObjects[i].GetComponent<Waypoint>().SetWaypointTransform(
                manager.waypointObjects[i-1].transform.position,
                manager.waypointObjects[i-1].GetComponent<Waypoint>().grabTransform.rotation);
        }

        manager.waypointObjects[index].GetComponent<Waypoint>().SetWaypointTransform(
            position,
            rotation);
        manager.lineObjects[index-1].GetComponent<DynamicLine>().ResetButtonState();
    }

    public static void DeleteWaypointAtIndex(int index,
                                             ref Vector3 position,
                                             ref Quaternion rotation,
                                             WalkthroughManager manager)
    {
        if (index >= manager.waypointObjects.Count) {
            Debug.LogWarning("Index out-of-range. No waypoints deleted.");
            return ;
        }

        // Store position and rotation in case of undo
        position = manager.waypointObjects[index].transform.position;
        rotation = manager.waypointObjects[index].GetComponent<Waypoint>().grabTransform.rotation;

        // Shift waypoints one index forward
        for (int i=index; i<(manager.waypointObjects.Count-1); i++) {
            manager.waypointObjects[i].GetComponent<Waypoint>().SetWaypointTransform(
                manager.waypointObjects[i+1].transform.position,
                manager.waypointObjects[i+1].GetComponent<Waypoint>().grabTransform.rotation);
        }
        manager.waypointObjects[index].GetComponent<Waypoint>().ResetButtonState();

        PopLastWaypoint(manager);
    }
}