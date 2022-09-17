using System;
using UnityEngine;

namespace StaticCamera;

public interface ICommonCameraAPI
{
	(OWCamera, Camera) CreateCustomCamera(string name, Action<OWCamera> postInitMethod);
	void ExitCamera(OWCamera OWCamera);
	void EnterCamera(OWCamera OWCamera);
}
