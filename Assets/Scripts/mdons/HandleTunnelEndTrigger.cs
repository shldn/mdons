using UnityEngine;
using System.Collections;

public class HandleTunnelEndTrigger : MonoBehaviour
{
	void OnTriggerEnter () {
        TunnelLight.DisableAll();

        // Display Proper Arrows for experiment
        if (TunnelGameManager.Inst.UseRotatableArrow)
            TunnelEnvironmentManager.Inst.rotatableArrow.SetActive(true);
        else
            TunnelArrowClickChoice.EnableAll();

        // Stop player and camera
        GameManager.Inst.LocalPlayer.playerController.StopMomentum();

        TunnelGameManager.Inst.HideAbstractPlayer();
        GameManager.Inst.LocalPlayer.Visible = false;
        TunnelGameManager.Inst.RegisterEvent(TunnelEvent.TUNNEL_EXIT);
	}
}
