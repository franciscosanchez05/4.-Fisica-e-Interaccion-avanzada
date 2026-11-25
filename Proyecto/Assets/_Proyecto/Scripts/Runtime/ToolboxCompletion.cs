using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TallerXRI
{
    
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

            
            if (complete && !wasComplete && audioSource != null && completeClip != null)
            {
                audioSource.PlayOneShot(completeClip);
            }

            wasComplete = complete;
        }
    }
}
