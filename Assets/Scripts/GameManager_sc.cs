using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class GameManager_sc : MonoBehaviour
{
    [Header("Q-Learning")]
    public QLearningBrain qBrain;

    [Header("Karakterler")]
    public Character_sc playerCharacter;
    public Character_sc enemyCharacter;

    [Header("Atış Ayarları")]
    public Transform playerFirePoint;
    public Transform enemyFirePoint;
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
    public System.Action<string> OnGameEnd;

    public bool isPlayerTurn = true;
    private float windValue;
    private int selectedAction = 0;

    // Haklar
    private bool playerDoubleUsed = false;
    private bool playerShieldUsed = false;
    private bool playerWeakenUsed = false;

    private bool enemyDoubleUsed = false;
    private bool enemyShieldUsed = false;
    private bool enemyWeakenUsed = false;

    private bool isCharging = false;
    private bool chargeUp = true;
    private bool doubleShotAktif = false;
    private int kalanMermi = 0;

    private bool isTurnProcessing = false;
    private Coroutine activeTurnRoutine = null;
    
    // AI Öğrenme için
    private float currentTurnReward = 0f;
    private int currentActionType = 0;
    private bool enemyHitThisTurn = false;

    // EĞİTİM MODU KONTROLÜ
    private bool isTrainingMode = false;

    IEnumerator Start()
{
    Time.timeScale = 1f;
    if (qBrain != null) yield return StartCoroutine(qBrain.LoadQTableWeb());

    // Training mode sadece AITrainer varsa!
    isTrainingMode = (OnGameEnd != null);

    // AI kullanılsın mı?
    bool useAI = false;
    if (MainMenuManager_sc.Instance != null)
    {
        useAI = MainMenuManager_sc.usePreTrainedAI;
    }

    if (qBrain != null)
    {
        // Eğitim modunda exploration 1.0, test/random'da 0.0
        qBrain.explorationRate = isTrainingMode ? 1.0f : (useAI ? 0.0f : 1.0f);
    }

    //  Daha net log
    if (isTrainingMode)
    {
        Debug.Log("Oyun Modu: EĞİTİM (AITrainer aktif)");
    }
    else if (useAI)
    {
        Debug.Log("Oyun Modu: TEST (Eğitilmiş AI)");
    }
    else
    {
        Debug.Log("Oyun Modu: RASTGELE (Basit AI)");
    }

    StartNewTurn();
}

    void Update()
    {
        if (isPlayerTurn && !isTurnProcessing) ChargeMechanic();
    }

    void ChargeMechanic()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            isCharging = true;
            chargeUp = true;
            powerSlider.value = 0;
            if (shootingPanel) shootingPanel.SetActive(true);
        }

        if (Input.GetMouseButton(0) && isCharging)
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
            if (shootingPanel) shootingPanel.SetActive(false);
            FirePlayerProjectile();
        }
    }

    public void RegisterEnemyHit(float damageDealt)
    {
        currentTurnReward += damageDealt;
        enemyHitThisTurn = true;
    }

    public void StartNewTurn()
    {
        windValue = Random.Range(-2f, 2f);
        if (windText) windText.text = "Rüzgar: " + windValue.ToString("F1");

        isTurnProcessing = false;

        if (isPlayerTurn)
        {
            playerCharacter.ResetTurnStatus();
            selectedAction = 0;
            if (actionPanel) actionPanel.SetActive(true);
            if (shootingPanel) shootingPanel.SetActive(false);
        }
        else
        {
            enemyCharacter.ResetTurnStatus();
            if (actionPanel) actionPanel.SetActive(false);
            if (shootingPanel) shootingPanel.SetActive(false);

            if (activeTurnRoutine != null) StopCoroutine(activeTurnRoutine);
            activeTurnRoutine = StartCoroutine(RakipTuru());
        }
    }

    IEnumerator RakipTuru()
    {
        if (isPlayerTurn) yield break;
        yield return new WaitForSecondsRealtime(0.05f);
        if (isPlayerTurn) yield break;
        bool useAI = (MainMenuManager_sc.Instance != null && MainMenuManager_sc.usePreTrainedAI);

        // Yapay zeka kapalıysa veya qBrain yoksa basit random AI
        if (qBrain == null || !isTrainingMode  || !useAI)
        {
            // Basit Random AI - %20 şansla özel yetenek
            if (Random.value < 0.2f)
            {
                // Kullanılabilir yetenekleri listele
                System.Collections.Generic.List<int> availableSpecials = new System.Collections.Generic.List<int>();
                if (Enemy_CanUseDouble()) availableSpecials.Add(1);
                if (Enemy_CanUseShield()) availableSpecials.Add(2);
                if (Enemy_CanUseWeaken()) availableSpecials.Add(3);

                if (availableSpecials.Count > 0)
                {
                    int randomSpecial = availableSpecials[Random.Range(0, availableSpecials.Count)];
                    
                    switch (randomSpecial)
                    {
                        case 1: // Double Shot
                            Enemy_MarkDoubleUsed();
                            float doublePower = Random.Range(10f, 14f);
                            StartCoroutine(EnemyDoubleShot(enemyFirePoint, doublePower));
                            activeTurnRoutine = null;
                            yield break;
                        case 2: // Shield
                            Enemy_ApplyShield();
                            activeTurnRoutine = null;
                            yield break;
                        case 3: // Weaken
                            Enemy_ApplyWeaken();
                            activeTurnRoutine = null;
                            yield break;
                    }
                }
            }

            // Normal rastgele atış
            float randomPower = Random.Range(8f, 16f);
            EnemyShoot(enemyFirePoint, randomPower);
        }
        else  // eğitilmiş AI (Yapay zeka açık)
        {
            string state = qBrain.GetState(
                enemyCharacter.mevcutCan,
                playerCharacter.mevcutCan,
                Enemy_CanUseDouble(),
                Enemy_CanUseShield(),
                Enemy_CanUseWeaken(),
                windValue
            );

            int action = qBrain.ChooseAction(state);
            var (actionType, aiPower) = qBrain.InterpretAction(action, Enemy_CanUseDouble(), Enemy_CanUseShield(), Enemy_CanUseWeaken());

            // AI değişkenlerini ayarla
            currentActionType = actionType;
            enemyHitThisTurn = false;
            currentTurnReward = 0f;

            if (actionType == 1 && enemyDoubleUsed) { actionType = 0; aiPower = 13f; }
            if (actionType == 2 && enemyShieldUsed) { actionType = 0; aiPower = 13f; }
            if (actionType == 3 && enemyWeakenUsed) { actionType = 0; aiPower = 13f; }

            switch (actionType)
            {
                case 0: EnemyShoot(enemyFirePoint, aiPower); break;
                case 1: Enemy_MarkDoubleUsed(); StartCoroutine(EnemyDoubleShot(enemyFirePoint, aiPower)); break;
                case 2: Enemy_ApplyShield(); break;
                case 3: Enemy_ApplyWeaken(); break;
            }
        }
        activeTurnRoutine = null;
    }

    // PLAYER BUTON FONKSİYONLARI
    public void Action_SelectDouble()
    {
        if (playerDoubleUsed) return;
        if (MainMenuManager_sc.Instance != null) MainMenuManager_sc.Instance.PlayButtonClick();
        selectedAction = 1; playerDoubleUsed = true;
        if (doubleShotButton != null) doubleShotButton.interactable = false;
    }

    public void Action_Shield()
    {
        if (playerShieldUsed) return;
        if (MainMenuManager_sc.Instance != null) MainMenuManager_sc.Instance.PlayButtonClick();
        playerCharacter.KalkaniAc(); playerShieldUsed = true;
        if (shieldButton != null) shieldButton.interactable = false;
        EndTurn();
    }

    public void Action_Weaken()
    {
        if (playerWeakenUsed) return;
        if (MainMenuManager_sc.Instance != null) MainMenuManager_sc.Instance.PlayButtonClick();
        enemyCharacter.MoraliBoz(); playerWeakenUsed = true;
        if (weakenButton != null) weakenButton.interactable = false;
        EndTurn();
    }

    // ENEMY FONKSİYONLARI
    public bool Enemy_CanUseDouble() { return !enemyDoubleUsed; }
    public bool Enemy_CanUseShield() { return !enemyShieldUsed; }
    public bool Enemy_CanUseWeaken() { return !enemyWeakenUsed; }

    public void Enemy_MarkDoubleUsed() { enemyDoubleUsed = true; }
    public void Enemy_ApplyShield() { enemyCharacter.KalkaniAc(); enemyShieldUsed = true; EndTurn(); }
    public void Enemy_ApplyWeaken() { playerCharacter.MoraliBoz(); enemyWeakenUsed = true; EndTurn(); }

    // ATIŞ FONKSİYONLARI
    void FirePlayerProjectile()
    {
        float finalVal = Mathf.Max(powerSlider.value, 0.2f);
        float power = finalVal * 20f;
        if (playerCharacter.isWeakened)
        {
            power *= 0.5f; playerCharacter.isWeakened = false;
            if (playerCharacter.moralBozSprite) playerCharacter.moralBozSprite.SetActive(false);
        }

        if (selectedAction == 0) Shoot(playerFirePoint, power);
        else if (selectedAction == 1) StartCoroutine(DoubleShot(playerFirePoint, power));
    }

    void Shoot(Transform point, float force)
    {
        GameObject mermi = Instantiate(mermiPrefab, point.position, Quaternion.identity);

        Bullet_sc bulletComp = mermi.GetComponent<Bullet_sc>();
        if (bulletComp != null)
            bulletComp.owner = (point == playerFirePoint) ? playerCharacter : enemyCharacter;

        Rigidbody2D rb = mermi.GetComponent<Rigidbody2D>();
        float xDir = (point == playerFirePoint) ? 0.6f : -0.6f;

        Vector2 atisYonu = new Vector2(xDir, 1.3f);

        rb.AddForce(atisYonu * force, ForceMode2D.Impulse);
        rb.AddForce(new Vector2(windValue * 0.75f, 0), ForceMode2D.Impulse);
    }

    IEnumerator DoubleShot(Transform point, float force)
    {
        doubleShotAktif = true; kalanMermi = 2;
        Shoot(point, force); yield return new WaitForSeconds(0.5f); Shoot(point, force);
    }

    void EnemyShoot(Transform point, float force)
    {
        if (enemyCharacter.isWeakened)
        {
            force *= 0.5f; enemyCharacter.isWeakened = false;
            if (enemyCharacter.moralBozSprite) enemyCharacter.moralBozSprite.SetActive(false);
        }
        Shoot(point, force);
    }

    IEnumerator EnemyDoubleShot(Transform point, float force)
    {
        doubleShotAktif = true; kalanMermi = 2;
        EnemyShoot(point, force); yield return new WaitForSeconds(0.5f); EnemyShoot(point, force);
    }

    public void EndTurn()
    {
        if (doubleShotAktif) { kalanMermi--; if (kalanMermi > 0) return; doubleShotAktif = false; }
        if (isTurnProcessing) return; isTurnProcessing = true;

        // SADECE EĞİTİM MODUNDA ÖĞREN!
        if (!isPlayerTurn && qBrain != null && isTrainingMode)
        {
            string nextState = qBrain.GetState(
                enemyCharacter.mevcutCan,
                playerCharacter.mevcutCan,
                Enemy_CanUseDouble(),
                Enemy_CanUseShield(),
                Enemy_CanUseWeaken(),
                windValue
            );

            // ÖDÜL HESAPLA
            float reward = qBrain.CalculateActionReward(
                currentActionType,
                currentTurnReward,
                enemyHitThisTurn
            );

            // Can durumuna göre bonus/ceza
            float myHealthRatio = enemyCharacter.mevcutCan / enemyCharacter.maxCan;
            float enemyHealthRatio = playerCharacter.mevcutCan / playerCharacter.maxCan;

            if (myHealthRatio < 0.3f) reward -= 50f; // Canım düşükse ceza
            if (enemyHealthRatio < 0.3f) reward += 50f; // Rakip zayıfsa bonus

            qBrain.Learn(reward, nextState);

            Debug.Log($"[AI] R:{reward:F0} | Hit:{enemyHitThisTurn} | Dmg:{currentTurnReward:F0} | Act:{currentActionType}");

            currentTurnReward = 0f;
            enemyHitThisTurn = false;
        }

        isPlayerTurn = !isPlayerTurn;
        StartCoroutine(YeniTurGecikmeli());
    }

    IEnumerator YeniTurGecikmeli() { yield return new WaitForSecondsRealtime(0.05f); StartNewTurn(); }

    public void OyunBitti(string kaybedenIsim)
    {
        Debug.Log(" [DEBUG] OyunBitti() çağrıldı!");
        
        // Coroutine'leri durdur
        if (activeTurnRoutine != null) StopCoroutine(activeTurnRoutine);
        
        // SADECE EĞİTİM MODUNDA ÖDÜL VER VE KAYDET!
        if (qBrain != null && isTrainingMode)
        {
            if (kaybedenIsim == playerCharacter.isim)
            {
                qBrain.Learn(500f, "WIN");
                Debug.Log("[AI] 🏆 KAZANDI! +500");
            }
            else
            {
                qBrain.Learn(-500f, "LOSE");
                Debug.Log("[AI] 💀 KAYBETTİ! -500");
            }
        }

        // AITrainer'a bildir (varsa)
        OnGameEnd?.Invoke(kaybedenIsim);
        
        // Eğer callback yoksa (normal oyun modu), UI göster ve oyunu durdur
        if (OnGameEnd == null)
        {
            string kazananIsim = (kaybedenIsim == playerCharacter.isim) ? enemyCharacter.isim : playerCharacter.isim;
            if (kazananText != null) kazananText.text = kazananIsim + " KAZANDI!";
            if (kazananPanel != null) kazananPanel.SetActive(true);
            if (actionPanel != null) actionPanel.SetActive(false);
            
            // SADECE EĞİTİM MODUNDA KAYDET!
            if (qBrain != null && isTrainingMode)
            {
                qBrain.SaveQTable();
                Debug.Log("💾 [GameManager] Q-Table kaydedildi (oyun bitti)");
            }
            
            Time.timeScale = 0f;
        }
    }
}