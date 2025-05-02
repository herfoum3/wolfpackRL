using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.Scriptables;
using Vector3 = UnityEngine.Vector3;


public class DeerMLAgent : Agent
{
    public Transform deerTransform;

    [Header("Zone Parameters")] public GameObject wallsOuter;
    private float m_StartTime;

    [Header("Degenerate Deer")] public float staminaThreshold = 10f;
    public float sprintDegeneration = 20f;

    // Animal Controller
    private Stats m_DeerStats;
    private Stat m_DeerStamina;
    private MAnimal m_MAnimal;
    private MInput m_MInput;
    private SoundReceiver m_SoundHeard;
    
    //Memory
    private int[] m_Memory;

    //DecisionRequest
    private float m_DecisionTimer = 0f;
    public FloatReference decisionInterval = new FloatReference(0.2f);
    private ActionBuffers m_LastActions;

    //Debug
    public enum DeerMode
    {
        Manual,
        Random,
        Frozen
    }

    public DeerMode mode = DeerMode.Manual;
    private string m_MemoryNames;

    public override void Initialize()
    {
        m_SoundHeard = transform.parent.GetComponentInChildren<SoundReceiver>();

        //Animal Controller
        m_DeerStats = GetComponentInParent<Stats>();
        m_DeerStamina = m_DeerStats.Stat_Get("Stamina");

        m_MAnimal = GetComponentInParent<MAnimal>();
        m_MInput = GetComponentInParent<MInput>();
        
        //Memory
        m_Memory = new int[] { 0, 0, 0 };
    }


    public override void OnEpisodeBegin()
    {
        // Reset Decision
        m_DecisionTimer = 0f;

        // Start timer
        m_StartTime = Time.time;

        // Reset Stamina
        m_DeerStamina.Value = m_DeerStamina.MaxValue;

        // Reset Action buffer
        m_LastActions = ActionBuffers.Empty;

        // Reset Speed Mode
        ResetAllToggles();

        // Reset Memory
        m_Memory = new int[] { 0, 0, 0 };
    }

    float Round3(float v) => Mathf.Round(v * 1000f) / 1000f;

    public override void CollectObservations(VectorSensor sensor)
    {
        float halfDiag = wallsOuter.transform.localScale.x * Mathf.Sqrt(2) * 0.5f;

        // This Position (normalized)
        Vector3 pos = deerTransform.localPosition;
        sensor.AddObservation(Round3(Mathf.Clamp(pos.x / halfDiag, -1f, 1f)));
        sensor.AddObservation(Round3(Mathf.Clamp(pos.z / halfDiag, -1f, 1f)));

        // Stamina (normalized)
        sensor.AddObservation(m_DeerStamina.Value / m_DeerStamina.MaxValue);

        // Current speed 
        sensor.AddObservation(GetCurrentSpeed());
        // sensor.AddObservation(m_LastAction); // Get last action (Idle, Walk, Sprint)

       
        for (int i = 0; i < m_SoundHeard.oneHot.Length; i++)
        {
            sensor.AddObservation(m_SoundHeard.oneHot[i]);
        }

        var oh = m_SoundHeard.oneHot;
        if (oh.Any(v => v != 0f) && transform.parent.name == "Deer")
        {
            // Debug.Log($"[SoundObs] {transform.parent.name} oneHot: " +
            //          $"N={oh[0]:F2}, E={oh[1]:F2}, S={oh[2]:F2}, W={oh[3]:F2}");
        }

    
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Debug.Log($"[{Time.time:F2}] DEER - New decision {actions.DiscreteActions[0] + 1}/{actions.DiscreteActions[1] + 1}/{actions.DiscreteActions[2] + 1}");
        m_LastActions = actions;
        ExecuteAction(actions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;

        switch (mode)
        {
            case DeerMode.Frozen:
            {
                // Freeze Deer
                discreteActionsOut[0] = 0; // Walk 
                discreteActionsOut[1] = 1; // h = 0
                discreteActionsOut[2] = 1; // v = 0
                break;
            }
            case DeerMode.Random:
            {
                // Random movement
                int action0 = (m_DeerStamina.Value > staminaThreshold) ? Random.Range(0, 2) : 0; // Sprint or Walk
                int h = Random.Range(0, 3);
                int v = Random.Range(0, 3);

                discreteActionsOut[0] = action0;
                discreteActionsOut[1] = h;
                discreteActionsOut[2] = v;
                break;
            }
            case DeerMode.Manual:
            default:
            {
                /*Action 1*/
                int inputAction;

                if (Input.GetKey(KeyCode.LeftShift) && m_DeerStamina.Value > staminaThreshold)
                {
                    inputAction = 1; // Sprint
                }

                else if (Input.GetKey(KeyCode.E) || m_DeerStamina.Value <= staminaThreshold)
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

        // Disable sprint
        if (m_DeerStamina.Value <= staminaThreshold)
        {
            actionMask.SetActionEnabled(0, 1, false);
        }

        /*Branches 1 and 2: movement direction*/
        if (mode == DeerMode.Frozen)
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
        string[] exclusiveActions = { "Sprint" };

        foreach (var inputName in exclusiveActions)
        {
            var input = m_MInput.FindInput(inputName);

            // Active action
            if (inputName == action)
            {
                // Debug.Log($"{action} Deer");
                if (input.Button == InputButton.Toggle) // Toggle
                {
                    if (!input.InputValue)
                    {
                        input.InputValue = true;
                        input.OnInputChanged.Invoke(true);
                        input.OnInputDown.Invoke();
                    }

                    m_DeerStamina.DegenRate = degeneration;
                    m_MAnimal.Sprint = true; // Active Degeneration Sprint/Sneak
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
        // Debug.Log("Walk/Idle Deer");
        m_MAnimal.Sprint = false;
        m_DeerStamina.Regenerate = true;
        string[] toggledInputs = { "Sprint" };

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

    public void OnDeerHitReward(float elapsedTime, float zoneArea)
    {
        /* Time reward */

        // Sigmoid parameters
        float k = 0.15f;
        float T = 3f + 10f * zoneArea;
        float b = 1f;
        float exponent =  - (k / zoneArea) * (elapsedTime - T) + b;
        float timeReward = 1f / (1f + Mathf.Exp(exponent));

        Debug.Log($"[DEER REWARD] Survived {elapsedTime:F1}s → {timeReward:F2}");
        
        AddReward(timeReward); // Reward
        EndEpisode(); // Reset
    }

    private void FixedUpdate()
    {
        if (StepCount + 1 >= MaxStep)
        {
            AddReward(1f); // success
            Debug.Log("[DEER SUCCESS] Episode ended by timeout");
            EndEpisode();
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
        if (action != m_Memory[0])
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
}