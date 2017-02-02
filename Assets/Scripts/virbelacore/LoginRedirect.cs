using UnityEngine;
using System.Collections;

public class LoginRedirect : MonoBehaviour {

#if UNITY_EDITOR
    void Awake() {
        if (!CommunicationManager.IsConnected && !Application.isWebPlayer)
            Application.LoadLevel("Connection");
    }
#endif
}
