using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // THÊM HÀM NÀY VÀO
    public void LoadSceneAndOpenExplore(string sceneName)
    {
        // Lưu một cờ hiệu báo rằng "Hãy mở tab Explore"
        PlayerPrefs.SetInt("OpenExploreTab", 1);
        PlayerPrefs.Save();
        
        // Chuyển Scene
        SceneManager.LoadScene(sceneName);
    }
}