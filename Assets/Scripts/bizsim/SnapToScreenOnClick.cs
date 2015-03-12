using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SnapToScreenOnClick : WebToolBarItem {

    private CollabBrowserTexture browserTexture;
    public GameObject screenToSnapTo;

    void OnGUI()
    {
        if (screenToSnapTo != null && Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
        {
            if (browserTexture == null)
                browserTexture = screenToSnapTo.GetComponent<CollabBrowserTexture>();
            if (browserTexture != null)
            {
                Event.current.Use();
                browserTexture.ToggleSnapCameraToObject();
                if( MainCameraController.Inst.cameraType == CameraType.SNAPCAM )
                    browserTexture.Focus();
            }

            SoundManager.Inst.PlayClick();
        }
    }
}
