using UnityEngine;

public class CameraAutoReposition : MonoBehaviour
{
    [SerializeField] private Transform wallsOuterParent;

    [SerializeField] private float referenceScale = 0.2f;

    [Header("Direction")]
    [SerializeField] private Vector3 direction = new Vector3(0, 0, -1);

    [SerializeField] private float distanceMultiplier = 10f;

    private Vector3 initialLocalPosition;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (wallsOuterParent == null) return;

        // Prend le scale local (comme CameraAutoFit)
        float currentScale = wallsOuterParent.localScale.x;
        float ratio = currentScale / referenceScale;

        // Ajuste le recul proportionnellement
        Vector3 offset = direction.normalized * (ratio - 1f) * distanceMultiplier;

        transform.localPosition = initialLocalPosition + offset;
    }
}
