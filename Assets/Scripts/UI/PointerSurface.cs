using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SinWheel
{
    /// <summary>
    /// A tap/hold/release surface. The interludes are one-thumb games — tap,
    /// hold, release, nothing else — and Unity's Button only reports clicks, so
    /// the ones that care about press and release use this instead.
    /// </summary>
    public sealed class PointerSurface : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event Action Pressed;
        public event Action Released;

        public void OnPointerDown(PointerEventData eventData) => Pressed?.Invoke();
        public void OnPointerUp(PointerEventData eventData) => Released?.Invoke();
    }
}
