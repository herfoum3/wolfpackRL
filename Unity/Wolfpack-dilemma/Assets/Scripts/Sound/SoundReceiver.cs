using MalbersAnimations.Controller;
using UnityEngine;

public enum CardinalDirection
{
    North,
    East,
    South,
    West
}

public class SoundReceiver : MonoBehaviour
{
    public float[] oneHot;

    private void Awake()
    {
        oneHot = new float[4];
    }

    
    public void ReceiveSound(Vector3 soundOrigin, string soundName)
    {
        System.Array.Clear(oneHot, 0, oneHot.Length); // Reset
        
        Vector3 rel = soundOrigin - transform.position;
        
        CardinalDirection dir;
        if (Mathf.Abs(rel.x) > Mathf.Abs(rel.z))
        {
            dir = rel.x > 0 ? CardinalDirection.East : CardinalDirection.West;
        }
        else
        {
            dir = rel.z > 0 ? CardinalDirection.North : CardinalDirection.South;
        }

        //Debug.Log($"{GetComponentInParent<MAnimal>().name} detect {soundName} from the {dir}");
        oneHot[(int)dir] = 1f;
    }
}
