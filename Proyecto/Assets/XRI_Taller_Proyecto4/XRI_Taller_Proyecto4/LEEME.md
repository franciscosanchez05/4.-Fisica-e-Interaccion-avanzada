# Proyecto 4 · Física e interacción avanzada (XR Interaction Toolkit 3.5)

Proyecto de Unity para la demo del proyecto 4. **Con un solo clic** genera tres escenas completas
con decoración (un taller de realidad virtual), objetos interactivos, sonidos y scripts.
No usa ningún asset externo: texturas, sonidos y modelos (primitivas) se generan por código.

## 1. Requisitos
- **Unity 6 (6000.x)**. Recomendado crear el proyecto con la plantilla **Universal 3D (URP)**.
- **XR Interaction Toolkit 3.5.x** (se probó la referencia con 3.5.1).

## 2. Instalación (5 pasos)
1. Unity Hub > New project > plantilla **Universal 3D** > nombre `XRI_Taller`.
2. Window > Package Manager > Unity Registry > **XR Interaction Toolkit** > elegir la versión **3.5.1** > Install.
   Si Unity pregunta por activar el nuevo sistema de entrada y reiniciar, acepta.
3. Cierra Unity y copia la carpeta **`Assets/_Proyecto`** de este zip dentro de `Assets/` de tu proyecto. Abre el proyecto.
4. Espera a que compile (si hay errores en rojo, mira el apartado 6).
5. Menú **Taller XRI > 1) Construir proyecto completo**. Si faltan los samples *Starter Assets* y
   *XR Interaction Simulator*, los importa solos y continúa cuando termina de compilar (1-2 minutos).

Al terminar se abre **Taller_Final**. Pulsa Play y usa el simulador (el panel de teclas aparece en pantalla).

## 3. Qué se genera
| Escena | Contenido |
|---|---|
| **Taller_Final** | La demo completa: sala decorada, mesa con CuboPruebas, Pared, CuboVisual, Pelota, tres herramientas, tablón, panel con 3 sockets, indicadores y luz de «caja completa». |
| **Taller_Inicial** | Punto de partida del RETO: la misma sala con las tres herramientas SIN configurar y el panel sin sockets. |
| **Reto_Solucion** | Solución del reto (herramientas configuradas, sockets, capas, indicadores y gestor). |

Decoración: suelo de baldosas, pared de ladrillo, rodapiés y zócalo, cornisa, puerta, ventana luminosa, 3 lámparas con luz,
alfombra, banco de trabajo con cajones y tornillo de banco, flexo, estantería con cajas y botes, barriles, cajones de madera,
taburete, caja de herramientas roja, planta, extintor, reloj, cartel «TALLER VR», pósters y señales de peligro.

## 4. Controles de la demo (Play)
- **Z / X / C**: Movement Type del CuboPruebas = Instantaneous / Kinematic / Velocity Tracking.
- **V**: activa/desactiva Throw On Detach.
- **H**: oculta o muestra la guía en pantalla.
- Si alguna tecla choca con el simulador, cámbiala en el Inspector (objeto *DemoController*).

## 5. Scripts (todos comentados en español, en `Scripts/Runtime`)
| Script | Para qué sirve | Objeto |
|---|---|---|
| `MovementTypeSwitcher` | Cambia el Movement Type en directo y lo muestra en pantalla | DemoController |
| `DemoGuiaHud` | Chuleta con los 5 bloques y las teclas | DemoController |
| `InteractionStateLabel` | Etiqueta IDLE / HOVER / SELECT / ACTIVATE | CuboPruebas, Tool_Martillo |
| `GrabFeedback` | Color y sonido en hover, agarrar y soltar (affordances) | Cubo, pelota, tablón, herramientas |
| `SocketIndicator` | Indicador rojo/verde y sonido al encajar | Cada Socket_* |
| `ToolboxCompletion` | Enciende una luz cuando los 3 sockets están ocupados | GestorCaja |
| `RespawnOnFall` | Devuelve el objeto a su sitio si se cae | Todos los agarrables |
| `ImpactSound` | Sonido de golpe según la velocidad del choque | Todos los agarrables |
| `Extras/OutlineOnHover.cs.txt` | (Opcional) Contorno con Quick Outline | — |

El código del constructor está en `Scripts/Editor` (es código de editor, no forma parte del juego).

## 6. Si algo falla
| Problema | Solución |
|---|---|
| Errores rojos de «namespace … does not exist» en los scripts | Los scripts usan los espacios de nombres de XRI 3.x (`...Toolkit.Interactables` e `...Toolkit.Interactors`). Comprueba que tienes XRI **3.x** (no 2.x). |
| Aviso «No encuentro el prefab XR Origin (XR Rig)» | Importa a mano los samples: Package Manager > XR Interaction Toolkit > Samples > Import (*Starter Assets* y *XR Interaction Simulator*) y vuelve a pulsar el menú 1. |
| Aviso de capas de interacción | Créalas en Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit > Interaction Layers: **Martillo, Destornillador, Llave**. Después pulsa **Taller XRI > 2) Reparar capas** con la escena abierta. |
| Aviso «La propiedad … no existe» | Esa propiedad de XRI cambió de nombre en tu versión. La escena sigue funcionando; ajusta el valor a mano en el Inspector (la consola dice cuál). |
| Todo se ve rosa | Estás en el pipeline Built-in con materiales URP del sample. Usa la plantilla Universal 3D (URP). |
| Los sockets aceptan cualquier herramienta | Faltan las capas de interacción (fila anterior). |

## 7. Assets de terceros (opcionales)
El proyecto no los necesita. Si quieres modelos mejores, sustituye el hijo *Modelo* de cada `Tool_*` por los
prefabs de **Workplace Tools** (Ilias Kap, gratuito) y mantén el hijo *Attach*. Para contornos de hover usa **Quick Outline**
(Chris Nolet, gratuito) con `Extras/OutlineOnHover.cs.txt`. Para sonidos reales, **FREE Casual Game SFX Pack** (Dustyroom).
Confirma siempre que ponga FREE en la Asset Store y no incluyas estos assets en la entrega pública (licencia).

## 8. Diferencias respecto al PDF de guion
- Las teclas del Movement Type son **Z / X / C / V** (en el PDF aparecían F1-F4; se cambió para no chocar con atajos del editor).
- Las herramientas son modelos hechos con primitivas (martillo, destornillador y llave inglesa).
- El feedback usa `GrabFeedback` y `SocketIndicator` (el «plan B» del PDF); el sistema de affordances de XRI se puede añadir a mano en el bloque 5.
