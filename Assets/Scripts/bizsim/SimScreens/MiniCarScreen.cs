using UnityEngine;
using System.IO;
using System.Collections;

public class MiniCarScreen : ProductScreen {
    protected override void Awake()
    {
        id = (int)ProductType.MINI;
        base.Awake();
        bssId = CollabBrowserId.MINICAR; 
        url = ProductScreen.GetProductURL("016200");
        requestReplacements.Add(BaseURL + "/images/upload/custom/greencar/automotive-mini.jpg", Directory.GetCurrentDirectory() + "/img/automotive-mini.png");
    }
}
