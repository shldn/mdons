using UnityEngine;
using System.Collections;


public class VirCustomButton {

    Texture2D buttonTexture;
    public int x = 0;
    public int y = 0;
    public int width = 0;
    public int height = 0;

    private bool dragged = false;
    private bool hovered = false;

    public string tooltip = "";

    public enum Corner{topLeft, topRight, bottomLeft, bottomRight};

    Rect displayRect = new Rect();

    public VirCustomButton(Texture2D buttonTexture, int x, int y, int width, int height, string tooltip_ = "")
    {
        this.buttonTexture = buttonTexture;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        displayRect = new Rect(x, y, width, height);
        tooltip = tooltip_;
    }

    public VirCustomButton(Texture2D buttonTexture, int x, int y, string tooltip_ = "")
    {
        this.buttonTexture = buttonTexture;
        this.x = x;
        this.y = y;
        this.width = buttonTexture.width;
        this.height = buttonTexture.height;
        displayRect = new Rect(x, y, width, height);
        tooltip = tooltip_;
    }

    public VirCustomButton(Texture2D buttonTexture, int x, int y, float scale, string tooltip_ = "")
    {
        this.buttonTexture = buttonTexture;
        this.x = x;
        this.y = y;
        this.width = (int)(buttonTexture.width * scale);
        this.height = (int)(buttonTexture.height * scale);
        displayRect = new Rect(x, y, width, height);
        tooltip = tooltip_;
    }

    public void Draw()
    {
        GUI.color = IsHovered() ? Color.white : new Color(1f, 1f, 1f, 0.8f);
        GUI.DrawTexture(displayRect, buttonTexture);
        if(IsHovered() || IsClicked() || IsDragged())
            PlayerController.ClickToMoveInterrupt();
        HandleTooltip();
    }

    public void Close()
    {
        GameGUI.Inst.guiLayer.SendGuiLayerTooltip(tooltip, false);
    }

    private void HandleTooltip()
    {
        if (hovered != IsHovered())
        {
            hovered = !hovered;
            GameGUI.Inst.guiLayer.SendGuiLayerTooltip(tooltip, hovered);
        }
    }

    public void SetPosition(Corner corner, int newX, int newY)
    {
        switch (corner)
        {
            case Corner.topLeft :
                x = newX;
                y = newY;
                break;
            case Corner.topRight:
                x = newX - width;
                y = newY;
                break;
            case Corner.bottomLeft:
                x = newX;
                y = newY - height;
                break;
            case Corner.bottomRight:
                x = newX - width;
                y = newY - height;
                break;
        }
        displayRect = new Rect(x, y, width, height);
    }

    public bool IsHovered()
    {
        return (Input.mousePosition.x > x) &&
               (Input.mousePosition.x < (x + width)) &&
               ((Screen.height - Input.mousePosition.y) > y) &&
               ((Screen.height - Input.mousePosition.y) < (y + height));
    }

    public bool IsClicked()
    {
        if (IsHovered() && (Event.current.type == EventType.MouseDown))
        {
            dragged = true;
            return true;
        }
        else
            return false;
    }

    public bool IsDragged()
    {
        if (!dragged)
            return IsClicked();
        else
        {
            if (Input.GetMouseButton(0))
                return true;
            else
            {
                dragged = false;
                return false;
            }
        }
    }


}