using UnityEngine;

public class TriggerAnimation : MonoBehaviour
{
    public string animationToPlay;
    public GameObject gameObject;
    public bool animateOnce = false;
    private bool hasAnimated = false;

    void OnTriggerEnter(Collider other)
    {
        if (gameObject != null && GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject && (!animateOnce || !hasAnimated))
        {
            Animation anim = gameObject.GetComponent<Animation>();
            anim.Play(animationToPlay);
            hasAnimated = true;
        }
    }
}