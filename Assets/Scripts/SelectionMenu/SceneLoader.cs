using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    // Singleton pattern
    public static SceneLoader Instance;

    // Nama scene target yang akan dimuat secara async
    private string sceneToLoad;

    // Objek operasi asinkron saat ini. Ini akan diakses oleh LoadingUIHandler.cs
    private AsyncOperation currentAsyncOperation;

    private void Awake()
    {
        // Implementasi Singleton
        if (Instance == null)
        {
            Instance = this;
            // Penting: Memastikan objek ini tidak hancur saat scene berpindah
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Hancurkan duplikat
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Dipanggil dari scene lain (misalnya VhcChgr) untuk memulai proses loading.
    /// </summary>
    /// <param name="sceneName">Nama scene yang akan dimuat setelah LoadingScene.</param>
    public void LoadNewScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name provided is null or empty!");
            return;
        }

        sceneToLoad = sceneName;
        // Langkah 1: Muat Scene Loading secara INSTAN
        SceneManager.LoadScene("LoadingScene");
        // Langkah 2: Ketika "LoadingScene" dimuat, skrip LoadingUIHandler di sana 
        // akan memanggil StartLoadingOperation() untuk melanjutkan ke sceneToLoad.
    }

    /// <summary>
    /// Dipanggil oleh LoadingUIHandler.cs setelah 'LoadingScene' selesai dimuat.
    /// Memulai operasi loading asinkron dan mengembalikan objek operasi tersebut.
    /// </summary>
    /// <returns>Objek AsyncOperation untuk melacak progress.</returns>
    public AsyncOperation StartLoadingOperation()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Attempted to start loading operation, but no scene was queued.");
            return null;
        }

        // Memulai coroutine loading dan menyimpan referensi operasi
        StartCoroutine(LoadAsynchronously(sceneToLoad));

        // Kembalikan operasi yang sedang berjalan ke LoadingUIHandler untuk pembaruan UI
        return currentAsyncOperation;
    }

    IEnumerator LoadAsynchronously(string sceneName)
    {
        // Memastikan sceneToLoad dikosongkan setelah digunakan untuk mencegah pemuatan ulang
        sceneToLoad = "";

        // Panggil operasi loading yang sebenarnya
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // Simpan referensi operasi
        currentAsyncOperation = operation;

        // Mencegah scene baru aktif secara otomatis pada 90%
        operation.allowSceneActivation = false;

        // Coroutine hanya menunggu hingga operasi selesai (yaitu mencapai 0.9)
        while (!operation.isDone)
        {
            yield return null;
        }
    }
}
