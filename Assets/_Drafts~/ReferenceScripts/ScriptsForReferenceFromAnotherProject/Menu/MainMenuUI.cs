using UnityEngine;
using Eflatun.SceneReference;
using UnityEngine.UI;
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] SceneReference startingLevel;
    [SerializeField] Button _startButton;
    [SerializeField] Button _quitButton;

    private void Awake()
    {
        _startButton.onClick.AddListener(() => Loader.Load(startingLevel));
        _quitButton.onClick.AddListener(() => Helper.QuitGame());
        Time.timeScale = 1f;
    }
}
