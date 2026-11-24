using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TallerXRI
{
    /// <summary>
    /// ToolboxCompletion
    /// =====================================================================
    /// PARA QUÉ SIRVE
    ///   Es el "cerebro" de la caja de herramientas: cuando TODOS los sockets de
    ///   la lista están ocupados, enciende una luz y reproduce un sonido de
    ///   "completado". Es el extra (a) del reto.
    ///
    /// CÓMO FUNCIONA
    ///   Llevamos la cuenta de cuántos sockets tienen pieza con un contador:
    ///     - cuando un socket lanza selectEntered  -> filled + 1
    ///     - cuando un socket lanza selectExited   -> filled - 1
    ///   Si filled es igual al número de sockets, la caja está completa.
    ///   Esta forma de contar no depende de preguntar al socket "¿tienes algo?"
    ///   en el momento exacto del evento, lo que evita errores de orden.
    ///
    /// CÓMO SE USA
    ///   1. Ponlo en un GameObject vacío (en el proyecto, GestorCaja).
    ///   2. Arrastra los tres sockets al array "Sockets".
    ///   3. Arrastra al campo "Complete Light" el objeto que se enciende
    ///      (debe estar DESACTIVADO al empezar).
    /// </summary>
    public class ToolboxCompletion : MonoBehaviour
    {
        [Tooltip("Todos los sockets que deben estar ocupados.")]
        [SerializeField] private XRSocketInteractor[] sockets;

        [Tooltip("Objeto que se activa cuando la caja está completa.")]
        [SerializeField] private GameObject completeLight;

        [Header("Sonido (opcional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip completeClip;

        private int filled;
        private bool wasComplete;

        private void Start()
        {
            // Estado inicial: ningún socket ocupado, luz apagada.
            Refresh();
        }

        private void OnEnable()
        {
            if (sockets == null) { return; }

            foreach (XRSocketInteractor socket in sockets)
            {
                if (socket == null) { continue; }
                socket.selectEntered.AddListener(OnEntered);
                socket.selectExited.AddListener(OnExited);
            }
        }

        private void OnDisable()
        {
            if (sockets == null) { return; }

            foreach (XRSocketInteractor socket in sockets)
            {
                if (socket == null) { continue; }
                socket.selectEntered.RemoveListener(OnEntered);
                socket.selectExited.RemoveListener(OnExited);
            }
        }

        private void OnEntered(SelectEnterEventArgs args)
        {
            filled++;
            Refresh();
        }

        private void OnExited(SelectExitEventArgs args)
        {
            filled = Mathf.Max(0, filled - 1);
            Refresh();
        }

        private void Refresh()
        {
            bool complete = sockets != null && sockets.Length > 0 && filled >= sockets.Length;

            if (completeLight != null)
            {
                completeLight.SetActive(complete);
            }

            // Solo suena en el momento en que PASA a estar completa, no en cada refresco.
            if (complete && !wasComplete && audioSource != null && completeClip != null)
            {
                audioSource.PlayOneShot(completeClip);
            }

            wasComplete = complete;
        }
    }
}
