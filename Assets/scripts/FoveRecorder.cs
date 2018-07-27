using System.Collections;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class FoveRecorder : MonoBehaviour {

    public FoveInterfaceBase fove = null;

    

    public KeyCode toggleRecordingKeyCode = KeyCode.Space;

    public uint writeAtDataCount = 1000;
    public string fileName = "fove_coordinates";
    public bool overwriteExistingFile = false;

    public struct RecordingPrecision_struct
    {
        [Tooltip("How many digits of decimal precision to record")]
        public int timePrecision;

        [Tooltip("How many digits of decimal precision to use when writing vector data")]
        public int vectorPrecision;
        [Tooltip("Forces unused decimal precision to be written out with zeros, for instance, 4 rpecision digits " +
                 "and a value of 0.12 would be written \"0.1200\"")]
        public bool forcePrecisionDigits;
    }

    public RecordingPrecision_struct recordingPrecision = new RecordingPrecision_struct
    {
        timePrecision = 10,
        vectorPrecision = 6,
        forcePrecisionDigits = true
    };

    private string tPrecision;
    private string vPrecision;

    private bool recordingStopped = true;

    class RecordingDatum
    {
        public float frameTime;
        public Vector3 leftEye;
        public Vector3 rightEye;
    }

    private List<RecordingDatum> dataSlice;

    private List<RecordingDatum> dataToWrite = null;

    private Mutex writingDataMutex = new Mutex(false);

    private EventWaitHandle threadWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

    // Track whether or not the write thread should live.
    private bool threadShouldLive = true;

    // The thread object which we will call into the write thread function.
    private Thread writeThread;

    // Use this for initialization
    void Start()
    {
       
        if (fove == null)
        {
            Debug.LogWarning("Forgot to assign a Fove interface to the FOVERecorder object.");
            enabled = false;
            return;
        }

        dataSlice = new List<RecordingDatum>((int)(writeAtDataCount + 1));

        {
            string testFileName = fileName + ".csv";
            if (!overwriteExistingFile)
            {
                int counter = 1;
                while (File.Exists(testFileName))
                {
                    testFileName = fileName + "_" + (counter++) + ".csv"; // e.g., "results_12.csv"
                }
            }
            fileName = testFileName;

            Debug.Log("Writing data to " + fileName);
        }

        try
        {
            File.WriteAllText(fileName, "frameTime,leftEye x,leftEye y,rightEye x, rightEye y \n");
        }
        catch (Exception e)
        {
            Debug.LogError("error writing header to output file :\n" + e);
            enabled = false;
            return;
        }

        char precisionChar = recordingPrecision.forcePrecisionDigits ? '0' : '#';
        vPrecision = "#0." + new string(precisionChar, recordingPrecision.vectorPrecision);
        tPrecision = "#0." + new string(precisionChar, recordingPrecision.timePrecision);

        StartCoroutine(RecordData());
        writeThread = new Thread(WriteThreadFunc);
        writeThread.Start();

    }

    // Update is called once per frame
    void Update()
    {
        // If you press the assigned key, it will toggle the "recordingStopped" variable.
        if (Input.GetKeyDown(toggleRecordingKeyCode))
        {
            recordingStopped = !recordingStopped;
            Debug.Log(recordingStopped ? "Stopping" : "Starting" + " data recording...");
          
        }

    }

    private void OnApplicationQuit()
    {
        if (writeThread == null)

            return;

        // Get a lock to the mutex to make sure data isn't being written. Wait up to 200 milliseconds.
        if (writingDataMutex.WaitOne(200))
        {
            // Tell the thread to end, then release the mutex so it can finish.
            threadShouldLive = false;

            CheckForNullDataToWrite();
            dataToWrite = dataSlice;
            dataSlice = null;

            writingDataMutex.ReleaseMutex();

            if (!threadWaitHandle.Set())
                Debug.LogError("Error setting the event to wake up the file writer thread on application quit");
        }
        else
        {
            // If it times out, tell the operating system to abort the thread.
            writeThread.Abort();
        }

        // Wait for the write thread to end (up to 1 second).
        writeThread.Join(1000);
    }
    void CheckForNullDataToWrite()
    {
        // The write thread sets dataToWrite to null when it's done, so if it isn't null here, it's likely
        // that some major error occured.
        if (dataToWrite != null)
        {
            Debug.LogError("dataToWrite was not reset when it came time to set it; this could indicate a" +
                           "serious problem in the data recording/writing process.");
        }
    }

    IEnumerator RecordData()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            if (recordingStopped)
            {
                continue;
            }

            RecordingDatum datum = new RecordingDatum
            {
                frameTime = Time.time,
                leftEye = fove.GetLeftEyeVector(),
                rightEye = fove.GetRightEyeVector()
            };

            dataSlice.Add(datum);

            if (dataSlice.Count >= writeAtDataCount)
            {
                if (!writingDataMutex.WaitOne(3))
                {
                    long excess = dataSlice.Count - writeAtDataCount;
                    if (excess > 1)
                        Debug.LogError("Data slice is " + excess + " entries over where it should be; this is" +
                                       "indicative of a major performance concern in the data recording and writing" +
                                       "process.");
                    continue;
                }
                CheckForNullDataToWrite();

                dataToWrite = dataSlice;
                dataSlice = new List<RecordingDatum>((int)(writeAtDataCount + 1));

                writingDataMutex.ReleaseMutex();

                if (!threadWaitHandle.Set())
                {
                    Debug.LogError("Error setting the event to wake up the file writer thread!");
                }
            }
        }
    }

    private void WriteDataFromThread()
    {

        if (!writingDataMutex.WaitOne(10))
        {
            Debug.LogWarning("Write thread couldn't lock mutex for 10ms, which is indicative of a problem where" +
                             "the core loop is holding onto the mutex for too long, or may have not released the" +
                             "mutex.");
            return;
        }

        if (dataToWrite != null)
        {
            Debug.Log("Writing " + dataToWrite.Count + " lines");
            try
            {
                string text = "";

                foreach (var datum in dataToWrite)
                {
                    // This writes each element in the data list as a CSV-formatted line. Be sure to update this
                    // (carefully) if you add or change around the data you're using.
                    text += string.Format(
                        "\"{0}\"," +
                        "\"{1}\",\"{2}\",\"{3}\",\"{4}\"\n",
                        datum.frameTime.ToString(tPrecision),
                        datum.leftEye.x.ToString(vPrecision),
                        datum.leftEye.y.ToString(vPrecision),
                        datum.rightEye.x.ToString(vPrecision),
                        datum.rightEye.y.ToString(vPrecision));
                }

                File.AppendAllText(fileName, text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Exception writing to data file:\n" + e);
                threadShouldLive = false;
            }

            dataToWrite = null;
        }

        writingDataMutex.ReleaseMutex();
    }


    private void WriteThreadFunc()
    {
        while (threadShouldLive)
        {
            if (threadWaitHandle.WaitOne())
                WriteDataFromThread();
        }

        // Try to write one last time once the thread ends to catch any missed elements
        WriteDataFromThread();
    }
}
