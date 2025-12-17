using System.Collections.Generic;
using Arcana.Tactics;
using UnityEngine;

public class LeaderBoardUI : MonoBehaviour
{
    public GameObject userRankBoardPrefab;
    public Transform userRankBoardContainer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateLeaderBoard()
    {
        gameObject.SetActive(true);

        // 기존 UI 요소들 제거
        foreach (Transform child in userRankBoardContainer)
        {
            Destroy(child.gameObject);
        }

        // 모든 유저의 TacticsData를 score 순으로 가져오기
        TacticsDataManager.Instance.GetAllUsersSortedByScore(onComplete);

        void onComplete(List<(string username, int score, int winCount, int loseCount)> users)
        {
            if (users == null || users.Count == 0)
            {
                Debug.LogWarning("리더보드 데이터가 없습니다.");
                return;
            }

            // 각 유저마다 Prefab 생성하고 업데이트
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                GameObject userRankBoard = Instantiate(userRankBoardPrefab, userRankBoardContainer);
                userRankBoard.GetComponent<UserRankBoardUI>().UpdateUI(i + 1, user.username, user.score);
            }

            Debug.Log($"리더보드 업데이트 완료: {users.Count}명의 유저");
        }
    }
}
