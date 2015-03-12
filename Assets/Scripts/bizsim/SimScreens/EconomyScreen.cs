
public class EconomyScreen : BizSimScreen {
    protected override void Awake()
    {
        base.Awake();
        bssId = CollabBrowserId.ECONOMY;
        url = BaseURL + "/economy.php";
    }
}
