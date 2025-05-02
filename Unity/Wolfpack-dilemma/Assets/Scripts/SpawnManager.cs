using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnManager : MonoBehaviour
{
    public float wolfSeparationRadius = 1f;
    public List<Transform> usedWolfPositions = new List<Transform>();
    public Transform deerPosition;

    private Transform m_WallsOuter;
    public LayerMask groundMask;
    public float deerCheckRadius = 0.5f;

    private void Awake()
    {
        m_WallsOuter = transform.parent.Find("WallsOuter");
    }

    public void SpawnWolf(List<Transform> wolfsAgent)
    {
        foreach (var wolf in wolfsAgent)
        {
            int maxTries = 50;
            int tries = 0;
            bool valid = false;
            Vector3 groundedLocalPos = Vector3.zero;

            while (!valid && tries < maxTries)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(0.1f, 1.0f) * wolfSeparationRadius;

                Vector3 localPos = new Vector3(
                    Mathf.Cos(angle) * distance,
                    10f,
                    Mathf.Sin(angle) * distance
                );

                Vector3 worldPos = transform.TransformPoint(localPos);
                Vector3 groundedWorldPos = RaycastToGround(worldPos);
                groundedLocalPos = transform.InverseTransformPoint(groundedWorldPos);

                valid = true;

                // Check overlap wolves
                foreach (Transform other in usedWolfPositions)
                {
                    if (Vector3.Distance(groundedLocalPos, other.localPosition) < wolfSeparationRadius)
                    {
                        valid = false;
                        break;
                    }
                }

                tries++;
            }

            wolf.localPosition = groundedLocalPos;
            wolf.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            ResetPhysics(wolf);
        }
    }

    public void SpawnDeer()
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        float wallThickness = 0.5f;

        foreach (Transform wall in m_WallsOuter)
        {
            Vector3 localPos = transform.InverseTransformPoint(wall.position);

            float thickness = Mathf.Min(wall.localScale.x, wall.localScale.z);
            wallThickness = Mathf.Max(wallThickness, thickness);

            if (localPos.x < minX) minX = localPos.x;
            if (localPos.x > maxX) maxX = localPos.x;
            if (localPos.z < minZ) minZ = localPos.z;
            if (localPos.z > maxZ) maxZ = localPos.z;
        }

        // wall offset
        float margin = wallThickness * 0.5f;
        minX += margin;
        maxX -= margin;
        minZ += margin;
        maxZ -= margin;

        // Spawn zone
        float edgeRangeMin = 0.1f;
        float edgeRangeMax = 0.2f;

        Vector3 localPosDeer = Vector3.zero;
        bool valid = false;
        int tries = 0;

        while (!valid && tries < 50)
        {
            bool edgeOnX = Random.value < 0.5f;

            float xFactor, zFactor;

            if (edgeOnX)
            {
                xFactor = Random.value < 0.5f
                    ? Random.Range(edgeRangeMin, edgeRangeMax)
                    : Random.Range(1f - edgeRangeMax, 1f - edgeRangeMin);
                zFactor = Random.Range(0.05f, 0.95f);
            }
            else
            {
                xFactor = Random.Range(0.05f, 0.95f);
                zFactor = Random.value < 0.5f
                    ? Random.Range(edgeRangeMin, edgeRangeMax)
                    : Random.Range(1f - edgeRangeMax, 1f - edgeRangeMin);
            }

            float x = Mathf.Lerp(minX, maxX, xFactor);
            float z = Mathf.Lerp(minZ, maxZ, zFactor);

            localPosDeer = new Vector3(x, 10f, z);
            Vector3 worldPos = transform.TransformPoint(localPosDeer);

            if (Physics.Raycast(worldPos, Vector3.down, out RaycastHit hit, 20f, groundMask))
            {
                Debug.DrawRay(worldPos, Vector3.down * 20f, Color.blue);
                Vector3 groundedPos = hit.point;

                // Check obstacle
                bool overlapsObstacle =
                    Physics.CheckSphere(groundedPos + Vector3.up * 0.1f, deerCheckRadius, ~groundMask);

                if (!overlapsObstacle)
                {
                    valid = true;
                    localPosDeer = transform.InverseTransformPoint(groundedPos);
                }
            }

            tries++;
        }

        Vector3 groundedWorldPos = RaycastToGround(transform.TransformPoint(localPosDeer));
        Vector3 groundedLocalPos = transform.InverseTransformPoint(groundedWorldPos);

        deerPosition.localPosition = groundedLocalPos;
        deerPosition.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        ResetPhysics(deerPosition);
    }


    Vector3 RaycastToGround(Vector3 from)
    {
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 20f, groundMask))
            return hit.point;
        return from; // fallback
    }

    void ResetPhysics(Transform obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}