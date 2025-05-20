using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Linq;

public class SceneMeshManager : MonoBehaviour
{
    public MRUK mRUK;
    public float checkDistBuffer = 0.05f;

    private List<List<Vector3>> pointCloudGroups = new();
    private MRUKRoom room;
    private Mesh globalMesh;
    private bool globalMeshObtained = false;
    private bool pointCloudsObtained = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GetMesh()
    {
        room = mRUK.GetCurrentRoom();
        MRUKAnchor globalAnchor = room.GetGlobalMeshAnchor();
        Mesh mesh = Instantiate(globalAnchor.GlobalMesh);  // Copy global mesh

        Vector3[] newVerts = new Vector3[mesh.vertices.Count()];
        for (int j = 0; j < mesh.vertices.Count(); j++)
        {
            newVerts[j] = globalAnchor.transform.rotation * mesh.vertices[j] + globalAnchor.transform.position;
        }
        mesh.vertices = newVerts;
        globalMesh = mesh;
        globalMeshObtained = true;
    }

    public void GetVertexGroups()
    {
        if (!globalMeshObtained) GetMesh();

        HashSet<int> vertexIndices = new(Enumerable.Range(0, globalMesh.vertices.Count()));
        foreach (MRUKAnchor anchor in room.Anchors)
        {
            if (anchor.Label == MRUKAnchor.SceneLabels.GLOBAL_MESH) continue;
            if (anchor.Label == MRUKAnchor.SceneLabels.CEILING ||
                    anchor.Label == MRUKAnchor.SceneLabels.FLOOR)
            {

            }
            if (anchor.VolumeBounds.HasValue)
            {
                vertexIndices.RemoveWhere(item => anchor.IsPositionInVolume(globalMesh.vertices[item], true, checkDistBuffer));
            }
            else
            {
                vertexIndices.RemoveWhere(item => anchor.GetDistanceToSurface(globalMesh.vertices[item]) <= checkDistBuffer);
            }
        }

        // Grouping using union find
        UnionFind<int> vertUnionFind = new();
        for (int i = 0; i < globalMesh.triangles.Count(); i += 3)
        {
            int t1 = globalMesh.triangles[i], t2 = globalMesh.triangles[i + 1], t3 = globalMesh.triangles[i + 2];
            if (!vertexIndices.Contains(t1) ||
                !vertexIndices.Contains(t2) ||
                !vertexIndices.Contains(t3))
                continue;
            vertUnionFind.Union(t1, t2);
            vertUnionFind.Union(t1, t3);  // (t2, t3) is redundant
        }

        Debug.Log("Union find completed");

        Dictionary<int, List<int>> vertexGroups = new();
        foreach (int index in vertexIndices)
        {
            int root = vertUnionFind.Find(index);
            if (!vertexGroups.ContainsKey(root)) vertexGroups[root] = new List<int>();
            vertexGroups[root].Add(index);
        }

        int grpCount = 0;
        foreach (List<int> group in vertexGroups.Values)
        {
            List<Vector3> vertVecGroup = new();
            for (int j = 0; j < group.Count; j++)
            {
                int index = group[j];
                vertVecGroup.Add(globalMesh.vertices[index]);
            }
            pointCloudGroups.Add(vertVecGroup);
            grpCount++;
        }

        Debug.Log("Vertex groups obtained.");
        pointCloudsObtained = true;
    }

    public bool PointCloudsReady() { return pointCloudsObtained; }
    public List<List<Vector3>> GetPointClouds() { return pointCloudGroups; }
}