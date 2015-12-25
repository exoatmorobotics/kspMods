// Analysis disable once RedundantUsingDirective
using System;
using UnityEngine;

// FIXME: This module turned into total spaghetti by now and needs some serious rethinking.
namespace JSI
{
    public class FlyingCamera
    {
        private readonly Vessel ourVessel;
        private readonly Part ourPart;
        private GameObject cameraTransform;
        private Part cameraPart;
        private readonly int fxCameraIndex = 6;
        private readonly string[] knownCameraNames = 
        {
            "GalaxyCamera",
            "Camera ScaledSpace",
            "Camera VE Underlay", // Environmental Visual Enhancements plugin camera
            "Camera VE Overlay",  // Environmental Visual Enhancements plugin camera
            "Camera 01",
            "Camera 00",
            "FXCamera"
        };
        private readonly Camera[] cameraObject = { null, null, null, null, null, null, null };
        private readonly float cameraAspect;
        private bool enabled;
        private readonly RenderTexture screenTexture;
        private bool isReferenceCamera, isReferenceClawCamera;
        private ModuleGrappleNode clawModule;
        private Part referencePart;
        private const string referenceCamera = "CurrentReferenceDockingPortCamera";
        private readonly Quaternion referencePointRotation = Quaternion.Euler(-90, 0, 0);
        private float flickerChance;
        private int flickerMaxTime;
        private int flickerCounter;

        public float FOV { get; set; }

        public FlyingCamera(Part thatPart, RenderTexture screen, float aspect)
        {
            ourVessel = thatPart.vessel;
            ourPart = thatPart;
            screenTexture = screen;
            cameraAspect = aspect;
        }

        public void SetFlicker(float flicker, int flickerTime)
        {
            flickerChance = flicker;
            flickerMaxTime = flickerTime;
            flickerCounter = 0;
        }

        public bool PointCamera(string newCameraName, float initialFOV)
        {
            CleanupCameraObjects();
            if (!string.IsNullOrEmpty(newCameraName))
            {
                FOV = initialFOV;

                if (newCameraName == referenceCamera)
                {
                    JUtil.LogMessage(this, "Tracking reference point docking port camera.");
                    return PointToReferenceCamera();
                }
                else
                {
                    isReferenceClawCamera = false;
                    return CreateCameraObjects(newCameraName);
                }
            }
            else
            {
                return false;
            }
        }

        private bool PointToReferenceCamera()
        {
            isReferenceCamera = true;
            referencePart = ourVessel.GetReferenceTransformPart();
            ModuleDockingNode thatPort = null;
            ModuleGrappleNode thatClaw = null;
            foreach (PartModule thatModule in ourVessel.GetReferenceTransformPart().Modules)
            {
                thatPort = thatModule as ModuleDockingNode;
                thatClaw = thatModule as ModuleGrappleNode;
                if (thatPort != null || thatClaw != null)
                    break;
            }
            if (thatPort != null || thatClaw != null)
            {
                if (thatPort != null)
                {
                    cameraPart = thatPort.part;
                    cameraTransform = ourVessel.ReferenceTransform.gameObject;
                    isReferenceClawCamera = false;
                }
                else if (thatClaw != null)
                {
                    // Mihara: Dirty hack to get around the fact that claws have their reference transform inside the structure.
                    if (LocateCamera(ourVessel.GetReferenceTransformPart(), "ArticulatedCap"))
                    {
                        isReferenceClawCamera = true;
                        clawModule = thatClaw;
                    }
                    else
                    {
                        JUtil.LogMessage(this, "Claw was not a stock part. Falling back to reference transform position...");
                        cameraPart = thatClaw.part;
                        cameraTransform = ourVessel.ReferenceTransform.gameObject;
                    }
                }
                return CreateCameraObjects();
            }
            else
            {
                return false;
            }
        }

        private bool CreateCameraObjects(string newCameraName = null)
        {

            if (!string.IsNullOrEmpty(newCameraName))
            {
                isReferenceCamera = false;
                isReferenceClawCamera = false; clawModule = null;
                // First, we search our own part for this camera transform,
                // only then we search all other parts of the vessel.
                if (!LocateCamera(ourPart, newCameraName))
                {
                    foreach (Part thatpart in ourVessel.parts)
                    {
                        if (LocateCamera(thatpart, newCameraName))
                            break;
                    }
                }
            }
            if (cameraTransform != null)
            {
                for (int i = 0; i < knownCameraNames.Length; ++i)
                {
                    CameraSetup(i, knownCameraNames[i]);
                }
                enabled = true;
                //JUtil.LogMessage(this, "Switched to camera \"{0}\".", cameraTransform.name);
                return true;
            }
            else
            {
                //JUtil.LogMessage(this, "Tried to switch to camera \"{0}\" but camera was not found.", newCameraName);
                return false;
            }
        }

        private void CleanupCameraObjects()
        {
            if (enabled)
            {
                for (int i = 0; i < cameraObject.Length; i++)
                {
                    try
                    {
                        UnityEngine.Object.Destroy(cameraObject[i]);
                        // Analysis disable once EmptyGeneralCatchClause
                    }
                    catch
                    {
                        // Yes, that's really what it's supposed to be doing.
                    }
                    finally
                    {
                        cameraObject[i] = null;
                    }
                }
                enabled = false;
                //JUtil.LogMessage(this, "Turning camera off.");
            }
            cameraPart = null;
            cameraTransform = null;
        }

        private bool LocateCamera(Part thatpart, string transformName)
        {
            Transform location = thatpart.FindModelTransform(transformName);
            if (location != null)
            {
                cameraTransform = location.gameObject;
                cameraPart = thatpart;
                return true;
            }
            return false;
        }

        private void CameraSetup(int index, string sourceName)
        {
            Camera sourceCam = JUtil.GetCameraByName(sourceName);

            if (sourceCam != null)
            {
                var cameraBody = new GameObject();
                cameraBody.name = typeof(RasterPropMonitor).Name + index + cameraBody.GetInstanceID();
                cameraObject[index] = cameraBody.AddComponent<Camera>();

                // Just in case to support JSITransparentPod.
                cameraObject[index].cullingMask &= ~(1 << 16 | 1 << 20);

                cameraObject[index].CopyFrom(sourceCam);
                cameraObject[index].enabled = false;
                cameraObject[index].targetTexture = screenTexture;
                cameraObject[index].aspect = cameraAspect;
            }
        }

        public Quaternion CameraRotation(float yawOffset = 0.0f, float pitchOffset = 0.0f)
        {
            Quaternion rotation = cameraTransform.transform.rotation;
            if (isReferenceCamera)
                rotation *= referencePointRotation;
            Quaternion offset = Quaternion.Euler(new Vector3(pitchOffset, yawOffset, 0.0f));
            return rotation * offset;
        }

        public Transform GetTransform()
        {
            return cameraTransform.transform;
        }

        public Vector3 GetTransformForward()
        {
            return isReferenceCamera ? cameraTransform.transform.up : cameraTransform.transform.forward;
        }

        public bool Render(float yawOffset = 0.0f, float pitchOffset = 0.0f)
        {

            if (isReferenceCamera && ourVessel.GetReferenceTransformPart() != referencePart)
            {
                CleanupCameraObjects();
                PointToReferenceCamera();
            }

            if (isReferenceClawCamera && clawModule.state != "Ready")
            {
                return false;
            }

            if (!enabled)
                return false;

            if (cameraPart == null || cameraPart.vessel != FlightGlobals.ActiveVessel || cameraTransform == null || cameraTransform.transform == null)
            {
                CleanupCameraObjects();
                return false;
            }

            // Randomized camera flicker.
            if (flickerChance > 0 && flickerCounter == 0)
            {
                if (flickerChance > UnityEngine.Random.Range(0f, 1000f))
                {
                    flickerCounter = UnityEngine.Random.Range(1, flickerMaxTime);
                }
            }
            if (flickerCounter > 0)
            {
                flickerCounter--;
                return false;
            }

            Quaternion rotation = cameraTransform.transform.rotation;

            if (isReferenceCamera && !isReferenceClawCamera)
            {
                // Reference transforms of docking ports have the wrong orientation, so need an extra rotation applied before that.
                rotation *= referencePointRotation;
            }

            Quaternion offset = Quaternion.Euler(new Vector3(pitchOffset, yawOffset, 0.0f));
            rotation = rotation * offset;


            // This is a hack - FXCamera isn't always available, so I need to add and remove it in flight.
            // I don't know if there's a callback I can use to find when it's added, so brute force it for now.
            bool fxCameraExists = JUtil.DoesCameraExist(knownCameraNames[fxCameraIndex]);
            if (cameraObject[fxCameraIndex] == null)
            {
                if (fxCameraExists)
                {
                    CameraSetup(fxCameraIndex, knownCameraNames[fxCameraIndex]);
                }
            }
            else if (!fxCameraExists)
            {
                try
                {
                    UnityEngine.Object.Destroy(cameraObject[fxCameraIndex]);
                    // Analysis disable once EmptyGeneralCatchClause
                }
                catch
                {
                    // Yes, that's really what it's supposed to be doing.
                }
                finally
                {
                    cameraObject[fxCameraIndex] = null;
                }
            }

            for (int i = 0; i < cameraObject.Length; i++)
            {
                if (cameraObject[i] != null)
                {
                    // ScaledSpace camera and it's derived cameras from Visual Enhancements mod are special - they don't move.
                    if (i >= 3)
                    {
                        cameraObject[i].transform.position = cameraTransform.transform.position;
                    }
                    cameraObject[i].transform.rotation = rotation;
                    cameraObject[i].fieldOfView = FOV;
                    cameraObject[i].Render();
                }
            }
            return true;
        }
    }
}

