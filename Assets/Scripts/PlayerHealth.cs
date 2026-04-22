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
        if (isDead) return;
        isDead = true;

        // 1. 맵에 있는 모든 적(EnemyAI 스크립트가 붙은 오브젝트)을 찾음
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();

        foreach (EnemyAI enemy in enemies)
        {
            // [얼음 단계 1] 공격 및 추적 로직 스크립트 자체를 끔
            enemy.enabled = false;

            // [얼음 단계 2] 네비게이션 에이전트 제어
            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                // 에이전트를 끄기 전에 속도를 완전히 0으로 만듦 (미끄러짐 방지)
                agent.velocity = Vector3.zero;
                // 에이전트 컴포넌트를 비활성화해서 길찾기 연산을 완전히 종료
                agent.enabled = false;
            }

            // [얼음 단계 3] 애니메이션 정지
            Animator anim = enemy.GetComponent<Animator>();
            if (anim != null)
            {
                // 애니메이션 속도를 0으로 만들어 때리던 자세 그대로 멈추게 함
                anim.speed = 0;
            }
        }

        // 2. 웨이브 매니저도 더 이상 적을 소환하지 못하게 정지
        if (WaveManager.instance != null)
        {
            WaveManager.instance.StopAllCoroutines();
            WaveManager.instance.enabled = false;
        }

        // 3. 게임 오버 UI 활성화 (이제 엔진이 멈추지 않으므로 버튼 클릭 가능!)
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
    }
    
    public void Retry()
    {

        // 혹시 모르니 다시 한번 시간을 1로 초기화
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}