using UnityEngine;
using System.Collections;

// Require a character controller to be attached to the same game object
public class CharacterAnimator : MonoBehaviour {

    public AnimationClip idleAnimation;
    public AnimationClip walkAnimation;
    public AnimationClip runAnimation;
    public AnimationClip jumpPoseAnimation;

    public float walkMaxAnimationSpeed = 0.75f;
    public float trotMaxAnimationSpeed = 1.0f;
    public float runMaxAnimationSpeed = 1.0f;
    public float jumpAnimationSpeed = 1.15f;
    public float landAnimationSpeed = 1.0f;

    private Animation _animation;
    private int _currentAnimation;

    void Awake ()
    {
	    _currentAnimation = (int)CharacterState.Idle;
	    _animation = GetComponent<Animation>();
	    if(!_animation)
		    Debug.Log("The character you would like to control doesn't have animations. Moving her might look weird.");

	    if(!idleAnimation) {
		    _animation = null;
		    Debug.Log("No idle animation found. Turning off animations.");
	    }
	    if(!walkAnimation) {
		    _animation = null;
		    Debug.Log("No walk animation found. Turning off animations.");
	    }
	    if(!runAnimation) {
		    _animation = null;
		    Debug.Log("No run animation found. Turning off animations.");
	    }
	    if(!jumpPoseAnimation) {
		    _animation = null;
		    Debug.Log("No jump animation found and the character has canJump enabled. Turning off animations.");
	    }
    }


    void Update() {
	    // ANIMATION sector
	    if(_animation) {
            int t = (int)CharacterState.Jumping;
		    if(_currentAnimation == t) 
		    {
			    _animation[jumpPoseAnimation.name].speed = landAnimationSpeed;
			    _animation[jumpPoseAnimation.name].wrapMode = WrapMode.ClampForever;
			    _animation.CrossFade(jumpPoseAnimation.name);				
		    }
            t = (int)CharacterState.Idle;
		    if(_currentAnimation == t) {
			    _animation.CrossFade(idleAnimation.name);
		    }
            t = (int)CharacterState.Running;
		    if(_currentAnimation == t) {
			    _animation[runAnimation.name].speed = runMaxAnimationSpeed;
			    _animation.CrossFade(runAnimation.name);	
		    }
            t = (int)CharacterState.Trotting;
		    if(_currentAnimation == t) {
			    _animation[walkAnimation.name].speed = trotMaxAnimationSpeed;
			    _animation.CrossFade(walkAnimation.name);	
		    }
            t = (int)CharacterState.Walking;
		    if(_currentAnimation == t) {
			    _animation[walkAnimation.name].speed = walkMaxAnimationSpeed;
			    _animation.CrossFade(walkAnimation.name);	
		    }
	    }
	    // ANIMATION sector
	
    }


    public int GetCurrentAnimation()
    {
	    return _currentAnimation;
    }

    public void SetCurrentAnimation(int state)
    {
	    _currentAnimation = state;
    }
}
