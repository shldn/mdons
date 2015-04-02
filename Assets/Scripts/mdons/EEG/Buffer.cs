using System;
using System.Collections.Generic;
/**
 * A sliding window based buffer. DATA_READY is a tag indicates if the new data is ready. Note that it doesn't imply the size of sample_pts has reached SLIDING_WINDOW_SIZE.
 * 
 * addData(): this funciton pushes the data into sample_temp_buffer first. 
 *              When its size reach ADVANCED_WINDOW_SIZE it moves the data into sample_pts and set the DATA_READY to true.
 *              
 * getData(): return data in a specific length(sec) when the sample_pts.length reaches SLIDING_WINDOW_SIZE, then set the DATA_READY to false. 
 * **/
namespace DrowsinessDetection
{
	public class Buffer{
		private  LinkedList <float[]> sample_pts;
		private LinkedList<float[]> sample_temp_buffer;
		public static int SRATE = 128;
		public static int CH = 14+2;//14eeg, gryo_x and gyro_y
		private static int MAX_CAPACITY = SRATE * 4;//sec
		public static int SLIDING_WINDOW_SIZE = MAX_CAPACITY;
		private int ADVANCED_WINDOW_SIZE = 1 *SRATE;//sec
		private object myLock = new object();
        private bool DATA_READY;

		public Buffer (){
			sample_pts = new LinkedList<float[]> ();
			sample_temp_buffer = new LinkedList<float[]>();
            DATA_READY = false;
		}
		//since the thread pushes one samples per time, this method queues the samples in sample_temp_buffer 
		//until 1sec reached and then copy to sample_pts
		public void addData(float[] d){
            float[] newData = new float[d.Length];
            d.CopyTo (newData, 0);
			if (sample_temp_buffer.Count < ADVANCED_WINDOW_SIZE) {
				sample_temp_buffer.AddLast(newData);
			} 
			else{
				//(sample_pts.Count == MAX_CAPACITY) 
				lock(myLock){
					for(int i=0;i<ADVANCED_WINDOW_SIZE;i++){//sliding ADVANCED_WINDOW_SIZE
						if(sample_pts.Count==SLIDING_WINDOW_SIZE)
							sample_pts.RemoveFirst ();
						sample_pts.AddLast(sample_temp_buffer.First.Value);
						sample_temp_buffer.RemoveFirst();
					}
                    DATA_READY = true;
                    //Console.WriteLine ("addDATA(), sample_pts size: " + sample_pts.Count/SRATE);
				}			
			}
			//sample_pts.AddLast (newData);//attach new data into tail
			//Console.WriteLine("EnQueue size: "+sample_pts.Count);
		}
		//return the data with specific-second-long, please call getSize to confirm the size first.
		// the return content is: ch*data
		public float[][] getData_ori(int second){
			float[][]data = new float[CH][] ;
            
			if (sample_pts.Count < second * SRATE) {
				return null;
			} 
			else {
				for(int i=0;i<CH;i++){
					data[i] = new float[second*SRATE];
				}
				lock(myLock){
					for (int samples=0;samples<second*SRATE;samples++){
                        float[] s = sample_pts.First.Value;
                        sample_pts.RemoveFirst();
						for(int ch=0;ch<CH;ch++){
							data[ch][samples] = s[ch];
						}
					}
                    DATA_READY = false;
				}			
			}
			return data;
		}
        public float[][] getData ( int second ) {
            float[][] data = new float[CH][];

            if (sample_pts.Count < second * SRATE) {
                return null;
            }
            else {
                for (int i = 0; i < CH; i++) {
                    data[i] = new float[second * SRATE];
                }
                lock (myLock) {
                    LinkedListNode<float[]> node = sample_pts.First;
                    for (int samples = 0; samples < second * SRATE; samples++) {
                        float[] s = node.Value;
                        node = node.Next;
                        //sample_pts.RemoveFirst ();
                        for (int ch = 0; ch < CH; ch++) {
                            data[ch][samples] = s[ch];
                        }
                    }
                    DATA_READY = false;
                }
            }
            return data;
        }
        public bool isNewDataReady () {
            return DATA_READY;
        }
		//return second-long data in the buffer
		public int getSize(){
			return sample_pts.Count/SRATE;
		}
		/**
		 * you might get a null instance that shows there is no data in the buffer
		 **/
		/*public float[] getData(){
			if (sample_pts.Count > 0) {
				Console.WriteLine ("DeQueue size: " + sample_pts.Count);
				return sample_pts.First.Value;
			} else {
				Console.WriteLine ("Queue size is smaller 0");
				return null;
			}
		}*/
	}
}

