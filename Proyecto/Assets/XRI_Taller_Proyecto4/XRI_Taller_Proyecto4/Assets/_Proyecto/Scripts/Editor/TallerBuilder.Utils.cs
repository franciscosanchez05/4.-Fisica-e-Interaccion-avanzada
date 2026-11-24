using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// =====================================================================
// TallerBuilder (parte 1: utilidades)
//
// Este archivo contiene las "herramientas" que usa el constructor:
//   - buscar tipos por nombre (reflexion) para que el constructor compile
//     aunque cambie la API de XRI entre versiones;
//   - crear materiales, texturas y sonidos generados por codigo;
//   - atajos para crear objetos con primitivas de Unity.
// Es codigo de EDITOR: solo funciona dentro del editor de Unity y no
// forma parte del juego.
// =====================================================================
namespace TallerXRI.Editor
{
    public static partial class TallerBuilder
    {
        const string Root = "Assets/_Proyecto";
        const string MatDir = Root + "/Materiales";
        const string TexDir = Root + "/Texturas";
        const string AudioDir = Root + "/Audio";
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Escenas";

        static void EnsureFolders()
        {
            foreach (string dir in new[] { MatDir, TexDir, AudioDir, PrefabDir, SceneDir })
            {
                Directory.CreateDirectory(dir);
            }

            AssetDatabase.Refresh();
        }

        // -----------------------------------------------------------------
        // REFLEXION: buscar tipos y propiedades por nombre
        // -----------------------------------------------------------------
        static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        /// <summary>Busca un tipo por su nombre corto (sin namespace) en todos los ensamblados cargados.</summary>
        static Type FindType(string shortName)
        {
            Type cached;
            if (typeCache.TryGetValue(shortName, out cached))
            {
                return cached;
            }

            Type found = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (Type t in types)
                {
                    if (t != null && t.Name == shortName)
                    {
                        found = t;
                        break;
                    }
                }

                if (found != null)
                {
                    break;
                }
            }

            typeCache[shortName] = found;
            return found;
        }

        /// <summary>Anade un componente a un GameObject buscando su tipo por nombre.</summary>
        static Component AddByName(GameObject go, string typeName)
        {
            Type t = FindType(typeName);

            if (t == null)
            {
                Debug.LogWarning("[Taller XRI] No encuentro el tipo '" + typeName + "'. Se omite en '" + go.name + "'.");
                return null;
            }

            return go.AddComponent(t);
        }

        /// <summary>Asigna una PROPIEDAD publica (por nombre) de un objeto. Si falla, avisa y sigue.</summary>
        static void SetProp(object target, string propName, object value)
        {
            if (target == null)
            {
                return;
            }

            PropertyInfo p = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

            if (p == null || !p.CanWrite)
            {
                Debug.LogWarning("[Taller XRI] La propiedad '" + propName + "' no existe en " + target.GetType().Name + " (version de XRI distinta?).");
                return;
            }

            try
            {
                object v = value;
                string s = value as string;

                if (p.PropertyType.IsEnum && s != null)
                {
                    v = Enum.Parse(p.PropertyType, s);
                }
                else if (p.PropertyType.Name == "InteractionLayerMask" && value is int)
                {
                    v = MakeLayerMask((int)value);
                }

                p.SetValue(target, v);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Taller XRI] No pude asignar '" + propName + "': " + e.Message);
            }
        }

        /// <summary>Asigna un CAMPO serializado (por nombre) de uno de nuestros scripts, via SerializedObject.</summary>
        static void SetField(Component c, string field, object value)
        {
            if (c == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(c);
            SerializedProperty sp = so.FindProperty(field);

            if (sp == null)
            {
                Debug.LogWarning("[Taller XRI] El campo '" + field + "' no existe en " + c.GetType().Name + ".");
                return;
            }

            if (value is UnityEngine.Object[])
            {
                UnityEngine.Object[] array = (UnityEngine.Object[])value;
                sp.arraySize = array.Length;

                for (int i = 0; i < array.Length; i++)
                {
                    sp.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                }
            }
            else if (value is UnityEngine.Object)
            {
                sp.objectReferenceValue = (UnityEngine.Object)value;
            }
            else if (value is float)
            {
                sp.floatValue = (float)value;
            }
            else if (value is int)
            {
                sp.intValue = (int)value;
            }
            else if (value is bool)
            {
                sp.boolValue = (bool)value;
            }
            else if (value is string)
            {
                sp.stringValue = (string)value;
            }
            else if (value is Color)
            {
                sp.colorValue = (Color)value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // -----------------------------------------------------------------
        // CAPAS DE INTERACCION (Interaction Layers de XRI)
        // -----------------------------------------------------------------

        /// <summary>Convierte un entero en un InteractionLayerMask usando su operador implicito.</summary>
        static object MakeLayerMask(int bits)
        {
            Type t = FindType("InteractionLayerMask");

            if (t == null)
            {
                return null;
            }

            foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] ps = m.GetParameters();

                if (m.Name == "op_Implicit" && m.ReturnType == t && ps.Length == 1 && ps[0].ParameterType == typeof(int))
                {
                    return m.Invoke(null, new object[] { bits });
                }
            }

            return null;
        }

        /// <summary>Devuelve la mascara (entero) con las capas de interaccion indicadas, o 0 si no existen.</summary>
        static int LayerMaskOf(params string[] names)
        {
            Type t = FindType("InteractionLayerMask");
            MethodInfo m = t != null ? t.GetMethod("GetMask", BindingFlags.Public | BindingFlags.Static) : null;

            if (m == null)
            {
                return 0;
            }

            try
            {
                return (int)m.Invoke(null, new object[] { names });
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>Intenta crear las capas de interaccion en los ajustes de XRI. Devuelve true si todas existen al final.</summary>
        static bool EnsureInteractionLayers(string[] names)
        {
            bool allExist = true;

            foreach (string n in names)
            {
                if (LayerMaskOf(n) == 0)
                {
                    allExist = false;
                }
            }

            if (allExist)
            {
                return true;
            }

            try
            {
                Type settings = FindType("InteractionLayerSettings");

                if (settings == null)
                {
                    return false;
                }

                const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                                         | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

                PropertyInfo instProp = settings.GetProperty("Instance", all);
                object instance = instProp != null ? instProp.GetValue(null, null) : null;

                if (instance == null)
                {
                    FieldInfo instField = settings.GetField("Instance", all);
                    instance = instField != null ? instField.GetValue(null) : null;
                }

                MethodInfo setName = settings.GetMethod("SetLayerNameAt", all);
                MethodInfo getName = settings.GetMethod("GetLayerNameAt", all);

                if (instance == null || setName == null || getName == null)
                {
                    return false;
                }

                foreach (string n in names)
                {
                    if (LayerMaskOf(n) != 0)
                    {
                        continue;
                    }

                    // Las primeras capas son las integradas; buscamos la primera libre a partir de la 6.
                    for (int i = 6; i < 32; i++)
                    {
                        string current = getName.Invoke(instance, new object[] { i }) as string;

                        if (string.IsNullOrEmpty(current))
                        {
                            setName.Invoke(instance, new object[] { i, n });
                            break;
                        }
                    }
                }

                UnityEngine.Object asObject = instance as UnityEngine.Object;

                if (asObject != null)
                {
                    EditorUtility.SetDirty(asObject);
                }

                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Taller XRI] No pude crear las capas de interaccion automaticamente: " + e.Message);
            }

            foreach (string n in names)
            {
                if (LayerMaskOf(n) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        // -----------------------------------------------------------------
        // MATERIALES
        // -----------------------------------------------------------------

        /// <summary>Elige el shader segun el pipeline: URP si esta activo, Standard si no.</summary>
        static Shader PickShader()
        {
            bool usesSrp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(usesSrp ? "Universal Render Pipeline/Lit" : "Standard");

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            return shader;
        }

        static void SetFloatIfExists(Material m, string property, float value)
        {
            if (m.HasProperty(property))
            {
                m.SetFloat(property, value);
            }
        }

        /// <summary>Crea (o reutiliza) un material guardado como asset en Assets/_Proyecto/Materiales.</summary>
        static Material Mat(string name, Color color, float metallic = 0f, float smoothness = 0.25f,
                            Texture2D tex = null, float tilingX = 1f, float tilingY = 1f,
                            bool emissive = false, float emissionIntensity = 1.5f, bool transparent = false)
        {
            string path = MatDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (m == null)
            {
                m = new Material(PickShader());
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = PickShader();
            }

            m.color = color;
            SetFloatIfExists(m, "_Metallic", metallic);
            SetFloatIfExists(m, "_Smoothness", smoothness);   // URP
            SetFloatIfExists(m, "_Glossiness", smoothness);   // Standard

            if (tex != null)
            {
                m.mainTexture = tex;
                m.mainTextureScale = new Vector2(tilingX, tilingY);
            }

            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * emissionIntensity);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (transparent)
            {
                MakeTransparent(m);
            }

            EditorUtility.SetDirty(m);
            return m;
        }

        static void MakeTransparent(Material m)
        {
            if (m.HasProperty("_Surface"))
            {
                // Universal Render Pipeline
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else if (m.HasProperty("_Mode"))
            {
                // Standard
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            }
        }

        // -----------------------------------------------------------------
        // TEXTURAS GENERADAS POR CODIGO (no hace falta ningun asset externo)
        // -----------------------------------------------------------------

        /// <summary>Ruido determinista en [0,1) a partir de dos enteros.</summary>
        static float Hash(int x, int y)
        {
            float s = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        static Texture2D MakeTexture(string name, int width, int height, Func<int, int, Color> pixel)
        {
            string path = TexDir + "/" + name + ".png";
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    colors[y * width + x] = pixel(x, y);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D TexFloor()
        {
            return MakeTexture("tex_suelo", 256, 256, (x, y) =>
            {
                int tx = x / 128, ty = y / 128;
                float baseV = 0.58f + (Hash(tx * 7 + 1, ty * 13 + 3) - 0.5f) * 0.10f;
                float noise = (Hash(x, y) - 0.5f) * 0.05f;
                bool joint = (x % 128) < 3 || (y % 128) < 3;
                float v = joint ? 0.32f : baseV + noise;
                return new Color(v, v, v * 1.02f, 1f);
            });
        }

        static Texture2D TexBrick()
        {
            return MakeTexture("tex_ladrillo", 128, 128, (x, y) =>
            {
                int brickH = 16, brickW = 32;
                int row = y / brickH;
                int shifted = x + ((row % 2 == 0) ? 0 : brickW / 2);
                bool mortar = (shifted % brickW) < 2 || (y % brickH) < 2;

                if (mortar)
                {
                    return new Color(0.70f, 0.68f, 0.64f, 1f);
                }

                float jitter = Hash((shifted / brickW) % 4, row % 8);
                float r = 0.55f + jitter * 0.18f;
                float g = 0.24f + jitter * 0.06f;
                float b = 0.18f + jitter * 0.04f;
                float noise = (Hash(x, y) - 0.5f) * 0.05f;
                return new Color(r + noise, g + noise, b + noise, 1f);
            });
        }

        static Texture2D TexWood()
        {
            return MakeTexture("tex_madera", 256, 256, (x, y) =>
            {
                int plank = 64;
                int py = y / plank;

                if ((y % plank) < 2)
                {
                    return new Color(0.25f, 0.16f, 0.09f, 1f);
                }

                float shade = 0.92f + (Hash(7, py) - 0.5f) * 0.22f;
                float grain = Mathf.Sin(x * 0.12f + Hash(0, py) * 10f + Mathf.PerlinNoise(x * 0.02f, y * 0.1f) * 6f) * 0.04f;
                return new Color(0.66f * shade + grain, 0.47f * shade + grain, 0.28f * shade + grain, 1f);
            });
        }

        static Texture2D TexPegboard()
        {
            return MakeTexture("tex_pegboard", 128, 128, (x, y) =>
            {
                float dx = (x % 16) - 8f;
                float dy = (y % 16) - 8f;

                if (dx * dx + dy * dy < 6.5f)
                {
                    return new Color(0.20f, 0.15f, 0.09f, 1f);
                }

                float noise = (Hash(x, y) - 0.5f) * 0.04f;
                return new Color(0.78f + noise, 0.66f + noise, 0.45f + noise, 1f);
            });
        }

        static Texture2D TexHazard()
        {
            return MakeTexture("tex_peligro", 64, 64, (x, y) =>
            {
                bool yellow = ((x + y) / 12) % 2 == 0;
                return yellow ? new Color(1f, 0.80f, 0f, 1f) : new Color(0.08f, 0.08f, 0.08f, 1f);
            });
        }

        static Texture2D TexBlueprint()
        {
            return MakeTexture("tex_poster_plano", 128, 128, (x, y) =>
            {
                Color bg = new Color(0.10f, 0.28f, 0.55f, 1f);
                Color line = new Color(0.55f, 0.75f, 0.95f, 1f);
                float dx = x - 64f, dy = y - 64f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > 26f && dist < 30f) { return Color.white; }
                if (Mathf.Abs(dx) < 1.5f || Mathf.Abs(dy) < 1.5f) { return Color.white; }
                if ((x % 64) < 2 || (y % 64) < 2) { return line; }
                if ((x % 16) < 1 || (y % 16) < 1) { return Color.Lerp(bg, line, 0.45f); }
                return bg;
            });
        }

        static Texture2D TexSunset()
        {
            return MakeTexture("tex_poster_atardecer", 128, 128, (x, y) =>
            {
                float t = y / 127f;
                Color sky = Color.Lerp(new Color(0.95f, 0.45f, 0.20f, 1f), new Color(0.35f, 0.20f, 0.50f, 1f), t);
                float dx = x - 64f, dy = y - 60f;

                if (y < 36) { return new Color(0.12f, 0.10f, 0.20f, 1f); }
                if (dx * dx + dy * dy < 22f * 22f) { return new Color(1f, 0.90f, 0.50f, 1f); }
                return sky;
            });
        }

        static Texture2D TexRug()
        {
            return MakeTexture("tex_alfombra", 128, 64, (x, y) =>
            {
                if (x < 6 || x > 121 || y < 6 || y > 57) { return new Color(0.20f, 0.10f, 0.08f, 1f); }
                return ((x / 16) % 2 == 0) ? new Color(0.62f, 0.15f, 0.12f, 1f) : new Color(0.90f, 0.82f, 0.65f, 1f);
            });
        }

        // -----------------------------------------------------------------
        // SONIDOS GENERADOS POR CODIGO (archivos .wav)
        // -----------------------------------------------------------------
        static AudioClip MakeClip(string name, float seconds, Func<float, float> sample)
        {
            const int sampleRate = 44100;
            int count = (int)(seconds * sampleRate);
            short[] data = new short[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float v = Mathf.Clamp(sample(t), -1f, 1f);
                data[i] = (short)(v * 30000f);
            }

            string path = AudioDir + "/" + name + ".wav";

            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int dataBytes = count * 2;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataBytes);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);          // PCM
                bw.Write((short)1);          // mono
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataBytes);

                for (int i = 0; i < count; i++)
                {
                    bw.Write(data[i]);
                }
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static float Tone(float t, float freq)
        {
            return Mathf.Sin(2f * Mathf.PI * freq * t);
        }

        static float Noise(float t)
        {
            float s = Mathf.Sin(t * 91234.5f) * 43758.5f;
            return (s - Mathf.Floor(s)) * 2f - 1f;
        }

        // -----------------------------------------------------------------
        // ATAJOS PARA CREAR OBJETOS
        // -----------------------------------------------------------------
        enum Col { Keep, None }

        static GameObject Empty(string name, Transform parent, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
                               Material mat, Vector3 euler = default(Vector3), Col col = Col.Keep)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;

            if (mat != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = mat;
            }

            if (col == Col.None)
            {
                Collider c = go.GetComponent<Collider>();

                if (c != null)
                {
                    UnityEngine.Object.DestroyImmediate(c);
                }
            }

            return go;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 size, Material mat,
                               Vector3 euler = default(Vector3), Col col = Col.Keep)
        {
            return Part(PrimitiveType.Cube, name, parent, pos, size, mat, euler, col);
        }

        static GameObject Quad(string name, Transform parent, Vector3 pos, Vector3 euler, float width, float height, Material mat)
        {
            return Part(PrimitiveType.Quad, name, parent, pos, new Vector3(width, height, 1f), mat, euler, Col.None);
        }

        static Light AddLight(string name, Transform parent, Vector3 pos, LightType type, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            Light l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>Busca un prefab por nombre exacto en todo el proyecto (incluidos los samples de paquetes).</summary>
        static GameObject FindPrefab(string exactName)
        {
            string[] guids = AssetDatabase.FindAssets(exactName + " t:Prefab");
            GameObject best = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileNameWithoutExtension(path) != exactName)
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                {
                    continue;
                }

                // Preferimos los que vienen de los samples importados.
                if (path.Contains("Samples") || best == null)
                {
                    best = prefab;
                }
            }

            return best;
        }
    }
}
