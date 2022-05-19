using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [SerializeField] private Transform m_root;
    [SerializeField] private Transform m_joint;
    [SerializeField] private Transform m_camTrans;

    private float m_jointPitch;
    private float m_rootToJointDist;
    private float m_rotation;

    public Vector3 RootPosition
    {
        get { return m_root.position; }
        set { m_root.position = value; }
    }

    public float RootToJointDist
    {
        get { return m_rootToJointDist; }
        set { m_rootToJointDist = value; }
    }

    public float Pitch
    {
        get { return m_jointPitch; }
        set { m_jointPitch = value; }
    }

    public float Rotation
    {
        get { return m_rotation; }
        set { m_rotation = value; }
    }

    private void Update()
    {
        //pitch
        float z = m_rootToJointDist * Unity.Mathematics.math.cos(m_jointPitch * Mathf.Deg2Rad);
        float y = m_rootToJointDist * Unity.Mathematics.math.sin(m_jointPitch * Mathf.Deg2Rad);
        m_joint.localPosition = new Vector3(0, y, -z);


        //rotation
        m_root.localEulerAngles = new Vector3(0, m_rotation, 0);

        m_camTrans.LookAt(m_root);

    }
}
