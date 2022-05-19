using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class EditingCamController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EditingCamModel m_editingCamModel;
    [SerializeField] private Camera m_cam;
    [SerializeField] private NavigationController m_navController;
    //[SerializeField] private InputManager m_inputManager;

    [Space]
    [Space]
    [SerializeField] private MinMax m_zoomRange;
    [SerializeField] private MinMax m_pitchRange;
    [SerializeField] private float m_zoomSpeed;
    [SerializeField] private float m_rotateSpeed;

    [SerializeField] private float m_resetRotation;
    [SerializeField] private float m_resetPitch;
    [SerializeField] private float m_resetZoom;
    [SerializeField] private Vector3 m_resetCenter;

    private CameraControlInput m_camControlInput;
    private bool m_panningFlag;
    private bool m_rotatingFlag;

    #region Mono Messege

    private void Start()
    {
        m_camControlInput = InputManager.CameraControlInput;
        m_camControlInput.EditingCam.Pan.started += OnPanningStart;
        m_camControlInput.EditingCam.Pan.canceled += OnPanningCancel;
        m_camControlInput.EditingCam.Rotate.started += OnRotatingStart;
        m_camControlInput.EditingCam.Rotate.canceled += OnRotatingCancel;
        m_camControlInput.EditingCam.InputMove.performed += OnInputMove;
        m_camControlInput.EditingCam.Zoom.performed += OnZooming;
        m_camControlInput.EditingCam.Reset.performed += OnCamReset;

        m_editingCamModel.SetPitch(m_resetPitch);
        m_editingCamModel.SetRotation(m_resetRotation);
        m_editingCamModel.SetZoom(m_resetZoom);
    }

    private void Update()
    {
        if (m_panningFlag)
        {
            Vector2 v2 = Mouse.current.position.ReadValue();
            m_editingCamModel.PanUpdate(v2);
        }
    }

    #endregion

    public void SetCamCenter(Vector3 position)
    {
        m_resetCenter = position;
        m_editingCamModel.SetCenter(m_resetCenter);
    }

    public void SetResetZoom(float zoom)
    {
        m_resetZoom = zoom;
    }

    public void SetZoomRange(MinMax range)
    {
        m_zoomRange = range;
    }

    #region InputAction Callbacks
    private void OnPanningStart(InputAction.CallbackContext arg)
    {
        if (!m_rotatingFlag)
            m_panningFlag = true;
        m_editingCamModel.PanStart();
    }

    private void OnPanningCancel(InputAction.CallbackContext arg)
    {
        m_panningFlag = false;
        m_editingCamModel.PanEnd();
    }

    private void OnRotatingStart(InputAction.CallbackContext arg)
    {
        if (!m_panningFlag)
            m_rotatingFlag = true;
    }

    private void OnRotatingCancel(InputAction.CallbackContext arg)
    {
        m_rotatingFlag = false;
    }

    private void OnInputMove(InputAction.CallbackContext arg)
    {
        if(m_rotatingFlag)
        {
            Vector2 v2 = arg.ReadValue<Vector2>();
            m_editingCamModel.Pitch(-v2.y * m_rotateSpeed, m_pitchRange);
            m_editingCamModel.Rotate(v2.x * m_rotateSpeed);
        }
    }
    private void OnZooming(InputAction.CallbackContext arg)
    {
        float val = arg.ReadValue<float>();
        float dir = -Mathf.Sign(val);
        m_editingCamModel.Zoom(m_zoomSpeed * dir, m_zoomRange);
    }

    private void OnCamReset(InputAction.CallbackContext arg)
    {
        m_editingCamModel.SetPitch(m_resetPitch);
        m_editingCamModel.SetRotation(m_resetRotation);
        m_editingCamModel.SetZoom(m_resetZoom);
        m_editingCamModel.SetCenter(m_resetCenter);
        m_navController.SetGroundAxis(Vector3.up);
    }
    #endregion
}
