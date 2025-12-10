using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetworkManager : MonoBehaviour
{
    public static List<IPhysical> allObjects = new List<IPhysical>();

    void Update()
    {
        for (int i0 = 0; i0 < allObjects.Count - 1; i0++)
        {
            for (int j0 = i0 + 1; j0 < allObjects.Count; j0++)
            {
                if (allObjects[i0] == null || allObjects[j0] == null)
                {
                    continue;
                }

                int i, j;
                if (allObjects[i0].rank >= allObjects[j0].rank)
                {
                    i = i0;
                    j = j0;
                }
                else
                {
                    i = j0;
                    j = i0;
                }

                if (allObjects[i] is MonoBehaviourPun pun && !pun.photonView.IsMine)
                {
                    continue;
                }

                if (allObjects[i].IsColliding(allObjects[j]))
                {
                    Vector3 PoS = Vector3.zero, vel = Vector3.zero;
                    allObjects[i].ResolveCollision(allObjects[j], ref PoS, ref vel);
                    allObjects[j].overrideAfterCollision(PoS, vel);
                }
            }
        }
    }
}