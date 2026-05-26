using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI")]
    public Slider hpSlider;
    public Canvas canvas;

    private EnemyBase enemy;
    private Camera cam;

    void Start()
    {
        enemy = GetComponentInParent<EnemyBase>();
        cam = Camera.main;

        if (hpSlider != null && enemy != null)
        {
            hpSlider.maxValue = enemy.maxHealth;
            hpSlider.value = enemy.currentHealth;
        }
    }

    void Update()
    {
        if (enemy == null) return;

        // Оновлюємо HP бар
        if (hpSlider != null)
            hpSlider.value = enemy.currentHealth;

        // Бар завжди дивиться на камеру
        if (canvas != null && cam != null)
            canvas.transform.rotation =
                cam.transform.rotation;
    }
}