﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace JSI
{
    /// <summary>
    /// JSIMechJeb provides an interface with the MechJeb plugin using
    /// reflection, which allows us to avoid the need for hard dependencies.
    /// </summary>
    internal class JSIMechJeb : IJSIModule
    {
        #region Reflection Definitions
        // MechJebCore
        private readonly Type mjMechJebCore_t;
        // MechJebCore.GetComputerModule(string)
        private readonly MethodInfo mjGetComputerModule;
        // MechJebCore.target
        private readonly FieldInfo mjCoreTarget;
        // MechJebCore.node
        private readonly FieldInfo mjCoreNode;
        // MechJebCore.attitude
        private readonly FieldInfo mjCoreAttitude;
        // MechJebCore.vesselState
        private readonly FieldInfo mjCoreVesselState;

        // AbsoluteVector
        // AbsoluteVector.latitude
        private readonly FieldInfo mjAbsoluteVectorLat;
        // AbsoluteVector.longitude
        private readonly FieldInfo mjAbsoluteVectorLon;
        // AbsoluteVector.(double)
        private readonly MethodInfo mjAbsoluteVectorToDouble;

        // MechJebModuleLandingPredictions
        // MechJebModuleLandingPredictions.GetResult()
        private readonly MethodInfo mjPredictionsGetResult;

        // ReentrySimulation.Result
        // ReentrySimulation.Result.outcome
        private readonly FieldInfo mjReentryOutcome;
        // ReentrySimulation.Result.endPosition
        private readonly FieldInfo mjReentryEndPosition;

        // ComputerModule
        // ComputerModule.enabled (get)
        private readonly MethodInfo mjModuleEnabled;
        private readonly FieldInfo mjModuleUsers;

        // MechJebModuleStageStats
        // MechJebModuleStageStats.RequestUpdate()
        private readonly MethodInfo mjRequestUpdate;
        // MechJebModuleStageStats.vacStats[]
        private readonly FieldInfo mjVacStageStats;
        // MechJebModuleStageStats.atmoStats[]
        private readonly FieldInfo mjAtmStageStats;

        // MechJebModuleTargetController
        // MechJebModuleTargetController.targetLatitude
        private readonly FieldInfo mjTargetLongitude;
        // MechJebModuleTargetController.targetLatitude
        private readonly FieldInfo mjTargetLatitude;
        // MechJebModuleTargetController.PositionTargetExists (get)
        private readonly MethodInfo mjGetPositionTargetExists;
        // MechJebModuleTargetController.NormalTargetExists (get)
        private readonly MethodInfo mjGetNormalTargetExists;
        // TargetOrbit (get)
        private readonly MethodInfo mjGetTargetOrbit;

        // MechJebModuleSmartASS
        // MechJebModuleSmartASS.target
        private readonly FieldInfo mjSmartassTarget;
        // MechJebModuleSmartASS.Engage
        private readonly MethodInfo mjSmartassEngage;
        // MechJebModuleSmartASS.forceRol
        private readonly FieldInfo mjSmartassForceRol;
        // MechJebModuleSmartASS.rol
        private readonly FieldInfo mjSmartassRol;
        // MechJebModuleSmartASS.ModeTexts
        public static string[] ModeTexts;
        // MechJebModuleSmartASS.TargetTexts
        public static string[] TargetTexts;

        // MechJebModuleNodeExecutor
        // MechJebModuleNodeExecutor.ExecuteOneNode(obj controller)
        private readonly MethodInfo mjExecuteOneNode;
        // MechJebModuleNodeExecutor.Abort()
        private readonly MethodInfo mjAbortNode;

        // FuelFlowSimulation.StageStats
        // FuelFlowSimulation.StageStats.deltaV
        private readonly FieldInfo mjStageDv;
        // FuelFlowSimulation.StageStats[].Length
        private readonly MethodInfo mjStageStatsGetLength;
        // FuelFlowSimulation.StageStats[].Get
        private readonly MethodInfo mjStageStatsGetIndex;

        // UserPool
        // UserPool.Add
        private readonly MethodInfo mjAddUser;
        // UserPool.Remove
        private readonly MethodInfo mjRemoveUser;
        // UserPool.Contains
        private readonly MethodInfo mjContainsUser;

        // VesselState
        // VesselState.TerminalVelocity
        private readonly MethodInfo mjTerminalVelocity;

        // MechJebModuleLandingAutopilot
        // MechJebModuleLandingAutopilot.LandAtPositionTarget
        private readonly MethodInfo mjLandAtPositionTarget;
        // MechJebModuleLandingAutopilot.LandUntargeted
        private readonly MethodInfo mjLandUntargeted;
        // MechJebModuleLandingAutopilot.StopLanding
        private readonly MethodInfo mjStopLanding;

        // EditableDouble
        // EditableDouble.val (get)
        private readonly MethodInfo mjSetEditableDouble;
        // EditableDouble.val (set)
        private readonly MethodInfo mjGetEditableDouble;

        // VesselExtensions.GetMasterMechJeb()
        private readonly MethodInfo mjGetMasterMechJeb;
        // VesselExtensions.PlaceManeuverNode()
        private readonly MethodInfo mjPlaceManeuverNode;

        // OrbitalManeuverCalculator
        // OrbitalManeuverCalculator.mjDeltaVAndTimeForHohmannTransfer
        private readonly MethodInfo mjDeltaVAndTimeForHohmannTransfer;
        // OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection
        private readonly MethodInfo mjDeltaVAndTimeForInterplanetaryTransferEjection;
        // OrbitalManeuverCalculator.DeltaVToCircularize
        private readonly MethodInfo mjDeltaVToCircularize;
        #endregion

        #region MechJeb enum imports
        public enum Mode
        {
            ORBITAL = 0,
            SURFACE = 1,
            TARGET = 2,
            ADVANCED = 3,
            AUTO = 4,
        }

        public enum Target
        {
            OFF = 0,
            KILLROT = 1,
            NODE = 2,
            SURFACE = 3,
            PROGRADE = 4,
            RETROGRADE = 5,
            NORMAL_PLUS = 6,
            NORMAL_MINUS = 7,
            RADIAL_PLUS = 8,
            RADIAL_MINUS = 9,
            RELATIVE_PLUS = 10,
            RELATIVE_MINUS = 11,
            TARGET_PLUS = 12,
            TARGET_MINUS = 13,
            PARALLEL_PLUS = 14,
            PARALLEL_MINUS = 15,
            ADVANCED = 16,
            AUTO = 17,
            SURFACE_PROGRADE = 18,
            SURFACE_RETROGRADE = 19,
            HORIZONTAL_PLUS = 20,
            HORIZONTAL_MINUS = 21,
            VERTICAL_PLUS = 22,
        }

        // Imported directly from MJ
        public static readonly Mode[] Target2Mode = new Mode[] { Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.SURFACE, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.ADVANCED, Mode.AUTO, Mode.SURFACE, Mode.SURFACE, Mode.SURFACE, Mode.SURFACE, Mode.SURFACE };

        public enum TimeReference
        {
            COMPUTED = 0,
            X_FROM_NOW = 1,
            APOAPSIS = 2,
            PERIAPSIS = 3,
            ALTITUDE = 4,
            EQ_ASCENDING = 5,
            EQ_DESCENDING = 6,
            REL_ASCENDING = 7,
            REL_DESCENDING = 8,
            CLOSEST_APPROACH = 9,
        }
        #endregion
        
        private readonly bool mjFound;

        private bool landingCurrent, deltaVCurrent;
        private double deltaV, deltaVStage;

        private double landingLat, landingLon, landingAlt, landingErr = -1.0;

        private object activeJeb = null;

        public JSIMechJeb(Vessel _vessel)
            : base(_vessel)
        {
            try
            {
                var loadedMechJebAssy = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.name == "MechJeb2");

                if (loadedMechJebAssy == null)
                {
                    mjFound = false;
                    if (JUtil.debugLoggingEnabled)
                    {
                        JUtil.LogMessage(this, "A supported version of MechJeb is {0}", (mjFound) ? "present" : "not available");
                    }
                    return;
                }

                //--- Process all the reflection info
                // MechJebCore
                mjMechJebCore_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.MechJebCore");
                if (mjMechJebCore_t == null)
                {
                    mjFound = false;
                    if (JUtil.debugLoggingEnabled)
                    {
                        JUtil.LogMessage(this, "A supported version of MechJeb is {0}", (mjFound) ? "present" : "not available");
                    }
                    return;
                }
                mjGetComputerModule = mjMechJebCore_t.GetMethod("GetComputerModule", new Type[] { typeof(string) });
                if (mjGetComputerModule == null)
                {
                    throw new NotImplementedException("mjGetComputerModule");
                }
                mjCoreTarget = mjMechJebCore_t.GetField("target", BindingFlags.Instance | BindingFlags.Public);
                if (mjCoreTarget == null)
                {
                    throw new NotImplementedException("mjCoreTarget");
                }
                mjCoreNode = mjMechJebCore_t.GetField("node", BindingFlags.Instance | BindingFlags.Public);
                if (mjCoreNode == null)
                {
                    throw new NotImplementedException("mjCoreNode");
                }
                mjCoreAttitude = mjMechJebCore_t.GetField("attitude", BindingFlags.Instance | BindingFlags.Public);
                if (mjCoreAttitude == null)
                {
                    throw new NotImplementedException("mjCoreAttitude");
                }
                mjCoreVesselState = mjMechJebCore_t.GetField("vesselState", BindingFlags.Instance | BindingFlags.Public);
                if (mjCoreVesselState == null)
                {
                    throw new NotImplementedException("mjCoreVesselState");
                }

                // VesselExtensions
                Type mjVesselExtensions_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.VesselExtensions");
                if (mjVesselExtensions_t == null)
                {
                    throw new NotImplementedException("mjVesselExtensions_t");
                }
                mjGetMasterMechJeb = mjVesselExtensions_t.GetMethod("GetMasterMechJeb", BindingFlags.Static | BindingFlags.Public);
                if (mjGetMasterMechJeb == null)
                {
                    throw new NotImplementedException("mjGetMasterMechJeb");
                }
                mjPlaceManeuverNode = mjVesselExtensions_t.GetMethod("PlaceManeuverNode", BindingFlags.Static | BindingFlags.Public);
                if (mjPlaceManeuverNode == null)
                {
                    throw new NotImplementedException("mjPlaceManeuverNode");
                }

                // VesselState
                Type mjVesselState_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.VesselState");
                if (mjVesselState_t == null)
                {
                    throw new NotImplementedException("mjVesselState_t");
                }
                mjTerminalVelocity = mjVesselState_t.GetMethod("TerminalVelocity", BindingFlags.Instance | BindingFlags.Public);
                if (mjTerminalVelocity == null)
                {
                    throw new NotImplementedException("mjTerminalVelocity");
                }

                // MechJebModuleLandingPredictions
                Type mjModuleLandingPredictions_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.MechJebModuleLandingPredictions");
                if (mjModuleLandingPredictions_t == null)
                {
                    throw new NotImplementedException("mjModuleLandingPredictions_t");
                }
                mjPredictionsGetResult = mjModuleLandingPredictions_t.GetMethod("GetResult", BindingFlags.Instance | BindingFlags.Public);
                if (mjPredictionsGetResult == null)
                {
                    throw new NotImplementedException("mjPredictionsGetResult");
                }

                // MuMech.ReentrySimulation.Result
                Type mjReentrySim_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.ReentrySimulation");
                if (mjReentrySim_t == null)
                {
                    throw new NotImplementedException("mjReentrySim_t");
                }
                Type mjReentryResult_t = mjReentrySim_t.GetNestedType("Result");
                if (mjReentryResult_t == null)
                {
                    throw new NotImplementedException("mjReentryResult_t");
                }
                mjReentryOutcome = mjReentryResult_t.GetField("outcome", BindingFlags.Instance | BindingFlags.Public);
                if (mjReentryOutcome == null)
                {
                    throw new NotImplementedException("mjReentryOutcome");
                }
                mjReentryEndPosition = mjReentryResult_t.GetField("endPosition", BindingFlags.Instance | BindingFlags.Public);
                if (mjReentryEndPosition == null)
                {
                    throw new NotImplementedException("mjReentryEndPosition");
                }

                // MechJebModuleStageStats
                Type mjModuleStageStats_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.MechJebModuleStageStats");
                if (mjModuleStageStats_t == null)
                {
                    throw new NotImplementedException("mjModuleStageStats_t");
                }
                mjRequestUpdate = mjModuleStageStats_t.GetMethod("RequestUpdate", BindingFlags.Instance | BindingFlags.Public);
                if (mjRequestUpdate == null)
                {
                    throw new NotImplementedException("mjRequestUpdate");
                }
                mjVacStageStats = mjModuleStageStats_t.GetField("vacStats", BindingFlags.Instance | BindingFlags.Public);
                if (mjVacStageStats == null)
                {
                    throw new NotImplementedException("mjVacStageStats");
                }

                // Updated MechJeb (post 2.5.1) switched from using KER back to
                // its internal FuelFlowSimulation.  This sim uses an array of
                // structs, which entails a couple of extra hoops to jump through
                // when reading via reflection.
                mjAtmStageStats = mjModuleStageStats_t.GetField("atmoStats", BindingFlags.Instance | BindingFlags.Public);
                if (mjAtmStageStats == null)
                {
                    throw new NotImplementedException("mjAtmStageStats");
                }

                PropertyInfo mjStageStatsLength = mjVacStageStats.FieldType.GetProperty("Length");
                if (mjStageStatsLength == null)
                {
                    throw new NotImplementedException("mjStageStatsLength");
                }
                mjStageStatsGetLength = mjStageStatsLength.GetGetMethod();
                if (mjStageStatsGetLength == null)
                {
                    throw new NotImplementedException("mjStageStatsGetLength");
                }
                mjStageStatsGetIndex = mjVacStageStats.FieldType.GetMethod("Get");
                if (mjStageStatsGetIndex == null)
                {
                    throw new NotImplementedException("mjStageStatsGetIndex");
                }

                // AbsoluteVector
                Type mjAbsoluteVector_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.AbsoluteVector");
                if (mjAbsoluteVector_t == null)
                {
                    throw new NotImplementedException("mjAbsoluteVector_t");
                }
                mjAbsoluteVectorLat = mjAbsoluteVector_t.GetField("latitude", BindingFlags.Instance | BindingFlags.Public);
                if (mjAbsoluteVectorLat == null)
                {
                    throw new NotImplementedException("mjAbsoluteVectorLat");
                }
                mjAbsoluteVectorLon = mjAbsoluteVector_t.GetField("longitude", BindingFlags.Instance | BindingFlags.Public);
                if (mjAbsoluteVectorLon == null)
                {
                    throw new NotImplementedException("mjAbsoluteVectorLon");
                }

                // MuMech.FuelFlowSimulation
                Type mjFuelFlowSimulation_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.FuelFlowSimulation");
                if (mjFuelFlowSimulation_t == null)
                {
                    throw new NotImplementedException("mjFuelFlowSimulation_t");
                }
                // MuMech.FuelFlowSimulation.Stats
                Type mjFuelFlowSimulationStats_t = mjFuelFlowSimulation_t.GetNestedType("Stats");
                if (mjFuelFlowSimulationStats_t == null)
                {
                    throw new NotImplementedException("mjFuelFlowSimulationStats_t");
                }
                mjStageDv = mjFuelFlowSimulationStats_t.GetField("deltaV", BindingFlags.Instance | BindingFlags.Public);
                if (mjStageDv == null)
                {
                    throw new NotImplementedException("mjStageDv");
                }

                // MechJebModuleTargetController
                Type mjModuleTargetController_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.MechJebModuleTargetController");
                if (mjModuleTargetController_t == null)
                {
                    throw new NotImplementedException("mjModuleTargetController_t");
                }
                mjTargetLongitude = mjModuleTargetController_t.GetField("targetLongitude", BindingFlags.Instance | BindingFlags.Public);
                if (mjTargetLongitude == null)
                {
                    throw new NotImplementedException("mjTargetLongitude");
                }
                mjTargetLatitude = mjModuleTargetController_t.GetField("targetLatitude", BindingFlags.Instance | BindingFlags.Public);
                if (mjTargetLatitude == null)
                {
                    throw new NotImplementedException("mjTargetLatitude");
                }
                PropertyInfo mjPositionTargetExists = mjModuleTargetController_t.GetProperty("PositionTargetExists", BindingFlags.Instance | BindingFlags.Public);
                if (mjPositionTargetExists != null)
                {
                    mjGetPositionTargetExists = mjPositionTargetExists.GetGetMethod();
                }
                if (mjGetPositionTargetExists == null)
                {
                    throw new NotImplementedException("mjGetPositionTargetExists");
                }
                PropertyInfo mjNormalTargetExists = mjModuleTargetController_t.GetProperty("NormalTargetExists", BindingFlags.Instance | BindingFlags.Public);
                if (mjPositionTargetExists != null)
                {
                    mjGetNormalTargetExists = mjNormalTargetExists.GetGetMethod();
                }
                if (mjGetNormalTargetExists == null)
                {
                    throw new NotImplementedException("mjGetNormalTargetExists");
                }
                PropertyInfo mjTargetOrbit = mjModuleTargetController_t.GetProperty("TargetOrbit", BindingFlags.Instance | BindingFlags.Public); ;
                if (mjTargetOrbit != null)
                {
                    mjGetTargetOrbit = mjTargetOrbit.GetGetMethod();
                }
                if (mjGetTargetOrbit == null)
                {
                    throw new NotImplementedException("mjGetTargetOrbit");
                }

                // EditableAngle
                Type mjEditableAngle_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.EditableAngle");
                if (mjEditableAngle_t == null)
                {
                    throw new NotImplementedException("mjEditableAngle_t");
                }
                foreach (MethodInfo method in mjEditableAngle_t.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    // The method name reports as "op_Implicit", but there are two
                    if (method.ReturnType == typeof(System.Double))
                    {
                        mjAbsoluteVectorToDouble = method;
                        break;
                    }
                }
                if (mjAbsoluteVectorToDouble == null)
                {
                    throw new NotImplementedException("mjAbsoluteVectorToDouble");
                }

                // ComputerModule
                Type mjComputerModule_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.ComputerModule");
                PropertyInfo mjModuleEnabledProperty = mjComputerModule_t.GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                if (mjModuleEnabledProperty != null)
                {
                    mjModuleEnabled = mjModuleEnabledProperty.GetGetMethod();
                }
                if (mjModuleEnabled == null)
                {
                    throw new NotImplementedException("mjModuleEnabled");
                }
                mjModuleUsers = mjComputerModule_t.GetField("users", BindingFlags.Instance | BindingFlags.Public);
                if (mjModuleUsers == null)
                {
                    throw new NotImplementedException("mjModuleUsers");
                }

                // UserPool
                Type mjUserPool_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.UserPool");
                mjAddUser = mjUserPool_t.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
                if (mjAddUser == null)
                {
                    throw new NotImplementedException("mjAddUser");
                }
                mjRemoveUser = mjUserPool_t.GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public);
                if (mjRemoveUser == null)
                {
                    throw new NotImplementedException("mjRemoveUser");
                }
                mjContainsUser = mjUserPool_t.GetMethod("Contains", BindingFlags.Instance | BindingFlags.Public);
                if (mjContainsUser == null)
                {
                    throw new NotImplementedException("mjContainsUser");
                }

                // MechJebModuleNodeExecutor
                Type mjNodeExecutor_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.MechJebModuleNodeExecutor");
                mjExecuteOneNode = mjNodeExecutor_t.GetMethod("ExecuteOneNode", BindingFlags.Instance | BindingFlags.Public);
                if (mjExecuteOneNode == null)
                {
                    throw new NotImplementedException("mjExecuteOneNode");
                }
                mjAbortNode = mjNodeExecutor_t.GetMethod("Abort", BindingFlags.Instance | BindingFlags.Public);
                if (mjAbortNode == null)
                {
                    throw new NotImplementedException("mjAbortNode");
                }

                // MechJebModuleLandingAutopilot
                Type mjLandingAutopilot_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.MechJebModuleLandingAutopilot");
                mjLandAtPositionTarget = mjLandingAutopilot_t.GetMethod("LandAtPositionTarget", BindingFlags.Instance | BindingFlags.Public);
                if (mjLandAtPositionTarget == null)
                {
                    throw new NotImplementedException("mjLandAtPositionTarget");
                }
                mjLandUntargeted = mjLandingAutopilot_t.GetMethod("LandUntargeted", BindingFlags.Instance | BindingFlags.Public);
                if (mjLandUntargeted == null)
                {
                    throw new NotImplementedException("mjLandUntargeted");
                }
                mjStopLanding = mjLandingAutopilot_t.GetMethod("StopLanding", BindingFlags.Instance | BindingFlags.Public);
                if (mjStopLanding == null)
                {
                    throw new NotImplementedException("mjStopLanding");
                }

                // MechJebModuleSmartASS
                Type mjSmartass_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.MechJebModuleSmartASS");
                mjSmartassTarget = mjSmartass_t.GetField("target", BindingFlags.Instance | BindingFlags.Public);
                if (mjSmartassTarget == null)
                {
                    throw new NotImplementedException("mjSmartassTarget");
                }
                mjSmartassEngage = mjSmartass_t.GetMethod("Engage", BindingFlags.Instance | BindingFlags.Public);
                if (mjSmartassEngage == null)
                {
                    throw new NotImplementedException("mjSmartassEngage");
                }
                mjSmartassForceRol = mjSmartass_t.GetField("forceRol", BindingFlags.Instance | BindingFlags.Public);
                if (mjSmartassForceRol == null)
                {
                    throw new NotImplementedException("mjSmartassForceRol");
                }
                mjSmartassRol = mjSmartass_t.GetField("rol", BindingFlags.Instance | BindingFlags.Public);
                if (mjSmartassRol == null)
                {
                    throw new NotImplementedException("mjSmartassRol");
                }
                FieldInfo TargetTextsInfo = mjSmartass_t.GetField("TargetTexts", BindingFlags.Static | BindingFlags.Public);
                TargetTexts = (string[])TargetTextsInfo.GetValue(null);
                if (TargetTexts == null)
                {
                    throw new NotImplementedException("TargetTexts");
                }
                FieldInfo ModeTextsInfo = mjSmartass_t.GetField("ModeTexts", BindingFlags.Static | BindingFlags.Public);
                ModeTexts = (string[])ModeTextsInfo.GetValue(null);
                if (ModeTexts == null)
                {
                    throw new NotImplementedException("ModeTexts");
                }

                // EditableDouble
                Type mjEditableDouble_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.EditableDouble");
                PropertyInfo mjEditableDoubleVal = mjEditableDouble_t.GetProperty("val", BindingFlags.Instance | BindingFlags.Public);
                if (mjEditableDoubleVal != null)
                {
                    mjGetEditableDouble = mjEditableDoubleVal.GetGetMethod();
                    mjSetEditableDouble = mjEditableDoubleVal.GetSetMethod();
                }
                if (mjGetEditableDouble == null)
                {
                    throw new NotImplementedException("mjGetEditableDouble");
                }
                if (mjSetEditableDouble == null)
                {
                    throw new NotImplementedException("mjSetEditableDouble");
                }

                // OrbitalManeuverCalculator
                Type mjOrbitalManeuverCalculator_t = loadedMechJebAssy.assembly.GetExportedTypes()
                    .SingleOrDefault(t => t.FullName == "MuMech.OrbitalManeuverCalculator");
                mjDeltaVAndTimeForHohmannTransfer = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVAndTimeForHohmannTransfer", BindingFlags.Static | BindingFlags.Public);
                if (mjDeltaVAndTimeForHohmannTransfer == null)
                {
                    throw new NotImplementedException("mjDeltaVAndTimeForHohmannTransfer");
                }
                mjDeltaVAndTimeForInterplanetaryTransferEjection = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVAndTimeForInterplanetaryTransferEjection", BindingFlags.Static | BindingFlags.Public);
                if (mjDeltaVAndTimeForInterplanetaryTransferEjection == null)
                {
                    throw new NotImplementedException("mjDeltaVAndTimeForInterplanetaryTransferEjection");
                }
                mjDeltaVToCircularize = mjOrbitalManeuverCalculator_t.GetMethod("DeltaVToCircularize", BindingFlags.Static | BindingFlags.Public);
                if (mjDeltaVToCircularize == null)
                {
                    throw new NotImplementedException("mjDeltaVToCircularize");
                }

                mjFound = true;
            }
            catch (Exception e)
            {
                mjFound = false;
                JUtil.LogMessage(this, "Exception triggered when configuring: {0}", e);
            }

            if (JUtil.debugLoggingEnabled)
            {
                JUtil.LogMessage(this, "A supported version of MechJeb is {0}", (mjFound) ? "present" : "not available");
            }
        }

        private void InvalidateResults()
        {
            activeJeb = null;

            landingCurrent = false;
            deltaVCurrent = false;

            deltaV = 0.0;
            deltaVStage = 0.0;

            landingLat = 0.0;
            landingLon = 0.0;
            landingAlt = 0.0;
            landingErr = -1.0;
        }

        #region Internal Methods
        /// <summary>
        /// Invokes VesselExtensions.GetMasterMechJeb()
        /// </summary>
        /// <returns>true if MJ is available, false otherwise</returns>
        private bool GetMasterMechJeb()
        {
            // Is MechJeb installed?
            if (mjFound)
            {
                // Have we already updated activeJeb?
                if (activeJeb == null && vessel != null)
                {
                    foreach (Part part in vessel.Parts)
                    {
                        foreach (PartModule module in part.Modules)
                        {
                            if (module.GetType() == mjMechJebCore_t)
                            {
                                activeJeb = mjGetMasterMechJeb.Invoke(null, new object[] { vessel });
                                break;
                            }
                        }
                    }
                }
            }

            return (activeJeb != null);
        }

        /// <summary>
        /// Fetch a computer module from the MJ core
        /// </summary>
        /// <param name="masterMechJeb"></param>
        /// <param name="computerModule"></param>
        /// <returns></returns>
        private object GetComputerModule(object masterMechJeb, string computerModule)
        {
            if (masterMechJeb != null)
            {
                return mjGetComputerModule.Invoke(masterMechJeb, new object[] { computerModule });
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns whether the supplied MechJeb ComputerModule is enabled
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private bool ModuleEnabled(object module)
        {
            if (module != null)
            {
                return (bool)mjModuleEnabled.Invoke(module, null);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Return the latest landing simulation results, or null if there aren't any.
        /// </summary>
        /// <returns></returns>
        private object GetLandingResults(object masterMechJeb)
        {
            object predictor = GetComputerModule(masterMechJeb, "MechJebModuleLandingPredictions");
            if (predictor != null && ModuleEnabled(predictor) == true)
            {
                return mjPredictionsGetResult.Invoke(predictor, null);
            }

            return null;
        }

        private void EnactTargetAction(Target action)
        {
            GetMasterMechJeb();
            object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");

            JUtil.LogMessage(this, "EnactTargetAction {0}", action);
            if (activeSmartass != null)
            {
                mjSmartassTarget.SetValue(activeSmartass, (int)action);

                mjSmartassEngage.Invoke(activeSmartass, new object[] { true });
            }
        }

        private bool ReturnTargetState(Target action)
        {
            GetMasterMechJeb();
            object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");

            if (activeSmartass != null)
            {
                object target = mjSmartassTarget.GetValue(activeSmartass);
                return (int)action == (int)target;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Updater methods
        /// <summary>
        /// Update the landing prediction stats
        /// </summary>
        private void UpdateLandingStats()
        {
            try
            {
                GetMasterMechJeb();
                object result = GetLandingResults(activeJeb);
                if (result != null)
                {
                    object outcome = mjReentryOutcome.GetValue(result);
                    if (outcome != null && outcome.ToString() == "LANDED")
                    {
                        object endPosition = mjReentryEndPosition.GetValue(result);
                        if (endPosition != null)
                        {
                            landingLat = (double)mjAbsoluteVectorLat.GetValue(endPosition);
                            landingLon = (double)mjAbsoluteVectorLon.GetValue(endPosition);
                            // Small fudge factor - we define 0, 0 as "invalid", so always make sure
                            // this value is just off of 0.  This is an error of about 0* 0' 0.4" for
                            // this case only
                            if (landingLat == 0.0)
                            {
                                landingLat = 0.0001;
                            }
                            if (landingLon == 0.0)
                            {
                                landingLon = 0.0001;
                            }
                            landingAlt = FinePrint.Utilities.CelestialUtilities.TerrainAltitude(vessel.mainBody, landingLat, landingLon);

                            object target = mjCoreTarget.GetValue(activeJeb);
                            object targetLatField = mjTargetLatitude.GetValue(target);
                            object targetLonField = mjTargetLongitude.GetValue(target);
                            double targetLat = (double)mjAbsoluteVectorToDouble.Invoke(null, new object[] { targetLatField });
                            double targetLon = (double)mjAbsoluteVectorToDouble.Invoke(null, new object[] { targetLonField });
                            double targetAlt = FinePrint.Utilities.CelestialUtilities.TerrainAltitude(vessel.mainBody, targetLat, targetLon);

                            landingErr = Vector3d.Distance(vessel.mainBody.GetRelSurfacePosition(landingLat, landingLon, landingAlt),
                                                vessel.mainBody.GetRelSurfacePosition(targetLat, targetLon, targetAlt));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                JUtil.LogErrorMessage(this, "Exception trap in GetLandingError(): {0}", e);
            }
        }

        /// <summary>
        /// Updates dV stats (dV and dVStage)
        /// </summary>
        private void UpdateDeltaVStats()
        {
            try
            {
                GetMasterMechJeb();
                if (activeJeb != null)
                {
                    object stagestats = GetComputerModule(activeJeb, "MechJebModuleStageStats");

                    mjRequestUpdate.Invoke(stagestats, new object[] { this });

                    int atmStatsLength = 0, vacStatsLength = 0;

                    object atmStatsO = mjAtmStageStats.GetValue(stagestats);
                    object vacStatsO = mjVacStageStats.GetValue(stagestats);
                    if (atmStatsO != null)
                    {
                        atmStatsLength = (int)mjStageStatsGetLength.Invoke(atmStatsO, null);
                    }
                    if (vacStatsO != null)
                    {
                        vacStatsLength = (int)mjStageStatsGetLength.Invoke(vacStatsO, null);
                    }

                    deltaV = deltaVStage = 0.0;

                    if (atmStatsLength > 0 && atmStatsLength == vacStatsLength)
                    {
                        double atmospheresLocal = vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres;

                        for (int i = 0; i < atmStatsLength; ++i)
                        {
                            object atmStat = mjStageStatsGetIndex.Invoke(atmStatsO, new object[] { i });
                            object vacStat = mjStageStatsGetIndex.Invoke(vacStatsO, new object[] { i });
                            if (atmStat == null || vacStat == null)
                            {
                                throw new NotImplementedException("atmStat or vacState did not evaluate");
                            }

                            float atm = (float)mjStageDv.GetValue(atmStat);
                            float vac = (float)mjStageDv.GetValue(vacStat);
                            double stagedV = UtilMath.LerpUnclamped(vac, atm, atmospheresLocal);

                            deltaV += stagedV;

                            if (i == (atmStatsLength - 1))
                            {
                                deltaVStage = stagedV;
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                JUtil.LogErrorMessage(this, "Exception trap in UpdateDeltaVStats(): {0}", e);
            }
        }
        #endregion

        #region External interface
        /// <summary>
        /// Returns whether we've been able to link with MechJeb.  While this
        /// really should be static, it isn't, since RPMC always assumes it
        /// should supply the 'this' pointer (or whatever C# calls it).
        /// </summary>
        /// <returns>true if MJ is available for query</returns>
        public bool GetMechJebAvailable()
        {

            return GetMasterMechJeb();
        }

        public void SetSmartassMode(Target t)
        {
            EnactTargetAction(t);
        }

        /// <summary>
        /// Return the current Smartass mode
        /// </summary>
        /// <returns></returns>
        public int GetSmartassMode()
        {
            if (GetMasterMechJeb())
            {
                object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");

                if (activeSmartass != null)
                {
                    object target = mjSmartassTarget.GetValue(activeSmartass);
                    return (int)target;
                }
            }

            return (int)Target.OFF;
        }

        /// <summary>
        /// Returns the predicted landing error to a target.
        /// </summary>
        /// <returns>-1 if the prediction is unavailable for whatever reason</returns>
        public double GetLandingError()
        {
            if (GetMasterMechJeb())
            {
                if (moduleInvalidated)
                {
                    InvalidateResults();
                    moduleInvalidated = false;
                }

                if (landingCurrent == false)
                {
                    UpdateLandingStats();
                }

                return landingErr;
            }
            else
            {
                return -1.0;
            }
        }

        /// <summary>
        /// Returns the predicted latitude at landing.
        /// </summary>
        /// <returns>-1 if the prediction is unavailable for whatever reason</returns>
        public double GetLandingLatitude()
        {
            if (GetMasterMechJeb())
            {
                if (moduleInvalidated)
                {
                    InvalidateResults();
                    moduleInvalidated = false;
                }

                if (landingCurrent == false)
                {
                    UpdateLandingStats();
                }

                return landingLat;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns the predicted longitude at landing.
        /// </summary>
        /// <returns>-1 if the prediction is unavailable for whatever reason</returns>
        public double GetLandingLongitude()
        {
            if (GetMasterMechJeb())
            {
                if (moduleInvalidated)
                {
                    InvalidateResults();
                    moduleInvalidated = false;
                }

                if (landingCurrent == false)
                {
                    UpdateLandingStats();
                }

                return landingLon;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns the predicted altitude at landing.
        /// </summary>
        /// <returns>-1 if the prediction is unavailable for whatever reason</returns>
        public double GetLandingAltitude()
        {
            if (GetMasterMechJeb())
            {
                if (moduleInvalidated)
                {
                    InvalidateResults();
                    moduleInvalidated = false;
                }

                if (landingCurrent == false)
                {
                    UpdateLandingStats();
                }

                return landingAlt;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns net dV of the vessel.
        /// </summary>
        /// <returns>Returns NaN if MJ is unavailable.</returns>
        public double GetDeltaV()
        {
            if (GetMasterMechJeb())
            {
                if (moduleInvalidated)
                {
                    InvalidateResults();
                    moduleInvalidated = false;
                }

                if (deltaVCurrent == false)
                {
                    UpdateDeltaVStats();
                }

                return deltaV;
            }
            else
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// Returns dV of the active stage
        /// </summary>
        /// <returns>Returns NaN if MJ is unavailable.</returns>
        public double GetStageDeltaV()
        {
            if (GetMasterMechJeb())
            {
                if (moduleInvalidated)
                {
                    InvalidateResults();
                    moduleInvalidated = false;
                }

                if (deltaVCurrent == false)
                {
                    UpdateDeltaVStats();
                }

                return deltaVStage;
            }
            else
            {
                return double.NaN;
            }
        }

        public double GetForceRollAngle()
        {
            GetMasterMechJeb();
            object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");
            if (activeSmartass != null)
            {
                object forceRol = mjSmartassForceRol.GetValue(activeSmartass);
                object rolValue = mjSmartassRol.GetValue(activeSmartass);
                return (double)mjGetEditableDouble.Invoke(rolValue, null);
            }
            else
            {
                return 0.0;
            }

        }

        public double GetTerminalVelocity()
        {
            if (GetMasterMechJeb())
            {
                object vesselState = mjCoreVesselState.GetValue(activeJeb);
                if (vesselState != null)
                {
                    double value = (double)mjTerminalVelocity.Invoke(vesselState, null);
                    return (double.IsNaN(value)) ? double.PositiveInfinity : value;
                }
            }

            return double.NaN;
        }

        /// <summary>
        /// Returns true when the current MJ target is a ground target.
        /// </summary>
        /// <returns></returns>
        public bool PositionTargetExists()
        {
            if (GetMasterMechJeb())
            {
                object target = mjCoreTarget.GetValue(activeJeb);
                if ((bool)mjGetPositionTargetExists.Invoke(target, null))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when any autopilots are enabled (and thus Smartass is
        /// in "AUTO" mode).
        /// </summary>
        /// <returns></returns>
        public bool AutopilotEnabled()
        {
            // It appears the SmartASS module does not know if MJ is in
            // automatic pilot mode (like Ascent Guidance or Landing
            // Guidance) without querying indirectly like this.
            // MOARdV BUG: This doesn't seem to work if any of the
            // attitude settings are active (like "Prograde").
            //if (activeJeb.attitude.enabled && !activeJeb.attitude.users.Contains(activeSmartass))
            GetMasterMechJeb();
            object attitude = mjCoreAttitude.GetValue(activeJeb);
            if (ModuleEnabled(attitude))
            {
                object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");
                object users = mjModuleUsers.GetValue(attitude);
                return (bool)mjContainsUser.Invoke(users, new object[] { activeSmartass });
            }

            return false;
        }

        private bool ForceRollState(double roll)
        {
            GetMasterMechJeb();
            object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");
            if (activeSmartass != null)
            {
                object forceRol = mjSmartassForceRol.GetValue(activeSmartass);
                object rolValue = mjSmartassRol.GetValue(activeSmartass);
                double rol = (double)mjGetEditableDouble.Invoke(rolValue, null);

                return (bool)forceRol && (Math.Abs(roll - rol) < 0.5);
            }
            else
            {
                return false;
            }
        }

        public bool GetModuleExists(string moduleName)
        {
            GetMasterMechJeb();
            object module = GetComputerModule(activeJeb, moduleName);

            return (module != null);
        }

        public void ForceRoll(bool state, double roll)
        {
            GetMasterMechJeb();
            object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");
            if (activeSmartass != null)
            {
                if (state)
                {
                    object rolValue = mjSmartassRol.GetValue(activeSmartass);
                    mjSetEditableDouble.Invoke(rolValue, new object[] { roll });
                    mjSmartassRol.SetValue(activeSmartass, rolValue);
                }
                mjSmartassForceRol.SetValue(activeSmartass, state);
                mjSmartassEngage.Invoke(activeSmartass, new object[] { true });
            }
        }

        public void CircularizeAt(double UT)
        {
            Vector3d dV;

            dV = (Vector3d)mjDeltaVToCircularize.Invoke(null, new object[] { vessel.orbit, UT });

            if (vessel.patchedConicSolver != null)
            {
                while (vessel.patchedConicSolver.maneuverNodes.Count > 0)
                {
                    vessel.patchedConicSolver.RemoveManeuverNode(vessel.patchedConicSolver.maneuverNodes.Last());
                }
            }

            mjPlaceManeuverNode.Invoke(null, new object[] { vessel, vessel.orbit, dV, UT });
        }
        #endregion

        #region MechJebRPMButtons

        /// <summary>
        /// Enables / disable "Execute One Node"
        /// </summary>
        /// <param name="state"></param>
        public void ButtonNodeExecute(bool state)
        {
            if (GetMasterMechJeb())
            {
                object node = mjCoreNode.GetValue(activeJeb);
                object mp = GetComputerModule(activeJeb, "MechJebModuleManeuverPlanner");
                if (node != null && mp != null)
                {
                    if (state)
                    {
                        if (!ModuleEnabled(node))
                        {
                            mjExecuteOneNode.Invoke(node, new object[] { mp });
                        }
                    }
                    else
                    {
                        mjAbortNode.Invoke(node, null);
                    }
                }
            }
        }

        /// <summary>
        /// Returns whether the maneuver node executor is enabled.
        /// </summary>
        /// <returns></returns>
        public bool ButtonNodeExecuteState()
        {
            if (GetMasterMechJeb())
            {
                object ap = mjCoreNode.GetValue(activeJeb);
                return ModuleEnabled(ap);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Enable the Ascent Autopilot
        /// </summary>
        /// <param name="state"></param>
        public void ButtonAscentGuidance(bool state)
        {
            GetMasterMechJeb();
            object ap = GetComputerModule(activeJeb, "MechJebModuleAscentAutopilot");
            object agPilot = GetComputerModule(activeJeb, "MechJebModuleAscentGuidance");

            if (ap != null && agPilot != null)
            {
                object users = mjModuleUsers.GetValue(ap);
                if (users == null)
                {
                    throw new NotImplementedException("mjModuleUsers(ap) was null");
                }
                if (ModuleEnabled(ap))
                {
                    mjRemoveUser.Invoke(users, new object[] { agPilot });
                }
                else
                {
                    mjAddUser.Invoke(users, new object[] { agPilot });
                }
            }
        }

        /// <summary>
        /// Indicates whether the ascent autopilot is enabled
        /// </summary>
        /// <returns></returns>
        public bool ButtonAscentGuidanceState()
        {
            if (GetMasterMechJeb())
            {
                object ap = GetComputerModule(activeJeb, "MechJebModuleAscentAutopilot");
                return ModuleEnabled(ap);
            }
            else
            {
                return false;
            }
        }

        public void ButtonDockingGuidance(bool state)
        {
            GetMasterMechJeb();
            object autopilot = GetComputerModule(activeJeb, "MechJebModuleDockingAutopilot");
            object autopilotController = GetComputerModule(activeJeb, "MechJebModuleDockingGuidance");

            if (autopilot != null && autopilotController != null)
            {
                object users = mjModuleUsers.GetValue(autopilot);
                if (users == null)
                {
                    throw new NotImplementedException("mjModuleUsers(autopilot) was null");
                }
                if (ModuleEnabled(autopilot))
                {
                    mjRemoveUser.Invoke(users, new object[] { autopilotController });
                }
                else if (FlightGlobals.fetch.VesselTarget is ModuleDockingNode)
                {
                    mjAddUser.Invoke(users, new object[] { autopilotController });
                }
            }
        }

        public bool ButtonDockingGuidanceState()
        {
            GetMasterMechJeb();
            object ap = GetComputerModule(activeJeb, "MechJebModuleDockingAutopilot");
            return ModuleEnabled(ap);
        }

        /// <summary>
        /// Plot a Hohmann transfer to the current target.  This is an instant
        /// fire-and-forget function, not a toggle switch
        /// </summary>
        /// <param name="state">unused</param>
        public void ButtonPlotHohmannTransfer(bool state)
        {
            if (!ButtonPlotHohmannTransferState())
            {
                // Target is not one MechJeb can successfully plot.
                return;
            }

            GetMasterMechJeb();

            object target = mjCoreTarget.GetValue(activeJeb);
            Orbit targetOrbit = (Orbit)mjGetTargetOrbit.Invoke(target, null);
            Orbit o = vessel.orbit;
            Vector3d dV;
            double nodeUT = 0.0;
            if (o.referenceBody == targetOrbit.referenceBody)
            {
                object[] args = new object[] { o, targetOrbit, Planetarium.GetUniversalTime(), nodeUT };
                dV = (Vector3d)mjDeltaVAndTimeForHohmannTransfer.Invoke(null, args);
                nodeUT = (double)args[3];
            }
            else
            {
                object[] args = new object[] { o, Planetarium.GetUniversalTime(), targetOrbit, true, nodeUT };
                dV = (Vector3d)mjDeltaVAndTimeForInterplanetaryTransferEjection.Invoke(null, args);
                nodeUT = (double)args[4];
            }

            if (vessel.patchedConicSolver != null)
            {
                while (vessel.patchedConicSolver.maneuverNodes.Count > 0)
                {
                    vessel.patchedConicSolver.RemoveManeuverNode(vessel.patchedConicSolver.maneuverNodes.Last());
                }
            }

            mjPlaceManeuverNode.Invoke(null, new object[] { vessel, o, dV, nodeUT });
        }

        /// <summary>
        /// Indicate whether a Hohmann Transfer Orbit can be plotted
        /// </summary>
        /// <returns>true if a transfer can be plotted, false if not</returns>
        public bool ButtonPlotHohmannTransferState()
        {
            if (!mjFound)
            {
                return false;
            }

            if (!GetMasterMechJeb())
            {
                return false;
            }

            object target = mjCoreTarget.GetValue(activeJeb);
            if (target == null)
            {
                return false;
            }

            // Most of these conditions are directly from MJ, or derived from
            // it.
            if ((bool)mjGetNormalTargetExists.Invoke(target, null) == false)
            {
                return false;
            }

            Orbit o = vessel.orbit;
            if (o.eccentricity > 0.2)
            {
                // Need fairly circular orbit to plot.
                return false;
            }

            Orbit targetOrbit = (Orbit)mjGetTargetOrbit.Invoke(target, null);
            if (o.referenceBody == targetOrbit.referenceBody)
            {
                // Target is in our SoI

                if (targetOrbit.eccentricity >= 1.0)
                {
                    // can't intercept hyperbolic targets
                    return false;
                }

                if (o.RelativeInclination(targetOrbit) > 30.0 && o.RelativeInclination(targetOrbit) < 150.0)
                {
                    // Target is in a drastically different orbital plane.
                    return false;
                }
            }
            else
            {
                // Target is not in our SoI
                if (o.referenceBody.referenceBody == null)
                {
                    // Can't plot a transfer from an orbit around the sun (really?)
                    return false;
                }
                if (o.referenceBody.referenceBody != targetOrbit.referenceBody)
                {
                    return false;
                }
                if (o.referenceBody.orbit.RelativeInclination(targetOrbit) > 30.0)
                {
                    // Can't handle highly inclined targets
                    return false;
                }
            }

            // Did we get through all the tests?  Then we can plot an orbit!
            return true;
        }

        /// <summary>
        /// Enables / disables landing guidance.  When a target is selected and
        /// this mode is enabled, the ship goes into "Land at Target" mode.  If
        /// a target is not selected, it becomes "Land Somewhere".
        /// </summary>
        /// <param name="state"></param>
        public void ButtonLandingGuidance(bool state)
        {
            if (GetMasterMechJeb())
            {
                object autopilot = GetComputerModule(activeJeb, "MechJebModuleLandingAutopilot");
                if (state != ModuleEnabled(autopilot))
                {
                    if (state)
                    {
                        object landingGuidanceAP = GetComputerModule(activeJeb, "MechJebModuleLandingGuidance");
                        if (landingGuidanceAP != null)
                        {
                            object target = mjCoreTarget.GetValue(activeJeb);
                            if ((bool)mjGetPositionTargetExists.Invoke(target, null))
                            {
                                mjLandAtPositionTarget.Invoke(autopilot, new object[] { landingGuidanceAP });
                            }
                            else
                            {
                                mjLandUntargeted.Invoke(autopilot, new object[] { landingGuidanceAP });
                            }
                        }
                    }
                    else
                    {
                        mjStopLanding.Invoke(autopilot, null);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current state of the landing guidance feature
        /// </summary>
        /// <returns>true if on, false if not</returns>
        public bool ButtonLandingGuidanceState()
        {
            if (GetMasterMechJeb())
            {
                object ap = GetComputerModule(activeJeb, "MechJebModuleLandingAutopilot");
                return ModuleEnabled(ap);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Toggles SmartASS Force Roll mode.
        /// </summary>
        /// <param name="state"></param>
        public void ButtonForceRoll(bool state)
        {
            GetMasterMechJeb();
            object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");
            if (activeSmartass != null)
            {
                mjSmartassForceRol.SetValue(activeSmartass, state);
                mjSmartassEngage.Invoke(activeSmartass, new object[] { true });
            }
        }

        /// <summary>
        /// Indicates whether SmartASS Force Roll is on or off
        /// </summary>
        /// <returns></returns>
        public bool ButtonForceRollState()
        {
            GetMasterMechJeb();
            object activeSmartass = GetComputerModule(activeJeb, "MechJebModuleSmartASS");
            if (activeSmartass != null)
            {
                object forceRol = mjSmartassForceRol.GetValue(activeSmartass);
                return (bool)forceRol;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Force the roll to zero degrees.
        /// </summary>
        /// <param name="state"></param>
        public void ButtonForceRoll0(bool state)
        {
            ForceRoll(state, 0.0);
        }
        /// <summary>
        /// Returns true when Force Roll is on, and the roll is set to 0.
        /// </summary>
        /// <returns></returns>
        public bool ButtonForceRoll0State()
        {
            return ForceRollState(0.0);
        }

        /// <summary>
        /// Force the roll to +90 degrees.
        /// </summary>
        /// <param name="state"></param>
        public void ButtonForceRoll90(bool state)
        {
            ForceRoll(state, 90.0);
        }
        /// <summary>
        /// Returns true when Force Roll is on, and the roll is set to 90.
        /// </summary>
        /// <returns></returns>
        public bool ButtonForceRoll90State()
        {
            return ForceRollState(90.0);
        }

        /// <summary>
        /// Force the roll to 180 degrees.
        /// </summary>
        /// <param name="state"></param>
        public void ButtonForceRoll180(bool state)
        {
            ForceRoll(state, 180.0);
        }

        /// <summary>
        /// Returns true when Force Roll is on, and the roll is set to 180.
        /// </summary>
        /// <returns></returns>
        public bool ButtonForceRoll180State()
        {
            return ForceRollState(180.0);
        }

        /// <summary>
        /// Force the roll to -90 degrees.
        /// </summary>
        /// <param name="state"></param>
        public void ButtonForceRoll270(bool state)
        {
            ForceRoll(state, -90.0);
        }

        /// <summary>
        /// Returns true when Force Roll is on, and the roll is set to -90.
        /// </summary>
        /// <returns></returns>
        public bool ButtonForceRoll270State()
        {
            return ForceRollState(-90.0);
        }

        /// <summary>
        /// The MechJeb landing prediction simulator runs on a separate thread,
        /// and it may be costly for lower-end computers to leave it running
        /// all the time.  This button allows the player to indicate whether
        /// it needs to be running, or not.
        /// </summary>
        /// <param name="state">Enable/disable</param>
        public void ButtonEnableLandingPrediction(bool state)
        {
            GetMasterMechJeb();
            object predictor = GetComputerModule(activeJeb, "MechJebModuleLandingPredictions");
            object landingGuidanceAP = GetComputerModule(activeJeb, "MechJebModuleLandingGuidance");

            if (predictor != null && landingGuidanceAP != null)
            {
                object users = mjModuleUsers.GetValue(predictor);
                if (users == null)
                {
                    throw new NotImplementedException("mjModuleUsers(predictor) was null");
                }
                if (state)
                {
                    mjAddUser.Invoke(users, new object[] { landingGuidanceAP });
                }
                else
                {
                    mjRemoveUser.Invoke(users, new object[] { landingGuidanceAP });
                }
            }
        }

        /// <summary>
        /// Returns whether the landing prediction simulator is currently
        /// running.
        /// </summary>
        /// <returns></returns>
        public bool ButtonEnableLandingPredictionState()
        {
            GetMasterMechJeb();
            object ap = GetComputerModule(activeJeb, "MechJebModuleLandingPredictions");
            return ModuleEnabled(ap);
        }

        /// <summary>
        /// Engages / disengages Rendezvous Autopilot
        /// </summary>
        /// <param name="state"></param>
        public void ButtonRendezvousAutopilot(bool state)
        {
            GetMasterMechJeb();
            object autopilot = GetComputerModule(activeJeb, "MechJebModuleRendezvousAutopilot");
            object autopilotController = GetComputerModule(activeJeb, "MechJebModuleRendezvousAutopilotWindow");

            if (autopilot != null && autopilotController != null)
            {
                object users = mjModuleUsers.GetValue(autopilot);
                if (users == null)
                {
                    throw new NotImplementedException("mjModuleUsers(autopilot) was null");
                }
                if (state)
                {
                    mjAddUser.Invoke(users, new object[] { autopilotController });
                }
                else
                {
                    mjRemoveUser.Invoke(users, new object[] { autopilotController });
                }
            }
        }

        /// <summary>
        /// Indicates whether the Rendezvous Autopilot is engaged.
        /// </summary>
        /// <returns></returns>
        public bool ButtonRendezvousAutopilotState()
        {
            GetMasterMechJeb();
            object ap = GetComputerModule(activeJeb, "MechJebModuleRendezvousAutopilot");
            return ModuleEnabled(ap);
        }

        // All the other buttons are pretty much identical and just use different enum values.

        // Off button
        // Analysis disable once UnusedParameter
        public void ButtonOff(bool state)
        {
            EnactTargetAction(Target.OFF);
        }

        public bool ButtonOffState()
        {
            return ReturnTargetState(Target.OFF);
        }

        // NODE button
        public void ButtonNode(bool state)
        {
            if (vessel.patchedConicSolver != null)
            {
                if (state && vessel.patchedConicSolver.maneuverNodes.Count > 0)
                {
                    EnactTargetAction(Target.NODE);
                }
                else if (!state)
                {
                    EnactTargetAction(Target.OFF);
                }
            }
        }

        public bool ButtonNodeState()
        {
            return ReturnTargetState(Target.NODE);
        }

        // KillRot button
        public void ButtonKillRot(bool state)
        {
            EnactTargetAction((state) ? Target.KILLROT : Target.OFF);
        }

        public bool ButtonKillRotState()
        {
            return ReturnTargetState(Target.KILLROT);
        }

        // Prograde button
        public void ButtonPrograde(bool state)
        {
            EnactTargetAction((state) ? Target.PROGRADE : Target.OFF);
        }
        public bool ButtonProgradeState()
        {
            return ReturnTargetState(Target.PROGRADE);
        }

        // Retrograde button
        public void ButtonRetrograde(bool state)
        {
            EnactTargetAction((state) ? Target.RETROGRADE : Target.OFF);
        }
        public bool ButtonRetrogradeState()
        {
            return ReturnTargetState(Target.RETROGRADE);
        }

        // NML+ button
        public void ButtonNormalPlus(bool state)
        {
            EnactTargetAction((state) ? Target.NORMAL_PLUS : Target.OFF);
        }
        public bool ButtonNormalPlusState()
        {
            return ReturnTargetState(Target.NORMAL_PLUS);
        }

        // NML- button
        public void ButtonNormalMinus(bool state)
        {
            EnactTargetAction((state) ? Target.NORMAL_MINUS : Target.OFF);
        }
        public bool ButtonNormalMinusState()
        {
            return ReturnTargetState(Target.NORMAL_MINUS);
        }

        // RAD+ button
        public void ButtonRadialPlus(bool state)
        {
            EnactTargetAction((state) ? Target.RADIAL_PLUS : Target.OFF);
        }
        public bool ButtonRadialPlusState()
        {
            return ReturnTargetState(Target.RADIAL_PLUS);
        }

        // RAD- button
        public void ButtonRadialMinus(bool state)
        {
            EnactTargetAction((state) ? Target.RADIAL_MINUS : Target.OFF);
        }
        public bool ButtonRadialMinusState()
        {
            return ReturnTargetState(Target.RADIAL_MINUS);
        }

        // Surface prograde button
        public void ButtonSurfacePrograde(bool state)
        {
            EnactTargetAction((state) ? Target.SURFACE_PROGRADE : Target.OFF);
        }
        public bool ButtonSurfaceProgradeState()
        {
            return ReturnTargetState(Target.SURFACE_PROGRADE);
        }

        // Surface Retrograde button
        public void ButtonSurfaceRetrograde(bool state)
        {
            EnactTargetAction((state) ? Target.SURFACE_RETROGRADE : Target.OFF);
        }
        public bool ButtonSurfaceRetrogradeState()
        {
            return ReturnTargetState(Target.SURFACE_RETROGRADE);
        }

        // Horizontal + button
        public void ButtonHorizontalPlus(bool state)
        {
            EnactTargetAction((state) ? Target.HORIZONTAL_PLUS : Target.OFF);
        }
        public bool ButtonHorizontalPlusState()
        {
            return ReturnTargetState(Target.HORIZONTAL_PLUS);
        }

        // Horizontal - button
        public void ButtonHorizontalMinus(bool state)
        {
            EnactTargetAction((state) ? Target.HORIZONTAL_MINUS : Target.OFF);
        }
        public bool ButtonHorizontalMinusState()
        {
            return ReturnTargetState(Target.HORIZONTAL_MINUS);
        }

        // Up button
        public void ButtonVerticalPlus(bool state)
        {
            EnactTargetAction((state) ? Target.VERTICAL_PLUS : Target.OFF);
        }
        public bool ButtonVerticalPlusState()
        {
            return ReturnTargetState(Target.VERTICAL_PLUS);
        }

        // Target group buttons additionally require a target to be set to press.
        // TGT+ button
        public void ButtonTargetPlus(bool state)
        {
            if (!state)
            {
                EnactTargetAction(Target.OFF);
            }
            else if (FlightGlobals.fetch.VesselTarget != null)
            {
                EnactTargetAction(Target.TARGET_PLUS);
            }
        }
        public bool ButtonTargetPlusState()
        {
            return ReturnTargetState(Target.TARGET_PLUS);
        }

        // TGT- button
        public void ButtonTargetMinus(bool state)
        {
            if (!state)
            {
                EnactTargetAction(Target.OFF);
            }
            else if (FlightGlobals.fetch.VesselTarget != null)
            {
                EnactTargetAction(Target.TARGET_MINUS);
            }
        }
        public bool ButtonTargetMinusState()
        {
            return ReturnTargetState(Target.TARGET_MINUS);
        }

        // RVEL+ button
        public void ButtonRvelPlus(bool state)
        {
            if (!state)
            {
                EnactTargetAction(Target.OFF);
            }
            else if (FlightGlobals.fetch.VesselTarget != null)
            {
                EnactTargetAction(Target.RELATIVE_PLUS);
            }
        }
        public bool ButtonRvelPlusState()
        {
            return ReturnTargetState(Target.RELATIVE_PLUS);
        }

        // RVEL- button
        public void ButtonRvelMinus(bool state)
        {
            if (!state)
            {
                EnactTargetAction(Target.OFF);
            }
            else if (FlightGlobals.fetch.VesselTarget != null)
            {
                EnactTargetAction(Target.RELATIVE_MINUS);
            }
        }
        public bool ButtonRvelMinusState()
        {
            return ReturnTargetState(Target.RELATIVE_MINUS);
        }

        // PAR+ button
        public void ButtonParPlus(bool state)
        {
            if (!state)
            {
                EnactTargetAction(Target.OFF);
            }
            else if (FlightGlobals.fetch.VesselTarget != null)
            {
                EnactTargetAction(Target.PARALLEL_PLUS);
            }
        }
        public bool ButtonParPlusState()
        {
            return ReturnTargetState(Target.PARALLEL_PLUS);
        }

        // PAR- button
        public void ButtonParMinus(bool state)
        {
            if (!state)
            {
                EnactTargetAction(Target.OFF);
            }
            else if (FlightGlobals.fetch.VesselTarget != null)
            {
                EnactTargetAction(Target.PARALLEL_MINUS);
            }
        }
        public bool ButtonParMinusState()
        {
            return ReturnTargetState(Target.PARALLEL_MINUS);
        }

        #endregion
    }
}
