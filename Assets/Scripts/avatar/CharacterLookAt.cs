
using UnityEngine;


public class CharacterLookAt : MonoBehaviour {
    
    // look at parameters
    public float weight = 1.0f;
    public float headWeight = 1.0f;
    public float bodyWeight = 0.1f;
    public float eyesWeight = 0.0f;
    public float clampWeight = 0.5f;

    public Animator animator = null;
    public GameObject lookAtGameObject = null;
    public Vector3 offset = Vector3.zero;

	protected virtual void Start() {
        if (animator == null)
            animator = GetComponent<Animator>();
	}
	
	void Update () {
        animator.SetLookAtPosition(lookAtGameObject.transform.position + offset);
        animator.SetLookAtWeight(weight, bodyWeight, headWeight, eyesWeight, clampWeight);
	}

    public void UseCharacterOffset()
    {
        offset = new Vector3(0, 3, 0); // to look at eyes
    }
}
