using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TallerXRI
{
    /// <summary>
    /// SocketIndicator
    /// =====================================================================
    /// PARA QUÉ SIRVE
    ///   Convierte un hueco (XR Socket Interactor) en algo que el jugador
    ///   entiende de un vistazo: una luz ROJA cuando el hueco está vacío y
    ///   VERDE cuando tiene una pieza dentro, con un "clac" al encajar.
    ///
    /// CONCEPTO DE XRI QUE ILUSTRA
    ///   Un socket es un interactor que SELECCIONA automáticamente cualquier
    ///   objeto que entre en su zona. Cuando eso ocurre, el socket lanza el
    ///   evento "selectEntered"; cuando la pieza sale, "selectExited". Nos
    ///   suscribimos a esos dos eventos para cambiar el color y el sonido.
    ///   Es el mismo patrón que verías en un juego: el hueco "avisa" de que
    ///   ha ocurrido algo.
    ///
    /// CÓMO SE USA
    ///   1. Ponlo en el MISMO GameObject que el XR Socket Interactor.
    ///   2. Arrastra al campo "Indicator" el Renderer de la esfera indicadora.
    ///   3. (Opcional) Asigna un AudioSource y un clip para el sonido al encajar.
    /// </summary>
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

            // El hueco empieza vacío: indicador en rojo.
            SetColor(emptyColor);
        }

        private void OnEnable()
        {
            // Nos suscribimos a los eventos del socket. Hay que desuscribirse en
            // OnDisable para no dejar referencias colgando.
            socket.selectEntered.AddListener(OnSelectEntered);
            socket.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited.RemoveListener(OnSelectExited);
        }

        // Se llama cuando el socket "agarra" una pieza.
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            SetColor(filledColor);

            if (audioSource != null && fillClip != null)
            {
                audioSource.PlayOneShot(fillClip);
            }
        }

        // Se llama cuando la pieza sale del socket (la coge el jugador).
        private void OnSelectExited(SelectExitEventArgs args)
        {
            SetColor(emptyColor);
        }

        private void SetColor(Color color)
        {
            if (indicator != null)
            {
                // Usamos .material (una copia propia) para que cada indicador
                // tenga su color independiente.
                indicator.material.color = color;
            }
        }
    }
}
