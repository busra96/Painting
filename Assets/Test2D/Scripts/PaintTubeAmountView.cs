using TMPro;
using UnityEngine;

// Only animates serialized UI graphics; the PaintAmount budget remains authoritative.
public sealed class PaintTubeAmountView : MonoBehaviour
{
    public PaintAmount amount;
    public ColorSelection selection;
    public PaintTubeGraphic tube;
    public TMP_Text remainingLabel;
    [Min(1)] public float fillResponseSpeed = 12;
    private float displayed = 1;
    private int previousPercent = -1;

    private void OnEnable()
    {
        displayed = amount != null ? amount.RemainingFraction : 1;
        previousPercent = -1;
    }

    private void LateUpdate() => Advance(Time.deltaTime);

    // Deterministic stepping also lets editor checks verify actual budget-driven animation.
    public void Advance(float deltaTime)
    {
        if (tube == null || amount == null) return;
        float dt = Mathf.Max(0, deltaTime);
        float target = amount.RemainingFraction;
        displayed = Mathf.Lerp(displayed, target, 1 - Mathf.Exp(-fillResponseSpeed * dt));
        if (Mathf.Abs(displayed - target) < 0.0001f) displayed = target;
        Color paint = selection != null ? selection.GetOriginalColor() : tube.previewPigment;
        tube.SetState(displayed, paint);
        int percent = Mathf.CeilToInt(target * 100);
        if (remainingLabel != null && percent != previousPercent)
        {
            remainingLabel.SetText("{0}%", percent);
            previousPercent = percent;
        }
    }
}
