using UnityEngine;
using UnityEngine.UI;

public class ScrollSynchronizer : MonoBehaviour
{
    public ScrollRect sourceScrollRect;
    public ScrollRect targetScrollRect;

    void Update()
    {
        if (sourceScrollRect != null && targetScrollRect != null)
        {
            targetScrollRect.horizontalNormalizedPosition = sourceScrollRect.horizontalNormalizedPosition;
            targetScrollRect.verticalNormalizedPosition = sourceScrollRect.verticalNormalizedPosition;
        }
    }
}
