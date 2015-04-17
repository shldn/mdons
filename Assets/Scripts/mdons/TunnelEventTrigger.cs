using UnityEngine;

public class TunnelEventTrigger : MonoBehaviour {

    public int splinePtIdx = 0;
    void OnTriggerEnter()
    {
        if( splinePtIdx > 0 && splinePtIdx < 3)
            TunnelGameManager.Inst.RegisterEvent((splinePtIdx == 1) ? TunnelEvent.TURN_START : TunnelEvent.TURN_END);
    }
}
