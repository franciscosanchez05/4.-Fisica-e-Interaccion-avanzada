using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TallerXRI
{
    /// <summary>
    /// MovementTypeSwitcher
    /// =====================================================================
    /// PARA QUÉ SIRVE
    ///   Permite cambiar EN DIRECTO, con el teclado, el "Movement Type" de un
    ///   XR Grab Interactable mientras el juego está en Play, y muestra en
    ///   pantalla qué valor está activo. Está pensado para el BLOQUE 2 de la
    ///   demo: así puedes enseñar los tres modos sin tener que ir al
    ///   Inspector cada vez (ahorra tiempo y se ve mucho más claro en el vídeo).
    ///
    /// CONCEPTO DE XRI QUE ILUSTRA
    ///   El Movement Type decide CÓMO se mueve el objeto cuando lo agarras:
    ///     - Instantaneous    : se coloca en la pose de la mano cada frame
    ///                          (teletransporte). Latencia mínima, pero NO
    ///                          respeta la física: atraviesa paredes.
    ///     - Kinematic        : se mueve con un Rigidbody cinemático en
    ///                          FixedUpdate. Más fiel a la física, pero al ser
    ///                          cinemático tampoco lo frena una pared.
    ///     - VelocityTracking : se le dan velocidad y velocidad angular para
    ///                          que alcance la pose. Es física "de verdad":
    ///                          choca con las paredes y se detiene.
    ///
    /// CÓMO SE USA
    ///   1. Ponlo en un GameObject vacío (en el proyecto se llama DemoController).
    ///   2. Arrastra al campo "Target" el objeto que tenga el XR Grab Interactable
    ///      (en el proyecto, CuboPruebas).
    ///   3. En Play pulsa las teclas configuradas (por defecto Z, X, C y V).
    ///      Si alguna choca con las teclas del XR Interaction Simulator, cámbiala
    ///      en el Inspector.
    ///
    /// NOTA TÉCNICA
    ///   Leemos el teclado con el Input System nuevo (Keyboard.current), que es
    ///   el que usa XRI. Las propiedades movementType y throwOnDetach del
    ///   XRGrabInteractable se pueden cambiar en tiempo de ejecución.
    /// </summary>
    [DisallowMultipleComponent]
    public class MovementTypeSwitcher : MonoBehaviour
    {
        [Header("Objeto que se controla")]
        [Tooltip("El XR Grab Interactable cuyo Movement Type vamos a cambiar.")]
        [SerializeField] private XRGrabInteractable target;

        [Header("Teclas (cámbialas si chocan con el simulador)")]
        [SerializeField] private Key instantaneousKey = Key.Z;
        [SerializeField] private Key kinematicKey = Key.X;
        [SerializeField] private Key velocityTrackingKey = Key.C;
        [SerializeField] private Key throwOnDetachKey = Key.V;

        [Header("Pantalla")]
        [Tooltip("Muestra en la esquina superior izquierda el valor actual.")]
        [SerializeField] private bool showHud = true;

        // Estilo de texto para OnGUI. Se crea una sola vez (crearlo en cada
        // frame generaría basura innecesaria).
        private GUIStyle hudStyle;

        private void Update()
        {
            // Keyboard.current es null si no hay teclado conectado (por ejemplo
            // en un visor sin teclado). Comprobarlo evita errores.
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || target == null)
            {
                return;
            }

            // wasPressedThisFrame es true SOLO en el frame en que se pulsa la tecla,
            // así que cambiamos el valor una vez por pulsación.
            if (keyboard[instantaneousKey].wasPressedThisFrame)
            {
                target.movementType = XRBaseInteractable.MovementType.Instantaneous;
            }

            if (keyboard[kinematicKey].wasPressedThisFrame)
            {
                target.movementType = XRBaseInteractable.MovementType.Kinematic;
            }

            if (keyboard[velocityTrackingKey].wasPressedThisFrame)
            {
                target.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            }

            if (keyboard[throwOnDetachKey].wasPressedThisFrame)
            {
                // Alterna "Throw On Detach": si está activo, al soltar el objeto
                // hereda la velocidad de la mano y sale lanzado.
                target.throwOnDetach = !target.throwOnDetach;
            }
        }

        // OnGUI dibuja en pantalla (vista Game). No se ve dentro de unas gafas,
        // pero sí en la grabación con el simulador, que es lo que necesitamos.
        private void OnGUI()
        {
            if (!showHud || target == null)
            {
                return;
            }

            if (hudStyle == null)
            {
                // GUI.skin solo se puede usar dentro de OnGUI, por eso el estilo
                // se crea aquí y no en Awake.
                hudStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
                hudStyle.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(20, 20, 900, 34),
                "Movement Type: " + target.movementType, hudStyle);
            GUI.Label(new Rect(20, 54, 900, 34),
                "Throw On Detach: " + target.throwOnDetach, hudStyle);
            GUI.Label(new Rect(20, 88, 900, 34),
                "[" + instantaneousKey + "] Instantaneous  [" + kinematicKey + "] Kinematic  ["
                + velocityTrackingKey + "] Velocity Tracking  [" + throwOnDetachKey + "] Throw", hudStyle);
        }
    }
}
