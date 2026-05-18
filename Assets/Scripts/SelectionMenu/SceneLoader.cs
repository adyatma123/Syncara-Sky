using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// 1. Create a serializable class/struct to hold your stage parameters
// so they can be viewed and edited in the Unity Inspector
[System.Serializable]
public class StageData
{
    public string stageName;
    public string location;
    public string date;
}

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("Current Loading Data")]
    // 2. This will hold the data for the scene currently being loaded
    public StageData currentStageData;

    private string sceneToLoad;
    private AsyncOperation currentAsyncOperation;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this method from your level triggers to start loading.
    /// It now accepts the StageData which you can set in the Inspector of the calling script.
    /// </summary>
    public void LoadNewScene(string sceneName, StageData stageData)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name provided is null or empty!");
            return;
        }

        sceneToLoad = sceneName;
        currentStageData = stageData; // Save the parameters to read later

        SceneManager.LoadScene("LoadingScene");
    }

    public AsyncOperation StartLoadingOperation()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Attempted to start loading operation, but no scene was queued.");
            return null;
        }

        StartCoroutine(LoadAsynchronously(sceneToLoad));
        return currentAsyncOperation;
    }

    IEnumerator LoadAsynchronously(string sceneName)
    {
        sceneToLoad = "";
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        currentAsyncOperation = operation;

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            yield return null;
        }
    }
}