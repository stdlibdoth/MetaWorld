using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloorObjScript m_floorObjPrefab;
    [SerializeField] private CoordinatePanelScript m_coordinatePanelScript;
    [SerializeField] private EditingCamController m_editingCamController;
    [SerializeField] private GroundPanelScript m_groundPanelScript;



    [Space]
    [Space]
    [SerializeField] private Vector3 m_navCenter;
    [SerializeField] private Vector3Int m_floorCenter;
    [SerializeField] private float m_extent;

    private FloorObjScript m_floorObj;
    private Vector3 m_voxelCenter;
    private Vector3 m_centerOffset;
    private bool m_initFlag;
    private IMeshControl m_meshControl;
    private VoxelCommandModel m_voxelCommands;

    public Vector3 NavCenter { get { return m_navCenter; } }

    private void Awake()
    {
        m_floorObj = Instantiate(m_floorObjPrefab);
        m_groundPanelScript.OnAnyToggle.AddListener(OnGroundToggle);
    }

    private void Update()
    {
        if(VoxelManager.MeshGenerator != null && !m_initFlag)
        {
            m_initFlag = true;
            LoadInitalData();
        }
    }


    private void LoadInitalData()
    {
        m_meshControl = VoxelManager.MeshGenerator.GetComponent<IMeshControl>();
        m_voxelCommands = VoxelManager.MeshGenerator.GetComponent<VoxelCommandModel>();
        m_meshControl.SetCenter(m_navCenter,false);
        m_meshControl.SetRenderExtent(m_extent);
        m_coordinatePanelScript.ResetPanel(m_navCenter);
    }

    public void OffsetNavigationCenter(Vector3 offset, bool continuous_mode)
    {
        m_centerOffset = offset;
        SetVoxelCenter(continuous_mode);
    }

    public void SetNavigationCenter(Vector3 center, bool continuous_mode)
    {
        m_navCenter = center;
        m_centerOffset = Vector3.zero;
        SetVoxelCenter(continuous_mode);
        m_editingCamController.SetCamCenter(center);
        m_floorObj.Axis = Vector3.up;
    }

    public Vector3 GroundPosistion()
    {
        return m_floorObj.transform.position;
    }

    public void SetGroundPosition(Vector3 center)
    {
        m_floorObj.transform.position = center;
    }

    public void SetGroundAxis(Vector3 axis)
    {
        m_floorObj.Axis = axis;
        string _axis = "";
        if (axis == Vector3.up)
            _axis = "z";
        else if (axis == Vector3.back)
            _axis = "y";
        else if (axis == Vector3.right)
            _axis = "x";
        m_groundPanelScript.SetTogglesWithoutNotify(_axis);
    }

    public void ToggleGroundAxis()
    {
        if (m_floorObj.Axis == Vector3.up)
        {
            m_floorObj.Axis = Vector3.right;
        }
        else if (m_floorObj.Axis == Vector3.right)
        {
            m_floorObj.Axis = Vector3.back;
        }
        else if (m_floorObj.Axis == Vector3.back)
        {
            m_floorObj.Axis = Vector3.up;
        }
    }

    private void OnGroundToggle(Vector3 v3)
    {
        SetGroundAxis(v3);
    }

    private void SetVoxelCenter(bool continuous_mode)
    {
        m_voxelCenter = m_navCenter + m_centerOffset;
        m_meshControl.SetCenter(m_voxelCenter, continuous_mode);
        m_meshControl.SetUpdateMode(continuous_mode);
        if (!continuous_mode)
        {
            m_voxelCommands.RefreshMesh(m_meshControl.GetCoordRangeMin(), m_meshControl.GetCoordRangeMax());
        }
    }
}
