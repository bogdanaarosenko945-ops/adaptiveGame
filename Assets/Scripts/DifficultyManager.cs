using UnityEngine;
using TMPro;

public enum Difficulty { Easy, Hard }

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Current difficulty")]
    public Difficulty currentDifficulty =
        Difficulty.Easy;

    [Header("Optional UI")]
    public GameObject difficultyPanel;
    public TextMeshProUGUI difficultyText;

    public float GetFillModifier()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return -8f;
            case Difficulty.Hard: return 8f;
            default: return 0f;
        }
    }

    public float GetEnemyModifier()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return 0.5f;
            case Difficulty.Hard: return 1.5f;
            default: return 1f;
        }
    }

    public float GetLootModifier()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return 1.3f;
            case Difficulty.Hard: return 0.75f;
            default: return 1f;
        }
    }

    public float GetSmoothModifier()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return 2f;
            case Difficulty.Hard: return -2f;
            default: return 0f;
        }
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (PlayerPrefs.HasKey("SelectedDifficulty"))
        {
            int saved = PlayerPrefs.GetInt(
                "SelectedDifficulty",
                (int)Difficulty.Easy);
            currentDifficulty =
                saved <= 0 ? Difficulty.Easy : Difficulty.Hard;
        }

        UpdateUI();
    }

    public void SetEasy()
    {
        SetDifficulty(Difficulty.Easy);
    }

    public void SetHard()
    {
        SetDifficulty(Difficulty.Hard);
    }

    public void SetDifficulty(Difficulty difficulty)
    {
        currentDifficulty = difficulty;
        PlayerPrefs.SetInt(
            "SelectedDifficulty",
            (int)currentDifficulty);
        PlayerPrefs.Save();
        UpdateUI();
        Debug.Log("Difficulty: " + currentDifficulty);
    }

    void UpdateUI()
    {
        if (difficultyText == null) return;

        switch (currentDifficulty)
        {
            case Difficulty.Easy:
                difficultyText.text = "Easy";
                difficultyText.color = Color.green;
                break;
            case Difficulty.Hard:
                difficultyText.text = "Hard";
                difficultyText.color = Color.red;
                break;
        }
    }
}
