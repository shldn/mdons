using UnityEngine;
using System.Collections;

public class ShepardEngine : MonoBehaviour {

	public static ShepardEngine Inst;

	public AudioClip tone = null;
	AudioSource[] sources;
	public int numVoices = 3;
	public float loopTime = 40f;
	public float minPitch = 0f;
	public float maxPitch = 1f;
	float volume = 0f;
	bool changing = false;

	float timeElapsed = 0f;
	public float velocity = 1f;


	void Awake(){
		Inst = this;
	} // End of Awake().


	void Start(){
		sources = new AudioSource[numVoices];
		for(int i = 0; i < numVoices; i++){
			AudioSource newSource = gameObject.AddComponent<AudioSource>();
			sources[i] = newSource;
			newSource.clip = tone;
			newSource.loop = true;
			newSource.Play();
		}
	} // End of Start().
	
	
	void Update(){
		velocity = Mathf.MoveTowards(velocity, 0f, Time.deltaTime);

		volume = Mathf.Lerp(volume, changing? 1f : 0f, Time.deltaTime);
		changing = false;
	} // End of Update().


	public void SetVelocity(float newVelocity){
		velocity = newVelocity;
		changing = true;
	} // End of SetVelocity().


	void FixedUpdate(){
		timeElapsed += Time.fixedDeltaTime * velocity;

		for(int i = 0; i < sources.Length; i++){
			AudioSource curSource = sources[i];
			float runner = Mathf.Repeat((timeElapsed / loopTime) + ((1f / numVoices) * i), 1f);
			curSource.pitch = minPitch + (runner * (maxPitch - minPitch));
			curSource.volume = (1f - (0.5f + (Mathf.Cos(runner * Mathf.PI * 2f) * 0.5f))) / Mathf.Sqrt(numVoices * 0.5f) * volume;
		}
	} // End of FixedUpdate().

} // End of ShepardEngine.
