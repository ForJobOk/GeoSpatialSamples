using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// サーバーへ接続
/// </summary>
public class ConnectPunServer : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions(), TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);
    }
}