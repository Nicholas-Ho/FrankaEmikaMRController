using Oculus.Interaction;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

public class GrabFreeTransformerEventBool : MonoBehaviour, ITransformer
{
    private struct GrabPointDelta
    {
        private const float _epsilon = 1E-06f;

        public Vector3 PrevCentroidOffset { get; private set; }

        public Vector3 CentroidOffset { get; private set; }

        public Quaternion PrevRotation { get; private set; }

        public Quaternion Rotation { get; private set; }

        public GrabPointDelta(Vector3 centroidOffset, Quaternion rotation)
        {
            Vector3 prevCentroidOffset = (CentroidOffset = centroidOffset);
            PrevCentroidOffset = prevCentroidOffset;
            Quaternion prevRotation = (Rotation = rotation);
            PrevRotation = prevRotation;
        }

        public void UpdateData(Vector3 centroidOffset, Quaternion rotation)
        {
            PrevCentroidOffset = CentroidOffset;
            CentroidOffset = centroidOffset;
            PrevRotation = Rotation;
            if (Quaternion.Dot(rotation, Rotation) < 0f)
            {
                rotation.x = 0f - rotation.x;
                rotation.y = 0f - rotation.y;
                rotation.z = 0f - rotation.z;
                rotation.w = 0f - rotation.w;
            }

            Rotation = rotation;
        }

        public bool IsValidAxis()
        {
            return CentroidOffset.sqrMagnitude > 1E-06f;
        }
    }

    [SerializeField]
    [Tooltip("Constrains the position of the object along different axes. Units are meters.")]
    private TransformerUtils.PositionConstraints _positionConstraints = new TransformerUtils.PositionConstraints
    {
        XAxis = default(TransformerUtils.ConstrainedAxis),
        YAxis = default(TransformerUtils.ConstrainedAxis),
        ZAxis = default(TransformerUtils.ConstrainedAxis)
    };

    [SerializeField]
    [Tooltip("Constrains the rotation of the object along different axes. Units are degrees.")]
    private TransformerUtils.RotationConstraints _rotationConstraints = new TransformerUtils.RotationConstraints
    {
        XAxis = default(TransformerUtils.ConstrainedAxis),
        YAxis = default(TransformerUtils.ConstrainedAxis),
        ZAxis = default(TransformerUtils.ConstrainedAxis)
    };

    [SerializeField]
    [Tooltip("Constrains the local scale of the object along different axes. Expressed as a scale factor.")]
    private TransformerUtils.ScaleConstraints _scaleConstraints = new TransformerUtils.ScaleConstraints
    {
        ConstraintsAreRelative = true,
        XAxis = new TransformerUtils.ConstrainedAxis
        {
            ConstrainAxis = true,
            AxisRange = new TransformerUtils.FloatRange
            {
                Min = 1f,
                Max = 1f
            }
        },
        YAxis = new TransformerUtils.ConstrainedAxis
        {
            ConstrainAxis = true,
            AxisRange = new TransformerUtils.FloatRange
            {
                Min = 1f,
                Max = 1f
            }
        },
        ZAxis = new TransformerUtils.ConstrainedAxis
        {
            ConstrainAxis = true,
            AxisRange = new TransformerUtils.FloatRange
            {
                Min = 1f,
                Max = 1f
            }
        }
    };

    private IGrabbable _grabbable;

    private Pose _grabDeltaInLocalSpace;

    private TransformerUtils.PositionConstraints _relativePositionConstraints;
    private TransformerUtils.RotationConstraints _relativeRotationConstraints;

    private TransformerUtils.ScaleConstraints _relativeScaleConstraints;

    private Quaternion _lastRotation = Quaternion.identity;

    private Vector3 _lastScale = Vector3.one;

    private GrabPointDelta[] _deltas;
    private bool grabbed = false;

    public void Initialize(IGrabbable grabbable)
    {
        _grabbable = grabbable;
        _relativePositionConstraints = TransformerUtils.GenerateParentConstraints(_positionConstraints, _grabbable.Transform.localPosition);
        _relativeRotationConstraints = GenerateParentConstraints(_rotationConstraints, _grabbable.Transform.localRotation);
        _relativeScaleConstraints = TransformerUtils.GenerateParentConstraints(_scaleConstraints, _grabbable.Transform.localScale);
    }

    public void BeginTransform()
    {
        int count = _grabbable.GrabPoints.Count;
        Vector3 centroid = GetCentroid(_grabbable.GrabPoints);
        _deltas = ArrayPool<GrabPointDelta>.Shared.Rent(count);
        for (int i = 0; i < count; i++)
        {
            Vector3 centroidOffset = GetCentroidOffset(_grabbable.GrabPoints[i], centroid);
            _deltas[i] = new GrabPointDelta(centroidOffset, _grabbable.GrabPoints[i].rotation);
        }

        Transform transform = _grabbable.Transform;
        _grabDeltaInLocalSpace = new Pose(transform.InverseTransformVector(centroid - transform.position), transform.rotation);
        _lastRotation = Quaternion.identity;
        _lastScale = transform.localScale;

        // Refresh constraint's initial position
        _relativePositionConstraints = TransformerUtils.GenerateParentConstraints(_positionConstraints, _grabbable.Transform.localPosition);
        _relativeRotationConstraints = GenerateParentConstraints(_rotationConstraints, _grabbable.Transform.localRotation);

        grabbed = true;
    }

    public void UpdateTransform()
    {
        int count = _grabbable.GrabPoints.Count;
        Transform transform = _grabbable.Transform;
        Vector3 vector = UpdateTransformerPointData(_grabbable.GrabPoints);
        _lastScale = ((count <= 1) ? transform.localScale : (UpdateScale(count) * _lastScale));
        transform.localScale = TransformerUtils.GetConstrainedTransformScale(_lastScale, _relativeScaleConstraints);
        _lastRotation = UpdateRotation(count) * _lastRotation;
        Quaternion unconstrainedRotation = _lastRotation * _grabDeltaInLocalSpace.rotation;
        transform.rotation = TransformerUtils.GetConstrainedTransformRotation(unconstrainedRotation, _relativeRotationConstraints, transform.parent);
        Vector3 unconstrainedPosition = vector - transform.TransformVector(_grabDeltaInLocalSpace.position);
        transform.position = TransformerUtils.GetConstrainedTransformPosition(unconstrainedPosition, _relativePositionConstraints, transform.parent);
    }

    public void EndTransform()
    {
        ArrayPool<GrabPointDelta>.Shared.Return(_deltas);
        _deltas = null;

        grabbed = false;
    }

    private Vector3 UpdateTransformerPointData(List<Pose> poses)
    {
        Vector3 centroid = GetCentroid(poses);
        for (int i = 0; i < poses.Count; i++)
        {
            Vector3 centroidOffset = GetCentroidOffset(poses[i], centroid);
            _deltas[i].UpdateData(centroidOffset, poses[i].rotation);
        }

        return centroid;
    }

    private Vector3 GetCentroid(List<Pose> poses)
    {
        int count = poses.Count;
        Vector3 zero = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            zero += poses[i].position;
        }

        return zero / count;
    }

    private Vector3 GetCentroidOffset(Pose pose, Vector3 centre)
    {
        return centre - pose.position;
    }

    private Quaternion UpdateRotation(int count)
    {
        Quaternion quaternion = Quaternion.identity;
        float t = 1f / (float)count;
        for (int i = 0; i < count; i++)
        {
            GrabPointDelta grabPointDelta = _deltas[i];
            Quaternion b = grabPointDelta.Rotation * Quaternion.Inverse(grabPointDelta.PrevRotation);
            if (grabPointDelta.IsValidAxis())
            {
                Vector3 normalized = grabPointDelta.CentroidOffset.normalized;
                Quaternion b2 = Quaternion.FromToRotation(grabPointDelta.PrevCentroidOffset.normalized, normalized);
                quaternion = Quaternion.Slerp(Quaternion.identity, b2, t) * quaternion;
                b.ToAngleAxis(out var angle, out var axis);
                float num = Vector3.Dot(axis, normalized);
                b = Quaternion.AngleAxis(angle * num, normalized);
            }

            quaternion = Quaternion.Slerp(Quaternion.identity, b, t) * quaternion;
        }

        return quaternion;
    }

    private float UpdateScale(int count)
    {
        float num = 0f;
        for (int i = 0; i < count; i++)
        {
            GrabPointDelta grabPointDelta = _deltas[i];
            if (grabPointDelta.IsValidAxis())
            {
                float num2 = Mathf.Sqrt(grabPointDelta.CentroidOffset.sqrMagnitude / grabPointDelta.PrevCentroidOffset.sqrMagnitude);
                num += num2 / (float)count;
            }
            else
            {
                num += 1f / (float)count;
            }
        }

        return num;
    }

    public void InjectOptionalPositionConstraints(TransformerUtils.PositionConstraints constraints)
    {
        _positionConstraints = constraints;
    }

    public void InjectOptionalRotationConstraints(TransformerUtils.RotationConstraints constraints)
    {
        _rotationConstraints = constraints;
    }

    public void InjectOptionalScaleConstraints(TransformerUtils.ScaleConstraints constraints)
    {
        _scaleConstraints = constraints;
    }

    TransformerUtils.RotationConstraints GenerateParentConstraints(TransformerUtils.RotationConstraints constraints, Quaternion initialRotation)
{
    TransformerUtils.RotationConstraints rotationConstraints;
    rotationConstraints = new TransformerUtils.RotationConstraints();
    rotationConstraints.XAxis = default(TransformerUtils.ConstrainedAxis);
    rotationConstraints.YAxis = default(TransformerUtils.ConstrainedAxis);
    rotationConstraints.ZAxis = default(TransformerUtils.ConstrainedAxis);
    Vector3 eulerAngles = initialRotation.eulerAngles;
    if (constraints.XAxis.ConstrainAxis)
    {
        rotationConstraints.XAxis.ConstrainAxis = true;
        rotationConstraints.XAxis.AxisRange.Min = constraints.XAxis.AxisRange.Min * eulerAngles.x;
        rotationConstraints.XAxis.AxisRange.Max = constraints.XAxis.AxisRange.Max * eulerAngles.x;
    }

    if (constraints.YAxis.ConstrainAxis)
    {
        rotationConstraints.YAxis.ConstrainAxis = true;
        rotationConstraints.YAxis.AxisRange.Min = constraints.YAxis.AxisRange.Min * eulerAngles.y;
        rotationConstraints.YAxis.AxisRange.Max = constraints.YAxis.AxisRange.Max * eulerAngles.y;
    }

    if (constraints.ZAxis.ConstrainAxis)
    {
        rotationConstraints.ZAxis.ConstrainAxis = true;
        rotationConstraints.ZAxis.AxisRange.Min = constraints.ZAxis.AxisRange.Min * eulerAngles.z;
        rotationConstraints.ZAxis.AxisRange.Max = constraints.ZAxis.AxisRange.Max * eulerAngles.z;
    }

    return rotationConstraints;
}

    public bool isGrabbed() { return grabbed; }
}