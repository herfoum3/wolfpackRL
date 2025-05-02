using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    private Camera[] cameras;
    private int currentIndex = 0;

    void Start()
    {
        cameras = GetComponentsInChildren<Camera>(true);

        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == 0);
        }
    }

    public void SwitchToNextCamera()
    {
        cameras[currentIndex].enabled = false;

        currentIndex = (currentIndex + 1) % cameras.Length;

        cameras[currentIndex].enabled = true;
    }
}
