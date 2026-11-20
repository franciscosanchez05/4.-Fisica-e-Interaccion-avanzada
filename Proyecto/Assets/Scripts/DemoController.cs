using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DemoController : MonoBehaviour
{
    [SerializeField] XRGrabInteractable target;
    [SerializeField] bool showHud = true;
    GUIStyle style;
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || target == null) return;
        if (kb.f1Key.wasPressedThisFrame)
            target.movementType = XRBaseInteractable.MovementType.Instantaneous;
        if (kb.f2Key.wasPressedThisFrame)
            target.movementType = XRBaseInteractable.MovementType.Kinematic;
        if (kb.f3Key.wasPressedThisFrame)
            target.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        if (kb.f4Key.wasPressedThisFrame) target.throwOnDetach = !target.throwOnDetach;
    }
    void OnGUI()
    {
        if (!showHud || target == null) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(20, 20, 900, 36),
        "Movement Type: " + target.movementType, style);
        GUI.Label(new Rect(20, 92, 900, 36),
        "F1 Instantaneous | F2 Kinematic | F3 Velocity Tracking", style);
        GUI.Label(new Rect(20, 56, 900, 36),
        "Throw On Detach: " + target.throwOnDetach + " [F4]", style);
    }
}   
