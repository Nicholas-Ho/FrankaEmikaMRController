using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GK;

public enum AnchorStatus
{
    Loading = 0,
    Available = 1,
    Unavailable = 2
};

public class URDFPointCloudManager : MonoBehaviour
{
    public SceneMeshManager sceneMeshManager;
    public JointPositionState jointsManager;
    public SpatialAnchorManager anchorManager;
    public GameObject urdfModel;
    private List<Vector3> urdfHullVertices;

    private const string playerPrefsAnchorKey = "FrankEmikaAnchorID";
    private AnchorStatus anchorStatus = AnchorStatus.Loading;

    // Persistent across scenes
    [HideInInspector]
    public static TransformData robotOriginTransform;
    private static bool robotOriginAvailable = false;
    private static Quaternion inverseOriginQuaternion;
    private static bool invertedQuaternion = false;
    public static Vector3 VectorFromRobotSpace(Vector3 vector)
    {
        return robotOriginTransform.rotation * vector +
            robotOriginTransform.position;
    }

    public static Vector3 VectorToRobotSpace(Vector3 vector)
    {
        if (!invertedQuaternion)
        {
            inverseOriginQuaternion = Quaternion.Inverse(robotOriginTransform.rotation);
            invertedQuaternion = true;
        }
        return inverseOriginQuaternion * (vector - robotOriginTransform.position);
    }

    public static Quaternion RotateFromRobotSpace(Quaternion rotation)
    {
        return robotOriginTransform.rotation * rotation;
    }

    public static Quaternion RotateToRobotSpace(Quaternion rotation)
    {
        if (!invertedQuaternion)
        {
            inverseOriginQuaternion = Quaternion.Inverse(robotOriginTransform.rotation);
            invertedQuaternion = true;
        }
        return inverseOriginQuaternion * rotation;
    }

    public static bool RobotOriginAvailable() { return robotOriginAvailable; }

    // Start is called before the first frame update
    void Start()
    {
        anchorStatus = AnchorStatus.Unavailable;
        return;
        _ = anchorManager.LoadAnchor(playerPrefsAnchorKey,
            // Successful load
            (success, anchor) =>
            {
                if (success)
                {
                    // Successful localise
                    OVRSpatialAnchor anchorObject = new GameObject().AddComponent<OVRSpatialAnchor>();
                    anchor.BindTo(anchorObject);
                    robotOriginTransform = new()
                    {
                        position = anchorObject.transform.position,
                        rotation = anchorObject.transform.rotation
                    };
                    anchorStatus = AnchorStatus.Available;
                    robotOriginAvailable = true;
                    urdfModel.transform.SetPositionAndRotation(robotOriginTransform.position, robotOriginTransform.rotation);
                }
                else
                {
                    // Unsuccessful localise
                    Debug.Log("Unable to localise anchor");
                    anchorStatus = AnchorStatus.Unavailable;
                }
            },
            // Unsuccessful load
            () =>
            {
                Debug.LogWarning("Failed to load anchor");
                anchorStatus = AnchorStatus.Unavailable;
            }
        );
    }

    // Update is called once per frame
    void Update()
    {
        if (anchorStatus != AnchorStatus.Unavailable || robotOriginAvailable) return;

        if (!sceneMeshManager.PointCloudsReady())
        {
            sceneMeshManager.GetVertexGroups();
        }

        if (jointsManager.IsSet() && sceneMeshManager.PointCloudsReady())
        {
            List<Vector3> modelVertices = RetrievePoints(urdfModel);
            urdfHullVertices = new List<Vector3>(GenerateConvexHull(modelVertices).vertices);

            List<List<Vector3>> groups = sceneMeshManager.GetPointClouds();
            float minError = float.MaxValue;
            TransformData transformData = new();
            foreach (List<Vector3> group in groups)
            {
                float error = float.MaxValue;
                List<Vector3> groupHullVertices = new(GenerateConvexHull(group).vertices);
                TransformData candidate = FitModel(urdfHullVertices, groupHullVertices, ref error);
                if (error < minError)
                {
                    minError = error;
                    transformData = candidate;
                }
            }
            robotOriginTransform = transformData;
            robotOriginAvailable = true;
            urdfModel.transform.SetPositionAndRotation(robotOriginTransform.position, robotOriginTransform.rotation);
            // anchorManager.CreateAndSaveAnchor(robotOriginTransform, playerPrefsAnchorKey);
        }
    }

    List<Vector3> RetrievePoints(GameObject obj)
    {
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>(true);
        List<Vector3> points = new();
        Quaternion modelInverseRotation = Quaternion.Inverse(obj.transform.rotation);
        foreach (MeshFilter filter in meshFilters)
        {
            Vector3[] vertices = filter.sharedMesh.vertices;
            for (int i = 0; i < vertices.Count(); i++)
            {
                vertices[i] = filter.gameObject.transform.rotation * vertices[i] + filter.gameObject.transform.position;  // Get coordinates
                vertices[i] = modelInverseRotation * (vertices[i] - obj.transform.position);  // Remove parent transform
            }
            points.AddRange(vertices);
        }
        return points;
    }

    Mesh GenerateConvexHull(List<Vector3> vertices)
    {
        ConvexHullCalculator calc = new();
        List<Vector3> verts = new();
        List<int> tris = new();
        List<Vector3> normals = new();

        calc.GenerateHull(vertices, false, ref verts, ref tris, ref normals);

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(normals);
        return mesh;
    }

    TransformData FitModel(List<Vector3> moveable, List<Vector3> target, ref float error)
    {
        TransformData transformData = new();
        IterativeClosestPoint icp = new();
        Matrix4x4 transformation = icp.ICP(moveable,
                                           target,
                                           ref error,
                                           10,
                                           1e-5f,
                                           true);
        transformData.position = MatrixHelpers.ExtractTranslationFromMatrix(ref transformation);
        transformData.rotation = MatrixHelpers.ExtractRotationFromMatrix(ref transformation);

        return transformData;
    }
}