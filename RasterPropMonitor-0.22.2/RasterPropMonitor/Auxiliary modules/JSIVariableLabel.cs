using UnityEngine;

namespace JSI
{
    public class JSIVariableLabel : InternalModule
    {
        [KSPField]
        public string labelText = "<=0=>$&$ALTITUDE";
        [KSPField]
        public string transformName;
        [KSPField]
        public float fontSize = 0.008f;
        [KSPField]
        public int refreshRate = 10;
        [KSPField]
        public bool oneshot;
        private bool oneshotComplete;
        private InternalText textObj;
        private Transform textObjTransform;
        private int updateCountdown;
        // Annoying as it is, that is the only font actually available to InternalComponents for some bizarre reason,
        // even though I'm pretty sure there are quite a few other fonts in there.
        private const string fontName = "Arial";
        private string sourceString;
        private PersistenceAccessor persistence;

        public void Start()
        {
            textObjTransform = internalProp.FindModelTransform(transformName);
            textObj = InternalComponents.Instance.CreateText(fontName, fontSize, textObjTransform, string.Empty);
            // Force oneshot if there's no variables:
            oneshot |= !labelText.Contains("$&$");
            sourceString = labelText.UnMangleConfigText();
            if (!oneshot)
            {
                RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
                comp.UpdateDataRefreshRate(refreshRate);
            }
            persistence = new PersistenceAccessor(internalProp);
        }

        public void OnDestroy()
        {
            //JUtil.LogMessage(this, "OnDestroy()");
            persistence = null;
        }

        private bool UpdateCheck()
        {
            if (updateCountdown <= 0)
            {
                updateCountdown = refreshRate;
                return true;
            }
            updateCountdown--;
            return false;
        }

        public override void OnUpdate()
        {
            if (oneshotComplete && oneshot)
            {
                return;
            }
            if (!JUtil.VesselIsInIVA(vessel) || !UpdateCheck())
            {
                return;
            }

            RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
            textObj.text.Text = StringProcessor.ProcessString(sourceString, comp, persistence);
            oneshotComplete = true;
        }
    }
}

