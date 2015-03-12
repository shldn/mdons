
public class MiniCarDetailScreen : BizSimScreen {
    protected override void Awake()
    {
        base.Awake();
        removeTitle = false;
        disableScrolling = false;
        url = BaseURL + "/licenceinfo.php?licid=" + "016200";
        url = "https://docs.google.com/document/d/1HO-M5C3OJeAF6N3ZAPpGjGQQ4DShPiHj_hXcxiDK_LQ";
//        url = "https://docs.google.com/spreadsheet/ccc?key=0AoQxIM08pIJldGVQcmJCd2FELVp0ZFZ2TE9xRS02enc&rm=minimal#gid=0";
//        url = "http://openetherpad.org/p/VirBELA";
        blacklistRequestURLFragments.Add("holdinginfo.php?playerid");
    }
}
