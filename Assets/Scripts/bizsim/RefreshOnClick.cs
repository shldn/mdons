using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RefreshOnClick : WebToolBarItem {
    private CollabBrowserTexture browserTexture;
    public GameObject screenToRefresh;
    private bool gotMouseDown = false; // make sure we get the down and up for a refresh.

    void OnGUI()
    {
        if (screenToRefresh != null && Event.current != null && gotMouseDown && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
        {
            gotMouseDown = false;
            if (browserTexture == null)
                browserTexture = screenToRefresh.GetComponent<CollabBrowserTexture>();
            if (browserTexture != null)
                browserTexture.RefreshWebView();
            SoundManager.Inst.PlayClick();
        }
        if (Event.current != null && Event.current.type == EventType.MouseDown)
            gotMouseDown = (MouseHelpers.GetCurrentGameObjectHit() == gameObject);

    }
}
