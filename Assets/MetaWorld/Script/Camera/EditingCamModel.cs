using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem;


public class EditingCamModel : MonoBehaviour,IEditingCam
{
    [SerializeField] private CameraRig m_camRig;
    [SerializeField] private Camera m_cam;
    [SerializeField] private LayerMask m_groundLayer;


    private bool m_panMoveFlag = false;
    private Vector3 m_panInitMousePos;
    private Vector3 m_panTargetMousePos;
    private Vector3 m_panInitCamPos;
    private Vector3 m_panTargetPos;

    public void SetCenter(Vector3 posistion)
    {
        m_camRig.RootPosition = posistion;
    }

    public void Zoom(float step, MinMax range)
    {
        float dist = m_camRig.RootToJointDist;
        dist += step;
        m_camRig.RootToJointDist = math.clamp(dist, range.min, range.max);
    }

    public void SetZoom(float zoom_value)
    {
        m_camRig.RootToJointDist = zoom_value;
    }

    public void Pitch(float step, MinMax range)
    {
        float angle = m_camRig.Pitch;
        angle += step;
        m_camRig.Pitch = math.clamp(angle, range.min, range.max);
    }

    public void SetPitch(float pitch_angle)
    {
        m_camRig.Pitch = pitch_angle;
    }

    public void Rotate(float step)
    {
        m_camRig.Rotation += step;
    }

    public void SetRotation(float angle)
    {
        m_camRig.Rotation = angle;
    }

    public void PanStart()
    {
        Ray ray = m_cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 500, m_groundLayer))
        {
            m_panInitMousePos = hit.point;
            m_panInitCamPos = m_camRig.RootPosition;
        }
    }

    public void PanUpdate(Vector2 mouse_pos)
    {
        Vector3 rootPos = m_camRig.RootPosition;
        if (!m_panMoveFlag)
        {
            Ray ray = m_cam.ScreenPointToRay(mouse_pos);
            if (Physics.Raycast(ray, out RaycastHit hit, 500, m_groundLayer))
            {
                m_panTargetMousePos = hit.point;
                Vector3 delta = -m_panTargetMousePos + m_panInitMousePos;
                m_panTargetPos = m_panInitCamPos + delta;
                m_panMoveFlag = true;
            }
        }
        if (m_panMoveFlag && rootPos != m_panTargetPos)
        {
            //m_camRig.RootPosition = Vector3.MoveTowards(rootPos, m_panTargetPos, pan_speed);
            m_camRig.RootPosition = m_panTargetPos;
        }
        if (m_panMoveFlag && rootPos == m_panTargetPos)
        {
            Ray ray = m_cam.ScreenPointToRay(mouse_pos);
            if (Physics.Raycast(ray, out RaycastHit hit, 500, m_groundLayer))
            {
                m_panInitMousePos = hit.point;
                m_panInitCamPos = rootPos;
            }
            m_panMoveFlag = false;
        }
    }

    public void PanEnd()
    {
        m_panMoveFlag = false;
    }
}
