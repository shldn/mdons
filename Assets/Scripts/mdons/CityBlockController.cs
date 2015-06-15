using UnityEngine;
using System.Collections;

public class CityBlockController : MonoBehaviour {

    CityBlockGenerator cityGenOut = null;
    CityBlockGenerator cityGenIn = null;
    public float scaleDiffMax = 4f;
    public bool autoPopulate = true;
    public bool autoCycle = true;

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
            float invBlockScale = 1f / cityGenOut.BlockScale;
            //Debug.LogError("bounds: " + cityGenOut.IgnoreBounds + " inv s: " + invScale + " inv bs: " + invBlockScale + " playerPos: " + playerXZPos + " scaled: " + (invBlockScale * (invScale * playerXZPos)));
            if (Time.frameCount > 10 && CityLevelsInitialized() && !cityGenOut.IgnoreBounds.Contains(invBlockScale * (invScale * playerXZPos)))
            {
                if( autoCycle )
                    CycleUp();
                else
                {
                    cityGenOut.CreateNextHigherLevel(transform);
                    cityGenOut = cityGenOut.higherLevel.GetComponent<CityBlockGenerator>();
                }
            }

            //Debug.LogError(cityGenIn.blockScale + " scale diff: " + (Mathf.Log10(cityGenIn.blockScale * transform.lossyScale.x)) + " " + Mathf.Log10(GameManager.Inst.LocalPlayer.Scale.x));
            if (Time.frameCount > 10 && CityLevelsInitialized() && Mathf.Abs((Mathf.Log10(cityGenIn.BlockScale * transform.lossyScale.x) - Mathf.Log10(GameManager.Inst.LocalPlayer.Scale.x))) < scaleDiffMax)
            {
                if (autoCycle)
                    CycleDown();
                else
                {
                    cityGenIn.CreateNextLowerLevel();
                    cityGenIn = cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>();
                }
            }
        }

        if(Input.GetKeyUp(KeyCode.Alpha9))
            CycleUp();
        if (Input.GetKeyUp(KeyCode.Alpha8))
            CycleDown();
        if (Input.GetKeyUp(KeyCode.Alpha5))
            RebalanceCityScale();
	}

    // Try to stay within a good floating point range.
    // 
    void RebalanceCityScale()
    {
        FindHighestAndLowestCityLevels();

        // Set everything back to how it was originally - Controller has a scale of 1.
        Vector3 currScale = transform.localScale;

        // Multiply all levels
        GameObject walker = cityGenIn.gameObject;
        while(walker != null)
        {
            walker.GetComponent<CityBlockGenerator>().BlockScale *= currScale.x;
            walker = walker.GetComponent<CityBlockGenerator>().higherLevel;
        }

        // Adjust top level for multiplication
        transform.localScale = Vector3.one;

    }

    // Take second smallest piece and move it to the new highest position
    void CycleUp()
    {
        GameObject cityToMove = cityGenIn.higherLevel;
        Transform meshContainer = cityToMove.transform.FindChild("city_meshes");
        Vector3 currScale = meshContainer.localScale;
        cityToMove.GetComponent<CityBlockGenerator>().BlockScale = cityGenOut.GetNextHigherLevelScale();

        // grow center piece to fill hole
        cityGenIn.BlockScale = currScale.x;
 
        // Remap all the pointers
        cityGenIn.higherLevel = cityToMove.GetComponent<CityBlockGenerator>().higherLevel;
        cityGenIn.higherLevel.GetComponent<CityBlockGenerator>().lowerLevel = cityGenIn.gameObject;
        cityToMove.GetComponent<CityBlockGenerator>().lowerLevel = cityGenOut.gameObject;
        cityToMove.GetComponent<CityBlockGenerator>().higherLevel = null;
        cityGenOut.higherLevel = cityToMove;

        cityGenOut = cityToMove.GetComponent<CityBlockGenerator>();

        if (transform.localScale.x < 0.01f)
            RebalanceCityScale();
    }

    // Take highest position block and move it down to the second smallest
    void CycleDown()
    {
        GameObject cityToMove = cityGenOut.gameObject;
        Transform meshContainer = cityToMove.transform.FindChild("city_meshes");
        Vector3 currScale = meshContainer.localScale;
        cityToMove.GetComponent<CityBlockGenerator>().BlockScale = cityGenIn.BlockScale;

        // shrink center piece
        cityGenIn.BlockScale = cityGenIn.GetNextLowerLevelScale();

        // Remap all the pointers
        cityGenOut = cityToMove.GetComponent<CityBlockGenerator>().lowerLevel.GetComponent<CityBlockGenerator>();

        cityToMove.GetComponent<CityBlockGenerator>().higherLevel = cityGenIn.higherLevel;
        cityToMove.GetComponent<CityBlockGenerator>().lowerLevel.GetComponent<CityBlockGenerator>().higherLevel = null;

        cityGenIn.higherLevel.GetComponent<CityBlockGenerator>().lowerLevel = cityToMove.gameObject;
        cityGenIn.higherLevel = cityToMove;

        if (transform.localScale.x > 100f)
            RebalanceCityScale();
        
    }

    void FindHighestAndLowestCityLevels()
    {
        while (cityGenOut.higherLevel != null)
            cityGenOut = cityGenOut.higherLevel.GetComponent<CityBlockGenerator>();

        while (cityGenIn.lowerLevel != null)
            cityGenIn = cityGenIn.lowerLevel.GetComponent<CityBlockGenerator>();
    }

    bool CityLevelsInitialized()
    {
        FindHighestAndLowestCityLevels();
        return cityGenIn.Initialized && cityGenOut.Initialized;
    }
}
