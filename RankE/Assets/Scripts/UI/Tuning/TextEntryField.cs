using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// A minimal click-to-type text field driven by the new Input System (the project ships
    /// with the legacy Input backend off, so uGUI's <c>InputField</c> can't receive keystrokes).
    /// Click to focus, type to edit, Enter to submit, Esc to cancel focus. Dev-tool quality —
    /// used to name tuning presets in the control panel.
    /// </summary>
    public sealed class TextEntryField : MonoBehaviour, IPointerClickHandler
    {
        Text label;
        string placeholder = "";
        string value = "";
        bool focused;
        bool subscribed;

        /// <summary>Raised when the user presses Enter, with the current text.</summary>
        public Action<string> Submitted;

        public string Value
        {
            get => value;
            set { this.value = value ?? ""; Render(); }
        }

        public void Init(Text label, string placeholder)
        {
            this.label = label;
            this.placeholder = placeholder ?? "";
            Render();
        }

        public void OnPointerClick(PointerEventData e) => SetFocus(true);

        void SetFocus(bool on)
        {
            focused = on;
            Subscribe(on);
            Render();
        }

        void Subscribe(bool want)
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (want && !subscribed) { kb.onTextInput += OnText; subscribed = true; }
            else if (!want && subscribed) { kb.onTextInput -= OnText; subscribed = false; }
        }

        void OnText(char c)
        {
            if (!focused) return;
            if (c == '\b' || c == (char)127)
            {
                if (value.Length > 0) value = value.Substring(0, value.Length - 1);
            }
            else if (c == '\n' || c == '\r')
            {
                return; // Enter handled in Update so submit fires once
            }
            else if (!char.IsControl(c))
            {
                value += c;
            }
            Render();
        }

        void Update()
        {
            if (!focused) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                SetFocus(false);
                Submitted?.Invoke(value);
            }
            else if (kb.escapeKey.wasPressedThisFrame)
            {
                SetFocus(false);
            }
        }

        void Render()
        {
            if (label == null) return;
            if (string.IsNullOrEmpty(value) && !focused)
            {
                label.text = placeholder;
                label.color = new Color(0.5f, 0.5f, 0.6f);
            }
            else
            {
                label.text = focused ? value + "_" : value;
                label.color = Color.white;
            }
        }

        void OnDisable() => Subscribe(false);
        void OnDestroy() => Subscribe(false);
    }
}
