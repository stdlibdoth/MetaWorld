using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;


public class VoxelFormatter : IFormatter<Voxel[]>
{
    private Task m_exportingTask;
    private Action m_onExportAction;


    private Task m_readingTask;
    private FrameTimer m_statusTimer;

    public VoxelFormatter()
    {
        m_statusTimer = FrameTimerManager.GetTimer(1, FrameTimerMode.Repeat);
        m_statusTimer.OnTimeUp.AddListener(UpdateTaskStatus);
    }


    public void Export(Voxel[] data, Vector3Int coord, string dir, Action onExport)
    {
        if (m_exportingTask.Status == TaskStatus.Running || m_readingTask.Status == TaskStatus.Running)
            return;
        m_exportingTask = new Task(() =>
        {
            string path = dir + "/" + coord.ToString() + ".txt";
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                string str = "";
                for (int i = 0; i < data.Length; i++)
                {
                    sw.WriteLine(data[i].render);
                    sw.WriteLine(data[i].userId);
                    sw.WriteLine(data[i].color);
                }
                sw.Write(str);
            }
        });
        m_exportingTask.Start();
        if (!m_statusTimer.IsCounting)
            m_statusTimer.Start();
        m_onExportAction = onExport;
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


    public void ReadData(string dir, Vector3Int coord, int data_length, Action<Voxel[]> onReadAction)
    {
        if (m_exportingTask.Status == TaskStatus.Running || m_readingTask.Status == TaskStatus.Running)
            return;
        Task readTask = new Task(() =>
        {
            Voxel[] data = new Voxel[data_length];
            int indexCounter = 0;
            string path = dir + "/" + coord.ToString() + ".txt";
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    data[indexCounter].render = int.Parse(line);

                    line = sr.ReadLine();
                    data[indexCounter].userId = int.Parse(line);

                    line = sr.ReadLine();
                    float r = float.Parse(line.Substring(5, 5));
                    float g = float.Parse(line.Substring(11, 5));
                    float b = float.Parse(line.Substring(17, 5));
                    float a = float.Parse(line.Substring(23, 5));
                    data[indexCounter].color = new Color(r, g, b, a);
                    indexCounter++;
                }
            }
            onReadAction.Invoke(data);
        });
        readTask.Start();
        if (!m_statusTimer.IsCounting)
            m_statusTimer.Start();
    }


    private void UpdateTaskStatus()
    {
        if (m_exportingTask.Status == TaskStatus.RanToCompletion
            && m_readingTask.Status == TaskStatus.RanToCompletion)
            m_statusTimer.Stop();


        if (m_exportingTask.Status == TaskStatus.RanToCompletion)
        {
            m_onExportAction.Invoke();
            m_exportingTask.Dispose();
        }
        if (m_readingTask.Status == TaskStatus.RanToCompletion)
        {
            m_readingTask.Dispose();
        }
    }

}
