using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static UnityEngine.EventSystems.EventTrigger;

public class FeedBackAgarre : MonoBehaviour
{
    [Header("Color (opcional)")]
    [SerializeField] Renderer[] renderers;
    [SerializeField] Color hoverColor = new Color(1f, 0.92f, 0.3f);
    [SerializeField] Color selectColor = new Color(0.3f, 1f, 0.4f);
    [Header("Sonido (opcional)")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip hoverClip;
    [SerializeField] AudioClip grabClip;
    [SerializeField] AudioClip dropClip;
    XRBaseInteractable interactable;
    Color[] originalColors;
    int hoverCount;
    int selectCount;
    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }
    void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
    }
    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
    }
    void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;
        hoverCount++;
       
    Play(hoverClip);
        ApplyColor();
    }
    void OnHoverExited(HoverExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;
        hoverCount = Mathf.Max(0, hoverCount - 1);
        ApplyColor();
    }
    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;
        selectCount++;
        Play(grabClip);
        ApplyColor();
    }
    void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;
        selectCount = Mathf.Max(0, selectCount - 1);
        Play(dropClip);
        ApplyColor();
    }
    void ApplyColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (selectCount > 0) renderers[i].material.color = selectColor;
            else if (hoverCount > 0) renderers[i].material.color = hoverColor;
            else renderers[i].material.color = originalColors[i];
        }
    }
    void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }
}
