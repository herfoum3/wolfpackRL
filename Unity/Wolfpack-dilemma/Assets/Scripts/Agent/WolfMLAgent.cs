using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.Scriptables;
using Vector3 = UnityEngine.Vector3;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;


[System.Serializable]
public class LLMResponse
{
    public float llmReward;
}

[System.Serializable]
public class Position
{
    public string x, y, z;
}

[System.Serializable]
public class WolfTrajectory
{
    public int id;
    public Position[] trajectory;
}

[System.Serializable]
public class CoopFullRequest
{
    public WolfTrajectory[] wolf_histories;
    public Position[] deer_history;
    public int attacker_id;
    public float save_interval;
}


public class WolfMLAgent : Agent
{
    public bool isWolfMaster = false;

    private List<Transform> m_WolfsPositions;
    private SimpleMultiAgentGroup m_WolfGroup;
    private SoundReceiver m_SoundHeard;
    private Transform m_WolfTransform;
    private int m_WolfID;
    private DeerMLAgent m_DeerAgent;
    private float m_ElapsedTime;

    private GameManager.TrainingMode m_TrainingMode;
    private GameManager.CoopRewardMode m_CoopRewardMode;

    [Header("Zone Parameters")] private float m_ZoneArea;
    private float m_StartTime;

    [Header("Degenerate Wolf")] public float staminaThreshold = 10f;
    public float sprintDegeneration = 20f;
    public float attackDegeneration = 10f;
    public float sneakDegeneration = 10f;

    // Animal Controller
    private Stats m_WolfStats;
    private Stat m_WolfStamina;
    private MAnimal m_MAnimal;
    private MInput m_MInput;

    [Header("Control Mode")]
    // Spawn point
    public GameObject spawnPoint;

    private SpawnManager m_SpawnManager;
    private GameManager m_GameManager;

    //Memory
    private int[] m_Memory;

    private float m_SaveTimer;

    //DecisionRequest
    private float m_DecisionTimer;
    public FloatReference decisionInterval = new FloatReference(0.2f);
    private ActionBuffers m_LastActions;

    //Debug
    public enum WolfMode
    {
        Manual,
        Random,
        Frozen
    }

    public WolfMode controlMode = WolfMode.Manual;

    public override void Initialize()
    {
        m_WolfTransform = transform.parent.GetComponent<Transform>();
        m_SoundHeard = transform.parent.GetComponentInChildren<SoundReceiver>();

        // Spawn point
        m_SpawnManager = spawnPoint.GetComponent<SpawnManager>();
        m_GameManager = spawnPoint.GetComponent<GameManager>();

        // Init wolf history
        m_WolfID = GetWolfId();
        if (!m_GameManager.WolfHistories.ContainsKey(m_WolfID))
        {
            m_GameManager.WolfHistories[m_WolfID] = new List<Vector3>();
        }

        //Get Deer
        m_DeerAgent = m_SpawnManager.deerPosition.GetComponentInChildren<DeerMLAgent>();

        // Register current wolf
        m_TrainingMode = m_GameManager.trainingMode;
        if (m_TrainingMode == GameManager.TrainingMode.POCA)
        {
            m_WolfGroup = m_GameManager.WolfGroup;
            m_WolfGroup.RegisterAgent(this);
        }

        // Get current reward
        m_CoopRewardMode = m_GameManager.coopRewardMode;

        //Animal Controller
        m_WolfStats = GetComponentInParent<Stats>();
        m_WolfStamina = m_WolfStats.Stat_Get("Stamina");

        m_MAnimal = GetComponentInParent<MAnimal>();
        m_MInput = GetComponentInParent<MInput>();

        // Get Wolfs positions
        m_WolfsPositions = m_SpawnManager.usedWolfPositions;

        //Memory
        m_Memory = new int[] { 0, 0, 0 };
    }

    public override void OnEpisodeBegin()
    {
        // Reset Decision
        m_DecisionTimer = 0f;
        m_SaveTimer = 0f;

        // Start timer
        m_StartTime = Time.time;

        // Reset wall size
        if (isWolfMaster)
        {
            m_GameManager.SetEnvironmentParameters();
            // Reset Local Position
            m_SpawnManager.SpawnWolf(m_WolfsPositions);
            m_SpawnManager.SpawnDeer();
            m_GameManager.attackAlreadyHandled = false;
            m_GameManager.ResetHistories();
        }

        // Reset Stamina
        m_WolfStamina.Value = m_WolfStamina.MaxValue;

        // Reset Action buffer
        m_LastActions = ActionBuffers.Empty;

        // Reset Speed mode
        ResetAllToggles();

        // Reset Memory
        m_Memory = new int[] { 0, 0, 0 };
    }

    float Round3(float v) => Mathf.Round(v * 1000f) / 1000f;

    public override void CollectObservations(VectorSensor sensor)
    {
        float halfDiag = GetZoneDiagonalLength() * 0.5f;

        // This Position
        Vector3 pos = m_WolfTransform.localPosition;
        sensor.AddObservation(Round3(Mathf.Clamp(pos.x / halfDiag, -1f, 1f)));
        sensor.AddObservation(Round3(Mathf.Clamp(pos.z / halfDiag, -1f, 1f)));

        // Partner position
        foreach (Transform wolfPartner in m_WolfsPositions)
        {
            if (wolfPartner == null || wolfPartner == transform.parent) continue;

            Vector3 rel = wolfPartner.localPosition;
            sensor.AddObservation(Round3(Mathf.Clamp(rel.x / halfDiag, -1f, 1f)));
            sensor.AddObservation(Round3(Mathf.Clamp(rel.z / halfDiag, -1f, 1f)));
        }

        sensor.AddObservation(m_WolfStamina.Value / m_WolfStamina.MaxValue); // Get Stamina Value
        sensor.AddObservation(GetCurrentSpeed()); // Current Movement (Idle, Walk, Sprint, Sneak)

        for (int i = 0; i < m_SoundHeard.oneHot.Length; i++)
        {
            sensor.AddObservation(m_SoundHeard.oneHot[i]);
        }

        var oh = m_SoundHeard.oneHot;
        if (oh.Any(v => v != 0f) && transform.parent.name == "Wolf 1")
        {
            //Debug.Log($"[SoundObs] {transform.parent.name} oneHot: " +
            //          $"N={oh[0]:F2}, E={oh[1]:F2}, S={oh[2]:F2}, W={oh[3]:F2}");
        }

        // sensor.AddObservation(m_LastAction); // Get last action (Idle, Walk, Sprint, Sneak, Attack)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Debug.Log($"[{Time.time:F2}] WOLF - New decision {actions.DiscreteActions[0] + 1}/{actions.DiscreteActions[1] + 1}/{actions.DiscreteActions[2] + 1}");
        m_LastActions = actions;
        ExecuteAction(actions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;

        switch (controlMode)
        {
            case WolfMode.Frozen:
            {
                // Freeze Wolf
                discreteActionsOut[0] = 0; // Walk
                discreteActionsOut[1] = 1; // h = 0
                discreteActionsOut[2] = 1; // v = 0
                break;
            }
            case WolfMode.Random:
            {
                // Random movement
                int action0 = Random.Range(0, 4); // Walk, Sprint, Attack, Sneak

                if (m_WolfStamina.Value <= staminaThreshold && (action0 == 1 || action0 == 3))
                    action0 = 0;
                if (m_WolfStamina.Value - attackDegeneration <= staminaThreshold && action0 == 2)
                    action0 = 0;

                int h = Random.Range(0, 3);
                int v = Random.Range(0, 3);

                discreteActionsOut[0] = action0;
                discreteActionsOut[1] = h;
                discreteActionsOut[2] = v;
                break;
            }
            case WolfMode.Manual:
            default:
            {
                /*Action 1*/
                int inputAction;

                if (Input.GetKey(KeyCode.LeftShift) && m_WolfStamina.Value > staminaThreshold)
                {
                    inputAction = 1; // Sprint
                }

                else if (Input.GetKey(KeyCode.Mouse0) && m_WolfStamina.Value - attackDegeneration > staminaThreshold)
                {
                    inputAction = 2; // Attack
                }

                else if (Input.GetKey(KeyCode.C) && m_WolfStamina.Value > staminaThreshold)
                {
                    inputAction = 3; // Sneak
                }

                else if (Input.GetKey(KeyCode.E) || m_WolfStamina.Value <= staminaThreshold)
                {
                    inputAction = 0; // Walk
                }
                else
                {
                    inputAction = m_Memory[0]; //Same input
                }

                /*Action 2*/

                int h = 0;
                int v = 0;

                if (inputAction != 2)
                {
                    h = Input.GetKey(KeyCode.LeftArrow) ? -1 :
                        Input.GetKey(KeyCode.RightArrow) ? 1 : 0;

                    v = Input.GetKey(KeyCode.UpArrow) ? 1 :
                        Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                }

                discreteActionsOut[0] = inputAction;
                discreteActionsOut[1] = h + 1;
                discreteActionsOut[2] = v + 1;
                break;
            }
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        /*Branch 0 : Main action*/

        // Disable Sprint/Sneak
        if (m_WolfStamina.Value <= staminaThreshold)
        {
            actionMask.SetActionEnabled(0, 1, false); // Sprint
            actionMask.SetActionEnabled(0, 3, false); // Sneak
        }

        // Disable Attack
        if (m_WolfStamina.Value - attackDegeneration <= staminaThreshold)
        {
            actionMask.SetActionEnabled(0, 2, false); // Attack
        }

        /*Branches 1 and 2: movement direction*/
        if (controlMode == WolfMode.Frozen)
        {
            // Only allow neutral direction
            actionMask.SetActionEnabled(1, 0, false); // h = -1
            actionMask.SetActionEnabled(1, 2, false); // h = +1
            actionMask.SetActionEnabled(2, 0, false); // v = -1
            actionMask.SetActionEnabled(2, 2, false); // v = +1
        }
    }


    public void ToggleAction(string action, float degeneration)
    {
        string[] exclusiveActions = { "Sprint", "Attack1", "Sneak" };

        foreach (var inputName in exclusiveActions)
        {
            var input = m_MInput.FindInput(inputName);

            // Active action
            if (inputName == action)
            {
                // Debug.Log($"{action} Wolf");
                if (input.Button == InputButton.Toggle) // Toggle
                {
                    if (!input.InputValue)
                    {
                        input.InputValue = true;
                        input.OnInputChanged.Invoke(true);
                        input.OnInputDown.Invoke();
                    }

                    m_WolfStamina.DegenRate = degeneration;
                    m_MAnimal.Sprint = true; // Active Degeneration Sprint/Sneak
                }
                else // Press Attack
                {
                    input.InputValue = true;
                    input.OnInputChanged.Invoke(true);
                    input.OnInputDown.Invoke();

                    // Reset instantly
                    input.InputValue = false;
                    input.OnInputChanged.Invoke(false);
                    input.OnInputUp.Invoke();
                    ResetAllToggles();
                }
            }

            // Disable other actions
            else
            {
                // Disable all toggles
                if (input.Button == InputButton.Toggle && input.InputValue)
                {
                    input.InputValue = false;
                    input.OnInputChanged.Invoke(false);
                    input.OnInputUp.Invoke();
                }
            }
        }
    }

    void ResetAllToggles()
    {
        // Debug.Log("Walk/Idle Wolf");
        m_MAnimal.Sprint = false;
        m_WolfStamina.Regenerate = true;
        string[] toggledInputs = { "Sprint", "Sneak" };

        foreach (var inputName in toggledInputs)
        {
            var input = m_MInput.FindInput(inputName);

            if (input != null && input.Button == InputButton.Toggle && input.InputValue)
            {
                input.InputValue = false;
                input.OnInputChanged.Invoke(false);
                input.OnInputUp.Invoke();
            }

            m_Memory[0] = 0;
        }
    }

    int GetCurrentSpeed()
    {
        string speedName = m_MAnimal.CurrentSpeedModifier.name;
        switch (speedName)
        {
            case "Trot": return 1;
            case "Wounded Walk": return 2;
            case "Run Sprint": return 3;
            default: return 0;
        }
    }

    public bool IsAttacking()
    {
        return m_MAnimal.ActiveMode != null && m_MAnimal.ActiveMode.ID.name == "Attack1";
    }

    private void ApplyFinalReward(float baseReward, float timeReward, float coopReward)
    {
        // Combine base reward + time bonus scale by wall size
        float totalReward = baseReward + timeReward + coopReward;
        Debug.Log($"{transform.root.name} - [WOLF REWARD] base={baseReward:F2}/{m_GameManager.baseWeight:F2} + " +
                  $"time={timeReward:F2}/{m_GameManager.timeWeight:F2} + coop={coopReward:F2}/{m_GameManager.coopWeight:F2}" +
                  $" →  {totalReward:F2} (elapsed: {m_ElapsedTime:F1}s | area: {m_ZoneArea:F2} | attacker : {transform.parent.name})");

        // Attribute reward
        if (m_TrainingMode == GameManager.TrainingMode.POCA)
        {
            m_WolfGroup.AddGroupReward(totalReward);
            m_WolfGroup.EndGroupEpisode();
        }
        else
        {
            var wolfsCopy = new List<Transform>(m_WolfsPositions); //avoid internal changes issues
            foreach (Transform wolfTf in wolfsCopy)
            {
                WolfMLAgent otherWolf = wolfTf.GetComponentInChildren<WolfMLAgent>();

                // Skip the wolf is the attacker
                if (otherWolf == null || otherWolf == this || !otherWolf.enabled) continue;
                otherWolf.AddReward(totalReward);
                otherWolf.EndEpisode();
            }

            AddReward(totalReward);
            EndEpisode();
        }
        
        m_DeerAgent.OnDeerHitReward(m_ElapsedTime, m_ZoneArea);
    }

    private void ComputeReward()
    {
        m_ZoneArea = m_GameManager.wallsOuter.transform.localScale.x;

        /* Base reward */
        float baseReward = 1f * m_GameManager.baseWeight;

        /* Time reward */
        m_ElapsedTime = Time.time - m_StartTime;

        // Sigmoid parameters
        float k = 0.15f;
        float T = 3f + 10f * m_ZoneArea;
        float b = -2f;
        float exponent = (k / m_ZoneArea) * (m_ElapsedTime - T) + b;
        float timeFactor = 1f / (1f + Mathf.Exp(exponent));
        float timeReward = timeFactor * m_GameManager.timeWeight;

        /* Coop reward */
        float coopReward;
        
        if (m_CoopRewardMode == GameManager.CoopRewardMode.LLM && m_GameManager.WolfHistories[m_WolfID].Count >= 10)
        {
            StartCoroutine(GetCoopRewardFromLLM((results) =>
            {
                coopReward = results;
                ApplyFinalReward(baseReward, timeReward, coopReward);
            }));
            
        }
        else
        {
            coopReward = GiveCooperativeReward() * m_GameManager.coopWeight;
            ApplyFinalReward(baseReward, timeReward, coopReward);
        }
    }

    public void OnSuccessfulAttack()
    {
        // Check to avoid simultaneous attacks
        if (m_GameManager.attackAlreadyHandled)
        {
            Debug.Log("Attack already handled");
            return;
        }

        m_GameManager.attackAlreadyHandled = true;
        ComputeReward();
    }

    private IEnumerator GetCoopRewardFromLLM(System.Action<float> callback)
    {
        //Debug.Log("LLM Reward");

        var wolfHistories = m_GameManager.WolfHistories.Select(pair => new WolfTrajectory
        {
            id = pair.Key,
            trajectory = pair.Value.Select(v => new Position
            {
                x = v.x.ToString("F2"),
                y = v.y.ToString("F2"),
                z = v.z.ToString("F2")
            }).ToArray()
        }).ToArray();

        var deerHistory = m_GameManager.deerHistory.Select(v => new Position
        {
            x = v.x.ToString("F2"),
            y = v.y.ToString("F2"),
            z = v.z.ToString("F2")
        }).ToArray();

        var coopRequest = new CoopFullRequest
        {
            wolf_histories = wolfHistories,
            deer_history = deerHistory,
            attacker_id = m_WolfID,
            save_interval = m_GameManager.saveInterval,
        };

        string jsonData = JsonUtility.ToJson(coopRequest, false);
        // Debug.Log(jsonData);

        using (var request = new UnityWebRequest(m_GameManager.llmServerUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<LLMResponse>(request.downloadHandler.text);
                callback(response.llmReward * m_GameManager.coopWeight);
            }
            else
            {
                Debug.LogWarning($"[LLM] Error: {request.error}");
                callback(0f);
            }
        }
    }

    private int GetWolfId()
    {
        string wolfName = transform.parent.name;
        if (wolfName.StartsWith("Wolf"))
        {
            if (int.TryParse(wolfName.Substring(5), out int id))
                return id;
        }

        return 0;
    }

    private float GiveCooperativeReward()
    {
        //Debug.Log("Distance Reward");

        // Estimate the maximum distance in the zone
        float radius = GetZoneDiagonalLength() * m_GameManager.wolfRadiusReward;
        List<float> coopList = new List<float>();

        var wolfsCopy = new List<Transform>(m_WolfsPositions); //avoid internal changes issues

        foreach (Transform wolfTf in wolfsCopy)
        {
            WolfMLAgent otherWolf = wolfTf.GetComponentInChildren<WolfMLAgent>();

            // Skip the wolf is the attacker
            if (otherWolf == null || otherWolf == this || !otherWolf.enabled) continue;

            // Measure distance
            float dist = Vector3.Distance(wolfTf.localPosition, this.m_WolfTransform.localPosition);

            float proxyReward = 0f;
            if (dist <= radius)
            {
                // Radius reward
                proxyReward = 1f - Mathf.Clamp01(Mathf.Abs(dist - 0.1f) / radius);
            }

            coopList.Add(proxyReward);
        }

        if (coopList.Count == 0)
            return 0f;

        // Compute reward
        float coopReward = coopList.Average();
        return coopReward;
    }

    private float GetZoneDiagonalLength()
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (Transform wall in m_GameManager.wallsOuter.transform)
        {
            Vector3 pos = wall.position;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;
        }

        float width = maxX - minX;
        float height = maxZ - minZ;

        // Pytha pytha
        float diagonal = Mathf.Sqrt(width * width + height * height);
        return diagonal;
    }

    private void FixedUpdate()
    {
        m_SaveTimer += Time.fixedDeltaTime;

        if (m_SaveTimer >= m_GameManager.saveInterval && m_GameManager.coopRewardMode == GameManager.CoopRewardMode.LLM)
        {
            m_SaveTimer = 0f;

            if (m_GameManager.WolfHistories.ContainsKey(m_WolfID))
            {
                m_GameManager.WolfHistories[m_WolfID].Add(m_WolfTransform.localPosition);
                if (m_GameManager.WolfHistories[m_WolfID].Count > 10)
                {
                    m_GameManager.WolfHistories[m_WolfID].RemoveAt(0);
                }
            }

            if (isWolfMaster)
            {
                // History deer
                if (m_SpawnManager.deerPosition != null)
                {
                    m_GameManager.deerHistory.Add(m_SpawnManager.deerPosition.localPosition);
                    if (m_GameManager.deerHistory.Count > 10)
                    {
                        m_GameManager.deerHistory.RemoveAt(0);
                    }
                }
            }
        }

        if (StepCount + 1 >= MaxStep)
        {
            Debug.Log("[WOLF FAIL] Episode ended by timeout");
            if (m_TrainingMode == GameManager.TrainingMode.POCA)
            {
                m_WolfGroup.AddGroupReward(-0.5f);
                m_WolfGroup.EndGroupEpisode();
                return;
            }

            var wolfsCopy = new List<Transform>(m_WolfsPositions); //avoid internal changes issues
            foreach (Transform wolfTf in wolfsCopy)
            {
                WolfMLAgent otherWolf = wolfTf.GetComponentInChildren<WolfMLAgent>();

                // Skip the wolf is the attacker
                if (otherWolf == null || otherWolf == this || !otherWolf.enabled) continue;
                otherWolf.AddReward(-0.5f);
                otherWolf.EndEpisode();
            }

            return;
        }

        m_DecisionTimer += Time.fixedDeltaTime;

        if (m_DecisionTimer >= decisionInterval.Value)
        {
            RequestDecision();
            m_DecisionTimer = 0f;
        }
        else if (m_LastActions.DiscreteActions.Length > 0)
        {
            ExecuteAction(m_LastActions);
        }
    }

    public void ExecuteAction(ActionBuffers actions)
    {
        /* Action 1*/
        int action = actions.DiscreteActions[0];
        if (action != m_Memory[0] && !IsAttacking())
        {
            m_Memory[0] = action;

            switch (action)
            {
                case 0: // Idle / Walk
                    ResetAllToggles();
                    break;

                case 1: // Sprint
                    ToggleAction("Sprint", sprintDegeneration);
                    break;

                case 2: // Attack
                    ToggleAction("Attack1", 0f);
                    m_WolfStamina.Value -= attackDegeneration;
                    break;

                case 3: // Sneak
                    ToggleAction("Sneak", sneakDegeneration);
                    break;
            }
        }

        /* Action 2*/
        if (action != 2)
        {
            int h = actions.DiscreteActions[1] - 1;
            int v = actions.DiscreteActions[2] - 1;

            Vector3 inputDir = new Vector3(h, 0, v);
            m_MAnimal.SetInputAxis(inputDir);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || m_WolfTransform == null || m_GameManager.wallsOuter == null) return;

        float radius = GetZoneDiagonalLength() * m_GameManager.wolfRadiusReward;

        Color fillColor = new Color(0f, 1f, 0f, 0.25f);
        Color borderColor = Color.green;

        // Disc
        Handles.color = fillColor;
        Handles.DrawSolidDisc(m_WolfTransform.position, Vector3.up, radius);

        Handles.color = borderColor;
        Handles.DrawWireDisc(m_WolfTransform.position, Vector3.up, radius);
    }
#endif
}