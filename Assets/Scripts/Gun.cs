using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum Side { Left, Right }
    [Header("설정")]
    public Side handSide = Side.Right; 

    [Header("효과 리소스")]
    public GameObject bulletImpactPrefab; // 프리팹으로 관리하는 것이 더 깔끔합니다.
    public Transform crosshair;          // 조준점 오브젝트
    private AudioSource bulletAudio;

    [Header("사격 옵션")]
    public float range = 100f;        
    public bool useCrosshair = true;

    void Start()
    {
        bulletAudio = GetComponent<AudioSource>();
        if (crosshair != null) crosshair.gameObject.SetActive(false);
    }

    void Update()
    {
        ARAVRInput.Controller controller = (handSide == Side.Left) ? 
            ARAVRInput.Controller.LTouch : ARAVRInput.Controller.RTouch;
        
        Transform handTr = (handSide == Side.Left) ? 
            ARAVRInput.LHand : ARAVRInput.RHand;

        // 내 손 안에서 Muzzle 찾기
        Transform muzzle = FindMuzzleInHand(handTr);

        if (muzzle != null)
        {
            // 1. 조준점 처리
            if (useCrosshair && crosshair != null)
            {
                crosshair.gameObject.SetActive(true);
                UpdateCrosshair(muzzle);
            }

            // 2. 사격 처리 (Index Trigger)
            if (ARAVRInput.GetDown(ARAVRInput.Button.IndexTrigger, controller))
            {
                Shoot(controller, muzzle);
            }
        }
        else
        {
            if (crosshair != null) crosshair.gameObject.SetActive(false);
        }
    }

    private void UpdateCrosshair(Transform muzzle)
    {
        Ray ray = new Ray(muzzle.position, muzzle.forward);
        RaycastHit hit;
        int layerMask = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Tower"));

        if (Physics.Raycast(ray, out hit, range, layerMask))
        {
            // 충돌 지점에 조준점 배치
            crosshair.position = hit.point + (hit.normal * 0.01f);
            crosshair.forward = hit.normal;
        }
        else
        {
            // 허공을 쏠 때
            crosshair.position = muzzle.position + (muzzle.forward * 10f);
            crosshair.forward = -muzzle.forward;
        }
    }

void Shoot(ARAVRInput.Controller controller, Transform muzzle)
{
    ARAVRInput.PlayVibration(controller);
    if (bulletAudio != null) bulletAudio.Play();

    Ray ray = new Ray(muzzle.position, muzzle.forward);
    RaycastHit hit;
    
    // Player와 Tower 레이어는 무시하고 나머지는 다 맞춤
    int layerMask = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Tower"));

    if (Physics.Raycast(ray, out hit, range, layerMask))
    {
        // 1. 파티클 생성
        if (bulletImpactPrefab != null)
        {
            GameObject impact = Instantiate(bulletImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 1.0f);
        }

        // 2. 데미지 전달 (이 부분이 핵심!)
        // 적 오브젝트 자체 혹은 부모/자식에 EnemyAI가 있는지 확인
        EnemyAI enemy = hit.transform.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(10f); // 10만큼 데미지
            Debug.Log(hit.transform.name + "에게 데미지를 입혔습니다!");
        }
    }
}

    private Transform FindMuzzleInHand(Transform hand)
    {
        foreach (Transform child in hand)
        {
            // 자식 오브젝트들 중에서 "Muzzle" 이름을 가진 녀석을 찾음
            if (child.name == "Muzzle") return child;
            // 만약 총 오브젝트 안에 Muzzle이 숨어있다면 아래 방식 사용
            Transform target = child.Find("Muzzle");
            if (target != null) return target;
        }
        return null;
    }
}