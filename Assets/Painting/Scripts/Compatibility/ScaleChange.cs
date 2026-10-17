using PaintIn2D;
using UnityEngine;

public class ScaleChange : MonoBehaviour
{
    public SettingsContain SettingsContain;
    private CwPaintDecal2D _cwPaintDecal2D;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private float _lerpSpeed;
    private float MaxScaleX;
    
    public void Init(Vector3 startPos, CwPaintDecal2D cwPaintDecal2D)
    {
        _cwPaintDecal2D = cwPaintDecal2D;
        _startPos = startPos;
        _lerpSpeed = Random.Range(SettingsContain.MinScaleXLerpSpeed, SettingsContain.MaxScaleXLerpSpeed);
        MaxScaleX = Random.Range(SettingsContain.MinScaleX, SettingsContain.MaxScaleX);
    }

    public void SetEndPos(Vector3 endPos)
    {
        _endPos = endPos;
    }
    
    public void UpdateScaleX()
    {
        float totalDistance = Vector3.Distance(_startPos, _endPos);
        float currentDistance = Vector3.Distance(transform.position, _endPos);
  
        float progress = 1 - (currentDistance / totalDistance);
      
        float scale = Mathf.Lerp(1f, MaxScaleX, progress);
        
        _cwPaintDecal2D.Scale = new Vector3(Mathf.Lerp( _cwPaintDecal2D.Scale.x, scale, Time.deltaTime * _lerpSpeed),1,1);
    }

}
