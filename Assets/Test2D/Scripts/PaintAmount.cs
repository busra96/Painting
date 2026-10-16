using System;
using UnityEngine;
using UnityEngine.UI;

public class PaintAmount : MonoBehaviour
{
    public Slider PaintAmountSlider;
    public float MaxAmount = 100;
    public bool isFull;
    public static Action OnFinishPaint;
    public float UsedAmount { get; private set; }
    public float RemainingAmount => Mathf.Max(0, MaxAmount - UsedAmount);
    public float RemainingFraction => Mathf.Clamp01(RemainingAmount / Mathf.Max(1, MaxAmount));
    private float displayedValue = 100;
    private bool displayUsage = true;

    private void OnEnable() => Test_RaycastSpawn_CameraPerspective.OnSpawnPaintDecal += CalculatePaintAmount;
    private void OnDisable() => Test_RaycastSpawn_CameraPerspective.OnSpawnPaintDecal -= CalculatePaintAmount;

    private void Start()
    {
        if (PaintAmountSlider == null) return;
        PaintAmountSlider.minValue = 0;
        PaintAmountSlider.maxValue = 100;
        PaintAmountSlider.value = 100;
    }

    private void Update()
    {
        if (!displayUsage || PaintAmountSlider == null) return;
        displayedValue = Mathf.Lerp(displayedValue, RemainingFraction * 100, 1f - Mathf.Exp(-14 * Time.deltaTime));
        PaintAmountSlider.value = displayedValue;
        if (PaintAmountSlider.fillRect != null && PaintAmountSlider.fillRect.TryGetComponent(out Image fill))
            fill.color = Color.Lerp(new Color(1, 0.3f, 0.22f), new Color(0.2f, 0.85f, 0.65f), Mathf.Clamp01(RemainingFraction * 3));
    }

    public void ShowSpreadProgress(float progress)
    {
        displayUsage = false;
        if (PaintAmountSlider == null) return;
        PaintAmountSlider.value = Mathf.Clamp01(progress) * 100;
        if (PaintAmountSlider.fillRect != null && PaintAmountSlider.fillRect.TryGetComponent(out Image fill))
            fill.color = new Color(0.25f, 0.65f, 1);
    }

    public void CalculatePaintAmount()
    {
        Spend(1);
    }

    public float Spend(float requested)
    {
        if (isFull || requested <= 0) return 0;
        float granted = Mathf.Min(requested, RemainingAmount);
        UsedAmount += granted;
        if (RemainingAmount < 0.0001f)
        {
            UsedAmount = MaxAmount;
            isFull = true;
            OnFinishPaint?.Invoke();
        }
        return granted;
    }

    public void RestoreUsedAmount(float used)
    {
        UsedAmount = Mathf.Clamp(used, 0, MaxAmount);
        isFull = RemainingAmount < 0.0001f;
        displayUsage = true;
        displayedValue = RemainingFraction * 100;
        if (PaintAmountSlider != null) PaintAmountSlider.value = displayedValue;
    }
}
