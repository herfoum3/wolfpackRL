using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public readonly SimpleMultiAgentGroup WolfGroup = new SimpleMultiAgentGroup();
    private float m_WallSize;
    public bool play = false;
    
    /*Default values*/

    // Training Mode
    public enum TrainingMode
    {
        PPO,
        POCA
    }

    public TrainingMode trainingMode = TrainingMode.PPO;

    // Coop Mode
    public enum CoopRewardMode
    {
        Distance,
        LLM
    }

    public CoopRewardMode coopRewardMode = CoopRewardMode.Distance;

    // Behavior Name
    public enum BehaviorName
    {
        Wolf,
        Deer
    }

    public BehaviorName behavior = BehaviorName.Wolf;

    // Default wall size
    public bool randomness;
    public float manualWallSize = 0.2f;

    // Reward
    [Header("Reward Settings")] public float wolfRadiusReward = 0.25f;
    [Range(0f, 1f)] public float baseWeight = 0.2f;
    [Range(0f, 1f)] public float timeWeight = 0.5f;
    [Range(0f, 1f)] public float coopWeight = 0.3f;

    [Header("LLM Cooperation")] public string llmServerUrl = "http://localhost:5000/get_coop_reward";
    public float saveInterval = 0.5f;
    
    // Static values
    [HideInInspector] public Transform wallsOuter;
    [HideInInspector] public bool attackAlreadyHandled = false;
    [HideInInspector] public Dictionary<int, List<Vector3>> WolfHistories = new Dictionary<int, List<Vector3>>();
    [HideInInspector] public List<Vector3> deerHistory = new List<Vector3>();

    private void Awake()
    {
        wallsOuter = transform.parent.Find("WallsOuter");
    }
    
    public void ResetHistories()
    {
        foreach (var wolfList in WolfHistories.Values)
        {
            wolfList.Clear();
        }

        deerHistory.Clear();
    }

    public void SetEnvironmentParameters()
    {
        // Rewards
        float rewards = Academy.Instance.EnvironmentParameters.GetWithDefault("rewards", 0f);
        if (rewards != 0f)
        {
            baseWeight = Mathf.Floor(rewards / 100f) / 10f;
            timeWeight = Mathf.Floor((rewards % 100f) / 10f) / 10f;
            coopWeight = (rewards % 10f) / 10f;
        }

        coopRewardMode =
            (CoopRewardMode)Academy.Instance.EnvironmentParameters.GetWithDefault("isLLM", (float)coopRewardMode);

        // Target
        float behaviorTarget =
            Academy.Instance.EnvironmentParameters.GetWithDefault("behavior_target", (float)behavior);

        // Wall size
        float defaultRandomness  = Academy.Instance.EnvironmentParameters.GetWithDefault("randomness", randomness ? 1f : 0f);
        
        m_WallSize = Academy.Instance.EnvironmentParameters.GetWithDefault("wall_size", manualWallSize);
        if (defaultRandomness != 0f)
        {
            m_WallSize = behaviorTarget == (float)BehaviorName.Wolf
                ? Random.Range(0.2f, m_WallSize)
                : Random.Range(m_WallSize, 1f);
        }

        wallsOuter.localScale = new Vector3(m_WallSize, 0.5f, m_WallSize);
        
        if (behaviorTarget == (float)BehaviorName.Wolf)
        {
            coopRewardMode = CoopRewardMode.Distance;
        }
        
        Debug.Log($"[ENV PARAM] rewards: {rewards} | coopMode: {coopRewardMode} | behaviorTarget: {behaviorTarget} " +
                  $"| Wall size {m_WallSize}");
    }
}
