using UnityEngine;
using System.Collections;

public class CityBlockController : MonoBehaviour {

    CityBlockGenerator cityGenOut = null;
    CityBlockGenerator cityGenIn = null;
    public bool autoPopulate = true;

	void Start () {
        cityGenOut = GetComponent<CityBlockGenerator>();
        cityGenIn = cityGenOut;

        FindHighestAndLowestCityLevels();
	}
	
	void Update () {

        if (autoPopulate)
        {
            FindHighestAndLowestCityLevels();

            Vector3 playerXZPos = GameManager.Inst.LocalPlayer.gameObject.transform.position - transform.position;
            playerXZPos.y = 0f;
            float invScale = 1f / transform.lossyScale.x;
            float invBlockScale = 1f / cityGenOut.blockScale;
            //Debug.LogError("bounds: " + cityGenOut.IgnoreBounds + " inv s: " + invScale + " inv bs: " + invBlockScale + " playerPos: " + playerXZPos + " scaled: " + (invBlockScale * (invScale * playerXZPos)));
            if (!cityGenOut.IgnoreBounds.Contains(invBlockScale * (invScale * playerXZPos)))
            {
                cityGenOut.CreateNextHigherLevel(transform);
                cityGenOut = cityGenOut.higherLevel.GetComponent<CityBlockGenerator>();
            }

            //Debug.LogError(cityGenIn.blockScale + " scale diff: " + (Mathf.Log10(cityGenIn.blockScale * transform.lossyScale.x)) + " " + Mathf.Log10(GameManager.Inst.LocalPlayer.Scale.x));
            if (Time.frameCount > 10 && Mathf.Abs((Mathf.Log10(cityGenIn.blockScale * transform.lossyScale.x) - Mathf.Log10(GameManager.Inst.LocalPlayer.Scale.x))) < 4f)
            {
                cityGenIn.CreateNextLowerLevel();
                cityGenIn = cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>();
            }
        }

        if(Input.GetKeyUp(KeyCode.Alpha9))
            CycleUp();
        if (Input.GetKeyUp(KeyCode.Alpha8))
            CycleDown();
	}

    // Take second smallest piece and move it to the new highest position
    void CycleUp()
    {
        GameObject cityToMove = cityGenIn.higherLevel;
        Transform meshContainer = cityToMove.transform.FindChild("city_meshes");
        Vector3 currScale = meshContainer.localScale;
        cityToMove.GetComponent<CityBlockGenerator>().blockScale = cityGenOut.GetNextHigherLevelScale();
        meshContainer.localScale = cityGenOut.GetNextHigherLevelScale() * Vector3.one;

        // grow center piece to fill hole
        cityGenIn.blockScale = currScale.x;
        cityGenIn.transform.FindChild("city_meshes").localScale = currScale;

        // Remap all the pointers
        cityGenIn.higherLevel = cityToMove.GetComponent<CityBlockGenerator>().higherLevel;
        cityGenIn.higherLevel.GetComponent<CityBlockGenerator>().lowerLevel = cityGenIn.gameObject;
        cityToMove.GetComponent<CityBlockGenerator>().lowerLevel = cityGenOut.gameObject;
        cityToMove.GetComponent<CityBlockGenerator>().higherLevel = null;
        cityGenOut.higherLevel = cityToMove;

        cityGenOut = cityToMove.GetComponent<CityBlockGenerator>();
    }

    // Take highest position block and move it down to the second smallest
    void CycleDown()
    {
        GameObject cityToMove = cityGenOut.gameObject;
        Transform meshContainer = cityToMove.transform.FindChild("city_meshes");
        Vector3 currScale = meshContainer.localScale;
        cityToMove.GetComponent<CityBlockGenerator>().blockScale = cityGenIn.blockScale;
        meshContainer.localScale = cityGenIn.blockScale * Vector3.one;

        // shrink center piece
        cityGenIn.blockScale = cityGenIn.GetNextLowerLevelScale();
        cityGenIn.transform.FindChild("city_meshes").localScale = cityGenIn.blockScale * Vector3.one;

        // Remap all the pointers
        cityGenOut = cityToMove.GetComponent<CityBlockGenerator>().lowerLevel.GetComponent<CityBlockGenerator>();

        cityToMove.GetComponent<CityBlockGenerator>().higherLevel = cityGenIn.higherLevel;
        cityToMove.GetComponent<CityBlockGenerator>().lowerLevel.GetComponent<CityBlockGenerator>().higherLevel = null;

        cityGenIn.higherLevel.GetComponent<CityBlockGenerator>().lowerLevel = cityToMove.gameObject;
        cityGenIn.higherLevel = cityToMove;

        
    }

    void FindHighestAndLowestCityLevels()
    {
        while (cityGenOut.higherLevel != null && cityGenOut.higherLevel.GetComponent<CityBlockGenerator>().higherLevel != null)
            cityGenOut = cityGenOut.higherLevel.GetComponent<CityBlockGenerator>().higherLevel.GetComponent<CityBlockGenerator>();

        while (cityGenIn.lowerLevel != null && cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>().lowerLevel != null)
            cityGenIn = cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>().lowerLevel.GetComponent<CityBlockGenerator>();
    }
}
