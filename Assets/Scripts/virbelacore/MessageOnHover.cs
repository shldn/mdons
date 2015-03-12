using UnityEngine;


public class MessageOnHover : MonoBehaviour {

    public string message = "";
    public float maxDistance = -1;
    public string serverName = ""; // if specified, will only show up for this virtual server
    public CameraType hideForCameraType = CameraType.NONE;
    private bool msgShown = false;

    void Awake()
    {
        if (GetComponent<BoxCollider>() == null && GetComponent<MeshCollider>() == null)
            gameObject.AddComponent<BoxCollider>();
    }

    void OnGUI()
    {
        if (MouseHelpers.GetCurrentGameObjectHit() == gameObject && (string.IsNullOrEmpty(serverName) || serverName == GameManager.Inst.ServerConfig) && GameManager.Inst.LocalPlayer && MaxDistCheck() && (hideForCameraType == CameraType.NONE || hideForCameraType != MainCameraController.Inst.cameraType))
        {
            if( !msgShown )
                InfoMessageManager.Display(message);
            msgShown = true;
        }
        else
            msgShown = false;
    }

    bool MaxDistCheck()
    {
        return (maxDistance < 0 || MathHelper.Distance2D(gameObject.transform.position, GameManager.Inst.LocalPlayer.gameObject.transform.position) < maxDistance);
    }
}
