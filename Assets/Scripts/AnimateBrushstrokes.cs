using UnityEngine;
using System.Collections;

public class AnimateBrushstrokes : MonoBehaviour 
{

	public Texture2D[] textures;
	public float speed = 1;
	int counter = 0;
	
	
	// Grab the renderer
	void Start () 
	{
		        		
		//calls the changeTex function
		changeTex();
		
		InvokeRepeating("changeTex", 0, speed);
		
    }
	
	//	
    void changeTex() {
		
	    GetComponent<Renderer>().material.mainTexture = (textures[counter]);
		
		counter = (counter + 1);
		
		if(counter >= textures.Length){
			counter = 0;
		}
	}
    
}

