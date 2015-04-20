using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScaleAvatarOnCollision : MonoBehaviour {

    public Transform scaleTransform = null;
    public float scaleAdjust = 1.0f;
    public static HashSet<ScaleAvatarOnCollision> used = new HashSet<ScaleAvatarOnCollision>();

    void Start()
    {
        if (scaleTransform != null)
            scaleAdjust = scaleTransform.localScale.y;
    }

    void OnCollisionEnter(Collision c)
    {
        if (GameManager.Inst.LocalPlayer.gameObject == c.gameObject && !used.Contains(this))
        {
            GameManager.Inst.LocalPlayer.Scale *= scaleAdjust;
            used.Add(this);
        }
    }
}
