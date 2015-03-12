
public class SectorInfoScreen : BizSimScreen {
    public static SectorInfoScreen mInst = null;
    public static SectorInfoScreen Inst { get { return mInst; } }
    protected override void Awake()
    {
        base.Awake();
        bssId = CollabBrowserId.SECTORINFO;
        url = BaseURL + "/sectorinfo.php";
        refreshOnInvestmentBudgetChange = true;
        mInst = this;
    }
	
	public override void Initialize()
    {
 	    base.Initialize();
        bTex.LoadComplete += OnLoadCompleted;
    }
	
	private void OnLoadCompleted(System.Object sender, System.EventArgs args)
    {
        string javaCmd = "var elems = document.getElementsByTagName(\"img\"); if(elems.length > 0){for(var i=0; i<elems.length; i++){if (elems[i].attributes[\"src\"].value == \"/images/upload/custom/sustainability/icon_greencar.jpg\") elems[i].style.display=\"none\"}}";
        bTex.ExecuteJavaScript(javaCmd);
    }
}