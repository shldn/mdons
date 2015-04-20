using UnityEngine;

public class EgocentricTunnelTestLookAt : TunnelArrowClickChoice {

    public BezierSpline tunnel = null;

	void Start () {
        ReCompute();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
            ChooseThis();
    }

    override public void ReCompute()
    {
        transform.LookAt(tunnel.GetPoint(0.0f));
    }

    override protected void ChooseThis()
    {
        TunnelGameManager.Inst.RegisterChoice(TunnelChoice.EGOCENTRIC);
        base.ChooseThis();
    }

    override protected void OnGUI()
    {
        base.OnGUI();

        if (TunnelGameManager.Inst.UseMouseButtonsToChoose && Event.current != null && Event.current.type == EventType.MouseUp && Event.current.button == 0)
            ChooseThis();
    }
}
