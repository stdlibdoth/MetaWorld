using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GUIScript : MonoBehaviour
{

    [SerializeField] Texture m_btnTexture;

    private Transform m_meshTransform;

    private GUIStyle m_textStyle;
    private GUIStyle m_buttonStyle;

    private float m_prevTime;
    private float m_fps;
    private float m_prevDisplayTime;
    private float m_displayFps;


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
        m_textStyle = new GUIStyle(GUI.skin.label);
        m_textStyle.fontSize = 60;
        m_textStyle.normal.textColor = Color.black;
        m_buttonStyle = new GUIStyle(GUI.skin.button);
        m_buttonStyle.fontSize = 40;
        GUI.Label(new Rect(100, 100, 300, 100), m_displayFps.ToString("F1"), m_textStyle);
        //if (GUI.Button(new Rect(100, 300, 300, 100), "Clear", m_buttonStyle))
        //    VoxelManager.MeshGenerator.ClearAllChunk();
            // Move();
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
