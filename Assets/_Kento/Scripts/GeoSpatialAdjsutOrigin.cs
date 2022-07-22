using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// アンカーの位置がUnity座標系の原点となるように振る舞う
/// </summary>
public class GeoSpatialAdjsutOrigin : MonoBehaviour
{
    [SerializeField] private ARAnchorManager arAnchorManager;
    [SerializeField] private AREarthManager arEarthManager;

    [SerializeField] private double latitude;
    [SerializeField] private double longitude;
    [SerializeField] private double altitude;
    
    [SerializeField] private Text statusText;

    private const double VERTICAL_THRESHOLD = 25;
    private const double HOLIZONTAL_THRESHOLD = 25;

    private string currentInfo;
    private ARGeospatialAnchor anchor;
    
    private GameObject contentOffsetGameObject;

    /// <summary>
    /// ARSessionOriginの配下にAR座標系の原点となるオブジェクトを生成する
    /// </summary>
    private Transform contentOffsetTransform
    {
        get
        {
            if (contentOffsetGameObject == null)
            {
                contentOffsetGameObject = new GameObject("Content Placement Offset");
                contentOffsetGameObject.transform.SetParent(transform, false);
                
                for (var i = 0; i < transform.childCount; ++i)
                {
                    var child = transform.GetChild(i);
                    if (child != contentOffsetGameObject.transform)
                    {
                        child.SetParent(contentOffsetGameObject.transform, true);
                        --i; 
                    }
                }
            }

            return contentOffsetGameObject.transform;
        }
    }

    private void Update()
    {
        //UnityEditorではAREarthManagerが動作しないのでスキップ
        if (Application.isEditor)
        {
            currentInfo = "On Editor.";
            return;
        }
        
        //ARFoundationのトラッキング準備が完了するまで何もしない
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            currentInfo = "ARSession.state is not ready.";
            return;
        }
        
        if (!IsSupportedDevice())
        {
            currentInfo = "This device is out of support GeoSpatial.";
            return;
        }

        if (!IsHighAccuracyDeviceEarthPosition())
        {
            currentInfo = "Accuracy is low.";
            return;
        }
        else
        {
            currentInfo = "Accuracy is High.";
        }
        
        if (IsAddGeoSpatialAnchor(latitude, longitude, altitude))
        {
            currentInfo = "Adjust position and rotation.";
            Adjust();
        }
    }

    private void LateUpdate()
    {
        statusText.text = currentInfo;
    }

    /// <summary>
    /// 対応端末かチェック
    /// </summary>
    /// <returns>対応端末であればTrueを返す</returns>
    private bool IsSupportedDevice()
    {
        return arEarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled) != FeatureSupported.Unsupported;
    }

    /// <summary>
    /// デバイスの位置精度をチェック
    /// </summary>
    /// <returns>閾値以上の位置精度であればTrueを返す</returns>
    private bool IsHighAccuracyDeviceEarthPosition()
    {
        //EarthTrackingStateが準備できていない場合
        if (arEarthManager.EarthTrackingState != TrackingState.Tracking) return false;

        //自身の端末の位置を取得し、精度が高い位置情報が取得できているか確認する
        var pose = arEarthManager.CameraGeospatialPose;
        var verticalAccuracy = pose.VerticalAccuracy;
        var horizontalAccuracy = pose.HorizontalAccuracy;

        //位置情報が安定していない場合
        if (verticalAccuracy > VERTICAL_THRESHOLD && horizontalAccuracy > HOLIZONTAL_THRESHOLD) return false;
        
        return true;
    }
    
    /// <summary>
    /// アンカーが原点の位置に来るようにAR座標系を動かして補正する
    /// </summary>
    private void Adjust()
    {
        //回転補正
        var rot = Quaternion.Inverse(anchor.transform.rotation) * contentOffsetTransform.rotation;

        //位置補正
        var pos = contentOffsetTransform.position - anchor.transform.position;

        contentOffsetTransform.SetPositionAndRotation(pos, rot);
    }

    /// <summary>
    /// アンカーを追加する
    /// </summary>
    private bool IsAddGeoSpatialAnchor(double lat, double lng, double alt)
    {
        //EarthTrackingStateの準備ができていない場合は処理しない
        if (arEarthManager.EarthTrackingState != TrackingState.Tracking)
        {
            return false;
        }

        if (anchor == null)
        {
            //GeoSpatialアンカーを作成
            //この瞬間に反映されるわけではなくARGeospatialAnchorのUpdate関数で毎フレーム補正がかかる
            var offsetRotation = Quaternion.AngleAxis(180f, Vector3.up);
            anchor = arAnchorManager.AddAnchor(lat, lng, alt, offsetRotation);
        }

        return anchor != null;
    }
}