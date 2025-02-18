using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/***
 * 
 * XRInputDevice detects the XR input devices and update the transform with actual position/rotation of the device if shouldSynchDevicePosition boolean is set to true
 * 
 ***/

namespace Fusion.XR.Shared.Rig
{
    public class XRInputDevice : MonoBehaviour
    {
        [Header("Detected input device")]
        [SerializeField] bool isDeviceFound = false;
        public InputDevice device;

        [Header("Synchronisation")]
        public bool shouldSynchDevicePosition = true;

        [Header("Positioning timing")]
        public bool updateOnAfterInputSystemUpdate = true;

        protected virtual InputDeviceCharacteristics DesiredCharacteristics => InputDeviceCharacteristics.TrackedDevice;
        protected bool isUsingOculusPlugin = false;
        const string OcculusDeviceName = "oculus display"; // const string OpenXRPluginDeviceName = "OpenXR Display";

#if ENABLE_INPUT_SYSTEM
        protected void OnEnable()
        {
            UnityEngine.InputSystem.InputSystem.onAfterUpdate += OnAfterInputSystemUpdate;

        }

        protected void OnDisable()
        {
            UnityEngine.InputSystem.InputSystem.onAfterUpdate -= OnAfterInputSystemUpdate;
        }
#endif

        public void OnAfterInputSystemUpdate()
        {
            if (updateOnAfterInputSystemUpdate)
            {
                UpdatePosition();
            }
        }

        public virtual void DetectDevice()
        {
            if (isDeviceFound) return;
            foreach (var d in DeviceLookup())
            {
                device = d;
                isDeviceFound = true;
                isUsingOculusPlugin = XRSettings.loadedDeviceName == OcculusDeviceName;
                break;
            }
        }
        public virtual List<InputDevice> DeviceLookup()
        {
            InputDeviceCharacteristics desiredCharacteristics = DesiredCharacteristics;
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, devices);
            return devices;
        }

        protected virtual void Update()
        {
            UpdatePosition();
        }

        protected void UpdatePosition()
        {
            if (shouldSynchDevicePosition)
            {
                DetectDevice();
                if (isDeviceFound && device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation))
                {
                    transform.localRotation = AdaptRotation(rotation);
                }
                if (isDeviceFound && device.TryGetFeatureValue(CommonUsages.devicePosition, out var position))
                {
                    transform.localPosition = AdaptPosition(position);
                }

            }
        }

        protected virtual Vector3 AdaptPosition(Vector3 pos)
        {
            return pos;
        }

        protected virtual Quaternion AdaptRotation(Quaternion rot)
        {
            return rot;
        }
    }
}
