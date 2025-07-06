using UnityEngine;

public enum offsetType {
    Global,
    Local
}
public enum offsetAxis {
    X,
    Y,
    Z
}

public class FollowObject : MonoBehaviour
{
    [Header("Offset")]
    public bool isFollow = false; // if true, follow the anchor  
    public bool isLerp = false; // if true, use lerp to follow the anchor
    [SerializeField] private Transform m_anchor;
    [SerializeField] private Vector3 m_anchorOffset;
    [SerializeField] private float m_followSpeed = 1f;
    [SerializeField] private bool m_isLimitMinHeight = false;
    [SerializeField] private offsetType m_xOffsetType = offsetType.Global;
    [SerializeField] private offsetAxis m_xAxis = offsetAxis.X;
    [SerializeField] private offsetType m_yOffsetType = offsetType.Global;
    [SerializeField] private offsetAxis m_yAxis = offsetAxis.Y;
    [SerializeField] private offsetType m_zOffsetType = offsetType.Global;
    [SerializeField] private offsetAxis m_zAxis = offsetAxis.Z;

    [Header("Look At")]
    [SerializeField] private Transform m_lookTarget;
    public bool isLookAt = false; // if true, look at the look target
    public bool islookATFlat = false;

    void OnEnable()
    {
        if (isFollow)
        {
            FollowAnchor();
        }
    }

    private void Update()
    {
        if (isFollow)
        {
            FollowAnchor();
        }

        if (isLookAt)
        {
            LookAtTarget();
        }
    }
    
    private void FollowAnchor()
    {
        Vector3 finalOffset = Vector3.zero;

        // X offset
        if (m_xOffsetType == offsetType.Global)
        {
            finalOffset.x = m_anchorOffset.x;
        }
        else
        {
            Vector3 localAxis = GetLocalAxis(m_xAxis);
            finalOffset += localAxis * m_anchorOffset.x;
        }

        // Y offset
        if (m_yOffsetType == offsetType.Global)
        {
            finalOffset.y = m_anchorOffset.y;
        }
        else
        {
            Vector3 localAxis = GetLocalAxis(m_yAxis);
            finalOffset += localAxis * m_anchorOffset.y;
        }

        // Z offset
        if (m_zOffsetType == offsetType.Global)
        {
            finalOffset.z = m_anchorOffset.z;
        }
        else
        {
            Vector3 localAxis = GetLocalAxis(m_zAxis);
            finalOffset += localAxis * m_anchorOffset.z;
        }

        Vector3 targetPosition = m_anchor.position + finalOffset;

        if (isLerp)
        {
            Vector3 newPos = Vector3.Lerp(transform.position, targetPosition, m_followSpeed * Time.deltaTime);

            if (m_isLimitMinHeight)
            {
                newPos.y = Mathf.Max(newPos.y, targetPosition.y);
            }

            transform.position = newPos;
        }
        else
        {
            this.transform.position = targetPosition;
        }
    }

    private Vector3 GetLocalAxis(offsetAxis axis)
    {
        switch (axis)
        {
            case offsetAxis.X:
                return transform.right;
            case offsetAxis.Y:
                return transform.up;
            case offsetAxis.Z:
                return transform.forward;
            default:
                return Vector3.zero;
        }
    }

    private void LookAtTarget()
    {
        if (m_lookTarget == null)
        {
            Debug.LogWarning("FollowObject: m_lookTarget is null but isLookAt is true.");
            return;
        }

        Vector3 targetPosition = m_lookTarget.position;

        if (islookATFlat) {
            targetPosition.y = this.transform.position.y;
        }

        this.transform.LookAt(targetPosition);
    }
}
