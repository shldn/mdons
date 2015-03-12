using UnityEngine;
using System.Collections;

public class SpawnBot : MonoBehaviour {

    public string name = "";
    public bool male = true;
    public bool addToUserList = false;
    public string[] cmds = null;

	void Start () {
        LocalBotManager.Inst.Create(transform.position, transform.rotation, male, addToUserList, name);

        foreach (string c in cmds)
            ConsoleInterpreter.Inst.ProcCommand(c);
	}

}
