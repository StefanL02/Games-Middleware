using UnityEngine;
using UnityEngine.UIElements;

public interface IPhysical
{
    int rank { get; }

    bool IsColliding(IPhysical other);

    void ResolveCollision(IPhysical other, ref Vector3 position, ref Vector3 velocity);

    void overrideAfterCollision(Vector3 pos, Vector3 vel);
}