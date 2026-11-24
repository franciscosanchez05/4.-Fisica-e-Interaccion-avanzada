using UnityEngine;
using UnityEngine.InputSystem;

namespace TallerXRI
{
    /// <summary>
    /// DemoGuiaHud
    /// =====================================================================
    /// PARA QUÉ SIRVE
    ///   Muestra en la esquina superior derecha un "chuleta" con los 5 bloques
    ///   de la demo y las teclas que usamos. Sirve de apoyo mientras se graba
    ///   la clase: quien narra ve de un vistazo en qué bloque está y qué
    ///   tecla pulsar. Se oculta y se muestra con una tecla (por defecto H)
    ///   para que no salga en el vídeo final si no se quiere.
    ///
    /// CÓMO FUNCIONA
    ///   OnGUI dibuja una caja con texto. Es la forma más simple de pintar
    ///   interfaz en pantalla sin crear un Canvas. La lista de bloques es un
    ///   array de textos que puedes editar en el Inspector.
    ///
    /// CÓMO SE USA
    ///   Ponlo en el GameObject DemoController (o en cualquier objeto).
    /// </summary>
    public class DemoGuiaHud : MonoBehaviour
    {
        [Tooltip("Tecla para mostrar / ocultar la guía.")]
        [SerializeField] private Key toggleKey = Key.H;

        [SerializeField] private bool visibleAtStart = true;

        [Tooltip("Líneas que se muestran en la guía (una por bloque).")]
        [SerializeField] private string[] lines =
        {
            "DEMO (15 min)",
            "1  Escena base y primer agarre (0:00-3:00)",
            "2  Movement Type y lanzamiento (3:00-7:00)",
            "3  Agarres avanzados (7:00-10:00)",
            "4  Sockets (10:00-13:00)",
            "5  Affordances y cierre (13:00-15:00)",
            "",
            "Z X C = Movement Type  |  V = Throw  |  H = ocultar guia"
        };

        private bool visible;
        private GUIStyle textStyle;

        private void Awake()
        {
            visible = visibleAtStart;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                textStyle.normal.textColor = Color.white;
            }

            float width = 470f;
            float height = 14f + lines.Length * 24f;
            Rect box = new Rect(Screen.width - width - 20f, 20f, width, height);

            // GUI.Box dibuja un panel oscuro semitransparente de fondo.
            GUI.Box(box, GUIContent.none);

            for (int i = 0; i < lines.Length; i++)
            {
                GUI.Label(new Rect(box.x + 10f, box.y + 8f + i * 24f, width - 20f, 24f), lines[i], textStyle);
            }
        }
    }
}
