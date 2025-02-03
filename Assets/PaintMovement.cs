using System;
using DG.Tweening;
using PaintIn2D;
//using PaintIn3D;
using UnityEngine;
using Random = UnityEngine.Random;

public class PaintMovement : MonoBehaviour
{ 
   private CwPaintDecal2D cwPaintDecal2D;
   public Vector3 startPos;
   public bool canMove;
   private bool isOpacityChanged;

   public int currentIndex;
   [SerializeField] private float movementSpeed = 1.5f;

   public float LifeTime;
   
   public PaintMovement lastPaintMovement { get; set; }
   
   private void Start()
   {
       cwPaintDecal2D = GetComponent<CwPaintDecal2D>();
       startPos = transform.position;
       //Deneme();
      // canMove = true;
   }

   public void Deneme()
   {
       float randomX = Random.Range(1f, 2f);
       Debug.Log(" random X " + randomX);
       cwPaintDecal2D.Scale = new Vector3(randomX, 1, 1);
   }
   
   public Color GetCWPaintDecalColor() => cwPaintDecal2D.Color;
   
   private void Update()
   {
       if (Input.GetKeyDown(KeyCode.A))
       {
           canMove = true;
       }
       
       if (canMove)
       {
           var distance = Vector3.Distance(transform.position, startPos);
           transform.position += Vector3.up * (movementSpeed * -1 * Time.deltaTime);
            if (distance >= (LifeTime - .2f))
            {
                StartLifeTime();
            }
       }
   }

   private void StartLifeTime()
   {
       if(isOpacityChanged)
           return;
           
       isOpacityChanged = true;
       DOTween.To(() => cwPaintDecal2D.Opacity, x => cwPaintDecal2D.Opacity = x, 0, .2f).OnComplete(DestroyPaint);
   }

   private void DestroyPaint()
   {
       ClearLastPaintMovement();

       canMove = false;
       gameObject.SetActive(false);
   }

   private void ClearLastPaintMovement()
   {
       if (lastPaintMovement == null) return;

       ActivateLastPaintMovement(transform.position);
       lastPaintMovement = null;
   }

   private void ActivateLastPaintMovement(Vector3 pos)
   {
       lastPaintMovement.transform.position = pos;
       startPos = pos;
       lastPaintMovement.isOpacityChanged = false;
       lastPaintMovement.canMove = true;
       lastPaintMovement.gameObject.SetActive(true);
   }
   
   public void OnTriggerEnter2D(Collider2D other)
   {
       if (other.TryGetComponent(out PaintMovement paintMovement) && canMove)
       {
           if (paintMovement.currentIndex == currentIndex) return;
           
           paintMovement.canMove = true;
           paintMovement.lastPaintMovement = this;
           
           DisablePaint();
       }
   }

   private void DisablePaint()
   {
       canMove = false;
       transform.DOKill();
       isOpacityChanged = false;
       gameObject.SetActive(false);
   }
   


   /* public float MaxDistance;
    private P3dPaintSphere paintSphere; 
    private Vector3 startPos;
    public bool canMove;
    private bool isOpacityChanged;

    private void Start()
    {
        paintSphere = GetComponent<P3dPaintSphere>();
        startPos = transform.position;
    }

    private void Update()
    {
        if (canMove)
        {
            var distance = Vector3.Distance(transform.position, startPos);
            transform.position += Vector3.right * -.05f;
            if (distance >= MaxDistance)
            {
                if(isOpacityChanged)
                    return;

                isOpacityChanged = true;
                DOTween.To(() => paintSphere.Opacity, x => paintSphere.Opacity = x, 0, .2f).OnComplete(()=> gameObject.SetActive(false));
            }
        }
    }*/
}
