using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arcana.Tactics
{
    /// <summary>
    /// 랭킹 관련 기능 관리
    /// </summary>
    public static class RankingManager
    {
        /// <summary>
        /// 점수를 받아서 전체 랭킹을 반환합니다 (비동기)
        /// </summary>
        /// <param name="score">랭킹을 확인할 점수</param>
        /// <param name="onComplete">완료 콜백 (랭킹, 0이면 데이터 로드 실패)</param>
        public static void GetRanking(int score, System.Action<int> onComplete)
        {
            if (JSONBinManager.Instance == null || !JSONBinManager.Instance.isInitialized)
            {
                Debug.LogWarning("JSONBinManager가 초기화되지 않았습니다.");
                onComplete?.Invoke(0);
                return;
            }

            // JSONBinManager에서 모든 Tactics 데이터 로드
            JSONBinManager.Instance.GetAllTactics((success, database) =>
            {
                if (!success || database == null || database.tactics == null)
                {
                    Debug.LogWarning("Tactics 데이터 로드 실패");
                    onComplete?.Invoke(0);
                    return;
                }

                // 모든 Tactics JSON을 파싱하여 TacticsFileData로 변환
                Dictionary<string, int> userScores = new Dictionary<string, int>();

                foreach (JSONBinManager.TacticsData tactic in database.tactics)
                {
                    if (string.IsNullOrEmpty(tactic.tacticsJson)) continue;

                    try
                    {
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tactic.tacticsJson);
                        if (tacticsData != null && !string.IsNullOrEmpty(tacticsData.username))
                        {
                            // key 값 설정 (JSONBinManager의 key 사용)
                            if (string.IsNullOrEmpty(tacticsData.key))
                            {
                                tacticsData.key = tactic.key;
                            }
                            
                            userScores[tacticsData.key] = tacticsData.score;
                            
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Tactics JSON 파싱 실패: {e.Message}");
                        continue;
                    }
                }

                // score가 높은 순서대로 정렬
                var sortedScores = userScores.Values.OrderByDescending(x => x).ToList();

                // 주어진 score보다 높은 점수를 가진 사용자 수를 세어서 랭킹 계산
                // 예: [100, 90, 80, 70], 내 점수 85 -> 랭킹 3 (100, 90이 더 높음)
                int ranking = sortedScores.Count(s => s > score) + 1;

                // 가장 높은 score를 가진 사용자의 이름과 score 가져오기
                string highestScoreUsername = userScores.Keys.FirstOrDefault(k => userScores[k] == sortedScores.First());
                int highestScore = sortedScores.First();

                Debug.Log($"가장 높은 score를 가진 사용자: {highestScoreUsername}, score: {highestScore}");

                onComplete?.Invoke(ranking);
            });
        }

        /// <summary>
        /// 모든 유저의 TacticsData를 score 순으로 가져옵니다 (비동기)
        /// </summary>
        /// <param name="onComplete">완료 콜백 (유저 데이터 리스트: username, score, winCount, loseCount)</param>
        public static void GetAllUsersSortedByScore(System.Action<List<(string username, int score, int winCount, int loseCount)>> onComplete)
        {
            if (JSONBinManager.Instance == null || !JSONBinManager.Instance.isInitialized)
            {
                Debug.LogWarning("JSONBinManager가 초기화되지 않았습니다.");
                onComplete?.Invoke(new List<(string, int, int, int)>());
                return;
            }

            // JSONBinManager에서 모든 Tactics 데이터 로드
            JSONBinManager.Instance.GetAllTactics((success, database) =>
            {
                if (!success || database == null || database.tactics == null)
                {
                    Debug.LogWarning("Tactics 데이터 로드 실패");
                    onComplete?.Invoke(new List<(string, int, int, int)>());
                    return;
                }

                // 모든 Tactics JSON을 파싱하여 TacticsFileData로 변환
                Dictionary<string, (int score, int winCount, int loseCount)> userData = new Dictionary<string, (int, int, int)>();

                foreach (JSONBinManager.TacticsData tactic in database.tactics)
                {
                    if (string.IsNullOrEmpty(tactic.tacticsJson)) continue;

                    try
                    {
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tactic.tacticsJson);                        
                        if (tacticsData != null && !string.IsNullOrEmpty(tacticsData.username))
                        {
                            userData[tacticsData.username] = (tacticsData.score, tacticsData.winCount, tacticsData.loseCount);                            
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Tactics JSON 파싱 실패: {e.Message}");
                        continue;
                    }
                }

                // score가 높은 순서대로 정렬하여 리스트로 변환
                var sortedUsers = userData
                    .Select(kvp => (username: kvp.Key, score: kvp.Value.score, winCount: kvp.Value.winCount, loseCount: kvp.Value.loseCount))
                    .OrderByDescending(x => x.score)
                    .ToList();

                onComplete?.Invoke(sortedUsers);
            });
        }

        /// <summary>
        /// 사용자의 랭킹을 가져옵니다 (비동기)
        /// </summary>
        /// <param name="key">랭킹을 확인할 사용자 key</param>
        /// <param name="onComplete">완료 콜백 (랭킹, 0이면 사용자를 찾을 수 없음)</param>
        public static void GetRankingByKey(string key, System.Action<int> onComplete)
        {
            if (JSONBinManager.Instance == null || !JSONBinManager.Instance.isInitialized)
            {
                Debug.LogWarning("JSONBinManager가 초기화되지 않았습니다.");
                onComplete?.Invoke(0);
                return;
            }

            // JSONBinManager에서 모든 Tactics 데이터 로드
            JSONBinManager.Instance.GetAllTactics((success, database) =>
            {
                if (!success || database == null || database.tactics == null)
                {
                    Debug.LogWarning("Tactics 데이터 로드 실패");
                    onComplete?.Invoke(0);
                    return;
                }

                // 모든 Tactics JSON을 파싱하여 TacticsFileData로 변환
                Dictionary<string, int> userScores = new Dictionary<string, int>();

                foreach (JSONBinManager.TacticsData tactic in database.tactics)
                {
                    if (string.IsNullOrEmpty(tactic.tacticsJson)) continue;

                    try
                    {
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tactic.tacticsJson);
                        if (tacticsData != null && !string.IsNullOrEmpty(tacticsData.username))
                        {
                            // key 값 설정 (JSONBinManager의 key 사용)
                            if (string.IsNullOrEmpty(tacticsData.key))
                            {
                                tacticsData.key = tactic.key;
                            }

                            userScores[tacticsData.key] = tacticsData.score;                           
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Tactics JSON 파싱 실패: {e.Message}");
                        continue;
                    }
                }

                // score가 높은 순서대로 정렬
                var sortedUsers = userScores.OrderByDescending(x => x.Value).ToList();

                // 주어진 key의 순서 찾기
                for (int i = 0; i < sortedUsers.Count; i++)
                {
                    if (sortedUsers[i].Key == key)
                    {
                        onComplete?.Invoke(i + 1); // 1부터 시작하는 랭킹
                        return;
                    }
                }

                // 사용자를 찾을 수 없음
                onComplete?.Invoke(0);
            });
        }
    }
}

