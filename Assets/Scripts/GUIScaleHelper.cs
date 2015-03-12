using UnityEngine;  
using System.Collections;  
  
public class GUIScaleHelper : MonoBehaviour   
{  
    //a GUISkin object to draw the GUI image  
    public GUISkin guiSkin;  
  
    //the GUI scale ratio  
    private float guiRatio;  
  
    //the screen width  
    private float sWidth;  
  
    //create a scale Vector3 with the above ratio  
    private Vector3 GUIsF;  
  
    //At this script initialization  
    void Awake()  
    {  
        //get the screen's width  
        sWidth = Screen.width;  
        //calculate the scale ratio  
        guiRatio = sWidth/1920;  
        //create a scale Vector3 with the above ratio  
        GUIsF = new Vector3(guiRatio,guiRatio,1);  
    }  
  
    //Draws GUI elements  
    void OnGUI()   
    {  
        //scale and position the GUI element to draw it at the screen's top left corner  
        GUI.matrix = Matrix4x4.TRS(new Vector3(GUIsF.x,GUIsF.y,0),Quaternion.identity,GUIsF);  
        //draw GUI on the top left  
        GUI.Label(new Rect(20,20,258,89),"",guiSkin.customStyles[0]);  
  
        //scale and position the GUI element to draw it at the screen's bottom right corner  
        GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width - 258*GUIsF.x,Screen.height - 89*GUIsF.y,0),Quaternion.identity,GUIsF);  
        //draw GUI on the bottom right  
        GUI.Label(new Rect(-20,-20,258,89),"",guiSkin.customStyles[0]);  
    }  
}  