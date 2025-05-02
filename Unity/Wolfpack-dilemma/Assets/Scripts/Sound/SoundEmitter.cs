using System;
using UnityEngine;
using System.Collections.Generic;
using MalbersAnimations.Controller;

public class SoundEmitter : MonoBehaviour
{
    // Define radius
    public float walkingSoundRadius = 5f;
    public float sprintingSoundRadius = 10f;

    // Filter receiver
    public LayerMask targetLayerMask;
    private HashSet<SoundReceiver> m_DejaVu;
    
    // Manimal
    private MAnimal m_MAnimal;

    private void Start()
    {
        m_DejaVu = new HashSet<SoundReceiver>();

        // Manimal
        m_MAnimal = GetComponentInParent<MAnimal>();
        //
        // walkingSoundRadius *= wallsOuter.transform.localScale.x;
        // sprintingSoundRadius *= wallsOuter.transform.localScale.x;
        //
        // Debug.Log(walkingSoundRadius);
    }

    private void Update()
    {
        if (m_MAnimal.MovementDetected)
        {
            string action = MapSpeedName(m_MAnimal.CurrentSpeedModifier.name);
            EmitSound(action);
        }
        else if (IsAttacking())
        {
            EmitSound("attack");
        }
    }


    public void EmitSound(string action)
    {
        float radius = 0f;
        m_DejaVu.Clear();

        // Radius different state
        switch (action.ToLower())
        {
            case "walk":
                radius = walkingSoundRadius;
                break;
            case "attack":
                radius = sprintingSoundRadius;
                break;
            case "sprint":
                radius = sprintingSoundRadius;
                break;
            case "sneak":
                return;
            default:
                Debug.LogWarning("Unknown action" + action);
                return;
        }

        Collider[] detectedCollider = Physics.OverlapSphere(transform.position, radius, targetLayerMask);
        //Debug.Log($"{transform.root.name} on {action} - Collider detected : {detectedCollider.Length}");

        foreach (Collider col in detectedCollider)
        {
            var animalEar = col.GetComponent<SoundReceiver>();
            if (animalEar == null) continue;

            var otherAnimal = col.GetComponentInParent<MAnimal>();
            var thisAnimal  = m_MAnimal;
            
            // Ignore
            if (otherAnimal == thisAnimal) 
                continue;

            if (m_DejaVu.Add(animalEar))
            {
                animalEar.ReceiveSound(transform.position, thisAnimal.name);
            }
        }
    }

    private string MapSpeedName(string original)
    {
        switch (original)
        {
            case "Trot":
                return "Walk";

            case "Wounded Walk":
                return "Sneak";

            case "Run Sprint":
                return "Sprint";

            default:
                return original;
        }
    }
    
    public bool IsAttacking()
    {
        return m_MAnimal.ActiveMode != null && m_MAnimal.ActiveMode.ID.name == "Attack1";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, walkingSoundRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sprintingSoundRadius);
    }
}