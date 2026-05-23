using UnityEngine;

public class ScoreUi : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI scoreText;

    private void Awake()
    {
        scoreText.text = "0";
    }

    private void Start(){
        UpdateScoreText();
    }

    public void UpdateScoreText(){
        StartCoroutine(UpdateScoreNextFrame());
    }

    System.Collections.IEnumerator UpdateScoreNextFrame(){
        yield return null;
        scoreText.text = GameManager.Instance.Score.ToString();
    }
}
