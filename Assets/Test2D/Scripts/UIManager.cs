using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Button DoneButton;

    public GameObject Spatula;
    public GameObject ShadowDecalObj;
    public Test_RaycastSpawn_CameraPerspective RaycastAndSpawn;
    public MeshDrawer_MatClone MeshDrawerMatClone;

    public ColorSelection ColorSelection;

    private void Start()
    {
        Spatula.SetActive(false);
        
        DoneButton.onClick.AddListener(DoneButtonClicked);
    }

    private void DoneButtonClicked()
    {
        Spatula.SetActive(true);
        ShadowDecalObj.SetActive(false);
        RaycastAndSpawn.gameObject.SetActive(false);
        MeshDrawerMatClone.gameObject.SetActive(false);
        ColorSelection.PaintDecalsAreDeactive();
    }
}
