using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TallerXRI
{
    
    [RequireComponent(typeof(XRSocketInteractor))]
    public class SocketIndicator : MonoBehaviour
    {
        [Tooltip("Renderer del objeto que cambia de color (por ejemplo, una esfera pequeña).")]
        [SerializeField] private Renderer indicator;

        [SerializeField] private Color emptyColor = Color.red;
        [SerializeField] private Color filledColor = Color.green;

        [Header("Sonido (opcional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fillClip;

        private XRSocketInteractor socket;

        private void Awake()
        {
            socket = GetComponent<XRSocketInteractor>();

            
            SetColor(emptyColor);
        }

        private void OnEnable()
        {
            
            socket.selectEntered.AddListener(OnSelectEntered);
            socket.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited.RemoveListener(OnSelectExited);
        }

       
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            SetColor(filledColor);

            if (audioSource != null && fillClip != null)
            {
                audioSource.PlayOneShot(fillClip);
            }
        }

        
        private void OnSelectExited(SelectExitEventArgs args)
        {
            SetColor(emptyColor);
        }

        private void SetColor(Color color)
        {
            if (indicator != null)
            {
               
                indicator.material.color = color;
            }
        }
    }
}
