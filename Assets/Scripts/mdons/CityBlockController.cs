using UnityEngine;
using System.Collections;

public class CityBlockController : MonoBehaviour {

    CityBlockGenerator cityGenOut = null;
    CityBlockGenerator cityGenIn = null;

	void Start () {
        cityGenOut = GetComponent<CityBlockGenerator>();
        cityGenIn = cityGenOut;

        while (cityGenOut.higherLevel != null && cityGenOut.higherLevel.GetComponent<CityBlockGenerator>().higherLevel != null)
            cityGenOut.higherLevel = cityGenOut.higherLevel.GetComponent<CityBlockGenerator>().higherLevel;

        while (cityGenIn.lowerLevel != null && cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>().lowerLevel != null)
            cityGenIn.lowerLevel = cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>().lowerLevel;

	}
	
	void Update () {
        Vector3 playerXZPos = GameManager.Inst.LocalPlayer.gameObject.transform.position;
        playerXZPos.y = 0f;
        if (!cityGenOut.IgnoreBounds.Contains(playerXZPos))
        {
            cityGenOut.CreateNextHigherLevel(transform);
            cityGenOut = cityGenOut.higherLevel.GetComponent<CityBlockGenerator>();
        }
	}
}
