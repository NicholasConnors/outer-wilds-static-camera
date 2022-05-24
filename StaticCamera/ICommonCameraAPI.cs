using System;
using UnityEngine;

namespace StaticCamera
{
    public interface ICommonCameraAPI
    {
        void RegisterCustomCamera(OWCamera OWCamera);
        (OWCamera, Camera) CreateCustomCamera(string name);
    }
}
