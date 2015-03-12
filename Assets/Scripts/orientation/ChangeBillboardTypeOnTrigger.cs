using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CameraSettingsTrigger))]
public class ChangeBillboardTypeOnTrigger : MonoBehaviour {

    CameraSettingsTrigger camTrigger = null;
    public Billboard billboard = null;
    public BillboardType typeOnTrigger = BillboardType.MATCH_ORIENT;
    public BillboardType typeOffTrigger = BillboardType.MATCH_POSITION;

    void Start () {
        camTrigger = GetComponent<CameraSettingsTrigger>();
	}
	
	void Update () {
        billboard.Type = (billboard != null && camTrigger.settingsTriggered) ? typeOnTrigger : typeOffTrigger;
	}
}
