using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameTimerManager : MonoBehaviour
{
    private static FrameTimerManager m_Singleton;

    private List<FrameTimer> m_Timers;
    private bool m_runFlag;

    private void Awake()
    {
        if (m_Singleton == null)
        {
            m_Singleton = this;
            m_Timers = new List<FrameTimer>();
        }
        else
            DestroyImmediate(m_Singleton);
    }

    private void Update()
    {
        if (!m_runFlag)
            return;
        for (int i = 0; i < m_Timers.Count; i++)
        {
            if (m_Timers[i].IsCounting)
                m_Timers[i].Increment();
        }
    }

    public static void StartAll()
    {
        m_Singleton.m_runFlag = true;
    }

    public static void StopAll()
    {
        m_Singleton.m_runFlag = false;
    }

    public static FrameTimer GetTimer(int frame_Interval, FrameTimerMode mode)
    {
        FrameTimer frameTimer = new FrameTimer(frame_Interval, mode);
        m_Singleton.m_Timers.Add(frameTimer);
        if (!m_Singleton.m_runFlag)
            m_Singleton.m_runFlag = true;
        return frameTimer;
    }

    public static void DisposeTimer(FrameTimer timer)
    {
        timer.OnTimeUp.RemoveAllListeners();
        m_Singleton.m_Timers.Remove(timer);
    }

}
