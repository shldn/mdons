using UnityEngine;
using System.Collections;

public class CameraSettings {
    public CameraSettings() { }
    public CameraSettings(float height_, float distance_, float rightOffset_, float tilt_){
        height = height_;
        distance = distance_;
        rightOffset = rightOffset_;
        tilt = tilt_;
    }

    public float height;
    public float distance;
    public float rightOffset;
    public float tilt;
} // End of CameraSettings.

public class CameraSettingsTrigger : MonoBehaviour {

    public float triggerRadius = 7f;
    public float triggerAngle = 90f;
    [HideInInspector] public bool settingsTriggered = false;
    
    public float desiredHeight = 2.0f;
    public float desiredDistance = 9.0f;
    public float desiredRtOffset = 0.0f;
    public float desiredTilt = 0f;

    public bool disableGazeTilt = false;

    public TriggerArea exclusionTrigger = null;


    CameraSettings initSettings = null;

    CameraSettings GetCamSettings(){
        return new CameraSettings(MainCameraController.Inst.followPlayerHeight, MainCameraController.Inst.followPlayerDistance, MainCameraController.Inst.rightOffset, MainCameraController.Inst.tilt);
    } // End of GetCamSettings().

    void SetCamSettings(CameraSettings s){
        MainCameraController.Inst.followPlayerHeight = s.height;
        MainCameraController.Inst.followPlayerDistance = s.distance;
        MainCameraController.Inst.rightOffset = s.rightOffset;
        MainCameraController.Inst.tilt = s.tilt;
    } // End of SetCamSettings().

    void Update(){
        if (GameManager.Inst.LocalPlayer != null){

            Vector3 vectorToTrigger = transform.position - GameManager.Inst.LocalPlayer.gameObject.transform.position;
            float playerAngleToThis = Mathf.Abs(Vector3.Angle(GameManager.Inst.LocalPlayer.gameObject.transform.forward, vectorToTrigger));

            // Trigger settings if player within trigger area and facing table.
            if (!settingsTriggered && ((exclusionTrigger == null) || !exclusionTrigger.ContainsPoint(GameManager.Inst.LocalPlayer.gameObject.transform.position)) && (Vector3.Distance(GameManager.Inst.LocalPlayer.gameObject.transform.position, transform.position) <= triggerRadius) && (playerAngleToThis <= triggerAngle)){
                settingsTriggered = true;

                if (initSettings == null)
                    initSettings = GetCamSettings();
                SetCamSettings(new CameraSettings(desiredHeight, desiredDistance, desiredRtOffset, desiredTilt));
            }

            if(settingsTriggered){
                if (disableGazeTilt)
                    MainCameraController.Inst.gazePanLock = true;
            }

            // Revert settings if player faces away or moves out of trigger.
            if (settingsTriggered && ((Vector3.Distance(GameManager.Inst.LocalPlayer.gameObject.transform.position, transform.position) > triggerRadius) || (playerAngleToThis > triggerAngle) || (exclusionTrigger != null && exclusionTrigger.ContainsPoint(GameManager.Inst.LocalPlayer.gameObject.transform.position))))
            {
                settingsTriggered = false;

                SetCamSettings(initSettings);
            }
        }
    } // End of Update().

    void OnDrawGizmos(){
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    } // End of OnDrawGizmos().
} // End of CameraSettingsTrigger.
