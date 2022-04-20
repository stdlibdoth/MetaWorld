using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;


public class VoxelDataFormatter : IFormatter<Voxel[]>
{
    private Dictionary<Vector3Int,Task> m_writingTasks;
    private Dictionary<Vector3Int,Task> m_writeWaitingTasks;
    private Dictionary<Vector3Int, Action> m_onExportActions;
    private Queue<Vector3Int> m_writeWaitingQueue;

    private Dictionary<Vector3Int, Task> m_readingTasks;
    private Dictionary<Vector3Int, Task> m_readWaitingTasks;
    private Queue<Vector3Int> m_readWaitingQueue;
    private FrameTimer m_writeTimer;
    private FrameTimer m_readTimer;
    private FrameTimer m_statusTimer;
    private FrameTimer m_initTimer;

    public int ReadingTaskCount { get { return m_readingTasks.Count; } }
    public int WritingTaskCount { get { return m_writeWaitingTasks.Count + m_writingTasks.Count; } }

    public VoxelDataFormatter(int write_interval,int read_interval)
    {
        m_writingTasks = new Dictionary<Vector3Int, Task>();
        m_writeWaitingTasks = new Dictionary<Vector3Int, Task>();
        m_onExportActions = new Dictionary<Vector3Int, Action>();
        m_writeWaitingQueue = new Queue<Vector3Int>();
        m_readingTasks = new Dictionary<Vector3Int, Task>();
        m_readWaitingTasks = new Dictionary<Vector3Int, Task>();
        m_readWaitingQueue = new Queue<Vector3Int>();


        m_writeTimer = FrameTimerManager.GetTimer(write_interval, FrameTimerMode.Repeat);
        m_writeTimer.OnTimeUp.AddListener(UpdateWriteScheduler);
        m_writeTimer.Start();

        m_readTimer = FrameTimerManager.GetTimer(read_interval, FrameTimerMode.Repeat);
        m_readTimer.OnTimeUp.AddListener(UpdateReadScheduler);

        m_initTimer = FrameTimerManager.GetTimer(1, FrameTimerMode.OneShot);
        m_initTimer.OnTimeUp.AddListener(InitTimers);
        m_initTimer.Start();

        m_statusTimer = FrameTimerManager.GetTimer(1, FrameTimerMode.Repeat);
        m_statusTimer.OnTimeUp.AddListener(UpdateTaskStatus);
        m_statusTimer.Start();
    }


    private void InitTimers()
    {
        m_readTimer.Start();
        FrameTimerManager.DisposeTimer(m_initTimer);
    }

    private void ScheduleExport(Voxel[] data, Vector3Int coord, string dir)
    {
        Task formatTask = new Task(() =>
        {
            string path = dir + "/" + coord.ToString() + ".txt";
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].render != 0)
                    {
                        sw.WriteLine(i);
                        sw.WriteLine(data[i].userId);
                        sw.WriteLine(data[i].color);
                    }
                }
            }
            
        });
        m_writeWaitingTasks.Add(coord, formatTask);
        m_writeWaitingQueue.Enqueue(coord);
    }

    public void Export(Voxel[] data, Vector3Int coord,string dir, Action onExport)
    {
        if (m_writingTasks.ContainsKey(coord) || m_writeWaitingTasks.ContainsKey(coord))
            return;
        ScheduleExport(data, coord, dir);
        m_onExportActions.Add(coord, onExport);
    }



    public string Format(Voxel[] data)
    {
        string str = "";
        for (int i = 0; i < data.Length; i++)
        {
            string line = data[i].render.ToString() + "\n";
            line += data[i].userId.ToString() + "\n";
            line += data[i].color.ToString() + "\n";
            str = str + line;
        }
        return str;
    }

    private void ScheduleRead(string dir, Vector3Int coord, int data_length, Action<Voxel[]> onReadAction)
    {
        Task readTask = new Task(() =>
        {
            Voxel[] data = new Voxel[data_length];
            int indexCounter = 0;
            string path = dir + "/" + coord.ToString() + ".txt";
            if (!File.Exists(path))
            {
                //Debug.Log("gen:" + coord);
                System.Random rand = new System.Random();
                Color color = new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble(), 1);
                for (int i = 0; i < data_length; i++)
                {
                    data[i].render = rand.Next(0, 10) == 1 ? 1 : 0;
                    data[i].color = color;
                }
            }
            else
            {
                //Debug.Log("read:" + coord);
                using (StreamReader sr = new StreamReader(path))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        indexCounter = int.Parse(line);

                        //line = sr.ReadLine();
                        data[indexCounter].render = 1;

                        line = sr.ReadLine();
                        data[indexCounter].userId = int.Parse(line);

                        line = sr.ReadLine();
                        data[indexCounter].color =
                        new Color(float.Parse(line.Substring(5, 5)),
                            float.Parse(line.Substring(12, 5)),
                            float.Parse(line.Substring(19, 5)),
                            float.Parse(line.Substring(26, 5)));
                        //indexCounter++;
                    }
                }
            }
            onReadAction.Invoke(data);
        });
        m_readWaitingTasks.Add(coord, readTask);
        m_readWaitingQueue.Enqueue(coord);
    }

    public void ReadData(string dir, Vector3Int coord, int data_length, Action<Voxel[]> onReadAction)
    {
        if (m_readingTasks.ContainsKey(coord) || m_readWaitingTasks.ContainsKey(coord))
            return;
        ScheduleRead(dir, coord, data_length, onReadAction);
    }


    private void UpdateWriteScheduler()
    {
        if (m_writeWaitingQueue.Count > 0)
        {
            Vector3Int coord = m_writeWaitingQueue.Dequeue();
            Task task = m_writeWaitingTasks[coord];
            task.Start();
            m_writingTasks.Add(coord, task);
            m_writeWaitingTasks.Remove(coord);
        }
    }

    private void UpdateReadScheduler()
    {
        if (m_readWaitingQueue.Count > 0)
        {
            Vector3Int coord = m_readWaitingQueue.Dequeue();
            Task task = m_readWaitingTasks[coord];
            task.Start();
            m_readingTasks.Add(coord, task);
            m_readWaitingTasks.Remove(coord);
        }
    }

    private void UpdateTaskStatus()
    {
        List<Vector3Int> keys = new List<Vector3Int>();
        foreach (var pair in m_writingTasks)
        {
            if (m_writingTasks[pair.Key].Status == TaskStatus.RanToCompletion)
            {
                m_onExportActions[pair.Key].Invoke();
                keys.Add(pair.Key);
                m_onExportActions.Remove(pair.Key);
            }
        }
        Vector3Int[] keyArray = keys.ToArray();
        for (int i = 0; i < keyArray.Length; i++)
        {
            m_writingTasks.Remove(keyArray[i]);
        }
        keys.Clear();

        foreach (var pair in m_readingTasks)
        {
            if (m_readingTasks[pair.Key].Status == TaskStatus.RanToCompletion)
            {
                keys.Add(pair.Key);
            }
        }
        keyArray = keys.ToArray();
        for (int i = 0; i < keyArray.Length; i++)
        {
            m_readingTasks.Remove(keyArray[i]);
        }

    }

    //private struct SaveJob : IJobFor
    //{
    //    [ReadOnly]
    //    [DeallocateOnJobCompletion]
    //    [NativeDisableParallelForRestriction]
    //    public NativeArray<Voxel> voxelData;


    //    [ReadOnly]
    //    [DeallocateOnJobCompletion]
    //    [NativeDisableParallelForRestriction]
    //    public NativeArray<Vector3Int> coords;


    //    [ReadOnly]
    //    public int chunkLength;

    //    //[ReadOnly]
    //    //public dir;

    //    public void Execute(int index)
    //    {
    //        string str = "";
    //        for (int i = 0; i < chunkLength; i++)
    //        {
    //            int vIndex = index * chunkLength + i;
    //            string r = voxelData[vIndex].render ? "1" : "0";
    //            string line = r + "\n";
    //            line += voxelData[vIndex].userId.ToString() + "\n";
    //            line += voxelData[vIndex].color.r.ToString("F2") + "," +
    //                voxelData[vIndex].color.g.ToString("F2") + "," +
    //                voxelData[vIndex].color.b.ToString("F2") + "," +
    //                voxelData[vIndex].color.a.ToString("F2") + "\n";
    //            str = str + line;
    //        }
    //        string path = "F:/" + coords[index].ToString() + ".txt";

    //        using (StreamWriter sw = new StreamWriter(path, false))
    //        {
    //            sw.Write(str);
    //        }
    //    }
    //}
}
