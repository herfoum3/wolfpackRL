using UnityEngine;

public class WolfAttackSensor : MonoBehaviour
{
    private WolfMLAgent m_WolfAgent;
    public LayerMask targetLayer;

    private void Start()
    {
        m_WolfAgent = transform.parent.parent.GetComponentInChildren<WolfMLAgent>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if ((targetLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (other.CompareTag("Deer")) // && m_Agent.IsAttacking()
            {
                m_WolfAgent.OnSuccessfulAttack();
            }
        }
    }
}