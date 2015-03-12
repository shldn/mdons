
public class CharacterLookAtLocalPlayer : CharacterLookAt {
    protected override void Start()
    {
        base.Start();
        UseCharacterOffset();
        if (lookAtGameObject == null && GameManager.Inst.LocalPlayer != null)
            lookAtGameObject = GameManager.Inst.LocalPlayer.gameObject;
    }	
}
