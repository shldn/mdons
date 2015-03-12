using UnityEngine;

// hides for the specified server
public class HideForServer : MonoBehaviour
{
    public string serverName = "";

    void Start()
    {
        if( serverName == GameManager.Inst.ServerConfig )
            gameObject.SetActive(false);
    }

}

