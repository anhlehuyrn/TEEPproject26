using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene loading

public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Loads a scene using its exact string name.
    /// You can hook this up to a UI Button's OnClick event.
    /// </summary>
    /// <param name="sceneName">The exact name of the scene to load.</param>
    public void LoadSceneByName(string sceneName)
    {
        // Check if the string is empty to avoid unnecessary errors
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is empty! Please provide a valid scene name.");
        }
    }
}