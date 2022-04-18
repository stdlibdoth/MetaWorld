using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIScript : MonoBehaviour
{
    private GUIStyle m_guiStyle;

    private float m_prevTime;
    private float m_fps;
    private float m_prevDisplayTime;
    private float m_displayFps;

    private void Awake()
    {
        m_guiStyle = new GUIStyle();
        m_guiStyle.fontSize = 60;
    }

    private void Update()
    {
        m_fps = 1 / (Time.time - m_prevTime);
        m_prevTime = Time.time;
        if(Time.time - m_prevDisplayTime > 1)
        {
            m_displayFps = m_fps;
            m_prevDisplayTime = Time.time;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(100, 100, 300, 100), m_displayFps.ToString("F1"), m_guiStyle);
    }
}
