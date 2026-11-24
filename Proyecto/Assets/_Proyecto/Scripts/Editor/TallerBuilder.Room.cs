using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// =====================================================================
// TallerBuilder (parte 3: escena, sala y decoracion)
//
// Construye el taller: paredes, suelo, techo, luces, puerta, ventana,
// banco de trabajo, estanterias, barriles, posters, planta, etc.
// Todo con primitivas de Unity (cubos, cilindros, esferas) y texturas
// generadas por codigo: no usa ningun asset externo.
//
// COORDENADAS (metros): el jugador empieza en (0,0,0) mirando hacia +Z.
//   Sala:      x de -2.6 a 2.6  |  z de -2.0 a 3.6  |  altura 3.0
//   Mesa:      centro (0, 0.88, 1.0)  -> superficie a y = 0.91
//   Panel:     pared derecha (x = 2.6), de cara al jugador
//   Caballete: a la izquierda de la mesa (para el tablon)
// =====================================================================
namespace TallerXRI.Editor
{
    public static partial class TallerBuilder
    {
        const float TableTop = 0.91f;

        static string BuildScene(string sceneName, Variant variant, Kit k, bool layersOk)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Ambiente: luz de relleno suave para que las zonas sin luz directa no queden negras.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.48f);

            // ---- Luz direccional tenue (relleno, sin sombras)
            Light sun = AddLight("Luz_Relleno", null, new Vector3(0f, 3f, 0f), LightType.Directional,
                                 new Color(1f, 0.96f, 0.90f), 0.45f, 10f);
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            // ---- Entorno
            GameObject environment = Empty("Entorno", null, Vector3.zero);
            BuildRoom(environment.transform, k);
            BuildWorkbench(environment.transform, k);
            BuildDecoration(environment.transform, k);

            // ---- Rig de realidad virtual y simulador (samples de XRI)
            BuildRig();

            // ---- Contenido segun la variante
            if (variant == Variant.Final)
            {
                BuildPhysicsGroup(k, layersOk);
                BuildSocketsGroup(k, layersOk, true, true, true);
                BuildDemoController(k);
            }
            else if (variant == Variant.Solucion)
            {
                BuildSocketsGroup(k, layersOk, true, false, true);
            }
            else
            {
                // Taller_Inicial: herramientas SIN configurar y panel sin sockets (es el reto).
                BuildSocketsGroup(k, layersOk, false, false, false);
            }

            string path = SceneDir + "/" + sceneName + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // -----------------------------------------------------------------
        // RIG DE VR + SIMULADOR
        // -----------------------------------------------------------------
        static void BuildRig()
        {
            GameObject rigPrefab = FindPrefab("XR Origin (XR Rig)");

            if (rigPrefab != null)
            {
                GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                rig.transform.position = Vector3.zero;
                rig.transform.rotation = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning("[Taller XRI] No encuentro el prefab 'XR Origin (XR Rig)'. Importa el sample Starter Assets "
                                 + "(Package Manager > XR Interaction Toolkit > Samples) y arrastralo a la escena.");
            }

            GameObject simPrefab = FindPrefab("XR Interaction Simulator");

            if (simPrefab == null)
            {
                simPrefab = FindPrefab("XR Device Simulator");
            }

            if (simPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(simPrefab);
            }
            else
            {
                Debug.LogWarning("[Taller XRI] No encuentro el prefab del simulador. Importa el sample 'XR Interaction Simulator' "
                                 + "y arrastra su prefab a la escena.");
            }
        }

        // -----------------------------------------------------------------
        // SALA: suelo, techo, paredes, rodapies, puerta, ventana, luces
        // -----------------------------------------------------------------
        static void BuildRoom(Transform e, Kit k)
        {
            Transform r = Empty("Sala", e, Vector3.zero).transform;

            Cube("Suelo", r, new Vector3(0f, -0.05f, 0.8f), new Vector3(5.4f, 0.1f, 5.8f), k.Suelo);
            Cube("Techo", r, new Vector3(0f, 3.05f, 0.8f), new Vector3(5.4f, 0.1f, 5.8f), k.Techo);

            Cube("Pared_Trasera_Ladrillo", r, new Vector3(0f, 1.5f, 3.65f), new Vector3(5.4f, 3f, 0.1f), k.Ladrillo);
            Cube("Pared_Izquierda", r, new Vector3(-2.65f, 1.5f, 0.8f), new Vector3(0.1f, 3f, 5.8f), k.Pared);
            Cube("Pared_Derecha", r, new Vector3(2.65f, 1.5f, 0.8f), new Vector3(0.1f, 3f, 5.8f), k.Pared);
            Cube("Pared_Frontal", r, new Vector3(0f, 1.5f, -2.05f), new Vector3(5.4f, 3f, 0.1f), k.Pared);

            // Rodapies
            Cube("Rodapie_Frontal", r, new Vector3(0f, 0.07f, -1.97f), new Vector3(5.2f, 0.14f, 0.04f), k.Rodapie, default(Vector3), Col.None);
            Cube("Rodapie_Trasero", r, new Vector3(0f, 0.07f, 3.57f), new Vector3(5.2f, 0.14f, 0.04f), k.Rodapie, default(Vector3), Col.None);
            Cube("Rodapie_Izquierdo", r, new Vector3(-2.57f, 0.07f, 0.8f), new Vector3(0.04f, 0.14f, 5.6f), k.Rodapie, default(Vector3), Col.None);
            Cube("Rodapie_Derecho", r, new Vector3(2.57f, 0.07f, 0.8f), new Vector3(0.04f, 0.14f, 5.6f), k.Rodapie, default(Vector3), Col.None);

            // Franjas decorativas (zocalo de color en la parte baja de las paredes)
            Cube("Franja_Izquierda", r, new Vector3(-2.58f, 0.6f, 0.8f), new Vector3(0.04f, 0.9f, 5.5f), k.Franja, default(Vector3), Col.None);
            Cube("Franja_Derecha", r, new Vector3(2.58f, 0.6f, 0.8f), new Vector3(0.04f, 0.9f, 5.5f), k.Franja, default(Vector3), Col.None);
            Cube("Franja_Frontal", r, new Vector3(0f, 0.6f, -1.98f), new Vector3(5.3f, 0.9f, 0.04f), k.Franja, default(Vector3), Col.None);

            // Cornisa superior
            Cube("Cornisa_Izquierda", r, new Vector3(-2.58f, 2.94f, 0.8f), new Vector3(0.05f, 0.1f, 5.6f), k.Blanco, default(Vector3), Col.None);
            Cube("Cornisa_Derecha", r, new Vector3(2.58f, 2.94f, 0.8f), new Vector3(0.05f, 0.1f, 5.6f), k.Blanco, default(Vector3), Col.None);
            Cube("Cornisa_Trasera", r, new Vector3(0f, 2.94f, 3.58f), new Vector3(5.2f, 0.1f, 0.05f), k.Blanco, default(Vector3), Col.None);
            Cube("Cornisa_Frontal", r, new Vector3(0f, 2.94f, -1.98f), new Vector3(5.2f, 0.1f, 0.05f), k.Blanco, default(Vector3), Col.None);

            // ---- Puerta (pared frontal, a la derecha; queda detras del jugador)
            Transform door = Empty("Puerta", r, Vector3.zero).transform;
            Cube("Marco_Izq", door, new Vector3(1.08f, 1.05f, -1.97f), new Vector3(0.08f, 2.1f, 0.1f), k.MaderaOscura, default(Vector3), Col.None);
            Cube("Marco_Der", door, new Vector3(2.12f, 1.05f, -1.97f), new Vector3(0.08f, 2.1f, 0.1f), k.MaderaOscura, default(Vector3), Col.None);
            Cube("Marco_Sup", door, new Vector3(1.6f, 2.12f, -1.97f), new Vector3(1.12f, 0.08f, 0.1f), k.MaderaOscura, default(Vector3), Col.None);
            Cube("Hoja", door, new Vector3(1.6f, 1.04f, -1.95f), new Vector3(0.98f, 2.05f, 0.04f), k.Puerta, default(Vector3), Col.None);
            Part(PrimitiveType.Sphere, "Pomo", door, new Vector3(1.99f, 1.0f, -1.90f), new Vector3(0.07f, 0.07f, 0.07f), k.Metal, default(Vector3), Col.None);

            // ---- Ventana (pared izquierda): cristal luminoso + marco + cruceta
            Transform window = Empty("Ventana", r, Vector3.zero).transform;
            Cube("Cristal", window, new Vector3(-2.585f, 1.75f, 1.2f), new Vector3(0.02f, 0.95f, 1.35f), k.Ventana, default(Vector3), Col.None);
            Cube("Marco_Superior", window, new Vector3(-2.57f, 2.27f, 1.2f), new Vector3(0.06f, 0.07f, 1.5f), k.Blanco, default(Vector3), Col.None);
            Cube("Marco_Inferior", window, new Vector3(-2.57f, 1.23f, 1.2f), new Vector3(0.1f, 0.07f, 1.5f), k.Blanco, default(Vector3), Col.None);
            Cube("Marco_Lateral_A", window, new Vector3(-2.57f, 1.75f, 0.47f), new Vector3(0.06f, 1.05f, 0.07f), k.Blanco, default(Vector3), Col.None);
            Cube("Marco_Lateral_B", window, new Vector3(-2.57f, 1.75f, 1.93f), new Vector3(0.06f, 1.05f, 0.07f), k.Blanco, default(Vector3), Col.None);
            Cube("Cruceta_V", window, new Vector3(-2.57f, 1.75f, 1.2f), new Vector3(0.05f, 1.0f, 0.04f), k.Blanco, default(Vector3), Col.None);
            Cube("Cruceta_H", window, new Vector3(-2.57f, 1.75f, 1.2f), new Vector3(0.05f, 0.04f, 1.4f), k.Blanco, default(Vector3), Col.None);
            AddLight("Luz_Ventana", window, new Vector3(-2.2f, 1.8f, 1.2f), LightType.Point, new Color(0.75f, 0.85f, 1f), 0.7f, 4.5f);

            // ---- Lamparas de techo (plafon luminoso + luz puntual)
            Vector3[] lampPositions =
            {
                new Vector3(-1.3f, 2.9f, 0.8f),
                new Vector3(1.3f, 2.9f, 0.8f),
                new Vector3(0f, 2.9f, 2.6f)
            };

            for (int i = 0; i < lampPositions.Length; i++)
            {
                Transform lamp = Empty("Lampara_" + (i + 1), r, lampPositions[i]).transform;
                Cube("Plafon", lamp, Vector3.zero, new Vector3(0.9f, 0.06f, 0.3f), k.Lampara, default(Vector3), Col.None);
                Cube("Soporte", lamp, new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.04f, 0.06f), k.MetalOscuro, default(Vector3), Col.None);
                AddLight("Luz", lamp, new Vector3(0f, -0.25f, 0f), LightType.Point, new Color(1f, 0.93f, 0.82f), 1.1f, 7f);
            }
        }

        // -----------------------------------------------------------------
        // BANCO DE TRABAJO
        // -----------------------------------------------------------------
        static void BuildWorkbench(Transform e, Kit k)
        {
            Transform b = Empty("Mesa", e, Vector3.zero).transform;

            Cube("Tablero", b, new Vector3(0f, 0.88f, 1.0f), new Vector3(2.4f, 0.06f, 0.8f), k.Mesa);

            float[] legX = { -1.12f, 1.12f };
            float[] legZ = { 0.66f, 1.34f };

            foreach (float x in legX)
            {
                foreach (float z in legZ)
                {
                    Cube("Pata", b, new Vector3(x, 0.425f, z), new Vector3(0.08f, 0.85f, 0.08f), k.MaderaOscura);
                }
            }

            Cube("Estante_Inferior", b, new Vector3(0f, 0.25f, 1.0f), new Vector3(2.2f, 0.03f, 0.7f), k.MaderaClara);
            Cube("Travesano_Frontal", b, new Vector3(0f, 0.80f, 0.66f), new Vector3(2.2f, 0.06f, 0.04f), k.MaderaOscura, default(Vector3), Col.None);
            Cube("Travesano_Trasero", b, new Vector3(0f, 0.80f, 1.34f), new Vector3(2.2f, 0.06f, 0.04f), k.MaderaOscura, default(Vector3), Col.None);

            // Cajones
            Cube("Cajon_1", b, new Vector3(0.55f, 0.72f, 0.62f), new Vector3(0.5f, 0.16f, 0.03f), k.MaderaClara, default(Vector3), Col.None);
            Cube("Tirador_1", b, new Vector3(0.55f, 0.72f, 0.595f), new Vector3(0.12f, 0.02f, 0.02f), k.Metal, default(Vector3), Col.None);
            Cube("Cajon_2", b, new Vector3(1.02f, 0.72f, 0.62f), new Vector3(0.38f, 0.16f, 0.03f), k.MaderaClara, default(Vector3), Col.None);
            Cube("Tirador_2", b, new Vector3(1.02f, 0.72f, 0.595f), new Vector3(0.12f, 0.02f, 0.02f), k.Metal, default(Vector3), Col.None);

            // Tornillo de banco (esquina izquierda delantera)
            Cube("Tornillo_Base", b, new Vector3(-1.05f, 0.95f, 0.72f), new Vector3(0.12f, 0.08f, 0.14f), k.MetalOscuro, default(Vector3), Col.None);
            Cube("Tornillo_Mordaza", b, new Vector3(-1.05f, 0.995f, 0.63f), new Vector3(0.12f, 0.07f, 0.03f), k.MetalOscuro, default(Vector3), Col.None);
            Part(PrimitiveType.Cylinder, "Tornillo_Husillo", b, new Vector3(-1.05f, 0.96f, 0.56f), new Vector3(0.02f, 0.05f, 0.02f), k.Plata, new Vector3(90f, 0f, 0f), Col.None);
            Part(PrimitiveType.Cylinder, "Tornillo_Palanca", b, new Vector3(-1.05f, 0.96f, 0.49f), new Vector3(0.015f, 0.06f, 0.015f), k.Plata, new Vector3(0f, 0f, 90f), Col.None);

            // Lampara de mesa (esquina trasera derecha)
            Part(PrimitiveType.Cylinder, "Flexo_Base", b, new Vector3(1.0f, 0.925f, 1.28f), new Vector3(0.14f, 0.012f, 0.14f), k.Negro, default(Vector3), Col.None);
            Part(PrimitiveType.Cylinder, "Flexo_Brazo", b, new Vector3(1.0f, 1.12f, 1.28f), new Vector3(0.012f, 0.2f, 0.012f), k.Negro, default(Vector3), Col.None);
            Part(PrimitiveType.Cylinder, "Flexo_Pantalla", b, new Vector3(1.0f, 1.33f, 1.28f), new Vector3(0.12f, 0.03f, 0.12f), k.Amarillo, default(Vector3), Col.None);
            AddLight("Flexo_Luz", b, new Vector3(1.0f, 1.25f, 1.28f), LightType.Point, new Color(1f, 0.9f, 0.7f), 0.5f, 2.5f);
        }

        // -----------------------------------------------------------------
        // DECORACION: estanteria, barriles, cajas, posters, reloj, planta...
        // -----------------------------------------------------------------
        static void BuildDecoration(Transform e, Kit k)
        {
            Transform d = Empty("Decoracion", e, Vector3.zero).transform;

            // ---- Alfombra bajo el jugador
            Cube("Alfombra", d, new Vector3(0f, 0.006f, 0.1f), new Vector3(3.4f, 0.012f, 2.2f), k.Alfombra, default(Vector3), Col.None);

            // ---- Estanteria en la pared trasera (izquierda)
            Transform shelf = Empty("Estanteria", d, Vector3.zero).transform;
            Cube("Montante_A", shelf, new Vector3(-2.4f, 0.98f, 3.4f), new Vector3(0.05f, 1.96f, 0.35f), k.MaderaOscura);
            Cube("Montante_B", shelf, new Vector3(-0.6f, 0.98f, 3.4f), new Vector3(0.05f, 1.96f, 0.35f), k.MaderaOscura);
            float[] shelfY = { 0.5f, 1.0f, 1.5f, 1.95f };

            foreach (float y in shelfY)
            {
                Cube("Balda", shelf, new Vector3(-1.5f, y, 3.4f), new Vector3(1.8f, 0.04f, 0.35f), k.MaderaClara);
            }

            // Objetos sobre las baldas
            Cube("Caja_1", shelf, new Vector3(-2.1f, 0.67f, 3.4f), new Vector3(0.3f, 0.3f, 0.28f), k.MaderaOscura);
            Cube("Caja_2", shelf, new Vector3(-1.6f, 0.65f, 3.4f), new Vector3(0.25f, 0.26f, 0.25f), k.Naranja);
            Cube("Caja_3", shelf, new Vector3(-1.1f, 0.68f, 3.4f), new Vector3(0.32f, 0.32f, 0.28f), k.Azul);
            Part(PrimitiveType.Cylinder, "Bote_Pintura_1", shelf, new Vector3(-2.0f, 1.1f, 3.4f), new Vector3(0.16f, 0.07f, 0.16f), k.Rojo);
            Part(PrimitiveType.Cylinder, "Bote_Pintura_2", shelf, new Vector3(-1.75f, 1.1f, 3.4f), new Vector3(0.16f, 0.07f, 0.16f), k.Verde);
            Part(PrimitiveType.Cylinder, "Bote_Pintura_3", shelf, new Vector3(-1.5f, 1.1f, 3.4f), new Vector3(0.16f, 0.07f, 0.16f), k.Amarillo);
            Cube("Caja_4", shelf, new Vector3(-1.0f, 1.17f, 3.4f), new Vector3(0.4f, 0.28f, 0.28f), k.MaderaOscura);
            Part(PrimitiveType.Cylinder, "Maceta_Estante", shelf, new Vector3(-2.0f, 1.62f, 3.4f), new Vector3(0.18f, 0.07f, 0.18f), k.Maceta);
            Part(PrimitiveType.Sphere, "Planta_Estante", shelf, new Vector3(-2.0f, 1.78f, 3.4f), new Vector3(0.24f, 0.24f, 0.24f), k.Hojas);
            Cube("Caja_5", shelf, new Vector3(-1.3f, 1.65f, 3.4f), new Vector3(0.45f, 0.2f, 0.28f), k.Rojo);

            // ---- Cartel TALLER VR (texto 3D) y reloj en la pared trasera
            BuildSign(d, k);
            Part(PrimitiveType.Cylinder, "Reloj", d, new Vector3(1.4f, 2.4f, 3.57f), new Vector3(0.45f, 0.02f, 0.45f), k.Blanco, new Vector3(90f, 0f, 0f), Col.None);
            Cube("Reloj_Aguja_Minutos", d, new Vector3(1.4f, 2.45f, 3.54f), new Vector3(0.015f, 0.16f, 0.005f), k.Negro, default(Vector3), Col.None);
            Cube("Reloj_Aguja_Horas", d, new Vector3(1.43f, 2.40f, 3.54f), new Vector3(0.02f, 0.11f, 0.005f), k.Negro, new Vector3(0f, 0f, 70f), Col.None);

            // ---- Barriles y cajas de madera (rincon trasero derecho)
            BuildBarrel(d, k, "Barril_Azul", new Vector3(1.9f, 0.45f, 3.15f), k.Azul);
            BuildBarrel(d, k, "Barril_Naranja", new Vector3(2.3f, 0.45f, 2.8f), k.Naranja);
            Cube("Cajon_Madera_1", d, new Vector3(1.2f, 0.25f, 3.3f), new Vector3(0.5f, 0.5f, 0.5f), k.MaderaClara);
            Cube("Cajon_Madera_2", d, new Vector3(1.2f, 0.75f, 3.3f), new Vector3(0.5f, 0.5f, 0.5f), k.MaderaOscura, new Vector3(0f, 12f, 0f));
            Cube("Cajon_Madera_3", d, new Vector3(0.65f, 0.25f, 3.3f), new Vector3(0.5f, 0.5f, 0.5f), k.MaderaOscura);

            // ---- Taburete junto a la mesa
            Transform stool = Empty("Taburete", d, new Vector3(0.9f, 0f, -0.1f)).transform;
            Part(PrimitiveType.Cylinder, "Asiento", stool, new Vector3(0f, 0.46f, 0f), new Vector3(0.32f, 0.03f, 0.32f), k.Rojo);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Part(PrimitiveType.Cylinder, "Pata", stool, new Vector3(Mathf.Cos(angle) * 0.11f, 0.22f, Mathf.Sin(angle) * 0.11f),
                     new Vector3(0.03f, 0.22f, 0.03f), k.MetalOscuro);
            }

            // ---- Caja de herramientas roja en el suelo
            Transform tb = Empty("Caja_Herramientas_Decor", d, new Vector3(1.75f, 0f, 0.55f)).transform;
            Cube("Cuerpo", tb, new Vector3(0f, 0.14f, 0f), new Vector3(0.5f, 0.28f, 0.24f), k.Rojo);
            Cube("Tapa", tb, new Vector3(0f, 0.295f, 0f), new Vector3(0.52f, 0.03f, 0.26f), k.MetalOscuro);
            Part(PrimitiveType.Cylinder, "Asa", tb, new Vector3(0f, 0.34f, 0f), new Vector3(0.02f, 0.12f, 0.02f), k.Plata, new Vector3(0f, 0f, 90f), Col.None);

            // ---- Planta en la esquina delantera izquierda
            Transform plant = Empty("Planta", d, new Vector3(-2.2f, 0f, -1.5f)).transform;
            Part(PrimitiveType.Cylinder, "Maceta", plant, new Vector3(0f, 0.16f, 0f), new Vector3(0.34f, 0.16f, 0.34f), k.Maceta);
            Part(PrimitiveType.Cylinder, "Tallo", plant, new Vector3(0f, 0.55f, 0f), new Vector3(0.04f, 0.3f, 0.04f), k.MaderaOscura, default(Vector3), Col.None);
            Part(PrimitiveType.Sphere, "Hojas_1", plant, new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 0.45f, 0.5f), k.Hojas, default(Vector3), Col.None);
            Part(PrimitiveType.Sphere, "Hojas_2", plant, new Vector3(0.14f, 0.78f, 0.08f), new Vector3(0.32f, 0.3f, 0.32f), k.Hojas, default(Vector3), Col.None);
            Part(PrimitiveType.Sphere, "Hojas_3", plant, new Vector3(-0.14f, 0.82f, -0.06f), new Vector3(0.3f, 0.28f, 0.3f), k.Hojas, default(Vector3), Col.None);

            // ---- Extintor junto a la puerta
            Transform ext = Empty("Extintor", d, new Vector3(2.3f, 0f, -1.85f)).transform;
            Part(PrimitiveType.Cylinder, "Deposito", ext, new Vector3(0f, 0.38f, 0f), new Vector3(0.16f, 0.22f, 0.16f), k.Rojo, default(Vector3), Col.None);
            Part(PrimitiveType.Cylinder, "Valvula", ext, new Vector3(0f, 0.63f, 0f), new Vector3(0.06f, 0.03f, 0.06f), k.Negro, default(Vector3), Col.None);
            Cube("Soporte", ext, new Vector3(0f, 0.38f, -0.1f), new Vector3(0.18f, 0.06f, 0.02f), k.MetalOscuro, default(Vector3), Col.None);

            // ---- Posters en la pared izquierda (de cara al interior: normal hacia +X => rotacion Y = -90)
            BuildPoster(d, k.PosterPlano, "Poster_Plano", new Vector3(-2.585f, 1.7f, -0.8f), new Vector3(0f, -90f, 0f), 0.7f, 0.9f, k);
            BuildPoster(d, k.PosterAtardecer, "Poster_Atardecer", new Vector3(-2.585f, 1.7f, -1.55f), new Vector3(0f, -90f, 0f), 0.7f, 0.9f, k);

            // ---- Senales de peligro: cartel en la pared trasera y banda en el suelo junto al panel
            Quad("Senal_Peligro", d, new Vector3(-0.05f, 1.55f, 3.595f), Vector3.zero, 0.5f, 0.5f, k.Peligro);
            Quad("Banda_Peligro_Suelo", d, new Vector3(2.15f, 0.015f, 1.0f), new Vector3(90f, 0f, 0f), 0.4f, 1.9f, k.Peligro);
        }

        static void BuildBarrel(Transform parent, Kit k, string name, Vector3 pos, Material mat)
        {
            Transform barrel = Empty(name, parent, pos).transform;
            Part(PrimitiveType.Cylinder, "Cuerpo", barrel, Vector3.zero, new Vector3(0.5f, 0.45f, 0.5f), mat);
            Part(PrimitiveType.Cylinder, "Aro_Superior", barrel, new Vector3(0f, 0.3f, 0f), new Vector3(0.52f, 0.015f, 0.52f), k.MetalOscuro, default(Vector3), Col.None);
            Part(PrimitiveType.Cylinder, "Aro_Inferior", barrel, new Vector3(0f, -0.3f, 0f), new Vector3(0.52f, 0.015f, 0.52f), k.MetalOscuro, default(Vector3), Col.None);
        }

        static void BuildPoster(Transform parent, Material mat, string name, Vector3 pos, Vector3 euler, float w, float h, Kit k)
        {
            Transform p = Empty(name, parent, Vector3.zero).transform;

            // Marco: un cubo ligeramente mas grande, un poco hacia dentro de la pared.
            Cube("Marco", p, pos + new Vector3(-0.012f, 0f, 0f), new Vector3(0.02f, h + 0.08f, w + 0.08f), k.Negro, default(Vector3), Col.None);
            Quad("Lamina", p, pos, euler, w, h, mat);
        }

        /// <summary>Cartel "TALLER VR" con TextMesh. Si la fuente integrada no existe, se omite el texto.</summary>
        static void BuildSign(Transform parent, Kit k)
        {
            Cube("Cartel_Fondo", parent, new Vector3(-1.5f, 2.55f, 3.57f), new Vector3(1.9f, 0.4f, 0.05f), k.Negro, default(Vector3), Col.None);

            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                if (font == null)
                {
                    return;
                }

                GameObject text = new GameObject("Cartel_Texto");
                text.transform.SetParent(parent, false);
                text.transform.localPosition = new Vector3(-1.5f, 2.55f, 3.53f);

                TextMesh mesh = text.AddComponent<TextMesh>();
                mesh.text = "TALLER VR";
                mesh.font = font;
                mesh.fontSize = 100;
                mesh.characterSize = 0.045f;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.color = new Color(1f, 0.85f, 0.2f);

                text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Taller XRI] No pude crear el texto del cartel: " + ex.Message);
            }
        }
    }
}
