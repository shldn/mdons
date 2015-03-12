using UnityEngine;
using System.Collections;

public class LoginRedirect : MonoBehaviour {

#if !UNITY_WEBPLAYER
    void Awake() {
        if (!CommunicationManager.IsUserAuthenticated && !Application.isWebPlayer)
            Application.LoadLevel("Connection");
    }
#endif
}
