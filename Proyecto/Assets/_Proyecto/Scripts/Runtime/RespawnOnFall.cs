using UnityEngine;

namespace TallerXRI
{
    
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

       
        public void Respawn()
        {
            
            if (body != null && !body.isKinematic)
            {
#if UNITY_6000_0_OR_NEWER
              
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
