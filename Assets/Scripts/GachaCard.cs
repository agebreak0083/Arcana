using System;
using Arcana.Tactics;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GachaCard : MonoBehaviour
{
    public Image portrait;
    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
     
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void ShowCharacter(CharacterDefinition character)
    {
        if (portrait == null)
        {
            Debug.LogError("portraits is not set");
            return;
        }

        gameObject.SetActive(true);

        //Resources/Portraits 폴더에 있는 이미지를 로드한다.
        string portraitPath = "Portraits/" + character.Portrait;
        Sprite portraitSprite = Resources.Load<Sprite>(portraitPath);
        if(portraitSprite == null)  
        {
            Debug.LogError("portrait is not set: " + portraitPath);
            return;
        }
        portrait.sprite = portraitSprite;
        audioSource.Play();
        
        // DoTween 사용. Start : Scale 3 -> To : Scale 1. 0.5초에 걸쳐서 변경.
        gameObject.transform.localScale = new Vector3(3, 3, 3);
        gameObject.transform.DOScale(new Vector3(1, 1, 1), 0.5f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            //gameObject.transform.DOScale(new Vector3(1, 1, 1), 0.5f).SetEase(Ease.InOutSine);
            
        });
    }
}
