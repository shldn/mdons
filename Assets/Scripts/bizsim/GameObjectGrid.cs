using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Class to make a 2D Grid of Game Objects, will grow in the initial direction first, 
// then out infinitely in the secondary direction as objects are added.
public class GameObjectGrid : MonoBehaviour {

    private Stack<GameObjectStack> gameObject2DStack = new Stack<GameObjectStack>();
    private int numObjects = 0;
    
    public Vector3 InitialGrowDirection { get; set; }
    public Vector3 SecondaryGrowDirection { get; set; } // when the initial grow direction reaches it's limit, start a new row offset in the secondary direction
    public int InitialDirectionMaxObjects { get; set; } // when the max is reached in the initial direction, start a new row.
    public float InitialDirSpacingDist { get; set; }
    public float SecondaryDirSpacingDist { get; set; }

    public string PrefabName { get; set; }

    public int NumObjects {
        get { return numObjects; }
        set
        {
            if (numObjects == value || value < 0)
                return;

            if (gameObject2DStack.Count == 0)
                AddStack();

            while ((InitialDirectionMaxObjects * (gameObject2DStack.Count - 1)) + gameObject2DStack.Peek().NumObjects < value)
            {
                if (gameObject2DStack.Peek().NumObjects == InitialDirectionMaxObjects)
                    AddStack();
                gameObject2DStack.Peek().NumObjects = gameObject2DStack.Peek().NumObjects + 1;
            }
            while (gameObject2DStack.Count != 0 && (InitialDirectionMaxObjects * (gameObject2DStack.Count - 1)) + gameObject2DStack.Peek().NumObjects > value)
            {
                gameObject2DStack.Peek().NumObjects = gameObject2DStack.Peek().NumObjects - 1;
                if (gameObject2DStack.Peek().NumObjects == 0)
                    gameObject2DStack.Pop();
            }
            numObjects = value;
        }
    }

    private void AddStack()
    {
        GameObjectStack newStack = gameObject.AddComponent<GameObjectStack>();
        newStack.DropDirection = InitialGrowDirection;
        newStack.DropPointSpacingDist = InitialDirSpacingDist;
        newStack.PositionOffset = SecondaryGrowDirection * SecondaryDirSpacingDist * gameObject2DStack.Count;
        newStack.PrefabName = PrefabName;
        gameObject2DStack.Push(newStack);
    }

    public IEnumerator<GameObjectStack> GetEnumerator() { return gameObject2DStack.GetEnumerator(); }
}
