using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEditingCam
{
    public void Zoom(float step, MinMax range);
    public void SetZoom(float zoom_value);
    public void Pitch(float step, MinMax range);
    public void SetPitch(float pitch_angle);
    public void Rotate(float step);
    public void SetRotation(float angle);
}
