using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Burst;


public class VoxelDataFormatter : IFormatter<Voxel[]>,IDisposable
{
    private int m_writeBatchSize;

    private NativeList<Vector3Int> m_writingChunks;
    private Dictionary<Vector3Int,NativeArray<Voxel>> m_writeDataBuffer;
    private Dictionary<Vector3Int, Action> m_onExportActions;
    private NativeQueue<Vector3Int> m_writeQueue;
    private int m_writeQueueSize;
    private JobHandle m_writeHandle;
    private int m_writeState;


    private Dictionary<Vector3Int, Task> m_readingTasks;
    private Dictionary<Vector3Int, Task> m_readWaitingTasks;
    private Queue<Vector3Int> m_readWaitingQueue;
    private FrameTimer m_writeTimer;
    private FrameTimer m_readTimer;
    private FrameTimer m_statusTimer;
    private FrameTimer m_initTimer;

    public int ReadingTaskCount { get { return m_readingTasks.Count; } }
    public int WritingTaskCount { get { return m_writeDataBuffer.Keys.Count; } }

    public VoxelDataFormatter(int write_interval,int read_interval, int writeBatchSize)
    {
        m_writeState = 0;
        m_writeBatchSize = writeBatchSize;
        m_writingChunks = new NativeList<Vector3Int>(Allocator.Persistent);
        m_writeDataBuffer = new Dictionary<Vector3Int, NativeArray<Voxel>>();
        m_onExportActions = new Dictionary<Vector3Int, Action>();
        m_writeQueue = new NativeQueue<Vector3Int>(Allocator.Persistent);
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

    private void ScheduleExport()
    {
        m_writeState = 1;
        ExportJobs exportJobs = new ExportJobs();

        NativeList<Voxel> data = new NativeList<Voxel>(Allocator.Temp);
        int batchSize = m_writeBatchSize < m_writeQueueSize ? m_writeBatchSize : m_writeQueueSize;
        for (int i = 0; i < batchSize; i++)
        {
            Vector3Int writeChunk = m_writeQueue.Dequeue();
            m_writingChunks.Add(writeChunk);
            data.AddRange(m_writeDataBuffer[writeChunk]);

            m_writeQueueSize--;
            m_writeDataBuffer[writeChunk].Dispose();
            m_writeDataBuffer.Remove(writeChunk);
        }

        exportJobs.coords = m_writingChunks.ToArray(Allocator.Persistent);
        exportJobs.voxelData = data.ToArray(Allocator.Persistent);
        data.Dispose();

        exportJobs.dataLength = VoxelManager.chunkSize * VoxelManager.chunkSize * VoxelManager.chunkSize;
        exportJobs.dir = VoxelManager.VoxelDataDir;
        Debug.Log(batchSize);
        exportJobs.Schedule(batchSize, 10);
    }

    public void Export(Voxel[] data, Vector3Int coord, Action onExport)
    {
        //if (m_writingTasks.ContainsKey(coord) || m_writeWaitingTasks.ContainsKey(coord))
        //    return;

        if (m_writeDataBuffer.ContainsKey(coord))
            return;
        NativeArray<Voxel> d = new NativeArray<Voxel>(data, Allocator.Persistent);
        m_writeDataBuffer[coord] = d;
        m_writeQueue.Enqueue(coord);
        m_writeQueueSize++;
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
        if (m_writeQueueSize > 0 && m_writeState == 0)
        {
            ScheduleExport();
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
        if(m_writeState == 1 && m_writeHandle.IsCompleted)
        {
            m_writeHandle.Complete();
            NativeArray<Vector3Int> chunks = m_writingChunks.ToArray(Allocator.Temp);
            for (int i = 0; i < chunks.Length; i++)
            {
                m_onExportActions[chunks[i]].Invoke();
                m_onExportActions.Remove(chunks[i]);
            }
            chunks.Dispose();
            m_writingChunks.Clear();
            m_writeState = 0;
        }

        List<Vector3Int> keys = new List<Vector3Int>();
        foreach (var pair in m_readingTasks)
        {
            if (m_readingTasks[pair.Key].Status == TaskStatus.RanToCompletion)
            {
                keys.Add(pair.Key);
            }
        }
        Vector3Int[] keyArray = keys.ToArray();
        for (int i = 0; i < keyArray.Length; i++)
        {
            m_readingTasks.Remove(keyArray[i]);
        }

    }

    public void Dispose()
    {
        m_writeQueue.Dispose();
        m_writingChunks.Dispose();
    }

    private struct ExportJobs : IJobParallelFor
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Voxel> voxelData;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Vector3Int> coords;

        [ReadOnly]
        public FixedString512Bytes dir;
        //public string dir;

        [ReadOnly]
        public int dataLength;

        public void Execute(int chunkIndex)
        {
            string path = dir + "/" + coords[chunkIndex].ToString() + ".txt";
            int startIndex = dataLength * chunkIndex;
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                for (int i = 0; i < dataLength; i++)
                {
                    int index = startIndex + i;
                    if (voxelData[index].render != 0)
                    {
                        sw.WriteLine(i);
                        sw.WriteLine(voxelData[index].userId);
                        sw.WriteLine(voxelData[index].color);
                    }
                }
            }
        }
    }
}
