using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;

public enum ProductValueType
{
    NONE = 0,
    SALES_PRICE = 1,
    MARKETING_BUDGET = 2,
    PRODUCTION = 4,
	TAB = 8,
    INVENTORY = 16,
    FACTORIES = 32
} // stay below 256 for additions here, logic for message passing depends on it.
	
public enum ProductType
{
	MINI=0,
	SMALL=1,
	COMPACT=2,
	MIDSIZE=3,
	LUXURY=4
};

public struct ProductVals
{
    public ProductVals(string val_, string sliderVal_)
    {
        val = val_;
        sliderVal = sliderVal_;
    }

    private int RemoveNonDigitsAndConvertToInt(string str)
    {
        string intStr = Regex.Replace(str, "\\D", ""); // \D == not digits (not worrying about negative numbers for now)
        return Convert.ToInt32(intStr);
    }

    public int GetValInt()
    {
        // strip commas
        return RemoveNonDigitsAndConvertToInt(val);
    }

    public int GetSliderValInt()
    {
        // strip "px"
        return RemoveNonDigitsAndConvertToInt(sliderVal);
    }

    public string val;
    public string sliderVal;
}

public class ProductScreen : BizSimScreen{
    private static List<ProductScreen> products = new List<ProductScreen>();
    new public static List<ProductScreen> GetAll() { return products; }

    protected int id;
    private string idTag;
    private bool activeStateOnLoad = true;
    ProductVals sales;
    ProductVals marketingBudget;
    ProductVals production;
	ProductVals blankVal;
	private int numFactories = -1;
    private int productLifeCycle = -1;
	public string currentActiveTab;
    private string tabOnLoad = "";
    private int dirtyFlags;
    private float[] waitTime = { 0.025f, 1.0f }; // check close to immediately and then after some time in case the javascript values didn't update immediately
	public static readonly ArrayList tabNames = new ArrayList{"sales", "marketing", "competitors", "expand", "calc", "acc", "prod"};
    public static readonly int maxSliderVal = 106; // px, (defined by industry masters, the max position of the slider), min is 0.
    private static Dictionary<string, string> nameFromTag = new Dictionary<string, string> // product tag --> type name
        {
            {"016200", "mini"}, {"5V73Ub", "small"}, {"016215", "compact"}, {"016210", "midsize"}, {"016240", "luxury"}
        };
    public static string GetProductURL(string productTag){
        return (BizSimScreen.GetStageItemURL(100) + "&ptag=" + productTag);
    }

    protected int GetTabIdx(string tabName)
    {
        for (int i = 0; i < tabNames.Count; ++i)
            if (tabNames[i] == tabName)
                return i;
        return -1;
    }

    private void AddProductToList(int id, ProductScreen p)
    {
        while (id >= products.Count)
            products.Add(null);
        products[id] = p;
    }

    protected override void Awake()
    {
        base.Awake();
        stageItem = 100;
        AddProductToList(id, this);
        currentActiveTab = "sales";
        blankVal = new ProductVals("", "");
        refreshOnInvestmentBudgetChange = true;
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        products.Remove(this);
    }

    public override void Initialize()
    {
        base.Initialize();
        if (bTex.IsLoaded)
            InitializeValues();
        else
            bTex.AddLoadCompleteEventListener(OnLoadComplete);

        bTex.supportedDialogMessages.Add("Are you sure you want to upsize this business unit?", "bssid=" + bssId + ",div=butexpand");
        bTex.supportedDialogMessages.Add("Are you sure you want to liquidate this business unit?", "bssid=" + bssId + ",div=butliquidate");
        bTex.supportedDialogMessages.Add("Are you sure you want to downsize this business unit?", "bssid=" + bssId + ",div=butdownsize");
        bTex.supportedDialogMessages.Add("Are you sure you want to relaunch this business unit?", "bssid=" + bssId + ",div=butrelaunch");
    }

    void InitializeValues()
    {
        UpdateTabOnLoad();
        UpdateValues();
    }

    public override void Refresh()
    {
        tabOnLoad = currentActiveTab;
        base.Refresh();
    }

    public void SetActive(bool active, bool waitTilLoaded = false)
	{
        activeStateOnLoad = active;
        if (!waitTilLoaded || (IsURLLoaded))
            gameObject.SetActive(active);       
	}

    private void UpdateProductLifeCycleVal()
    {
        string jsCmd = "var elems = document.getElementsByClassName(\"tabhold\"); if (elems != null && elems.length > 0 && elems[0].rows.length > 0) { elems[0].rows[1].cells[1].getElementsByTagName(\"img\")[0].src; }";        
        string result = GetCmdStrResult(jsCmd);

        bool error = false;
        int newProductLCV = -1;
        int splitIdx = result.LastIndexOf("_");
        error = splitIdx == -1;
        if (!error)
            error = !int.TryParse(result.Substring(splitIdx + 1, 1), out newProductLCV);
        if (!error && newProductLCV > 0)
        {
            bool relaunchDetected = productLifeCycle > 0 && newProductLCV < productLifeCycle;
            productLifeCycle = newProductLCV;
            if (relaunchDetected)
                UpdateServerPLC();
        }
        else
            Debug.LogError("UpdateProductLifeCycleVal parse failed");

    }

    private void UpdateValueType(ProductValueType productType, ref ProductVals currentVals)
    {
		if (productType == ProductValueType.TAB)
		{
			string newVal = GetActiveTab();
            if (newVal != currentActiveTab && newVal != "")
			{
				currentActiveTab = newVal;
	            dirtyFlags |= (int)productType;
			}
		}
		else
		{
	        ProductVals newVals = new ProductVals();
            string elemName = ProductHelper.GetValueDivName(productType, IDTag);
            string cmd = ProductHelper.GetDisplayValueCmd(elemName);
	        newVals.val = GetCmdStrResult(cmd);
	        if (newVals.val != currentVals.val)
	        {
                elemName = ProductHelper.GetSliderElemName(productType);
                cmd = ProductHelper.GetSliderPositionCmd(elemName);
	            newVals.sliderVal = GetCmdStrResult(cmd);
	
	            currentVals = newVals;
	            dirtyFlags |= (int)productType;
	        }
		}
    }

    private void UpdateValues()
    {
        UpdateValueType(ProductValueType.SALES_PRICE, ref sales);
        UpdateValueType(ProductValueType.MARKETING_BUDGET, ref marketingBudget);
        UpdateValueType(ProductValueType.PRODUCTION, ref production);
        UpdateValueType(ProductValueType.TAB, ref blankVal);
    }

    public void UpdateSlider(string sliderElementName, string newValue)
    {
        if (bTex.isWebViewBusy())
            return;
        string cmd = "var elem = document.getElementById(\"" + sliderElementName + "\"); if( elem != null && elem.style != null){elem.style.left=\"" + newValue + "\";}";
        bTex.ExecuteJavaScript(cmd);
    }

    public void UpdateDisplayValue(string displayElementName, string newValue)
    {
        if (bTex.isWebViewBusy())
            return;
        string cmd = "var elem = document.getElementById(\"" + displayElementName + "\"); if(elem != null && elem.firstChild != null && elem.firstChild.firstChild != null){elem.firstChild.firstChild.textContent=\"" + newValue + "\";}";
        bTex.ExecuteJavaScript(cmd);
    }
	
	public string GetTabDisplayState(string tab)
	{
        return GetCmdStrResult("var elem = document.getElementById(\"" + tab + IDTag + "\"); if(elem != null && elem.parentNode != null){elem.parentNode.style.display;}");
	}
	
	public string GetActiveTab()
	{
		foreach (string n in ProductScreen.tabNames)
		{
			if (GetTabDisplayState(n) == "block")
				return n;
		}
        Debug.LogError("No Active Tab yet");
		return "";
	}

    public void UpdateActiveTab(int tabIdx)
    {
        UpdateActiveTab((string)tabNames[tabIdx]);
    }

	public void UpdateActiveTab(string newTab)
	{
        if (currentActiveTab == newTab || newTab == "")
            return;

        if (bTex == null || bTex.isWebViewBusy())
        {
            tabOnLoad = newTab;
            return;
        }

        UpdateTabInBrowser(currentActiveTab, newTab);
        currentActiveTab = newTab;
        tabOnLoad = currentActiveTab;
	}

    private void UpdateTabOnLoad()
    {
        if( tabOnLoad != "" )
            UpdateTabInBrowser("sales", tabOnLoad); // browser always defaults tab to "sales"
    }

    private void UpdateTabInBrowser(string prevTab, string newTab)
    {
        if (bTex.isWebViewBusy())
            return;
        string cmd = "var lis = document.getElementsByClassName(\"domtabs\")[0].children; for(var i=0; i<lis.length; i++){ if (lis[i].firstChild.href.search(\"" + newTab + "\") > 0){ lis[i].className=\"active\"; lis[i].firstChild.parentNode.parentNode.currentLink = lis[i].firstChild; lis[i].firstChild.parentNode.parentNode.currentSection = lis[i].firstChild.href.match(/#(\\w.+)/)[1];} else lis[i].className=\"\";} document.getElementById(\"" + prevTab + IDTag + "\").parentNode.style.display = \"none\";document.getElementById(\"" + newTab + IDTag + "\").parentNode.style.display = \"block\";";
        bTex.ExecuteJavaScript(cmd);
    }
	
    public string GetDisplayValue(ProductValueType valueType)
	{
        string cmd = ProductHelper.GetDisplayValueCmd(ProductHelper.GetValueDivName(valueType, IDTag));
		return GetCmdStrResult(cmd);
	}

    // note: this may not be called on the first load, InitializeValues will though
    private void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        InitializeValues(); // keep me!
		NumFactories = ReadNumFactories();
        UpdateProductLifeCycleVal();
        if (activeStateOnLoad == false)
        {
            SetActive(false);   
            UpdateInventoryDisplay();
        }
        else
            UpdateDisplay();
    }

    public void HandleMouseUpEvent()
    {
        StartCoroutine(HandleMouseUpDelayed(waitTime[0], waitTime[1]));
    }

    // if the webview is busy (loading a page), then both checks get canceled (industry masters server has latest info, so a refresh will give us that info)
	private IEnumerator HandleMouseUpDelayed( float waitTime1, float waitTime2 )
    {
		yield return new WaitForSeconds(waitTime1);
        if (!bTex.isWebViewBusy())
        {
            dirtyFlags = (int)ProductValueType.NONE;
            UpdateValues();

            if (dirtyFlags != (int)ProductValueType.NONE)
            {
                switch (dirtyFlags)
                {
                    case (int)ProductValueType.SALES_PRICE:
                        UpdateServerWithNewValues(sales);
                        break;
                    case (int)ProductValueType.MARKETING_BUDGET:
                        UpdateMarketingBudgetDisplay();
                        UpdateServerWithNewValues(marketingBudget);
                        break;
                    case (int)ProductValueType.PRODUCTION:
                        UpdateProductionDisplay();
                        UpdateServerWithNewValues(production);
                        break;
                    case (int)ProductValueType.TAB:
                        UpdateServerWithNewTab();
                        break;
                    default:
                        Debug.Log("Missed product value, were 2 values changed? val: " + dirtyFlags);
                        break;
                }
            }
            else
            {
                if (waitTime2 > 0)
                    StartCoroutine(HandleMouseUpDelayed(waitTime2, -1));
            }
        }
    }
	
	public void UpdateServerWithNewTab()
	{
        string rmVarName = "cat" + id;
        int tabIdx = GetTabIdx(currentActiveTab);
        List<RoomVariable> roomVariables = new List<RoomVariable>();
        roomVariables.Add(new SFSRoomVariable(rmVarName, tabIdx));
        CommunicationManager.SendMsg(new SetRoomVariablesRequest(roomVariables, CommunicationManager.LastValidRoom()));
	}

    private ISFSObject CreateProductUpdateMessageWithID()
    {
        // put id in dirtyFlags int
        ProductHelper.PutIDIntoFlags(ref dirtyFlags, id);

        // build msg object
        ISFSObject productUpdateObj = new SFSObject();
        productUpdateObj.PutInt("t", dirtyFlags); // type
        return productUpdateObj;

    }
	public void UpdateServerWithNewValues(ProductVals newVals)
	{
		if (newVals.val == "")
			return;
				
        ISFSObject productUpdateObj = CreateProductUpdateMessageWithID();
        productUpdateObj.PutUtfString("v", newVals.val); // value
        productUpdateObj.PutUtfString("sv", newVals.sliderVal); // slider value
        CommunicationManager.SendObjectMsg(productUpdateObj);
	}

    private void UpdateServerPLC()
    {
        ISFSObject productUpdateObj = CreateProductUpdateMessageWithID();
        productUpdateObj.PutInt("plc", productLifeCycle); // product life cycle
        CommunicationManager.SendObjectMsg(productUpdateObj);
    }

    private void UpdateServerFactoryCount()
    {
        ISFSObject productUpdateObj = CreateProductUpdateMessageWithID();
        productUpdateObj.PutInt("nf", NumFactories); // num factories
        CommunicationManager.SendObjectMsg(productUpdateObj);
    }
    public void UpdateServerRefreshMe()
    {
        ISFSObject productUpdateObj = CreateProductUpdateMessageWithID();
        productUpdateObj.PutBool("rp", true); // refresh product
        CommunicationManager.SendObjectMsg(productUpdateObj);
    }

    private int ReadNumFactories()
    {
        string jsCmd = "var thElems = document.getElementsByTagName(\"th\"); if (thElems != null && thElems.length > 0) { thElems[0].textContent; }";
        string factoryStr = GetCmdStrResult(jsCmd);
        if (factoryStr.Equals(""))
            return -1;

        string numFactoriesStr = factoryStr.Substring(0,factoryStr.IndexOf('x'));

        return Convert.ToInt32(numFactoriesStr);
    }

    private int ReadInventory()
    {
        string jsCmd = "var fontElems = document.getElementsByTagName(\"font\"); if (fontElems != null && fontElems.length > 0) { fontElems[0].textContent; }";
        string invHTMLStr = GetCmdStrResult(jsCmd);
        if (invHTMLStr == "")
            return 0;
        int firstSpaceIdx = invHTMLStr.IndexOf(' ');
        string invStr = invHTMLStr.Substring(firstSpaceIdx, invHTMLStr.IndexOf(' ', firstSpaceIdx+1));
        string invStrNoCommas = Regex.Replace(invStr, "\\D", ""); // \D == not digits (not worrying about negative numbers for now)

        if (invStr.Contains("CO2"))
            return 0;
        else
            return Convert.ToInt32(invStrNoCommas);
    }

	public ProductVals GetTabVals(string tab)
	{
		if(tab == "marketing")
			return marketingBudget;
		else if(tab == "prod")
			return production;
		else if (tab == "sales" || tab == "competitors" || tab == "expand" || tab == "calc" || tab == "acc")
			return sales;
		else
			return blankVal;
	}

    private void UpdateProductValue(ProductValueType valueType, string newValue, string newSliderValue, ref ProductVals valsToUpdate)
    {
        UpdateSlider(ProductHelper.GetSliderElemName(valueType), newSliderValue);
        UpdateDisplayValue(ProductHelper.GetValueDivName(valueType, IDTag), newValue);
        valsToUpdate.val = newValue; 
        valsToUpdate.sliderVal = newSliderValue;
    }

    public void HandleMessage(int flags, ISFSObject msgObj)
    {
        ProductValueType valueType = ProductHelper.GetProductValueType(flags);
        
        if (valueType != ProductValueType.TAB)
        {
            string newValue = msgObj.GetUtfString("v");
            string newSliderValue = msgObj.GetUtfString("sv");
            switch (valueType)
            {
                case ProductValueType.PRODUCTION:
                    UpdateProductValue(valueType, newValue, newSliderValue, ref production);
                    UpdateProductionDisplay();
                    break;
                case ProductValueType.MARKETING_BUDGET:
                    UpdateProductValue(valueType, newValue, newSliderValue, ref marketingBudget);
                    UpdateMarketingBudgetDisplay();
                    break;
                case ProductValueType.SALES_PRICE:
                    UpdateProductValue(valueType, newValue, newSliderValue, ref sales);
                    break;
            }
        }
        else            
            Debug.LogError("Tab switches should be handled via a room variable now.");
    }

    void UpdateDisplay()
    {
        if (BizSimManager.Inst == null || BizSimManager.Inst.productMgr == null || string.IsNullOrEmpty(production.val))
            return;
        UpdateInventoryDisplay();
        UpdateProductionDisplay();
        UpdateMarketingBudgetDisplay();
        UpdateFactoryDisplay();
    }

    void UpdateInventoryDisplay()
    {
        BizSimManager.Inst.productMgr.productDisplay.UpdateInventory(TypeName, ReadInventory());
    }   

    void UpdateProductionDisplay()
    {
        BizSimManager.Inst.productMgr.productDisplay.UpdateProductionLevel(TypeName, production.GetSliderValInt());
    }

    void UpdateMarketingBudgetDisplay()
    {
        BizSimManager.Inst.productMgr.productDisplay.UpdateMarketingLevel(TypeName, marketingBudget.GetSliderValInt());
    }

    void UpdateFactoryDisplay()
    {
        BizSimManager.Inst.productMgr.productDisplay.UpdateNumFactories(TypeName, NumFactories);
    }

    // Accessors
    public int Inventory { get { return ReadInventory(); } }
    public int ProductLifeCycle { get { return productLifeCycle; } }
    public int NumFactories {
        get { return numFactories; }
        set {
            if (value == -1)
                return;
            if ((numFactories != value) && (numFactories != -1))
            {
                numFactories = value; // UpdateServerFactoryCount relies on numFactories being up to date
                UpdateServerFactoryCount();
                HandleInvestmentBudgetChange(true, false);
            }
            numFactories = value;
        }
    }
    public string IDTag{ 
        get {
            if (idTag == null)
                idTag = url.Substring(url.LastIndexOf("=") + 1); // +1 to ignore '=';
            return idTag;
        }
    }
    public string TypeName{ get { return nameFromTag[IDTag]; } }
}
