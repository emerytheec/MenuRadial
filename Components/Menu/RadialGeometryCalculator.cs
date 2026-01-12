using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Calculadora de geometría para menús radiales
    /// Responsabilidad única: Cálculos matemáticos y posicionamiento circular
    /// </summary>
    public static class RadialGeometryCalculator
    {
        /// <summary>
        /// Calcula la posición de un botón en el círculo basado en su ángulo
        /// </summary>
        /// <param name="centerX">Centro X del menú</param>
        /// <param name="centerY">Centro Y del menú</param>
        /// <param name="angle">Ángulo en grados</param>
        /// <param name="radius">Radio desde el centro</param>
        /// <returns>Posición calculada</returns>
        public static Vector2 CalculateButtonPosition(float centerX, float centerY, float angle, float radius)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            return new Vector2(
                centerX + Mathf.Cos(angleRad) * radius,
                centerY + Mathf.Sin(angleRad) * radius
            );
        }
        
        /// <summary>
        /// Calcula el radio promedio entre el radio exterior e interior
        /// </summary>
        /// <param name="outerRadius">Radio exterior</param>
        /// <param name="innerRadius">Radio interior</param>
        /// <returns>Radio promedio</returns>
        public static float CalculateAverageRadius(float outerRadius, float innerRadius)
        {
            return (outerRadius + innerRadius) / 2f;
        }
        
        /// <summary>
        /// Calcula el ángulo por botón basado en el número total de botones
        /// </summary>
        /// <param name="totalButtons">Número total de botones</param>
        /// <returns>Ángulo en grados por botón</returns>
        public static float CalculateAnglePerButton(int totalButtons)
        {
            return totalButtons > 0 ? 360f / totalButtons : 0f;
        }
        
        /// <summary>
        /// Calcula el radio del menú basado en el área disponible
        /// </summary>
        /// <param name="area">Rect del área disponible</param>
        /// <returns>Radio calculado</returns>
        public static float CalculateMenuRadius(Rect area)
        {
            return Mathf.Min(area.width, area.height) / 2f;
        }
        
        /// <summary>
        /// Calcula el radio interior basado en el radio exterior
        /// </summary>
        /// <param name="outerRadius">Radio exterior</param>
        /// <param name="ratio">Ratio del radio interior (por defecto 0.4f)</param>
        /// <returns>Radio interior calculado</returns>
        public static float CalculateInnerRadius(float outerRadius, float ratio = 0.4f)
        {
            return outerRadius * ratio;
        }
        
        /// <summary>
        /// Calcula el centro del menú basado en el área
        /// </summary>
        /// <param name="area">Rect del área disponible</param>
        /// <returns>Centro calculado</returns>
        public static Vector2 CalculateMenuCenter(Rect area)
        {
            return new Vector2(
                area.x + area.width / 2f,
                area.y + area.height / 2f
            );
        }
        
        /// <summary>
        /// Calcula el ángulo inicial (12 en punto = -90 grados)
        /// </summary>
        /// <returns>Ángulo inicial en grados</returns>
        public static float GetInitialAngle()
        {
            return -90f; // 12 en punto
        }
        
        /// <summary>
        /// Calcula el tamaño del sector de un botón
        /// </summary>
        /// <param name="outerRadius">Radio exterior</param>
        /// <param name="innerRadius">Radio interior</param>
        /// <param name="ratio">Ratio del tamaño (por defecto 0.7f)</param>
        /// <returns>Tamaño del sector calculado</returns>
        public static float CalculateSectorSize(float outerRadius, float innerRadius, float ratio = 0.7f)
        {
            return (outerRadius - innerRadius) * ratio;
        }
        
        /// <summary>
        /// Calcula un Rect centrado para un botón
        /// </summary>
        /// <param name="center">Centro del botón</param>
        /// <param name="size">Tamaño del botón</param>
        /// <returns>Rect calculado</returns>
        public static Rect CalculateCenteredRect(Vector2 center, float size)
        {
            return new Rect(
                center.x - size / 2f,
                center.y - size / 2f,
                size,
                size
            );
        }
        
        /// <summary>
        /// Calcula la distancia entre dos puntos
        /// </summary>
        /// <param name="point1">Primer punto</param>
        /// <param name="point2">Segundo punto</param>
        /// <returns>Distancia calculada</returns>
        public static float CalculateDistance(Vector2 point1, Vector2 point2)
        {
            return Vector2.Distance(point1, point2);
        }
        
        /// <summary>
        /// Calcula el ángulo del mouse relativo al centro del menú
        /// </summary>
        /// <param name="mousePosition">Posición del mouse</param>
        /// <param name="center">Centro del menú</param>
        /// <returns>Ángulo en grados (ajustado para que 0° esté arriba)</returns>
        public static float CalculateMouseAngle(Vector2 mousePosition, Vector2 center)
        {
            Vector2 direction = mousePosition - center;
            float mouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Ajustar para que 0% esté arriba (12 en punto) y crezca en sentido horario
            mouseAngle += 90f;
            if (mouseAngle < 0) mouseAngle += 360f;
            if (mouseAngle >= 360f) mouseAngle -= 360f;
            
            return mouseAngle;
        }
        
        /// <summary>
        /// Calcula puntos para dibujar un arco/sector
        /// </summary>
        /// <param name="center">Centro del arco</param>
        /// <param name="radius">Radio del arco</param>
        /// <param name="startAngle">Ángulo inicial en grados</param>
        /// <param name="endAngle">Ángulo final en grados</param>
        /// <param name="segments">Número de segmentos para suavidad</param>
        /// <returns>Array de puntos que forman el arco</returns>
        public static Vector3[] CalculateArcPoints(Vector2 center, float radius, float startAngle, float endAngle, int segments)
        {
            if (segments < 2) segments = 2;
            
            Vector3[] points = new Vector3[segments + 2];
            
            // Primer punto: centro
            points[0] = center;
            
            // Segundo punto: inicio del arco
            float startRadians = startAngle * Mathf.Deg2Rad;
            points[1] = center + new Vector2(Mathf.Cos(startRadians), Mathf.Sin(startRadians)) * radius;
            
            // Puntos del arco
            float angleRange = endAngle - startAngle;
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                float currentAngle = startAngle + (t * angleRange);
                float currentRadians = currentAngle * Mathf.Deg2Rad;
                points[i + 2] = center + new Vector2(Mathf.Cos(currentRadians), Mathf.Sin(currentRadians)) * radius;
            }
            
            return points;
        }
    }
}
