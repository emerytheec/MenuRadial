#!/bin/bash
# generate-booth-description.sh
# Genera booth-description.txt con la descripcion del producto en 6 idiomas
# Uso: bash tools/generate-booth-description.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

PACKAGE_JSON="$REPO_ROOT/package.json"
OUTPUT="$REPO_ROOT/booth-description.txt"

# --- Extraer version de package.json ---
VERSION=$(grep '"version"' "$PACKAGE_JSON" | head -1 | sed 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')
if [ -z "$VERSION" ]; then
    echo "ERROR: No se pudo extraer la version de $PACKAGE_JSON"
    exit 1
fi
echo "Version: $VERSION"

# --- Links ---
LINK_VPM="https://emerytheec.github.io/vpm-listing/"
LINK_GITHUB="https://github.com/emerytheec/MenuRadial"
LINK_CHANGELOG="https://github.com/emerytheec/MenuRadial/blob/main/CHANGELOG.md"
LINK_KOFI="https://ko-fi.com/bender_dios"

# --- Generar booth-description.txt ---
cat > "$OUTPUT" << LANG_JP
🇯🇵 日本語 / 🇺🇸 English / 🇪🇸 Español / 🇨🇳 中文 / 🇰🇷 한국어 / 🇷🇺 Русский

━━━━━━━━━━━━━━━━━━━━━━━━━
🇯🇵 日本語
━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}
VRChatアバター用の自動ラジアルメニュー生成システム

✨ 特徴
• 衣装追加だけで自動ラジアルメニュー生成
• On/Off・A/B・リニアスライダー対応
• マテリアル切替・ブレンドシェイプ制御
• lilToon照明コントロール
• PhysBone整理・Bounds統一
• Modular Avatar完全対応（NDMF非破壊）

📋 Unity 2022.3.22f1 / VRChat SDK 3.5.0+ / NDMF 1.4.0+

🔗 VPM: ${LINK_VPM}
GitHub: ${LINK_GITHUB}
変更履歴: ${LINK_CHANGELOG}
Ko-fi: ${LINK_KOFI}
LANG_JP

cat >> "$OUTPUT" << LANG_EN

━━━━━━━━━━━━━━━━━━━━━━━━━
🇺🇸 English
━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}
Automated radial menu generation for VRChat avatars

✨ Features
• Auto-generates radial menus by adding outfits
• On/Off toggles, A/B switches, Linear sliders
• Material switching & Blendshape control
• lilToon lighting control
• PhysBone organization & Bounds unification
• Full Modular Avatar support (NDMF non-destructive)

📋 Unity 2022.3.22f1 / VRChat SDK 3.5.0+ / NDMF 1.4.0+

🔗 VPM: ${LINK_VPM}
GitHub: ${LINK_GITHUB}
Changelog: ${LINK_CHANGELOG}
Ko-fi: ${LINK_KOFI}
LANG_EN

cat >> "$OUTPUT" << LANG_ES

━━━━━━━━━━━━━━━━━━━━━━━━━
🇪🇸 Español
━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}
Generacion automatica de menus radiales para avatares VRChat

✨ Caracteristicas
• Genera menus radiales automaticamente al agregar ropa
• Toggles On/Off, switches A/B, sliders Lineales
• Cambio de materiales y control de blendshapes
• Control de iluminacion lilToon
• Organizacion de PhysBones y unificacion de Bounds
• Compatible con Modular Avatar (NDMF no destructivo)

📋 Unity 2022.3.22f1 / VRChat SDK 3.5.0+ / NDMF 1.4.0+

🔗 VPM: ${LINK_VPM}
GitHub: ${LINK_GITHUB}
Changelog: ${LINK_CHANGELOG}
Ko-fi: ${LINK_KOFI}
LANG_ES

cat >> "$OUTPUT" << LANG_ZH

━━━━━━━━━━━━━━━━━━━━━━━━━
🇨🇳 中文
━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}
VRChat虚拟形象自动径向菜单生成系统

✨ 功能
• 添加服装即可自动生成径向菜单
• 支持开/关、A/B切换、线性滑块
• 材质切换和形态键控制
• lilToon照明控制
• PhysBone整理、Bounds统一
• 完全兼容Modular Avatar（NDMF非破坏性）

📋 Unity 2022.3.22f1 / VRChat SDK 3.5.0+ / NDMF 1.4.0+

🔗 VPM: ${LINK_VPM}
GitHub: ${LINK_GITHUB}
更新日志: ${LINK_CHANGELOG}
Ko-fi: ${LINK_KOFI}
LANG_ZH

cat >> "$OUTPUT" << LANG_KO

━━━━━━━━━━━━━━━━━━━━━━━━━
🇰🇷 한국어
━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}
VRChat 아바타용 자동 래디얼 메뉴 생성 시스템

✨ 기능
• 의상 추가만으로 래디얼 메뉴 자동 생성
• 온/오프 토글, A/B 전환, 리니어 슬라이더
• 머티리얼 전환 및 블렌드셰이프 제어
• lilToon 조명 제어
• PhysBone 정리 및 Bounds 통합
• Modular Avatar 완벽 호환 (NDMF 비파괴적)

📋 Unity 2022.3.22f1 / VRChat SDK 3.5.0+ / NDMF 1.4.0+

🔗 VPM: ${LINK_VPM}
GitHub: ${LINK_GITHUB}
변경사항: ${LINK_CHANGELOG}
Ko-fi: ${LINK_KOFI}
LANG_KO

cat >> "$OUTPUT" << LANG_RU

━━━━━━━━━━━━━━━━━━━━━━━━━
🇷🇺 Русский
━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}
Автогенерация радиальных меню для аватаров VRChat

✨ Возможности
• Автоматическая генерация меню при добавлении одежды
• Переключатели Вкл/Выкл, A/B, линейные слайдеры
• Переключение материалов и управление блендшейпами
• Управление освещением lilToon
• Организация PhysBone и объединение Bounds
• Полная совместимость с Modular Avatar (NDMF)

📋 Unity 2022.3.22f1 / VRChat SDK 3.5.0+ / NDMF 1.4.0+

🔗 VPM: ${LINK_VPM}
GitHub: ${LINK_GITHUB}
Изменения: ${LINK_CHANGELOG}
Ko-fi: ${LINK_KOFI}
LANG_RU

# --- Verificar tamano ---
CHARS=$(wc -c < "$OUTPUT")
echo ""
echo "Generado: $OUTPUT"
echo "Idiomas: JP, EN, ES, ZH, KO, RU"
echo "Version: v$VERSION"
echo "Caracteres: $CHARS / 6000"
if [ "$CHARS" -gt 6000 ]; then
    echo "WARN: Excede el limite de 6000 caracteres de Booth.pm!"
fi
