using UnityEngine;
using System.Collections;

public class AITrainer : MonoBehaviour
{
    [Header("Eğitim Ayarları")]
    [Tooltip("Kaç oyun oynanacak (0 = sonsuz)")]
    public int targetGameCount = 1000;
    
    [Tooltip("Kaç oyunda bir Q-Table kaydedilecek")]
    public int saveInterval = 50;
    
    [Tooltip("Oyun hızı (1 = normal, 10 = çok hızlı)")]
    [Range(1f, 100f)]
    public float trainingSpeed = 10f;
    
    [Tooltip("Otomatik başlat (Play'de otomatik eğitim başlar)")]
    public bool autoStart = false;

    [Header("Bağlantılar")]
    public GameManager_sc gameManager;
    public Character_sc aiCharacter; // Player_Amca (EĞİTİLECEK AI - Normal oyunda rakip)
    public Character_sc opponentCharacter; // Piyon (Sadece eğitim için - RandomAI ile oynayacak)
    public RandomAI opponentAI; // Piyon'un RandomAI scripti
    
    [Header("İstatistikler (Sadece Görüntüleme)")]
    [SerializeField] private int totalGames = 0;
    [SerializeField] private int aiWins = 0;
    [SerializeField] private int opponentWins = 0;
    [SerializeField] private float aiWinRate = 0f;
    [SerializeField] private int qTableSize = 0;

    private bool isTraining = false;
    private bool shouldStop = false;

    void Start()
    {
        // Otomatik başlatma
        if (autoStart)
        {
            StartTraining();
        }
    }

    void Update()
    {
        // SPACE tuşu ile başlat/durdur
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTraining)
                StopTraining();
            else
                StartTraining();
        }
        
        // İstatistikleri güncelle
        UpdateStats();
    }

    [ContextMenu("Start Training")]
    public void StartTraining()
    {
        if (isTraining) return;
        
        // Kontroller
        if (gameManager == null)
        {
            Debug.LogError("❌ [AITrainer] GameManager referansı eksik!");
            return;
        }
        
        if (aiCharacter == null || opponentCharacter == null)
        {
            Debug.LogError("❌ [AITrainer] AI Character veya Opponent Character referansı eksik!");
            return;
        }
        
        if (opponentAI == null)
        {
            Debug.LogError("❌ [AITrainer] Opponent AI referansı eksik! Piyon GameObject'ine RandomAI scripti ekleyin!");
            return;
        }

        Debug.Log("[AITrainer] Eğitim başlatılıyor...");
        Debug.Log($"Hedef: {targetGameCount} oyun | Kayıt: Her {saveInterval} oyunda");
        Debug.Log("SPACE tuşu ile durdurabilirsiniz");
        
        isTraining = true;
        shouldStop = false;
        
        // RandomAI'yi aktif et
        if (opponentAI != null)
        {
            opponentAI.SetActive(true);
            Debug.Log("✅ [AITrainer] RandomAI aktif edildi!");
        }
        
        // Oyun hızını ayarla
        Time.timeScale = trainingSpeed;
        
        // GameManager'daki callback'i bağla
        gameManager.OnGameEnd = OnTrainingGameEnd;
        
        // İlk oyunu başlat
        StartCoroutine(StartNewTrainingGame());
    }

    [ContextMenu("Stop Training")]
    public void StopTraining()
    {
        if (!isTraining) return;
        
        Debug.Log("⏸️ [AITrainer] Eğitim durduruluyor...");
        
        shouldStop = true;
        isTraining = false;
        
        // RandomAI'yi devre dışı bırak
        if (opponentAI != null)
        {
            opponentAI.SetActive(false);
            Debug.Log("⏹️ [AITrainer] RandomAI devre dışı bırakıldı!");
        }
        
        // Oyun hızını normale döndür
        Time.timeScale = 1f;
        
        // Callback'i kaldır
        if (gameManager != null)
            gameManager.OnGameEnd = null;
        
        // Son Q-Table'ı kaydet
        if (gameManager.qBrain != null)
        {
            gameManager.qBrain.SaveQTable();
            Debug.Log("💾 [AITrainer] Q-Table kaydedildi!");
        }
        
        Debug.Log($"✅ [AITrainer] Eğitim tamamlandı!");
        Debug.Log($"📊 Toplam: {totalGames} | AI Kazandı: {aiWins} | Piyon Kazandı: {opponentWins} | AI Kazanma Oranı: %{aiWinRate:F1}");
    }

    IEnumerator StartNewTrainingGame()
    {
        // Kısa bir gecikme
        yield return new WaitForSecondsRealtime(0.05f);
        
        if (shouldStop) yield break;
        
        // Karakterleri resetle
        aiCharacter.mevcutCan = aiCharacter.maxCan;
        aiCharacter.gameObject.SetActive(true);
        aiCharacter.ResetTurnStatus();
        aiCharacter.ResetWeakened();
        
        opponentCharacter.mevcutCan = opponentCharacter.maxCan;
        opponentCharacter.gameObject.SetActive(true);
        opponentCharacter.ResetTurnStatus();
        opponentCharacter.ResetWeakened();
        
        // GameManager'daki hakları resetle (reflection)
        ResetGameManagerFlags();
        
        //  ÖNEMLİ: GameManager'a hangi karakter AI olduğunu söyle
        gameManager.playerCharacter = opponentCharacter; // Player_Amca1 (piyon)
        gameManager.enemyCharacter = aiCharacter;        // Player_Amca (EĞİTİLEN AI)
        
        // UI panellerini kapatma (eğitim sırasında görünmesin diye)
        if (gameManager.actionPanel) gameManager.actionPanel.SetActive(false);
        if (gameManager.shootingPanel) gameManager.shootingPanel.SetActive(false);
        if (gameManager.kazananPanel) gameManager.kazananPanel.SetActive(false);
        
        // Oyunu başlat - Enemy'den başlasın (AI olan Player_Amca)
        gameManager.isPlayerTurn = false;
        gameManager.StartNewTurn();
    }

    void ResetGameManagerFlags()
    {
        // Reflection ile private flagları resetle
        var type = gameManager.GetType();
        
        type.GetField("playerDoubleUsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameManager, false);
        type.GetField("playerShieldUsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameManager, false);
        type.GetField("playerWeakenUsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameManager, false);
        
        type.GetField("enemyDoubleUsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameManager, false);
        type.GetField("enemyShieldUsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameManager, false);
        type.GetField("enemyWeakenUsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameManager, false);
    }

    void OnTrainingGameEnd(string loserName)
    {
        if (!isTraining || shouldStop) return;
        
        // İstatistikleri güncelle
        totalGames++;
        
        if (loserName == aiCharacter.isim)
        {
            // AI kaybetti, Opponent kazandı
            opponentWins++;
        }
        else
        {
            // AI kazandı! (Opponent kaybetti)
            aiWins++;
        }
        
        // Her 10 oyunda bir log
        if (totalGames % 10 == 0)
        {
            UpdateStats();
            Debug.Log($"Oyun #{totalGames} | AI: {aiWins} | Piyon: {opponentWins} | AI Oran: %{aiWinRate:F1} | Q-Table: {qTableSize} state");
        }
        
        // Periyodik kaydetme
        if (totalGames % saveInterval == 0)
        {
            if (gameManager.qBrain != null)
            {
                gameManager.qBrain.SaveQTable();
                Debug.Log($" [AITrainer] Q-Table kaydedildi! (Oyun #{totalGames})");
            }
        }
        
        // Hedef sayıya ulaşıldı mı?
        if (targetGameCount > 0 && totalGames >= targetGameCount)
        {
            Debug.Log($"[AITrainer] Hedef sayıya ulaşıldı! {totalGames} oyun tamamlandı.");
            StopTraining();
            return;
        }
        
        // Yeni oyun başlat
        StartCoroutine(StartNewTrainingGame());
    }

    void UpdateStats()
    {
        // AI kazanma oranını hesapla
        aiWinRate = totalGames > 0 ? (aiWins * 100f / totalGames) : 0f;
        
        // Q-Table boyutunu al (reflection)
        if (gameManager != null && gameManager.qBrain != null)
        {
            var qTableField = gameManager.qBrain.GetType().GetField("qTable", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (qTableField != null)
            {
                var qTable = qTableField.GetValue(gameManager.qBrain) as System.Collections.IDictionary;
                if (qTable != null)
                {
                    qTableSize = qTable.Count;
                }
            }
        }
    }

    void OnDestroy()
    {
        // Callback'i temizle
        if (gameManager != null)
            gameManager.OnGameEnd = null;
        
        // Oyun hızını normale döndür
        Time.timeScale = 1f;
    }
}