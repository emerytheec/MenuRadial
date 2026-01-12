# Menu Radial (MR)

Sistema de menu radial para avatares VRChat desarrollado en Unity. Genera animaciones (.anim), controladores FX (AnimatorController), parametros (VRCExpressionParameters) y menus (VRCExpressionsMenu) para el sistema de expresiones de VRChat SDK3.

![Unity](https://img.shields.io/badge/Unity-2022.3-blue)
![VRChat SDK](https://img.shields.io/badge/VRChat%20SDK-3.5+-green)
![License](https://img.shields.io/badge/License-MIT-yellow)
![Version](https://img.shields.io/badge/Version-0.6.0-orange)

## Caracteristicas

- **MRAgruparObjetos**: Captura estados del avatar (objetos, materiales, blendshapes)
- **MRUnificarObjetos**: Agrupa frames y genera animaciones OnOff/AB/Linear
- **MRIluminacionRadial**: Control de iluminacion lilToon
- **MRUnificarMateriales**: Control unificado de materiales multiples
- **MRAgruparMateriales**: Agrupacion de materiales alternativos
- **MRMenuControl**: Orquesta todo y genera archivos VRChat finales
- **MRCoserRopa**: Sistema de cosido de ropa a avatares (NDMF)
- **MRAjustarBounds**: Ajuste de bounds de meshes y particulas

## Instalacion

### Opcion 1: VRChat Creator Companion (Recomendado)

1. Abre VRChat Creator Companion
2. Ve a Settings > Packages > Add Repository
3. Agrega la URL: `https://bender-dios.github.io/MenuRadial/index.json`
4. Busca "Menu Radial" en la lista de paquetes
5. Click en "Add" para instalarlo en tu proyecto

### Opcion 2: Instalacion Manual

1. Descarga la ultima release desde [GitHub Releases](https://github.com/Bender-Dios/MenuRadial/releases)
2. Importa el .unitypackage en tu proyecto Unity

## Requisitos

- Unity 2022.3.22f1 o superior
- VRChat SDK3 Avatars 3.5.0 o superior
- (Opcional) NDMF para funciones no-destructivas

## Uso Rapido

1. Agrega el componente `MRMenuControl` a tu avatar
2. Arrastra el avatar al campo correspondiente
3. Crea slots con `MRUnificarObjetos` para tus toggles
4. Usa `MRAgruparObjetos` para capturar estados
5. Click en "Generar Archivos VRChat"

## Componentes

| Componente | Proposito |
|------------|-----------|
| `MRAgruparObjetos` | Captura un estado del avatar |
| `MRUnificarObjetos` | Contenedor de frames, genera animaciones |
| `MRIluminacionRadial` | Control de iluminacion lilToon |
| `MRUnificarMateriales` | Control de materiales multiples |
| `MRAgruparMateriales` | Agrupa materiales alternativos |
| `MRMenuControl` | Genera archivos VRChat |
| `MRCoserRopa` | Cose armatures de ropa |
| `MRAjustarBounds` | Ajusta bounds de meshes |

## Tipos de Animacion

- **OnOff** (1 frame): Genera `nombre_on.anim` y `nombre_off.anim`
- **AB** (2 frames): Genera `nombre_A.anim` y `nombre_B.anim`
- **Linear** (3+ frames): Genera `nombre_lin.anim` (255 frames)

## Documentacion

Para documentacion detallada, consulta el archivo [CLAUDE.md](CLAUDE.md) incluido en el proyecto.

## Licencia

Este proyecto esta bajo la licencia MIT. Ver [LICENSE](LICENSE) para mas detalles.

## Autor

**Bender_Dios**

## Contribuciones

Las contribuciones son bienvenidas. Por favor, abre un issue primero para discutir los cambios que te gustaria hacer.

---

Hecho con :heart: para la comunidad de VRChat
