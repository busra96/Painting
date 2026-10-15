using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Button DoneButton;
    public GameObject Spatula;
    public GameObject ShadowDecalObj;
    public Test_RaycastSpawn_CameraPerspective RaycastAndSpawn;
    public ColorSelection ColorSelection;
    private enum Phase { Painting, Spreading, Evaluating, Result }
    private Phase phase;
    private PaintAmount amount;
    private DragYPos_Spatula drag;
    private GridDataComparer comparer;
    private TMP_Text buttonLabel;
    private TMP_Text statusLabel;

    private void Start()
    {
        amount = FindFirstObjectByType<PaintAmount>();
        if (amount != null && amount.PaintAmountSlider != null)
            statusLabel = amount.PaintAmountSlider.GetComponentInChildren<TMP_Text>(true);
        if (statusLabel != null)
        {
            statusLabel.enableAutoSizing = true;
            statusLabel.fontSizeMin = 18;
            statusLabel.fontSizeMax = statusLabel.fontSize;
            statusLabel.textWrappingMode = TextWrappingModes.NoWrap;
        }
        if (Spatula != null)
        {
            drag = Spatula.GetComponentInChildren<DragYPos_Spatula>(true);
            Spatula.SetActive(false);
        }
        if (drag != null)
        {
            drag.OnCompleted += CompleteSpread;
            comparer = drag.gridDataComparer;
        }
        if (comparer != null)
        {
            comparer.OnCompared += ShowResult;
            comparer.OnComparisonFailed += ShowComparisonFailure;
        }
        if (DoneButton != null)
        {
            buttonLabel = DoneButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.enableAutoSizing = true;
                buttonLabel.fontSizeMin = 18;
                buttonLabel.fontSizeMax = buttonLabel.fontSize;
                buttonLabel.textWrappingMode = TextWrappingModes.NoWrap;
            }
            DoneButton.onClick.AddListener(DoneButtonClicked);
        }
    }

    private void Update()
    {
        if (phase == Phase.Painting)
        {
            if (DoneButton != null) DoneButton.interactable = RaycastAndSpawn != null && RaycastAndSpawn.PaintCount > 0;
            if (buttonLabel != null) buttonLabel.text = amount != null && amount.isFull ? "BOYA HAZIR - YAY" : "BOYAYI YAY";
            if (statusLabel != null && amount != null)
                statusLabel.text = RaycastAndSpawn != null && RaycastAndSpawn.PaintCount == 0 ? "BASILI TUT: BOYA BIRAK" :
                    "BOYA  " + Mathf.CeilToInt(amount.RemainingAmount) + " / " + Mathf.CeilToInt(amount.MaxAmount);
        }
        else if (phase == Phase.Spreading && drag != null)
        {
            if (amount != null) amount.ShowSpreadProgress(drag.Progress);
            if (statusLabel != null) statusLabel.text = "ASAGI CEK  " + Mathf.RoundToInt(drag.Progress * 100) + "%";
        }
    }

    private void DoneButtonClicked()
    {
        if (phase == Phase.Result)
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.path);
            return;
        }
        if (phase != Phase.Painting || RaycastAndSpawn == null || RaycastAndSpawn.PaintCount == 0) return;
        phase = Phase.Spreading;
        if (ImpastoPaintSurface.Active != null) ImpastoPaintSurface.Active.Flush();
        RaycastAndSpawn.EndStroke();
        RaycastAndSpawn.gameObject.SetActive(false);
        if (ShadowDecalObj != null) ShadowDecalObj.SetActive(false);
        if (ColorSelection != null)
        {
            ColorSelection.PaintDecalsAreDeactive();
            ColorSelection.ColorsDeactive();
        }
        if (DoneButton != null) DoneButton.gameObject.SetActive(false);
        if (Spatula != null) Spatula.SetActive(true);
    }

    private void CompleteSpread()
    {
        if (phase != Phase.Spreading) return;
        phase = Phase.Evaluating;
        if (amount != null) amount.ShowSpreadProgress(1);
        if (statusLabel != null) statusLabel.text = "RESIM KARSILASTIRILIYOR";
        StartCoroutine(FinishPainting());
    }

    private IEnumerator FinishPainting()
    {
        // The last fixed step must consume paint and run its trigger callbacks before hiding emitters.
        yield return new WaitForFixedUpdate();
        if (ImpastoPaintSurface.Active != null) ImpastoPaintSurface.Active.FinishSpread();
        if (Spatula != null) Spatula.SetActive(false);
        if (comparer != null) comparer.Test();
        else ShowComparisonFailure();
    }

    private void ShowResult(float score)
    {
        if (statusLabel != null) statusLabel.text = "ESLESME  %" + Mathf.RoundToInt(score);
        ShowRetry();
    }

    private void ShowComparisonFailure()
    {
        if (statusLabel != null) statusLabel.text = "SONUC OKUNAMADI";
        ShowRetry();
    }

    private void ShowRetry()
    {
        phase = Phase.Result;
        if (DoneButton == null) return;
        DoneButton.gameObject.SetActive(true);
        DoneButton.interactable = true;
        if (buttonLabel != null) buttonLabel.text = "TEKRAR DENE";
    }

    private void OnDestroy()
    {
        if (DoneButton != null) DoneButton.onClick.RemoveListener(DoneButtonClicked);
        if (drag != null) drag.OnCompleted -= CompleteSpread;
        if (comparer != null)
        {
            comparer.OnCompared -= ShowResult;
            comparer.OnComparisonFailed -= ShowComparisonFailure;
        }
    }
}
