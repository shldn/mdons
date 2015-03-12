using UnityEngine;
using System.IO;
using System.Collections;

public class CompactCarScreen : ProductScreen {
    protected override void Awake()
    {
        id = (int)ProductType.COMPACT;
        base.Awake();
        bssId = CollabBrowserId.COMPACTCAR; 
        url = ProductScreen.GetProductURL("016215");
        requestReplacements.Add(BaseURL + "/images/upload/custom/greencar/automotive-compact.jpg", Directory.GetCurrentDirectory() + "/img/automotive-compact.png");
    }
}
