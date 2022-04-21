using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GUIScript : MonoBehaviour
{
    private Transform m_meshTransform;

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
        if(Time.time - m_prevDisplayTime > 0.5f)
        {
            m_displayFps = m_fps;
            m_prevDisplayTime = Time.time;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(100, 100, 300, 100), m_displayFps.ToString("F1"), m_guiStyle);
        if (GUI.Button(new Rect(100, 300, 300, 100), "Click"))
            Move();
    }

    private void Move()
    {
        if (m_meshTransform == null)
            m_meshTransform = GameObject.FindObjectOfType<MeshGenerator>().transform;
        DOTween.Sequence()
            .Append(m_meshTransform.DOMoveX(50, 8))
            .Append(m_meshTransform.DOMoveX(-50, 8))
            .SetLoops(2);
    }
}
