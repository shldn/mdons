using UnityEngine;
using System.Collections;
using LSL;
using System.Threading;
using DrowsinessDetection;

// Handles data from LSL
public class LSLManager {

    private static bool initialized = false;

    //LSL
    private static liblsl.StreamInlet inlet;
    private static liblsl.StreamInfo[] results;
    private static float[] sample;// = new float[8];
    private static bool INITIAL = false;
    private static bool STOP_RECEIVING_IMPL = false;
    private static bool STOP_RETRIVING_IMPL = true;
    private static Thread ReceivingFromLSL = new Thread(new ThreadStart(receiving)); // Pushes to buffer and updates Gyro data
    private static Thread RetrivingFromBuffer = new Thread(new ThreadStart(retriving));


    //rotation the cube
    private static float[] gyroXmean, gyroYmean;
    private static float baseGyroX, baseGyroY;
    private static float lastGyroImplX, lastGyroImplY;
    private static float rotationX, rotationY;
    private static int gyroIndex, gyroIndexMax = Buffer.SRATE;
    private static bool gyroTag;
    private static int gyroFrameIDImpl = 0; // increments with each data point

    //EPOC
    private static Buffer buffer;

    //drowsiness detection
    private static FeatureExtraction fe;

    // Thread-safe accessors at bottom


    public static void StartReceivingData()
    {
        if (initialized)
            return;

        //EPOC..
        buffer = new Buffer();
        fe = new FeatureExtraction();
        sample = new float[Buffer.CH];
        gyroXmean = new float[gyroIndexMax];
        gyroYmean = new float[gyroIndexMax];
        gyroIndex = 0;
        gyroTag = false;
        baseGyroX = baseGyroY = 0;
        rotationX = rotationY = 0;
        // create stream info and outlet
        //dispText.text = "initial";
        ReceivingFromLSL.Start();
        RetrivingFromBuffer.Start();
        initialized = true;

    }

    public static void StopReceivingData()
    {
        STOP_RECEIVING = true;
        STOP_RETRIVING = true;
    }

    public static bool HasNewGyroData(int lastGyroFrameID)
    {
        return GyroFrameID != lastGyroFrameID;
    }

    public static void Destroy()
    {
        StopReceivingData();
        if (inlet != null)
            inlet.close_stream();
        Debug.Log("Stream closed");

        initialized = false;
    }

    static void updateGyro(float x, float y)
    {
        //calculate the mean gyro_x and gyro_y using 1sec data 
        if (!GyroBaseCalcDone)
        {
            gyroXmean[gyroIndex] = x;
            gyroYmean[gyroIndex] = y;
            gyroIndex++;
            if (gyroIndex == gyroIndexMax)
            {
                //calculate the mean
                for (int i = 0; i < gyroIndexMax; i++)
                {
                    baseGyroX += gyroXmean[i];
                    baseGyroY += gyroYmean[i];
                }
                baseGyroX = baseGyroX / gyroIndexMax;
                baseGyroY = baseGyroY / gyroIndexMax;
                GyroBaseCalcDone = true;
                //Debug.Log ("gyrox= "+gyroX.ToString()+" gyroy= "+gyroY.ToString()+" Buffer size= "+buffer.getSize());
            }
        }
        else
        {
            LastGyroX = x;
            LastGyroY = y;
            GyroFrameID++;
            //if (Mathf.Abs(x - gyroX) > 10 && (Inst.gyroControl == GyroControl.MOVEMENT || Mathf.Abs(x - gyroX) < 300))
            //    rotationX = gyroX - x;
            //if (Mathf.Abs(gyroY - y) > 10 && (Inst.gyroControl == GyroControl.MOVEMENT || Mathf.Abs(gyroY - y) < 300))
            //    rotationY = y - gyroY;
        }
        //Debug.Log ("gyrox= "+gyroX.ToString()+" gyroy= "+gyroY.ToString()+" Buffer size= "+buffer.getSize());
        //guiStr = "rot: " + rotationX + ", " + rotationY + " gyroX: " + gyroX + " gyroY: " + gyroY + " x: " + x + " y: " + y;
        //Debug.LogError("rot: " + rotationX + ", " + rotationY + " gyroX: " + gyroX + " gyroY: " + gyroY + " x: " + x + " y: " + y);
    }
    //thread for receiving data from LSL
    private static void receiving()
    {
        //Debug.Log ("In thread");
        results = liblsl.resolve_stream("type", "EEG");
        // open an inlet and print some interesting info about the stream (meta-data, etc.)
        inlet = new liblsl.StreamInlet(results[0]);
        //Debug.Log (inlet.info ().as_xml ());
        while (!STOP_RECEIVING && inlet.pull_sample(sample) != 0)
        {
            //Debug.Log (sample[0].ToString());
            //for (int index = 14; index<sample.Length ;index++)
            updateGyro(sample[14], sample[15]);
            buffer.addData(sample);//push into the buffer
        }
        Debug.Log("Stop from thread receiving()");
    }
    //thread for retriving 4sec-data from buffer
    private static void retriving()
    {
        int count = 0;
        while (!STOP_RETRIVING)
        {
            if (buffer.getSize() >= (Buffer.SLIDING_WINDOW_SIZE / Buffer.SRATE) && buffer.isNewDataReady())
            {//one second data in the buffer
                float[][] a = buffer.getData((Buffer.SLIDING_WINDOW_SIZE / Buffer.SRATE));
                //debug
                /*string ooo = "";
                for (int i = 0; i < a[0].GetLength (0); i++) {
                    ooo = ooo+a[0][i].ToString()+" ";
                }
                System.IO.File.AppendAllText (@"C:\Users\UCSD\Desktop\YT\raw.txt", ooo);*/
                //Debug.Log (a[14][0] + " , " + a[15][0]);
                double[][] b = new double[a.GetLength(0)][];
                for (int i = 0; i < a.GetLength(0); i++)
                {
                    b[i] = new double[a[i].GetLength(0)];
                    for (int j = 0; j < b[i].Length; j++)
                    {
                        b[i][j] = (double)a[i][j];
                    }
                }
                //Debug.Log ("Get data: " + ++count);
                ++count;
                if (count <= fe.TRAINING_openEye_TIME)
                {
                    int[] q = { 0 };
                    fe.setData(b, q);
                    Debug.Log("Training open eyes..." + count);
                }
                else if (count <= (fe.TRAINING_closeEye_TIME + fe.TRAINING_openEye_TIME) && count > fe.TRAINING_openEye_TIME)
                {
                    //Debug.Log ("----------Open eye training complete---------");
                    Debug.Log("Training close eyes..." + count);
                    int[] q = { 1 };
                    fe.setData(b, q);
                }
                else
                {
                    //Debug.Log ("----------Close eye training complete---------");
                    int[] oo = fe.getPrediction(b);
                    float drowsinessPercent = 0;
                    Debug.Log("Prediction: ");//Console.Write ("Prediction: ");
                    for (int qq = 0; qq < oo.Length; qq++)
                    {
                        //Debug.Log ("oo[qq]");//Console.Write (oo[qq]);
                        if (oo[qq] == 1)
                            drowsinessPercent++;
                    }
                    Debug.Log(", " + (drowsinessPercent / oo.Length * 100f) + "%");//Console.Write (", " + (drowsinessPercent / oo.Length * 100f) + "%");
                    //Console.WriteLine ();
                }
                Thread.Sleep(1000);
            }
            else
            {
                //Debug.Log ("Buffer is not ready");
                Thread.Sleep(500);
            }
        }
        Debug.Log("Stop from thread retriving()");
    }
    //thread for retriving data from buffer
    private static void retriving_ori()
    {
        int count = 0;
        while (!STOP_RETRIVING)
        {
            if (buffer.getSize() >= Buffer.SLIDING_WINDOW_SIZE)
            {//one second data in the buffer
                float[][] a = buffer.getData(Buffer.SLIDING_WINDOW_SIZE);
                double[][] b = new double[a.GetLength(0)][];
                for (int i = 0; i < a.GetLength(0); i++)
                {
                    b[i] = new double[a.GetLength(1)];
                    for (int j = 0; j < b[i].Length; j++)
                    {
                        b[i][j] = (double)a[i][j];
                    }
                }
                Debug.Log("Get data: " + ++count);
                Thread.Sleep(500);
            }
            else
            {
                Debug.Log("Less than 1sec in the buffer");
                Thread.Sleep(1000);
            }
        }
        Debug.Log("Stop from thread retriving()");
    }


    
    // Thread-safe accessors
    private static object recLock = new object();
    private static object retLock = new object();
    private static object gyroLockX = new object();
    private static object gyroLockY = new object();
    private static object gyroIDLock = new object();
    private static object gyroBaseLock = new object();
    private static bool STOP_RECEIVING
    {
        get{
            bool tmp;
            lock (recLock){
                tmp = STOP_RECEIVING_IMPL;
            }
            return tmp;
        }
        set{
            lock (recLock){
                STOP_RECEIVING_IMPL = value;
            }
        }
    }
    private static bool STOP_RETRIVING
    {
        get{
            bool tmp;
            lock (retLock){
                tmp = STOP_RETRIVING_IMPL;
            }
            return tmp;
        }
        set{
            lock (retLock){
                STOP_RETRIVING_IMPL = value;
            }
        }
    }
    private static float LastGyroX{
        get{
            float tmp;
            lock (gyroLockX){
                tmp = lastGyroImplX;
            }
            return tmp;
        }
        set{
            lock (gyroLockX){
                lastGyroImplX = value;
            }
        }
    }
    private static float LastGyroY{
        get{
            float tmp;
            lock (gyroLockY){
                tmp = lastGyroImplY;
            }
            return tmp;
        }
        set{
            lock (gyroLockY){
                lastGyroImplY = value;
            }
        }
    }
    private static bool GyroBaseCalcDone{
        get{
            bool tmp;
            lock (gyroBaseLock){
                tmp = gyroTag;
            }
            return tmp;
        }
        set{
            lock (gyroBaseLock){
                gyroTag = value;
            }
        }
    }
    public static float GyroOffsetX{
        get{
            if( !GyroBaseCalcDone )
                return 0f;
            return baseGyroX - LastGyroX;
        }
    }
    public static float GyroOffsetY{
        get{
            if( !GyroBaseCalcDone )
                return 0f;
            return LastGyroY - baseGyroY;
        }
    }
    public static int GyroFrameID{
        get{
            int tmp;
            lock (gyroIDLock){
                tmp = gyroFrameIDImpl;
            }
            return tmp;
        }
        set{
            lock (gyroIDLock){
                gyroFrameIDImpl = value;
            }
        }
    }




}
