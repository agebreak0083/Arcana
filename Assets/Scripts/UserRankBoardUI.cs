using TMPro;
using UnityEngine;

public class UserRankBoardUI : MonoBehaviour
{
    public TextMeshProUGUI rankingText;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI scoreText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateUI(int ranking, string playerName, int score)
    {
        rankingText.text = ranking.ToString();
        playerNameText.text = playerName;
        scoreText.text = score.ToString();
    }
}
