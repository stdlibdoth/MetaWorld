using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


public class Vector3Event : UnityEvent<Vector3> { }


public class GroundPanelScript : MonoBehaviour
{
    [SerializeField] ToggleGroup m_toggleGroup;
    [SerializeField] Toggle m_xToggle;
    [SerializeField] Toggle m_yToggle;
    [SerializeField] Toggle m_zToggle;

    private Vector3Event m_onAnyToggle;

    public Vector3Event OnAnyToggle 
    {
        get
        {
            if (m_onAnyToggle == null)
                m_onAnyToggle = new Vector3Event();
            return m_onAnyToggle;
        }
    }

    private void Awake()
    {
        m_onAnyToggle = new Vector3Event();
        m_xToggle.onValueChanged.AddListener(OnXToggle);
        m_yToggle.onValueChanged.AddListener(OnYToggle);
        m_zToggle.onValueChanged.AddListener(OnZToggle);
    }

    public void SetTogglesWithoutNotify(string toggle)
    {
        if (toggle == "x")
            m_xToggle.SetIsOnWithoutNotify(true);
        else if (toggle == "y")
            m_yToggle.SetIsOnWithoutNotify(true);
        else if (toggle == "z")
            m_zToggle.SetIsOnWithoutNotify(true);
    }

    private void OnXToggle(bool val)
    {
        if (val == true)
            m_onAnyToggle.Invoke(Vector3.right);
    }

    private void OnYToggle(bool val)
    {
        if (val == true)
            m_onAnyToggle.Invoke(Vector3.back);
    }

    private void OnZToggle(bool val)
    {
        if (val == true)
            m_onAnyToggle.Invoke(Vector3.up);
    }
}
