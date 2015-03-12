using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MarketDataScreen : MonoBehaviour {
    private static List<MarketDataScreen> marketScreens = new List<MarketDataScreen>();
    public static List<MarketDataScreen> GetAll() { return marketScreens; }
    public int orderIndex;
    void Awake()
    {
        marketScreens.Add(this);
    }

    void OnEnable()
    {
        SetAllOthersInvisible();
    }

    void OnDestroy()
    {
        marketScreens.Remove(this);
    }

    void SetAllOthersInvisible()
    {
        foreach (MarketDataScreen screen in marketScreens)
            if (screen != this)
                screen.gameObject.SetActive(false);
    }
}
