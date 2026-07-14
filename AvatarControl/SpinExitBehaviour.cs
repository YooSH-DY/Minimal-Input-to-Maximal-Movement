using UnityEngine;

public class SpinExitBehaviour : StateMachineBehaviour
{
    // Spin 상태 애니메이션이 완전히 끝나고 나갈 때 호출됩니다.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"SpinExitBehaviour.OnStateExit 호출 - 스핀 애니메이션 상태 종료");
        
        // FootGlideController의 OnSpinComplete 메서드 호출
        FootGlideController controller = animator.GetComponent<FootGlideController>();
        if (controller != null)
        {
            controller.OnSpinComplete();
        }
        
        // Walking, Runningman, Spongebob 모두 지원하도록 수정
        bool lastWalking = animator.GetBool("isWalkingLast");
        bool lastRunning = animator.GetBool("isRunningmanLast");
        bool lastSpongebob = animator.GetBool("isSpongebobLast");

        Debug.Log($"SpinExitBehaviour: Walking={lastWalking}, Running={lastRunning}, Spongebob={lastSpongebob}");

        if (lastWalking)
        {
            animator.SetTrigger("ToWalking");
            Debug.Log("SpinExitBehaviour: ToWalking 트리거");
        }
        else if (lastRunning)
        {
            animator.SetTrigger("ToRunningman");
            Debug.Log("SpinExitBehaviour: ToRunningman 트리거");
        }
        else if (lastSpongebob)
        {
            animator.SetTrigger("ToSpongebob");
            Debug.Log("SpinExitBehaviour: ToSpongebob 트리거");
        }
        else
        {
            Debug.LogWarning("SpinExitBehaviour: 어떤 댄스로 돌아갈지 알 수 없음!");
        }
    }
}