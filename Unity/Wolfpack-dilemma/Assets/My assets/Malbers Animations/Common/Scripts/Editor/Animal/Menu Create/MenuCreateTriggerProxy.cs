#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace MalbersAnimations.Utilities
{
    public class MenuCreateTriggerProxy
    {
        [MenuItem("GameObject/Malbers Animations/Create Trigger Proxy", false, 10)]
        static void CreateZoneGameObject()
        {
            GameObject newObject = new GameObject("New Trigger Proxy");
            newObject.AddComponent<BoxCollider>();
            newObject.AddComponent<TriggerProxy>();

            if (Selection.activeGameObject != null)
            {
                newObject.transform.SetParent(Selection.activeGameObject.transform);
                newObject.transform.ResetLocal();
            }

            Selection.activeGameObject = newObject;
        }
    }
}

#endif
