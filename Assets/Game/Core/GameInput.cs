using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Lithostride.Core
{
    /// <summary>
    /// Único ponto que lê teclado e mouse (Input System), uma vez por quadro,
    /// antes de todos. Ações de mundo (clique, roda, movimento) são bloqueadas
    /// quando o ponteiro está sobre a interface ou há campo de texto em foco:
    /// um clique nunca ataca, minera e aperta botão ao mesmo tempo.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameInput : MonoBehaviour
    {
        [SerializeField] private EventSystem eventSystem;

        public float MoveAxis { get; private set; }
        public float VerticalAxis { get; private set; }
        public bool RunHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }

        /// <summary>Clique esquerdo no mundo (fora da interface).</summary>
        public bool PrimaryPressed { get; private set; }
        public bool PrimaryHeld { get; private set; }
        public bool SecondaryPressed { get; private set; }

        public Vector2 PointerScreen { get; private set; }
        public bool PointerOverUi { get; private set; }

        /// <summary>Passos da roda do mouse (0 sobre a interface).</summary>
        public float ScrollSteps { get; private set; }

        /// <summary>1, 2 ou 3 apertado neste quadro (modo), ou 0.</summary>
        public int ModeKey { get; private set; }

        /// <summary>Q = -1, E = +1 neste quadro.</summary>
        public int Cycle { get; private set; }

        public bool ShapeCyclePressed { get; private set; }
        public bool PanelTogglePressed { get; private set; }
        public bool EscapePressed { get; private set; }
        public bool NoclipTogglePressed { get; private set; }

        /// <summary>Bloqueio pedido por outro sistema (morte, pausa): zera as ações de jogo.</summary>
        public bool GameplayBlocked { get; set; }

        public void Configure(EventSystem system)
        {
            eventSystem = system;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            bool typing = eventSystem != null && eventSystem.currentSelectedGameObject != null &&
                          eventSystem.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;

            PointerScreen = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            PointerOverUi = eventSystem != null && eventSystem.IsPointerOverGameObject();

            bool keys = keyboard != null && !typing;
            PanelTogglePressed = keys && keyboard.f1Key.wasPressedThisFrame;
            EscapePressed = keys && keyboard.escapeKey.wasPressedThisFrame;

            bool play = keys && !GameplayBlocked;
            float left = play && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1f : 0f;
            float right = play && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1f : 0f;
            float down = play && (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1f : 0f;
            float up = play && (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1f : 0f;
            MoveAxis = right - left;
            VerticalAxis = up - down;
            RunHeld = play && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            JumpPressed = play && (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame ||
                                   keyboard.upArrowKey.wasPressedThisFrame);
            JumpHeld = play && (keyboard.spaceKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);

            ModeKey = !play ? 0 : keyboard.digit1Key.wasPressedThisFrame ? 1 : keyboard.digit2Key.wasPressedThisFrame ? 2
                : keyboard.digit3Key.wasPressedThisFrame ? 3 : 0;
            Cycle = !play ? 0 : keyboard.qKey.wasPressedThisFrame ? -1 : keyboard.eKey.wasPressedThisFrame ? 1 : 0;
            ShapeCyclePressed = play && keyboard.rKey.wasPressedThisFrame;
            NoclipTogglePressed = play && keyboard.nKey.wasPressedThisFrame;

            bool world = mouse != null && !PointerOverUi && !GameplayBlocked;
            PrimaryPressed = world && mouse.leftButton.wasPressedThisFrame;
            PrimaryHeld = world && mouse.leftButton.isPressed;
            SecondaryPressed = world && mouse.rightButton.wasPressedThisFrame;

            float scroll = mouse != null && !PointerOverUi ? mouse.scroll.ReadValue().y : 0f;
            // No Windows um passo da roda chega como 120; normalizado, como 1.
            ScrollSteps = Mathf.Abs(scroll) >= 10f ? scroll / 120f : scroll;
        }
    }
}
