
using UnityEngine;
using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Core;

public class ProductManager : MonoBehaviour {

    public Product3DDisplay productDisplay = new Product3DDisplay();
    List<ProductScreen> Products {
        get { return ProductScreen.GetAll(); }
    }

    public int NumProducts { get { return Products.Count; } }

    void Start() {
        if( GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM )
            productDisplay.Initialize();
    }

    void OnGUI()
    {
	    if (Products.Count == 0)
       		return;
		
		if( Event.current != null && Event.current.type == EventType.MouseUp )
		{
            GameObject hitObject = MouseHelpers.GetCurrentGameObjectHit();
            ProductScreen product = (hitObject != null) ? hitObject.GetComponent<ProductScreen>() : null;
            if (product != null)
                product.HandleMouseUpEvent();
		}
    }

    public void HandleMessageObject(ISFSObject msgObj)
    {
        if (msgObj.ContainsKey("sv") || msgObj.ContainsKey("rp") || msgObj.ContainsKey("nf") || msgObj.ContainsKey("plc"))
        {
            int flags = msgObj.GetInt("t");
            byte productID = ProductHelper.GetIDFromFlags(flags);
            if (productID < Products.Count)
			{
                if (msgObj.ContainsKey("nf")) // num factories
                {
                    int newNumFactories = msgObj.GetInt("nf");
                    if (newNumFactories != Products[productID].NumFactories) // remote client gets message, notices factories have increased and sends a nf message, needs refactoring
                        Products[productID].Refresh(); // will handle new factory count on load, refresh necessary to update expand options.
                }
                else if (msgObj.ContainsKey("plc"))
                {
                    int newPLC = msgObj.GetInt("plc");
                    if (newPLC != Products[productID].ProductLifeCycle) // remote client gets message, notices plc has changed and sends a plc message, needs refactoring
                        Products[productID].Refresh(); // will handle new plc value on load, could replace the image, refreshing for now.
                }
                else if (msgObj.ContainsKey("rp"))
                {
                    Products[productID].Refresh();
                }
                else
                    Products[productID].HandleMessage(flags, msgObj);
			}
            else
                Debug.LogError("Invalid productID " + productID + " Products.Count = " + Products.Count);
        }
    }

    public void SetNewTab(int productID, int tabIdx)
    {
        if (productID < 0 || productID >= Products.Count)
            return;
        Products[productID].UpdateActiveTab(tabIdx);
    }	
}
