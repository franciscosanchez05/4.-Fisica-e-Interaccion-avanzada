using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PruebaCoger : MonoBehaviour
{
    [SerializeField] Vector3 offset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] string displayName = "";
    XRBaseInteractable interactable;
    int hoverCount;
    int selectCount;
    bool activated;
    GUIStyle style;
    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (string.IsNullOrEmpty(displayName)) displayName = gameObject.name;
    }
    void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
        interactable.activated.AddListener(OnActivated);
        interactable.deactivated.AddListener(OnDeactivated);
    }
    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
        interactable.activated.RemoveListener(OnActivated);
        interactable.deactivated.RemoveListener(OnDeactivated);
    }
    void OnHoverEntered(HoverEnterEventArgs args) { hoverCount++; }
    void OnHoverExited(HoverExitEventArgs args) { hoverCount = Mathf.Max(0, hoverCount - 1); }
    void OnSelectEntered(SelectEnterEventArgs args) { selectCount++; }
    void OnSelectExited(SelectExitEventArgs args)
    {
        selectCount = Mathf.Max(0, selectCount - 1);
        activated = false;
    }
    void OnActivated(ActivateEventArgs args) { activated = true; }
    void OnDeactivated(DeactivateEventArgs args) { activated = false; }
    string CurrentState()
    {
        if (activated) return "ACTIVATE";
        if (selectCount > 0) return "SELECT";
        if (hoverCount > 0) return "HOVER";
        return "IDLE";
    }
    Color CurrentColor()
    {
        if (activated) return new Color(1f, 0.55f, 0.1f);
        if (selectCount > 0) return new Color(0.3f, 1f, 0.4f);
        if (hoverCount > 0) return new Color(1f, 0.92f, 0.3f);
        return Color.white;
    }
    void OnGUI()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(transform.position + offset);
        if (sp.z <= 0f) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
            };
        }
        style.normal.textColor = CurrentColor();
        var rect = new Rect(sp.x - 120f, Screen.height - sp.y - 16f, 240f, 32f);
        GUI.Label(rect, displayName + ": " + CurrentState(), style);
    }
}
