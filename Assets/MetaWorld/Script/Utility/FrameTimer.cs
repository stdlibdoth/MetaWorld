using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum FrameTimerMode
{
    OneShot,
    Repeat,
}

public class FrameTimer
{
    private FrameTimerMode m_Mode;
    private int m_Interval;
    private int m_Counter;
    private UnityEvent m_TimeUpEvent;
    private bool m_IsCountingFlag;

    public UnityEvent OnTimeUp
    {
        get { return m_TimeUpEvent; }
    }

    public bool IsCounting
    {
        get { return m_IsCountingFlag; }
    }
     

    public FrameTimer(int frame_Interval, FrameTimerMode mode)
    {
        m_Mode = mode;
        m_Counter = 0;
        m_TimeUpEvent = new UnityEvent();
        m_Interval = frame_Interval;
        m_IsCountingFlag = false;
    }

    public void Start()
    {
        m_IsCountingFlag = true;
    }

    public void Stop()
    {
        m_IsCountingFlag = false;
        m_Counter = 0;
    }

    public void Pause()
    {
        m_IsCountingFlag = false;
    }

    public void Reset(int interval)
    {
        m_Counter = 0;
        m_Interval = interval;
    }

    public void Increment()
    {
        m_Counter++;
        if (m_Counter >= m_Interval)
        {
            m_Counter = 0;
            m_TimeUpEvent.Invoke();
            if (m_Mode == FrameTimerMode.OneShot)
                m_IsCountingFlag = false;
        }
    }
}
