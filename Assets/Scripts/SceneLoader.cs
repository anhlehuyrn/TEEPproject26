using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Hàm này cho phép bạn gõ tên Scene muốn đến trực tiếp từ ngoài màn hình Unity
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}