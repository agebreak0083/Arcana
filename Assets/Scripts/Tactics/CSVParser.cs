using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcana.Tactics
{
    /// <summary>
    /// CSV 파일 파싱 유틸리티
    /// </summary>
    public static class CSVParser
    {
        /// <summary>
        /// 캐릭터 CSV 파싱
        /// </summary>
        public static CharacterDefinition[] ParseCharacterCSV(string csvText)
        {
            var list = new List<CharacterDefinition>();
            string[] lines = csvText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Assume header is first line or check content
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Simple comma split (assuming no commas in values)
                string[] parts = line.Split(',');

                // Skip header (Name,Portrait,Class,Cost)
                if (parts.Length >= 4 && parts[0] == "Name" && parts[2] == "Class")
                    continue;

                if (parts.Length >= 4)
                {
                    CharacterDefinition def = new CharacterDefinition();
                    def.Name = parts[0].Trim();
                    def.Portrait = parts[1].Trim();
                    def.Class = parts[2].Trim();

                    if (int.TryParse(parts[3].Trim(), out int cost))
                    {
                        def.Cost = cost;
                    }
                    else
                    {
                        def.Cost = 2; // default
                    }

                    // Model 필드는 CSV에 없을 수 있으므로 기본값으로 빈 문자열
                    def.Model = parts.Length >= 5 ? parts[4].Trim() : "";

                    list.Add(def);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 클래스 CSV 파싱
        /// </summary>
        public static List<ClassInfo> ParseClassCSV(string csvText)
        {
            var list = new List<ClassInfo>();
            string[] lines = csvText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Header: name,description,cost,model,advantage,hp,physicalAttack,physicalDefense,magicalAttack,magicalDefense,accuracy,evasion,criticalRate,guardRate,actionSpeed,actionPoint,passivePoint

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Split(',');

                // Skip header based on content
                if (parts.Length > 0 && parts[0] == "name") continue;

                if (parts.Length >= 17) // Ensure we have enough columns
                {
                    try
                    {
                        ClassInfo info = new ClassInfo();
                        info.name = parts[0].Trim();
                        info.description = parts[1].Trim();
                        info.cost = int.Parse(parts[2].Trim());
                        info.model = parts[3].Trim();

                        // Advantage (semicolon separated)
                        string advRaw = parts[4].Trim();
                        if (!string.IsNullOrEmpty(advRaw))
                        {
                            info.advantage = new List<string>(advRaw.Split(';'));
                        }
                        else
                        {
                            info.advantage = new List<string>();
                        }

                        info.stats = new ClassStats();
                        info.stats.hp = parts[5].Trim();
                        info.stats.physicalAttack = parts[6].Trim();
                        info.stats.physicalDefense = parts[7].Trim();
                        info.stats.magicalAttack = parts[8].Trim();
                        info.stats.magicalDefense = parts[9].Trim();
                        info.stats.accuracy = parts[10].Trim();
                        info.stats.evasion = parts[11].Trim();
                        info.stats.criticalRate = parts[12].Trim();
                        info.stats.guardRate = parts[13].Trim();
                        info.stats.actionSpeed = parts[14].Trim();
                        info.stats.actionPoint = int.Parse(parts[15].Trim());
                        info.stats.passivePoint = int.Parse(parts[16].Trim());

                        list.Add(info);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing class CSV line: {line}. Error: {e.Message}");
                    }
                }
            }
            return list;
        }
    }
}

