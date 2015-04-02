using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Animation))]
public class ClickToAnimate : MonoBehaviour {

    public string animationToPlay;
    public bool alternateDir = false;
    public bool startBackwards = false;
    private int clickCount = 0;

    void Start()
    {
        clickCount = startBackwards ? 1 : 0;
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseUp)
        {
            GameObject hitObject = MouseHelpers.GetCurrentGameObjectHit();
            if (hitObject == gameObject)
            {
                Animation anim = GetComponent<Animation>();
                anim.GetComponent<Animation>()[animationToPlay].speed = (!alternateDir || clickCount % 2 == 0) ? 1 : -1;
                anim.GetComponent<Animation>()[animationToPlay].time = (!alternateDir || clickCount % 2 == 0) ? 0 : anim.GetComponent<Animation>()[animationToPlay].length;
                anim.Play(animationToPlay);
                ++clickCount;
            }
        }
    }
}
