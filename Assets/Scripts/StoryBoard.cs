using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryBoard : MonoBehaviour, IPointerClickHandler
{
    private Image storyBoardImage;
    public Sprite[] storyBoardSprites;
    public AudioClip[] storyBoardAudioClips;
    private AudioSource audioSource;
    private int currentSpriteIndex = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created    
    void Start()
    {
        storyBoardImage = GetComponent<Image>();
        storyBoardImage.sprite = storyBoardSprites[0];
        
        // Image의 Raycast Target이 활성화되어 있어야 클릭이 감지됩니다
        if (storyBoardImage != null)
        {
            storyBoardImage.raycastTarget = true;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = storyBoardAudioClips[0];
        audioSource.Play();
    }

    /// <summary>
    /// 이미지 클릭 시 호출되는 메서드
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 이미지를 터치하면 다음 이미지로 넘어감 
        currentSpriteIndex++;
        if (currentSpriteIndex >= storyBoardSprites.Length)
        {
            currentSpriteIndex = 0;
            SceneManager.LoadScene("TacticsScene");
        }
        else
        {
            storyBoardImage.sprite = storyBoardSprites[currentSpriteIndex];

            if(storyBoardAudioClips.Length > currentSpriteIndex)
            {
                audioSource.clip = storyBoardAudioClips[currentSpriteIndex];
                audioSource.Play();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
