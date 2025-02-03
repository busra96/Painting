using UnityEngine;

[CreateAssetMenu]
public class SettingsContain : ScriptableObject
{
    public float MinOpacityLerpSpeed;
    public float MaxOpacityLerpSpeed;
    
    [Header(" Opacity Density")]
    public float MinDensityLerpSpeed;
    public float MaxDensityLerpSpeed;
    public float MaxDensity;
    public float OpacityMultiplier;
    
    [Space]
    public float MinScaleXLerpSpeed;
    public float MaxScaleXLerpSpeed;

    [Space]
    public float MinScaleX;
    public float MaxScaleX;
}
