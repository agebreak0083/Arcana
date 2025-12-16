using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Arcana.Tactics.Data
{
    [Serializable]
    public class BattleSetting
    {
        private const string GOOGLE_SHEET_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vT01LXMR-Ug_Pk9S3PGnOeEt768dzf2TrgJz1xI_e2n2YW3O1LMaT49NmupyLQrYXxIN8-OoANuxhEJ/pub?output=csv";

        public static float GUARD_EFFECT_LOW = 0.7f;
        public static float GUARD_EFFECT_MEDIUM = 0.5f;
        public static float GUARD_EFFECT_HIGH = 0.3f;
        public static float GUARD_EFFECT_MAXIMUM = 0.0f;
        public static int MAX_ROUNDS = 3;
        public static int TICKET_FOR_WIN = 10;
        public static int TICKET_FOR_LOSE = 5;        
        public static float DAMAGE_MULTIPLIER = 1.0f;

        [Header("내부 데이터")]
        public static string enemyTactics = ""; // 택틱스씬에서 정해진 적 택틱스 이름

        /// <summary>
        /// 구글 시트에서 CSV를 다운로드하여 BattleSetting 값을 설정합니다.
        /// </summary>
        public static IEnumerator LoadFromGoogleSheet(TextMeshProUGUI textComponent)
        {
            Debug.Log("BattleSetting: 구글 시트에서 설정값 로딩 시작...");

            using (UnityWebRequest request = UnityWebRequest.Get(GOOGLE_SHEET_CSV_URL))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string csvData = request.downloadHandler.text;
                    ParseAndApplyCSV(csvData);                    
                    Debug.Log("BattleSetting: 구글 시트에서 설정값 로딩 완료!");
                }
                else
                {
                    Debug.LogError($"BattleSetting: 구글 시트 로딩 실패 - {request.error}");
                    Debug.LogWarning("BattleSetting: 기본값을 사용합니다.");
                }

                PrintAllSettings(textComponent);
            }
        }

        /// <summary>
        /// CSV 데이터를 파싱하여 BattleSetting 값에 적용합니다.
        /// </summary>
        private static void ParseAndApplyCSV(string csvData)
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();

            // CSV 파싱 (첫 번째 줄은 헤더이므로 건너뜀)
            string[] lines = csvData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 1; i < lines.Length; i++) // 첫 번째 줄(헤더) 건너뛰기
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // CSV 파싱 (쉼표로 분리)
                string[] parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    settings[key] = value;
                }
            }

            // 설정값 적용
            foreach (var kvp in settings)
            {
                ApplySetting(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 키-값 쌍을 BattleSetting 필드에 적용합니다.
        /// </summary>
        private static void ApplySetting(string key, string value)
        {
            try
            {
                switch (key)
                {
                    case "GUARD_EFFECT_LOW":
                        if (float.TryParse(value, out float low))
                            GUARD_EFFECT_LOW = low;
                        break;
                    case "GUARD_EFFECT_MEDIUM":
                        if (float.TryParse(value, out float medium))
                            GUARD_EFFECT_MEDIUM = medium;
                        break;
                    case "GUARD_EFFECT_HIGH":
                        if (float.TryParse(value, out float high))
                            GUARD_EFFECT_HIGH = high;
                        break;
                    case "GUARD_EFFECT_MAXIMUM":
                        if (float.TryParse(value, out float maximum))
                            GUARD_EFFECT_MAXIMUM = maximum;
                        break;
                    case "MAX_ROUNDS":
                        if (int.TryParse(value, out int maxRounds))
                            MAX_ROUNDS = maxRounds;
                        break;
                    case "TICKET_FOR_WIN":
                        if (int.TryParse(value, out int ticketWin))
                            TICKET_FOR_WIN = ticketWin;
                        break;
                    case "TICKET_FOR_LOSE":
                        if (int.TryParse(value, out int ticketLose))
                            TICKET_FOR_LOSE = ticketLose;
                        break;
                    case "DAMAGE_MULTIPLIER":
                        if (float.TryParse(value, out float damageMult))
                            DAMAGE_MULTIPLIER = damageMult;
                        Debug.Log($"BattleSetting: DAMAGE_MULTIPLIER 설정: {DAMAGE_MULTIPLIER}");
                        break;
                    default:
                        Debug.LogWarning($"BattleSetting: 알 수 없는 키 '{key}'를 무시합니다.");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"BattleSetting: '{key}' 값을 설정하는 중 오류 발생 - {e.Message}");
            }
        }

        public static void PrintAllSettings(TextMeshProUGUI textComponent)
        {
            if (textComponent == null)
            {
                Debug.LogError("BattleSetting: textComponent is null");
                return;
            }

            textComponent.text += $"GUARD_EFFECT_LOW: {GUARD_EFFECT_LOW}\nGUARD_EFFECT_MEDIUM: {GUARD_EFFECT_MEDIUM}\nGUARD_EFFECT_HIGH: {GUARD_EFFECT_HIGH}\nGUARD_EFFECT_MAXIMUM: {GUARD_EFFECT_MAXIMUM}\nMAX_ROUNDS: {MAX_ROUNDS}\nTICKET_FOR_WIN: {TICKET_FOR_WIN}\nTICKET_FOR_LOSE: {TICKET_FOR_LOSE}\nDAMAGE_MULTIPLIER: {DAMAGE_MULTIPLIER}";
        }
    }
}
