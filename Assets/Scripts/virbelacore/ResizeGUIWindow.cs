using UnityEngine;
using System.Collections;

public class ResizeGUIWindow
{
    static bool isResizing = false;
    static Rect resizeStart = new Rect();
    static Vector2 minWindowSize = new Vector2(75, 50);
    static GUIStyle styleWindowResize = null;
    static GUIContent gcDrag = new GUIContent("//", "drag to resize");

    public static Rect ResizeWindow(Rect windowRect)
    {
        if (styleWindowResize == null)
        {
            // this is a custom style that looks like a // in the lower corner
            styleWindowResize = GUI.skin.GetStyle("WindowResizer");
        }

        Vector2 mouse = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        Rect r = GUILayoutUtility.GetRect(gcDrag, styleWindowResize);

        if (Event.current.type == EventType.mouseDown && r.Contains(mouse))
        {
            isResizing = true;
            resizeStart = new Rect(mouse.x, mouse.y, windowRect.width, windowRect.height);
            //Event.current.Use();  // the GUI.Button below will eat the event, and this way it will show its active state
        }
        else if (Event.current.type == EventType.mouseUp && isResizing)
        {
            isResizing = false;
        }
        else if (!Input.GetMouseButton(0))
        {
            // if the mouse is over some other window we won't get an event, this just kind of circumvents that by checking the button state directly
            isResizing = false;
        }
        else if (isResizing)
        {
            windowRect.width = Mathf.Max(minWindowSize.x, resizeStart.width + (mouse.x - resizeStart.x));
            windowRect.height = Mathf.Max(minWindowSize.y, resizeStart.height + (mouse.y - resizeStart.y));
            windowRect.xMax = Mathf.Min(Screen.width, windowRect.xMax);  // modifying xMax affects width, not x
            windowRect.yMax = Mathf.Min(Screen.height, windowRect.yMax);  // modifying yMax affects height, not y
        }

        GUI.Button(r, gcDrag, styleWindowResize);

        return windowRect;
    }
}
