using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TallerXRI
{
   
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

        
        private void OnHoverEntered(HoverEnterEventArgs args) { hoverCount++; }

        
        private void OnHoverExited(HoverExitEventArgs args) { hoverCount = Mathf.Max(0, hoverCount - 1); }

        private void OnSelectEntered(SelectEnterEventArgs args) { selectCount++; }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            selectCount = Mathf.Max(0, selectCount - 1);

            activated = false;
        }

        private void OnActivated(ActivateEventArgs args) { activated = true; }
        private void OnDeactivated(DeactivateEventArgs args) { activated = false; }

        
        private string CurrentState()
        {
            if (activated) { return "ACTIVATE"; }
            if (selectCount > 0) { return "SELECT"; }
            if (hoverCount > 0) { return "HOVER"; }
            return "IDLE";
        }

        private Color CurrentColor()
        {
            if (activated) { return new Color(1f, 0.55f, 0.1f); }   
            if (selectCount > 0) { return new Color(0.3f, 1f, 0.4f); } 
            if (hoverCount > 0) { return new Color(1f, 0.92f, 0.3f); } 
            return Color.white;
        }

        private void OnGUI()
        {
            
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 screenPoint = cam.WorldToScreenPoint(transform.position + offset);

           
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

           
            Rect rect = new Rect(screenPoint.x - 120f, Screen.height - screenPoint.y - 16f, 240f, 32f);
            GUI.Label(rect, displayName + ": " + CurrentState(), labelStyle);
        }
    }
}
