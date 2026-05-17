using UnityEngine;

/// <summary>
/// Coeficiente de restitución del obstáculo.
/// Cuasi-elástico (goma):    e ? 0.8
/// Inelástico (saco arena):  e ? 0.2
/// Default (pared):          e = 0.6
/// </summary>
public class ObstacleProperties : MonoBehaviour
{
    [Range(0f, 1f)]
    public float restitution = 0.8f;
}
