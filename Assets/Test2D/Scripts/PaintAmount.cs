using System;
using UnityEngine;
using UnityEngine.UI;

public class PaintAmount : MonoBehaviour
{
    public Slider PaintAmountSlider;
    private float amount = 0;
    public float MaxAmount = 100;
    public bool isFull;

    public static Action OnFinishPaint;
    
    private void OnEnable()
    {
        Test_RaycastSpawn_CameraPerspective.OnSpawnPaintDecal += CalculatePaintAmount;
    }

    private void OnDisable()
    {
        Test_RaycastSpawn_CameraPerspective.OnSpawnPaintDecal -= CalculatePaintAmount;
    }

    private void Start()
    {
        isFull = false;
        amount = 0;
        PaintAmountSlider.maxValue = MaxAmount;
        float percAmount =  Helper.Remap(amount, 0, MaxAmount,0, 100 );
        PaintAmountSlider.value = percAmount;
    }

    public void CalculatePaintAmount()
    {
        if(isFull) return;
        
        amount++;

        if (amount >= MaxAmount)
        {
            amount = MaxAmount;
            isFull = true;
            OnFinishPaint?.Invoke();
        }
        
        float percAmount =  Helper.Remap(amount, 0, MaxAmount,0, 100 );
        PaintAmountSlider.value = percAmount;
    }
}
