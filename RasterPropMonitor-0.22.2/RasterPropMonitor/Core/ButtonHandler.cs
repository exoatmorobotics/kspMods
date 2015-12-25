using UnityEngine;
using System;
using System.Collections.Generic;

namespace JSI
{
    public class SmarterButton : MonoBehaviour
    {
        private readonly List<HandlerID> clickHandlersID = new List<HandlerID>();
        private readonly List<HandlerID> releaseHandlersID = new List<HandlerID>();
        private readonly List<Action> clickHandlers = new List<Action>();
        private readonly List<Action> releaseHandlers = new List<Action>();
        private readonly List<PageTriggerSet> pageTriggers = new List<PageTriggerSet>();
        private Part part;

        private struct HandlerID
        {
            public Action<int> function;
            public int idValue;
        }

        private class PageTriggerSet
        {
            private int counter;
            private readonly Action<MonitorPage> selector;
            private readonly List<MonitorPage> pages = new List<MonitorPage>();

            public PageTriggerSet(Action<MonitorPage> function, MonitorPage page)
            {
                selector = function;
                pages.Add(page);
                counter = -1;
            }

            public bool Add(Action<MonitorPage> function, MonitorPage page)
            {
                if (function == selector)
                {
                    pages.Add(page);
                    return true;
                }
                return false;
            }

            public void ShowNext()
            {
                if (pages.Count > 0)
                {
                    if (counter < 0 || pages[counter].IsActive)
                    {
                        ++counter;
                        if (counter >= pages.Count)
                        {
                            counter = 0;
                        }
                    }
                    selector(pages[counter]);
                }
            }
        }

        public void OnMouseDown()
        {
            if (part != null)
            {
                Kerbal k = part.FindCurrentKerbal();
                if (k != null)
                {
                    // Disallow tourists using props
                    if (k.protoCrewMember.type == ProtoCrewMember.KerbalType.Tourist)
                    {
                        if (UnityEngine.Random.Range(0, 10) > 8)
                        {
                            ScreenMessages.PostScreenMessage(string.Format("Stop touching buttons, {0}!", k.name), 4.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                        else
                        {
                            ScreenMessages.PostScreenMessage(string.Format("Tourist {0} may not operate equipment.", k.name), 4.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                        return;
                    }
                }
            }
            foreach (PageTriggerSet monitor in pageTriggers)
            {
                monitor.ShowNext();
            }
            foreach (HandlerID consumer in clickHandlersID)
            {
                consumer.function(consumer.idValue);
            }
            foreach (Action clickHandler in clickHandlers)
            {
                clickHandler();
            }
        }

        public void OnMouseUp()
        {
            foreach (HandlerID consumer in releaseHandlersID)
            {
                consumer.function(consumer.idValue);
            }
            foreach (Action releaseHandler in releaseHandlers)
            {
                releaseHandler();
            }
        }

        private static SmarterButton AttachBehaviour(InternalProp thatProp, InternalModel thatModel, string buttonName)
        {

            if (thatModel == null)
            {
                string[] tokens = buttonName.Split('|');
                if (tokens.Length == 2)
                {
                    // First token is the button name, second is the prop ID.
                    int propID;
                    if (int.TryParse(tokens[1], out propID))
                    {
                        if (propID < thatProp.internalModel.props.Count)
                        {
                            if (propID < 0)
                            {
                                thatModel = thatProp.internalModel;
                            }
                            else
                            {
                                thatProp = thatProp.internalModel.props[propID];
                            }

                            buttonName = tokens[0].Trim();
                        }
                        else
                        {
                            Debug.LogError(string.Format("Could not find a prop with ID {0}", propID));
                        }
                    }
                }
                else
                {
                    buttonName = buttonName.Trim();
                }
            }
            try
            {
                GameObject buttonObject;
                buttonObject = thatModel == null ? thatProp.FindModelTransform(buttonName).gameObject : thatModel.FindModelTransform(buttonName).gameObject;
                SmarterButton thatComponent = buttonObject.GetComponent<SmarterButton>() ?? buttonObject.AddComponent<SmarterButton>();
                return thatComponent;
            }
            catch
            {
                Debug.LogError(string.Format(
                    "Could not register a button on transform named '{0}' in {2} named '{1}'. Check your configuration.",
                    buttonName, thatModel == null ? thatProp.propName : thatModel.name, thatModel == null ? "prop" : "internal model"));
            }
            return null;
        }

        public static void CreateButton(InternalProp thatProp, string buttonName, MonitorPage thatPage, Action<MonitorPage> handlerFunction, InternalModel thatModel = null)
        {
            SmarterButton buttonBehaviour;
            if ((buttonBehaviour = AttachBehaviour(thatProp, thatModel, buttonName)) == null)
            {
                return;
            }
            foreach (PageTriggerSet pageset in buttonBehaviour.pageTriggers)
            {
                if (pageset.Add(handlerFunction, thatPage))
                {
                    return;
                }
            }

            buttonBehaviour.pageTriggers.Add(new PageTriggerSet(handlerFunction, thatPage));
            buttonBehaviour.part = (thatModel == null) ? thatProp.part : thatModel.part;
        }

        public static void CreateButton(InternalProp thatProp, string buttonName, int numericID, Action<int> clickHandlerFunction, Action<int> releaseHandlerFunction, InternalModel thatModel = null)
        {
            SmarterButton buttonBehaviour;
            if ((buttonBehaviour = AttachBehaviour(thatProp, thatModel, buttonName)) == null)
            {
                return;
            }

            buttonBehaviour.clickHandlersID.Add(new HandlerID
            {
                function = clickHandlerFunction,
                idValue = numericID
            });
            buttonBehaviour.releaseHandlersID.Add(new HandlerID
            {
                function = releaseHandlerFunction,
                idValue = numericID
            });
            buttonBehaviour.part = (thatModel == null) ? thatProp.part : thatModel.part;
        }

        public static void CreateButton(InternalProp thatProp, string buttonName, Action handlerFunction, Action releaseHandlerFunction = null, InternalModel thatModel = null)
        {
            SmarterButton buttonBehaviour;
            if ((buttonBehaviour = AttachBehaviour(thatProp, thatModel, buttonName)) == null)
            {
                return;
            }
            buttonBehaviour.clickHandlers.Add(handlerFunction);
            if (releaseHandlerFunction != null)
            {
                buttonBehaviour.releaseHandlers.Add(releaseHandlerFunction);
            }
            buttonBehaviour.part = (thatModel == null) ? thatProp.part : thatModel.part;
        }
    }
}
