using System;
using System.Collections.Generic;
using Arcana.Tactics.Data;
using UnityEngine;

namespace Arcana.Tactics
{
    /// <summary>
    /// Tactics 관련 데이터 모델 클래스들
    /// </summary>
    
    [System.Serializable]
    public class CharacterDefinition
    {
        public string Name;
        public string Portrait;
        public string Model;
        public string Class;
        public int Cost;
        public string Voice_Skill;
    }

    [System.Serializable]
    public class CharacterPoolItem
    {
        public string Name;
    }

    [System.Serializable]
    public class ClassListWrapper
    {
        public ClassInfo[] classes;
    }

    [System.Serializable]
    public class ClassInfo
    {
        public string name;
        public string description;
        public List<string> advantage;
        public ClassStats stats;
    }

    /// <summary>
    /// Tactics 파일 데이터 구조 (Save/Load 공용)
    /// </summary>
    [System.Serializable]
    public class TacticsFileData
    {
        public string key;
        public string username;            
        public int score = 0;
        public int winCount = 0;
        public int loseCount = 0;
        public PositionData[] positions;
    }

    [System.Serializable]
    public class PositionData
    {
        public string position;
        public string name;
        public TacticsData[] tactics;
    }

    [System.Serializable]
    public class TacticsData
    {
        public string characterClass;  // Save용 필드명

        [System.NonSerialized]
        private string _class;  // Load용 필드명 (@class)

        // JSON에서 "class" 필드를 읽을 때 사용
        public string @class
        {
            get => string.IsNullOrEmpty(_class) ? characterClass : _class;
            set
            {
                _class = value;
                characterClass = value;
            }
        }

        public TacticRowData[] plan;
    }

    [System.Serializable]
    public class TacticRowData
    {
        public string skill;
        public string condition1;
        public string condition2;
    }

    /// <summary>
    /// CharacterPool 데이터 구조 (Save/Load 공용)
    /// </summary>
    [System.Serializable]
    public class CharacterPoolData
    {
        public string Name;
        public TacticsData[] tactics;
    }

    public class FormationLoadResult
    {
        public string username;
        public int score = 0;
        public int winCount = 0;
        public int loseCount = 0;
        public CharacterData[] unitSlots;
        public Dictionary<string, TacticsPlan> codingData;
    }

    /// <summary>
    /// JsonUtility용 Wrapper (배열 직렬화를 위해 필요)
    /// </summary>
    [System.Serializable]
    public class CharacterPoolDataWrapper
    {
        public CharacterPoolData[] characters;
    }

    /// <summary>
    /// TacticsRecommend.json 데이터 구조
    /// </summary>
    [System.Serializable]
    public class TacticsRecommendWrapper
    {
        public TacticsRecommendClass[] classes;
    }

    [System.Serializable]
    public class TacticsRecommendClass
    {
        public string name;
        public TacticsRecommendTactics[] tactics;
    }

    [System.Serializable]
    public class TacticsRecommendTactics
    {
        public string characterClass;
        public TacticsRecommendRow[] plan;
    }

    [System.Serializable]
    public class TacticsRecommendRow
    {
        public string skill;
        public string condition1;
        public string condition2;
    }

    /// <summary>
    /// Helper for array JSONs
    /// </summary>
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }
}

