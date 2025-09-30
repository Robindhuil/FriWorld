using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollEventRedirector : MonoBehaviour, IScrollHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public ScrollRect outerScrollRect;

    public void OnScroll(PointerEventData eventData)
    {
        outerScrollRect.OnScroll(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        outerScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        outerScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        outerScrollRect.OnEndDrag(eventData);
    }
}
