using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Sfs2X.Core;
using Sfs2X.Util;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;

public class CSVMsgData
{
    public CSVMsgData(int msgIdx_, MsgData msg)
    {
        msgIdx = msgIdx_;
        SetPositionRot(msg);
        mouseMoved = GetMouseMoved(msg);
        talked = DidTalk(msg);
        talkTimeSum = 0.0f;
        if (talked)
            GetRelativeVolume(VoiceManager.GetVoicePacketFromMsg(SFSObject.NewFromBinaryData(new ByteArray(msg.msg))), out volume, out frequency, out minFrequency, out maxFrequency);
    }

    bool DidTalk(MsgData msg)
    {
        if( msg.msgType != SFSEvent.OBJECT_MESSAGE )
            return false;

        SFSObject dataObj = SFSObject.NewFromBinaryData(new ByteArray(msg.msg));
        return dataObj.ContainsKey("type") && dataObj.GetUtfString("type") == "voice";
    }

    void GetRelativeVolume(VoiceChatPacket packet, out float maxAmplitude, out int highVolumeFrequency, out int minFrequency, out int maxFrequency)
    {
        float[] sample = null;
        VoiceChatUtils.Decompress(new NSpeex.SpeexDecoder(NSpeex.BandMode.Narrow), packet, out sample);

        // clear fftBuffer
        for (int i = 0; i < fftBuffer.Length; ++i)
            fftBuffer[i] = 0;
        Array.Copy(sample, 0, fftBuffer, 0, sample.Length);
        Exocortex.DSP.Fourier.FFT(fftBuffer, fftBuffer.Length / 2, Exocortex.DSP.FourierDirection.Forward);

        highVolumeFrequency = -1;
        minFrequency = -1;
        maxFrequency = -1;
        maxAmplitude = -1;
        for (int i = 0; i < fftBuffer.Length; ++i)
        {
            if (fftBuffer[i] > maxAmplitude)
            {
                maxAmplitude = fftBuffer[i];
                highVolumeFrequency = i;
            }
            if (minFrequency == - 1 && fftBuffer[i] > 0)
                minFrequency = i;
            if (fftBuffer[i] > 0)
                maxFrequency = i;
        }
    }

    void SetPositionRot(MsgData msg)
    {
        if (msg.changedVars == null)
            return;
        for( int i=0; i < msg.changedVars.Count; ++i )
        {
            UserVariable userVar = msg.changedVars[i];
            if( userVar.Name == "x" || userVar.Name == "y" || userVar.Name == "z" || userVar.Name == "rot")
                ReplayManager.UpdateTransformVars(userVar, ref rotAngle, ref position);
        }
    }

    bool GetMouseMoved(MsgData msg)
    {
        if( msg.msgType != SFSEvent.OBJECT_MESSAGE )
            return false;
        SFSObject dataObj = SFSObject.NewFromBinaryData(new ByteArray(msg.msg));
        return !dataObj.ContainsKey("type") && dataObj.ContainsKey("mpo");
    }

    public bool talked;
    public bool mouseMoved;
    public int msgIdx;
    public float rotAngle;
    public float talkTimeSum; // used to sum up talk time across a string of voice messages, displayed at the end of a string
    public float volume = -1;
    public int frequency = -1;
    public int minFrequency = -1;
    public int maxFrequency = -1;
    public Vector3 position;
    public Vector3 mousePosition;

    private static float[] fftBuffer = new float[VoiceChatUtils.ClosestPowerOfTwo(VoiceChatSettings.Instance.SampleSize)];

}

public class ReplayFileReader {

    private DateTime startFileTime;
    private int fileVersion = -1;
    private int level = -1;

    private static ReplayFileReader mInstance;
    public static ReplayFileReader Inst {
        get {
            if (mInstance == null)
                mInstance = new ReplayFileReader();
            return mInstance;
        }
    }
    public DateTime StartFileTime { get { return startFileTime; } }
    public int Level { get { return level; } }

    public List<MsgData> ReadFile(string filename, int msgOffset = 0, int numMsgsToLoad = -1, bool saveCSV = false)
    {
        try{
            FileStream fileStream = File.Open(filename, FileMode.Open);
            return ReadStream(fileStream, filename, msgOffset, numMsgsToLoad, saveCSV);
        }catch(Exception e) {
			Debug.LogError("Caught Exception reading replay file: " + e.ToString());
		}
        return new List<MsgData>();
    }

    public List<MsgData> ReadStream(Stream stream, string filename, int msgOffset = 0, int numMsgsToLoad = -1, bool saveCSV = false)
    {
        bool savePosData = saveCSV;
        int msgCount = 0;
        List<MsgData> msgData = new List<MsgData>();
        List<CSVMsgData> csvMsgData = new List<CSVMsgData>();
        Dictionary<int, CSVMsgData> lastVoiceMsg = new Dictionary<int, CSVMsgData>();

		try{
            using (BinaryReader reader = new BinaryReader(stream))
            {
                startFileTime = DateTime.FromBinary(reader.ReadInt64()); // start time
                fileVersion = reader.ReadInt32(); // file version
                if (fileVersion >= 5)
                    level = (int)reader.ReadByte();
                Debug.LogError("FileVersion: " + fileVersion + " Replay Level: " + ((GameManager.Level)level).ToString());
                while (reader.BaseStream.Position != reader.BaseStream.Length && (numMsgsToLoad < 0 || msgData.Count < numMsgsToLoad))
                {
                    MsgData data = new MsgData();
                    data.time = DateTime.FromBinary(reader.ReadInt64());
                    data.msgType = reader.ReadString();

                    if (data.msgType == SFSEvent.USER_VARIABLES_UPDATE)
                        ReadUserVariableUpdate(reader, ref data);
                    else if (data.msgType == SFSEvent.OBJECT_MESSAGE)
                        ReadObjectMessage(reader, ref data);
                    else if (data.msgType == SFSEvent.ROOM_VARIABLES_UPDATE)
                        ReadRoomVariablesUpdate(reader, ref data);
                    else if (data.msgType == SFSEvent.PUBLIC_MESSAGE || data.msgType == SFSEvent.PRIVATE_MESSAGE || data.msgType == SFSEvent.ADMIN_MESSAGE)
                        ReadPublicPrivateAdminMessage(reader, ref data);
                    else if (data.msgType == SFSEvent.USER_ENTER_ROOM || data.msgType == SFSEvent.USER_EXIT_ROOM)
                        ReadUserEnterExit(reader, ref data);
                    else
                    {
                        Debug.LogError(reader.BaseStream.Position + " Unknown msg type: " + data.msgType + " assuming data corrupted, searching for next valid message");
                        reader.BaseStream.Seek(-data.msgType.Length - 8, SeekOrigin.Current);
                        if (msgData.Count > 0)
                            FindNextValidMessage(reader, msgData[msgData.Count - 1].time);
                        continue;
                    }
                    msgCount++;
                    if (msgCount >= msgOffset)
                    {
                        msgData.Add(data);
                        if (saveCSV)
                        {        
                            float voiceDelayToConsiderTalkingStopped = 1.0f; // seconds -- used for CSV file
                            CSVMsgData newMsg = new CSVMsgData(msgData.Count - 1, data);
                            csvMsgData.Add(newMsg);
                            if (newMsg.talked)
                            {
                                newMsg.talkTimeSum += 0.04f; // 640/16000 -- Speex_16K
                                CSVMsgData lastMsg = null;
                                if (lastVoiceMsg.TryGetValue(data.id, out lastMsg))
                                {
                                    if (data.time.Subtract(msgData[lastMsg.msgIdx].time).TotalSeconds >= voiceDelayToConsiderTalkingStopped)
                                        lastVoiceMsg[data.id] = newMsg;
                                    else
                                    {
                                        newMsg.talkTimeSum += lastMsg.talkTimeSum; 
                                        lastMsg.talkTimeSum = 0;
                                        lastVoiceMsg[data.id] = newMsg;
                                    }
                                }
                                else
                                    lastVoiceMsg.Add(data.id, newMsg);
                            }
                        }
                    }
                }
            }
		}catch(Exception e) {
			Debug.LogError("Caught Exception reading replay file: " + e.ToString());
		}

        if (saveCSV)
            WriteCSVFile(filename + ".txt", csvMsgData, msgData);
        if (savePosData)
            WritePosFile(filename + ".pos.txt", msgData);
        return msgData;
    }

    void WriteCSVFile(string filename, List<CSVMsgData> csvMsgData, List<MsgData> msgData)
    {
        Vector3 lastPos;
        int lastToTalkID = -1;
        Dictionary<int, Vector3> lastPosition = new Dictionary<int, Vector3>();
        string delim = "\t";

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
        {
            file.WriteLine("Date" + delim + "Name" + delim + "Moved" + delim + "MouseMoved" + delim + "Talked" + delim + "RelVolume" + delim + "Frequency" + delim + "MinFrequency" + delim + "MaxFrequency" + delim + "TalkedLength" + delim + "NotLastToTalk");
            for(int i=0; i < csvMsgData.Count; ++i)
            {
                CSVMsgData csvMsg = csvMsgData[i];
                MsgData msg = msgData[csvMsg.msgIdx];
                string line = msg.time.ToString();

                float distMoved = 0.0f;
                if(lastPosition.TryGetValue(msg.id, out lastPos))
                    distMoved = Vector3.Distance(lastPos, csvMsg.position);

                line += delim + msg.name;
                line += delim + distMoved;
                line += delim + (csvMsg.mouseMoved ? "1" : "");
                line += delim + (csvMsg.talked ? "0.04" : ""); // time of talk sample for october competition was 640/16000 -- Speex_16K
                line += delim + (csvMsg.talked ? csvMsg.volume.ToString() : "");
                line += delim + (csvMsg.talked ? csvMsg.frequency.ToString() : "");
                line += delim + (csvMsg.talked ? csvMsg.minFrequency.ToString() : "");
                line += delim + (csvMsg.talked ? csvMsg.maxFrequency.ToString() : "");
                line += delim + (csvMsg.talkTimeSum > 0.0f ? csvMsg.talkTimeSum.ToString() : "");
                line += delim + (csvMsg.talked && lastToTalkID != msg.id ? "1" : "");
                file.WriteLine(line);

                lastPosition[msg.id] = csvMsg.position;
                if (csvMsg.talked)
                    lastToTalkID = msg.id;
            }
        }
        Debug.LogError("Wrote " + filename);
    }

    void WritePosFile(string filename, List<MsgData> msgData)
    {
        float sampleSeconds = 5; // seconds
        PositionDataExporter exporter = new PositionDataExporter(filename, msgData[0].time, sampleSeconds);
        List<string> names = GetNames(msgData, (int)PlayerType.NORMAL);
        exporter.Initialize(names);
        for (int i = 0; i < msgData.Count; ++i)
        {
            if (msgData[i].IsPositionChange())
                exporter.Update(msgData[i].time, msgData[i].name, msgData[i].GetPosition());
            else if (msgData[i].msgType == SFSEvent.USER_EXIT_ROOM)
                exporter.PlayerExit(msgData[i].name);
        }
        Debug.LogError("Wrote " + filename);
    }

    // maxPlayerType == -1 will return all player types
    List<string> GetNames(List<MsgData> msgData, int maxPlayerType = -1) 
    {
        HashSet<string> names = new HashSet<string>();
        HashSet<string> namesToIgnore = new HashSet<string>();
        for (int i = 0; i < msgData.Count; ++i)
            if (msgData[i].name != null && msgData[i].name != "")
            {
                names.Add(msgData[i].name);
                if (maxPlayerType > -1 && msgData[i].changedVars != null)
                {
                    for(int j=0; j < msgData[i].changedVars.Count; ++j)
                        if (msgData[i].changedVars[j].Name == "ptype" && msgData[i].changedVars[j].GetIntValue() > maxPlayerType)
                            namesToIgnore.Add(msgData[i].name);
                }
            }
        names.ExceptWith(namesToIgnore); // remove namesToIgnore
        List<string> nameList = new List<string>(names);
        nameList.Sort();
        return nameList;
    }

    void ReadUserData(BinaryReader reader, ref MsgData data)
    {
        data.id = reader.ReadInt32();
        data.playerID = reader.ReadInt32();
        data.name = reader.ReadString();
    }

    void ReadMsgAndDataArrays(BinaryReader reader, ref MsgData data)
    {
        int msgLength = reader.ReadInt32();
        if (msgLength > 0)
            data.msg = reader.ReadBytes(msgLength);
        int dataLength = reader.ReadInt32();
        if (dataLength > 0)
            data.data = reader.ReadBytes(dataLength);
    }

    void ReadUserVariableUpdate(BinaryReader reader, ref MsgData data)
    {
        ReadUserData(reader, ref data);
        ReadMsgAndDataArrays(reader, ref data);
        data.changedVars = ReadUserVariables(reader);
    }

    void ReadObjectMessage(BinaryReader reader, ref MsgData data)
    {
        ReadUserData(reader, ref data);
        ReadMsgAndDataArrays(reader, ref data);
    }

    void ReadRoomVariablesUpdate(BinaryReader reader, ref MsgData data)
    {
        ReadMsgAndDataArrays(reader, ref data);
        data.room = reader.ReadString();
        data.changedVars = ReadUserVariables(reader);
    }

    void ReadPublicPrivateAdminMessage(BinaryReader reader, ref MsgData data)
    {
        ReadUserData(reader, ref data);
        data.msgStr = reader.ReadString();
        ReadMsgAndDataArrays(reader, ref data);
    }
    void ReadUserEnterExit(BinaryReader reader, ref MsgData data)
    {
        ReadUserData(reader, ref data);
        ReadMsgAndDataArrays(reader, ref data);
        data.room = reader.ReadString();
        if (fileVersion > 1 && data.msgType == SFSEvent.USER_ENTER_ROOM)
            data.changedVars = ReadUserVariables(reader);
    }

    IEnumerator HandleVoiceMessage(float waitSeconds, SFSObject dataObj)
    {
        yield return new WaitForSeconds(Math.Max(0.001f, waitSeconds));
        VoiceManager.Inst.HandleMessage(dataObj);
    }

    private List<UserVariable> ReadUserVariables(BinaryReader reader)
    {
        bool error = false;
        long startPos = reader.BaseStream.Position;
        int numChangedVars = reader.ReadInt32();
        List<UserVariable> changedVars = new List<UserVariable>(numChangedVars);

        for (int i = 0; i < numChangedVars && !error; ++i)
        {
            try {
                string varName = reader.ReadString();
                VariableType varType = (VariableType)reader.ReadInt32();
                object obj = null;
                switch (varType)
                {
                    case VariableType.INT:
                        obj = reader.ReadInt32();
                        break;
                    case VariableType.DOUBLE:
                        obj = reader.ReadDouble();
                        break;
                    case VariableType.STRING:
                        obj = reader.ReadString();
                        break;
                    case VariableType.OBJECT:
                        int len = reader.ReadInt32();
                        byte[] objBytes = reader.ReadBytes(len);
                        obj = SFSObject.NewFromBinaryData(new ByteArray(objBytes));
                        break;
                    default:
                        Debug.LogError("Caught unknown variable type: " + (int)varType);
                        error = true;
                        break;
                }
                if (!error)
                {
                    SFSUserVariable uv = new SFSUserVariable(varName, obj, (int)varType);
                    changedVars.Add(uv);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Caught exception reading user variable: " + e.ToString());
                error = true;
            }
        }
        if (error)
        {
            reader.BaseStream.Position = startPos + 1;
            FindNextValidMessage(reader, startFileTime);
        }
        return changedVars;
    }

    long TicksFromDateBinary(long dateBinary)
    {
        return dateBinary & 0x3FFFFFFFFFFFFFFF;
    }

    void FindNextValidMessage(BinaryReader reader, DateTime lastValidDate)
    {
        Debug.LogError("Looking for next valid message");
        long lastValidDateTicks = TicksFromDateBinary(lastValidDate.ToBinary());
        long startPos = reader.BaseStream.Position;
        while (reader.BaseStream.Position < reader.BaseStream.Length - 8) // 8 bytes for the Int64
        {
            long ticks = TicksFromDateBinary(reader.ReadInt64());
            if (ticks > lastValidDateTicks && ticks < (lastValidDateTicks + TimeSpan.TicksPerDay))
            {
                DateTime newDate = DateTime.FromBinary(ticks);
                Debug.LogError("Last valid date: " + lastValidDate.ToString() + " found " + newDate.ToString());
                reader.BaseStream.Seek(-8, SeekOrigin.Current);
                return;
            }
            reader.BaseStream.Seek(-7, SeekOrigin.Current); // try again, byte by byte to find the next start point, go back 7 since we read 8 for the last iteration
        }
        Debug.LogError("Did not find another valid date from " + startPos + " to " + reader.BaseStream.Length + " diff: " + (reader.BaseStream.Length - startPos));
        reader.BaseStream.Position = reader.BaseStream.Length;
    }

}
