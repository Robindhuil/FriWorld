using UnityEngine;

public class RandomizeAnimatorOffset : MonoBehaviour
{
    [SerializeField] private bool randomizeSpeed = true;
    [SerializeField] private float minSpeed = 0.9f;
    [SerializeField] private float maxSpeed = 1.1f;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("[RandomizeAnimatorOffset] Animator not found on " + gameObject.name);
            return;
        }

        if (randomizeSpeed)
        {
            animator.speed = Random.Range(minSpeed, maxSpeed);
        }

        StartCoroutine(RandomizeStartOffsetNextFrame());
    }

    System.Collections.IEnumerator RandomizeStartOffsetNextFrame()
    {
        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float randomTime = Random.Range(0f, 1f);

        animator.Play(stateInfo.fullPathHash, 0, randomTime);
    }
}
