using UnityEngine;
using System.Collections;
using LSL;
using System.Threading;
using DrowsinessDetection;
//using System.Collections;
/// <summary>
/// Rlllllll
/// </summary>

public class ReceiveLSL : MonoBehaviour {
	//LSL
	private static liblsl.StreamInlet inlet;
	private static liblsl.StreamInfo[] results ;
	private static float[] sample;// = new float[8];
	private static string t;
	private static bool INITIAL = false;
	private static bool STOP_RECEIVING = false;
	private static bool STOP_RETRIVING = false;
	Thread ReceivingFromLSL = new Thread(new ThreadStart(receiving));
	Thread RetrivingFromBuffer = new Thread (new ThreadStart (retriving));
	
	//for GUI
	public int rotateSpeed;
	//public GUIText dispText;
	
	//rotation the cube
	private static float[] gyroXmean,gyroYmean;
	private static float gyroX,gyroY;
	private static float rotationX,rotationY;
	private static int gyroIndex,gyroIndexMax=Buffer.SRATE;
	private static bool gyroTag;
	
	//EPOC
	private static Buffer buffer;

	//drowsiness detection
	private static FeatureExtraction fe;

	void OnDestroy() {
		if (!INITIAL) {
			INITIAL = true;
		} else {
			STOP_RECEIVING = true;
			STOP_RETRIVING = true;
		}
		if(inlet != null)
			inlet.close_stream ();
		print ("Stream closed");
	}
	void OnMouseDown() {
		//STOP_RECEIVING = true;
		//STOP_RETRIVING = true;
		print ("mouse down");
	}
	// Use this for initialization
	void Start () {
		//EPOC..
		buffer = new Buffer ();
		fe = new FeatureExtraction ();
		sample = new float[Buffer.CH];
		gyroXmean = new float[gyroIndexMax];
		gyroYmean = new float[gyroIndexMax];
		gyroIndex = 0;
		gyroTag = false;
		gyroX = gyroY = 0;
		rotationX = rotationY = 0;
		// create stream info and outlet
		//dispText.text = "initial";
		ReceivingFromLSL.Start();
		RetrivingFromBuffer.Start ();
	}
	void Update(){
		transform.Rotate (new Vector3 (rotationY, rotationX, 0) * rotateSpeed * Time.deltaTime);
		rotationX = 0;
		rotationY = 0;
	}
	void FixedUpdate()
	{
		//dispText.text = t;
	}
	float getGyroX(){
		return gyroX;
	}
	float getGyroY(){
		return gyroY;
	}
	static void updataGyro(float x,float y){
		//calculate the mean gyro_x and gyro_y using 1sec data 
		if (!gyroTag) {
			gyroXmean [gyroIndex] = x;
			gyroYmean [gyroIndex] = y;
			gyroIndex++;
			if (gyroIndex == gyroIndexMax) {
				//calculate the mean
				for (int i=0; i<gyroIndexMax; i++) {
					gyroX += gyroXmean [i];
					gyroY += gyroYmean [i];
				}
				gyroX = gyroX / gyroIndexMax;
				gyroY = gyroY / gyroIndexMax;
				gyroTag = true;
				//print ("gyrox= "+gyroX.ToString()+" gyroy= "+gyroY.ToString()+" Buffer size= "+buffer.getSize());
			}
		} else {
			if( Mathf.Abs(x-gyroX) >10 && Mathf.Abs(x-gyroX) <300  )
				rotationX = gyroX-x;
			if(Mathf.Abs(gyroY-y) >10 && Mathf.Abs(gyroY-y) <300 )
				rotationY = y-gyroY;
		}
		//print ("gyrox= "+gyroX.ToString()+" gyroy= "+gyroY.ToString()+" Buffer size= "+buffer.getSize());
	}
	//thread for receiving data from LSL
	private static void receiving(){
		//print ("In thread");
		results = liblsl.resolve_stream("type", "EEG");
		// open an inlet and print some interesting info about the stream (meta-data, etc.)
		inlet = new liblsl.StreamInlet(results[0]);
		//print (inlet.info ().as_xml ());
		while (inlet.pull_sample (sample) != 0 && !STOP_RECEIVING) {
			//print (sample[0].ToString());
			//for (int index = 14; index<sample.Length ;index++)
			t = sample [14].ToString ()+ " " + sample[15].ToString();//print((string)sample [index].ToString ()+ " ");
			updataGyro(sample[14],sample[15]);
			buffer.addData(sample);//push into the buffer
		}
		print ("Stop from thread receiving()");
	}
	//thread for retriving 4sec-data from buffer
	private static void retriving () {
		int count = 0;
		while (!STOP_RETRIVING) {
			if (buffer.getSize () >= (Buffer.SLIDING_WINDOW_SIZE / Buffer.SRATE) && buffer.isNewDataReady ()) {//one second data in the buffer
				float[][] a = buffer.getData ((Buffer.SLIDING_WINDOW_SIZE / Buffer.SRATE));
				//debug
				/*string ooo = "";
                for (int i = 0; i < a[0].GetLength (0); i++) {
                    ooo = ooo+a[0][i].ToString()+" ";
                }
                System.IO.File.AppendAllText (@"C:\Users\UCSD\Desktop\YT\raw.txt", ooo);*/
				//print (a[14][0] + " , " + a[15][0]);
				double[][] b = new double[a.GetLength (0)][];
				for (int i = 0; i < a.GetLength (0); i++) {
					b[i] = new double[a[i].GetLength (0)];
					for (int j = 0; j < b[i].Length; j++) {
						b[i][j] = (double)a[i][j];
					}
				}
				//print ("Get data: " + ++count);
				++count;
				if (count <= fe.TRAINING_openEye_TIME) {
					int[] q = { 0 };
					fe.setData (b, q);
					print ("Training open eyes..." + count);
				}
				else if (count <= (fe.TRAINING_closeEye_TIME + fe.TRAINING_openEye_TIME) && count > fe.TRAINING_openEye_TIME) {
					//print ("----------Open eye training complete---------");
					print ("Training close eyes..." + count);
					int[] q = { 1 };
					fe.setData (b, q);
				}
				else {
					//print ("----------Close eye training complete---------");
					int[] oo = fe.getPrediction (b);
					float drowsinessPercent = 0;
					print ("Prediction: ");//Console.Write ("Prediction: ");
					for (int qq = 0; qq < oo.Length; qq++) {
						print ("oo[qq]");//Console.Write (oo[qq]);
						if (oo[qq] == 1)
							drowsinessPercent++;
					}
					print (", " + (drowsinessPercent / oo.Length * 100f) + "%");//Console.Write (", " + (drowsinessPercent / oo.Length * 100f) + "%");
					//Console.WriteLine ();
				}
				Thread.Sleep (1000);
			}
			else {
				//print ("Buffer is not ready");
				Thread.Sleep (500);
			}
		}
		print ("Stop from thread retriving()");
	}
	//thread for retriving data from buffer
	private static void retriving_ori(){
		int count = 0;
		while (!STOP_RETRIVING) {
			if (buffer.getSize()>= Buffer.SLIDING_WINDOW_SIZE) {//one second data in the buffer
				float[][]a = buffer.getData(Buffer.SLIDING_WINDOW_SIZE);
				double[][]b = new double[a.GetLength(0)][];
				for(int i=0;i<a.GetLength(0);i++){
					b[i] = new double[a.GetLength(1)];
					for(int j=0;j<b[i].Length;j++){
						b[i][j] = (double)a[i][j];
					}
				}
				print ("Get data: "+ ++count);
				Thread.Sleep(500);
			}
			else{
				print ("Less than 1sec in the buffer");
				Thread.Sleep(1000);
			}
		}
		print ("Stop from thread retriving()");
	}
}
