using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

[System.Serializable]
public class QEntry
{
    public string state;
    public float[] qValues;
}

[System.Serializable]
public class QTableData
{
    public List<QEntry> entries;
}

public class QLearningBrain : MonoBehaviour
{
    public int actionCount = 4; // 0=Zayıf, 1=Orta, 2=Güçlü, 3=Özel Yetenek
    public float learningRate = 0.5f;
    public float discountFactor = 0.9f;
    public float explorationRate = 1.0f;

    private Dictionary<string, float[]> qTable = new Dictionary<string, float[]>();
    private string savePath;
    private string lastState;
    private int lastAction;
    public static QLearningBrain Instance;

    void Awake()
    {
        savePath = Path.Combine(Application.streamingAssetsPath, "qtable.json");
    }

    // STATE OLUŞTUR
    public string GetState(float myHealth, float enemyHealth, bool canDouble, bool canShield, bool canWeaken, float wind)
    {
        // Can durumu: 0, 1, 2, 3 (her 34 HP = 1 seviye)
        int myH = Mathf.Clamp(Mathf.CeilToInt(myHealth / 34f), 0, 3);
        int enH = Mathf.Clamp(Mathf.CeilToInt(enemyHealth / 34f), 0, 3);

        int cD = canDouble ? 1 : 0;
        int cS = canShield ? 1 : 0;
        int cW = canWeaken ? 1 : 0;

        // Rüzgar: -1=Sol, 0=Normal, 1=Sağ
        int windLevel = 0;
        if (wind < -0.7f) windLevel = -1;
        else if (wind > 0.7f) windLevel = 1;

        return $"{myH}_{enH}_{cD}_{cS}_{cW}_{windLevel}";
    }

    // AKSİYON SEÇ
    public int ChooseAction(string state)
    {
        lastState = state;
        if (!qTable.ContainsKey(state))
            qTable[state] = new float[actionCount];

        int selectedAction = 0;

        if (Random.value < explorationRate)
        {
            // Rastgele keşfet
            selectedAction = Random.Range(0, actionCount);
        }
        else
        {
            // En iyi aksiyonu seç
            float[] values = qTable[state];
            float maxVal = -9999f;
            List<int> bestActions = new List<int>();

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > maxVal)
                {
                    maxVal = values[i];
                    bestActions.Clear();
                    bestActions.Add(i);
                }
                else if (values[i] == maxVal)
                {
                    bestActions.Add(i);
                }
            }
            selectedAction = bestActions[Random.Range(0, bestActions.Count)];
        }

        lastAction = selectedAction;
        return selectedAction;
    }

    // AKSİYONU YORUMLA
    public (int actionType, float power) InterpretAction(int actionIndex, bool canDouble, bool canShield, bool canWeaken)
    {
        int type = 0;
        float power = 13f;

        switch (actionIndex)
        {
            case 0: // Zayıf atış
                type = 0;
                power = 9f;
                break;

            case 1: // Orta atış
                type = 0;
                power = 13f;
                break;

            case 2: // Güçlü atış
                type = 0;
                power = 17f;
                break;

            case 3: // Özel yetenekler (en iyi olanı seç)
                if (canShield)
                {
                    type = 2; // Shield
                }
                else if (canWeaken)
                {
                    type = 3; // Weaken
                }
                else if (canDouble)
                {
                    type = 1; // DoubleShot
                    power = 13f;
                }
                else
                {
                    type = 0;
                    power = 13f;
                }
                break;
        }

        return (type, power);
    }

    // ÖĞREN
    public void Learn(float reward, string nextState)
    {
        if (string.IsNullOrEmpty(lastState)) return;
        if (!qTable.ContainsKey(nextState))
            qTable[nextState] = new float[actionCount];

        float[] currentQ = qTable[lastState];
        float[] nextQ = qTable[nextState];
        float maxNextQ = -9999f;
        foreach (float v in nextQ)
            if (v > maxNextQ) maxNextQ = v;

        float oldQ = currentQ[lastAction];
        currentQ[lastAction] = oldQ + learningRate * (reward + discountFactor * maxNextQ - oldQ);

        // Exploration azalt
        explorationRate = Mathf.Max(0.05f, explorationRate * 0.9995f);
    }

    // ÖDÜL HESAPLA
    public float CalculateActionReward(int actionType, float damageDealt, bool hit)
    {
        float reward = 0f;

        if (hit)
        {
            // İsabet başarısı - BÜYÜK ÖDÜL
            reward += 100f;
            reward += damageDealt * 5f; // Hasar kadar ekstra
        }
        else
        {
            // Iskaladı - CEZA
            reward -= 30f;
        }

        // Aksiyon tipi bonusları
        switch (actionType)
        {
            case 1: // DoubleShot
                reward += hit ? 50f : -40f; // Riskli ama etkili
                break;
            case 2: // Shield
                reward += 40f; // Savunma her zaman iyi
                break;
            case 3: // Weaken
                reward += 40f; // Rakibi zayıflatmak iyi
                break;
        }

        return reward;
    }

    // Q-TABLE YÜKLE (WEB)
    public IEnumerator LoadQTableWeb()
    {
        Debug.Log("Q-Table aranıyor: " + savePath);
        using (UnityWebRequest www = UnityWebRequest.Get(savePath))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    QTableData data = JsonUtility.FromJson<QTableData>(www.downloadHandler.text);
                    // Dosya yapısı değiştiği için eski dosyayı kabul etme
                    if (data.entries.Count > 0 && data.entries[0].qValues.Length != actionCount)
                    {
                        qTable.Clear();
                        Debug.LogWarning("Eski Q-Table formatı, yeni eğitime başlanıyor.");
                    }
                    else
                    {
                        qTable.Clear();
                        foreach (var entry in data.entries)
                            qTable[entry.state] = entry.qValues;
                        Debug.Log($"Q-Table yüklendi! {data.entries.Count} state bulundu.");
                    }
                }
                catch
                {
                    qTable.Clear();
                    Debug.LogWarning("Q-Table okunamadı, yeni başlanıyor.");
                }
            }
            else
            {
                qTable.Clear();
                Debug.Log("Q-Table dosyası bulunamadı, yeni eğitime başlanıyor.");
            }
        }
    }

    // Q-TABLE KAYDET
    public void SaveQTable()
    {
        QTableData data = new QTableData();
        data.entries = new List<QEntry>();
        foreach (var kvp in qTable)
        {
            QEntry entry = new QEntry();
            entry.state = kvp.Key;
            entry.qValues = kvp.Value;
            data.entries.Add(entry);
        }
        string json = JsonUtility.ToJson(data, true);
        if (!Directory.Exists(Application.streamingAssetsPath))
            Directory.CreateDirectory(Application.streamingAssetsPath);
        File.WriteAllText(savePath, json);
        Debug.Log($"Q-Table kaydedildi! {data.entries.Count} state kaydedildi.");
    }
}