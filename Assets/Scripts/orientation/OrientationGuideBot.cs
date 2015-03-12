using UnityEngine;
using System.Collections;

public class OrientationGuideBot : MonoBehaviour {

    Player guide = null;
    public Transform guideSpawn = null;
    public Transform guideStageDest = null;

    float timer = 0f;

    int step = 0;
    int thisStep = 0;

	void Start () {
        if (GameManager.buildType != GameManager.BuildType.DEMO)
        {
            gameObject.SetActive(false);
            return;
        }

	    guide = LocalBotManager.Inst.Create(guideSpawn.position, guideSpawn.rotation, false, false, "VirBELA Guide");
        
	}
	
	void Update () {

        timer += Time.deltaTime;


        thisStep = 0;

        if((timer >= 2f) && Step()){
            guide.gameObject.GetComponent<AnimatorHelper>().StartAnim("Wave", false);
        }
        else if((timer >= 5f) && Step()){
            guide.playerController.SetNavDestination(guideStageDest.position, guideStageDest.eulerAngles.y);
        }

	}

    bool Step(){
        bool yep = thisStep == step;
        if(yep)
            step++;
        thisStep++;
        return yep;
    }
}
