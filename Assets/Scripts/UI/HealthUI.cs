using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    private NetworkHealth networkHealth;

    private void Awake()
    {
        if (healthSlider == null || healthText == null)
            throw new InvalidOperationException("HealthUI requires a Slider and health text.");
    }

    private void OnEnable()
    {
        Subscribe();
        UpdateUI();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void UpdateUI()
    {
        if (networkHealth != null && networkHealth.Object != null && networkHealth.Object.IsValid)
        {
            SetValues(networkHealth.CurrentHealth, networkHealth.MaxHealth);
            return;
        }
        if (playerHealth != null)
            SetValues(playerHealth.CurHealth, playerHealth.MaxHealth);
        else
            SetValues(0f, 0f);
    }

    private void SetValues(float current, float maximum)
    {
        healthSlider.value = maximum > 0f ? current / maximum : 0f;
        healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
    }

    public void Bind(NetworkHealth target)
    {
        Unsubscribe();
        networkHealth = target;
        playerHealth = null;
        if (isActiveAndEnabled)
            Subscribe();

        UpdateUI();
    }

    private void Subscribe()
    {
        if (networkHealth != null)
            networkHealth.OnHealthChanged += UpdateUI;
        else if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateUI;
    }

    private void Unsubscribe()
    {
        if (networkHealth != null)
            networkHealth.OnHealthChanged -= UpdateUI;
        else if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateUI;
    }
}
