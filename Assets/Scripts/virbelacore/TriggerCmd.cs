using UnityEngine;
using System.Collections;

public class TriggerCmd : MonoBehaviour {

    public string[] cmds = null;
	
	void OnTriggerEnter () {
        foreach (string c in cmds)
            ConsoleInterpreter.Inst.ProcCommand(c);
	}

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
