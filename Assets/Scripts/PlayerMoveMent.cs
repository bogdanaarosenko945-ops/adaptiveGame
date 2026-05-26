using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Рух")]
    public float moveSpeed = 5f;

    [Header("Посилання")]
    public PlayerTracker tracker;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed) x = -1f;
        else if (Keyboard.current.dKey.isPressed) x = 1f;

        if (Keyboard.current.sKey.isPressed) y = -1f;
        else if (Keyboard.current.wKey.isPressed) y = 1f;

        moveInput = new Vector2(x, y).normalized;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    public void OnKill(string weaponType)
    {
        if (tracker != null)
            tracker.RegisterKill(weaponType);
    }

    public void OnDeath()
    {
        if (tracker != null)
            tracker.RegisterDeath(rb.position);
        Respawn();
    }

    void Respawn()
    {
        MapGenerator map =
            Object.FindAnyObjectByType<MapGenerator>();
        if (map != null)
            transform.position = map.GetSpawnPoint();
    }
}