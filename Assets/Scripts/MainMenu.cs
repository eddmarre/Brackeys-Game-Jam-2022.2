using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private int gameBuildIndex;

    public void StartGame()
    {
        SceneManager.LoadScene(gameBuildIndex);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}