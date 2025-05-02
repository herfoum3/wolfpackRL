#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MalbersAnimations.Controller
{
    public class MenuCreateZone
    {
        [MenuItem("GameObject/Malbers Animations/Create New Zone", false, 10)]
        static void CreateZoneGameObject()
        {
            GameObject newObject = new("New Zone");
            newObject.AddComponent<BoxCollider>();
            newObject.AddComponent<Zone>();

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