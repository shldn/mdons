using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Accord.Statistics.Analysis;
//Author @Yute Wang, Mar/2015
namespace DrowsinessDetection {
    class FeatureExtraction {
        private LomontFFT myFFT = new LomontFFT ();
        private double[][] power;//power afte applying FFT
        private int[] tag;

        private float alphaBandLow, alphaBandHigh;
        public int TRAINING_openEye_TIME, TRAINING_closeEye_TIME;
        private LinkedList<double[][]> trainingDataOpenEye, trainingDataCloseEye;
        private bool READY_TO_ONLINE;
        private LinearDiscriminantAnalysis lda;

        //training data setting
        private readonly int TRAINING_OPENEYE = 0;
        private readonly int TRAINING_CLOSEEYE = 1;
        private static int FEATURES = 2;//0 close eye, 1 open eye, shold be equal to above training variable
        private readonly int TRAINING_ALL = 99;
        private bool[] trainDataState;
        private bool[] channelTag;
        private int selectedChannelAmount;

        public FeatureExtraction () {
            alphaBandHigh = 10.75f;//hz
            alphaBandLow = 9f;//hz
            TRAINING_openEye_TIME = 20;//10sec training class1, open eyes
            TRAINING_closeEye_TIME = 20;//10sec training class2, close eyes
            trainingDataCloseEye = new LinkedList<double[][]>();
            trainingDataOpenEye = new LinkedList<double[][]>();
            READY_TO_ONLINE = false;

            trainDataState = new bool[FEATURES];
            channelTag = new bool[Buffer.CH];
            channelTag[6] = true;
            channelTag[7] = true;
            for (int i = 0; i < Buffer.CH; i++) {
                if (channelTag[i])
                    selectedChannelAmount++;
            }
                
        }

        public bool setData ( double[][] data, int []tagForData ) {
            /*if (data.GetLength (0) != tagForData.Length)
                throw new ArgumentException (
                    "The number of rows in the input array must match the number of given tagForData.");
            if (data.GetLength (1) != 4 * Buffer.SRATE)
                    throw new ArgumentException (
                       "The number of rows in the input array must a 4sec-length data");
            */
            computePower (data);

            if (tagForData[0] == 0 && !getTrainDataStateArray(TRAINING_OPENEYE))
                extractFeatures_open_eye ();
            if (tagForData[0] == 1 && !getTrainDataStateArray(TRAINING_CLOSEEYE))
                extractFeatures_close_eye ();
            if (getTrainDataStateArray (TRAINING_ALL)) {//TRAINING_CLOSEEYE_FULL && TRAINING_OPENEYE_FULL && !READY_TO_ONLINE) {
                //form the dataset
                double[][] inputs1 = trainingDataOpenEye.First.Value;
                double[][] inputs2 = trainingDataCloseEye.First.Value;
                int pts = inputs1.GetLength (0)  * trainingDataOpenEye.Count +
                          inputs2.GetLength (0)  * trainingDataCloseEye.Count;
                int[] tag = new int[pts];
                for(int i=0; i<pts;i++)
                    if (i >= ((inputs1.GetLength (0) * trainingDataOpenEye.Count)))
                        tag[i] = 1;
                double[][] input = new double[pts][];
                double[][] temp;
                int ptsIndex=0;

                //merge into one array
                for (int i = 0; i < trainingDataOpenEye.Count; i++){
                    temp = trainingDataOpenEye.First.Value;
                    for(int j=0;j<temp.GetLength(0);j++,ptsIndex++)
                        input[ptsIndex] = temp[j];
                }
                for (int i = 0; i < trainingDataCloseEye.Count; i++) {
                    temp = trainingDataCloseEye.First.Value;
                    for (int j = 0; j < temp.GetLength (0); j++, ptsIndex++)
                        input[ptsIndex] = temp[j];
                }
                //debug
                /*Console.Write ("power in training: ");
                for (int i = 0; i < input.Length; i++) {
                    Console.Write (input[i].ToString()+",");
                }
                Console.WriteLine("");*/
                //-------------------------

                lda = new LinearDiscriminantAnalysis (input, tag);
                lda.Compute (); // Compute the analysis

                READY_TO_ONLINE = true;
                
            }
            return READY_TO_ONLINE;
        }
        /**
         * return an one dimension array that is consisted of [ch1.feature1 ch1.feature2 ch1.featureN ... chX.feature1 chX.feature2 chX.featureN]
         * **/
        public int[] getPrediction (double[][]data) {
            computePower (data);
            int[] results = lda.Classify (getAlphabandFeatures());
            return results;
        }
        //check if the trainingData is full. If index ==99, check if all trainindData are full
        private bool getTrainDataStateArray (int index) {
            bool oo = false;
            if (index < trainDataState.Length) {
                oo = trainDataState[index];
            }
            else if (index == 99) {
                oo = true;
                for (int i = 0; i < trainDataState.Length;i++ ) {
                    if (!trainDataState[i]){
                        oo = false;
                        break;
                    }
                }
            }
            return oo;
        }
        //using LomontFFT
        private void computePower (double[][] data) {
            int channels = data.GetLength (0);
            power = new double[channels][];
            //for each channel, compute the power
            for (int i = 0; i < channels; i++) {
                power[i] = new double[Buffer.SLIDING_WINDOW_SIZE];
                power[i] = data[i];
                myFFT.RealFFT2 (power[i], true);

                //debug
                /*if (i == 0) {
                    string ooo = "";
                    for (int ii = 0; ii < power[0].GetLength (0); ii++) {
                        ooo = ooo + power[i][ii].ToString () + " ";
                    }
                    System.IO.File.AppendAllText (@"C:\Users\UCSD\Desktop\YT\fftout.txt", ooo);
                }*/
                
            }
        }
        //extract alpha band power and transform to a matrix that should contain
        // variables as columns(channel) and observations of each variable as rows(time)
        private void extractFeatures_open_eye(){
            trainingDataOpenEye.AddLast (getAlphabandFeatures ());
            if (trainingDataOpenEye.Count == TRAINING_openEye_TIME) {
                trainDataState[TRAINING_OPENEYE] = true;
                Console.WriteLine ("TRAINING_OPENEYE_FULL = true");
            }
        }
        private void extractFeatures_close_eye () {
            trainingDataCloseEye.AddLast (getAlphabandFeatures());
            if (trainingDataCloseEye.Count == TRAINING_closeEye_TIME) {
                trainDataState[TRAINING_CLOSEEYE]= true;
                Console.WriteLine ("TRAINING_CLOSEEYE_FULL = true");
            }
        }
        private void extractFeatures_onLine () {
            double[][] data = getAlphabandFeatures();
            
        }
        private double[][] getAlphabandFeatures_ori () {
            int channels = power.GetLength (0);
            int observations = power[0].GetLength (0);
            double[][] tempData = new double[channels * ((getFreqIndex (alphaBandHigh, Buffer.SLIDING_WINDOW_SIZE) - getFreqIndex (alphaBandLow, Buffer.SLIDING_WINDOW_SIZE)) / 2 + 1)][];
            
            //-----debug-----------
            //int ddd = getFreqIndex (alphaBandHigh);
            //int qqqq = getFreqIndex (alphaBandLow);
            //---------------------

            int tempIndex = 0;//index for each observation
            float tempBandIndex = alphaBandLow;

            for (int ch = 0; ch < channels; ch++) {
                for (int freq = getFreqIndex (alphaBandLow, Buffer.SLIDING_WINDOW_SIZE); freq <= getFreqIndex (alphaBandHigh, Buffer.SLIDING_WINDOW_SIZE); ) {
                //for (int freq = getFreqIndex (alphaBandLow); freq <= getFreqIndex (alphaBandHigh); freq++, tempBandIndex += 0.25f) {
                    tempData[tempIndex++] = new double[] { tempBandIndex, power[ch][freq] };
                    tempBandIndex += 0.25f;
                    freq = getFreqIndex (tempBandIndex, Buffer.SLIDING_WINDOW_SIZE);
                }
                tempBandIndex = alphaBandLow;
            }
            return tempData;
        }
        //an update function that we can select only few channels. 
        private double[][] getAlphabandFeatures () {
            int channels = power.GetLength (0);
            int observations = power[0].GetLength (0);
            double[][] tempData = new double[selectedChannelAmount * ((getFreqIndex (alphaBandHigh, Buffer.SLIDING_WINDOW_SIZE) - getFreqIndex (alphaBandLow, Buffer.SLIDING_WINDOW_SIZE)) / 2 + 1)][];

            //-----debug-----------
            //int ddd = getFreqIndex (alphaBandHigh);
            //int qqqq = getFreqIndex (alphaBandLow);
            //---------------------

            int tempIndex = 0;//index for each observation
            float tempBandIndex = alphaBandLow;

            for (int ch = 0; ch < channels; ch++) {
                if (channelTag[ch]) {
                    for (int freq = getFreqIndex (alphaBandLow, Buffer.SLIDING_WINDOW_SIZE); freq <= getFreqIndex (alphaBandHigh, Buffer.SLIDING_WINDOW_SIZE); ) {
                        //for (int freq = getFreqIndex (alphaBandLow); freq <= getFreqIndex (alphaBandHigh); freq++, tempBandIndex += 0.25f) {
                        tempData[tempIndex++] = new double[] { tempBandIndex, power[ch][freq] };
                        //Console.WriteLine ("CH/Freq(index)/Power: " + (ch+1)+"/"+tempBandIndex + "(" + freq + ")/" + power[ch][freq]);
                        tempBandIndex += getResolution(Buffer.SLIDING_WINDOW_SIZE);
                        freq = getFreqIndex (tempBandIndex, Buffer.SLIDING_WINDOW_SIZE);
                    }
                }
                tempBandIndex = alphaBandLow;
            }
            return tempData;
        }
        private float getResolution ( int windowSize ) {
            float r = windowSize / Buffer.SRATE;
            if (r == 4 )
                return 0.25f;
            else if (r == 1)
                return 1f;
            else
                return 1f;
        }
        private int getFreqIndex ( float freq , int windowSize) {
            if (windowSize == 4 * Buffer.SRATE)
                return ((int)((freq + 0.25) * 8)-2);
            else if (windowSize == 1 * Buffer.SRATE)
                return ((int)(freq * 2 ));
            else
                return 0;
        }
    }
}
