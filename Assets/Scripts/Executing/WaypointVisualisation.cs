using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaypointVisualisation : MonoBehaviour
{
    public GameObject staticWaypointPrefab;
    private static List<Vector3> waypoints =  new List<Vector3>();
    private static List<GameObject> waypointObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        waypoints = WalkthroughManager.waypoints;
        for (int i=0; i<waypoints.Count; i++) {
            GameObject waypointObject = Instantiate(staticWaypointPrefab,
                                                    waypoints[i],
                                                    Quaternion.identity);
            waypointObject.GetComponentInChildren<Waypoint>().SetIndex(i);
            waypointObjects.Add(waypointObject);
            waypointObject.transform.SetParent(transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
