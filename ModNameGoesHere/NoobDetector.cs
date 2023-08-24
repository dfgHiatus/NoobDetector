using CloudX.Shared;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModNameGoesHere
{
    public class NoobDetector : NeosMod
    {
        public override string Name => "NoobDetector";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/NoobDetector/";

        private volatile bool _worldDiscoveryChanged;

        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.NoobDetector").PatchAll();
            Engine.Current.WorldAnnouncer.WorldDiscoveryChanged += OnWorldDiscoveryChanged;
        }

        private void OnWorldDiscoveryChanged(bool sessionsAddedOrRemoved)
        {
            // _worldDiscoveryChanged = true;
            Userspace.Current.Slot.RunSynchronously(delegate
            {
                GetNewWorlds();
            });
        }

        private void GetNewWorlds()
        {
            // if (!_worldDiscoveryChanged) return;
            // _worldDiscoveryChanged = false;

            List<SessionInfo> newSessionInfo = new List<SessionInfo>();
            if (!UserspaceRadiantDash.DashBlocked)
            {
                Engine.Current.WorldAnnouncer.GetDiscoveredWorlds(newSessionInfo);
            }

            var newSessionHosts = newSessionInfo
                .Where(i =>
                    i.Name == "MTC Training Center" &&
                    (i.AccessLevel == SessionAccessLevel.Anyone ||
                     i.AccessLevel == SessionAccessLevel.RegisteredUsers) &&
                    i.ActiveUsers > 0 &&
                    i.SessionId != null &&
                    i.HeadlessHost == false &&
                    i.IsOnLAN == false)
                .Select(j => new UserDescriptor(j.HostUserId, j.HostUsername, j.SanitizedHostUsername, j.SessionId, j.Name, j.SessionURLs))
                .Where(k => DateTime.Now - Engine.Current.Cloud.GetUser(k.UserId).Result.Entity.RegistrationDate <= TimeSpan.FromDays(1))
                .First();

            if (newSessionHosts == null) return;

            var slot = Userspace.Current.Slot.AddSlot("NoobDetectorPanel");
            slot.PositionInFrontOfUser();
            var noob = slot.AttachComponent<NoobDetectorJoinPanel>();
            noob.Setup(newSessionHosts);
        }

        public class UserDescriptor
        {
            public string UserId;
            public string Username;
            public string SanitizedUsername;
            public string SessionId;
            public string SessionName;
            public List<string> SessionURLs;

            public UserDescriptor(string userId, string username, string sanitizedUsername, string sessionId, string sessionName, List<string> sessionURLs)
            {
                UserId = userId;
                Username = username;
                SanitizedUsername = sanitizedUsername;
                SessionId = sessionId;
                SessionName = sessionName;
                SessionURLs = sessionURLs;
            }
        }
    }
}