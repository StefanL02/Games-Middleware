using UnityEngine;

public class NetworkPlanePhysics : MonoBehaviour, IPhysical
{
    public Vector3 normal
    {
        get => transform.up;
        set => transform.up = value.normalized;
    }

    public int rank => 0;

    void OnEnable()
    {
        NetworkManager.allObjects.Add(this);
    }

    void OnDisable()
    {
        NetworkManager.allObjects.Remove(this);
    }

    public bool IsColliding(IPhysical other)
    {
        if (other is NetworkPlanePhysics) return false;

        if (other is NetworkSpherePhysics sphere)
        {
            return isCollidingWith(sphere);
        }

        return false;
    }

    public void overrideAfterCollision(Vector3 pos, Vector3 vel)
    {

    }

    public void ResolveCollision(IPhysical other, ref Vector3 position, ref Vector3 velocity)
    {

    }

    internal bool isCollidingWith(NetworkSpherePhysics spherePhysics)
    {
        Vector3 v = spherePhysics.transform.position - this.transform.position;
        Vector3 p = Utils.ParallelTo(v, normal);
        return p.magnitude < spherePhysics.Radius;
    }
}