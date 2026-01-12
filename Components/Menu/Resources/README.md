# 🎨 Iconos del Sistema MR Control Menu

Este directorio contiene los iconos básicos para el sistema de menú radial.

## 📋 Iconos Requeridos

### Básicos (Obligatorios)
- **Bsx_gm_back.png** - Botón de regreso (posición 12)
- **Bsx_gm_Toggle.png** - Para animaciones ON/OFF y A/B  
- **Bsx_gm_Option.png** - Para submenús (otros MR Control Menu)
- **Bsx_gm_Radial.png** - Para animaciones lineales

### Adicionales (Opcionales)
- **Bsx_gm_Default.png** - Icono por defecto
- **Bsx_gm_Tools.png** - Para herramientas
- **Bsx_gm_Gear.png** - Para configuraciones

## 🔧 Especificaciones

- **Formato:** PNG con transparencia
- **Tamaño:** 64x64 píxeles (recomendado)
- **Estilo:** Iconos claros y simples, visibles sobre fondo oscuro
- **Colores:** Preferiblemente blancos/grises para máxima compatibilidad

## 📝 Uso

Los usuarios pueden:
1. Usar estos iconos predeterminados
2. Asignar sus propias imágenes PNG desde cualquier carpeta del proyecto
3. Dejar vacío para usar iconos de texto automáticos

## 🚀 Implementación

Los iconos se cargan dinámicamente desde el campo `iconImage` de cada slot.
Si no se asigna imagen, se usa un icono de texto basado en el tipo de animación.
