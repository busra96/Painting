using UnityEngine;

[CreateAssetMenu]
public class SettingsContain : ScriptableObject
{
    [Header("Thick acrylic paint")]
    [Min(0.001f)] public float PaintVolumePerUnit = 0.12f;
    [Range(0.1f, 0.6f)] public float DepositRadius = 0.24f;
    [Min(0.1f)] public float PourUnitsPerSecond = 7f;
    [Min(0.1f)] public float PourUnitsPerWorldUnit = 1.8f;
    [Range(0.02f, 0.3f)] public float FilmThickness = 0.085f;
    [Range(0.1f, 1f)] public float FeatherDistance = 0.4f;
    [Range(20, 120)] public float PigmentOpacity = 65f;
    [Header("Paint carried by the blade")]
    [Range(0, 0.03f)] public float LateralFlow = 0.012f;
    [Range(0, 1)] public float ColorRibbons = 0.68f;
    [Range(0, 1)] public float SurfaceVariation = 0.32f;
    [Header("Paint lighting")]
    [Range(0.01f, 0.2f)] public float ReliefHeight = 0.075f;
    [Range(0, 1)] public float WetGloss = 0.32f;
    [Range(0, 1)] public float ContactShadow = 0.32f;
    [Range(0, 1)] public float SculptedOilLighting;

    [Header("Legacy decal scenes")]
    [Min(0.1f)] public float PaintDistancePerDab = 2.8f;
    [Min(0.1f)] public float MaxPaintDistance = 14f;
    [Range(0.05f, 1f)] public float PaintFadeFraction = 0.25f;
    [Range(0.5f, 2f)] public float PaintSpreadMultiplier = 1.15f;

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
