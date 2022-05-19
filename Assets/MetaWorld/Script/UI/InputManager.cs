using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager m_singleton = null;

    private VoxelEditInput m_voxelEditInput;
    private CameraControlInput m_camControlInput;


    public static VoxelEditInput VoxelEditInput
    {
        get { return m_singleton.m_voxelEditInput; }
    }

    public static CameraControlInput CameraControlInput
    {
        get { return m_singleton.m_camControlInput; }
    }

    private void Awake()
    {
        if (m_singleton != null)
        {
            DestroyImmediate(this);
        }
        else
        {
            m_singleton = this;
            Init();
        }
    }

    private void Start()
    {
        m_camControlInput.Enable();
        m_voxelEditInput.ShortcutToggle.Enable();
    }

    private void Init()
    {
        m_voxelEditInput = new VoxelEditInput();
        m_camControlInput = new CameraControlInput();
    }
}
