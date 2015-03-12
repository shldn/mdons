using UnityEngine;

// shows for the specified server, hides for all others
public class ShowForServer : MonoBehaviour {

    public string serverName = "";

	void Start () {
        gameObject.SetActive(serverName == GameManager.Inst.ServerConfig);
	}
	
}
