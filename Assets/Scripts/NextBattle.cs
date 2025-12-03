using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// NextBattle 버튼에 연결할 스크립트
/// 전투 종료 후 다음 전투로 진행하거나 씬을 재시작합니다.
/// </summary>
public class NextBattle : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnNextBattleClicked);
        }
        else
        {
            Debug.LogError("NextBattle: Button 컴포넌트를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// NextBattle 버튼 클릭 시 호출
    /// </summary>
    public void OnNextBattleClicked()
    {
        Debug.Log("NextBattle 버튼 클릭: 전투 씬 재시작");

        ReturnToTactics(); 
    }

    /// <summary>
    /// 현재 전투 씬 재시작
    /// </summary>
    private void RestartBattle()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// 다음 전투 씬으로 이동 (구현 예정)
    /// </summary>
    private void LoadNextBattle()
    {
        // TODO: 다음 전투 씬 이름을 결정하는 로직
        // 예: 스테이지 번호를 관리하는 GameManager에서 가져오기

        // 임시로 BattleScene 재시작
        SceneManager.LoadScene("BattleScene");
    }

    /// <summary>
    /// 타이틀 화면으로 돌아가기
    /// </summary>
    public void ReturnToTitle()
    {
        Debug.Log("타이틀 화면으로 이동");
        SceneManager.LoadScene("TitleScene"); // 타이틀 씬 이름에 맞게 수정
    }

    /// <summary>
    /// Tactics 씬으로 돌아가기
    /// </summary>
    public void ReturnToTactics()
    {
        Debug.Log("Tactics 씬으로 이동");
        SceneManager.LoadScene("TacticsScene");
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnNextBattleClicked);
        }
    }
}
