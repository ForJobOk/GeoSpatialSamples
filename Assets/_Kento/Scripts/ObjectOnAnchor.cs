using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// アンカーの位置にオブジェクトを表示する
/// </summary>
public class ObjectOnAnchor : MonoBehaviour
{
    [SerializeField] private ARAnchorManager arAnchorManager;
    [SerializeField] private AREarthManager arEarthManager;
    [SerializeField] private Transform arObject;

    [SerializeField] private double latitude;
    [SerializeField] private double longitude;
    [SerializeField] private double altitude;
    
    [SerializeField] private Text statusText;

    private const double VERTICAL_THRESHOLD = 25;
    private const double HOLIZONTAL_THRESHOLD = 25;
    
    private ARGeospatialAnchor anchor;

    private void Update()
    {
        //UnityEditorではAREarthManagerが動作しないのでスキップ
        if (Application.isEditor)
        {
            SetInfo("On Editor.");
            return;
        }
        
        //ARFoundationのトラッキング準備が完了するまで何もしない
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            SetInfo("ARSession.state is not ready.");
            return;
        }
        
        if (!IsSupportedDevice())
        {
            SetInfo("This device is out of support GeoSpatial.");
            return;
        }

        if (!IsHighAccuracyDeviceEarthPosition())
        {
            SetInfo("Accuracy is low.");
            return;
        }
        else
        {
            SetInfo("Accuracy is High.");
        }
        
        if (IsExistGeoSpatialAnchor(latitude, longitude, altitude))
        {
            SetInfo("Adjust position and rotation.");
            Adjust();
        }
    }

    private void SetInfo(string info)
    {
        statusText.text = info;
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
    /// アンカーの位置にオブジェクトを出す
    /// </summary>
    private void Adjust()
    {
        arObject.SetPositionAndRotation(anchor.transform.position,anchor.transform.rotation);
    }

    /// <summary>
    /// アンカーの存在を確認
    /// なければ追加
    /// </summary>
    private bool IsExistGeoSpatialAnchor(double lat, double lng, double alt)
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