using System.Collections;
using System.Collections.Generic;
using Assimp;
using UnityEngine;

public class JointPositionState : MonoBehaviour
{
    public string prefix = "fer_link";
    public int numberOfJoints = 7;
    private Transform[] linkRefs;  // Links after joints
    private bool refsSet = false;
    private bool jointsSet = false;

    // Start is called before the first frame update
    void Start()
    {
        GetLinkReferences();
    }

    // Update is called once per frame
    void Update()
    {
        // SetJointPositions(new float[] {30, 30, 30, 30, 30, 30, 30});
    }

    // Get all references to the links corresponding to each joint
    void GetLinkReferences()
    {
        linkRefs = new Transform[numberOfJoints];
        Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (!child.name.StartsWith(prefix)) continue;  // Child is not a link
            string linkIndexStr = child.name.Substring(prefix.Length);
            int linkIndex;
            if (!int.TryParse(linkIndexStr, out linkIndex)) continue;  // String parsing failed

            // If within range, add to references. Note leftshift of index.
            if (--linkIndex >= 0 && linkIndex < numberOfJoints) linkRefs[linkIndex] = child;
        }
        refsSet = true;
    }

    // Set the joint positions
    public void SetJointPositions(float[] positions)
    {
        if (!refsSet) GetLinkReferences();

        if (linkRefs.Length != positions.Length)
        {
            Debug.LogWarning("Unable to update joint positions: number of joints does not match.");
            return;
        }

        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 newJointPos = linkRefs[i].localEulerAngles;
            // Accounting for URDF import shenanigans
            if (i == 0)
            {
                newJointPos.y = -positions[i];
            }
            else if (i == 1 || i == 4)
            {
                newJointPos.x = positions[i];
            }
            else
            {
                newJointPos.x = -positions[i];
            }
            if (i == 3 || i == 5)
            {
                newJointPos.y = 0; newJointPos.z = -90;
            }
            linkRefs[i].localEulerAngles = newJointPos;
        }

        jointsSet = true;
    }

    public bool IsSet() { return jointsSet;  }
}
