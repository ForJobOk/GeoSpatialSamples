using System;
using System.Collections;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// アンカーの位置が原点となるように振る舞う
/// </summary>
public class GeoSpatialAdjustOrigin : MonoBehaviour
{
    [SerializeField] private ARAnchorManager arAnchorManager;
    [SerializeField] private AREarthManager arEarthManager;
    [SerializeField] private Transform origin;
    [SerializeField] private double latitude;
    [SerializeField] private double longitude;
    [SerializeField] private double altitude;
    [SerializeField, Range(1, 10)] private float scanTime = 3f;
    [SerializeField] private Text statusText;
    
    private const double VERTICAL_THRESHOLD = 15;
    private const double HOLIZONTAL_THRESHOLD = 15;
    
    private ARGeospatialAnchor anchor;
    private GameObject contentOffsetGameObject;
    private Coroutine runningCoroutine;

    public bool IsAdjustCompleted { get; private set; }

    /// <summary>
    /// ARSessionOriginの配下にSession Spaceの原点となるオブジェクトを生成する
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
        //位置合わせ完了後は何もしない
        if (IsAdjustCompleted) return;
        
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
            runningCoroutine ??= StartCoroutine(AdjustCoroutine(AdjustComplete));
        }
    }

    /// <summary>
    /// 位置合わせ完了時に行う処理
    /// </summary>
    private void AdjustComplete()
    {
        SetInfo("Adjust Complete.");
        IsAdjustCompleted = true;
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
    /// アンカーが原点の位置に来るようにSession Spaceを動かして補正する
    /// 指定秒数間位置合わせ精度が安定したら位置補正完了とみなし、補正しない
    /// </summary>
    private IEnumerator AdjustCoroutine(Action adjustCompleteAction)
    {
        var startAdjustTime = Time.time;

        //Accuracyが一定秒数間安定するまで位置補正処理を行う
        while (scanTime > Time.time - startAdjustTime)
        {
            if (IsHighAccuracyDeviceEarthPosition() && ARSession.state == ARSessionState.SessionTracking)
            {
                //回転補正
                var rot = Quaternion.Inverse(anchor.transform.rotation) * contentOffsetTransform.rotation;

                //位置補正
                var pos = contentOffsetTransform.position - anchor.transform.position;

                contentOffsetTransform.SetPositionAndRotation(pos, rot);
            }
            else
            {
                //精度が落ちたらやり直し
                startAdjustTime = Time.time;
            }

            yield return null;
        }
        
        adjustCompleteAction.Invoke();
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
    
    /// <summary>
    /// ワールド座標を任意の点から見たローカル座標に変換
    /// </summary>
    /// <param name="world">ワールド座標</param>
    /// <returns></returns>
    public Vector3 WorldToOriginLocal(Vector3 world)
    {
        return origin.transform.InverseTransformDirection(world);
    }
}