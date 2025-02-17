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
    private WalkthroughManager manager;
    public AppendWaypointCommand(Vector3 pos, WalkthroughManager wm) { position = pos; manager = wm; }
    public void Execute()
    {
        WaypointCommandUtilities.AppendWaypointObject(position, Quaternion.identity, manager);
    }

    public void Unexecute()
    {
        WaypointCommandUtilities.PopLastWaypointObject(manager);
    }
}

struct DeleteWaypointCommand : IWaypointCommand
{
    private int index;
    private Vector3 poppedPosition;
    private Quaternion poppedRotation;
    private WalkthroughManager manager;
    public DeleteWaypointCommand(int i, WalkthroughManager wm) {
        index = i;
        manager = wm;

        // Defaults
        poppedPosition = Vector3.zero;
        poppedRotation = Quaternion.identity;
    }
    public void Execute()
    {
        if (index >= manager.waypointObjects.Count) {
            Debug.LogWarning("Index out-of-range. No waypoints deleted.");
            return ;
        }

        // Store position and rotation in case of undo
        poppedPosition = manager.waypointObjects[index].transform.position;
        poppedRotation = manager.waypointObjects[index].transform.rotation;

        // Shift waypoints one index forward
        for (int i=index; i<(manager.waypointObjects.Count-1); i++) {
            manager.waypointObjects[i].transform.position = manager.waypointObjects[i+1].transform.position;
            manager.waypointObjects[i].transform.rotation = manager.waypointObjects[i+1].transform.rotation;
            manager.waypointObjects[i].GetComponentInChildren<ProximityButton>().ResetState();
        }

        WaypointCommandUtilities.PopLastWaypointObject(manager);
    }

    public void Unexecute()
    {
        WaypointCommandUtilities.AppendWaypointObject(Vector3.zero, Quaternion.identity, manager);

        // Shift waypoints one index backward
        for (int i=manager.waypointObjects.Count-1; i>index; i--) {
            manager.waypointObjects[i].transform.position = manager.waypointObjects[i-1].transform.position;
            manager.waypointObjects[i].transform.rotation = manager.waypointObjects[i-1].transform.rotation;
            manager.waypointObjects[i].GetComponentInChildren<ProximityButton>().ResetState();
        }

        manager.waypointObjects[index].transform.position = poppedPosition;
        manager.waypointObjects[index].transform.rotation = poppedRotation;
    }
}

struct WaypointCommandUtilities
{
    public static void AppendWaypointObject(Vector3 position, Quaternion rotation, WalkthroughManager manager)
    {
        // Add waypoint object
        GameObject waypoint = UnityEngine.Object.Instantiate(manager.waypointPrefab, position, rotation);
        waypoint.transform.SetParent(manager.transform);
        waypoint.GetComponent<Waypoint>().SetIndex(manager.waypointObjects.Count);

        // Add delete callback
        waypoint.GetComponentInChildren<ProximityButton>().callback.AddListener(
            eventData => {
                Debug.Log(waypoint.GetComponent<Waypoint>().GetIndex()); manager.DeleteWaypoint(
                    waypoint.GetComponent<Waypoint>().GetIndex());
            }
        );

        // If there is already a waypoint, draw a dynamic line
        if (manager.waypointObjects.Count > 0) {
            GameObject line = UnityEngine.Object.Instantiate(manager.dynamicLinePrefab);
            line.transform.SetParent(manager.transform);
            DynamicLine dynamicLine = line.GetComponent<DynamicLine>();
            dynamicLine.ref1 = manager.waypointObjects[^1];
            dynamicLine.ref2 = waypoint;
            manager.lineObjects.Add(line);
        }

        // Add waypoint to list
        manager.waypointObjects.Add(waypoint);
    }

    public static void PopLastWaypointObject(WalkthroughManager manager)
    {
        // Remove last waypoint and line
        if (manager.lineObjects.Count > 0) {
            GameObject lastLine = manager.lineObjects[manager.lineObjects.Count-1];
            manager.lineObjects.RemoveAt(manager.lineObjects.Count-1);
            UnityEngine.Object.Destroy(lastLine);
        }
        GameObject lastWaypoint = manager.waypointObjects[^1];
        manager.waypointObjects.RemoveAt(manager.waypointObjects.Count-1);
        UnityEngine.Object.Destroy(lastWaypoint);
    }
}