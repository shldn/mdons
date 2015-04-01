using UnityEngine;
using System.Collections;

public class RecursiveObjectGenerator : MonoBehaviour {

    GameObject baseObject = null;
    public int numLevels = 10;
    public Transform nextLevelTransform = null;

	void Start () {
        if (baseObject == null)
            baseObject = gameObject;
        if( nextLevelTransform == null )
            nextLevelTransform = transform.Find("RecursiveTransform");
        if (numLevels <= 0)
            return;

        GameObject child = GameObject.Instantiate(baseObject) as GameObject;
        child.name = name;
        child.transform.position = nextLevelTransform.position;
        child.transform.rotation = nextLevelTransform.rotation;
        child.transform.parent = transform;
        child.transform.localScale = nextLevelTransform.localScale;
        child.GetComponent<RecursiveObjectGenerator>().nextLevelTransform = child.transform.Find(nextLevelTransform.name);
        child.GetComponent<RecursiveObjectGenerator>().numLevels = numLevels - 1;
	}
	
}
