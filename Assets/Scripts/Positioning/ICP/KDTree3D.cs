#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public class KDTree3D
{

    private struct KDNode {
        public Vector3 val;
        public int alignment;  // 0, 1, 2
        public int left, right;
        public KDNode(Vector3 vec, int dim) {
            val = vec;
            alignment = dim;
            left = -1; right = -1;
        }
    }

    private NativeArray<KDNode> nodes;

    static private float GetVec3Value(Vector3 vec, int dim) {
        if (dim == 0) return vec.x;
        if (dim == 1) return vec.y;
        if (dim == 2) return vec.z;
        throw new ArgumentOutOfRangeException("Dimensions must be 0, 1 or 2.");
    }

    private int root = -1;

    public KDTree3D(List<Vector3> points) {
        if (points.Count == 0) {
            Debug.LogWarning("KDTree should have at least one point.");
            return ;
        }

        // Clone to avoid modification of original
        NativeArray<Vector3> _points = new NativeArray<Vector3>(points.ToArray(), Allocator.TempJob);

        // Build tree, initially aligned to x
        nodes = new NativeArray<KDNode>(points.Count, Allocator.Persistent);
        NativeArray<int> _root = new NativeArray<int>(1, Allocator.TempJob);
        var job = new BuildTreeJob
        {
            points =  _points,
            root = _root,
            nodes = nodes,
        };
        job.Schedule().Complete();
        root = _root[0];

        // Cleanup
        _points.Dispose();
        _root.Dispose();
    }

    ~KDTree3D()
    {
        nodes.Dispose();
    }

    private struct BuildTreeJob : IJob
    {
        public NativeArray<Vector3> points;
        [WriteOnly]
        public NativeArray<int> root;
        public NativeArray<KDNode> nodes;

        public void Execute() {
            int available = 0;
            root[0] = BuildTree(points, 0, 0, points.Length, ref available);
        }

        private int BuildTree(NativeArray<Vector3> points, int dim, int low, int high, ref int available) {
            if (high - low == 0) return -1;

            // Separates vectors into smaller and larger than median
            int k = (high+low)/2;
            Vector3 median = QuickselectKD.FindKthElement(points, low, high-1, k, dim);
            KDNode node = new KDNode(median, dim);
            int index = available++;  // Use avaiable index for node and increment

            node.left = BuildTree(points, (dim+1)%3, low, k, ref available);
            node.right = BuildTree(points, (dim+1)%3, k+1, high, ref available);

            nodes[index] = node;
            return index;  // Return index of node
        }
    }


    // Nearest Neighbour Search
    public List<Vector3> GetNearestNeighbours(List<Vector3> targets) {
        if (root == -1) {
            Debug.LogWarning("KDTree contains no points");
            return new List<Vector3>();
        }

        NativeArray<Vector3> _closestPoints = new NativeArray<Vector3>(targets.Count, Allocator.TempJob);
        NativeArray<Vector3> _targets = new NativeArray<Vector3>(targets.ToArray(), Allocator.TempJob);
        var job = new NNSearchJob
        {
            nodes = nodes,
            root = root,
            targets = _targets,
            closestPoints = _closestPoints
        };
        job.Schedule().Complete();        
        List<Vector3> closestPoints = new List<Vector3>(_closestPoints);

        _targets.Dispose();        
        _closestPoints.Dispose();

        return closestPoints;

    }

    [BurstCompile]
    private struct NNSearchJob : IJob
    {
        [ReadOnly]
        public NativeArray<KDNode> nodes;
        [ReadOnly]
        public int root;
        [ReadOnly]
        public NativeArray<Vector3> targets;
        [WriteOnly]
        public NativeArray<Vector3> closestPoints;
        public void Execute() {
            for (int i=0; i<targets.Length; i++) {
                float shortestDist = float.PositiveInfinity;
                Vector3 currentBest = Vector3.zero;
                NNSearch(root, targets[i], ref currentBest, ref shortestDist);
                closestPoints[i] = currentBest;
            }
        }


        private void NNSearch(int node, Vector3 target, ref Vector3 currentBest, ref float shortestDist) {
            if (node == -1) return ;

            float diffAlongAxis = GetVec3Value(target, nodes[node].alignment) -
                GetVec3Value(nodes[node].val, nodes[node].alignment);

            if (diffAlongAxis < 0) {
                NNSearch(nodes[node].left, target, ref currentBest, ref shortestDist);
                float currDist = Vector3.Distance(nodes[node].val, target);
                if (currDist < shortestDist) {
                    currentBest = nodes[node].val;  // If closer, set as current best
                    shortestDist = currDist;
                }

                // If the splitting hyperplane is closer than the current best, check the other side
                if (Math.Abs(diffAlongAxis) < shortestDist)
                    NNSearch(nodes[node].right, target, ref currentBest, ref shortestDist);
            } else {
                NNSearch(nodes[node].right, target, ref currentBest, ref shortestDist);
                float currDist = Vector3.Distance(nodes[node].val, target);
                if (currDist < shortestDist) currentBest = nodes[node].val;  // If closer, set as current best

                // If the splitting hyperplane is closer than the current best, check the other side
                if (Math.Abs(diffAlongAxis) < shortestDist)
                    NNSearch(nodes[node].left, target, ref currentBest, ref shortestDist);
            }
        }
    }

    // Quickselect for the KDTree class
    private struct QuickselectKD
    {
        static private int partitions(NativeArray<Vector3> list, int low, int high, int dim) {
            // Use middle as pivot to prevent worst-case performance on sorted list
            swap(list, high, (low+high)/2);

            Vector3 pivot = list[high];
            int pivotloc = low;
            for (int i=low; i<=high; i++) {
                if (GetVec3Value(list[i], dim) < GetVec3Value(pivot, dim)) {
                    swap(list, i, pivotloc);
                    pivotloc++;
                }
            }
            swap(list, high, pivotloc);
            return pivotloc;
        }

        static private void swap(NativeArray<Vector3> list, int a, int b) {
            Vector3 temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }

        static public Vector3 FindKthElement(NativeArray<Vector3> list, int low, int high, int k, int dim) {
            int partition = -1;
            while (partition != k) {
                partition = partitions(list, low, high, dim);
                if (partition < k) low = partition + 1;
                if (partition > k) high = partition - 1;
            }
            return list[partition];
        }
    }
}