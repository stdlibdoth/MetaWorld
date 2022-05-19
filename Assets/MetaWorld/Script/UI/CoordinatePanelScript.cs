using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CoordinatePanelScript : MonoBehaviour
{
    [SerializeField] private NavigationController m_navController;


    [Space]
    [Space]
    [Header("UI References")]
    [SerializeField] private Slider m_xSlider;
    [SerializeField] private Slider m_ySlider;
    [SerializeField] private Slider m_zSlider;
    [SerializeField] private Button m_goToBtn;

    [SerializeField] private TextMeshProUGUI m_xCoordText;
    [SerializeField] private TextMeshProUGUI m_yCoordText;
    [SerializeField] private TextMeshProUGUI m_zCoordText;

    [Space]
    [Space]
    [Header("Input Panel")]
    [SerializeField] private GameObject m_coordInputPanel;
    [SerializeField] private TMP_InputField m_xInput;
    [SerializeField] private TMP_InputField m_yInput;
    [SerializeField] private TMP_InputField m_zInput;
    [SerializeField] private Button m_okBtn;
    [SerializeField] private Button m_backBtn;


    private void Awake()
    {
        m_xSlider.onValueChanged.AddListener(OnSliderValueChange);
        m_ySlider.onValueChanged.AddListener(OnSliderValueChange);
        m_zSlider.onValueChanged.AddListener(OnSliderValueChange);
        m_xInput.onValueChanged.AddListener(OnInputValueChange);
        m_yInput.onValueChanged.AddListener(OnInputValueChange);
        m_zInput.onValueChanged.AddListener(OnInputValueChange);
        m_okBtn.onClick.AddListener(OnOkBtnPress);
        m_backBtn.onClick.AddListener(OnBackBtnPress);
        m_goToBtn.onClick.AddListener(OnGoToBtnPress);
    }

    public void ResetPanel(Vector3 pos)
    {
        m_xSlider.SetValueWithoutNotify(0);
        m_ySlider.SetValueWithoutNotify(0);
        m_zSlider.SetValueWithoutNotify(0);
        m_xCoordText.text = pos.x.ToString("f0");
        m_yCoordText.text = pos.z.ToString("f0");
        m_zCoordText.text = pos.y.ToString("f0");
    }

    private void OnSliderValueChange(float value)
    {
        Vector3 navCenter = m_navController.NavCenter;
        Vector3 offset = new Vector3(m_xSlider.value, m_zSlider.value, m_ySlider.value);
        Vector3 pos = navCenter + offset;
        m_navController.OffsetNavigationCenter(offset,true);
        m_navController.SetGroundPosition(pos);
        m_xCoordText.text = pos.x.ToString("f0");
        m_yCoordText.text = pos.z.ToString("f0");
        m_zCoordText.text = pos.y.ToString("f0");
    }

    private void OnInputValueChange(string str)
    {
        if (m_xInput.text == "" || m_yInput.text == "" || m_zInput.text == "")
            m_okBtn.interactable = false;
        else
            m_okBtn.interactable = true;
    }

    private void OnGoToBtnPress()
    {
        InputManager.CameraControlInput.Disable();
        Vector3 navCenter = m_navController.NavCenter;
        Vector3 offset = new Vector3(m_xSlider.value, m_zSlider.value, m_ySlider.value);
        Vector3 pos = navCenter + offset;
        m_coordInputPanel.gameObject.SetActive(true);
        m_xInput.text = pos.x.ToString("f0");
        m_yInput.text = pos.z.ToString("f0");
        m_zInput.text = pos.y.ToString("f0");
    }

    private void OnOkBtnPress()
    {
        Vector3 pos = new Vector3(int.Parse(m_xInput.text),
            int.Parse(m_yInput.text), int.Parse(m_zInput.text));
        m_navController.SetNavigationCenter(pos, false);
        m_navController.SetGroundPosition(pos);
        m_coordInputPanel.SetActive(false);
        ResetPanel(pos);
        InputManager.CameraControlInput.Enable();
    }

    private void OnBackBtnPress()
    {
        m_coordInputPanel.SetActive(false);
        InputManager.CameraControlInput.Enable();
    }
}
