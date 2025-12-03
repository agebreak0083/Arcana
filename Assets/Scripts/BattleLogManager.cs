using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 로그를 관리하는 매니저
/// </summary>
public class BattleLogManager : MonoBehaviour
{
    public static BattleLogManager Instance { get; private set; }

    [Header("UI References")]
    public ScrollRect scrollRect;
    public TextMeshProUGUI logText;
    public int maxLogLines = 50; // 최대 로그 라인 수

    private List<string> logMessages = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 로그 메시지 추가
    /// </summary>
    public void AddLog(string message)
    {
        logMessages.Add($"{message}");

        // 최대 라인 수 초과 시 오래된 로그 제거
        if (logMessages.Count > maxLogLines)
        {
            logMessages.RemoveAt(0);
        }

        UpdateLogDisplay();
        ScrollToBottom();
    }

    /// <summary>
    /// 공격 로그 추가
    /// </summary>
    public void LogAttack(string attackerName, string targetName, string skillName)
    {
        AddLog($"<color=#FFD700>{attackerName}</color>이(가) <color=#FF6B6B>{targetName}</color>을(를) <color=#87CEEB>{skillName}</color>(으)로 공격했습니다.");
    }

    /// <summary>
    /// 데미지 로그 추가
    /// </summary>
    public void LogDamage(string targetName, float damage)
    {
        AddLog($"<color=#FF6B6B>{targetName}</color>이(가) <color=#FF4444>{damage:F0}</color>의 데미지를 입었습니다.");
    }

    /// <summary>
    /// 회복 로그 추가
    /// </summary>
    public void LogHeal(string targetName, float healAmount)
    {
        AddLog($"<color=#90EE90>{targetName}</color>이(가) <color=#00FF00>{healAmount:F0}</color>의 HP를 회복했습니다.");
    }

    /// <summary>
    /// 턴 시작 로그
    /// </summary>
    public void LogTurnStart(string characterName, int round, int turn)
    {
        AddLog($"<color=#FFA500>--- {characterName}의 턴 (Round {round} - Turn {turn}) ---</color>");
    }

    /// <summary>
    /// 라운드 시작 로그
    /// </summary>
    public void LogRoundStart(int round)
    {
        AddLog($"<color=#FF00FF>=== 라운드 {round} 시작 ===</color>");
    }

    /// <summary>
    /// 로그 디스플레이 업데이트
    /// </summary>
    private void UpdateLogDisplay()
    {
        if (logText != null)
        {
            logText.text = string.Join("\n", logMessages);
            // 텍스트 업데이트 후 강제로 레이아웃 재계산
            LayoutRebuilder.ForceRebuildLayoutImmediate(logText.rectTransform);
        }
    }

    /// <summary>
    /// 스크롤을 맨 아래로 이동 (Auto Scroll)
    /// </summary>
    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            StopAllCoroutines();
            StartCoroutine(ScrollToBottomCoroutine());
        }
    }

    /// <summary>
    /// 스크롤을 맨 아래로 이동하는 코루틴
    /// </summary>
    private IEnumerator ScrollToBottomCoroutine()
    {
        // 레이아웃이 완전히 업데이트될 때까지 대기
        yield return new WaitForEndOfFrame();

        // Canvas 강제 업데이트
        Canvas.ForceUpdateCanvases();

        // Content의 레이아웃 강제 재계산
        if (scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }

        // 한 프레임 더 대기
        yield return null;

        // 스크롤을 맨 아래로
        scrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// 로그 초기화
    /// </summary>
    public void ClearLog()
    {
        logMessages.Clear();
        UpdateLogDisplay();
    }
}
