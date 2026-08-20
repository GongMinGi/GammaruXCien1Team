using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 카드 한 장의 앞면과 뒷면, 반짝임, 뒤집기 연출을 담당한다.
/// </summary>
public class StageCardView : MonoBehaviour
{
    [SerializeField] private RectTransform faceRoot;
    [SerializeField] private GameObject frontFace;
    [SerializeField] private Image frontImage;
    [SerializeField] private GameObject backFace;
    [SerializeField] private Image flashImage;
    [SerializeField] private Text stageNumberText;
    [SerializeField] private Button cardButton;
    [SerializeField] private float flashInDuration = 0.08f;
    [SerializeField] private float flashOutDuration = 0.3f;
    [SerializeField] private float flipDuration = 0.4f;

    private bool isFrontShown = true;

    /// <summary>
    /// 번호와 앞면 이미지를 표시하고, 앞면이 보이는 상태에서 눌렀을 때의 처리를 연결한다.
    /// 뒷면이 보이는 동안 누르면 스테이지를 열지 않고 카드를 뒤집는다.
    /// </summary>
    public void Initialize(
        int stageNumber,
        Sprite cardImage,
        UnityAction<int> stageOpenHandler)
    {
        //stageNumberText.text = stageNumber.ToString();
        frontImage.sprite = cardImage;
        cardButton.onClick.AddListener(delegate
        {
            if (!isFrontShown)
            {
                StartCoroutine(FlipToFront());
                return;
            }

            stageOpenHandler(stageNumber);
        });
    }

    /// <summary>
    /// 카드 자리가 흰색으로 반짝인 뒤 뒷면이 나타난다.
    /// </summary>
    public IEnumerator PlayRevealRoutine()
    {
        SoundManager.Instance.PlaySoundEffect("StageOpen");
        isFrontShown = false;
        frontFace.SetActive(false);
        backFace.SetActive(false);
        yield return flashImage.DOFade(1f, flashInDuration).WaitForCompletion();

        backFace.SetActive(true);
        yield return flashImage.DOFade(0f, flashOutDuration).WaitForCompletion();
    }

    /// <summary>
    /// Y축으로 반 바퀴 돌며 뒷면을 앞면으로 바꾼다.
    /// 옆을 보는 90도에서 각도를 -90도로 옮겨 앞면이 뒤집혀 보이지 않게 한다.
    /// </summary>
    private IEnumerator FlipToFront()
    {
        isFrontShown = true;
        yield return faceRoot
            .DOLocalRotate(new Vector3(0f, 90f, 0f), flipDuration * 0.5f)
            .WaitForCompletion();

        backFace.SetActive(false);
        frontFace.SetActive(true);
        faceRoot.localEulerAngles = new Vector3(0f, -90f, 0f);
        yield return faceRoot
            .DOLocalRotate(Vector3.zero, flipDuration * 0.5f)
            .WaitForCompletion();
    }
}
