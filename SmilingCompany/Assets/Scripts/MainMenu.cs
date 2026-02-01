using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        // Load the game scene (SampleScene)
        SceneManager.LoadScene("SampleScene");
    }

    public void QuitGame()
    {
        // This works in a built game, not in the editor
        Application.Quit();

        // For testing in editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
