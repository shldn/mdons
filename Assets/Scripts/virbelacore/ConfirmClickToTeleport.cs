using UnityEngine;

public class ConfirmClickToTeleport : Teleporter {
    
    delegate void ConfirmCallbackDelegate();

    bool showConfirmDialog = false;
    public string confirmText = "Are you sure?";

    void OnGUI()
    {
        if (showConfirmDialog)
            ConfirmDialog(confirmText, Teleport, HideConfirmDialog);
        if (Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject && levelDestination != GameManager.Level.NONE)
            showConfirmDialog = true;
    }

    void HideConfirmDialog()
    {
        showConfirmDialog = false;
    }
    
    void ConfirmDialog(string text, ConfirmCallbackDelegate yesAction, ConfirmCallbackDelegate closeAction)
    {
        GUI.color = new Color32(59, 126, 196, 255);
        GUI.skin.label.fontSize = (int)(0.04f * (float)Screen.height);
        GUI.skin.button.fontSize = (int)Mathf.Min(24f, (0.04f * (float)Screen.height));
        float popupWidth = 0.4f * Screen.width;
        float popupHeight = 0.3f * Screen.height;
        Rect popupRect = new Rect((Screen.width * 0.5f) - (popupWidth * 0.5f), (Screen.height * 0.5f) - (popupHeight * 0.5f), popupWidth, popupHeight);
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        DropShadowLabel(popupRect, text);
        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        popupRect.height *= 0.25f;
        Rect btnRect = new Rect(popupRect.xMin, popupRect.yMin + (popupHeight - popupRect.height), popupRect.width, popupRect.height);
        GUILayout.BeginArea(btnRect);
        GUILayout.BeginHorizontal();
        GUILayout.Space(0.08f * Screen.width);
        if (GUILayout.Button("Yes"))
        {
            yesAction();
            closeAction();
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Cancel"))
            closeAction();
        GUILayout.Space(0.08f * Screen.width);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void DropShadowLabel(Rect rect, string text)
    {
        GUIContent guiContent = new GUIContent(text);
        GUI.color = Color.black;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), guiContent);
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), guiContent);
    } // End of DropShadowLabel().
}
