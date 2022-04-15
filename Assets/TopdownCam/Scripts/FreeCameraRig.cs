using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownCam
{
    //[ExecuteInEditMode]
    public class FreeCameraRig : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform m_rootHolder;
        [SerializeField] private Transform m_RigHolder;
        [SerializeField] private Transform m_camHolder;
        [SerializeField] private Camera m_camera;

        [Header("Param")]
        [SerializeField] private MinMax m_pitchLimit;
        [SerializeField] private float m_camPitch;
        [SerializeField] private float m_cam2JointOffset;
        [SerializeField] private float m_camHeight;
        public float RootYaw
        {
            get
            {
                return m_rootHolder.localEulerAngles.y;
            }
            set
            {
                m_rootHolder.localEulerAngles = m_rootHolder.localEulerAngles.SetY(value);
            }
        }
        public Vector3 RootPosition
        {
            get
            {
                return m_rootHolder.position;
            }
            set
            {
                m_rootHolder.position = value;
            }
        }
        public float CamHeight
        {
            get
            {
                return m_camHolder.position.y;
            }
            set
            {
                float minHeight = m_camera.orthographicSize * Mathf.Cos(m_camPitch * Mathf.Deg2Rad) + 0.2f;
                m_camHeight = Mathf.Clamp(m_camHeight, minHeight, float.MaxValue);
            }
        }



        public float CamJointOffset
        {
            get
            {
                return Mathf.Abs(m_camHolder.localPosition.z);
            }
            private set
            {
                m_cam2JointOffset = Mathf.Clamp(value, 0, Vector3.Distance(m_camHolder.position, m_rootHolder.position));
                m_camHolder.localPosition = new Vector3(0, 0, -m_cam2JointOffset);
            }
        }
        public Vector3 JointWorldPos
        {
            get
            {
                return m_RigHolder.position;
            }
        }
        public float CamPitch
        {
            get
            {
                return m_RigHolder.localEulerAngles.x;
            }
            set
            {
                m_camPitch = Mathf.Clamp(value, m_pitchLimit.min, m_pitchLimit.max);
                m_RigHolder.localEulerAngles = Vector3.right * m_camPitch;
            }
        }


        private void Awake()
        {
            CamJointOffset = m_cam2JointOffset;
        }

        private void LateUpdate()
        {
            UpdateRigParametres();
            AimTarget(m_rootHolder.position);
        }

        private void UpdateRigParametres()
        {
            CamJointOffset = m_cam2JointOffset;
            CamHeight = m_camHeight;
            CamPitch = m_camPitch;
        }


        //aim at the target position based on camera angle and height
        private void AimTarget(Vector3 targetPos)
        {
            m_rootHolder.position = targetPos;
            float rad = m_camPitch * Mathf.Deg2Rad;

            //********************
            //Solve equations for L
            //Mathf.Sin(m_camGroundAngle * Mathf.Deg2Rad) = y / L
            //y / m_camHeight = L / (L + m_cam2JointOffset)
            //********************

            float root2Joint = m_camHeight / Mathf.Sin(rad) - m_cam2JointOffset;
            m_RigHolder.localPosition = new Vector3(0, root2Joint * Mathf.Sin(rad), -root2Joint * Mathf.Cos(rad));
        }
    }
}