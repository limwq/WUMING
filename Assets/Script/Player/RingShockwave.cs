using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RingShockwave : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private float expansionSpeed = 10f; // How fast it grows
    [SerializeField] private float startWidth = 0.5f;      // Thickness of the ring
    [SerializeField] private float fadeSpeed = 5f;       // How fast it disappears
    [SerializeField] private int segments = 50;          // Smoothness of the circle

    private LineRenderer lineRen;
    private float currentRadius = 0.5f;
    private float currentAlpha = 1.0f;
    private Color baseColor;

    void Awake() {
        lineRen = GetComponent<LineRenderer>();
        lineRen.positionCount = segments;
        lineRen.useWorldSpace = false; // Moves with the object

        // Setup initial color/width
        baseColor = lineRen.startColor;
        lineRen.startWidth = startWidth;
        lineRen.endWidth = startWidth;
    }

    void Update() {
        // 1. Expand Radius
        currentRadius += expansionSpeed * Time.deltaTime;
        DrawCircle();

        // 2. Fade Out
        currentAlpha -= fadeSpeed * Time.deltaTime;

        // Apply Fade to Color
        Color newColor = baseColor;
        newColor.a = currentAlpha;
        lineRen.startColor = newColor;
        lineRen.endColor = newColor;

        // 3. Destroy when invisible
        if (currentAlpha <= 0) {
            Destroy(gameObject);
        }
    }

    void DrawCircle() {
        float angle = 0f;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++) {
            // Math to find the X,Z position on a circle
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * currentRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * currentRadius;

            lineRen.SetPosition(i, new Vector3(x, 0, z));

            angle += angleStep;
        }
    }
}