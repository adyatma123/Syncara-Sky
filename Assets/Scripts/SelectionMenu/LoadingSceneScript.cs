using UnityEngine;

public class LoadingSceneManager : MonoBehaviour
{
    void Start()
    {
        if (SceneLoader.Instance != null)
        {
            // Tell the persistent manager to start the actual async loading
            //SceneLoader.Instance.StartLoading();
        }
        else
        {
            Debug.LogError("SceneLoader instance not found!");
        }
    }
}