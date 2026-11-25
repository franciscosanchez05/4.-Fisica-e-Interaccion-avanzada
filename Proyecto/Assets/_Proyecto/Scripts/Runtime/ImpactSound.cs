using UnityEngine;

namespace TallerXRI
{
    
    [RequireComponent(typeof(Rigidbody))]
    public class ImpactSound : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;

        [Tooltip("Velocidad de choque (m/s) por debajo de la cual no suena nada.")]
        [SerializeField] private float minSpeed = 0.6f;

        [Tooltip("Velocidad de choque (m/s) a partir de la cual suena al volumen máximo.")]
        [SerializeField] private float maxSpeed = 5f;

        [Tooltip("Tiempo mínimo (s) entre dos sonidos seguidos.")]
        [SerializeField] private float cooldown = 0.08f;

        private AudioSource source;
        private float lastTime;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.spatialBlend = 1f; 
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            float speed = collision.relativeVelocity.magnitude;

            if (speed < minSpeed || Time.time - lastTime < cooldown)
            {
                return;
            }

            lastTime = Time.time;

            float volume = Mathf.Clamp01(Mathf.InverseLerp(minSpeed, maxSpeed, speed));
            AudioClip clip = clips[Random.Range(0, clips.Length)];

            
            source.PlayOneShot(clip, Mathf.Max(0.1f, volume));
        }
    }
}
