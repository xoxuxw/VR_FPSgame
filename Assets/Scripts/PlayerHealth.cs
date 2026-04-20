using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float maxHp = 100f;
    public float currentHp;
    public Slider hpSlider;

    [Header("피드백 UI")]
    public Image damageImage;
    public float flashSpeed = 5f;
    public Color flashColor = new Color(1f, 0f, 0f, 0.4f);
    public GameObject gameOverUI;

    private bool isDead = false;

    void Start()
    {
        // 시작할 때 무조건 시간을 1로 설정하여 엔진이 멈춰있지 않게 함
        Time.timeScale = 1f;
        currentHp = maxHp;
        if (hpSlider != null) hpSlider.value = 1f;
        if (gameOverUI != null) gameOverUI.SetActive(false);
    }

    void Update()
    {
        // 피격 효과 (TimeScale과 상관없이 작동)
        if (damageImage != null && damageImage.color.a > 0)
        {
            damageImage.color = Color.Lerp(damageImage.color, Color.clear, flashSpeed * Time.unscaledDeltaTime);
        }

        // 죽었을 때 버튼 입력 체크
        if (isDead)
        {
            // VR 컨트롤러의 A 또는 X 버튼 (ARAVRInput 기준)
            if (ARAVRInput.GetDown(ARAVRInput.Button.One, ARAVRInput.Controller.RTouch) ||
                ARAVRInput.GetDown(ARAVRInput.Button.One, ARAVRInput.Controller.LTouch))
            {
                Retry();
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHp -= amount;
        if (hpSlider != null) hpSlider.value = currentHp / maxHp;
        if (damageImage != null) damageImage.color = flashColor;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 이미 죽은 상태라면 다시 실행하지 않음 (무한 루프 방지)
        if (isDead) return;
        isDead = true;

        Debug.Log("사망 로직 시작 - 에디터를 멈추지 않습니다.");

        // 1. 모든 적들을 찾아서 로직만 정지
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI enemy in enemies)
        {
            enemy.enabled = false;

            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true; // enabled = false 대신 우선 이걸 써보세요.
                agent.velocity = Vector3.zero;
            }

            var anim = enemy.GetComponent<Animator>();
            if (anim != null) anim.speed = 0;
        }

        // 2. 웨이브 매니저 정지
        if (WaveManager.instance != null)
        {
            WaveManager.instance.StopAllCoroutines();
            WaveManager.instance.enabled = false;
        }

        // 3. 게임 오버 UI 활성화
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        // 절대 넣으면 안 되는 코드:
        // Time.timeScale = 0; <- 이것도 에디터를 먹통으로 만들 수 있음
        // Debug.Break(); <- 유니티 에디터를 일시정지 시키는 주범! (삭제 필수)
    }
    // UI 버튼에 연결하거나 컨트롤러 입력으로 호출
    public void Retry()
    {

        // 혹시 모르니 다시 한번 시간을 1로 초기화
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}