using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arcana.Tactics;
using Arcana.Tactics.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Arcana.Tactics.TacticsDataManager;

public class GachaManager : MonoBehaviour
{
    public VideoOnImage videoOnImage;
    public Button ticketButton;
    public Button gacha1Button;
    public Button gacha10Button;

    // Character obtained popup UI (optional - can be null)
    public GameObject characterObtainedPopup_1;    
    public GameObject characterObtainedPopup_10;
    public Button gotoTacticsButton;
    public Button gotoIntroButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ticketButton.onClick.AddListener(OnTicketButtonClicked);
        gacha1Button.onClick.AddListener(OnGacha1ButtonClicked);
        gacha10Button.onClick.AddListener(OnGacha10ButtonClicked);
        gotoTacticsButton.onClick.AddListener(OnGotoTacticsButtonClicked);
        gotoIntroButton.onClick.AddListener(OnGotoIntroButtonClicked);
        UpdateTicketText();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnGotoTacticsButtonClicked()
    {
        SceneManager.LoadScene("TacticsScene");
    }

    void OnGotoIntroButtonClicked()
    {
        SceneManager.LoadScene("IntroScene");
    }

    void UpdateTicketText()
    {
        // userdata 에서 티켓 개수를 가져온다.
        int ticketCount = UserDataManager.Instance.currentUserData.tickets;
        ticketButton.GetComponentInChildren<TextMeshProUGUI>().text = $"티켓 : {ticketCount}";
    }

    void OnTicketButtonClicked()
    {
        // userdata 에서 티켓 개수를 증가시킨다.
        UserDataManager.Instance.AddTickets(10);
        UpdateTicketText();
    }

    void OnGacha1ButtonClicked()
    {
        if (UserDataManager.Instance.currentUserData.tickets < 1)
        {
            Debug.Log("티켓이 부족합니다.");
            return;
        }

        // TacticsDataManager가 로드되었는지 확인
        if (TacticsDataManager.Instance == null || !TacticsDataManager.Instance.isDataLoaded)
        {
            Debug.LogError("TacticsDataManager가 아직 로드되지 않았습니다.");
            return;
        }

        // userdata 에서 티켓 개수를 감소시킨다.
        UserDataManager.Instance.SpendTickets(1);
        UpdateTicketText();

        CharacterDefinition randomCharDef = GetRandomCharacter();
        if (randomCharDef != null)
        {
            //캐릭터 획득 팝업을 표시한다.            
            characterObtainedPopup_1.SetActive(true);
            characterObtainedPopup_10.SetActive(false);
            GachaCard gachaCard = characterObtainedPopup_1.GetComponentInChildren<GachaCard>();
            if (gachaCard != null)
            {
                gachaCard.ShowCharacter(randomCharDef);
            }
        }
    }

    private CharacterDefinition GetRandomCharacter()
    {
        // 1. 캐릭터 목록을 TacticsDataManager에서 가져온다.
        CharacterDefinition[] allCharacters = TacticsDataManager.Instance.GetAllCharacterDefinitions();
        if (allCharacters == null || allCharacters.Length == 0)
        {
            Debug.LogError("캐릭터 목록을 가져올 수 없습니다.");
            return null;
        }

        // 2. 캐릭터 목록에서 랜덤으로 1개를 선택한다.
        CharacterDefinition randomCharDef = allCharacters[UnityEngine.Random.Range(0, allCharacters.Length)];

        // 3. 캐릭터를 획득한다.
        // UserDataManager에 추가
        UserDataManager.Instance.AddCharacter(randomCharDef.Name);

        // CharacterPool.json에 추가 (TacticsDataManager를 통해)
        TacticsDataManager.Instance.AddCharacterToPool(randomCharDef.Name);

        return randomCharDef;
    }

    void OnGacha10ButtonClicked()
    {
        if (UserDataManager.Instance.currentUserData.tickets < 10)
        {
            Debug.Log("티켓이 부족합니다.");
            return;
        }

        // TacticsDataManager가 로드되었는지 확인
        if (TacticsDataManager.Instance == null || !TacticsDataManager.Instance.isDataLoaded)
        {
            Debug.LogError("TacticsDataManager가 아직 로드되지 않았습니다.");
            return;
        }

        // userdata 에서 티켓 개수를 감소시킨다.
        UserDataManager.Instance.SpendTickets(10);
        UpdateTicketText();

        StartCoroutine(ShowGacha10Popup());
    }

    IEnumerator ShowGacha10Popup()
    {
        characterObtainedPopup_1.SetActive(false);
        characterObtainedPopup_10.SetActive(false);

        // 배경 영상 재생 시작 (코루틴 사용)
        videoOnImage.videoPlayer.Play();

        // 배경 영상 재생 시간만큼 대기
        yield return new WaitUntil(() => videoOnImage.videoPlayer.isPlaying == false);

        // 배경 영상 재생 완료 후 캐릭터 획득 팝업 표시
        characterObtainedPopup_10.SetActive(true);  

        // 10번 캐치
        GachaCard[] gachaCards = characterObtainedPopup_10.GetComponentsInChildren<GachaCard>();
        if (gachaCards == null || gachaCards.Length == 0)
        {
            yield break;
        }
        
        foreach (var gachaCard in gachaCards)
        {
            gachaCard.gameObject.SetActive(false);
        }
        
        foreach (var gachaCard in gachaCards)
        {
            CharacterDefinition randomCharDef = GetRandomCharacter();
            if (randomCharDef != null)
            {
                gachaCard.ShowCharacter(randomCharDef);
            }
            yield return new WaitForSeconds(0.5f);
        }
        
        yield return null;
    }
}
