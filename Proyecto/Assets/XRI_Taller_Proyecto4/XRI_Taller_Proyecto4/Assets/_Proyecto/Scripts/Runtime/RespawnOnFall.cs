using UnityEngine;

namespace TallerXRI
{
    /// <summary>
    /// RespawnOnFall
    /// =====================================================================
    /// PARA QUÉ SIRVE
    ///   Evita el problema típico de las demos de VR: alguien lanza un objeto
    ///   y se cae fuera del mundo (o rueda tras la mesa) y hay que parar la
    ///   grabación para recolocarlo. Con este script, si el objeto cae por
    ///   debajo de una altura mínima, vuelve solo a su posición inicial.
    ///
    /// CÓMO FUNCIONA
    ///   - En Awake guardamos la posición y la rotación iniciales.
    ///   - En Update comprobamos la altura (Y). Si es menor que "minY",
    ///     llamamos a Respawn().
    ///   - Respawn() pone a cero las velocidades del Rigidbody (si no, el objeto
    ///     reaparecería con la velocidad de la caída) y lo recoloca.
    ///
    /// CÓMO SE USA
    ///   Ponlo en todos los objetos agarrables con Rigidbody. Puedes llamar a
    ///   Respawn() desde otro script o desde un evento si quieres recolocarlo
    ///   a mano.
    /// </summary>
    public class RespawnOnFall : MonoBehaviour
    {
        [Tooltip("Si el objeto baja de esta altura (en metros), vuelve a su sitio.")]
        [SerializeField] private float minY = -2f;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Rigidbody body;

        private void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
            body = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (transform.position.y < minY)
            {
                Respawn();
            }
        }

        /// <summary>Devuelve el objeto a su posición y rotación iniciales y lo detiene.</summary>
        public void Respawn()
        {
            // No se puede asignar velocidad a un Rigidbody cinemático (Unity avisa),
            // por eso se comprueba antes.
            if (body != null && !body.isKinematic)
            {
#if UNITY_6000_0_OR_NEWER
                // En Unity 6 la propiedad "velocity" se renombró a "linearVelocity".
                body.linearVelocity = Vector3.zero;
#else
                body.velocity = Vector3.zero;
#endif
                body.angularVelocity = Vector3.zero;
            }

            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}
