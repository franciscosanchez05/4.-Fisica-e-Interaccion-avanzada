using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TallerXRI
{
    
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

        
        private GUIStyle hudStyle;

        private void Update()
        {
            
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || target == null)
            {
                return;
            }

            
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
               
                target.throwOnDetach = !target.throwOnDetach;
            }
        }

       
        private void OnGUI()
        {
            if (!showHud || target == null)
            {
                return;
            }

            if (hudStyle == null)
            {
                
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
