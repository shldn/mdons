using UnityEngine;
using System.Collections;

public class ProductHelper {

    public static string GetSliderPositionCmd(string sliderElementName)
    {
        if (sliderElementName == null || sliderElementName == "")
            return "";
        return "var elem = document.getElementById(\"" + sliderElementName + "\"); if(elem != null && elem.style != null){elem.style.left;}else{null;}";
    }

    public static string GetDisplayValueCmd(string elemName)
    {
        if (elemName == null || elemName == "")
            return "";
        return "var elem = document.getElementById(\"" + elemName + "\"); if(elem != null && elem.firstChild != null && elem.firstChild.firstChild != null){elem.firstChild.firstChild.textContent;}else{null;}";
    }

    public static string GetSliderElemName(ProductValueType valueType)
    {
        switch (valueType)
        {
            case ProductValueType.SALES_PRICE:
                return "sl0slider";
            case ProductValueType.MARKETING_BUDGET:
                return "sl1slider";
            case ProductValueType.PRODUCTION:
                return "sl2slider";
            default:
                Debug.LogError("Unhandled product id: " + valueType); // should assert instead.
                return "";
        }
    }

    public static string GetValueDivName(ProductValueType valueType, string idTag)
    {
        string productID = "";
        switch (valueType)
        {
            case ProductValueType.SALES_PRICE:
                productID = "P";
                break;
            case ProductValueType.MARKETING_BUDGET:
                productID = "M";
                break;
            case ProductValueType.PRODUCTION:
                productID = "L";
                break;
            default:
                Debug.LogError("Unhandled product id: " + valueType); // should assert instead.
                return "";
        }
        return "ajaxDiv_" + productID + "_" + idTag;
    }

    public static byte GetIDFromFlags(int flags)
    {
        return (byte)(flags >> 8);
    }

    public static void PutIDIntoFlags(ref int flags, int id)
    {
        flags |= (id << 8);
    }

    public static ProductValueType GetProductValueType(int flags)
    {
        int mask = 0xFF;
        return (ProductValueType)(flags & mask);
    }
}
