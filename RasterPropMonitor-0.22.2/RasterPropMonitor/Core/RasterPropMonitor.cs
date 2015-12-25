using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JSI
{
    public class RasterPropMonitor : InternalModule
    {
        [KSPField]
        public string screenTransform = "screenTransform";
        [KSPField]
        public string fontTransform = "fontTransform";
        [KSPField]
        public string textureLayerID = "_MainTex";
        [KSPField]
        public string emptyColor = string.Empty;
        public Color emptyColorValue = Color.clear;
        [KSPField]
        public int screenWidth = 32;
        [KSPField]
        public int screenHeight = 8;
        [KSPField]
        public int screenPixelWidth = 512;
        [KSPField]
        public int screenPixelHeight = 256;
        [KSPField]
        public int fontLetterWidth = 16;
        [KSPField]
        public int fontLetterHeight = 32;
        [KSPField]
        public float cameraAspect = 2f;
        [KSPField]
        public int refreshDrawRate = 2;
        [KSPField]
        public int refreshTextRate = 5;
        [KSPField]
        public int refreshDataRate = 10;
        [KSPField]
        public string globalButtons;
        [KSPField]
        public string buttonClickSound;
        [KSPField]
        public float buttonClickVolume = 0.5f;
        [KSPField]
        public bool needsElectricCharge = true;
        [KSPField]
        public string defaultFontTint = string.Empty;
        public Color defaultFontTintValue = Color.white;
        [KSPField]
        public string noSignalTextureURL = string.Empty;
        [KSPField]
        public string fontDefinition = string.Empty;
        [KSPField]
        public bool doScreenshots = true;
        [KSPField]
        public bool oneshot = false;
        // This needs to be public so that pages can point it.
        public FlyingCamera cameraStructure;
        // Internal stuff.
        private TextRenderer textRenderer;
        private RenderTexture screenTexture;
        private Texture2D frozenScreen;
        // Local variables
        private int refreshDrawCountdown;
        private int refreshTextCountdown;
        private int vesselNumParts;
        private bool firstRenderComplete;
        private bool textRefreshRequired;
        private readonly List<MonitorPage> pages = new List<MonitorPage>();
        private MonitorPage activePage;
        private PersistenceAccessor persistence;
        private string persistentVarName;
        private string screenBuffer;
        private FXGroup audioOutput;
        private double electricChargeReserve;
        public Texture2D noSignalTexture;
        private Material screenMat;
        private bool startupComplete;
        private string fontDefinitionString = @" !""#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_`abcdefghijklmnopqrstuvwxyz{|}~Δ☊¡¢£¤¥¦§¨©ª«¬☋®¯°±²³´µ¶·¸¹º»¼½¾¿";

        private int loopsWithoutInitCounter = 0;
        private bool startupFailed = false;

        private bool ourPodIsTransparent = false;

        private static Texture2D LoadFont(object caller, InternalProp thisProp, string location)
        {
            Texture2D font = null;
            if (!string.IsNullOrEmpty(location))
            {
                if (GameDatabase.Instance.ExistsTexture(location.EnforceSlashes()))
                {
                    font = GameDatabase.Instance.GetTexture(location.EnforceSlashes(), false);
                    JUtil.LogMessage(caller, "Loading font texture from URL \"{0}\"", location);
                }
                else
                {
                    font = (Texture2D)thisProp.FindModelTransform(location).renderer.material.mainTexture;
                    JUtil.LogMessage(caller, "Loading font texture from a transform named \"{0}\"", location);
                }
            }
            return font;
        }

        public void Start()
        {

            // If we're not in the correct location, there's no point doing anything.
            if (!InstallationPathWarning.Warn())
            {
                return;
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                return;
            }

            try
            {
                // Install the calculator module.
                RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
                comp.UpdateDataRefreshRate(refreshDataRate);

                persistence = new PersistenceAccessor(internalProp);

                // Loading the font...
                List<Texture2D> fontTexture = new List<Texture2D>();
                fontTexture.Add(LoadFont(this, internalProp, fontTransform));

                // Damn KSP's config parser!!!
                if (!string.IsNullOrEmpty(emptyColor))
                {
                    emptyColorValue = ConfigNode.ParseColor32(emptyColor);
                }
                if (!string.IsNullOrEmpty(defaultFontTint))
                {
                    defaultFontTintValue = ConfigNode.ParseColor32(defaultFontTint);
                }

                if (!string.IsNullOrEmpty(fontDefinition))
                {
                    JUtil.LogMessage(this, "Loading font definition from {0}", fontDefinition);
                    fontDefinitionString = File.ReadAllLines(KSPUtil.ApplicationRootPath + "GameData/" + fontDefinition.EnforceSlashes(), Encoding.UTF8)[0];
                }

                // Now that is done, proceed to setting up the screen.

                screenTexture = new RenderTexture(screenPixelWidth, screenPixelHeight, 24, RenderTextureFormat.ARGB32);
                screenMat = internalProp.FindModelTransform(screenTransform).renderer.material;
                foreach (string layerID in textureLayerID.Split())
                {
                    screenMat.SetTexture(layerID.Trim(), screenTexture);
                }

                if (GameDatabase.Instance.ExistsTexture(noSignalTextureURL.EnforceSlashes()))
                {
                    noSignalTexture = GameDatabase.Instance.GetTexture(noSignalTextureURL.EnforceSlashes(), false);
                }

                // Create camera instance...
                cameraStructure = new FlyingCamera(part, screenTexture, cameraAspect);

                // The neat trick. IConfigNode doesn't work. No amount of kicking got it to work.
                // Well, we don't need it. GameDatabase, gimme config nodes for all props!
                foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("PROP"))
                {
                    // Now, we know our own prop name.
                    if (node.GetValue("name") == internalProp.propName)
                    {
                        // So this is the configuration of our prop in memory. Nice place.
                        // We know it contains at least one MODULE node, us.
                        // And we know our moduleID, which is the number in order of being listed in the prop.
                        // Therefore the module by that number is our module's own config node.

                        ConfigNode moduleConfig = node.GetNodes("MODULE")[moduleID];
                        ConfigNode[] pageNodes = moduleConfig.GetNodes("PAGE");

                        // Which we can now parse for page definitions.
                        for (int i = 0; i < pageNodes.Length; i++)
                        {
                            // Mwahahaha.
                            try
                            {
                                var newPage = new MonitorPage(i, pageNodes[i], this);
                                activePage = activePage ?? newPage;
                                if (newPage.isDefault)
                                    activePage = newPage;
                                pages.Add(newPage);
                            }
                            catch (ArgumentException e)
                            {
                                JUtil.LogMessage(this, "Warning - {0}", e);
                            }

                        }

                        // Now that all pages are loaded, we can use the moment in the loop to suck in all the extra fonts.
                        foreach (string value in moduleConfig.GetValues("extraFont"))
                        {
                            fontTexture.Add(LoadFont(this, internalProp, value));
                        }

                        break;
                    }
                }
                JUtil.LogMessage(this, "Done setting up pages, {0} pages ready.", pages.Count);

                textRenderer = new TextRenderer(fontTexture, new Vector2((float)fontLetterWidth, (float)fontLetterHeight), fontDefinitionString, 17, screenPixelWidth, screenPixelHeight);

                // Load our state from storage...
                persistentVarName = "activePage" + internalProp.propID;
                int activePageID = persistence.GetVar(persistentVarName, pages.Count);
                if (activePageID < pages.Count)
                {
                    activePage = pages[activePageID];
                }
                activePage.Active(true);

                // If we have global buttons, set them up.
                if (!string.IsNullOrEmpty(globalButtons))
                {
                    string[] tokens = globalButtons.Split(',');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string buttonName = tokens[i].Trim();
                        // Notice that holes in the global button list ARE legal.
                        if (!string.IsNullOrEmpty(buttonName))
                            SmarterButton.CreateButton(internalProp, buttonName, i, GlobalButtonClick, GlobalButtonRelease);
                    }
                }

                audioOutput = JUtil.SetupIVASound(internalProp, buttonClickSound, buttonClickVolume, false);

                // One last thing to make sure of: If our pod is transparent, we're always active.
                ourPodIsTransparent = JUtil.IsPodTransparent(part);

                // And if the try block never completed, startupComplete will never be true.
                startupComplete = true;
            }
            catch
            {
                JUtil.AnnoyUser(this);
                startupFailed = true;
                // We can also disable ourselves, that should help.
                enabled = false;
                // And now that we notified the user that config is borked, we rethrow the exception so that
                // it gets logged and we can debug.
                throw;
            }

        }

        public void OnDestroy()
        {
            // Makes sure we don't leak our render texture
            if (screenTexture != null)
            {
                screenTexture.Release();
                screenTexture = null;
            }
            if (frozenScreen != null)
            {
                Destroy(frozenScreen);
            }
            if (screenMat != null)
            {
                Destroy(screenMat);
            }
            persistence = null;
        }

        private static void PlayClickSound(FXGroup audioOutput)
        {
            if (audioOutput != null)
            {
                audioOutput.audio.Play();
            }
        }

        public void GlobalButtonClick(int buttonID)
        {
            if (needsElectricCharge && electricChargeReserve < 0.01d)
            {
                return;
            }
            if (activePage.GlobalButtonClick(buttonID))
            {
                PlayClickSound(audioOutput);
            }
        }

        public void GlobalButtonRelease(int buttonID)
        {
            // Or do we allow a button release to have effects?
            /* Mihara: Yes, I think we should. Otherwise if the charge
             * manages to run out in the middle of a pressed button, it will never stop.
            if (needsElectricCharge && electricChargeReserve < 0.01d)
                return;
            */
            activePage.GlobalButtonRelease(buttonID);
        }

        private MonitorPage FindPageByName(string pageName)
        {
            if (!string.IsNullOrEmpty(pageName))
            {
                foreach (MonitorPage page in pages)
                {
                    if (page.name == pageName)
                        return page;
                }
            }
            return null;
        }

        public void PageButtonClick(MonitorPage triggeredPage)
        {
            if (needsElectricCharge && electricChargeReserve < 0.01d)
            {
                return;
            }

            // Apply page redirect like this:
            triggeredPage = FindPageByName(activePage.ContextRedirect(triggeredPage.name)) ?? triggeredPage;
            if (triggeredPage != activePage && (activePage.SwitchingPermitted(triggeredPage.name) || triggeredPage.unlocker))
            {
                activePage.Active(false);
                activePage = triggeredPage;
                activePage.Active(true);
                persistence.SetVar(persistentVarName, activePage.pageNumber);
                refreshDrawCountdown = refreshTextCountdown = 0;
                firstRenderComplete = false;
                PlayClickSound(audioOutput);
            }
        }

        // Update according to the given refresh rate.
        private bool UpdateCheck()
        {
            refreshDrawCountdown--;
            refreshTextCountdown--;
            if (vesselNumParts != vessel.Parts.Count)
            {
                refreshDrawCountdown = 0;
                refreshTextCountdown = 0;
                vesselNumParts = vessel.Parts.Count;
            }
            if (refreshTextCountdown <= 0)
            {
                textRefreshRequired = true;
                refreshTextCountdown = refreshTextRate;
            }

            if (refreshDrawCountdown <= 0)
            {
                refreshDrawCountdown = refreshDrawRate;
                return true;
            }

            return false;
        }

        private void RenderScreen()
        {
            RenderTexture backupRenderTexture = RenderTexture.active;

            if (!screenTexture.IsCreated())
            {
                screenTexture.Create();
            }
            screenTexture.DiscardContents();
            RenderTexture.active = screenTexture;

            if (needsElectricCharge && electricChargeReserve < 0.01d)
            {
                // If we're out of electric charge, we're drawing a blank screen.
                GL.Clear(true, true, emptyColorValue);
                RenderTexture.active = backupRenderTexture;
                return;
            }

            // This is the important witchcraft. Without that, DrawTexture does not print where we expect it to.
            // Cameras don't care because they have their own matrices, but DrawTexture does.
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, screenPixelWidth, screenPixelHeight, 0);

            // Actual rendering of the background is delegated to the page object.
            activePage.RenderBackground(screenTexture);

            if (!string.IsNullOrEmpty(activePage.Text))
            {
                textRenderer.Render(screenTexture, screenBuffer, activePage);
            }

            activePage.RenderOverlay(screenTexture);
            GL.PopMatrix();

            RenderTexture.active = backupRenderTexture;
        }

        private void FillScreenBuffer()
        {
            RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
            StringBuilder bf = new StringBuilder();
            string[] linesArray = activePage.Text.Split(JUtil.LineSeparator, StringSplitOptions.None);
            for (int i = 0; i < linesArray.Length; i++)
            {
                bf.AppendLine(StringProcessor.ProcessString(linesArray[i], comp, persistence));
            }
            textRefreshRequired = false;
            screenBuffer = bf.ToString();

            // This is where we request electric charge reserve. And if we don't have any, well... :)
            CheckForElectricCharge();
        }

        private void CheckForElectricCharge()
        {
            if (needsElectricCharge)
            {
                RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
                electricChargeReserve = (double)comp.ProcessVariable("SYSR_ELECTRICCHARGE", null);
            }
        }

        public override void OnUpdate()
        {

            if (HighLogic.LoadedSceneIsEditor)
            {
                return;
            }

            // If we didn't complete startup, we can't do anything anyway.
            // The only trouble is that situations where update happens before startup is complete do happen sometimes,
            // particularly when docking, so we can't use it to detect being broken by a third party plugin.
            if (!startupComplete)
            {
                loopsWithoutInitCounter++;
                return;
            }

            // If we were spawned while in editor, we need to blank the screen out and halt.
            // So we switch to oneshot mode.
            //oneshot |= HighLogic.LoadedSceneIsEditor;

            if (!ourPodIsTransparent && !JUtil.UserIsInPod(part))
            {
                return;
            }

            // Screenshots need to happen in at this moment, because otherwise they may miss.
            if (doScreenshots && GameSettings.TAKE_SCREENSHOT.GetKeyDown() && part.ActiveKerbalIsLocal())
            {
                // Let's try to save a screenshot.
                JUtil.LogMessage(this, "SCREENSHOT!");

                string screenshotName = string.Format("{0}{1}{2:yyyy-MM-dd_HH-mm-ss}_{4}_{3}.png",
                                            KSPUtil.ApplicationRootPath, "Screenshots/monitor", DateTime.Now, internalProp.propID, part.GetInstanceID());
                var screenshot = new Texture2D(screenTexture.width, screenTexture.height);
                RenderTexture backupRenderTexture = RenderTexture.active;
                RenderTexture.active = screenTexture;
                screenshot.ReadPixels(new Rect(0, 0, screenTexture.width, screenTexture.height), 0, 0);
                RenderTexture.active = backupRenderTexture;
                var bytes = screenshot.EncodeToPNG();
                Destroy(screenshot);
                File.WriteAllBytes(screenshotName, bytes);
            }

            if (!UpdateCheck())
            {
                return;
            }

            if (!activePage.isMutable)
            {
                // In case the page is empty and has no camera, the screen is treated as turned off and blanked once.
                if (!firstRenderComplete)
                {
                    FillScreenBuffer();
                    RenderScreen();
                    firstRenderComplete = true;
                }
                else
                {
                    CheckForElectricCharge();
                    if (needsElectricCharge && electricChargeReserve < 0.01d)
                    {
                        RenderScreen();
                    }
                }
            }
            else
            {
                if (textRefreshRequired)
                {
                    FillScreenBuffer();
                }
                RenderScreen();
                firstRenderComplete = true;
            }

            // Oneshot screens: We create a permanent texture from our RenderTexture if the first pass of the render is complete,
            // set it in place of the rendertexture -- and then we selfdestruct.
            // MOARdV: Except we don't want to self-destruct, because we will leak the frozenScreen texture.
            if (oneshot && firstRenderComplete)
            {
                frozenScreen = new Texture2D(screenTexture.width, screenTexture.height);
                RenderTexture backupRenderTexture = RenderTexture.active;
                RenderTexture.active = screenTexture;
                frozenScreen.ReadPixels(new Rect(0, 0, screenTexture.width, screenTexture.height), 0, 0);
                RenderTexture.active = backupRenderTexture;
                foreach (string layerID in textureLayerID.Split())
                {
                    screenMat.SetTexture(layerID.Trim(), frozenScreen);
                }
            }
        }

        public void OnApplicationPause(bool pause)
        {
            firstRenderComplete &= pause;
        }

        public void LateUpdate()
        {

            if (HighLogic.LoadedSceneIsEditor)
                return;

            // If we reached a set number of update loops and startup still didn't happen, we're getting killed by a third party module.
            // We might STILL be getting killed by a third party module even during update, but I hope this will catch at least some cases.
            if (!startupFailed && loopsWithoutInitCounter > 600)
            {
                ScreenMessages.PostScreenMessage("RasterPropMonitor cannot complete initialization.", 120, ScreenMessageStyle.UPPER_CENTER);
                ScreenMessages.PostScreenMessage("The cause is usually some OTHER broken mod.", 120, ScreenMessageStyle.UPPER_CENTER);
                loopsWithoutInitCounter = 0;
            }
        }

    }
}

