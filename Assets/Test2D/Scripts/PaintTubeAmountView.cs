using TMPro;
using UnityEngine;

// Only animates serialized UI graphics; the PaintAmount budget remains authoritative.
public sealed class PaintTubeAmountView : MonoBehaviour
{
    public PaintAmount amount;
    public ColorSelection selection;
    public PaintTubeGraphic tube;
    public TMP_Text remainingLabel;
    [Min(1)] public float collapseSpeed = 12;
    [Min(1)] public float releaseSpeed = 9;
    private float previousUsed, displayed = 1, pressure;
    private int previousPercent = -1;

    private void OnEnable()
    {
        displayed = amount != null ? amount.RemainingFraction : 1;
        previousUsed = amount != null ? amount.UsedAmount : 0;
        pressure = 0;
        previousPercent = -1;
    }

    private void LateUpdate() => Advance(Time.deltaTime);

    // Deterministic stepping also lets editor checks verify actual budget-driven animation.
    public void Advance(float deltaTime)
    {
        if (tube == null || amount == null) return;
        float spent = Mathf.Max(0, amount.UsedAmount - previousUsed);
        previousUsed = amount.UsedAmount;
        float dt = Mathf.Max(0, deltaTime);
        float target = amount.RemainingFraction;
        displayed = Mathf.Lerp(displayed, target, 1 - Mathf.Exp(-collapseSpeed * dt));
        if (Mathf.Abs(displayed - target) < 0.0001f) displayed = target;
        // A final granted drop still squeezes; rejected spends and spreading never do.
        pressure = Mathf.Max(0, pressure - releaseSpeed * dt);
        if (spent > 0) pressure = Mathf.Clamp01(pressure + 0.32f + spent / Mathf.Max(1, amount.MaxAmount) * 12);
        Color paint = selection != null ? selection.GetOriginalColor() : tube.previewPigment;
        tube.SetState(displayed, pressure, paint);
        int percent = Mathf.CeilToInt(target * 100);
        if (remainingLabel != null && percent != previousPercent)
        {
            remainingLabel.SetText("{0}%", percent);
            previousPercent = percent;
        }
    }
}
