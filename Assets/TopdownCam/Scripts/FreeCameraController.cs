using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownCam
{

    public class FreeCameraController : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private FreeCameraRig m_camRig;
        [SerializeField] private Camera m_camera;

        [Header("Param")]
        //Pan
        [SerializeField] private float m_panSpeed;
        private bool m_panFlag;
        private bool m_panMoveFlag;
        private Vector3 m_panInitMousePos;
        private Vector3 m_panTargetMousePos;
        private Vector3 m_panInitCamPos;
        private Vector3 m_panTargetPos;

        //Zoom
        [SerializeField] private float m_zoomSpeed;
        [SerializeField] private MinMax m_camSizeLimit;

        //Rotate
        [SerializeField] private float m_rotateSpeed;
        [SerializeField] private LayerMask m_groundLayer;
        private Vector3 m_groundNormal = Vector3.up;
        private bool m_rotateFlag;

        //Scroll
        [SerializeField] private float m_scrollSpeed;
        [SerializeField] private int m_screenEdgeWidth;
        private bool m_scrollFlag;

        //Mouse Position Buffer
        private List<Vector3> m_mousePosBuffer = new List<Vector3>();
        private const int m_mouseBufferSize = 5;
        private bool m_mouseBufferFlag;


        [Header("Control")]
        public bool camControlEnabled;
        public bool camScrollEnabled;


        private void Start()
        {
        }

        private void Update()
        {
            if (camControlEnabled)
            {
                UpdateMouseBuffer();
                UpdateZoom();
                UpdatePan();
                UpdateRotate();
                UpdateScroll();
            }
        }

        private void UpdateMouseBuffer()
        {
            if (!m_mouseBufferFlag)
                return;
            if (m_scrollFlag || m_panFlag)
            {
                Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 2 * m_camRig.CamHeight / Mathf.Sin(m_camRig.CamPitch * Mathf.Deg2Rad), m_groundLayer))
                    m_mousePosBuffer.Add(hit.point);
            }
            else if (m_rotateFlag)
                m_mousePosBuffer.Add(Input.mousePosition);
            if (m_mousePosBuffer.Count > m_mouseBufferSize)
                m_mousePosBuffer.RemoveAt(0);
        }
        public void UpdateZoom()
        {
            float delta = Input.mouseScrollDelta.y;
            float size = m_camera.orthographicSize - delta * m_zoomSpeed * Time.deltaTime;
            m_camera.orthographicSize = Mathf.Clamp(size, m_camSizeLimit.min, m_camSizeLimit.max);
        }
        public void UpdateScroll()
        {
            if (!camScrollEnabled || m_panFlag || m_rotateFlag)
                return;

            Vector2 v = Input.mousePosition.XY() - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            int halfWidth = Screen.width / 2 - m_screenEdgeWidth;
            int halfHeight = Screen.height / 2 - m_screenEdgeWidth;
            Vector2 screenDir = new Vector2((int)v.x / halfWidth, (int)v.y / halfHeight);
            Vector3 dir = m_camRig.transform.forward * screenDir.y + m_camRig.transform.right * screenDir.x;
            m_scrollFlag = screenDir.sqrMagnitude > 0;
            float speed = m_camera.orthographicSize * Time.deltaTime * m_scrollSpeed;
            m_camRig.RootPosition += (speed * dir);
        }
        public void UpdatePan()
        {
            if (m_rotateFlag || m_scrollFlag)
                return;

            if (Input.GetMouseButtonDown(2) && !m_panFlag)
            {
                m_panFlag = true;
                Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 2 * m_camRig.CamHeight / Mathf.Sin(m_camRig.CamPitch * Mathf.Deg2Rad), m_groundLayer))
                {
                    m_panInitMousePos = hit.point;
                    m_panInitCamPos = m_camRig.RootPosition;
                }
            }
            else if (Input.GetMouseButton(2) && m_panFlag)
            {
                if (!m_panMoveFlag)
                {
                    Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 2 * m_camRig.CamHeight / Mathf.Sin(m_camRig.CamPitch * Mathf.Deg2Rad), m_groundLayer))
                    {
                        m_panTargetMousePos = hit.point;
                        Vector3 delta = -m_panTargetMousePos + m_panInitMousePos;
                        m_panTargetPos = m_panInitCamPos + delta;
                        m_panMoveFlag = true;
                    }
                }
                if (m_panMoveFlag && m_camRig.RootPosition != m_panTargetPos)
                {
                    m_camRig.RootPosition = Vector3.MoveTowards(m_camRig.RootPosition, m_panTargetPos, m_panSpeed);
                }
                if (m_panMoveFlag && m_camRig.RootPosition == m_panTargetPos)
                {
                    Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 2 * m_camRig.CamHeight / Mathf.Sin(m_camRig.CamPitch * Mathf.Deg2Rad), m_groundLayer))
                    {
                        m_panInitMousePos = hit.point;
                        m_panInitCamPos = m_camRig.RootPosition;
                    }
                    m_panMoveFlag = false;
                }
            }
            else if (!Input.GetMouseButton(2) && m_panFlag)
            {
                m_panFlag = false;
                m_mousePosBuffer.Clear();
                m_panMoveFlag = false;
            }
        }
        public void UpdateRotate()
        {
            if (m_panFlag || m_scrollFlag)
                return;
            if (Input.GetMouseButtonDown(1) && !m_rotateFlag)
            {
                m_rotateFlag = true;
                m_mouseBufferFlag = true;
            }
            else if (Input.GetMouseButton(1) && m_rotateFlag)
            {
                if (m_mousePosBuffer.Count == m_mouseBufferSize)
                {
                    float delta = Vector3.Distance(m_mousePosBuffer[m_mousePosBuffer.Count - 1], m_mousePosBuffer[0]);
                    float speed = delta * m_rotateSpeed * Time.deltaTime / m_mouseBufferSize;

                    Vector2 mouseDir = (m_mousePosBuffer[m_mousePosBuffer.Count - 1] - m_mousePosBuffer[0]).normalized;
                    if (Mathf.Abs(mouseDir.y) < Mathf.Abs(mouseDir.x))
                    {
                        float yaw = m_camRig.RootYaw + Mathf.Sign(mouseDir.x) * speed;
                        m_camRig.RootYaw = yaw % 360;
                    }
                    else if (Mathf.Abs(mouseDir.y) > Mathf.Abs(mouseDir.x))
                    {
                        float pitch = m_camRig.CamPitch - Mathf.Sign(mouseDir.y) * speed;
                        m_camRig.CamPitch = pitch % 360;
                    }
                }

            }
            else if (!Input.GetMouseButton(1) && m_rotateFlag)
            {
                m_rotateFlag = false;
                m_mouseBufferFlag = false;
                m_mousePosBuffer.Clear();
            }
        }
    }
}
