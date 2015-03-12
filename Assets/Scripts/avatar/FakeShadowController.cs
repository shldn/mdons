using UnityEngine;
using System.Collections;

public class FakeShadowController : MonoBehaviour {

    public GameObject fakeShadowGO;
    float distToCheckBelow = 2;
	void Start () {
        enabled = GameManager.Inst.LevelLoaded != GameManager.Level.NAVTUTORIAL;
        if( fakeShadowGO == null )
        {
            Transform shadowTrans = gameObject.transform.Find("char_shadow");
            fakeShadowGO = (shadowTrans != null ) ? shadowTrans.gameObject : null;
        }
        if( fakeShadowGO != null )
            fakeShadowGO.SetActive(enabled);
	}
	
	void Update () {
        if (fakeShadowGO != null)
            fakeShadowGO.SetActive(Physics.Raycast(transform.position, -Vector3.up, distToCheckBelow));
	}

}
