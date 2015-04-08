using UnityEngine;
using System.Collections;

public class RecursiveObjectGenerator : MonoBehaviour {

    GameObject baseObject = null;
    public int numLevels = 10;
    public Transform nextLevelTransform = null;
    public GameObject objToTransform = null;

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

        // To help copy results without generating more.
        //Destroy(this);

        if( objToTransform != null )
        {
            GameObject childTest = GameObject.Instantiate(objToTransform) as GameObject;
            childTest.transform.position = objToTransform.transform.position;
            childTest.transform.rotation = objToTransform.transform.rotation;
            if( numLevels != 2)
                RecurseTransform(childTest.transform, -1);
        }
	}

    public void RecurseTransform(Transform t, int level)
    {
        if (level == 0)
            return;
        if( level > 0 )
        {
            // Find offset relative to parent
            Vector3 posOffset = (t.position - transform.position);

            t.position = nextLevelTransform.position + nextLevelTransform.localScale.x * (nextLevelTransform.rotation * posOffset);
            t.rotation = nextLevelTransform.rotation;

            // ignoring possibility of different scales in different dimensions.
            float newScale = t.localScale.x * nextLevelTransform.localScale.x;
            t.localScale = new Vector3(newScale, newScale, newScale);
            RecurseTransform(t, level - 1);
        }
        else
        {
            // Find offset relative to parent
            Vector3 posOffset = (t.position - transform.position);

            Quaternion rotToHigherLevel = Quaternion.Inverse(transform.rotation);
            float scaleToHigherLevel = 1.0f / nextLevelTransform.localScale.x;
            Vector3 posOffsetToHigherLevel = -nextLevelTransform.position;


            t.position = transform.parent.position + (rotToHigherLevel * (scaleToHigherLevel * posOffset));

            //t.rotation = nextLevelTransform.rotation;

            // ignoring possibility of different scales in different dimensions.
            float newScale = t.localScale.x * nextLevelTransform.localScale.x;
            //t.localScale = new Vector3(newScale, newScale, newScale);
            //RecurseTransform(t, level - 1);

        }
    }
	
}
