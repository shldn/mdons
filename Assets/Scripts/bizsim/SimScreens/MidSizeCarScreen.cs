using UnityEngine;
using System.IO;
using System.Collections;

public class MidSizeCarScreen : ProductScreen {
    protected override void Awake()
    {
        id = (int)ProductType.MIDSIZE;
        base.Awake();
        bssId = CollabBrowserId.MIDSIZECAR;
        url = ProductScreen.GetProductURL("016210");
        requestReplacements.Add(BaseURL + "/images/upload/custom/greencar/automotive-mid-size.jpg", Directory.GetCurrentDirectory() + "/img/automotive-midsize.png");
    }
}
