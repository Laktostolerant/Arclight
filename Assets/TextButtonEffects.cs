using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using UnityEditor;

public class TextButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private TextMeshProUGUI textComponent;
    private Image imgComponent;

    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.2f, 1.2f, 1.2f);
    public float scaleSpeed = 0.1f;

    [Header("Sound Effects")]
    [SerializeField] AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip pressSound;

    [Header("Brackets, only used if text button.")]
    public string leftBracket = "[";
    public string rightBracket = "]";
    private string originalText;

    bool isImageButton;

    private void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
        {
            isImageButton = true;
            imgComponent = GetComponent<Image>();
            return;
        }

        originalText = textComponent.text;
        textComponent.transform.localScale = normalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }

        if (isImageButton)
            ScaleImage(true);
        else
            ScaleText(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isImageButton)
            ScaleImage(false);
        else
            ScaleText(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (pressSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pressSound);
        }

        if (isImageButton)
            ScaleImage(false);
        else
            ScaleText(false);
    }

    private void ScaleText(bool scaleUp)
    {
        if (scaleUp)
        {
            textComponent.text = leftBracket + " " + originalText + " " + rightBracket;
            LeanTween.scale(textComponent.gameObject, hoverScale, scaleSpeed).setIgnoreTimeScale(true);
        }
        else
        {
            textComponent.text = originalText;
            LeanTween.scale(textComponent.gameObject, normalScale, scaleSpeed).setIgnoreTimeScale(true);
        }
    }

    private void ScaleImage(bool scaleUp)
    {
        if (scaleUp)
            LeanTween.scale(imgComponent.gameObject, hoverScale, scaleSpeed).setIgnoreTimeScale(true);
        else
            LeanTween.scale(imgComponent.gameObject, normalScale, scaleSpeed).setIgnoreTimeScale(true);
    }
}
