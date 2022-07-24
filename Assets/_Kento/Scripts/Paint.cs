using Photon.Pun;
using UnityEngine;

/// <summary>
/// お絵描き機能
/// </summary>
public class Paint : MonoBehaviourPun
{
    [SerializeField] private GameObject inkPrefab;
    [SerializeField] private Transform inkParent;

    /// <summary>
    /// 原点を定めるコンポーネント
    /// </summary>
    private GeoSpatialAdjustOrigin geoSpatialAdjsutOrigin;

    private void Start()
    {
        geoSpatialAdjsutOrigin = FindObjectOfType<GeoSpatialAdjustOrigin>();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        //位置合わせ完了までお絵描き機能は利用できない
        if (!geoSpatialAdjsutOrigin.IsAdjustCompleted) return;

        if (0 < Input.touchCount)
        {
            var touch = Input.GetTouch(0);
            var inputPosition = Input.GetTouch(0).position;
            var paintPosZ = 0.5f;
            var tmpTouchPos = new Vector3(inputPosition.x, inputPosition.y, paintPosZ);
            var touchWorldPos = geoSpatialAdjsutOrigin.WorldToOriginLocal(Camera.main.ScreenToWorldPoint(tmpTouchPos));

            if (touch.phase == TouchPhase.Began)
            {
                photonView.RPC(nameof(PaintStartRPC), RpcTarget.All, touchWorldPos);
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                photonView.RPC(nameof(PaintingRPC), RpcTarget.All, touchWorldPos);
            }
        }
    }

    /// <summary>
    /// RPCで生成
    /// </summary>
    [PunRPC]
    private void PaintStartRPC(Vector3 inkPosition)
    {
        Instantiate(inkPrefab, inkPosition, Quaternion.identity, inkParent);
    }

    /// <summary>
    /// RPCで動かす
    /// </summary>
    [PunRPC]
    private void PaintingRPC(Vector3 inkPosition)
    {
        if (inkParent.childCount > 0)
        {
            inkParent.transform.GetChild(inkParent.childCount - 1).transform.localPosition = inkPosition;
        }
    }
}