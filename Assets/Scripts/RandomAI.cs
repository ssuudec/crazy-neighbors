using UnityEngine;
using System.Collections;

public class RandomAI : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Rastgele atış gücü aralığı (min)")]
    public float minPower = 8f;
    
    [Tooltip("Rastgele atış gücü aralığı (max)")]
    public float maxPower = 16f;
    
    [Tooltip("Rastgele aksiyon seçme şansı (0-1)")]
    [Range(0f, 1f)]
    public float specialActionChance = 0.2f;

    private GameManager_sc gameManager;
    private Character_sc myCharacter;
    private bool isActive = false;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager_sc>();
        myCharacter = GetComponent<Character_sc>();
        
        if (gameManager == null)
        {
            Debug.LogError("[RandomAI] GameManager bulunamadı!");
        }
    }

    void Update()
    {
        // Sadece bu karakterin turu ise ve eğitim modundaysa aktif
        if (gameManager == null || myCharacter == null) return;
        
        // Eğer bu karakter playerCharacter ise ve turu ise
        if (gameManager.playerCharacter == myCharacter && gameManager.isPlayerTurn)
        {
            if (!isActive)
            {
                isActive = true;
                StartCoroutine(PlayTurn());
            }
        }
        else
        {
            isActive = false;
        }
    }

    IEnumerator PlayTurn()
    {
        // Kısa gecikme (daha gerçekçi görünsün)
        yield return new WaitForSecondsRealtime(0.1f);
        
        if (gameManager == null || !gameManager.isPlayerTurn) yield break;
        
        // Rastgele aksiyon seç
        float randomChance = Random.value;
        
        // %20 şansla özel yetenek kullan
        if (randomChance < specialActionChance)
        {
            TryUseSpecialAction();
        }
        else
        {
            // Normal atış yap
            FireRandomShot();
        }
    }

    void TryUseSpecialAction()
    {
        if (gameManager == null) return;
        
        // Sadece %20 şansla özel yetenek dene
        if (Random.value > specialActionChance)
        {
            FireRandomShot();
            return;
        }
        
        // Kullanılabilir yetenekleri kontrol et
        bool canDouble = !HasUsedAction("playerDoubleUsed");
        bool canShield = !HasUsedAction("playerShieldUsed");
        bool canWeaken = !HasUsedAction("playerWeakenUsed");
        
        // Kullanılabilir yetenekleri listele
        System.Collections.Generic.List<int> availableActions = new System.Collections.Generic.List<int>();
        if (canDouble) availableActions.Add(0);
        if (canShield) availableActions.Add(1);
        if (canWeaken) availableActions.Add(2);
        
        //GÜVENLİK: Liste boş mu tekrar kontrol
        if (availableActions.Count == 0)
        {
            FireRandomShot();
            return;
        }
        
        //GÜVENLİK: Index sınırlarını kontrol et
        int randomIndex = Random.Range(0, availableActions.Count);
        
        if (randomIndex < 0 || randomIndex >= availableActions.Count)
        {
            Debug.LogWarning($"[RandomAI] Index hatası önlendi! Count: {availableActions.Count}, Index: {randomIndex}");
            FireRandomShot();
            return;
        }
        
        int randomAction = availableActions[randomIndex];
        
        // Seçimi uygula
        switch (randomAction)
        {
            case 0: // Double Shot
                if (!HasUsedAction("playerDoubleUsed")) 
                {
                    MarkActionUsed("playerDoubleUsed");
                    StartCoroutine(DoubleShot());
                }
                else
                {
                    FireRandomShot();
                }
                break;
                
            case 1: // Shield
                if (!HasUsedAction("playerShieldUsed")) 
                {
                    MarkActionUsed("playerShieldUsed");
                    ApplyShield();
                }
                else
                {
                    FireRandomShot();
                }
                break;
                
            case 2: // Weaken
                if (!HasUsedAction("playerWeakenUsed")) 
                {
                    MarkActionUsed("playerWeakenUsed");
                    ApplyWeaken();
                }
                else
                {
                    FireRandomShot();
                }
                break;
                
            default:
                FireRandomShot();
                break;
        }
    }
    
    // Çift atış
    IEnumerator DoubleShot()
    {
        FireRandomShot();
        yield return new WaitForSecondsRealtime(0.5f);
        FireRandomShot();
    }
    
    // Kalkan
    void ApplyShield()
    {
        if (myCharacter != null)
        {
            myCharacter.KalkaniAc();
        }
        if (gameManager != null)
        {
            gameManager.EndTurn();
        }
    }
    
    // Moral Boz
    void ApplyWeaken()
    {
        if (gameManager != null && gameManager.enemyCharacter != null)
        {
            gameManager.enemyCharacter.MoraliBoz();
        }
        if (gameManager != null)
        {
            gameManager.EndTurn();
        }
    }
    
    // Flag'i işaretle
    void MarkActionUsed(string flagName)
    {
        var field = gameManager.GetType().GetField(flagName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(gameManager, true);
        }
    }

    IEnumerator DelayedShot()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        FireRandomShot();
    }

    void FireRandomShot()
    {
        if (gameManager == null) return;
        
        // Rastgele güç
        float power = Random.Range(minPower, maxPower);
        
        // Reflection ile private FirePlayerProjectile'ı çağır
        var method = gameManager.GetType().GetMethod("Shoot", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            // Zayıflama kontrolü
            if (myCharacter.isWeakened)
            {
                power *= 0.5f;
                myCharacter.isWeakened = false;
                if (myCharacter.moralBozSprite) 
                    myCharacter.moralBozSprite.SetActive(false);
            }
            
            method.Invoke(gameManager, new object[] { gameManager.playerFirePoint, power });
        }
    }

    bool HasUsedAction(string flagName)
    {
        var field = gameManager.GetType().GetField(flagName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (bool)field.GetValue(gameManager);
        }
        return false;
    }

    // AITrainer tarafından aktifleştirme/devre dışı bırakma
    public void SetActive(bool active)
    {
        enabled = active;
        isActive = false;
    }
}