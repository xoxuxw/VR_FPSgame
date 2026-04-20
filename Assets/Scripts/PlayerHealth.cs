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
        currentHp = maxHp;
        Time.timeScale = 1f; // 시작 시 시간 정상화
        if (gameOverUI != null) gameOverUI.SetActive(false);
    }

    void Update()
    {
        // 피격 붉은 화면 효과 (시간 정지 중에도 작동하도록 unscaledDeltaTime 사용)
        if (damageImage != null && damageImage.color.a > 0)
        {
            damageImage.color = Color.Lerp(damageImage.color, Color.clear, flashSpeed * Time.unscaledDeltaTime);
        }

        // 죽었을 때 컨트롤러 A/X 버튼으로 리트라이
        if (isDead)
        {
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

        if (currentHp <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        Time.timeScale = 0f; // 세상 정지
        if (gameOverUI != null) gameOverUI.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f; // 시간 다시 흐르게 함
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}