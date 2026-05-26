using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [Header("Camera")]
    public Camera gameCamera;

    [Header("View settings")]
    public float overviewSize = 35f;
    public float gameSize = 10f;

    [Header("Smoothing")]
    public float smoothSpeed = 5f;
    public float zoomSmoothSpeed = 6f;
    public bool clampToMapBounds = true;

    private Camera controlledCamera;
    private bool isOverview;
    private Transform player;
    private float targetCameraSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureCameraController();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCameraController();
    }

    static void EnsureCameraController()
    {
        if (FindAnyObjectByType<CameraController>() != null)
            return;

        Camera main = Camera.main;
        if (main == null)
            return;

        CameraController controller =
            main.gameObject.AddComponent<CameraController>();
        controller.gameCamera = main;
    }

    void Awake()
    {
        controlledCamera = gameCamera != null
            ? gameCamera
            : GetComponent<Camera>();

        if (controlledCamera == null)
            controlledCamera = Camera.main;

        if (controlledCamera == null)
        {
            enabled = false;
            return;
        }

        targetCameraSize = gameSize;
    }

    void Start()
    {
        FindPlayer();
        SetGameMode();
    }

    void Update()
    {
        if (controlledCamera == null)
            return;

        if (player == null)
            FindPlayer();

        if (Keyboard.current != null &&
            Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleCamera();
        }

        controlledCamera.orthographicSize = Mathf.Lerp(
            controlledCamera.orthographicSize,
            targetCameraSize,
            zoomSmoothSpeed * Time.deltaTime);

        if (!isOverview && player != null)
        {
            Vector3 target = new Vector3(
                player.position.x,
                player.position.y,
                controlledCamera.transform.position.z);
            target = ClampCameraPosition(target);

            controlledCamera.transform.position = Vector3.Lerp(
                controlledCamera.transform.position,
                target,
                smoothSpeed * Time.deltaTime);
        }
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    void ToggleCamera()
    {
        isOverview = !isOverview;

        if (isOverview)
            SetOverviewMode();
        else
            SetGameMode();
    }

    void SetOverviewMode()
    {
        MapGenerator map = GameManager.Instance != null
            ? GameManager.Instance.mapGenerator
            : FindAnyObjectByType<MapGenerator>();

        if (map != null && map.width > 0 && map.height > 0)
        {
            controlledCamera.transform.position = new Vector3(
                map.width / 2f,
                map.height / 2f,
                controlledCamera.transform.position.z);

            float aspect = controlledCamera.aspect;
            float sizeByHeight = map.height * 0.55f;
            float sizeByWidth = map.width / aspect * 0.55f;
            targetCameraSize = Mathf.Max(
                overviewSize,
                sizeByHeight,
                sizeByWidth);
        }
        else
        {
            targetCameraSize = overviewSize;
        }

        Debug.Log("Огляд карти: Tab повертає ігрову камеру.");
    }

    void SetGameMode()
    {
        targetCameraSize = gameSize;
        Debug.Log("Ігровий режим камери.");
    }

    Vector3 ClampCameraPosition(Vector3 target)
    {
        if (!clampToMapBounds)
            return target;

        MapGenerator map = GameManager.Instance != null
            ? GameManager.Instance.mapGenerator
            : FindAnyObjectByType<MapGenerator>();

        if (map == null || map.width <= 0 || map.height <= 0)
            return target;

        float vertical = controlledCamera.orthographicSize;
        float horizontal = vertical * controlledCamera.aspect;

        float minX = horizontal;
        float maxX = map.width - horizontal;
        float minY = vertical;
        float maxY = map.height - vertical;

        target.x = minX <= maxX
            ? Mathf.Clamp(target.x, minX, maxX)
            : map.width / 2f;
        target.y = minY <= maxY
            ? Mathf.Clamp(target.y, minY, maxY)
            : map.height / 2f;

        return target;
    }
}
