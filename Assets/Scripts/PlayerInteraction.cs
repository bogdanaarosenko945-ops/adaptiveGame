using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Радіус взаємодії")]
    public float interactRadius = 1.5f;

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
            TryInteract();
    }

    void TryInteract()
    {
        // Шукаємо лут поблизу
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, interactRadius);

        PlayerHealth health =
            GetComponent<PlayerHealth>();

        foreach (var hit in hits)
        {
            LootItem loot =
                hit.GetComponent<LootItem>();
            if (loot != null && health != null)
            {
                loot.TryCollect(health);
                return;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position, interactRadius);
    }
}