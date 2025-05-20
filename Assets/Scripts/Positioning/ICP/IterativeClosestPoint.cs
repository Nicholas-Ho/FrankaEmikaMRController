// As detailed in https://www.sci.utah.edu/~shireen/pdfs/tutorials/Elhabian_ICP09.pdf

using System.Collections.Generic;
using UnityEngine;
using SimpleQRAlgorithm;
using System;
using Oculus.Platform.Models;

public class IterativeClosestPoint
{
    enum Axes { x, y, z }
    private struct TRS {
        public Vector3 t;
        public Quaternion r;
        public Vector3 s;
        public TRS(Vector3 tr, Quaternion ro, float sc) {
            t = tr;
            r = ro;
            s = new Vector3(sc, sc, sc);
        }
        public void Blend(float blend) {
            t *= blend;
            r = Quaternion.Lerp(Quaternion.identity, r, blend);
            s = s * blend + Vector3.one * (1 - blend);
        }
    }

    public Matrix4x4 ICP(List<Vector3> points,
                         List<Vector3> target,
                         ref float error,
                         int maxIterations=200,
                         float threshold=1e-3f,
                         bool useScale=false) {

        KDTree3D kdTree = new KDTree3D(target);

        int Np = points.Count;
        Matrix4x4 transformMatrix = Matrix4x4.identity;

        List<Vector3> _points = new List<Vector3>(points);  // Allow modifications without changing original
        List<Vector3> correspondence;
        float resError = float.MaxValue;
        for (int i = 0; i < maxIterations; i++) {
            // Retrieve closest points
            correspondence = kdTree.GetNearestNeighbours(_points);

            // Find alignment
            TRS trs = FindAlignment(_points, target);
            if (!useScale) trs.s = new Vector3(1, 1, 1);  // Don't use scale if not useScale
            Matrix4x4 mat = new Matrix4x4();
            mat.SetTRS(trs.t, trs.r, trs.s);

            // Add to output
            transformMatrix = mat * transformMatrix;

            // Compute residual error. If within threshold, break
            resError = 0;
            for (int j = 0; j < Np; j++) {
                _points[j] = mat.MultiplyPoint3x4(_points[j]);
                resError += (correspondence[j] - _points[j]).sqrMagnitude; ;
            }
            if (resError / Np < threshold) break;
        }
        error = resError;
        return transformMatrix;
    }

    private TRS FindAlignment(List<Vector3> points, List<Vector3> target) {
        List<Vector3> Pprime = CentreAboutZero(points, out Vector3 Pmu);
        List<Vector3> Tprime = CentreAboutZero(target, out Vector3 Tmu);
        float Sxx = ComputeS(Pprime, Axes.x, Tprime, Axes.x);
        float Sxy = ComputeS(Pprime, Axes.x, Tprime, Axes.y);
        float Sxz = ComputeS(Pprime, Axes.x, Tprime, Axes.z);
        float Syx = ComputeS(Pprime, Axes.y, Tprime, Axes.x);
        float Syy = ComputeS(Pprime, Axes.y, Tprime, Axes.y);
        float Syz = ComputeS(Pprime, Axes.y, Tprime, Axes.z);
        float Szx = ComputeS(Pprime, Axes.z, Tprime, Axes.x);
        float Szy = ComputeS(Pprime, Axes.z, Tprime, Axes.y);
        float Szz = ComputeS(Pprime, Axes.z, Tprime, Axes.z);
        
        float[,] Nmatrix = {
            { Sxx + Syy + Szz,  Syz - Szy,          -Sxz + Szx,         Sxy - Syx       },
            { -Szy + Syz,       Sxx - Szz - Syy,    Sxy + Syx,          Sxz + Szx       },
            { Szx - Sxz,        Syx + Sxy,          Syy - Szz - Sxx,    Syz + Szy       },
            { -Syx + Sxy,       Szx + Sxz,          Szy + Syz,          Szz - Syy - Sxx }
        };

        // Compute rotation quaternion as dominant eigenvector of Nmatrix
        QRAlgorithm.Diagonalize(Nmatrix, 10, out float[,] eigenvalues, out float[,] eigenvectors);
        int domInd = -1;
        for (int i=0; i<4; i++) {
            if (eigenvalues[i,i] < 0) continue;
            if (domInd == -1 ||
                eigenvalues[i,i] > eigenvalues[domInd, domInd]) domInd = i;
        }
        if (domInd == -1) throw new Exception("No positive eigenvalues in Nmatrix");

        // Note the different order, as deduced from the layout of the rotation matrix.
        // The order is hence (w, x, y, z).
        // This is in a different order from Unity's (x, y, z, w).
        Quaternion rotation = new Quaternion(
            eigenvectors[1, domInd],
            eigenvectors[2, domInd],
            eigenvectors[3, domInd],
            eigenvectors[0, domInd]
        );
        
        // Compute scaling factor
        float PsumSqMg = 0, TsumSqMg = 0;
        for (int i=0; i<Pprime.Count; i++) PsumSqMg += Pprime[i].sqrMagnitude;
        for (int i=0; i<Tprime.Count; i++) TsumSqMg += Tprime[i].sqrMagnitude;
        float scale = (float) Math.Sqrt(TsumSqMg / PsumSqMg);

        // Compute translation
        Vector3 translation = Tmu - scale * (rotation * Pmu);

        return new TRS(translation, rotation, scale);
    }

    // Helper functions
    private Vector3 FindCentroid(List<Vector3> vecs) {
        Vector3 output = Vector3.zero;
        foreach (Vector3 vec in vecs) {
            output += vec;
        }
        return output / vecs.Count;
    }

    private List<Vector3> CentreAboutZero(List<Vector3> vecs, out Vector3 centre) {
        List<Vector3> output = new List<Vector3>();
        centre = FindCentroid(vecs);
        for (int i=0; i<vecs.Count; i++) {
            output.Add(vecs[i] - centre);
        }
        return output;
    }

    private float ComputeS(List<Vector3> a, Axes aDim, List<Vector3> b, Axes bDim) {
        float output = 0;
        if (a.Count != b.Count) throw new Exception("Both lists must be of equal length");
        for (int i=0; i<a.Count; i++) {
            output += GetVecValue(a[i], aDim) * GetVecValue(b[i], bDim);
        }
        return output;
    }

    private float GetVecValue(Vector3 vec, Axes dim) {
        if (dim == Axes.x) return vec.x;
        if (dim == Axes.y) return vec.y;
        return vec.z;
    }
}