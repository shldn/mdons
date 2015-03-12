using Awesomium.Mono;
using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;

public class NewProductInvestmentsScreen : GenericSimScreen
{
    private static Dictionary<int, NewProductInvestmentsScreen> allNewProdInvesters = new Dictionary<int, NewProductInvestmentsScreen>();
    new public static Dictionary<int, NewProductInvestmentsScreen> GetAll() { return allNewProdInvesters; }

    protected override void Awake()
    {
        base.Awake();
        stageItem = 28;
        bssId = CollabBrowserId.NEWPRODUCT;
        url = BizSimScreen.GetStageItemURL(stageItem);
        allNewProdInvesters[bssId] = this;
    }

    public override void Initialize()
    {
        base.Initialize();
        bTex.AllowURLChanges = true;
        bTex.AddLoadCompleteEventListener(OnLoadComplete);
    }

    private void OnLoadComplete(System.Object sender, System.EventArgs args)
    {

		bTex.webCallbackHandler.RegisterNotificationCallback("LaunchProduct", HandleLaunchProduct);        // setup callbacks

        // setup callbacks
        string cmd = "$( \"td :submit\" ).click(function(){if( this.value.indexOf(\"aunch\") != -1 || this.value.indexOf(\"List Product\") != -1){UnityClient.Notify(\"LaunchProduct\");}});";
        bTex.ExecuteJavaScript(cmd);
    }

    private void HandleLaunchProduct(JSValue[] args)
    {
        UpdateServerWithProductLaunch();
        ProductManagementScreen.HandleNewProductForAll();
    }

    private void UpdateServerWithProductLaunch()
    {
        // build msg object
        ISFSObject prodLaunchObj = new SFSObject();
        prodLaunchObj.PutUtfString("pl", ""); // value
        CommunicationManager.SendObjectMsg(prodLaunchObj);
    }
}
