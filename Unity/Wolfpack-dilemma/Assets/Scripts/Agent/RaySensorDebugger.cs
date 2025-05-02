using System;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections.Generic;

public class RaySensorDebugger : MonoBehaviour
{
    public bool atStart = true;
    public bool atUpdate = false;
    private List<RaycastInfo> m_Infos;

    [Header("Show infos")] 
    public bool showRayIndex = true;
    public bool showGameObject = true;
    public bool showHitDistance = true;
    public bool showTag = true;
    private RayPerceptionSensorComponent3D m_RayComponent;

    private void Start()
    {
        m_RayComponent = transform.parent.GetComponentInChildren<RayPerceptionSensorComponent3D>();
        if (atStart)
        {
            m_Infos = CheckRayCast();
            DebugRay();
        }
    }

    private void Update()
    {
        if (atUpdate)
        {
            m_Infos = CheckRayCast();
            DebugRay();
        }
    }

    private void DebugRay()
    {
        foreach (var info in m_Infos)
        {
            string log = "Ray ";

            if (showRayIndex)
                log += $"{info.RayIndex + 1} ";

            if (showGameObject)
                log += $"- GameObject: {info.HitObject.name} ";

            if (showHitDistance)
                log += $"- Hit distance: {info.HitDistance:F2} ";

            if (showTag)
                log += $"- Tag: {info.Tag}";

            Debug.Log(log);
        }
    }

    public class RaycastInfo
    {
        public int RayIndex;
        public GameObject HitObject;
        public float HitDistance;
        public string Tag;
    }

    private List<RaycastInfo> CheckRayCast()
    {
        var rayOutputs = RayPerceptionSensor.Perceive(m_RayComponent.GetRayPerceptionInput(), true).RayOutputs;
        int lengthOfRayOutputs = rayOutputs.Length;

        List<RaycastInfo> raycastInfos = new List<RaycastInfo>();

        for (int i = 0; i < lengthOfRayOutputs; i++)
        {
            GameObject goHit = rayOutputs[i].HitGameObject;
            if (goHit != null)
            {
                var rayDirection = rayOutputs[i].EndPositionWorld - rayOutputs[i].StartPositionWorld;
                var scaledRayLength = rayDirection.magnitude;
                float rayHitDistance = rayOutputs[i].HitFraction * scaledRayLength;

                RaycastInfo info = new RaycastInfo
                {
                    RayIndex = i,
                    HitObject = goHit,
                    HitDistance = rayHitDistance,
                    Tag = goHit.tag
                };

                raycastInfos.Add(info);
            }
        }

        return raycastInfos;
    }
}