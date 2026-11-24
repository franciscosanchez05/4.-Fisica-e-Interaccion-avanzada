using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TallerXRI
{
    /// <summary>
    /// GrabFeedback
    /// =====================================================================
    /// PARA QUÉ SIRVE
    ///   Da "feedback" al jugador (la idea de los AFFORDANCES): el objeto cambia
    ///   de color cuando la mano se acerca (hover) o lo agarra (select) y
    ///   reproduce sonidos al acercar, agarrar y soltar.
    ///
    ///   Es el "plan B" sencillo del sistema de affordances de XRI: hace lo
    ///   mismo con unas pocas líneas de código y no depende de que ese sistema
    ///   exista en la versión del paquete.
    ///
    /// DETALLE IMPORTANTE: IGNORA LOS SOCKETS
    ///   Cuando una herramienta está colocada en su socket, el socket también
    ///   la "selecciona". Si no lo ignoráramos, la herramienta se quedaría
    ///   verde dentro del hueco. Por eso, en cada evento, comprobamos si el
    ///   interactor es un XRSocketInteractor y, en ese caso, no hacemos nada.
    ///
    /// CÓMO SE USA
    ///   Ponlo en los objetos con XR Grab Interactable. Si no rellenas la lista
    ///   de Renderers, usa todos los renderers del objeto y sus hijos.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class GrabFeedback : MonoBehaviour
    {
        [Header("Color (opcional)")]
        [Tooltip("Renderers que cambian de color. Vacío = todos los del objeto y sus hijos.")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Color hoverColor = new Color(1f, 0.92f, 0.3f);
        [SerializeField] private Color selectColor = new Color(0.3f, 1f, 0.4f);

        [Header("Sonido (opcional)")]
        [Tooltip("Si está vacío, se crea un AudioSource 3D automáticamente.")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip grabClip;
        [SerializeField] private AudioClip dropClip;

        private XRBaseInteractable interactable;
        private Color[] originalColors;
        private int hoverCount;
        private int selectCount;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            // Guardamos el color original de cada renderer para poder restaurarlo.
            // Nota: renderer.material crea una COPIA del material para este objeto,
            // así que cambiar su color no afecta a otros objetos que compartan material.
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 1 = sonido totalmente 3D (se oye desde donde está el objeto)
            }
        }

        private void OnEnable()
        {
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // args.interactorObject es QUIÉN está interactuando (mano, rayo, socket...).
            if (args.interactorObject is XRSocketInteractor) { return; }

            hoverCount++;
            Play(hoverClip);
            ApplyColor();
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor) { return; }

            hoverCount = Mathf.Max(0, hoverCount - 1);
            ApplyColor();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor) { return; }

            selectCount++;
            Play(grabClip);
            ApplyColor();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor) { return; }

            selectCount = Mathf.Max(0, selectCount - 1);
            Play(dropClip);
            ApplyColor();
        }

        // Prioridad de color: agarrado > hover > color original.
        private void ApplyColor()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (selectCount > 0)
                {
                    renderers[i].material.color = selectColor;
                }
                else if (hoverCount > 0)
                {
                    renderers[i].material.color = hoverColor;
                }
                else
                {
                    renderers[i].material.color = originalColors[i];
                }
            }
        }

        private void Play(AudioClip clip)
        {
            // PlayOneShot permite que se solapen varios sonidos sin cortarse.
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
