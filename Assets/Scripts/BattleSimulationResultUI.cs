using System;
using Arcana.Tactics;
using Arcana.Tactics.UI;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Arcana.Tactics.TacticsDataManager;

public class BattleSimulationResultUI : MonoBehaviour
{
    public FormationSlotUI[] playerFormationSlots;
    public FormationSlotUI[] enemyFormationSlots;
    public TextMeshProUGUI battleResultText;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI enemyNameText;
    public Button closeButton;
    public Button startBattleButton;
    public GameObject hpbar_Player; 
    public GameObject hpbar_Enemy; 
    BattleSimulationResult battleSimulationResult = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        closeButton.onClick.AddListener(OnCloseButtonClicked);        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCloseButtonClicked()
    {
        transform.gameObject.SetActive(false);
    }    

     // 시뮬 결과가 나오기 전까지 초기화 
    public void InitializeUI(string playerName, string enemyName)
    {
        battleSimulationResult = new BattleSimulationResult();
        battleSimulationResult.playerName = playerName;
        battleSimulationResult.enemyName = enemyName;        

        UpdateBattleResult();
        UpdateFormation();
        UpdateHPBarUI();

        startBattleButton.interactable = false;
        closeButton.interactable = false;
    }
    internal void UpdateUI()
    {
        battleSimulationResult = BattleManager.battleSimulationResult;
        if(battleSimulationResult == null)
        {
            return;
        }

        UpdateBattleResult();
        UpdateFormation();
        UpdateHPBarUI();

        startBattleButton.interactable = true;
        closeButton.interactable = true;

        // 아이리스에게 게임 상황 데이터 전달
        IRISUIManager.Instance.ShowIrisUI(MessageToIRIS.BATTLE_SIMULATION_RESULT, new BattleSimulationGameStatusData(battleSimulationResult));
    }

    void UpdateBattleResult()
    {
        if(battleSimulationResult == null)
        {
            return;
        }
        
        if(battleSimulationResult.isPlayerWin)
        {
            battleResultText.text = "VICTORY";            
        }
        else
        {
            battleResultText.text = "DEFEAT";
        }       

        playerNameText.text = battleSimulationResult.playerName;
        enemyNameText.text = battleSimulationResult.enemyName;
    }

    void UpdateFormation()
    {
        for(int i = 0; i < playerFormationSlots.Length; i++)
        {
            playerFormationSlots[i].UpdateState(null);
        }
        for(int i = 0; i < enemyFormationSlots.Length; i++)
        {
            enemyFormationSlots[i].UpdateState(null);
        }

        FormationLoadResult playerFormationLoadResult = battleSimulationResult.playerFormationLoadResult;
        FormationLoadResult enemyFormationLoadResult = battleSimulationResult.enemyFormationLoadResult;
        if(playerFormationLoadResult != null)
        {
            for(int i = 0; i < playerFormationLoadResult.unitSlots.Length; i++)
            {
                var character = playerFormationLoadResult.unitSlots[i];
                if(character != null)
                {
                    playerFormationSlots[i].UpdateState(character);
                }
            }
        }
        if(enemyFormationLoadResult != null)
        {
            for(int i = 0; i < enemyFormationLoadResult.unitSlots.Length; i++)
            {
                var character = enemyFormationLoadResult.unitSlots[i];            
                if(character != null)
                {
                    enemyFormationSlots[i].UpdateState(character);
                }
            }
        }
    }

    void UpdateHPBarUI()
    {
        hpbar_Player.GetComponent<Slider>().value = (float)battleSimulationResult.playerHP_Remaining / battleSimulationResult.playerHP_Max;
        hpbar_Player.GetComponentInChildren<TextMeshProUGUI>().text = $"{battleSimulationResult.playerHP_Remaining} / {battleSimulationResult.playerHP_Max}";
    
        hpbar_Enemy.GetComponent<Slider>().value = (float)battleSimulationResult.enemyHP_Remaining / battleSimulationResult.enemyHP_Max;
        hpbar_Enemy.GetComponentInChildren<TextMeshProUGUI>().text = $"{battleSimulationResult.enemyHP_Remaining} / {battleSimulationResult.enemyHP_Max}";
    }

   
}
