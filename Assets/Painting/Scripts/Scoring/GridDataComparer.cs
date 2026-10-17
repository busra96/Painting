using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridDataComparer : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI yuzdelikText;
    public ImageGridProcessor imageGridProcessor;
    public CurrentImageGridProcessor currentImageGridProcessor;
    public event Action<float> OnCompared;
    public event Action OnComparisonFailed;
    public bool IsEvaluating { get; private set; }
    public float LastScore { get; private set; }
    private Tween scoreTween;

    [ContextMenu("TEST")]
    public void Test()
    {
        if (IsEvaluating) return;
        if (Application.isPlaying) StartCoroutine(EvaluateAfterPaint());
        else Evaluate();
    }

    private IEnumerator EvaluateAfterPaint()
    {
        IsEvaluating = true;
        // Let the final physics contacts and PaintIn2D LateUpdate finish first.
        yield return null;
        Evaluate();
        IsEvaluating = false;
    }

    private void Evaluate()
    {
        if (imageGridProcessor == null || currentImageGridProcessor == null ||
            !imageGridProcessor.RebuildGrid() || !currentImageGridProcessor.CapturePaintedImage())
        {
            Debug.LogWarning("The painted image is not ready for comparison.", this);
            OnComparisonFailed?.Invoke();
            return;
        }
        LastScore = CompareGridData(imageGridProcessor.GetGridData(), currentImageGridProcessor.GetGridData());
        scoreTween?.Kill();
        if (slider != null)
        {
            slider.gameObject.SetActive(true);
            slider.minValue = 0;
            slider.maxValue = 100;
            scoreTween = slider.DOValue(LastScore, 0.65f).SetEase(Ease.OutCubic);
        }
        if (yuzdelikText != null) yuzdelikText.text = Mathf.RoundToInt(LastScore) + " / 100";
        OnCompared?.Invoke(LastScore);
    }

    public float CompareGridData(List<GridCell> original, List<GridCell> current)
    {
        if (original == null || current == null || original.Count == 0 || original.Count != current.Count) return 0;
        float targetWeight = 0, paintedWeight = 0, matchedWeight = 0;
        for (int i = 0; i < original.Count; i++)
        {
            Color target = original[i].color;
            Color painted = current[i].color;
            float targetInk = InkAmount(target);
            float paintedInk = InkAmount(painted);
            float colorDistance = Mathf.Sqrt((target.r - painted.r) * (target.r - painted.r) +
                (target.g - painted.g) * (target.g - painted.g) + (target.b - painted.b) * (target.b - painted.b));
            float similarity = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.08f, 0.65f, colorDistance));
            targetWeight += targetInk;
            paintedWeight += paintedInk;
            matchedWeight += Mathf.Min(targetInk, paintedInk) * similarity;
        }
        // Colored coverage matters; matching empty white paper cannot inflate the result.
        float total = targetWeight + paintedWeight;
        return total <= 0.0001f ? 100 : Mathf.Clamp01(2f * matchedWeight / total) * 100;
    }

    private static float InkAmount(Color color)
    {
        return Mathf.Clamp01((1f - Mathf.Min(color.r, Mathf.Min(color.g, color.b))) / 0.25f);
    }

    private void OnDestroy() => scoreTween?.Kill();
}
