using System;
using UnityEngine;

/// <summary>
/// Internal helper that forwards OnTriggerEnter to a delegate.
/// Used by NpcProximityDetector to receive trigger events from its sibling collider object.
/// </summary>
[AddComponentMenu("")]
internal sealed class TriggerForwarder : MonoBehaviour
{
    internal Action<Collider> OnTriggerEnterAction;

    private void OnTriggerEnter(Collider other) => OnTriggerEnterAction?.Invoke(other);
}
