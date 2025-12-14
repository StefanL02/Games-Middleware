using UnityEngine;

public class HeadLookAt : MonoBehaviour
{
    public Transform target;
    public float maxYaw = 55f;
    public float maxPitch = 30f;

    //  
    public Vector3 axisOffset = new Vector3(0f, 180f, 0f);

    private void LateUpdate()
    {
        if (target == null || transform.parent == null) return;

        Vector3 dir = target.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        // World look rotation
        Quaternion targetRot = Quaternion.LookRotation(dir);

        // Convert to parent local space
        Quaternion localRot =
            Quaternion.Inverse(transform.parent.rotation) * targetRot;

        // Apply axis correction
        localRot *= Quaternion.Euler(axisOffset);

        Vector3 euler = localRot.eulerAngles;

        // Normalize angles
        euler.x = (euler.x > 180f) ? euler.x - 360f : euler.x;
        euler.y = (euler.y > 180f) ? euler.y - 360f : euler.y;

        euler.z = 0f;

        // Clamp
        euler.x = Mathf.Clamp(euler.x, -maxPitch, maxPitch);
        euler.y = Mathf.Clamp(euler.y, -maxYaw, maxYaw);

        transform.localRotation = Quaternion.Euler(euler);
    }
}
