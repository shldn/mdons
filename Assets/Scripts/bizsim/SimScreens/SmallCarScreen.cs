using UnityEngine;
using System.IO;
using System.Collections;

public class SmallCarScreen : ProductScreen {
    protected override void Awake()
    {
        id = (int)ProductType.SMALL;
        base.Awake();
        bssId = CollabBrowserId.SMALLCAR; 
        url = ProductScreen.GetProductURL("5V73Ub");
        requestReplacements.Add(BaseURL + "/images/upload/custom/greencar/automotive-small.jpg", Directory.GetCurrentDirectory() + "/img/automotive-small.png");

    }
}
