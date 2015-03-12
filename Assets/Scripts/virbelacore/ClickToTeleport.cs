using UnityEngine;

public class ClickToTeleport : Teleporter {

    public float maxDistance = 20;
    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject && levelDestination != GameManager.Level.NONE)
        {
            if (MaxDistCheck())
                Teleport();
            else
                InfoMessageManager.Display("Please move closer to teleport");
        }
    }

    bool MaxDistCheck()
    {
        return (maxDistance < 0 || MathHelper.Distance2D(gameObject.transform.position, GameManager.Inst.LocalPlayer.gameObject.transform.position) < maxDistance);
    }
}
