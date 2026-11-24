using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// =====================================================================
// TallerBuilder (parte 2: menu y flujo principal)
//
// Anade el menu "Taller XRI" al editor de Unity. Con UN CLIC construye:
//   - materiales, texturas y sonidos (generados por codigo),
//   - las capas de interaccion (Martillo, Destornillador, Llave),
//   - tres escenas completas con decoracion:
//        Taller_Final    -> la demo entera (bloques 1 a 5)
//        Taller_Inicial  -> punto de partida del RETO
//        Reto_Solucion   -> solucion del reto
// Es codigo de EDITOR (no forma parte del juego).
// =====================================================================
namespace TallerXRI.Editor
{
    public static partial class TallerBuilder
    {
        const string PendingKey = "TallerXRI.PendingBuild";

        enum Variant { Final, Inicial, Solucion }

        /// <summary>Recursos generados que se reutilizan en las tres escenas.</summary>
        sealed class Kit
        {
            public Material Suelo, Techo, Ladrillo, Pared, Rodapie, Franja, Mesa, MaderaOscura, MaderaClara;
            public Material Metal, MetalOscuro, Plata, Rojo, Azul, Naranja, Amarillo, Verde, Negro, Blanco;
            public Material Puerta, Pegboard, Peligro, PosterPlano, PosterAtardecer, Lampara, Ventana, Alfombra;
            public Material Hojas, Maceta, Concreto, CuboNaranja, PelotaRoja, MangoMadera, MangoAzul;
            public Material HoverVerde, NoPermitido, Indicador, Silueta;
            public AudioClip Hover, Grab, Drop, Impact, Socket, Complete;
            public UnityEngine.Object Rebote;
        }

        // -----------------------------------------------------------------
        // MENU
        // -----------------------------------------------------------------
        [MenuItem("Taller XRI/1) Construir proyecto completo")]
        public static void BuildAllMenu()
        {
            bool ok = EditorUtility.DisplayDialog("Taller XRI",
                "Voy a generar materiales, texturas, sonidos y tres escenas (Taller_Final, Taller_Inicial y Reto_Solucion) "
                + "con toda la decoracion.\n\nSi faltan los samples de XRI (Starter Assets y XR Interaction Simulator) "
                + "los importo primero y continuo solo.\n\nPuede tardar uno o dos minutos.",
                "Construir", "Cancelar");

            if (!ok)
            {
                return;
            }

            if (!EnsureSamples())
            {
                // Importar samples recompila scripts y recarga el dominio: el proceso se
                // reanuda solo en ResumeAfterReload() cuando termine.
                SessionState.SetBool(PendingKey, true);
                Debug.Log("[Taller XRI] Importando samples de XRI. El proceso continuara solo cuando termine la compilacion.");
                return;
            }

            BuildAll();
        }

        [MenuItem("Taller XRI/2) Reparar capas de interaccion en la escena abierta")]
        public static void RepairLayersMenu()
        {
            bool ok = EnsureInteractionLayers(new[] { "Martillo", "Destornillador", "Llave" });

            if (!ok)
            {
                EditorUtility.DisplayDialog("Taller XRI",
                    "No he podido crear las capas automaticamente.\n\nCreadlas a mano en:\n"
                    + "Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit > Interaction Layers\n"
                    + "con los nombres: Martillo, Destornillador, Llave. Despues volved a pulsar este menu.", "OK");
                return;
            }

            int applied = ApplyLayersToOpenScene();
            EditorSceneManager.MarkAllScenesDirty();
            EditorUtility.DisplayDialog("Taller XRI", "Capas aplicadas a " + applied + " objetos. Guardad la escena (Ctrl+S).", "OK");
        }

        [MenuItem("Taller XRI/3) Abrir Taller_Final")]
        public static void OpenFinalMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(SceneDir + "/Taller_Final.unity");
            }
        }

        [InitializeOnLoadMethod]
        static void ResumeAfterReload()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                return;
            }

            SessionState.SetBool(PendingKey, false);

            // delayCall espera a que el editor termine de cargarse antes de ejecutar.
            EditorApplication.delayCall += delegate
            {
                AssetDatabase.Refresh();
                BuildAll();
            };
        }

        // -----------------------------------------------------------------
        // SAMPLES DE XRI (Starter Assets y XR Interaction Simulator)
        // -----------------------------------------------------------------

        /// <summary>Devuelve true si ya podemos construir; false si se ha lanzado una importacion.</summary>
        static bool EnsureSamples()
        {
            if (FindPrefab("XR Origin (XR Rig)") != null)
            {
                return true;
            }

            bool requested = false;

            try
            {
                IEnumerable<UnityEditor.PackageManager.UI.Sample> samples =
                    UnityEditor.PackageManager.UI.Sample.FindByPackage("com.unity.xr.interaction.toolkit", null);

                foreach (UnityEditor.PackageManager.UI.Sample sample in samples)
                {
                    bool wanted = sample.displayName == "Starter Assets"
                                  || sample.displayName == "XR Interaction Simulator"
                                  || sample.displayName == "XR Device Simulator";

                    if (wanted && !sample.isImported)
                    {
                        sample.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
                        requested = true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Taller XRI] No pude importar los samples automaticamente: " + e.Message
                                 + "\nImportalos a mano: Window > Package Manager > XR Interaction Toolkit > Samples > Import "
                                 + "(Starter Assets y XR Interaction Simulator) y vuelve a pulsar el menu.");
            }

            return !requested;
        }

        // -----------------------------------------------------------------
        // FLUJO PRINCIPAL
        // -----------------------------------------------------------------
        static void BuildAll()
        {
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                EnsureFolders();

                EditorUtility.DisplayProgressBar("Taller XRI", "Generando texturas, materiales y sonidos...", 0.1f);
                Kit kit = GenerateKit();

                EditorUtility.DisplayProgressBar("Taller XRI", "Creando capas de interaccion...", 0.3f);
                bool layersOk = EnsureInteractionLayers(new[] { "Martillo", "Destornillador", "Llave" });

                if (!layersOk)
                {
                    Debug.LogWarning("[Taller XRI] No se pudieron crear las capas de interaccion automaticamente. "
                                     + "Crealas a mano (Project Settings > XR Plug-in Management > XR Interaction Toolkit > "
                                     + "Interaction Layers: Martillo, Destornillador, Llave) y usa el menu "
                                     + "'Taller XRI > 2) Reparar capas de interaccion'.");
                }

                List<string> scenes = new List<string>();

                EditorUtility.DisplayProgressBar("Taller XRI", "Construyendo Taller_Final...", 0.45f);
                scenes.Add(BuildScene("Taller_Final", Variant.Final, kit, layersOk));

                EditorUtility.DisplayProgressBar("Taller XRI", "Construyendo Taller_Inicial...", 0.70f);
                scenes.Add(BuildScene("Taller_Inicial", Variant.Inicial, kit, layersOk));

                EditorUtility.DisplayProgressBar("Taller XRI", "Construyendo Reto_Solucion...", 0.85f);
                scenes.Add(BuildScene("Reto_Solucion", Variant.Solucion, kit, layersOk));

                List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
                foreach (string path in scenes)
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }

                EditorBuildSettings.scenes = buildScenes.ToArray();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(scenes[0]);

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Taller XRI",
                    "Listo. Se ha abierto Taller_Final.\n\nPulsa Play y prueba con el XR Interaction Simulator.\n"
                    + "Teclas: Z, X, C = Movement Type | V = Throw On Detach | H = ocultar la guia.\n\n"
                    + (layersOk ? "Capas de interaccion: OK." : "ATENCION: las capas de interaccion hay que crearlas a mano (ver la consola)."),
                    "OK");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[Taller XRI] Error construyendo el proyecto: " + e);
                EditorUtility.DisplayDialog("Taller XRI", "Ha habido un error: " + e.Message + "\n\nMira la consola para mas detalles.", "OK");
            }
        }

        // -----------------------------------------------------------------
        // RECURSOS: materiales, texturas, sonidos
        // -----------------------------------------------------------------
        static Kit GenerateKit()
        {
            Kit k = new Kit();

            Texture2D floor = TexFloor();
            Texture2D brick = TexBrick();
            Texture2D wood = TexWood();
            Texture2D peg = TexPegboard();
            Texture2D hazard = TexHazard();
            Texture2D blueprint = TexBlueprint();
            Texture2D sunset = TexSunset();
            Texture2D rug = TexRug();

            k.Suelo = Mat("Mat_Suelo", Color.white, 0f, 0.35f, floor, 4.5f, 4.8f);
            k.Techo = Mat("Mat_Techo", new Color(0.88f, 0.88f, 0.86f));
            k.Ladrillo = Mat("Mat_Ladrillo", Color.white, 0f, 0.1f, brick, 6f, 5f);
            k.Pared = Mat("Mat_Pared", new Color(0.72f, 0.80f, 0.78f));
            k.Rodapie = Mat("Mat_Rodapie", new Color(0.20f, 0.22f, 0.24f));
            k.Franja = Mat("Mat_Franja", new Color(0.18f, 0.40f, 0.42f));
            k.Mesa = Mat("Mat_Mesa", Color.white, 0f, 0.3f, wood, 2f, 1f);
            k.MaderaOscura = Mat("Mat_MaderaOscura", new Color(0.33f, 0.21f, 0.12f));
            k.MaderaClara = Mat("Mat_MaderaClara", new Color(0.72f, 0.55f, 0.35f));
            k.Metal = Mat("Mat_Metal", new Color(0.55f, 0.56f, 0.58f), 0.8f, 0.55f);
            k.MetalOscuro = Mat("Mat_MetalOscuro", new Color(0.22f, 0.23f, 0.25f), 0.8f, 0.45f);
            k.Plata = Mat("Mat_Plata", new Color(0.80f, 0.81f, 0.84f), 0.9f, 0.65f);
            k.Rojo = Mat("Mat_Rojo", new Color(0.78f, 0.12f, 0.10f), 0.1f, 0.5f);
            k.Azul = Mat("Mat_Azul", new Color(0.15f, 0.32f, 0.70f), 0.1f, 0.45f);
            k.Naranja = Mat("Mat_Naranja", new Color(0.92f, 0.45f, 0.10f), 0.1f, 0.4f);
            k.Amarillo = Mat("Mat_Amarillo", new Color(0.95f, 0.78f, 0.12f), 0.1f, 0.4f);
            k.Verde = Mat("Mat_Verde", new Color(0.18f, 0.55f, 0.25f));
            k.Negro = Mat("Mat_Negro", new Color(0.06f, 0.06f, 0.07f));
            k.Blanco = Mat("Mat_Blanco", new Color(0.92f, 0.92f, 0.90f));
            k.Puerta = Mat("Mat_Puerta", Color.white, 0f, 0.3f, wood, 1f, 2f);
            k.Pegboard = Mat("Mat_Pegboard", Color.white, 0f, 0.2f, peg, 6f, 3.4f);
            k.Peligro = Mat("Mat_Peligro", Color.white, 0f, 0.3f, hazard, 1f, 1f);
            k.PosterPlano = Mat("Mat_PosterPlano", Color.white, 0f, 0.3f, blueprint);
            k.PosterAtardecer = Mat("Mat_PosterAtardecer", Color.white, 0f, 0.3f, sunset);
            k.Lampara = Mat("Mat_Lampara", new Color(1f, 0.95f, 0.82f), 0f, 0.5f, null, 1f, 1f, true, 2.0f);
            k.Ventana = Mat("Mat_Ventana", new Color(0.65f, 0.82f, 1f), 0f, 0.8f, null, 1f, 1f, true, 1.2f);
            k.Alfombra = Mat("Mat_Alfombra", Color.white, 0f, 0.1f, rug, 1f, 1f);
            k.Hojas = Mat("Mat_Hojas", new Color(0.20f, 0.60f, 0.25f));
            k.Maceta = Mat("Mat_Maceta", new Color(0.70f, 0.38f, 0.25f));
            k.Concreto = Mat("Mat_Concreto", new Color(0.55f, 0.55f, 0.56f));
            k.CuboNaranja = Mat("Mat_CuboPruebas", new Color(0.95f, 0.50f, 0.12f), 0f, 0.5f);
            k.PelotaRoja = Mat("Mat_Pelota", new Color(0.85f, 0.12f, 0.12f), 0f, 0.7f);
            k.MangoMadera = Mat("Mat_MangoMadera", new Color(0.55f, 0.34f, 0.18f));
            k.MangoAzul = Mat("Mat_MangoAzul", new Color(0.12f, 0.35f, 0.85f), 0f, 0.5f);
            k.HoverVerde = Mat("Mat_HoverVerde", new Color(0.2f, 1f, 0.35f, 0.35f), 0f, 0.5f, null, 1f, 1f, false, 1f, true);
            k.NoPermitido = Mat("Mat_NoPermitido", new Color(1f, 0.2f, 0.2f, 0.35f), 0f, 0.5f, null, 1f, 1f, false, 1f, true);
            k.Indicador = Mat("Mat_Indicador", Color.white, 0f, 0.6f);
            k.Silueta = Mat("Mat_Silueta", new Color(0.30f, 0.23f, 0.14f));

            // Sonidos sencillos generados con formulas (ver MakeClip).
            k.Hover = MakeClip("sfx_hover", 0.08f, t => Tone(t, 880f) * Mathf.Exp(-t * 45f) * 0.30f);
            k.Grab = MakeClip("sfx_agarrar", 0.12f, t => (Tone(t, 520f) * 0.6f + Noise(t) * 0.15f) * Mathf.Exp(-t * 30f) * 0.45f);
            k.Drop = MakeClip("sfx_soltar", 0.16f, t => Tone(t, 230f) * Mathf.Exp(-t * 22f) * 0.45f);
            k.Impact = MakeClip("sfx_golpe", 0.22f, t => (Tone(t, 140f) * 0.7f + Noise(t) * 0.5f) * Mathf.Exp(-t * 20f) * 0.8f);
            k.Socket = MakeClip("sfx_socket", 0.18f, t => (Tone(t, 700f) + Tone(t, 1050f) * 0.6f) * Mathf.Exp(-t * 24f) * 0.40f);
            k.Complete = MakeClip("sfx_completo", 0.75f, t =>
            {
                float freq = t < 0.2f ? 523f : (t < 0.4f ? 659f : 784f);
                float local = t < 0.2f ? t : (t < 0.4f ? t - 0.2f : t - 0.4f);
                return Tone(t, freq) * Mathf.Exp(-local * 6f) * 0.45f;
            });

            k.Rebote = MakeBounceMaterial();

            return k;
        }

        /// <summary>Crea un material fisico con rebote. Se busca el tipo por nombre porque en Unity 6 se llama PhysicsMaterial y antes PhysicMaterial.</summary>
        static UnityEngine.Object MakeBounceMaterial()
        {
            try
            {
                string path = Root + "/Materiales/Fis_Rebote.asset";
                UnityEngine.Object existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                if (existing != null)
                {
                    return existing;
                }

                Type t = FindType("PhysicsMaterial");

                if (t == null || !typeof(UnityEngine.Object).IsAssignableFrom(t))
                {
                    t = FindType("PhysicMaterial");
                }

                if (t == null)
                {
                    return null;
                }

                UnityEngine.Object material = (UnityEngine.Object)Activator.CreateInstance(t);
                SetProp(material, "bounciness", 0.6f);
                SetProp(material, "dynamicFriction", 0.4f);
                SetProp(material, "staticFriction", 0.4f);
                AssetDatabase.CreateAsset(material, path);
                return material;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Taller XRI] No pude crear el material fisico de rebote: " + e.Message);
                return null;
            }
        }
    }
}
