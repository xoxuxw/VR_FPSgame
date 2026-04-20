using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("능력치 설정")]
    public float hp = 30f;
    public float damage = 10f;
    public float attackRange = 2.0f;
    public float attackCooldown = 1.5f;
    public float attackDamageDelay = 0.5f; 

    private NavMeshAgent agent;
    private Animator anim;
    private PlayerHealth playerHealth;
    private bool isDead = false;
    private float nextAttackTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        
        // 최적화: 회피 품질 낮춤
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        
        playerHealth = GameObject.FindObjectOfType<PlayerHealth>();

        // 0.2초마다 경로 갱신 (렉 방지)
        if (playerHealth != null) StartCoroutine(UpdatePath());
    }

    IEnumerator UpdatePath()
    {
        while (!isDead)
        {
            if (playerHealth != null)
            {
                float distance = Vector3.Distance(transform.position, playerHealth.transform.position);
                if (distance > attackRange)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerHealth.transform.position);
                }
                else
                {
                    agent.isStopped = true;
                }
            }
            yield return new WaitForSeconds(0.2f); 
        }
    }

    void Update()
    {
        if (isDead || playerHealth == null) return;

        float distance = Vector3.Distance(transform.position, playerHealth.transform.position);
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void Attack() 
    { 
        anim.SetTrigger("Attack"); 
        StartCoroutine(DamageDelay()); 
    }

    IEnumerator DamageDelay() 
    {
        yield return new WaitForSeconds(attackDamageDelay); 
        if (playerHealth != null && !isDead)
        {
            float distance = Vector3.Distance(transform.position, playerHealth.transform.position);
            if (distance <= attackRange + 1.0f)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }

    public void TakeDamage(float amt) 
    { 
        if (isDead) return;
        hp -= amt; 
        if (hp <= 0) Die(); 
    }

    void Die() 
    { 
        if (isDead) return;
        isDead = true; 
        
        agent.isStopped = true;
        agent.enabled = false; 
        anim.SetTrigger("Die"); 
        
        // [중요] 죽는 순간 매니저에게 신호를 보냄
        // if (WaveManager.instance != null)
        // {
        //     WaveManager.instance.EnemyDefeated();
        // }
        
        Destroy(gameObject, 3f); 
    }
}