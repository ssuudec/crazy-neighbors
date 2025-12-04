using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;


public class GameManager_sc : MonoBehaviour
{
    [Header("Karakterler")]
    public Character_sc playerCharacter;
    public Character_sc enemyCharacter;

    [Header("Atış Ayarları")]
    public Transform playerFirePoint;
    public GameObject mermiPrefab;

    [Header("Arayüz (UI)")]
    public GameObject actionPanel;
    public Button doubleShotButton;
    public Button shieldButton;
    public Button weakenButton;
    public GameObject shootingPanel;
    public Slider powerSlider;
    public TextMeshProUGUI windText;
    public TextMeshProUGUI kazananText;
    public GameObject kazananPanel;

    private bool isPlayerTurn = true;
    private float windValue;

    private int selectedAction = 0;
    // player geliştirme hakları
    private bool playerDoubleUsed = false;
    private bool playerShieldUsed = false;
    private bool playerWeakenUsed = false;

    // enemy geliştirme hakları
    private bool enemyDoubleUsed = false;
    private bool enemyShieldUsed = false;
    private bool enemyWeakenUsed = false;
    private bool isCharging = false; 
    private bool chargeUp = true;    

    // enemy aksiyonları 
    public enum EnemyAction { SingleShot, DoubleShot, Shield, Weaken }
    public EnemyAction[] enemyActions;

    void Start()
    {
        Time.timeScale = 1f;  //oyunu başlatır
    
        // enemynin 4 tane aksiyonu
        enemyActions = new EnemyAction[4] { 
            EnemyAction.SingleShot, 
            EnemyAction.DoubleShot, 
            EnemyAction.Shield, 
            EnemyAction.Weaken 
        };

        Debug.Log("Rakibin aksiyonları: " + string.Join(", ", enemyActions));

        StartNewTurn();
    }

    void Update()
    {
        if (isPlayerTurn)
            ChargeMechanic();
    }

    void ChargeMechanic()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            isCharging = true;
            chargeUp = true;
            powerSlider.value = 0; 
            if(shootingPanel) shootingPanel.SetActive(true);
        }

        if (Input. GetMouseButton(0) && isCharging)
        {
            float speed = 0.8f * Time.deltaTime;

            if (chargeUp)
            {
                powerSlider.value += speed;
                if (powerSlider.value >= 1f) chargeUp = false;
            }
            else
            {
                powerSlider.value -= speed;
                if (powerSlider.value <= 0f) chargeUp = true;
            }
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            isCharging = false;
            if(shootingPanel) shootingPanel.SetActive(false);

            FirePlayerProjectile();
        }
    }

    void StartNewTurn()
    {
        windValue = Random.Range(-2f, 2f);
        if(windText) windText.text = "Rüzgar: " + windValue. ToString("F1");

        playerCharacter.ResetTurnStatus();
        enemyCharacter.ResetTurnStatus();

        selectedAction = 0;

        if (isPlayerTurn)
        {
            if(actionPanel) actionPanel. SetActive(true);
            if(shootingPanel) shootingPanel.SetActive(false);
        }
        else
        {
            if(actionPanel) actionPanel.SetActive(false);
            if(shootingPanel) shootingPanel.SetActive(false);

            // enemy hiçbir şey yapmadan bekliyor 
            StartCoroutine(RakipBekle());
        }
    }

    IEnumerator RakipBekle()
    {
        yield return new WaitForSecondsRealtime(1f);
        EndTurn();
    }

    // player aksiyonları
    public void Action_SelectDouble() 
    { 
        if (playerDoubleUsed) return;
        
        // buton sesi çalması için 
        if (MainMenuManager_sc.Instance != null)
            MainMenuManager_sc.Instance.PlayButtonClick();
        
        selectedAction = 1; 
        playerDoubleUsed = true;
        
        if (doubleShotButton != null)
            doubleShotButton.interactable = false;
        
        Debug.Log("Çift Atış Seçildi! "); 
    }

    public void Action_Shield() 
    { 
        if (playerShieldUsed) return;
        
        // buton sesi çalması için 
        if (MainMenuManager_sc.Instance != null)
            MainMenuManager_sc.Instance.PlayButtonClick();
        
        playerCharacter.KalkaniAc();
        playerShieldUsed = true;
        
        if (shieldButton != null)
            shieldButton.interactable = false;
        
        Debug.Log("Kalkan Açıldı!");
        EndTurn(); 
    }

    public void Action_Weaken() 
    { 
        if (playerWeakenUsed) return;
        
        //  buton sesi çalması için 
        if (MainMenuManager_sc.Instance != null)
            MainMenuManager_sc.Instance.PlayButtonClick();
        
        enemyCharacter.MoraliBoz();
        playerWeakenUsed = true;
        
        if (weakenButton != null)
            weakenButton.interactable = false;
        
        Debug.Log("Rakibin morali bozuldu!");
        EndTurn(); 
    }

    // ============ ENEMY AKSIYONLARI ============

    public bool Enemy_CanUseDouble()
    {
        return ! enemyDoubleUsed;
    }

    public bool Enemy_CanUseShield()
    {
        return ! enemyShieldUsed;
    }

    public bool Enemy_CanUseWeaken()
    {
        return ! enemyWeakenUsed;
    }

    public void Enemy_UseDoubleShot()
    {
        if (enemyDoubleUsed) return;
        enemyDoubleUsed = true;
        Debug.Log("Düşman Çift Atış kullandı!");
    }

    public void Enemy_UseShield()
    {
        if (enemyShieldUsed) return;
        enemyCharacter.KalkaniAc();
        enemyShieldUsed = true;
        Debug.Log("Düşman Kalkan kullandı!");
    }

    public void Enemy_UseWeaken()
    {
        if (enemyWeakenUsed) return;
        playerCharacter.MoraliBoz();
        enemyWeakenUsed = true;
        Debug. Log("Düşman Moral Bozdu!");
    }

    void FirePlayerProjectile()
    {
        float finalVal = Mathf. Max(powerSlider.value, 0.2f);
        float power = finalVal * 20f;

        if(playerCharacter.isWeakened) power *= 0.5f;

        if (selectedAction == 0)
        {
            Shoot(playerFirePoint, power);
            EndTurn();
        }
        else if (selectedAction == 1)
        {
            StartCoroutine(DoubleShot(playerFirePoint, power));
        }

        playerCharacter.isWeakened = false;
    }

    void Shoot(Transform point, float force)
    {
        GameObject mermi = Instantiate(mermiPrefab, point. position, Quaternion.identity);

        Bullet_sc bulletComp = mermi.GetComponent<Bullet_sc>();
        if (bulletComp != null)
            bulletComp. owner = playerCharacter;

        Rigidbody2D rb = mermi.GetComponent<Rigidbody2D>();

        // sabit kavis olması için 
        Vector2 direction = new Vector2(0.6f, 1.3f);

        rb. AddForce(direction * force, ForceMode2D. Impulse);

        // rüzgarın etkisi(oyun oynarken etkisi fazla olduğu için 1f yerine 0.3f yapıldı)
        rb.AddForce(new Vector2(windValue * 0.3f, 0), ForceMode2D. Impulse);
    }

    IEnumerator DoubleShot(Transform point, float force)
    {
        // ilk atış
        Shoot(point, force);

        // ikinci atış için tamamen aynı değerler kullanılacak
        yield return new WaitForSeconds(0.5f);
        Shoot(point, force);  // ikinci mermi tamamen aynı

        EndTurn();
    }

    void EndTurn()
    {
        isPlayerTurn = !isPlayerTurn;
        StartCoroutine(YeniTurGecikmeli());
    }

    IEnumerator YeniTurGecikmeli()
    {
        yield return new WaitForSecondsRealtime(1f);
        StartNewTurn();
    }

    public void OyunBitti(string kaybedenIsim)
    {
        Debug.Log(kaybedenIsim + " öldü!  Oyun bitti.");
        
        // kazananı bulur
        string kazananIsim = (kaybedenIsim == playerCharacter.isim) ?  enemyCharacter. isim : playerCharacter.isim;
        
        // ekranda gösterir
        if (kazananText != null)
        {
            kazananText.text = kazananIsim + " KAZANDI!";
        }
        if (kazananPanel != null)
        {
            kazananPanel.SetActive(true);
        }
        
        // butonları gizlemek için 
        if (actionPanel != null)
            actionPanel.SetActive(false);
        
        // oyun durdurulur
        Time. timeScale = 0f;
    }
}