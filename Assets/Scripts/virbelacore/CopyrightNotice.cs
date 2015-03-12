using UnityEngine;
using System;
using System.Collections;


[RequireComponent(typeof(GUIText))]
public class CopyrightNotice : MonoBehaviour {

	// Use this for initialization
	void Start () {
        guiText.text = "Powered by SmartFoxServer\nThis software is Copyright © " + DateTime.UtcNow.Year.ToString() + " The Regents of the University of California.  All Rights Reserved.";
        if (GameManager.Inst.ServerConfig == "Helomics")
            guiText.text = "VirBELA\n" + guiText.text;
	}
	
}
