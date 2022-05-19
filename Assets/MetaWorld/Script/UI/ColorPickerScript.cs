using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HSVPicker;

public class ColorPickerScript : MonoBehaviour
{
    [SerializeField] private Button m_colorBtn;
    [SerializeField] private ColorPicker m_colorPicker;



    private void Awake()
    {
        m_colorBtn.onClick.AddListener(OnColorBtnPress);
        m_colorPicker.onValueChanged.AddListener(OnColorChange);
    }




    private void OnColorBtnPress()
    {
        m_colorPicker.gameObject.SetActive(!m_colorPicker.gameObject.activeSelf);
    }


    private void OnColorChange(Color color)
    {
        ColorBlock cb = m_colorBtn.colors;
        cb.normalColor = color;
        cb.pressedColor = color;
        cb.selectedColor = color;
        cb.highlightedColor = color;
        m_colorBtn.colors = cb;
        if (VoxelManager.MeshGenerator != null)
            VoxelManager.MeshGenerator.GetComponent<VoxelBuilder>().VoxelColor = color;
    }
}
