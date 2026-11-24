using UnityEngine;

namespace TallerXRI
{
    /// <summary>
    /// ImpactSound
    /// =====================================================================
    /// PARA QUÉ SIRVE
    ///   Reproduce un sonido de golpe cuando el objeto choca contra algo, con el
    ///   volumen proporcional a la fuerza del choque. Un golpe suave suena bajo
    ///   y uno fuerte suena alto: refuerza mucho la sensación de que el objeto
    ///   es físico (es la parte de "física" del tema del proyecto).
    ///
    /// CÓMO FUNCIONA
    ///   - Unity llama a OnCollisionEnter cada vez que este Rigidbody empieza a
    ///     tocar otro collider.
    ///   - collision.relativeVelocity es la velocidad del choque. Si es muy baja
    ///     (el objeto solo reposa), ignoramos el evento.
    ///   - Mathf.InverseLerp convierte esa velocidad en un volumen entre 0 y 1
    ///     (minSpeed -> 0, maxSpeed -> 1).
    ///   - "cooldown" evita que un objeto que rebota muchas veces seguidas
    ///     genere decenas de sonidos en unos pocos milisegundos.
    ///
    /// CÓMO SE USA
    ///   Ponlo en objetos con Rigidbody y asigna uno o varios clips (si hay
    ///   varios, elige uno al azar para que no suene siempre igual).
    /// </summary>
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
            source.spatialBlend = 1f; // sonido 3D
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

            // Math.Max(0.1f, ...) garantiza que incluso un golpe suave se oiga un poco.
            source.PlayOneShot(clip, Mathf.Max(0.1f, volume));
        }
    }
}
