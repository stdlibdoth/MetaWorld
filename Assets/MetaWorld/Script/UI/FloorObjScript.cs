using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FloorObjScript : MonoBehaviour
{
    [SerializeField] private MeshRenderer m_meshRenderer;
    [SerializeField] private MeshFilter m_meshFilter;
    [SerializeField] private Transform m_floorTransform;
    [SerializeField] private Transform m_normalRef;
    [SerializeField] private Transform m_axis;
    [SerializeField] private float m_size;


    public Vector3 Position 
    { 
        get { return m_floorTransform.position; }
        set { m_floorTransform.position = value; }
    }


    public Vector3 Axis
    {
        get { return (m_normalRef.position - m_floorTransform.position).normalized; }
        set
        {
            m_axis.LookAt(value*int.MaxValue,Vector3.up);
        }
    }

    private void Awake()
    {
        SetScale(m_size);
    }

    public void SetSize(float size)
    {
        SetScale(size);
    }

    private void SetScale(float scale)
    {
        m_floorTransform.localScale = new Vector3(scale, m_floorTransform.localScale.y, scale);
        float xBound = m_meshFilter.mesh.bounds.size.x;
        float yBound = m_meshFilter.mesh.bounds.size.z;
        m_meshRenderer.material.mainTextureScale = new Vector2(xBound * scale, yBound * scale);
    }
}
