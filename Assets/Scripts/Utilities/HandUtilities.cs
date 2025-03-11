using UnityEngine;

class HandUtilities
{
    static public Transform GetHandTransform(OVRSkeleton skeleton)
    {
        // Uses middle knuckle as reference
        foreach (var b in skeleton.Bones) {
            if (b.Id == OVRSkeleton.BoneId.Hand_Middle1) {
                return b.Transform;
            }
        }
        Debug.LogWarning("Middle knuckle not found");
        return skeleton.Bones[0].Transform;
    }

    static public Transform GetIndexTipTransform(OVRSkeleton skeleton)
    {
        foreach (var b in skeleton.Bones) {
            if (b.Id == OVRSkeleton.BoneId.Hand_IndexTip) {
                return b.Transform;
            }
        }
        Debug.LogWarning("Right index finger not found");
        return skeleton.Bones[0].Transform;
    }

    static public Vector3 GetRelativePlaneProjected(Vector3 position, Vector3 normal, Vector3 reference)
    {
        // Vector3 is a struct - pass by value. OK to modify
        position -= reference;
        return Vector3.ProjectOnPlane(position, normal);
    }
}