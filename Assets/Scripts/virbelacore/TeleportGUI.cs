using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TeleportGUI : MonoBehaviour {

    private bool showGUIList;
    private int listEntryIdx;
	private bool showList = false;
    private GUIStyle listStyle;
	
    public int height = 20;
    public int width = 170;
    public int spaceHeight = 5;

    private void DrawLevelChange(int numLevel, int count, int x, int y, string levelName)
	{
        if (GameGUI.Inst.ButtonWithSound(new Rect(x, Screen.height - height - ((height + spaceHeight) * (count + numLevel)), width, height), levelName))
		{
			GameManager.Inst.LoadLevel(ConsoleInterpreter.GetLevel(levelName));
		}
	}
	
	public void DrawGUI (int x, int y) {
        if (!this.enabled)
            return;
        if (GameGUI.Inst.ButtonWithSound(new Rect(x, y, width, height), "Teleport"))
			showList = !showList;
		
		if( showList )
		{
			List<TeleportLocation> locations = TeleportLocation.GetAll();
			for( int i=0; i < locations.Count; ++i)
            {
				int verticalPos = Screen.height - height - ((height + spaceHeight) * (i+1));
                if (GameGUI.Inst.ButtonWithSound(new Rect(x, verticalPos, width, height), locations[i].name))
                    GetComponent<PlayerManager>().SetLocalPlayerTransform(locations[i].transform);
            }
		}
	}
}
