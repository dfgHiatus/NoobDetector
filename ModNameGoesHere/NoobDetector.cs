using BaseX;
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

        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.NoobDetector").PatchAll();
            Engine.Current.OnReady += () =>
            {
                // Don't get existing worlds on startup, only get new ones
                Userspace.Current.RunInSeconds(10, () =>
                {
                    Engine.Current.WorldAnnouncer.WorldDiscoveryChanged += OnWorldDiscoveryChanged;
                });
            };  
        }

        private void OnWorldDiscoveryChanged(bool sessionsAddedOrRemoved)
        {
            Debug("OnWorldDiscoveryChanged");
            Userspace.Current.RunSynchronously(delegate
            {
                GetNewWorlds();
            });
        }

        private static readonly HashSet<string> noobUsersVisited = new HashSet<string>();

        private void GetNewWorlds()
        {
            Debug("Looking for new worlds...");
            List<SessionInfo> newSessionInfo = new List<SessionInfo>();
            Engine.Current.WorldAnnouncer.GetDiscoveredWorlds(newSessionInfo);

            Debug("Looking for worlds with new active MTC users...");
            if (newSessionInfo == null) return;
            if (newSessionInfo.Count == 0) return;

            IEnumerable<SessionInfo> newSessionHosts = Enumerable.Empty<SessionInfo>();
            try
            {
                newSessionHosts = newSessionInfo
                .Where(i =>
                    i.Name == "Metaverse Training Center" &&
                    (i.AccessLevel == SessionAccessLevel.Anyone ||
                     i.AccessLevel == SessionAccessLevel.RegisteredUsers) &&
                    i.ActiveUsers > 0 &&
                    i.SessionId != null &&
                    i.HeadlessHost == false &&
                    i.IsOnLAN == false);
                // .Where(k => DateTime.Now - Engine.Current.Cloud.GetUser(k.UserId).Result.Entity.RegistrationDate <= TimeSpan.FromDays(1))
            }
            catch (InvalidOperationException e)
            {
                Error(e.Message);
                return;
            }

            Debug("Checking to see if user count is 0.");
            if (newSessionHosts.Count() == 0) return;

            Debug("Checking to see if we are already in the world.");
            // If we are already in the world, don't spawn a new oeb
            var newSessionHost = newSessionHosts.First();
            if (newSessionHost.HostUserId == Engine.Current.WorldManager.FocusedWorld.HostUser.UserID) return;

            Debug("Checking to see if we have already visited this world.");
            // If we have already visited this world, don't spawn another orb.
            // This technically causes a memory leak, but it's not a big deal.
            if (noobUsersVisited.Contains(newSessionHost.HostUserId)) return; 

            Debug("Spawning new world orb.");
            // Setup the world orb and add the user to our "seen" set
            noobUsersVisited.Add(newSessionHost.HostUserId);
            var noobWorldOrb = Engine.Current.WorldManager.FocusedWorld.AddSlot("NoobDetectorPanel").AttachComponent<WorldOrb>();
            noobWorldOrb.Slot.PositionInFrontOfUser();
            NotificationMessage.SpawnTextMessage(
                $"A new user {newSessionHost.HostUsername} has been born into the universe!\n Go say hi to them!",
                color.Magenta); 
            noobWorldOrb.ActiveSessionURLs = newSessionHost.GetSessionURLs();
            noobWorldOrb.ActiveUsers.Value = newSessionHost.ActiveUsers;
            noobWorldOrb.WorldName = newSessionHost.Name;
            noobWorldOrb.CreatorName = newSessionHost.SanitizedHostUsername;
            noobWorldOrb.CreatorTextColor = new color(1f, 0.2f, 1f, 0.4f);
            if (!string.IsNullOrEmpty(newSessionHost.Thumbnail))
            {
                noobWorldOrb.SetThumbnail(new Uri(newSessionHost.Thumbnail));
            }
        }
    }
}