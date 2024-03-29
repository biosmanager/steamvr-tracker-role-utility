﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Valve.VR;

namespace steamvr_tracker_role_utility
{
    internal class TrackedDevice
    {
        public uint Index { get; set; }
        public string SerialNumber { get; set; }
        public ETrackedDeviceClass DeviceClass { get; set; }
    }

    internal class OpenVrManager
    {
        public ObservableCollection<TrackerRole> TrackerRoles { get; private set; } = new ObservableCollection<TrackerRole>();
        public ObservableCollection<TrackedDevice> ConnectedDevices { get; private set; } = new ObservableCollection<TrackedDevice>();
        public CVRSystem? VrSystem { get; private set; }

        private Task eventTask;

        private event EventHandler deviceConnectedChanged;
        private event EventHandler trackersSectionChanged;
        private Dictionary<string, string> steamVrTrackerBindings;
        private string? steamVrConfigPath = null;


        public OpenVrManager()
        {
            if (!OpenVR.IsRuntimeInstalled())
            {
                Trace.WriteLine("SteamVR runtime not installed!");
                return;
            }

            // Ensure SteamVR is running
            var dummyError = EVRInitError.None;
            OpenVR.Init(ref dummyError, EVRApplicationType.VRApplication_Scene);
            SpinWait.SpinUntil(() => OpenVR.IsHmdPresent(), TimeSpan.FromSeconds(10));
            OpenVR.Shutdown();

            EVRInitError initError = EVRInitError.None;
            var vrSystem = OpenVR.Init(ref initError, EVRApplicationType.VRApplication_Utility);
            if (initError == EVRInitError.None)
            {
                VrSystem = vrSystem;
            }
            else
            {
                Trace.WriteLine($"Could not initialize OpenVR! {initError}");
                return;
            }

            var openVrPathsConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "openvr", "openvrpaths.vrpath");
            if (File.Exists(openVrPathsConfigPath))
            {
                var json = File.ReadAllText(openVrPathsConfigPath);
                var openVrPathsConfig = JObject.Parse(json);

                if (openVrPathsConfig.ContainsKey("config"))
                {
                    var paths = openVrPathsConfig["config"].ToObject<List<string>>();
                    if (paths.Count > 0)
                    {
                        steamVrConfigPath = paths[0];
                        Trace.WriteLine($"SteamVR config path: {paths[0]}");
                    }
                }
            }
            else
            {
                Trace.WriteLine($"Could not find {openVrPathsConfigPath}!");
            }

            Trace.WriteLine("Initialized OpenVR.");

            UpdateSteamVrTrackerBindings();
            UpdateConnectedDevices();
            UpdateTrackerRoles();

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var context = SynchronizationContext.Current;


            deviceConnectedChanged += (s, e) =>
            {
                context.Post(_ =>
                {
                    UpdateConnectedDevices();
                    UpdateTrackerRoles();
                }, null);
            };

            trackersSectionChanged += (s, e) =>
            {
                context.Post(_ =>
                {
                    UpdateSteamVrTrackerBindings();
                    UpdateTrackerRoles();
                }, null);
            };

            eventTask = Task.Run(async () =>
            {
                var vrEvent = new VREvent_t();

                while (!token.IsCancellationRequested)
                {
                    while (VrSystem.PollNextEvent(ref vrEvent, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t))))
                    {
                        switch ((EVREventType)vrEvent.eventType)
                        {
                            case EVREventType.VREvent_TrackedDeviceActivated:
                                Trace.WriteLine($"Activated device {vrEvent.trackedDeviceIndex}");
                                deviceConnectedChanged.Invoke(this, EventArgs.Empty);
                                break;
                            case EVREventType.VREvent_TrackedDeviceDeactivated:
                                Trace.WriteLine($"Deactivated device {vrEvent.trackedDeviceIndex}");
                                deviceConnectedChanged.Invoke(this, EventArgs.Empty);
                                break;
                            case EVREventType.VREvent_TrackersSectionSettingChanged:
                                Trace.WriteLine("SteamVR tracker bindings changed");
                                // Allow some time until SteamVR configuration file has been updated on disk
                                await Task.Delay(1000);
                                trackersSectionChanged.Invoke(this, EventArgs.Empty);
                                break;
                            default:
                                break;
                        }
                    }
                }
            });
        }

        public string GetStringProperty(ETrackedDeviceProperty prop, uint deviceId)
        {
            if (VrSystem is null) return "";

            var error = ETrackedPropertyError.TrackedProp_Success;
            var capacity = VrSystem.GetStringTrackedDeviceProperty(deviceId, prop, null, 0, ref error);
            if (capacity > 1)
            {
                var result = new System.Text.StringBuilder((int)capacity);
                VrSystem.GetStringTrackedDeviceProperty(deviceId, prop, result, capacity, ref error);
                return result.ToString();
            }
            return (error != ETrackedPropertyError.TrackedProp_Success) ? error.ToString() : "<unknown>";
        }

        public void UpdateConnectedDevices()
        {
            if (VrSystem is null) return;

            ConnectedDevices.Clear();
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (!VrSystem.IsTrackedDeviceConnected(i)) continue;

                ConnectedDevices.Add(new TrackedDevice
                {
                    Index = i,
                    SerialNumber = GetStringProperty(ETrackedDeviceProperty.Prop_SerialNumber_String, i),
                    DeviceClass = VrSystem.GetTrackedDeviceClass(i)
                });
            }
        }

        public void UpdateTrackerRoles()
        {
            if (VrSystem is null) return;

            TrackerRoles.Clear();
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                var deviceClass = VrSystem.GetTrackedDeviceClass(i);

                if (deviceClass != ETrackedDeviceClass.GenericTracker) continue;

                var serialNumber = GetStringProperty(ETrackedDeviceProperty.Prop_SerialNumber_String, i);

                var role = ETrackerRole.None;
                string binding = "";
                steamVrTrackerBindings.TryGetValue(serialNumber, out binding);

                TrackerRoles.Add(new TrackerRole
                {
                    Connected = VrSystem.IsTrackedDeviceConnected(i),
                    SerialNumber = serialNumber,
                    Role = TrackerRole.TrackerRoleEnumMappings.FirstOrDefault(x => x.Value == binding).Key
                });
            }

            foreach (var steamVrTrackerRole in steamVrTrackerBindings)
            {
                var alreadyInList = TrackerRoles.Any(x => x.SerialNumber == steamVrTrackerRole.Key);

                if (!alreadyInList)
                {
                    TrackerRoles.Add(new TrackerRole
                    {
                        Connected = false,
                        SerialNumber = steamVrTrackerRole.Key,
                        Role = TrackerRole.TrackerRoleEnumMappings.FirstOrDefault(x => x.Value == steamVrTrackerRole.Value).Key
                    });
                }
            }
        }

        public void UpdateSteamVrTrackerBindings()
        {
            if (VrSystem is null) return;

            steamVrTrackerBindings = GetSteamVrTrackerBindings();
        }

        public void WriteSteamVrTrackerBindings()
        {
            foreach (var trackerRole in TrackerRoles)
            {
                var roleString = "TrackerRole_None";
                TrackerRole.TrackerRoleEnumMappings.TryGetValue(trackerRole.Role, out roleString);

                var settingsError = EVRSettingsError.None;
                OpenVR.Settings.SetString(OpenVR.k_pch_Trackers_Section, $"/devices/htc/vive_tracker{trackerRole.SerialNumber}", roleString, ref settingsError);
                if (settingsError != EVRSettingsError.None)
                {
                    Trace.WriteLine($"Error when writing SteamVR tracker roles: {settingsError}");
                }
            }
        }

        private Dictionary<string, string> GetSteamVrTrackerBindings()
        {
            Dictionary<string, string> trackerBindings = new Dictionary<string, string>();

            string steamVrSettingsPath;
            if (steamVrConfigPath is null)
            {
                steamVrSettingsPath = Path.Combine(OpenVR.RuntimePath(), "../../../config/steamvr.vrsettings");
            }
            else
            {
                steamVrSettingsPath = Path.Combine(steamVrConfigPath, "steamvr.vrsettings");
            }

            if (!File.Exists(steamVrSettingsPath))
            {
                Trace.WriteLine("Could not find SteamVR configuration file!");
                return trackerBindings;
            }

            var json = File.ReadAllText(steamVrSettingsPath);
            var steamVrSettings = JObject.Parse(json);

            if (steamVrSettings.ContainsKey("trackers"))
            {
                var trackers = steamVrSettings["trackers"].ToObject<Dictionary<string, string>>();
                foreach (var pair in trackers)
                {
                    trackerBindings.Add(pair.Key.Replace("/devices/htc/vive_tracker", ""), pair.Value);
                }
            }

            return trackerBindings;
        }

        ~OpenVrManager()
        {
            OpenVR.Shutdown();
        }
    }
}
