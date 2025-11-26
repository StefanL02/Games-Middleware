using UnityEngine;

public class CharacterRigController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private HeadLookAtController headController;
    [SerializeField] private BodyPickupController bodyController;

    [Header("Global Settings")]
    [SerializeField] private bool enableHeadLook = true;
    [SerializeField] private bool enableBodyIK = true;

    void Update()
    {
        if (bodyController.IsHoldingObject)
        {
            if (headController != null)
                headController.enabled = false;
        }
        else
        {
            if (headController != null)
                headController.enabled = enableHeadLook;
        }
    }
}