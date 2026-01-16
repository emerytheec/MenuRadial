# Changelog

Todos los cambios notables de este proyecto seran documentados en este archivo.

El formato esta basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

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
