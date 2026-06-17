using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RankE.UI
{
    /// <summary>
    /// Press-and-hold auto-repeat for a stepper button: after a short delay the held button
    /// fires <see cref="OnRepeat"/> on a fixed interval, so dragging a tick value across a
    /// wide range in the tuning panel doesn't mean dozens of individual clicks. The button's
    /// own <c>onClick</c> still handles the single tap / controller Submit; this only adds the
    /// repeats once the press outlasts the delay, so a quick click never double-fires.
    /// Uses unscaled time (the panel is shown while paused, Time.timeScale untouched anyway).
    /// </summary>
    public sealed class HoldRepeat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Delay = 0.35f;
        const float Interval = 0.05f;

        public Action OnRepeat;

        bool held;
        float nextFire;

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            nextFire = Time.unscaledTime + Delay;
        }

        public void OnPointerUp(PointerEventData e) => held = false;
        public void OnPointerExit(PointerEventData e) => held = false;

        void Update()
        {
            if (!held || Time.unscaledTime < nextFire) return;
            OnRepeat?.Invoke();
            nextFire = Time.unscaledTime + Interval;
        }
    }
}
