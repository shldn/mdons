using UnityEngine;
using System.IO;
using System.Collections;

public class LuxuryCarScreen : ProductScreen {
    protected override void Awake()
    {
        id = (int)ProductType.LUXURY;
        base.Awake();
        bssId = CollabBrowserId.LUXURYCAR;
        url = ProductScreen.GetProductURL("016240");
        requestReplacements.Add(BaseURL + "/images/upload/custom/greencar/automotive-luxury.jpg", Directory.GetCurrentDirectory() + "/img/automotive-luxury.png");
    }
}
