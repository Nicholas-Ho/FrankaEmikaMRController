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
        manager.waypoints[index].SetWaypointTransform(endPosition, endRotation);
    }

    public void Unexecute()
    {
        manager.waypoints[index].SetWaypointTransform(startPosition, startRotation);
    }
}

struct WaypointCommandUtilities
{
    public static void AppendWaypoint(Vector3 position, Quaternion rotation, WalkthroughManager manager)
    {
        // Add waypoint object
        GameObject waypointObject = UnityEngine.Object.Instantiate(manager.waypointPrefab);
        waypointObject.transform.SetParent(manager.transform);

        // Set index and callbacks for delete and move
        Waypoint waypoint = waypointObject.GetComponent<Waypoint>();
        waypoint.SetIndex(manager.waypoints.Count);
        waypoint.SetButtonCallback(
            (eventData) => { manager.DeleteWaypoint(waypoint.GetIndex()); });
        waypoint.SetMoveCallback(
            (eventData) => {
                manager.MoveWaypoint(waypoint.GetIndex(),
                                     waypoint.transformer.startMovePosition,
                                     waypoint.transformer.startMoveRotation,
                                     waypoint.transformer.endMovePosition,
                                     waypoint.transformer.endMoveRotation);
            });
        waypoint.SetWaypointTransform(position, rotation);

        // Update the dynamic line
        if (manager.waypoints.Count > 0) manager.dynamicLine.SetActive(true);
        DynamicLine lineComponent = manager.dynamicLine.GetComponent<DynamicLine>();
        lineComponent.AddReferenceTransform(
            waypointObject.transform,
            (eventData) => {
                manager.InsertWaypoint(waypoint.GetIndex());
            });

        // Add waypoint to list
        manager.waypoints.Add(waypoint);
    }

    public static void PopLastWaypoint(WalkthroughManager manager)
    {
        // Remove last waypoint and line
        manager.dynamicLine.GetComponent<DynamicLine>().PopLastReferenceTransform();

        Waypoint lastWaypoint = manager.waypoints[^1];
        manager.waypoints.RemoveAt(manager.waypoints.Count-1);
        UnityEngine.Object.Destroy(lastWaypoint.gameObject);
    }

    public static void InsertWaypointAtIndex(int index,
                                             Vector3 position,
                                             Quaternion rotation,
                                             WalkthroughManager manager)
    {
        AppendWaypoint(Vector3.zero, Quaternion.identity, manager);

        // Shift waypoints one index backward
        for (int i=manager.waypoints.Count-1; i>index; i--) {
            TransformData next = manager.waypoints[i-1].GetWaypointTransform();
            manager.waypoints[i].SetWaypointTransform(
                next.position,
                next.rotation);
        }

        manager.waypoints[index].SetWaypointTransform(
            position,
            rotation);
        if (index > 0)
            manager.dynamicLine.GetComponent<DynamicLine>().ResetButtonState(index-1);
    }

    public static void DeleteWaypointAtIndex(int index,
                                             ref Vector3 position,
                                             ref Quaternion rotation,
                                             WalkthroughManager manager)
    {
        if (index >= manager.waypoints.Count) {
            Debug.LogWarning("Index out-of-range. No waypoints deleted.");
            return ;
        }

        // Store position and rotation in case of undo
        TransformData transformData = manager.waypoints[index].GetWaypointTransform();
        position = transformData.position;
        rotation = transformData.rotation;

        // Shift waypoints one index forward
        for (int i=index; i<(manager.waypoints.Count-1); i++) {
            TransformData next = manager.waypoints[i+1].GetWaypointTransform();
            manager.waypoints[i].SetWaypointTransform(
                next.position,
                next.rotation);
        }
        manager.waypoints[index].ResetButtonState();

        PopLastWaypoint(manager);
    }
}