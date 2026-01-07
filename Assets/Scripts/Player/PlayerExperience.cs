using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    int _currentLevel = 1;
    float _currentXP = 0f;
    float _requiredXP = 100f;
    int _maxLevel = 10;

    void Start()
    {
        GameUI.Instance.SetLevelProgress(_currentXP, _requiredXP, _currentLevel);
    }

    public void GainXP(float amount)
    {
        if (_currentLevel >= _maxLevel) return;

        _currentXP += amount;

        while (_currentXP >= _requiredXP && _currentLevel < _maxLevel)
        {
            _currentXP -= _requiredXP;
            LevelUp();
        }

        GameUI.Instance.SetLevelProgress(_currentXP, _requiredXP, _currentLevel);
    }

    void LevelUp()
    {
        _currentLevel++;
        _requiredXP *= 1.25f; //Siguiente nivel mas costoso

        //Mejora de stats
        PlayerStats.baseDamage *= 1.05f;
        PlayerStats.baseSpeed *= 1.03f;
        PlayerStats.maxHealth *= 1.1f;
        PlayerStats.health = PlayerStats.maxHealth;
        GameUI.Instance.healthBar.maxValue = PlayerStats.maxHealth;
        GameUI.Instance.UpdateHealthBar(PlayerStats.health);

        Debug.Log("Subiste al nivel " + _currentLevel);
    }

    public void LevelDown() //Se llama al morir con la esfera
    {
        if (_currentLevel <= 1) return;
        _currentLevel--;
        _requiredXP /= 1.25f;

        // Reducción de stats (inverso del LevelUp)
        PlayerStats.baseDamage /= 1.05f;
        PlayerStats.baseSpeed /= 1.03f;
        PlayerStats.maxHealth /= 1.1f;

        // Ajustar salud actual y UI
        PlayerStats.health = Mathf.Min(PlayerStats.health, PlayerStats.maxHealth);
        GameUI.Instance.healthBar.maxValue = PlayerStats.maxHealth;
        GameUI.Instance.UpdateHealthBar(PlayerStats.health);
        GameUI.Instance.SetLevelProgress(_currentXP, _requiredXP, _currentLevel);

        Debug.Log("Bajaste al nivel " + _currentLevel);
    }
}
