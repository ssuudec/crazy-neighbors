
using UnityEngine;
using UnityEngine.UI;

public class Character_sc : MonoBehaviour
{
    [Header("Kimlik")]
    public string isim; 
    
    [Header("Can Ayarları")]
    public float maxCan = 100f;
    public float mevcutCan;
    public Slider canBari; 

    [Header("Durumlar")]
    public bool hasShield = false;  
    public bool isWeakened = false;  

    [Header("Görseller")]
    public GameObject kalkanSprite;
    public GameObject moralBozSprite;

    [Header("Bağlantılar")]
    [SerializeField] private GameManager_sc gameManager;

    void Start()
    {
        mevcutCan = maxCan;
        if (gameManager == null)
            Debug.LogError("GameManager referansı atanmadı!  Inspector'dan atayın.");
        
        if (kalkanSprite) kalkanSprite.SetActive(false);
        if (moralBozSprite) moralBozSprite.SetActive(false);
        
        UIGuncelle();
    }

    public void ResetTurnStatus()
    {
        hasShield = false;
        if (kalkanSprite) kalkanSprite.SetActive(false);
    }

    public void ResetWeakened()
    {
        isWeakened = false;
        if (moralBozSprite) moralBozSprite.SetActive(false);
    }

    public void KalkaniAc()
    {
        hasShield = true;
        if (kalkanSprite) kalkanSprite.SetActive(true);
    }

    public void MoraliBoz()
    {
        isWeakened = true;
        if (moralBozSprite) moralBozSprite.SetActive(true);
    }

    public void TakeDamage(float miktar)
    {
        if (hasShield)
        {
            miktar *= 0.2f;
            Debug.Log(isim + " kalkanla hasarı blokladı!");
        }

        mevcutCan -= miktar;

        // eğer bu enemy ise ve hasar varsa sesi çal 
        if (gameManager != null && this == gameManager.enemyCharacter)
        {
            if (MainMenuManager_sc.Instance != null)
            {
                MainMenuManager_sc.Instance.PlayEnemyHurt();
            }
        }

        if (mevcutCan <= 0)
        {
            mevcutCan = 0;
            gameObject.SetActive(false);
            if (gameManager != null)
                gameManager.OyunBitti(isim);
        }
        UIGuncelle();
    }

    void UIGuncelle()
    {
        if (canBari != null) canBari.value = mevcutCan / maxCan;
    }
}
