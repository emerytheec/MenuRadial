# Changelog

Todos los cambios notables de este proyecto seran documentados en este archivo.

El formato esta basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [0.8.24] - 2026-02-10

### Eliminado
- **Código muerto**: 11 archivos eliminados (~1400 líneas) — RadialLifecycleManager, RadialUnityValidationManager, RadialPropertyValidator, RadialPathProcessor, RadialPropertyNotifier, IFrameComponent, IIlluminationComponent, PreviewStrategyBase, FrameOperationHelper, IFrameEventNotifier, directorio Internal/
- **RadialPropertyManager**: Simplificado de 393 a 104 líneas — inlineados 3 delegados (Validator, PathProcessor, Notifier), eliminados 7 métodos muertos (DoesPathExist, GetPathStatistics, GetPropertiesSummary, GenerateUniqueName, SuggestPathFromHierarchy, SetProperties, ConnectServiceCoordinator)
- **ObjectPool**: Simplificado de 241 a 69 líneas — eliminados 8 pools sin uso, PoolExtensions y PooledObject
- **RadialMenuState**: Eliminado campo `_propertyNotifier` (nunca accedido externamente) y llamada no-op `ConnectServiceCoordinator(null)`
- **Interfaces muertas**: Removido `IFrameComponent` de MRAgruparObjetos, `IIlluminationComponent` de MRIluminacionRadial
- **RadialPreviewManager**: Removido campo `_previewStrategy` sin uso

## [0.8.23] - 2026-02-10

### Eliminado
- **Código muerto**: ~30 archivos eliminados (~4500 líneas) — preview duplicados (FrameStateManager, MRFrameStateManager, FramePreviewService, PreviewStateManager, PreviewOperations), managers sin uso (FrameObjectManager, FrameMaterialManager, FrameBlendshapeManager, BaseReferenceManager), RadialPreviewService, WeakEventManager, ValidationRules, MRServiceInitializer, MRServiceAttribute
- **LinqOptimizations**: Clase eliminada (~330 líneas de extension methods sin callers), solo queda `FrameBasedCache`
- **ObjectPool**: Campos de estadísticas sin uso (`_totalCreated`, `_totalRequested`, `_totalReturned`, `_poolHits`)
- **MenuRadialServiceBootstrap**: Simplificado de 167 a 27 líneas — eliminada infraestructura DI (diccionarios, factories, `EnsureInitialized`, `ForceReinitialize`, `Cleanup`, `TryGetService`, 6 métodos muertos). Reemplazado con lazy singletons directos

### Corregido
- **MRMeshImportFixPlugin**: Ya no modifica assets del proyecto durante build NDMF (violaba principio no-destructivo). Ahora emite `Debug.LogWarning` con detalles de meshes que necesitan corrección manual
- **Matching externo NDMF**: Los 3 plugins (MRMenuRadialPlugin, MRCoserRopaPlugin, MRDisableMAPlugin) ahora validan `VRCAvatarDescriptor` en el AvatarRoot, excluyen el clon del avatar, y advierten si hay múltiples matches
- **Límite 256 bits**: `MRVRChatFileGenerator` ahora valida el costo de bits ANTES de generar archivos y bloquea la generación si excede el límite de 256 bits de VRChat

## [0.8.22] - 2026-02-10

### Corregido
- **CI/CD**: Workflow de release ya no falla cuando `VPM_TOKEN` expira o no está configurado
  - Usa `steps.checkout_vpm.outcome` en vez de `success()` para detectar fallo real del checkout
  - Verifica existencia del archivo `index.json` en vez del directorio (que se crea vacío al fallar)
  - Agrega paso de diagnóstico con warning claro cuando el token falta o es inválido
  - El release se crea correctamente independientemente del estado del VPM listing

## [0.8.21] - 2026-02-10

### Corregido
- **ObjectReference**: `Equals()` y `GetHashCode()` ahora incluyen el estado `IsActive` en la comparación, evitando que dos referencias al mismo objeto con diferente estado se consideren iguales
- **FrameData**: `ValidateReferences()` ahora valida objetos, materiales y blendshapes (antes solo validaba objetos)
- **FrameData**: `MaterialReferences` usa cache para evitar alocación de lista en cada acceso
- **MRDisableMAPlugin**: Usa `DestroyImmediate` en vez de `enabled = false` porque Modular Avatar detecta componentes desactivados con `GetComponentsInChildren(true)`
- **FramePreviewService**: `UpdateSavedStates()` bloquea durante preview activo para no capturar estados de preview como originales
- **IlluminationAnimationGenerator**: `ValidateMaterials()` retorna `false` para listas donde todos los materiales son null
- **FrameSegmentCalculator**: `ContainsTime()` usa end exclusivo (`<`) consistente con `ContainsFrame()`
- **RadialAnimationBuilder**: Animaciones Linear ahora soportan `MeshRenderer` además de `SkinnedMeshRenderer` para materiales
- **RadialAnimationBuilder**: Valores de blendshape usan presencia en región en vez de comparar con `!= 0f`, permitiendo valor 0 como estado deseado explícito

## [0.8.20] - 2026-01-31

### Mejorado
- **CI/CD**: El workflow de release ahora genera `.unitypackage` además del ZIP para VPM

## [0.8.19] - 2026-01-31

### Agregado
- **MRAnalisisColision**: Detección de meshes en raíz de ropa
  - Nuevo modelo `MeshOnRootEntry` para rastrear Renderers en GameObjects raíz
  - Sección de advertencia en inspector mostrando meshes mal ubicados
  - Botón de selección para localizar meshes problemáticos
  - Ayuda a identificar assets de ropa con meshes incorrectamente en la raíz
- **MRUnificarObjetos**: Botón de duplicar (+) para frames en la lista reordenable
- **MRCoserRopa**: Detección automática de ropa al cambiar la jerarquía
  - `SetDefaultClothingVisibility()` activa raíces de ropa y desactiva meshes
  - Escaneo automático de componentes MA cuando cambia la lista de ropa

### Mejorado
- **MRAnalisisColision**: UI simplificada
  - Removidos botones de acción (Escanear, Desactivar en Raíz, Restaurar Todos)
  - Sección NDMF Preview reemplazada por checkbox simple
  - Nota informativa sobre ubicación de componentes problemáticos
- **MRMenuControlInspector**: Solo resetea previews cuando hay uno activo

### Corregido
- Meshes directamente en raíz de ropa ya no se desactivan incorrectamente

## [0.8.18] - 2026-01-26

### Mejorado
- **MRAnalisisColision**: Mejoras en detección y desactivación de componentes críticos
  - Detecta `VertexFilterByAxisComponent` (nombre real del filtro de vértices de MA)
  - Componentes críticos (MeshCutter, VertexFilter, ShapeChanger) en raíz de ropa se muestran en **rojo y negrita**
  - Auto-desactivación de componentes críticos persiste en Edit Mode (marca escena como dirty)
  - Checkboxes funcionan correctamente con SerializedProperty
  - Categorías con 0 componentes se muestran en gris en lugar de desaparecer
  - Texto alineado correctamente a la derecha
- **ModularAvatarDetector**: Detección dinámica mejorada
  - Busca en namespaces adicionales (`nadena.dev.modular_avatar.core.vertex_filters`)
  - Incluye tipos que no empiezan con "ModularAvatar" pero pertenecen a MA
  - Método `DebugListAllComponents()` para diagnóstico
- **ColisionEntry**: Los métodos `Disable()` y `Restore()` marcan objetos como dirty en Edit Mode
- **MRMenuRadial**: Escaneo automático de colisiones después de detectar ropas

### Corregido
- Checkboxes de componentes ahora funcionan correctamente (usaban Toggle en lugar de EditorGUILayout.Toggle)
- Lógica de checkbox invertida: marcado = mantener activo, desmarcado = desactivar
- Problematic que NO están en raíz de ropa se reclasifican como UserDecision

## [0.8.17] - 2026-01-25

### Agregado
- **MRAnalisisColision**: Nuevo componente para detectar y gestionar conflictos con Modular Avatar
  - Categoriza componentes de MA en tres niveles: Problemático, Decisión Usuario, Compatible
  - **Problemático** (rojo): MA Vertex Filter, MA Mesh Cutter, MA Shape Changer, MA Mesh Settings
    - Se desactivan automáticamente si están en raíz de ropa
  - **Decisión Usuario** (amarillo): Animator, MA Merge Animator, MA Parameters, MA Menu Installer, MA Menu Group, MA Menu Item, MA Bone Proxy
    - Checkboxes para que el usuario decida qué desactivar
  - **Compatible** (verde): MA Merge Armature (MR ya lo respeta)
  - Inspector con secciones colapsables y código de colores
  - Botones "Desactivar Todos", "Desactivar en Raíz", "Restaurar Todos"
  - Indicador de conflictos en panel de componentes hijos de MRMenuRadial
- **MRAnalisisColisionPlugin**: Plugin NDMF que se ejecuta en fase Resolving ANTES de Modular Avatar
  - Desactiva componentes problemáticos en raíz de ropa automáticamente
  - Desactiva componentes marcados por el usuario
  - Se destruye después de procesar

### Mejorado
- **ModularAvatarDetector**: Nuevos arrays de clasificación y métodos
  - `MA_PROBLEMATIC_ON_ROOT`: Componentes que causan conflictos directos
  - `MA_USER_DECISION`: Componentes que requieren decisión del usuario
  - `MA_COMPATIBLE`: Componentes compatibles con MR
  - `ClassifyComponent()`: Clasifica componentes por categoría
  - `ScanForColisions()`: Escanea avatar buscando componentes de MA
- **MRMenuRadial**: Nueva referencia a MRAnalisisColision
  - Propiedades `DetectedColisionCount` y `HasProblematicColisions`
  - Propagación automática de avatar al componente
- **MRMenuRadialEditor**: Indicador visual de conflictos (⚠) en panel de componentes hijos

## [0.8.16] - 2026-01-25

### Agregado
- **MRMeshImportFixPlugin**: Nuevo plugin NDMF para corregir automáticamente problemas de importación de meshes
  - Corrige Read/Write disabled automáticamente
  - Corrige Blendshape Normals de "Calculate" a "Import/Legacy" (reduce tamaño del avatar)
  - Se ejecuta al inicio del build, antes de que VRChat SDK muestre errores
- **MeshVRAMCalculator**: Nuevo calculador de VRAM para meshes y blend shapes
  - Calcula VRAM de vértices con atributos (position, normal, tangent, UV, bone weights)
  - Calcula VRAM de index buffers (16/32 bits)
  - Calcula VRAM de blend shapes (40 bytes por vértice afectado con delta no-cero)
- **AssetSizeCalculator**: Nuevo calculador de tamaño para otros assets
  - Escanea animaciones usando Profiler API
  - Escanea materiales y audio
  - Integración con VRCAvatarDescriptor layers

### Mejorado
- **MRPesoTexturas**: Cálculo de VRAM significativamente más preciso
  - Nuevo algoritmo de mipmaps iterativo (reemplaza factor 1.33x)
  - Tabla de 35+ formatos de textura con bits-per-pixel exactos (incluyendo ASTC)
  - Escaneo de UI Sprites y Reflection Probes
  - Desglose completo en resumen: Texturas, Meshes, Blend Shapes, Animaciones, Materiales, Audio
  - Muestra "Total Bundle" (suma de todos los assets)
  - Texto en rojo si excede 500 MB de VRChat
- **TextureEntry**: Ahora almacena formato real de textura y mipmap count para cálculo preciso
- **Grupos de texturas**: Ahora están colapsados por defecto

### Eliminado
- Estimación de bundle basada en factor (reemplazada por cálculo real de assets)

## [0.8.15] - 2026-01-24

### Agregado
- **FolderStructureAnalyzer**: Nuevo analizador de estructura de carpetas para detección de materiales
- **Reordenamiento de materiales**: Botones ▲/▼ para mover materiales dentro de grupos
- **Detección de carpeta dominante**: Caso 2 ahora detecta carpeta principal con referencias puntuales

### Mejorado
- **Nombres de casos más amigables**:
  - Caso 1 → "Grupos en carpeta"
  - Caso 2 → "Estilo en carpeta"
  - Caso 3 → "Todo en carpeta"
- **UI reorganizada**: Nuevo orden de secciones (Slots → Vinculación → Drop Area → Grupos → Sugerencias)
- **Botón "Detectar Alternativas"**: Ahora es de color verde para mejor visibilidad
- **Filtrado de carpetas**: Respeta correctamente las carpetas seleccionadas manualmente

### Corregido
- **Filtrado de FolderInfos**: Ahora se filtra junto con las demás listas al seleccionar carpetas
- **Grupos de 1 material**: Ya no se muestran como sugerencias (se requieren 2+ materiales)
- **Paso de parámetros**: El modo forzado y carpetas seleccionadas ahora se pasan correctamente al detector

### Eliminado
- Sección "Estado" removida del UI del componente

## [0.8.14] - 2026-01-22

### Corregido
- **Localización**: Agregados 50+ campos faltantes en LocaleSection
  - Corrige textos que aparecían como `[]` en la UI de MRUnificarMateriales y MRAgruparMateriales
  - Campos para secciones: Common, Radial, Illumination, Menu, CoserRopa, UnifyMaterial, AlternativeMaterial
- **MRAgruparMateriales**: Corregido NullReferenceException al limpiar sugerencias

### Mejorado
- **MRAgruparMateriales UI**: Reorganización del inspector
  - Grupos de materiales ahora están debajo de Slots y arriba de Sugerencias
  - Grupos colapsados por defecto
  - Sugerencias de slots colapsadas por defecto al detectar
  - Nota de ayuda siempre visible arriba de la sección de sugerencias

## [0.8.13] - 2026-01-22

### Mejorado
- **MRPesoTexturas**: Mejora significativa en precisión del cálculo de peso VRAM
  - Nuevo `AnimationMaterialAnalyzer` para detectar materiales referenciados en animaciones
  - Filtro de texturas de `Packages/` y `Library/` (no contadas por VRChat)
  - Detección de `TextureCompressionType` para normal maps (BC5) y single channel (BC4)
  - Factor de mipmaps ajustado a 1.0 para coincidir con Mip Streaming de VRChat
  - Resultado: cálculo ahora dentro de ~25 MB del valor reportado por VRChat

## [0.8.12] - 2026-01-21

### Agregado
- **MRPesoTexturas**: Nuevo componente para analizar y optimizar peso de texturas
  - Escanea texturas del avatar base, ropas y materiales alternativos
  - Calcula peso estimado en VRAM usando fórmula de compresión BC
  - Agrupa texturas por fuente (avatar/ropa) para vista organizada
  - Función step-down para reducir resoluciones de texturas
  - Detección y corrección automática de Mip Streaming (requerido por VRChat)
  - Advertencias visuales para texturas pesadas (>10MB) y peso total (>500MB limite VRChat)
  - Integración con MRCoserRopa para detección de ropas
  - Integración con MRAgruparMateriales para texturas de materiales alternativos
- Nuevas constantes de peso de texturas en MRConstants.cs

## [0.8.11] - 2026-01-21

### Agregado
- **Sistema de detección automática de materiales alternativos**:
  - `MaterialAlternativeDetector`: Analiza estructura de carpetas y patrones de nombres
  - `MaterialSuggestion`: Estructuras de datos para sugerencias con niveles de confianza
  - UI de sugerencias colapsable en el inspector de MRAgruparMateriales
  - Detección de carpetas hermanas de variantes
  - Soporte para colores simples, compuestos (Wine_Red), y con número (brown1)
  - Reconocimiento de sub-partes (kanagu, button, lace, etc.)
  - Manejo de sufijos de material (_M, _Mat, _Texture)
  - División por CamelCase para nombres sin separadores (mt1A → mt1 + A)
  - Patrones de versión extendidos (a-f, v1, ver1, alt, var)
  - Sistema de puntuación de confianza (Alta ≥80%, Media ≥50%, Baja <50%)
- Localización en español para todas las nuevas funcionalidades

## [0.8.10] - 2026-01-20

### Agregado
- **Auto-generación de estructura de materiales**: Al asignar avatar con ropas detectadas
  - Crea MRUnificarMateriales como hijo de MenuControl
  - Crea MRAgruparMateriales por cada ropa detectada (no para avatar base)
  - Escanea automáticamente los slots de material de cada ropa
- **MRAgruparMateriales.SourceGameObject**: Nueva propiedad para rastrear el GameObject escaneado
- **MRAgruparMateriales.RescanFromSource()**: Método para re-escanear materiales si se modifica la ropa

### Mejorado
- **HasExistingStructure()**: Ahora detecta MRUnificarMateriales existente para evitar regeneración
- **GenerationResult**: Incluye información de materiales (frames creados, slots detectados)

## [0.8.9] - 2026-01-20

### Mejorado
- **Menú contextual simplificado**: "MR Menu Radial" ahora aparece directamente en el click derecho del Hierarchy, sin necesidad de navegar por submenús

## [0.8.8] - 2026-01-20

### Agregado
- **Compatibilidad con Modular Avatar**:
  - `ModularAvatarDetector`: Detecta componentes MA via reflexión sin dependencia directa
  - MRCoserRopa detecta y respeta MA Merge Armature en ropas (MA tiene prioridad)
  - Detección de MA Shape Changer con advertencias en editor
  - Detección y desactivación automática de MA Mesh Settings para evitar conflictos
- **Anchor Override en MRAjustarBounds**:
  - Nueva opción para unificar Probe Anchor de todos los meshes
  - Auto-detección del hueso Chest del avatar
  - Mismo funcionamiento que MA Mesh Settings
- **Configuración de integración del menú VRChat**:
  - Campo de nombre personalizado para el menú
  - Campo de icono personalizado (Texture2D)
  - Tres modos de ubicación: Menú Raíz, Submenú existente, Ruta personalizada
  - Creación automática de rutas de menús anidados (ej: "Outfits/Casual")

### Corregido
- **Iconos no aparecían en instalaciones VPM**:
  - MRIconLoader ahora usa `Resources.Load()` en lugar de rutas hardcodeadas
  - Funciona correctamente cuando el paquete está en `Packages/` (VPM) o `Assets/`
- **Auto-generación de menú**: Meshes ahora se configuran como `IsActive=true` por defecto

### Mejorado
- MRLocalizationWindow busca carpeta Locales dinámicamente
- ICONS_PATH marcado como obsoleto con guía para usar Resources.Load

## [0.8.7] - 2026-01-16

### Corregido
- **Compilacion Runtime**: Archivos de editor envueltos en `#if UNITY_EDITOR`
  - MRIconLoader, RadialIconManager, RadialMenuRenderer, RadialMenuInteractionHandler
  - RadialSliderIntegration, SimpleRadialMenuDrawer
  - MRMenuControl: llamadas a RadialSliderIntegration envueltas en directivas
  - Previene errores de compilacion cuando VRChat SDK construye el avatar
- **MRMenuControl**: Implementa `IEditorOnly` para evitar advertencias de VRChat SDK

## [0.8.6] - 2026-01-16

### Corregido
- **MRMenuRadialPlugin NDMF**: Limpieza completa de componentes MR durante build
  - `CleanupComponents()` ahora elimina TODOS los componentes del namespace `Bender_Dios.MenuRadial`
  - Previene advertencias de VRChat SDK al subir avatar ("MRMenuControl will be removed by client")
  - Busca MonoBehaviours por namespace ademas de tipos especificos

## [0.8.5] - 2026-01-16

### Corregido
- **RadialAnimationBuilder**: Bug en animaciones lineales (3+ frames)
  - El codigo ignoraba el valor `IsActive` de cada ObjectReference
  - Si un objeto estaba en un frame, siempre se ponia en 1 sin importar su IsActive
  - Ahora guarda y usa el valor IsActive configurado por el usuario en cada frame
  - Permite tener el mismo objeto en multiples frames con diferentes estados (ON/OFF)

## [0.8.4] - 2026-01-16

### Cambiado
- **MRMenuRadialPlugin NDMF**: Reescrito completamente para usar archivos generados
  - NDMF ahora USA los archivos creados con "Generar Archivos VRChat" en lugar de crear nuevos
  - Busca FX Controller, Parameters y Menu en la ruta de salida configurada
  - Si no encuentra los archivos, muestra advertencia y no hace nada
  - Mezcla los archivos encontrados con los del avatar (respeta existentes)

### Corregido
- MissingReferenceException cuando MRMenuRadial esta fuera del avatar
  - Ahora detecta si es interno o externo y solo limpia componentes internos
  - Guarda datos antes de operaciones que puedan destruir objetos

### Mejorado
- Verificaciones null y try-catch en clonado de StateMachines
- Mejor manejo de errores durante el proceso NDMF

## [0.8.3] - 2026-01-16

### Agregado
- **Control de Procesos NDMF**: Nuevos checkboxes en MRMenuRadial para control granular
  - `Desactivar Cosido de Huesos`: Evita que NDMF cosa automaticamente los armatures de ropa
  - `Desactivar Merge VRChat`: Evita que NDMF mezcle FX/Parameters/Menu con el avatar
  - Util para debugging y pruebas sin cambios automaticos
- Nueva seccion "NDMF - Control de Procesos" en el inspector de MRMenuRadial
- Advertencia visual cuando los procesos estan desactivados

### Mejorado
- MRCoserRopaPlugin verifica flag antes de ejecutar cosido
- MRMenuRadialPlugin verifica flag antes de ejecutar merge
- Documentacion actualizada en CLAUDE.md

## [0.8.2] - 2026-01-16

### Corregido
- **RadialPuppet**: Los valores ahora persisten correctamente al salir del menu
  - parameter.name debe estar vacio para RadialPuppet (parametro va solo en subParameters)
  - writeDefaultValues=false para estados Linear en FX controller

### Agregado
- **MRMenuRadialPlugin**: Plugin NDMF con integracion AnimatorServicesContext
  - Genera layers, parametros y menus automaticamente durante el build
  - Soporte para valores por defecto en parametros

### Mejorado
- MRFXControllerGenerator usa valores por defecto apropiados para parametros
- MRSubMenuManager y AutoMenuGenerator con mejoras varias

## [0.8.1] - 2026-01-15

### Eliminado
- **Soporte de Poiyomi**: Removido completamente del sistema de iluminacion
  - Eliminado PoiyomiShaderStrategy.cs
  - Eliminadas constantes MRPoiyomiShaderProperties y MRPoiyomiIlluminationConstants
  - Eliminadas propiedades de Poiyomi en IlluminationProperties
  - Eliminado UI de advertencias y preparacion de materiales Poiyomi
  - MRIluminacionRadial ahora solo soporta shaders lilToon

## [0.8.0] - 2026-01-14

### Agregado
- **MRMenuRadial**: Nuevo contenedor principal del sistema
  - Propaga avatar automaticamente a todos los componentes hijos
  - Auto-deteccion de ropas, PhysBones y meshes al asignar avatar
  - Auto-generacion de estructura de menu (MRUnificarObjetos + MRAgruparObjetos)
  - Panel de estado visual con progreso de cada componente
  - Botones "Preparar Todo" y "Generar Archivos VRChat" centralizados
- **AutoMenuGenerator**: Generador automatico de estructura de menu
  - Crea frame "Avatar" con meshes de accesorios
  - Crea frame por cada ropa detectada en MRCoserRopa
  - Usa reflexion para acceso cross-assembly a MRMenuControl
- **BodyMeshDetector**: Detector inteligente de meshes
  - Patrones de exclusion para body, head, hair, eyes
  - Patrones de ropa que NO deben excluirse (outfit, under_, item_, etc)
  - Analisis de huesos humanoid (>70% = body mesh)
  - Prioridad de patrones de ropa sobre analisis de huesos

### Mejorado
- MRSubMenuManager propaga avatar a MRIluminacionRadial al crearlo
- Limpieza de logs de depuracion excesivos
- Documentacion actualizada en CLAUDE.md

## [0.7.0] - 2026-01-13

### Agregado
- **MROrganizaPB**: Nuevo componente para organizar PhysBones y Colliders
  - Reorganiza VRCPhysBone y VRCPhysBoneCollider en contenedores organizados
  - Organizacion en tiempo de editor (no solo durante build NDMF)
  - Sistema de estados: NotScanned -> Scanned -> Organized
  - Funcion de revertir para devolver componentes a su ubicacion original
  - Deteccion automatica de contexto (avatar vs ropa)
  - Permite controlar dinamicas desde MRAgruparObjetos
  - Contenedores PhysBones/ y Colliders/ como hermanos del Armature

### Mejorado
- Documentacion actualizada en CLAUDE.md

## [0.6.0] - 2026-01-12

### Agregado
- **MRAjustarBounds**: Nuevo componente para ajustar bounds de meshes y particulas
  - Escaneo automatico de SkinnedMeshRenderer y ParticleSystem
  - Calculo de bounds unificados para meshes
  - Calculo de bounds individuales para particulas
  - Margen configurable (10% meshes, 20% particulas por defecto)
  - Integracion con NDMF para procesamiento automatico
  - Visualizacion de bounding box en Scene View
- Soporte para particulas con checkbox opcional

### Mejorado
- Documentacion actualizada
- Constantes centralizadas en MRConstants.cs

## [0.5.0] - 2026-01-XX

### Agregado
- **MRCoserRopa**: Sistema de cosido de ropa a avatares
  - Deteccion automatica de huesos humanoid
  - BoneNameDatabase con 230+ patrones de nombres
  - Soporte de prefijo/sufijo para nombres de huesos
  - Integracion con NDMF (flujo no-destructivo)
  - Preservacion de huesos no-humanoid (falda, pelo, etc.)

### Mejorado
- Sistema de validacion con cache
- Mejor manejo de errores

## [0.4.0] - 2026-01-XX

### Agregado
- **MRUnificarMateriales**: Control unificado de materiales multiples
- **MRAgruparMateriales**: Agrupacion de materiales alternativos
- Sistema de sliders radiales para componentes Linear

### Mejorado
- UI del editor mejorada
- Mejor rendimiento en preview

## [0.3.0] - 2025-XX-XX

### Agregado
- **MRIluminacionRadial**: Control de iluminacion lilToon
- Soporte para shaders lilToon
- Sistema de preview unificado

## [0.2.0] - 2025-XX-XX

### Agregado
- **MRVRChatFileGenerator**: Generacion completa de archivos VRChat
  - FX Controller con layers por slot
  - VRCExpressionParameters
  - VRCExpressionsMenu con submenus recursivos
- RadialAnimationBuilder: Generacion de archivos .anim

## [0.1.0] - 2025-XX-XX

### Agregado
- **MRAgruparObjetos**: Captura de estados (objetos, materiales, blendshapes)
- **MRUnificarObjetos**: Gestion de frames y tipos de animacion
- **MRMenuControl**: Componente principal del menu
- Sistema de referencias (ObjectReference, MaterialReference, BlendshapeReference)
- Arquitectura base con MRComponentBase
