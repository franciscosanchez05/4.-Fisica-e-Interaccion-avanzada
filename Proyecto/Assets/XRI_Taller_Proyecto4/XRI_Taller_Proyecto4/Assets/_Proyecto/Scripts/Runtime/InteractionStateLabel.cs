using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TallerXRI
{
    /// <summary>
    /// InteractionStateLabel
    /// =====================================================================
    /// PARA QUÉ SIRVE
    ///   Dibuja encima del objeto una etiqueta con su ESTADO de interacción:
    ///   IDLE, HOVER, SELECT o ACTIVATE. Sirve para explicar visualmente los
    ///   tres estados de XRI (bloque 1 de la demo y parte de conceptos).
    ///
    /// LOS ESTADOS EN XRI
    ///   - HOVER    : una mano o un rayo se acerca / apunta al objeto.
    ///   - SELECT   : el interactor lo ha agarrado.
    ///   - ACTIVATE : con el objeto ya agarrado, se pulsa el gatillo (acción
    ///                secundaria, por ejemplo disparar o encender algo).
    ///
    /// CÓMO FUNCIONA
    ///   Cada XRBaseInteractable expone eventos (hoverEntered, hoverExited,
    ///   selectEntered, selectExited, activated, deactivated). Nos suscribimos a
    ///   ellos en OnEnable y nos damos de baja en OnDisable (si no lo hiciéramos,
    ///   quedarían suscripciones "colgadas" al destruir o desactivar el objeto).
    ///
    ///   Usamos CONTADORES (hoverCount, selectCount) en lugar de booleanos porque
    ///   varios interactores pueden actuar a la vez sobre el mismo objeto (por
    ///   ejemplo, las dos manos). El objeto sigue en HOVER mientras quede al menos
    ///   un interactor apuntando y en SELECT mientras quede al menos uno agarrando.
    ///
    /// CÓMO SE USA
    ///   Ponlo en cualquier objeto que tenga un XR Grab Interactable.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class InteractionStateLabel : MonoBehaviour
    {
        [Tooltip("Desplazamiento de la etiqueta respecto al centro del objeto (en el mundo).")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.25f, 0f);

        [Tooltip("Nombre que se muestra. Si está vacío, se usa el nombre del GameObject.")]
        [SerializeField] private string displayName = "";

        private XRBaseInteractable interactable;
        private int hoverCount;
        private int selectCount;
        private bool activated;
        private GUIStyle labelStyle;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = gameObject.name;
            }
        }

        private void OnEnable()
        {
            // Los eventos de XRI son UnityEvents: AddListener registra nuestro método.
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
            interactable.activated.AddListener(OnActivated);
            interactable.deactivated.AddListener(OnDeactivated);
        }

        private void OnDisable()
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
            interactable.activated.RemoveListener(OnActivated);
            interactable.deactivated.RemoveListener(OnDeactivated);
        }

        // Cada manejador recibe unos "EventArgs" con información del evento
        // (quién interactúa, con qué objeto...). Aquí solo nos interesa que ocurrió.
        private void OnHoverEntered(HoverEnterEventArgs args) { hoverCount++; }

        // Mathf.Max(0, ...) evita que el contador baje de cero si algún evento
        // llegara duplicado.
        private void OnHoverExited(HoverExitEventArgs args) { hoverCount = Mathf.Max(0, hoverCount - 1); }

        private void OnSelectEntered(SelectEnterEventArgs args) { selectCount++; }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            selectCount = Mathf.Max(0, selectCount - 1);

            // Si se suelta el objeto, ya no puede estar "activado".
            activated = false;
        }

        private void OnActivated(ActivateEventArgs args) { activated = true; }
        private void OnDeactivated(DeactivateEventArgs args) { activated = false; }

        // El estado "más fuerte" gana: ACTIVATE > SELECT > HOVER > IDLE.
        private string CurrentState()
        {
            if (activated) { return "ACTIVATE"; }
            if (selectCount > 0) { return "SELECT"; }
            if (hoverCount > 0) { return "HOVER"; }
            return "IDLE";
        }

        private Color CurrentColor()
        {
            if (activated) { return new Color(1f, 0.55f, 0.1f); }   // naranja
            if (selectCount > 0) { return new Color(0.3f, 1f, 0.4f); } // verde
            if (hoverCount > 0) { return new Color(1f, 0.92f, 0.3f); } // amarillo
            return Color.white;
        }

        private void OnGUI()
        {
            // La etiqueta se coloca en pantalla: convertimos la posición del mundo
            // (objeto + offset) a coordenadas de pantalla con la cámara principal.
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 screenPoint = cam.WorldToScreenPoint(transform.position + offset);

            // z <= 0 significa que el punto está DETRÁS de la cámara: no se dibuja.
            if (screenPoint.z <= 0f)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            labelStyle.normal.textColor = CurrentColor();

            // En la pantalla de OnGUI el eje Y crece hacia ABAJO (al revés que en
            // WorldToScreenPoint), por eso se usa Screen.height - screenPoint.y.
            Rect rect = new Rect(screenPoint.x - 120f, Screen.height - screenPoint.y - 16f, 240f, 32f);
            GUI.Label(rect, displayName + ": " + CurrentState(), labelStyle);
        }
    }
}
