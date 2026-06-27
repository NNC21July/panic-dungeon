using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
    }

    private void OnEnable()
    {
        playerHealth.OnHealthChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDisable()
    {
        playerHealth.OnHealthChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        healthSlider.value = playerHealth.CurHealthPercent;
        healthText.text = $"{Mathf.CeilToInt(playerHealth.CurHealth)} / {Mathf.CeilToInt(playerHealth.MaxHealth)}";
    }
}