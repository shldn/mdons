using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Product3DDisplay {

    Dictionary<string, GameObjectGrid> inventoryCarGrid = new Dictionary<string, GameObjectGrid>();
    Dictionary<string, GameObjectGrid> factoryGrids = new Dictionary<string, GameObjectGrid>();
    Dictionary<string, GameObject> productionGOs = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> marketingGOs = new Dictionary<string, GameObject>();
    string[] types = new string[] { "mini", "small", "compact", "midsize", "luxury" };

    string currentProductName = "";
    int numFactories = 0;
    bool initialized = false;
    bool polluting = false;
    int maxInventoryToDisplay = 17 * 4;

    string GetInventoryPrefab(string typeName)
    {
        return "cars/car_" + typeName;
    }

    string GetMktObjName(string typeName)
    {
        return "mkt_" + typeName;
    }

    string GetScrnIconObjName(string typeName)
    {
        return "bizsim/screen_icons/car_" + typeName;
    }

    string GetProductionObjName(string typeName)
    {
        return "bizsim/prod_bld/prod_bld_" + typeName;
    }

    bool SimSupported(string simType)
    {
        return simType == "uimx_sustain";
    }

    public void Initialize()
    {
        if (initialized)
            return;

        if (SimSupported(BizSimManager.simType))
        {
            for (int i = 0; i < types.Length; ++i)
            {
                GameObject go = (GameObject)GameObject.Instantiate(Resources.Load(GetProductionObjName(types[i])));
                SetAnimationSpeed(go, 0.0f);
                productionGOs.Add(types[i], go);

                go = GameObject.Find(GetMktObjName(types[i]));
                marketingGOs.Add(types[i], go);
            }
        }

        initialized = true;
    }

    public void DisplayScreenIcons()
    {
        for (int i = 0; i < types.Length; ++i)
            GameObject.Instantiate(Resources.Load(GetScrnIconObjName(types[i])));
    }

    GameObjectGrid AddInventoryGrid(string productName)
    {
        string prefabName = GetInventoryPrefab(productName);
        GameObject go = (GameObject)GameObject.Instantiate(Resources.Load(prefabName));

        GameObjectGrid newInvCarGrid = BizSimManager.Inst.productMgr.gameObject.AddComponent<GameObjectGrid>();
        newInvCarGrid.InitialGrowDirection = go.transform.right;
        newInvCarGrid.SecondaryGrowDirection = go.transform.forward;
        newInvCarGrid.InitialDirectionMaxObjects = 4;
        newInvCarGrid.InitialDirSpacingDist = -1.5f;
        newInvCarGrid.SecondaryDirSpacingDist = 1f;
        newInvCarGrid.PrefabName = prefabName;

        Object.Destroy(go);
        inventoryCarGrid.Add(productName, newInvCarGrid);

        return newInvCarGrid;
    }

    public void UpdateInventory(string productType, int inventory)
    {
        GameObjectGrid invGrid;
        if (!inventoryCarGrid.TryGetValue(productType, out invGrid))
            invGrid = AddInventoryGrid(productType);
        invGrid.NumObjects = Mathf.Min(maxInventoryToDisplay, inventory);
    }

    public void UpdateNumFactories(string productType, int newNumFactories)
    {
        GameObjectGrid factoryGrid;
        // If no factory grid exists, create one.
        if (!factoryGrids.TryGetValue(productType, out factoryGrid))
        {
            string prefabName = "factory/factory_" + productType;
            GameObject go = (GameObject)GameObject.Instantiate(Resources.Load(prefabName));

            factoryGrid = BizSimManager.Inst.gameObject.AddComponent<GameObjectGrid>();
            factoryGrid.PrefabName = prefabName;
            factoryGrid.InitialGrowDirection = go.transform.forward;
            factoryGrid.SecondaryGrowDirection = -go.transform.right;
            factoryGrid.InitialDirectionMaxObjects = 6;
            factoryGrid.InitialDirSpacingDist = -2f;
            factoryGrid.SecondaryDirSpacingDist = 1.8f;
            Object.Destroy(go);
            factoryGrids.Add(productType, factoryGrid);
        }
        BizSimManager.Inst.UpdateAllFactoryPollutionVisuals(BizSimManager.Inst.Polluting); // make sure new factories display pollution correctly

        // If adding a factory, play FactorySpawn clip.
        if (factoryGrid.NumObjects > 0 && newNumFactories > factoryGrid.NumObjects)
            SoundManager.Inst.PlayFactorySpawn();

        factoryGrid.NumObjects = newNumFactories;

    }

    public void UpdateMarketingLevel(string productType, int newSliderVal)
    {
        Initialize();
        GameObject go;
        if (marketingGOs.TryGetValue(productType, out go))
        {
            float maxScaleVal = 1.0f; // set in editor as initial scale
            float scaleVal = maxScaleVal * ((float)(newSliderVal)) / (float)ProductScreen.maxSliderVal;
            go.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
        }
        else
            Debug.LogError("marketing level object: " + productType + " not found");
    }

    public void UpdateProductionLevel(string productType, int newSliderVal)
    {
        Initialize();
        GameObject go;
        if (productionGOs.TryGetValue(productType, out go))
            SetAnimationSpeed(go, (float)(newSliderVal) / (float)ProductScreen.maxSliderVal);
        else
            Debug.LogError("production level object: " + productType + " not found");
    }

    private void SetAnimationSpeed(GameObject go, float newSpeed)
    {
        if (go == null)
            return;
        Animation anim = go.GetComponent<Animation>();
        if( anim != null )
            foreach (AnimationState state in anim)
                state.speed = newSpeed;
    }
}
