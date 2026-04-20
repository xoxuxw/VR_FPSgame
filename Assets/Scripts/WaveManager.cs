using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;

    [System.Serializable]
    public struct WaveData
    {
        public int enemyCount;      
        public GameObject enemyPrefab;
        public GameObject bossPrefab; 
    }

    public List<WaveData> waves;
    public Transform[] spawnPoints;
    public TextMeshProUGUI waveNoticeText; 

    private int currentWaveIndex = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();

    void Awake() { instance = this; }

    void Start()
    {
        Invoke("StartFirstWave", 1.0f);
    }

    void StartFirstWave() { if (waves.Count > 0) StartWave(0); }

    public void StartWave(int index)
    {
        if (index >= waves.Count)
        {
            ShowNotice("ALL CLEAR!");
            return;
        }
        currentWaveIndex = index;
        StopAllCoroutines();
        StartCoroutine(WaveRoutine());
    }

    IEnumerator WaveRoutine()
    {
        WaveData data = waves[currentWaveIndex];
        activeEnemies.Clear(); 
        
        ShowNotice("WAVE " + (currentWaveIndex + 1));
        yield return new WaitForSeconds(3f);

        // 1. [절대 방어] 쫄병 소환이 다 끝날 때까지 아래 코드는 실행되지 않습니다.
        yield return StartCoroutine(SpawnMinions(data));

        // 2. [강력 체크] 필드에 적이 1마리라도 남아있다면 여기서 무한 대기
        while (true)
        {
            activeEnemies.RemoveAll(item => item == null); // 죽은 적 제거
            if (activeEnemies.Count <= 0) break;           // 적이 없으면 루프 탈출
            yield return new WaitForSeconds(0.5f);
        }

        // 3. 이제서야 보스 등장
        if (data.bossPrefab != null)
        {
            ShowNotice("BOSS APPEARED!");
            yield return new WaitForSeconds(2f);
            
            SpawnEnemy(data.bossPrefab);
            
            // 보스도 죽을 때까지 대기
            while (true)
            {
                activeEnemies.RemoveAll(item => item == null);
                if (activeEnemies.Count <= 0) break;
                yield return new WaitForSeconds(0.5f);
            }
        }

        yield return new WaitForSeconds(2f);
        NextWave();
    }

    // 쫄병 소환 전용 코루틴 (이게 끝나야 보스 로직이 시작됨)
    IEnumerator SpawnMinions(WaveData data)
    {
        for (int i = 0; i < data.enemyCount; i++)
        {
            SpawnEnemy(data.enemyPrefab);
            yield return new WaitForSeconds(1.0f); // 쫄병 간격
        }
        // 소환 직후 리스트가 업데이트될 시간을 아주 잠깐 줌
        yield return new WaitForSeconds(0.1f);
    }

    void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null) return;
        int idx = Random.Range(0, spawnPoints.Length);
        GameObject go = Instantiate(prefab, spawnPoints[idx].position, Quaternion.identity);
        activeEnemies.Add(go);
    }

    public void EnemyDefeated() { activeEnemies.RemoveAll(item => item == null); }

    void NextWave() { StartWave(currentWaveIndex + 1); }

    void ShowNotice(string message)
    {
        if (waveNoticeText != null)
        {
            waveNoticeText.text = message;
            waveNoticeText.gameObject.SetActive(true);
            CancelInvoke("HideNotice");
            Invoke("HideNotice", 3f);
        }
    }
    void HideNotice() { if (waveNoticeText != null) waveNoticeText.gameObject.SetActive(false); }
}