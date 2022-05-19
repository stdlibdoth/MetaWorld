using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(VoxelCommandModel))]
public class VoxelBuilder : MonoBehaviour
{
    [SerializeField] private GameObject m_previewCubePrefab;
    [SerializeField] private VoxelCommandModel m_voxelCommands;
    [SerializeField] private LayerMask m_gridLayer;

    private EditingState m_editState;
    private bool m_spawnFlag;
    private bool m_deleteFlag;

    private Vector3Int m_singleEditCoord;
    private Color m_voxelColor= Color.grey;

    private GameObject m_previewCube;
    private VoxelEditInput m_voxelEditInput;
    private Camera m_cam;

    public EditingState EditState { get { return m_editState; } }
    public Color VoxelColor
    {
        get { return m_voxelColor; }
        set { m_voxelColor = value; }
    }

    private void Awake()
    {
        m_previewCube = Instantiate(m_previewCubePrefab);
        m_voxelEditInput = InputManager.VoxelEditInput;
        m_voxelEditInput.SingleEditing.Spawn.started += OnSingleSpawn;
        m_voxelEditInput.SingleEditing.Delete.started += OnSingleDelete;
        m_voxelEditInput.SingleEditing.Spawn.canceled += OnSingleSpawnEnd;
        m_voxelEditInput.SingleEditing.Delete.canceled += OnSingleDeleteEnd;
        m_voxelEditInput.SingleEditing.InputMove.performed += OnSingleInputMove;
        m_voxelEditInput.RectAreaEditing.AreaSelecting.started += OnReactAreaSelectingStart;
        m_voxelEditInput.RectAreaEditing.AreaSelecting.canceled += OnReactAreaSelectingEnd;

        m_voxelEditInput.ShortcutToggle.ToggleShortcut.performed += ToggleShortcut;
        m_voxelEditInput.ShortcutActions.EnterNavigation.performed += EnterNavigation;
        m_voxelEditInput.ShortcutActions.EnterSingleEditing.performed += EnterSingleEditing;
        m_voxelEditInput.ShortcutActions.EnterAreaEditing.performed += EnterAreaEditing;
        m_voxelEditInput.ShortcutActions.ToggleGroundPlane.performed += ToggleGroundPlane;
        m_voxelEditInput.ShortcutActions.GroundPlaneIncrement.performed += GroundPlaneIncrement;
        m_voxelEditInput.ShortcutActions.GroundPlaneDecrement.performed += GroundPlaneDecrement;
        m_voxelEditInput.ShortcutActions.AreaHeightIncrement.performed += AreaHeightIncrement;
        m_voxelEditInput.ShortcutActions.AreaHeightDecrement.performed += AreaHeightDecrement;
    }

    private void Update()
    {
        if(m_editState == EditingState.SingleEdit)
        {
            if (m_spawnFlag)
                m_voxelCommands.SetVoxelCheckEqual(m_singleEditCoord, m_voxelColor);
            else if (m_deleteFlag)
                m_voxelCommands.ClearVoxel(m_singleEditCoord);
        }
        else if(m_editState == EditingState.AreaEdit)
        {

        }
    }

    private void OnSingleDelete(InputAction.CallbackContext obj)
    {
        m_spawnFlag = false;
        m_deleteFlag = true;
        m_voxelCommands.ClearVoxel(m_singleEditCoord);
    }

    private void OnSingleDeleteEnd(InputAction.CallbackContext obj)
    {
        m_deleteFlag = false;
    }

    private void OnSingleSpawn(InputAction.CallbackContext arg)
    {
        m_deleteFlag = false;
        m_spawnFlag = true;
        m_voxelCommands.SetVoxelCheckEqual(m_singleEditCoord, m_voxelColor);
    }

    private void OnSingleSpawnEnd(InputAction.CallbackContext obj)
    {
        m_spawnFlag = false;
    }

    private void OnSingleInputMove(InputAction.CallbackContext arg)
    {
        if (m_editState != EditingState.SingleEdit)
            return;

        Vector2 mousePos = arg.ReadValue<Vector2>();
        Ray ray = m_cam.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 500, m_gridLayer))
        {
            Vector3Int coord = m_voxelCommands.GetWorldCoord(hit.point);
            m_singleEditCoord = coord;
            Vector3 pos = new Vector3(coord.x, coord.y, coord.z);
            m_previewCube.transform.position = pos * VoxelManager.voxelSize;
        }
    }

    private void ToggleShortcut(InputAction.CallbackContext obj)
    {
        if (m_voxelEditInput.ShortcutActions.enabled)
            m_voxelEditInput.ShortcutActions.Disable();
        else
            m_voxelEditInput.ShortcutActions.Enable();
    }

    private void EnterNavigation(InputAction.CallbackContext arg)
    {
        InputManager.CameraControlInput.Enable();
        m_editState = EditingState.Navigate;
        m_voxelEditInput.SingleEditing.Disable();
        m_voxelEditInput.RectAreaEditing.Disable();
        m_previewCube.SetActive(false);
    }
    private void EnterAreaEditing(InputAction.CallbackContext arg)
    {
        m_voxelEditInput.Disable();
    }

    private void EnterSingleEditing(InputAction.CallbackContext arg)
    {
        InputManager.CameraControlInput.Disable();
        m_voxelEditInput.SingleEditing.Enable();
        m_cam = Camera.main;
        m_editState = EditingState.SingleEdit;
        m_previewCube.SetActive(true);
    }
    private void ToggleGroundPlane(InputAction.CallbackContext arg)
    {
        print(arg);
    }

    private void AreaHeightDecrement(InputAction.CallbackContext arg)
    {
        print(arg);
    }

    private void AreaHeightIncrement(InputAction.CallbackContext arg)
    {
        print(arg);
    }

    private void GroundPlaneDecrement(InputAction.CallbackContext arg)
    {
        print(arg);
    }

    private void GroundPlaneIncrement(InputAction.CallbackContext arg)
    {
        print(arg);
    }



    private void OnReactAreaSelectingStart(InputAction.CallbackContext arg)
    {
        if (m_editState == EditingState.AreaEdit)
            m_editState = EditingState.AreaEditOnGoing;
    }

    private void OnReactAreaSelectingEnd(InputAction.CallbackContext arg)
    {
        if (m_editState == EditingState.AreaEditOnGoing)
            m_editState = EditingState.AreaEdit;
    }


    public void SetEditState(EditingState editingState)
    {
        switch (editingState)
        {
            case EditingState.Navigate:
                break;
            case EditingState.SingleEdit:
                break;
            case EditingState.AreaEdit:
                break;
            case EditingState.AreaEditOnGoing:
                break;
            default:
                break;
        }
        m_editState = editingState;
    }
}
