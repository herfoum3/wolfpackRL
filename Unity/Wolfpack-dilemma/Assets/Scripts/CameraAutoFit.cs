using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoFit: MonoBehaviour
{
    [Header("Parent qui contient tous les murs WallsOuter")]
    [SerializeField] private Transform wallsOuterParent;

    [Header("Taille de la caméra pour outerWalls en scale 1")]
    [SerializeField] private float baseCameraSize = 16.5f;
    
    [Header("Optionnel : marge (padding) supplémentaire")]
    [SerializeField] private float padding = 0f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogWarning("Ce script est conçu pour une caméra orthographique.");
        }
    }

    private void LateUpdate()
    {
        if (wallsOuterParent == null) return;

        // On suppose une échelle uniforme
        float scaleFactor = wallsOuterParent.localScale.x; 
        cam.orthographicSize = baseCameraSize * scaleFactor + padding;
    }
}