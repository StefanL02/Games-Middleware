using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomLauncher : MonoBehaviourPunCallbacks
{
    void Start()
    {
        Debug.Log("Connecting...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master. Joining Room...");
        PhotonNetwork.JoinOrCreateRoom("MainRoom", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room. Spawning Player.");
        PhotonNetwork.Instantiate("Player", new Vector3(0, 1, 0), Quaternion.identity);
    }
}