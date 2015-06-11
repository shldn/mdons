using UnityEngine;
using System.Collections;

public class CityBlockController : MonoBehaviour {

    CityBlockGenerator cityGenOut = null;
    CityBlockGenerator cityGenIn = null;

	void Start () {
        cityGenOut = GetComponent<CityBlockGenerator>();
        cityGenIn = cityGenOut;

        FindHighestAndLowestCityLevels();
	}
	
	void Update () {
        Vector3 playerXZPos = GameManager.Inst.LocalPlayer.gameObject.transform.position - transform.position;
        playerXZPos.y = 0f;

        FindHighestAndLowestCityLevels();
        float invScale = 1f / transform.lossyScale.x;
        if (!cityGenOut.IgnoreBounds.Contains(invScale * playerXZPos))
        {
            cityGenOut.CreateNextHigherLevel(transform);
            cityGenOut = cityGenOut.higherLevel.GetComponent<CityBlockGenerator>();
        }

        //Debug.LogError(cityGenIn.objScale + " scale diff: " + (Mathf.Log10(cityGenIn.objScale * transform.lossyScale.x)) + " " + Mathf.Log10(GameManager.Inst.LocalPlayer.Scale.x));
        if (Time.frameCount > 10 && Mathf.Abs((Mathf.Log10(cityGenIn.objScale * transform.lossyScale.x) - Mathf.Log10(GameManager.Inst.LocalPlayer.Scale.x))) < 4f)
        {
            cityGenIn.CreateNextLowerLevel();
            cityGenIn = cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>();
        }
	}

    void FindHighestAndLowestCityLevels()
    {
        while (cityGenOut.higherLevel != null && cityGenOut.higherLevel.GetComponent<CityBlockGenerator>().higherLevel != null)
            cityGenOut = cityGenOut.higherLevel.GetComponent<CityBlockGenerator>().higherLevel.GetComponent<CityBlockGenerator>();

        while (cityGenIn.lowerLevel != null && cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>().lowerLevel != null)
            cityGenIn = cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>().lowerLevel.GetComponent<CityBlockGenerator>();
    }
}
