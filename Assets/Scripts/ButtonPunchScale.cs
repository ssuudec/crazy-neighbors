using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonPunchScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = 1.1f;   
    public float lerpSpeed = 15f;      

    private Vector3 originalScale;
    private bool isPressed = false;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        // basılıysa büyütür değilse normal
        Vector3 target = isPressed ? originalScale * pressedScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * lerpSpeed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // sadece buton aktifse efekt olması için 
        Button btn = GetComponent<Button>();
        if (btn != null && !btn.interactable) return;

        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //mouseun üzerinden click çekilirse normale dönmesi için 
        isPressed = false;
    }
}