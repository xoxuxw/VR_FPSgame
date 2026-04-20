using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabObject : MonoBehaviour
{
    public enum Side { Left, Right }
    [Header("설정")]
    public Side handSide = Side.Right;
    public LayerMask grabbedLayer;      // 물체의 레이어 (예: Item)
    public float grabRange = 0.3f;      // 잡기 인식 거리
    public float throwPower = 1.2f;     // 던지는 힘 배율

    private bool isGrabbing = false;
    private GameObject grabbedObject;
    
    // 던지기 관성 계산용
    private Vector3 prevPos;
    private Quaternion prevRot;

    void Update()
    {
        // 1. ARAVRInput 컨트롤러 및 위치 정보 가져오기
        ARAVRInput.Controller controller = (handSide == Side.Left) ? 
            ARAVRInput.Controller.LTouch : ARAVRInput.Controller.RTouch;

        Vector3 currentHandPos = (handSide == Side.Left) ? 
            ARAVRInput.LHandPosition : ARAVRInput.RHandPosition;
        
        Transform currentHandTr = (handSide == Side.Left) ? 
            ARAVRInput.LHand : ARAVRInput.RHand;

        if (!isGrabbing)
        {
            // 중지(Grip) 버튼을 누를 때 잡기 시도
            if (ARAVRInput.GetDown(ARAVRInput.Button.HandTrigger, controller))
            {
                TryGrab(currentHandPos, currentHandTr);
            }
        }
        else
        {
            // 중지(Grip) 버튼을 떼면 놓기
            if (ARAVRInput.GetUp(ARAVRInput.Button.HandTrigger, controller))
            {
                TryUngrab(currentHandPos, currentHandTr);
            }
            else
            {
                // 잡고 있는 동안 관성 데이터 갱신
                prevPos = currentHandPos;
                prevRot = currentHandTr.rotation;
            }
        }
    }

    private void TryGrab(Vector3 handPos, Transform handTr)
    {
        Collider[] hitObjects = Physics.OverlapSphere(handPos, grabRange, grabbedLayer);

        if (hitObjects.Length > 0)
        {
            int closest = 0;
            float minDistance = Vector3.Distance(handPos, hitObjects[0].transform.position);

            for (int i = 1; i < hitObjects.Length; i++)
            {
                float dist = Vector3.Distance(handPos, hitObjects[i].transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = i;
                }
            }

            isGrabbing = true;
            grabbedObject = hitObjects[closest].gameObject;
            
            Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;

            // [핵심] 물체를 손의 자식으로 설정
            grabbedObject.transform.parent = handTr;

            // [핵심] 손잡이(GrabPoint) 정렬 로직
            Transform grabPoint = grabbedObject.transform.Find("GrabPoint");
            if (grabPoint != null)
            {
                // GrabPoint의 위치와 회전을 손(handTr)에 일치시킴
                grabbedObject.transform.localRotation = Quaternion.Inverse(grabPoint.localRotation);
                grabbedObject.transform.localPosition = Vector3.zero; // 기준점 초기화
                
                // 손 위치에서 GrabPoint의 오프셋만큼 역으로 이동
                Vector3 offset = grabPoint.position - grabbedObject.transform.position;
                grabbedObject.transform.position -= offset;
            }
            else
            {
                // GrabPoint가 없으면 그냥 중심점에 부착
                grabbedObject.transform.localPosition = Vector3.zero;
                grabbedObject.transform.localRotation = Quaternion.identity;
            }
            
            // 데이터 초기화
            prevPos = handPos;
            prevRot = handTr.rotation;

            // 잡았을 때 짧은 진동 피드백
            ARAVRInput.PlayVibration((handSide == Side.Left) ? 
                ARAVRInput.Controller.LTouch : ARAVRInput.Controller.RTouch);
        }
    }

    private void TryUngrab(Vector3 handPos, Transform handTr)
    {
        if (grabbedObject == null) return;

        // 속도 및 회전 변화량 계산
        Vector3 velocity = (handPos - prevPos) / Time.deltaTime;
        Quaternion deltaRot = handTr.rotation * Quaternion.Inverse(prevRot);
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
        Vector3 angularVelocity = (axis * angle * Mathf.Deg2Rad) / Time.deltaTime;

        isGrabbing = false;
        grabbedObject.transform.parent = null;

        Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.velocity = velocity * throwPower;
            rb.angularVelocity = angularVelocity;
        }

        grabbedObject = null;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 handPos = (handSide == Side.Left) ? ARAVRInput.LHandPosition : ARAVRInput.RHandPosition;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(handPos, grabRange);
    }
}