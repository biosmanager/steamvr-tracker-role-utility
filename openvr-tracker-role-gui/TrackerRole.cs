using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace openvr_tracker_role_gui
{
    internal enum ETrackerRole
    {
        None,
        AnyHand,
        LeftHand,
        RightHand,
        LeftFoot,
        RightFoot,
        LeftShoulder,
        RightShoulder,
        LeftElbow,
        RightElbow,
        LeftKnee,
        RightKnee,
        Waist,
        Chest,
        Camera,
        Keyboard
    }

    internal class TrackerRole
    {
        public bool Connected { get; set; }
        public string SerialNumber { get; set; }
        public ETrackerRole Role { get; set; }

        public static readonly Dictionary<ETrackerRole, string> TrackerRoleEnumMappings = new Dictionary<ETrackerRole, string>
        {
            { ETrackerRole.None, "TrackerRole_None" }, 
            { ETrackerRole.AnyHand, "TrackerRole_Handed,TrackedControllerRole_Invalid" }, 
            { ETrackerRole.LeftHand, "TrackerRole_Handed,TrackedControllerRole_LeftHand" }, 
            { ETrackerRole.RightHand, "TrackerRole_Handed,TrackedControllerRole_RightHand" }, 
            { ETrackerRole.LeftFoot, "TrackerRole_LeftFoot" }, 
            { ETrackerRole.RightFoot, "TrackerRole_RightFoot" }, 
            { ETrackerRole.LeftShoulder, "TrackerRole_LeftShoulder" }, 
            { ETrackerRole.RightShoulder, "TrackerRole_RightShoulder" }, 
            { ETrackerRole.LeftElbow, "TrackerRole_LeftElbow" }, 
            { ETrackerRole.RightElbow, "TrackerRole_RightElbow" }, 
            { ETrackerRole.LeftKnee, "TrackerRole_LeftKnee" }, 
            { ETrackerRole.RightKnee, "TrackerRole_RightKnee" }, 
            { ETrackerRole.Waist, "TrackerRole_Waist" },
            { ETrackerRole.Chest, "TrackerRole_Chest" },
            { ETrackerRole.Camera, "TrackerRole_Camera" },
            { ETrackerRole.Keyboard, "TrackerRole_Keyboard" }
        };
    }
}
