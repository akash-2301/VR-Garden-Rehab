using UnityEngine;

public class MidpointAnimatorHelper : MonoBehaviour
{
    public Animator animator;               // assign inspector
    public int layerIndex = 0;              // usually 0

    // saved by animation event
    private int savedStateHash = 0;
    private float savedNormalizedTime = 0f;
    private bool hasSavedMidpoint = false;

    // Called by an Animation Event placed at the middle frame
    public void MarkMidpoint()
    {
        if (animator == null) return;

        // get current playing state info on the layer
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        savedStateHash = info.shortNameHash;
        // normalizedTime may be >1 if looping; keep fractional part
        savedNormalizedTime = info.normalizedTime % 1f;
        hasSavedMidpoint = true;

        Debug.Log($"Marked midpoint: stateHash={savedStateHash}, normTime={savedNormalizedTime:F3}");
    }

    // Call this when you want to start from that midpoint
    public void PlayFromMarkedMidpoint()
    {
        if (animator == null || !hasSavedMidpoint)
        {
            Debug.LogWarning("No midpoint saved or animator missing.");
            return;
        }

        // Play the saved state at the saved normalized time
        animator.Play(savedStateHash, layerIndex, savedNormalizedTime);

        // Force an immediate sample so the pose updates right away
        animator.Update(0f);

        // Ensure animator actually plays (if you use animator.speed elsewhere)
        animator.speed = 1f;

        Debug.Log($"Jumped to midpoint: stateHash={savedStateHash}, normTime={savedNormalizedTime:F3}");
    }

    public bool HasMidpoint() => hasSavedMidpoint;
}
