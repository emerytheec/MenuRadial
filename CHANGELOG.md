# Changelog

Todos los cambios notables de este proyecto seran documentados en este archivo.

El formato esta basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [0.9.68] - 2026-02-27

### Corregido
- **Fix: meshes de ropa no reconocidos por BodyMeshDetector**: Meshes con nombres como "Gown" eran excluidos del Frame Avatar porque no tenian patron de proteccion de ropa y el analisis de huesos los clasificaba como body. Ampliados ClothingPatterns con ~30 nuevos patrones: gown, cape, cloak, robe, corset, kimono, bikini, choker, necklace, wing, crown, sleeve, legging, blouse, uniform, costume, swimsuit, lingerie, sandal, heel, slipper, backpack, earring, bracelet, pendant, tiara, apron y equivalentes en español (capa, tunica, collar, pulsera, corona, disfraz, uniforme)

## [0.9.67] - 2026-02-27

### Corregido
- **Fix: avatar subia sin ropa con DefaultFrameIndex**: La activacion NDMF de objetos del frame por defecto fallaba silenciosamente porque usaba HierarchyPath (ruta absoluta desde la escena) con transform.Find() (que espera ruta relativa al avatar). Ahora usa referencias directas serializadas que Unity remapea automaticamente al clonar el avatar

## [0.9.66] - 2026-02-27

### Agregado
- **Frame por Defecto en VRChat (DefaultFrameIndex)**: Nuevo selector que permite elegir con que frame inicia el avatar en VRChat. Funciona para OnOff (toggle activado/desactivado), AB (frame A o B) y Linear (cualquier frame). Reemplaza el antiguo DefaultStateIsOn que solo funcionaba para OnOff
- **Activacion NDMF de frame por defecto**: Durante el build NDMF, los objetos, materiales y blendshapes del frame por defecto se activan en el clon del avatar como red de seguridad
- **Localizacion**: 7 nuevas claves (defaultFrame*) en 6 idiomas para el selector de frame por defecto
- **Migracion automatica**: Escenas con DefaultStateIsOn=true migran automaticamente a DefaultFrameIndex=1 via OnValidate

### Cambiado
- **Generadores VRChat**: FX Controller, Parameters y Menu ahora usan GetDefaultParameterValue() para calcular valores por defecto correctos en todos los tipos de animacion (OnOff, AB, Linear)
- **DefaultStateIsOn marcado [Obsolete]**: Ahora delega internamente a DefaultFrameIndex. Codigo existente sigue funcionando

## [0.9.65] - 2026-02-27

### Agregado
- **Vinculacion automatica de contenedores PhysBone a frames**: Los contenedores PhysBone (tanto creados por el sistema como pre-organizados por el creador de ropa) ahora se vinculan automaticamente a los frames correspondientes. Esto garantiza que al togglear ropa en VRChat, los PhysBones y Colliders se apaguen/enciendan junto con la prenda
- **Metodo centralizado LinkContainersToFrames**: Nueva logica en PhysBoneFrameLinker que encapsula la vinculacion con deduplicacion, usada desde el flujo automatico, el inspector y NDMF
- **Red de seguridad NDMF**: MROrganizaPBPlugin ahora vincula contenedores a frames durante el build como respaldo

### Corregido
- **Bug: contenedores pre-organizados desaparecian tras Organizar**: GetAllContainersWithContext() en estado Organized solo retornaba contenedores creados por el Relocator, perdiendo los pre-existentes (ej: VRCPBC del creador de ropa). Ahora combina ambas fuentes con deduplicacion

### Cambiado
- **MROrganizaPBEditor refactorizado**: La logica de vinculacion ahora delega al metodo centralizado en PhysBoneFrameLinker, manteniendo Undo y SetDirty en la capa editor

## [0.9.64] - 2026-02-26

### Cambiado
- **Auto-vinculacion de slots en MRAgruparMateriales**: Los slots ahora se vinculan automaticamente al crear grupos por drag-drop o al añadir materiales a grupos existentes. Ya no es necesario presionar un boton manual
- **Carpetas clickeables en Estructura de Carpetas**: Click en el nombre de una carpeta o carpeta padre hace ping en el Project (resalta sin navegar)

### Eliminado
- **Region Vinculacion Automatica**: Eliminados los botones "Detectar Vinculos" y "Desvincular Todos" — la vinculacion ahora es implicita en todos los flujos
- **Localizacion**: Eliminadas 4 claves (autoLinking, detectLinks, unlinkAll, detectLinksHint) en 6 idiomas
- **Metodo UnlinkAllSlots**: Eliminado por no tener uso

## [0.9.63] - 2026-02-26

### Agregado
- **Seccion Estructura de Carpetas en MRAgruparMateriales**: Nueva seccion informativa al final del inspector que muestra automaticamente las carpetas donde viven los materiales de cada pieza. Incluye checkboxes para incluir/excluir carpetas de la busqueda de sugerencias, icono linked para carpetas con slots capturados, y cantidad de materiales por carpeta. Cache automatico que solo se recalcula al cambiar los slots
- **Localizacion**: 3 nuevas claves (folderInfo, folderInfoParent, folderInfoNoSlots) en 6 idiomas

### Eliminado
- **Seccion Carpetas a incluir**: Reemplazada por la nueva seccion Estructura de Carpetas que cumple la misma funcion de forma automatica

## [0.9.62] - 2026-02-26

### Corregido
- **Fix columna Base de blendshapes**: La columna "Base" leia el valor del mesh en vivo, causando que el preview contaminara los valores mostrados. Ahora almacena el valor base original al momento de añadir la referencia usando `_actualValue` + flag `_hasBaseValue`. Datos antiguos mantienen el comportamiento previo como fallback
- **Fix valor base al añadir blendshape**: `AddBlendshape` ahora captura automaticamente el valor base del mesh si no se proporciona explicitamente. La ventana de seleccion pasa `CurrentValue` como base al crear la referencia

### Cambiado
- **UI Frame reorganizada**: Eliminados botones redundantes en las 3 secciones del frame:
  - Eliminado boton "Recalcular Rutas" (objetos, materiales, blendshapes) — AutoUpdatePaths y OnValidate ya lo hacen automaticamente
  - Eliminado boton "Limpiar Invalidos" (materiales, blendshapes) — el boton X por fila ya cubre esta funcion
  - Eliminado boton ojo (objetos, materiales, blendshapes) — su funcion se movio al click en el nombre del renderer/objeto
  - Unificado texto "Eliminar Todos" en las 3 secciones
- **Blendshapes: nuevo boton + y ping en renderer**: Boton "+" antes del renderer abre la ventana de seleccion. Click en el nombre del renderer hace ping en Hierarchy sin cambiar seleccion
- **Materiales: renderer y base clickeables**: Click en renderer hace ping en Hierarchy. Click en material base hace ping en Project. Ambos sin cambiar seleccion
- **Boton Previsualizar este Frame**: Ahora azul y mas grande para mayor visibilidad. Verde cuando preview activo
- **Boton Duplicar Frame**: Ahora color naranja

## [0.9.61] - 2026-02-25

### Corregido
- **Fix colision nombres animacion UnificarMateriales**: Los dos MRUnificarMateriales (Color Outfits y Color Pelucas) generaban ambos `UnifyMaterial_lin.anim`, sobreescribiendose. Ahora cada uno recibe su AnimationName al crearse (misma solucion que v0.8.48 para UnificarObjetos)
- **Fix preventivo IluminacionRadial**: CreateIluminacionRadial ahora tambien asigna AnimationName por consistencia

### Agregado
- **Red de seguridad anti-colision de AnimationNames**: Nuevo metodo `EnsureUniqueAnimationNames` en MRSlotInfoCollector que detecta AnimationNames duplicados antes de generar animaciones y los resuelve automaticamente usando el nombre del GameObject. Protege tanto el flujo del boton "Generar" como el build NDMF
- **Reordenamiento automatico de hijos del MenuControl**: Los componentes se ordenan logicamente: Outfits, Color Outfits, Pelucas, Color Pelucas, Iluminacion. Se reordenan tanto los GameObjects hijos como los AnimationSlots del inspector. Aplica en Generate() y SyncStructure()

### Cambiado
- **Renombrar Materiales a Color**: "Materiales Outfits" → "Color Outfits", "Materiales Pelucas" → "Color Pelucas"

## [0.9.59] - 2026-02-25

### Cambiado
- **Reordenar inspector MRMenuRadial**: Botones de accion y panel de estado ahora estan justo debajo del avatar. Configuracion avanzada (ruta de salida, integracion de menu, NDMF) al final
- **Unificar seccion de salida**: Ruta de salida y Namespace del Avatar fusionados en una sola region del inspector
- **Colores de botones en inspectors**: Botones de accion principales coloreados para mejor identificacion visual:
  - MRMenuRadial: Generate VRChat Files (verde), Sync Structure (azul)
  - MROrganizaPB: Escanear (azul), Organizar (rojo)
  - MRAjustarBounds: Escanear (azul), Aplicar Bounds (rojo)
  - MRIluminacionRadial: Generar animacion (verde)
  - MRUnificarObjetos: Crear Agrupar Objetos (naranja), Generar Animaciones (verde)
  - MRUnificarMateriales: Crear Agrupar Materiales (naranja), Generar Animaciones (verde)

## [0.9.58] - 2026-02-25

### Agregado
- **Materiales Pelucas**: Nuevo MRUnificarMateriales "Materiales Pelucas" que agrupa materiales alternativos de pelucas, espejando la simetria del sistema de radiales (Outfits/Pelucas). Se crea automaticamente cuando hay pelucas detectadas

### Cambiado
- **Materiales renombrado a Materiales Outfits**: El MRUnificarMateriales existente pasa de llamarse "Materiales" a "Materiales Outfits" para distinguirlo del nuevo "Materiales Pelucas"
- **SyncStructure identifica materiales por nombre**: Ya no toma el primer MRUnificarMateriales ciegamente — distingue "Materiales Pelucas" vs "Materiales Outfits" por nombre del GameObject
- **Mensajes de resultado actualizados**: Generate() y SyncStructure() incluyen conteos separados para materiales de outfits y pelucas

## [0.9.57] - 2026-02-24

### Agregado
- **Auto-deteccion de avatar**: MRMenuRadial y todos los componentes hijos (MRAjustarBounds, MRPesoTexturas, MRAnalisisColision, MROrganizaPB, MRCoserRopa) detectan automaticamente el avatar desde la jerarquia padre, sin necesidad de asignacion manual
- **Validacion de BoneProxy en MRAnalisisColision**: Detecta BoneProxy apuntando a Head con destino invalido y lo marca como problematico con detalle del error. Muestra el destino del BoneProxy en la lista de componentes
- **Inferencia de StitchZone desde MA**: MRCoserRopa infiere la zona de cosido automaticamente a partir de la info de target de Modular Avatar (MergeArmature/BoneProxy)
- **Deteccion de BoneProxy misplaced en piezas**: PieceEntry detecta cuando BoneProxy esta en la raiz de la pieza en vez del Armature hijo y lo marca para correccion

### Corregido
- **Duplicacion de Menu Control eliminada**: Menu Control ahora se crea desde una unica ruta (GetOrCreateMenuControl en AutoMenuGenerator). RecreateChildComponents ya no crea Menu Control directamente, evitando duplicados
- **Generacion automatica de estructura**: Al crear MRMenuRadial (via Creator menu o Add Component), el editor ejecuta PropagateAvatarToChildren automaticamente para generar la estructura dentro de Menu Control. Antes solo funcionaba al cambiar avatar manualmente
- **OnValidate no crea GameObjects**: ValidateInEditor solo auto-detecta avatar con asignacion directa al campo, sin disparar cadenas de creacion que causaban duplicados en domain reloads

### Mejorado
- **UI MRCoserRopa**: Seccion NDMF colapsada por defecto, boton Detectar mas grande y prominente, botones de accion reorganizados
- **UI MRAnalisisColision**: Info adicional de BoneProxy (destino) visible en la lista de componentes con colores segun categoria

## [0.9.56] - 2026-02-23

### Agregado
- **Deteccion de BoneProxy mal ubicado**: MRCoserRopa detecta automaticamente cuando MA BoneProxy esta en la raiz de una pieza en vez de en el Armature hijo. Muestra "BP!" en amarillo en la lista de piezas y un warning explicativo en los detalles
- **Boton Fix para reubicar BoneProxy**: Nueva columna "Fix" en la lista de piezas que permite mover BoneProxy al Armature hijo con un clic. La operacion copia boneReference, subPath y attachmentMode, y es reversible con Ctrl+Z
- **3 metodos en ModularAvatarDetector**: IsBoneProxyOnPieceRoot() detecta mala ubicacion, RelocateBoneProxy() mueve el componente conservando propiedades, GetBoneProxyRelocationCandidates() lista hijos validos
- **Localizacion en 6 idiomas**: Textos de warning y boton de reubicacion traducidos a es, en, zh, ja, ko, ru

## [0.9.55] - 2026-02-22

### Corregido
- **Localizacion robusta para VPM**: Resources.Load() podia fallar en primer import (race condition) y marcaba el sistema como inicializado con diccionario vacio, dejando todos los textos como `[key]`. Ahora solo marca inicializado si cargo traducciones, y reintenta hasta 5 veces en llamadas posteriores
- **Iconos visibles en instalacion VPM**: Los 12 componentes usaban ruta `Assets/Bender_Dios/MenuRadial/...` para el icono, que no existe cuando el paquete esta en `Packages/`. Cambiado a `Packages/com.bender-dios.menu-radial/...` para distribucion correcta

## [0.9.54] - 2026-02-22

### Cambiado
- **Renombrar ClothingEntry a PieceEntry**: Toda la terminologia interna cambia de "clothing/ropa" a "piece/pieza" para reflejar que el sistema maneja ropa, pelucas y accesorios. Nuevo enum PieceType (Ropa, Pelo, Pieza) para clasificacion explicita
- **Clasificacion inteligente de piezas**: DeterminePieceType() usa multiples senales para clasificar automaticamente: zona de cosido, WigDetector, MA BoneProxy, patrones de nombre y analisis de bone weights. Items con zona de cuerpo (FullBody, Torso, Chest, Hip, etc.) se clasifican como Ropa; zona Head con senales de pelo como Pelo; zona Head/None sin senales como Pieza
- **Analisis de bone weights para pelucas**: Cuando una peluca tiene MergeArmature (esqueleto completo, zona FullBody), BoneWeightAnalyzer verifica si TODOS los meshes pesan en huesos de la cabeza. Si es asi, se clasifica como Pelo aunque tenga zona FullBody. Esto distingue pelucas reales de ropa con accesorios de cabeza
- **MATargetInfo corregido para MergeArmature**: Cuando MergeArmature existe pero su target es vacio, ahora muestra "Armature" en vez de caer al fallback de BoneProxy que mostraba "Head" erroneamente
- **Etiquetas numeradas en lista de piezas**: La columna de tipo ahora muestra "Ropa 1", "Pelo 2", "Pieza 1" con conteo por tipo. Usa GenericMenu con colores por tipo
- **Zona de cosido editable**: La columna de zona en la lista de piezas ahora es un dropdown con opciones (Auto) + 12 zonas. Al cambiar zona se reclasifica el tipo automaticamente
- **Destruccion de componentes en AnalisisColision**: entry.Destroy() reemplaza entry.Disable() — MA no puede encontrar componentes destruidos con GetComponentsInChildren(true)
- **MA Mesh Settings auto-eliminado**: MRAjustarBounds destruye automaticamente MA Mesh Settings al detectarlo

### Agregado
- **PieceEntry.cs**: Nuevo modelo que reemplaza ClothingEntry con campos para PieceType, IsWig, AllMeshesHeadWeighted, HasManualStitchZone, ManualStitchZone y PieceTypeManuallySet
- **PieceType.cs**: Enum con valores Ropa, Pelo, Pieza para clasificacion explicita de piezas
- **25 campos de localizacion**: pieceTypeLabel/Ropa/Pelo/Pieza, disableMAToggle/WigWarning/MRWillProcess, manualZoneLabel/Auto/Active, ndmfDisableMAInfo/NoPiecesEnabled, zonas faltantes (RightHand, LeftHand, RightFoot, LeftFoot, Chest, None + variantes cortas)
- **StitchZone extendido**: Nuevas zonas RightHand, LeftHand, RightFoot, LeftFoot, Chest, None para clasificacion mas granular

### Corregido
- **Pelucas con MergeArmature mal clasificadas**: Items con esqueleto completo (FullBody) ya no se clasifican automaticamente como Ropa — se verifica bone weights y nombre antes de descartar la clasificacion de WigDetector
- **Zonas Chest/Hip/UpperLimb clasificadas como Pieza**: Ahora se clasifican correctamente como Ropa (IsBodyZone incluye todas las zonas corporales)
- **Falsos positivos de WigDetector**: Ropa con accesorios de cabeza que pasaba WigDetector ahora se filtra por bone weights (meshes del cuerpo no pasan el threshold de 60% head weight)

## [0.8.54] - 2026-02-21

### Corregido
- **Revertir MA Bone Proxy a UserDecision**: Destruir BoneProxy en raiz causaba que pelucas y accesorios sin huesos humanoid desaparecieran de MRCoserRopa. BoneProxy es el mecanismo de union de estas piezas y no debe tocarse. Vuelve a ser decision del usuario

## [0.8.53] - 2026-02-21

### Cambiado
- **MA Mesh Settings se elimina automaticamente**: MR Ajustar Bounds ahora destruye todos los componentes MA Mesh Settings al detectarlos (al abrir el inspector, escanear, o durante build NDMF). MA Mesh Settings interfiere con el calculo de bounds unificados. La eliminacion soporta Undo (Ctrl+Z)
- **Componentes problematicos ahora se destruyen**: MR Analisis Colision ahora destruye (en vez de solo desactivar) los componentes problematicos y los marcados por el usuario, tanto en Edit Mode como en build NDMF. Componentes desactivados podian ser encontrados por MA via GetComponentsInChildren(true)

## [0.8.52] - 2026-02-21

### Corregido
- **Revertido v0.8.51 — PhysBones agrupados incorrectamente**: La integracion de WigDetector en GetKnownArmatures() causaba que todos los PhysBones se agruparan en un solo contenedor. Al pasar null como clothings a WigDetector, este re-escaneaba todos los hijos del avatar sin filtrar ropas ya detectadas, creando contextos duplicados. El mecanismo de pseudo-armature (Root.transform) tambien causaba colisiones en la creacion de contenedores. Se revierte completamente a la logica original de v0.8.50

## [0.8.51] - 2026-02-21 [REVERTIDO]

### Corregido
- ~~PhysBones de pelucas no se vinculan a frames~~
- ~~Pelucas sin armature (MA BoneProxy)~~

## [0.8.50] - 2026-02-21

### Cambiado
- **Duplicacion de frames con numeracion secuencial**: Al duplicar un MRAgruparObjetos ahora se usa formato "NombreBase 01", "NombreBase 02", etc. en vez de "(Copy)". El conteo es global — duplicar cualquier clon siempre genera el siguiente numero disponible
- **Boton duplicar en MRAgruparObjetos**: Cuando un frame esta enlazado a un MRUnificarObjetos, aparece un boton "Duplicar frame" directamente en su inspector, permitiendo duplicar desde ambos componentes
- **Compatibilidad con nombres legacy**: Los nombres con sufijo "(Copy)" se reconocen y el nombre base se extrae correctamente para continuar la numeracion secuencial

## [0.8.49] - 2026-02-21

### Agregado
- **StitchZone**: Nuevo enum que clasifica zonas de cosido (FullBody, Torso, Head, UpperLimb, LowerLimb, Hip) basado en los HumanBodyBones detectados en bone mappings
- **ClothingEntry.DetermineStitchZone()**: Metodo estatico que analiza bone mappings para auto-clasificar la zona corporal de cada prenda
- **ClothingEntry.IsWig**: Campo para marcar prendas identificadas como pelucas por WigDetector
- **TextureGroupType.Wig**: Nuevo tipo de grupo de texturas para separar pelucas del Avatar Base en MRPesoTexturas
- **MRPesoTexturas.IncludeWigs**: Toggle para incluir/excluir texturas de pelucas en el escaneo
- **ScanWigs()**: MRPesoTexturasEditor ahora escanea pelucas via WigDetector y crea grupos separados, excluyendolas del Avatar Base y ropas
- **Zona en UI de CoserRopa**: La lista de ropas muestra la zona de cosido y las pelucas se resaltan en magenta
- **Localizacion**: 17 nuevas keys en 6 idiomas para zonas de cosido y pelucas en PesoTexturas

### Corregido
- **Pelucas con MA BoneProxy no detectadas**: La deteccion de MA ahora se ejecuta ANTES del chequeo de huesos humanoid, permitiendo que pelucas con huesos no-humanoid (hair_001, etc.) pasen si tienen MA configurado
- **Solo MergeArmature marcaba HasModularAvatar**: Ahora cualquier componente MA (incluyendo BoneProxy) marca la prenda como gestionada por MA
- **Segundo pase de deteccion MA**: Nuevo metodo DetectMAFilteredChildren() detecta hijos directos del avatar con MA cuyos huesos SMR apuntan al armature del avatar (filtrados por el loop principal)
- **Umbral de huesos humanoid**: Reducido de 3 a 1 para detectar pelucas y accesorios con pocos huesos

## [0.8.48] - 2026-02-20

### Corregido
- **Colision de nombres de animacion**: AutoMenuGenerator ahora asigna AnimationName basado en el nombre del componente ("Outfits", "Pelucas") en vez de usar el default "RadialToggle" para todos los radiales
- **Auto-resolucion de AnimationName duplicados**: SlotNameConflictValidator resuelve conflictos de AnimationName agregando sufijos numericos (_1, _2) ademas de los conflictos de slotName

### Cambiado
- **IAnimationProvider.AnimationName**: Ahora incluye setter en la interfaz para permitir renombrar AnimationName desde el auto-resolver de conflictos
- **MRAgruparObjetos y MRMenuControl**: AnimationName tiene setter no-op (nombre derivado de otros campos)

## [0.8.47] - 2026-02-20

### Agregado
- **BoneWeightAnalyzer**: Nuevo analizador de pesos de huesos que determina si un mesh tiene su geometria influenciada por huesos de la cabeza (pelo) o del cuerpo (ropa). Umbral: 60% peso en head bones = pelo
- **Deteccion de pelo del avatar base** (Fuente 3): Los meshes hermanos del armature excluidos por BodyMeshDetector como "pelo" ahora se detectan y agregan al radial "Pelucas"
- **Señal 8 de scoring**: Bone weight analysis como nueva señal en WigDetector (+2 para ropa/hijos, +4 para pelo de avatar)
- **Clasificacion hibrida de pelucas**: Ropas que contienen tanto meshes de pelo como de ropa se separan: meshes de pelo van al radial "Pelucas", meshes de ropa van al radial "Outfits"
- **BodyMeshDetector.GetHairExcludedMeshes()**: Expone meshes excluidos por patron de pelo para WigDetector Fuente 3

### Corregido
- **WigDetector Fuente 1**: Ahora usa BoneWeightAnalyzer para seleccionar solo meshes head-weighted del candidato a peluca, no todos los meshes de la ropa
- **Agrupacion de pelo de avatar**: Multiples meshes de pelo del avatar base se agrupan en UN solo frame en vez de crear uno por mesh

### Eliminado
- **BodyMeshDetector.MIN_BONE_WEIGHT**: Constante no usada eliminada

## [0.8.46] - 2026-02-19

### Agregado
- **Auto-deteccion de pelucas**: Las pelucas se detectan automaticamente y se crean en un radial "Pelucas" separado del radial "Outfits"
- **WigDetector**: Nuevo sistema de scoring multi-señal (threshold=7) que identifica pelucas por: ausencia de huesos de extremidades (+3), presencia de hueso Head (+2), meshes con nombre de pelo (+3), contenedor con nombre de pelo (+2), MA BoneProxy apuntando a Head (+2), Head con ≥3 hijos (+2), ≤5 huesos humanoides (+1)
- **Dos fuentes de deteccion**: Reclasifica ClothingEntries que parecen pelucas + escanea hijos del avatar no detectados como ropa
- **Sincronizar pelucas**: El boton Sincronizar detecta pelucas nuevas y las agrega al radial "Pelucas" sin tocar "Outfits"
- **Dialogos informativos**: Generar, Regenerar y Sincronizar muestran cuantas pelucas fueron detectadas cuando corresponde
- **Localizacion**: 3 nuevas keys en 6 idiomas para mensajes de pelucas

### Sin cambios
- Sin pelucas detectadas: comportamiento 100% identico al anterior
- MRCoserRopa, ClothingEntry, ArmatureFinder y BodyMeshDetector no fueron modificados

## [0.8.45] - 2026-02-18

### Agregado
- **Auto-vincular contenedores PhysBone a frames**: Al Escanear u Organizar PhysBones, los contenedores (VRCPB, VRCPBC) se vinculan automaticamente al frame (MRAgruparObjetos) correspondiente de cada ropa/avatar, para que se activen/desactiven junto con el outfit
- **Deteccion de contenedores pre-existentes**: PhysBones ya organizados por el usuario o creador se detectan y vinculan a sus frames sin necesidad de reorganizar
- **Deduplicacion**: Vincular multiples veces no duplica referencias en los frames
- **PhysBoneFrameLinker**: Nueva clase helper que detecta contenedores existentes y busca frames por nombre de contexto
- **PhysBoneRelocator.GetContainersWithContext()**: Expone contenedores con su contexto de organizacion
- **MROrganizaPB.GetAllContainersWithContext()**: Wrapper que unifica contenedores creados y pre-existentes
- **Localizacion**: 3 nuevas keys en 6 idiomas para mensajes de vinculacion de contenedores

## [0.8.44] - 2026-02-17

### Corregido
- **Compatibilidad MA ShapeChanger**: Los componentes `ModularAvatarShapeChanger` dejaban de funcionar al subir el avatar porque MR mezclaba su FX controller DESPUES de Modular Avatar — MA no podia detectar los toggles de MR y ShapeChanger no se conectaba a ellos
- **Orden de ejecucion NDMF**: MRMenuRadialPlugin ahora mezcla FX/Parameters/Menu ANTES de Modular Avatar para que MA detecte los toggles y conecte ShapeChanger correctamente
- **Bounds exagerados tras fix ShapeChanger**: Separado MRMenuRadialPlugin en dos passes (merge BeforeMA + cleanup AfterMA) para que MRAjustarBoundsPlugin pueda acceder a su componente antes de la limpieza
- **AnalisisColision destruia ShapeChanger**: Removido `ShapeChanger` de las listas `CRITICAL_COMPONENTS` y `MA_PROBLEMATIC_ON_ROOT` — no causa conflictos reales con MR y destruirlo rompia funcionalidad del avatar
- **AnalisisColision doble destruccion**: `GetProblematicOnClothingRoot()` ahora respeta el checkbox del usuario; `GetProblematicToDisable()` excluye entradas en raiz de ropa para evitar destruccion duplicada que anulaba el toggle `AutoDisableProblematicOnRoot`

## [0.8.43] - 2026-02-17

### Corregido
- **AjustarBounds: Bounds incorrectos en VRChat**: MeshRetargeter sobreescribia rootBone y localBounds durante el cosido de ropa en NDMF, causando bounds exagerados al subir el avatar
- **AjustarBounds: Bounds excedian limite VRChat**: Bounds unificados y de meshes sin huesos ahora se limitan a Very Poor - 1cm (4.99x5.99x4.99m) en lugar de pasar de largo

### Agregado
- **Plugin NDMF MRAjustarBounds**: Re-agregado plugin que recalcula bounds frescos DESPUES de MRCoserRopa y Modular Avatar, garantizando bounds correctos en el avatar final
- **Clamp VRChat para meshes sin huesos**: Meshes sin bones que excedan limites VRChat se limitan automaticamente al aplicar bounds
- **UI: Estado "Limitado"**: Meshes sin huesos que fueron limitados muestran estado "Limitado" en la lista y resumen con conteo separado

### Mejorado
- **Botones fusionados**: "Escanear" y "Calcular" unificados en un solo boton "Escanear" que ejecuta ambos pasos, eliminando el estado intermedio confuso

## [0.8.42] - 2026-02-16

### Corregido
- **AjustarBounds: Rediseno con BakeMesh**: Reemplazado el calculo de bounds basado en Encapsulate por el enfoque de WhiteFlare AvatarTools — usa `BakeMesh()` para obtener geometria real deformada y transforma vertices al espacio del rootBone compartido (Hips), produciendo bounds precisos en lugar de volumenes inflados de ~100m
- **AjustarBounds: Datos stale al re-escanear**: `ScanAvatar()` capturaba bounds ya aplicados como "originales" — ahora restaura bounds antes de re-escanear
- **AjustarBounds: Wireframe Scene View desalineado**: `OnSceneGUI` usaba la matriz del avatar root pero los bounds estan en espacio del rootBone (Hips)
- **PrepareAll no calculaba bounds**: Faltaba llamada a `CalculateBounds()` entre scan y apply en `MRMenuRadial.PrepareAll()`

### Mejorado
- **AjustarBounds UI**: Meshes sin huesos muestran "Sin huesos" en amarillo (antes se ignoraban silenciosamente), resumen de estado muestra conteo de meshes saltados, nombre del rootBone visible en resultados
- **Undo completo**: Apply/Restore ahora registran Undo tanto para el Renderer como para su Transform (se resetea a identity al aplicar)

### Eliminado
- **MRAjustarBoundsPlugin.cs**: Eliminado plugin NDMF de AjustarBounds — los bounds se aplican en editor via boton "Aplicar", no requieren build

## [0.8.41] - 2026-02-15

### Corregido
- **AnalisisColision mostraba conflictos resueltos**: Los conteos del resumen (lucecitas roja/naranja/verde) no filtraban componentes ya destruidos, mostrando errores fantasma tras resolver conflictos

### Agregado
- **Re-escaneo de AnalisisColision al Sincronizar**: El boton Sincronizar en MRMenuRadial ahora re-escanea componentes MA para reflejar cambios

## [0.8.40] - 2026-02-15

### Agregado
- **Refresh de AjustarBounds al Sincronizar**: Al presionar Sincronizar en MRMenuRadial, refresca bounds y anchor override automaticamente para cubrir meshes nuevos (ropa, pelucas, accesorios) sin requerir intervencion manual en el componente AjustarBounds

## [0.8.39] - 2026-02-15

### Corregido
- **Bounds unificados solo cubrian el primer mesh**: `Bounds.Encapsulate()` operaba sobre copia del struct (Nullable.Value retorna copia), causando que los bounds calculados ignoraran todos los meshes excepto el primero
- **Refresh de MeshBoundsInfo no actualizaba probe anchor**: `Refresh()` ahora recaptura `probeAnchor` y `hadOriginalProbeAnchor` del renderer

## [0.8.38] - 2026-02-14

### Corregido
- **Cache de armatures nunca se invalidaba**: `ClearDetection()` no reseteaba el cache interno del scanner, causando que re-escaneos usaran armatures obsoletas y PhysBones no fueran detectados
- **ScanPhysBones siempre re-detecta armatures**: Eliminado el guard condicional que saltaba la deteccion si ya existia cache, garantizando armatures frescas en cada ciclo de escaneo

## [0.8.37] - 2026-02-14

### Corregido
- **Deteccion de PhysBones en ropas no reconocidas**: PhysBoneScanner ahora recibe armatures detectadas por MRCoserRopa como fuente adicional, evitando que ropas con estructura no estandar pierdan sus PhysBones
- **Fallback al contexto del avatar**: PhysBones y Colliders que no pertenecen a ninguna armature detectada se asignan al contexto del avatar en lugar de descartarse silenciosamente

### Agregado
- Struct `PhysBoneScanner.KnownArmature` para recibir armatures externas
- Metodo `MROrganizaPB.GetKnownArmatures()` que extrae armatures de MRCoserRopa
- Helper `PhysBoneScanner.FindAvatarContext()` para fallback

## [0.8.36] - 2026-02-14

### Agregado
- **Boton Sincronizar estructura**: Detecta ropas nuevas y las agrega a la estructura de menu sin modificar lo existente
  - Nuevo metodo `SyncStructure()` en AutoMenuGenerator con diff incremental (frames + materiales)
  - Nuevo metodo `SyncMenuStructure()` en MRMenuRadial como wrapper publico
  - Boton "Sincronizar" en el inspector entre Generar/Regenerar y Generar Archivos VRChat
  - Re-detecta ropas automaticamente al sincronizar (no requiere "Preparar Todo" previo)
  - Reporta frames huerfanos (ropas eliminadas) sin borrarlos
  - Soporte completo de Undo (Ctrl+Z)
  - Localizado en 6 idiomas (es, en, zh, ja, ko, ru)

## [0.8.35] - 2026-02-14

### Corregido
- **Repaint global al cambiar idioma**: `SetLocale()` y `ReloadTranslations()` ahora fuerzan repaint de todos los inspectores abiertos, no solo la ventana de localización

### Eliminado
- **Secciones de debug en MRLocalizationWindow**: Eliminadas secciones "Probar Traducción" y "Acciones" (recargar/abrir carpeta/resetear)
- **9 claves muertas**: Eliminadas de MRLocalizationKeys.cs, MRLocalization.cs y los 6 archivos JSON

## [0.8.34] - 2026-02-14

### Agregado
- **Icono MR en inspector**: Logo MR visible en los 12 componentes del proyecto via atributo `[Icon]`
- **Landing page VPM**: Página web en GitHub Pages con tema oscuro estilo VRChat (emerytheec.github.io/vpm-listing)

## [0.8.33] - 2026-02-13

### Agregado
- **Localización completa**: Sistema de 6 idiomas (es, en, zh, ja, ko, ru)
  - 5 archivos JSON de idioma nuevos en `Localization/Resources/Locales/`
  - ~680 líneas por archivo, ~300 keys organizadas en 16 secciones
  - MRLocalization.cs: 6 nuevas secciones (menuRadial, organizaPB, analisisColision, pesoTexturas, localizationWindow, circularMenu)
  - MRLocalizationKeys.cs: 6 nuevas clases de keys + keys adicionales en clases existentes
- **13 editores localizados**: ~206 llamadas `MRLocalization.Get()` reemplazando strings hardcoded
  - MRMenuRadialEditor (84), MRPesoTexturasEditor (64), MRAnalisisColisionEditor (58)
  - MRCoserRopaEditor, MROrganizaPBEditor, MRLocalizationWindow
  - ObjectListEditor, MaterialListEditor, BlendshapeListEditor, BlendshapeSelectionWindow
  - MRUnificarObjetosUIRenderer, MRUnificarObjetosReorderableController, CircularLinearMenuWindow

### Mejorado
- **es.json**: Reescrito con todas las secciones nuevas y correcciones de valores para coserRopa y organizaPB

## [0.8.32] - 2026-02-13

### Eliminado
- **FramePreviewStrategy.cs**: Archivo completo eliminado (191 líneas, 0 referencias externas)
- **IFrameData.cs**: Interfaz eliminada (75 líneas, nunca usada como tipo polimórfico)
- **FrameData.cs**: Eliminados 9 métodos muertos (AddObjectReference, RemoveObjectReference, ClearObjectReferences, SetAllReferencesActive, ClearMaterialReferences, RestoreAllOriginalMaterials, AddBlendshapeReference, RemoveBlendshapeReference, ClearBlendshapeReferences) — 323→227 líneas
- **FrameBlendshapeController.cs**: Eliminados 5 métodos query muertos (FindBlendshapeReference, ContainsBlendshape, GetBlendshapesByRenderer, GetActiveBlendshapes, GetInactiveBlendshapes) — 383→208 líneas
- **FrameMaterialController.cs**: Eliminados 4 métodos query muertos (FindMaterialReference, ContainsMaterial, GetMaterialsWithAlternatives, GetMaterialsWithoutAlternatives) — 316→175 líneas
- **FrameObjectController.cs**: Eliminados 4 métodos query muertos (FindObjectReference, ContainsObject, GetActiveObjects, GetInactiveObjects) — 329→142 líneas
- **PreviewManager.cs**: Eliminado RegisterComponentIfNeeded redundante (RegisterComponent ya verifica duplicados)
- **HierarchyPathHelper.cs**: Eliminado IsValidPath() sin callers
- **MRUnificarObjetosEditor.cs**: Eliminado ValidateSerializedProperties() vacío + su caller
- **ReferenceListManager.cs**, **MRUnificarObjetosPreviewManager.cs**, **DynamicIconManager.cs**, **CircularLinearMenuWindow.cs**: Eliminados empty if blocks y variables contadoras sin usar
- Simplificadas funciones RemoveInvalid usando `RemoveAll()` directo
- **Total**: ~1000 líneas de código muerto eliminadas en 15 archivos

## [0.8.31] - 2026-02-13

### Mejorado
- **UI**: Eliminados corchetes `[]` de nombres y etiquetas en 12 archivos de 7+ componentes
  - Tipos de animación: `[Radial]` → `(Radial)`, `[Illumination]` → `(Illumination)`
  - Índices de material: `[0]` → `#0`
  - Fallbacks: `[Missing]` → `(Sin Renderer)`, `[None]` → `(Ninguno)`
  - Prefijos de estado: `[MA]` → `MA:`, `[MR]` → `MR:`
  - Paths: `[Missing Transform]` → `(Sin Transform)`
  - Referencias: `[Missing Reference]` → `(Sin Referencia)`

## [0.8.30] - 2026-02-13

### Corregido
- **MROrganizaPB**: 13 bugs corregidos en el sistema de organización de PhysBones
  - Filtrado por armature: solo detecta componentes dentro de armatures usando ArmatureFinder
  - Preserva estado null de rootTransform (HadExplicitRootTransform flag)
  - Ya no auto-organiza desde PrepareAll sin consentimiento del usuario
  - Contenedores renombrados a VRCPB/VRCPBC (nombres cortos, claros)
  - CopyComponentSimple usa reflexión en lugar de portapapeles (ComponentUtility)
  - Undo integrado en PhysBoneRelocator (SafeDestroyImmediate, RegisterCreatedObjectUndo)
  - Revert ya no destruye contenedores vacíos
  - Referencias de colliders actualizadas para PhysBones excluidos del escaneo
  - Reutilización segura de contenedores existentes (verifica contenido antes de reusar)
  - Logging reducido a resúmenes (eliminados logs por componente individual)
- **PhysBoneScanner**: Corregido fake null de Unity en rootTransform (UnassignedReferenceException)

### Mejorado
- **MROrganizaPBEditor**: UI rediseñada con cards agrupadas por contexto (avatar, ropas, pelucas)
  - Cada contexto tiene su propia tarjeta con foldout, contadores PB/Col, y toggle grupal
  - Click en nombre del componente hace ping en Hierarchy
  - Componentes ya organizados (fuera del armature) se muestran con checkmark verde
  - Grupos colapsados por defecto
- **MROrganizaPBPlugin**: NDMF auto-organiza durante build como red de seguridad (OrganizeForBuild sin Undo)

### Eliminado
- **ContextDetector.cs**: Eliminado, lógica reemplazada por ArmatureFinder en PhysBoneScanner

## [0.8.29] - 2026-02-11

### Eliminado
- **ListEditorBase.cs**: Eliminada clase abstracta genérica sin subclases (~322 líneas)
- **RadialMenuValidator.cs**: Eliminada clase nunca instanciada (~405 líneas)
- **RadialGeometryCalculator**: Eliminados métodos muertos `CalculateArcPoints` y `CalculateMouseAngle` (0 callers)
- **MRMenuControl**: Eliminado método vacío `RecalculatePaths()` (0 callers)

### Mejorado
- **AnimationBuilders**: Consolidado `FindAvatarRoot()` duplicado — ahora compartido como `internal` en RadialAnimationBuilder
- **AnimationBuilders**: Consolidado `SaveAnimation` duplicado — extraída lógica común en `SaveAnimationClip` compartido

## [0.8.28] - 2026-02-11

### Eliminado
- **9 archivos muertos eliminados** (~1870 líneas): FrameDataFactory, FrameDataValidator, RadialDataStore, RadialMenuValidator, RadialMenuDataStore, RadialInteractionHandler, PreviewServiceCoordinator, AssetValidationResult, FrameSegmentCalculator
- **5 archivos de Radial eliminados** (~1250 líneas): RadialFrameManager, RadialMenuState, RadialPreviewManager, RadialPropertyManager, UnifiedPreviewStrategy — lógica inlineada en MRUnificarObjetos
- **2 interfaces eliminadas**: IBoneMapper e IStitchingController (implementación única, nunca usadas polimórficamente)
- **MRConstants.cs**: Eliminadas 17 constantes muertas (~205 líneas), conservadas solo las 5 en uso
- **PreviewManager.cs**: Simplificado de 465 a 195 líneas — eliminados 3 eventos sin suscriptores, infraestructura de limpieza de eventos, y 6 propiedades/métodos muertos
- **FrameData.cs**: Eliminados 18 métodos/propiedades muertos de conteo y acceso a ListManager (~168 líneas)
- **MRAgruparObjetos.cs**: Eliminados 13 métodos `[Obsolete]` tras migrar los 6 callers a la API actual
- **Métodos muertos en 10+ archivos**: ReferenceBase (2), BlendshapeReference (5), MaterialReference (1), HierarchyPathHelper (4), FramePreviewController (2), ReferenceListManager (5), DynamicIconManager (5), IlluminationMaterialController (4), RadialMenuStateManager (2), ObjectPool (2)
- **Sistema ComponentVersion**: Eliminado campo `_componentVersion` de MRComponentBase y llamadas `UpdateVersion()` de 5 componentes
- **MRSlotManager**: Eliminada asignación muerta `isValid = false` (el setter era no-op)

## [0.8.27] - 2026-02-11

### Eliminado
- **FrameManager.cs**: Eliminada clase muerta (247 líneas) — nunca instanciada, puro wrapper delegando a FrameData
- **RadialMenuState**: Eliminado método vacío `UpdateFrames()` y 5 callers en MRUnificarObjetos
- **MRAgruparObjetos**: Eliminados 3 stubs vacíos (`SelectNextFrame`, `SelectPreviousFrame`, `SelectFrameByIndex`) — no son de interfaz
- **IlluminationAnimationController**: Eliminados 2 bloques if vacíos sin lógica
- **MRAnimationSlot**: Eliminado setter vacío de `isValid` — convertido a expression-bodied property
- **ShaderStrategyFactory**: Eliminados métodos muertos `GetStrategy(ShaderType)` y `GetAllStrategies()`

### Mejorado
- **IFramePreviewStrategy.cs** → **FramePreviewStrategy.cs**: Renombrado archivo para coincidir con la clase que contiene
- **UnifiedPreviewStrategy**: Renombrado enum anidado `PreviewType` → `StrategyMode` para evitar conflicto con el enum global `PreviewType`

## [0.8.26] - 2026-02-11

### Corregido
- **MROrganizaPB Revert**: El revert ahora restaura correctamente las referencias de colliders en PhysBones (antes se perdían al revertir porque los PhysBones se revertían primero)
- **MROrganizaPB Revert**: El rootTransform original ahora se restaura correctamente al revertir (antes se ponía a null incondicionalmente, perdiendo rootTransforms explícitos)
- **MROrganizaPB**: La copia de componentes ahora preserva el estado `enabled` del Behaviour (antes componentes desactivados se reactivaban al organizar)
- **MROrganizaPB**: La copia por reflexión ahora itera toda la cadena de herencia de clases (antes solo copiaba campos del tipo más derivado, perdiendo campos de clases base del SDK)

### Mejorado
- **MROrganizaPB Editor**: Confirmación antes de organizar con diálogo mostrando cantidad de PhysBones y Colliders que serán movidos
- **MROrganizaPB Editor**: Undo funciona correctamente para GameObjects creados durante la organización (contenedores e hijos)
- **MROrganizaPB UI**: Renombrado "Todos/Ninguno" a "Incluir todos/Excluir todos" y "habilitados" a "incluidos" para evitar confusión con el estado `enabled` del componente

### Eliminado
- **PhysBoneRelocator**: Eliminados métodos muertos `CopyFieldsViaReflection()`, `SetRootTransform()` y `SafeGetName()`
- **PhysBoneRelocator**: Eliminados ~40 logs de diagnóstico intermedios (mantenidos solo logs de inicio, éxito y error)
- **ContextDetector**: Eliminadas entradas duplicadas en minúscula de `ArmatureNames` (la comparación ya usa `OrdinalIgnoreCase`)
- **PhysBoneEntry/ColliderEntry**: Extraída clase base `ComponentEntry` eliminando ~160 líneas de código duplicado

## [0.8.25] - 2026-02-10

### Eliminado
- **Código muerto**: 17 archivos eliminados (~4286 líneas) — AsyncUnityOperationsRuntime, MRFallbackManager, EventSubscriptionManager, ReferenceValidator, RadialImmutableCache, RadialObjectPools, RadialAnimationSettings, VRChatSettings, ReferenceListOperations, ReferenceListValidator, ValidationCacheHelper, FrameObjectEventSystem, IFrameControllerFactory, DefaultFrameControllerFactory, IReferenceController (4 interfaces), RadialUnityIntegration
- **Patrón Observer vacío**: Eliminado FrameObjectEventSystem (8 eventos estáticos sin suscriptores) y 8 métodos Raise* de MRAgruparObjetos
- **Patrón Factory innecesario**: Eliminado IFrameControllerFactory + DefaultFrameControllerFactory — reemplazado con `new` directo en MRAgruparObjetos
- **Interfaces sin polimorfismo**: Eliminado IReferenceController.cs con 4 interfaces (IObjectReferenceController, IMaterialReferenceController, IBlendshapeReferenceController, IReferenceController) — ninguna se usaba polimórficamente
- **RadialUnityIntegration**: Eliminado completamente (402 líneas) — sus 7 métodos públicos no tenían callers
- **RadialMenuState**: Simplificado de 182 a 84 líneas — eliminada integración con RadialUnityIntegration y método Initialize
- **RadialPreviewManager**: Eliminados 4 métodos muertos (ForceCleanupToSafeState, CanActivatePreview, GetCurrentNormalizedValue, GetCurrentToggleState)
- **RadialFrameManager**: Eliminado método muerto GetFrameDataList
- **IFramePreviewStrategy**: Eliminada interfaz con un solo implementador — renombrado DefaultFramePreviewStrategy a FramePreviewStrategy

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
