using Photon.Pun;
using UnityEngine;

public class Ball : MonoBehaviourPun
{
    private NetworkSpherePhysics spherePhysics;

    void Awake()
    {
        spherePhysics = GetComponent<NetworkSpherePhysics>();
    }

    public bool IsFree()
    {
        return spherePhysics != null && !spherePhysics.isHeld;
    }

    public void Pickup(int playerViewID)
    {
        if (!IsFree()) return;

        if (!base.photonView.IsMine)
        {
            base.photonView.RequestOwnership();
        }

        photonView.RPC("RPC_Attach", RpcTarget.AllBuffered, playerViewID);
    }

    public void Throw(Vector3 velocity)
    {
        photonView.RPC("RPC_Throw", RpcTarget.AllBuffered, velocity);
    }

    [PunRPC]
    void RPC_Attach(int playerViewID)
    {
        PhotonView playerPV = PhotonView.Find(playerViewID);
        if (playerPV != null)
        {
            PlayerController pc = playerPV.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (spherePhysics) spherePhysics.isHeld = true;
                transform.SetParent(pc.handPosition);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }
    }

    [PunRPC]
    void RPC_Throw(Vector3 velocity)
    {
        transform.SetParent(null);
        if (spherePhysics)
        {
            spherePhysics.isHeld = false;
            spherePhysics.SetLaunchVelocity(velocity);
        }
    }
}