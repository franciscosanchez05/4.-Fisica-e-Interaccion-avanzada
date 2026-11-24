using System;
using UnityEditor;
using UnityEngine;

// =====================================================================
// TallerBuilder (parte 4: objetos interactivos)
//
// Construye lo que se usa en la demo:
//   Grupo_Fisica  : CuboPruebas, Pared, CuboVisual, Pelota (bloques 1 y 2)
//   Grupo_Sockets : herramientas, tablon, panel con sockets (bloques 3, 4 y 5)
//   DemoController: teclas para cambiar el Movement Type y guia en pantalla
//
// Los componentes de XRI (XR Grab Interactable, XR Socket Interactor) se
// anaden por NOMBRE y sus propiedades se asignan por reflexion. Asi el
// constructor compila aunque cambie la version de XRI; si alguna propiedad
// no existe, se avisa en la consola y se sigue.
// =====================================================================
namespace TallerXRI.Editor
{
    public static partial class TallerBuilder
    {
        static readonly string[] ToolKinds = { "Martillo", "Destornillador", "Llave" };
        static Component cuboGrabRef;

        // -----------------------------------------------------------------
        // Convierte un GameObject en "agarrable": Rigidbody + XR Grab Interactable + scripts de apoyo
        // -----------------------------------------------------------------
        static Component MakeGrabbable(GameObject go, float mass, string movementType, float easeIn,
                                       bool throwOnDetach, Kit k, Transform attach = null)
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();

            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
            }

            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Component grab = AddByName(go, "XRGrabInteractable");

            if (grab == null)
            {
                return null;
            }

            SetProp(grab, "movementType", movementType);
            SetProp(grab, "attachEaseInTime", easeIn);
            SetProp(grab, "throwOnDetach", throwOnDetach);

            if (attach != null)
            {
                SetProp(grab, "attachTransform", attach);
            }

            // Scripts de apoyo (se describen en cada archivo .cs)
            Component feedback = AddByName(go, "GrabFeedback");
            SetField(feedback, "hoverClip", k.Hover);
            SetField(feedback, "grabClip", k.Grab);
            SetField(feedback, "dropClip", k.Drop);

            AddByName(go, "RespawnOnFall");

            Component impact = AddByName(go, "ImpactSound");
            SetField(impact, "clips", new AudioClip[] { k.Impact });

            return grab;
        }

        // -----------------------------------------------------------------
        // GRUPO DE FISICA (bloques 1 y 2)
        // -----------------------------------------------------------------
        static void BuildPhysicsGroup(Kit k, bool layersOk)
        {
            Transform g = Empty("Grupo_Fisica", null, Vector3.zero).transform;

            // Pared de pruebas: solo collider (sin Rigidbody) para ver quien la atraviesa.
            Cube("Pared", g, new Vector3(-0.9f, TableTop + 0.2f, 1.1f), new Vector3(0.5f, 0.4f, 0.05f), k.Concreto);

            // ---- CuboPruebas: empieza en Instantaneous (se cambia en directo con Z, X, C)
            GameObject cubo = Cube("CuboPruebas", g, new Vector3(-0.9f, TableTop + 0.075f, 0.75f), Vector3.one * 0.15f, k.CuboNaranja);
            cuboGrabRef = MakeGrabbable(cubo, 1f, "Instantaneous", 0.1f, false, k);
            AddByName(cubo, "InteractionStateLabel");

            // ---- CuboVisual: raiz con fisica + hijo "Visuals" sin collider (Predicted Visuals Transform)
            GameObject visualRoot = new GameObject("CuboVisual");
            visualRoot.transform.SetParent(g, false);
            visualRoot.transform.localPosition = new Vector3(-0.45f, TableTop + 0.075f, 0.75f);
            BoxCollider box = visualRoot.AddComponent<BoxCollider>();
            box.size = Vector3.one * 0.15f;
            GameObject visuals = Part(PrimitiveType.Cube, "Visuals", visualRoot.transform, Vector3.zero, Vector3.one * 0.15f, k.Azul, default(Vector3), Col.None);
            Component visualGrab = MakeGrabbable(visualRoot, 1f, "VelocityTracking", 0.1f, false, k);
            SetProp(visualGrab, "predictedVisualsTransform", visuals.transform);

            // ---- Bandeja y pelota
            Transform tray = Empty("Bandeja", g, new Vector3(0f, TableTop, 0.8f)).transform;
            Cube("Base", tray, new Vector3(0f, 0.005f, 0f), new Vector3(0.22f, 0.01f, 0.22f), k.MetalOscuro);
            Cube("Borde_A", tray, new Vector3(0.105f, 0.02f, 0f), new Vector3(0.01f, 0.03f, 0.22f), k.Metal);
            Cube("Borde_B", tray, new Vector3(-0.105f, 0.02f, 0f), new Vector3(0.01f, 0.03f, 0.22f), k.Metal);
            Cube("Borde_C", tray, new Vector3(0f, 0.02f, 0.105f), new Vector3(0.22f, 0.03f, 0.01f), k.Metal);
            Cube("Borde_D", tray, new Vector3(0f, 0.02f, -0.105f), new Vector3(0.22f, 0.03f, 0.01f), k.Metal);

            GameObject ball = Part(PrimitiveType.Sphere, "Pelota", g, new Vector3(0f, TableTop + 0.065f, 0.8f), Vector3.one * 0.1f, k.PelotaRoja);
            Component ballGrab = MakeGrabbable(ball, 0.2f, "VelocityTracking", 0.05f, true, k);
            SetProp(ballGrab, "throwVelocityScale", 1.5f);

            if (k.Rebote != null)
            {
                SetProp(ball.GetComponent<Collider>(), "sharedMaterial", k.Rebote);
            }

            // Guardamos algunos objetos como prefabs (como pedia el guion del proyecto).
            TrySavePrefab(visualRoot, PrefabDir + "/CuboVisual.prefab");
            TrySavePrefab(ball, PrefabDir + "/Pelota.prefab");
        }

        static void TrySavePrefab(GameObject go, string path)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Taller XRI] No pude guardar el prefab " + path + ": " + e.Message);
            }
        }

        // -----------------------------------------------------------------
        // GRUPO DE SOCKETS (bloques 3, 4 y 5)
        // -----------------------------------------------------------------
        static void BuildSocketsGroup(Kit k, bool layersOk, bool configured, bool finalScene, bool withSockets)
        {
            Transform g = Empty("Grupo_Sockets", null, Vector3.zero).transform;

            // Herramientas sobre la mesa (la cabeza apunta hacia +Z, lejos del jugador)
            BuildTool("Martillo", g, new Vector3(0.45f, 0.935f, 0.75f), k, configured, layersOk, finalScene);
            BuildTool("Destornillador", g, new Vector3(0.75f, 0.93f, 0.75f), k, configured, layersOk, finalScene);
            BuildTool("Llave", g, new Vector3(1.05f, 0.92f, 0.75f), k, configured, layersOk, finalScene);

            if (finalScene)
            {
                BuildTablon(g, k);
            }

            BuildPanel(g, k, layersOk, withSockets);
        }

        /// <summary>
        /// Construye una herramienta con primitivas. CONVENIO: el mango va a lo largo del eje Z local
        /// y la cabeza hacia +Z. El hijo "Attach" marca el punto por donde se sujeta.
        /// </summary>
        static GameObject BuildTool(string kind, Transform parent, Vector3 worldPos, Kit k, bool configured, bool layersOk, bool savePrefab)
        {
            GameObject root = new GameObject("Tool_" + kind);
            root.transform.SetParent(parent, false);
            root.transform.position = worldPos;

            Transform model = Empty("Modelo", root.transform, Vector3.zero).transform;
            Vector3 attachPos;
            float mass;

            if (kind == "Martillo")
            {
                Part(PrimitiveType.Cylinder, "Mango", model, new Vector3(0f, 0f, -0.03f), new Vector3(0.03f, 0.14f, 0.03f), k.MangoMadera, new Vector3(90f, 0f, 0f));
                Part(PrimitiveType.Cube, "Cabeza", model, new Vector3(0f, 0f, 0.125f), new Vector3(0.11f, 0.04f, 0.04f), k.MetalOscuro);
                Part(PrimitiveType.Cube, "Garra", model, new Vector3(0.07f, 0f, 0.125f), new Vector3(0.03f, 0.035f, 0.05f), k.Metal);
                attachPos = new Vector3(0f, 0f, -0.07f);
                mass = 0.8f;
            }
            else if (kind == "Destornillador")
            {
                Part(PrimitiveType.Cylinder, "Mango", model, new Vector3(0f, 0f, -0.06f), new Vector3(0.035f, 0.045f, 0.035f), k.MangoAzul, new Vector3(90f, 0f, 0f));
                Part(PrimitiveType.Cylinder, "Vastago", model, new Vector3(0f, 0f, 0.055f), new Vector3(0.008f, 0.07f, 0.008f), k.Plata, new Vector3(90f, 0f, 0f));
                Part(PrimitiveType.Cube, "Punta", model, new Vector3(0f, 0f, 0.128f), new Vector3(0.012f, 0.003f, 0.012f), k.Plata);
                attachPos = new Vector3(0f, 0f, -0.06f);
                mass = 0.25f;
            }
            else
            {
                Part(PrimitiveType.Cube, "Mango", model, new Vector3(0f, 0f, -0.03f), new Vector3(0.03f, 0.012f, 0.2f), k.Metal);
                Part(PrimitiveType.Cube, "Marca", model, new Vector3(0f, 0.0001f, -0.08f), new Vector3(0.032f, 0.0125f, 0.03f), k.Amarillo, default(Vector3), Col.None);
                GameObject head = Part(PrimitiveType.Cylinder, "Cabeza", model, new Vector3(0f, 0f, 0.1f), new Vector3(0.05f, 0.006f, 0.05f), k.Metal);

                // El collider de un cilindro tan plano seria una capsula gruesa: lo cambiamos por una caja.
                UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
                BoxCollider headBox = head.AddComponent<BoxCollider>();
                headBox.size = new Vector3(1f, 2f, 1f);

                Part(PrimitiveType.Cube, "Mordaza_A", model, new Vector3(-0.027f, 0f, 0.135f), new Vector3(0.014f, 0.012f, 0.04f), k.Metal, default(Vector3), Col.None);
                Part(PrimitiveType.Cube, "Mordaza_B", model, new Vector3(0.027f, 0f, 0.135f), new Vector3(0.014f, 0.012f, 0.04f), k.Metal, default(Vector3), Col.None);
                attachPos = new Vector3(0f, 0f, -0.04f);
                mass = 0.5f;
            }

            if (!configured)
            {
                // Version "sin configurar" (Taller_Inicial): solo el modelo con sus colliders.
                return root;
            }

            Transform attach = Empty("Attach", root.transform, attachPos).transform;
            Component grab = MakeGrabbable(root, mass, "VelocityTracking", 0.15f, true, k, attach);

            if (layersOk)
            {
                SetProp(grab, "interactionLayers", LayerMaskOf("Default", kind));
            }

            if (kind == "Martillo")
            {
                AddByName(root, "InteractionStateLabel");
            }

            if (savePrefab)
            {
                TrySavePrefab(root, PrefabDir + "/Tool_" + kind + ".prefab");
            }

            return root;
        }

        /// <summary>Tablon largo sobre dos caballetes: se agarra con dos manos (Select Mode = Multiple).</summary>
        static void BuildTablon(Transform g, Kit k)
        {
            Transform rack = Empty("Caballetes", g, Vector3.zero).transform;
            float[] zs = { 0.5f, 1.5f };

            foreach (float z in zs)
            {
                Cube("Viga", rack, new Vector3(-1.9f, 0.63f, z), new Vector3(0.45f, 0.06f, 0.08f), k.MaderaOscura);
                Cube("Pata_Izq", rack, new Vector3(-2.1f, 0.31f, z), new Vector3(0.05f, 0.62f, 0.05f), k.MaderaOscura, new Vector3(0f, 0f, -14f));
                Cube("Pata_Der", rack, new Vector3(-1.7f, 0.31f, z), new Vector3(0.05f, 0.62f, 0.05f), k.MaderaOscura, new Vector3(0f, 0f, 14f));
            }

            GameObject board = Cube("Tablon", g, new Vector3(-1.9f, 0.70f, 1.0f), new Vector3(1.2f, 0.08f, 0.15f), k.MaderaClara, new Vector3(0f, 90f, 0f));
            Component grab = MakeGrabbable(board, 1.5f, "VelocityTracking", 0.1f, false, k);
            SetProp(grab, "selectMode", "Multiple");
            TrySavePrefab(board, PrefabDir + "/Tablon.prefab");
        }

        // -----------------------------------------------------------------
        // PANEL DE HERRAMIENTAS (pared derecha) con 3 sockets
        // -----------------------------------------------------------------
        static void BuildPanel(Transform g, Kit k, bool layersOk, bool withSockets)
        {
            Transform p = Empty("PanelHerramientas", g, Vector3.zero).transform;

            // Tablero perforado + marco de madera
            Cube("Tablero", p, new Vector3(2.58f, 1.55f, 1.0f), new Vector3(0.04f, 0.95f, 1.7f), k.Pegboard);
            Cube("Marco_Sup", p, new Vector3(2.575f, 2.045f, 1.0f), new Vector3(0.06f, 0.05f, 1.8f), k.MaderaOscura, default(Vector3), Col.None);
            Cube("Marco_Inf", p, new Vector3(2.575f, 1.055f, 1.0f), new Vector3(0.06f, 0.05f, 1.8f), k.MaderaOscura, default(Vector3), Col.None);
            Cube("Marco_Izq", p, new Vector3(2.575f, 1.55f, 0.125f), new Vector3(0.06f, 1.0f, 0.05f), k.MaderaOscura, default(Vector3), Col.None);
            Cube("Marco_Der", p, new Vector3(2.575f, 1.55f, 1.875f), new Vector3(0.06f, 1.0f, 0.05f), k.MaderaOscura, default(Vector3), Col.None);

            float y = 1.55f;
            float[] zs = { 0.55f, 1.0f, 1.45f };

            // Siluetas pintadas en el panel (decoracion): indican donde va cada herramienta.
            // Martillo
            Cube("Silueta_Martillo_Mango", p, new Vector3(2.5575f, y + 0.04f, zs[0]), new Vector3(0.005f, 0.28f, 0.03f), k.Silueta, default(Vector3), Col.None);
            Cube("Silueta_Martillo_Cabeza", p, new Vector3(2.5575f, y + 0.195f, zs[0]), new Vector3(0.005f, 0.04f, 0.11f), k.Silueta, default(Vector3), Col.None);
            // Destornillador
            Cube("Silueta_Destornillador_Mango", p, new Vector3(2.5575f, y, zs[1]), new Vector3(0.005f, 0.09f, 0.035f), k.Silueta, default(Vector3), Col.None);
            Cube("Silueta_Destornillador_Vastago", p, new Vector3(2.5575f, y + 0.115f, zs[1]), new Vector3(0.005f, 0.14f, 0.01f), k.Silueta, default(Vector3), Col.None);
            // Llave
            Cube("Silueta_Llave_Mango", p, new Vector3(2.5575f, y + 0.01f, zs[2]), new Vector3(0.005f, 0.2f, 0.03f), k.Silueta, default(Vector3), Col.None);
            Cube("Silueta_Llave_Cabeza", p, new Vector3(2.5575f, y + 0.14f, zs[2]), new Vector3(0.005f, 0.07f, 0.07f), k.Silueta, default(Vector3), Col.None);

            if (!withSockets)
            {
                return;
            }

            // ---- Sockets, indicadores y gestor
            Component[] socketComponents = new Component[3];

            for (int i = 0; i < ToolKinds.Length; i++)
            {
                string kind = ToolKinds[i];
                Vector3 pos = new Vector3(2.50f, y, zs[i]);

                GameObject socketGo = new GameObject("Socket_" + kind);
                socketGo.transform.SetParent(p, false);
                socketGo.transform.position = pos;

                // El socket necesita un Collider "Is Trigger": es su zona de deteccion.
                SphereCollider zone = socketGo.AddComponent<SphereCollider>();
                zone.isTrigger = true;
                zone.radius = 0.12f;

                // Attach del socket: la herramienta se coloca con la cabeza hacia ARRIBA y de cara a la sala.
                Transform attach = Empty("Attach", socketGo.transform, Vector3.zero).transform;
                attach.rotation = Quaternion.LookRotation(Vector3.up, Vector3.left);

                Component socket = AddByName(socketGo, "XRSocketInteractor");
                SetProp(socket, "attachTransform", attach);
                SetProp(socket, "showInteractableHoverMeshes", true);
                SetProp(socket, "interactableHoverMeshMaterial", k.HoverVerde);
                SetProp(socket, "interactableCantHoverMeshMaterial", k.NoPermitido);
                SetProp(socket, "hoverSocketSnapping", false);

                if (layersOk)
                {
                    SetProp(socket, "interactionLayers", LayerMaskOf(kind));
                }

                // Indicador rojo / verde
                GameObject indicator = Part(PrimitiveType.Sphere, "Indicador_" + kind, p, new Vector3(2.53f, 1.93f, zs[i]),
                                            Vector3.one * 0.06f, k.Indicador, default(Vector3), Col.None);

                AudioSource audio = socketGo.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 1f;

                Component indicatorScript = AddByName(socketGo, "SocketIndicator");
                SetField(indicatorScript, "indicator", indicator.GetComponent<Renderer>());
                SetField(indicatorScript, "audioSource", audio);
                SetField(indicatorScript, "fillClip", k.Socket);

                socketComponents[i] = socket;
            }

            // Gestor de la caja: enciende una luz cuando los tres sockets estan ocupados.
            GameObject manager = Empty("GestorCaja", p, new Vector3(2.4f, 2.5f, 1.0f));
            GameObject lightObject = new GameObject("LuzCompleta");
            lightObject.transform.SetParent(manager.transform, false);
            Part(PrimitiveType.Sphere, "Bombilla", lightObject.transform, Vector3.zero, Vector3.one * 0.12f,
                 Mat("Mat_LuzCompleta", new Color(0.3f, 1f, 0.4f), 0f, 0.5f, null, 1f, 1f, true, 3f), default(Vector3), Col.None);
            AddLight("Luz", lightObject.transform, Vector3.zero, LightType.Point, new Color(0.3f, 1f, 0.4f), 1.5f, 5f);
            lightObject.SetActive(false);

            AudioSource managerAudio = manager.AddComponent<AudioSource>();
            managerAudio.playOnAwake = false;
            managerAudio.spatialBlend = 1f;

            Component completion = AddByName(manager, "ToolboxCompletion");
            SetField(completion, "sockets", socketComponents);
            SetField(completion, "completeLight", lightObject);
            SetField(completion, "audioSource", managerAudio);
            SetField(completion, "completeClip", k.Complete);
        }

        // -----------------------------------------------------------------
        // DEMO CONTROLLER
        // -----------------------------------------------------------------
        static void BuildDemoController(Kit k)
        {
            GameObject controller = new GameObject("DemoController");
            Component switcher = AddByName(controller, "MovementTypeSwitcher");
            SetField(switcher, "target", cuboGrabRef);
            AddByName(controller, "DemoGuiaHud");
        }

        // -----------------------------------------------------------------
        // REPARAR CAPAS en la escena abierta (menu 2)
        // -----------------------------------------------------------------
        static int ApplyLayersToOpenScene()
        {
            int count = 0;
            Type grabType = FindType("XRGrabInteractable");
            Type socketType = FindType("XRSocketInteractor");

            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

            foreach (Transform tr in all)
            {
                foreach (string kind in ToolKinds)
                {
                    if (tr.name == "Tool_" + kind && grabType != null)
                    {
                        Component grab = tr.GetComponent(grabType);
                        SetProp(grab, "interactionLayers", LayerMaskOf("Default", kind));
                        count++;
                    }
                    else if (tr.name == "Socket_" + kind && socketType != null)
                    {
                        Component socket = tr.GetComponent(socketType);
                        SetProp(socket, "interactionLayers", LayerMaskOf(kind));
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
