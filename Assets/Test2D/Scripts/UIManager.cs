using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Button DoneButton;

    public GameObject Spatula;
    public Test_RaycastSpawn_CameraPerspective RaycastAndSpawn;
    public MeshDrawer_MatClone MeshDrawerMatClone;
    public GameObject PaintDecal;

    public ColorSelection ColorSelection;

    private void Start()
    {
        Spatula.SetActive(false);
        DoneButton.onClick.AddListener(DoneButtonClicked);
    }

    private void DoneButtonClicked()
    {
        Spatula.SetActive(true);
        RaycastAndSpawn.gameObject.SetActive(false);
        MeshDrawerMatClone.gameObject.SetActive(false);
        PaintDecal.SetActive(false);
        ColorSelection.PaintDecalsAreDeactive();
    }
}
