using UnityEngine;
using System.Collections;

public class TreeDisplayManager {

    private GameObject tinyTrees;
    private GameObject smallTrees;
    private GameObject medTrees;
    private GameObject bigTrees;

    static TreeDisplayManager mInstance = null;
    public static TreeDisplayManager Inst
    {
        get
        {
            if (mInstance == null)
                mInstance = new TreeDisplayManager();
            return mInstance;
        }
    }

    private TreeDisplayManager()
    {
        tinyTrees = GameObject.Find("sustain_conifer_tiny");
        smallTrees = GameObject.Find("sustain_conifer_small");
        medTrees = GameObject.Find("sustain_conifer_med");
        bigTrees = GameObject.Find("sustain_conifer_big");
    }

    // trees have 4 different levels of display
    public void SetLevel(int level)
    {
        Renderer[] treeRenderers = new Renderer[0];

        smallTrees = GameObject.Find("sustain_conifer_small");
        treeRenderers = smallTrees.GetComponentsInChildren<Renderer>() as Renderer[];
        for (int i = 0; i < treeRenderers.Length; i++)
            treeRenderers[i].enabled = (level >= 2);

        medTrees = GameObject.Find("sustain_conifer_med");
        treeRenderers = medTrees.GetComponentsInChildren<Renderer>() as Renderer[];
        for (int i = 0; i < treeRenderers.Length; i++)
            treeRenderers[i].enabled = (level >= 3);

        bigTrees = GameObject.Find("sustain_conifer_big");
        treeRenderers = bigTrees.GetComponentsInChildren<Renderer>() as Renderer[];
        for (int i = 0; i < treeRenderers.Length; i++)
            treeRenderers[i].enabled = (level >= 4);

    }

    public void Enable(bool enable)
    {
        if( tinyTrees )
            tinyTrees.SetActive(enable);
        if( smallTrees )
            smallTrees.SetActive(enable);
        if( medTrees )
            medTrees.SetActive(enable);
        if( bigTrees )
            bigTrees.SetActive(enable);
    }

    public void Destroy()
    {
        mInstance = null;
    }
}
