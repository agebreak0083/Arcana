using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// NextBattle 버튼에 연결할 스크립트
/// 전투 종료 후 다음 전투로 진행하거나 씬을 재시작합니다.
/// </summary>
public class NextBattleButton : MonoBehaviour
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
        if(BattleMapManager.Instance != null && BattleMapManager.Instance.currentPhase == BattleMapPhase.BATTLE_PHASE)
        {
            BattleMapManager.Instance.ChangeCurrentPhase(BattleMapPhase.END_PHASE);
        }
        else
        {
            ReturnToTactics(); 
        }
    }

    /// <summary>
    /// 타이틀 화면으로 돌아가기
    /// </summary>
    public void ReturnToTitle()
    {
        Debug.Log("타이틀 화면으로 이동");
        SceneManager.LoadScene("IntroScene"); 
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
