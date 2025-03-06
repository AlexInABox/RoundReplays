using System.IO;
using System.Linq;

namespace RoundReplays
{
    using System;
    using System.Collections.Generic;
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.Events;
    using UnityEngine.Networking;

    using MEC;



    public class RoundReplays : Plugin<Config>
    {
        public override string Prefix => "RoundReplays";
        public override string Name => "RoundReplays";
        public override string Author => "AlexInABox";
        public override Version Version => new Version(1, 0, 0);

        private static RoundReplays Singleton;
        public static RoundReplays Instance => Singleton;
        
        public override PluginPriority Priority { get; } = PluginPriority.Last;

        
        private readonly string _pathToLog = "/home/container/.config/EXILED/Logs/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".txt";
        private CoroutineHandle _mainLoop;
        public override void OnEnabled()
        {
            Singleton = this;
            Log.Info("RoundReplays has been enabled!");
            
            _mainLoop = Timing.RunCoroutine(MainLoop());

            Exiled.Events.Handlers.Server.EndingRound += EndingRound;
            
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Log.Info("RoundReplays has been disabled!");
            
            Timing.KillCoroutines(_mainLoop);
            
            Exiled.Events.Handlers.Server.EndingRound -= EndingRound;

            File.Delete(_pathToLog);

            base.OnDisabled();
        }

        private void EndingRound(Exiled.Events.EventArgs.Server.EndingRoundEventArgs ev)
        {
            Timing.KillCoroutines(_mainLoop);
            Timing.RunCoroutine(EndingRoundCoroutine());
        }
        
        private IEnumerator<float> EndingRoundCoroutine()
        {
            Timing.KillCoroutines(_mainLoop);

            byte[] fileData = File.ReadAllBytes(_pathToLog);
            UnityWebRequest request = new UnityWebRequest("http://172.20.0.1:85/roundReplays", "POST");
            request.uploadHandler = new UploadHandlerRaw(fileData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "text/plain");

            yield return Timing.WaitUntilDone(request.SendWebRequest());

            if (request.result == UnityWebRequest.Result.Success)
            {
                Log.Info("RoundReplay successfully uploaded!");
                File.Delete(_pathToLog);
            }
            else
            {
                Log.Error("Failed to upload RoundReplay log: " + request.error);
            }
        }


        private IEnumerator<float> MainLoop()
        {
            for (;;)
            {
                IEnumerable<Player> listOfPlayers = Player.List.Where(p => !p.DoNotTrack);
                
                File.AppendAllText(_pathToLog, $"## START SNAPSHOT ##{Environment.NewLine}");
                foreach (Player player in listOfPlayers)
                {
                    string logEntry = $"{player.UserId}: {player.Position}{Environment.NewLine}";
                    File.AppendAllText(_pathToLog, logEntry);
                }
                File.AppendAllText(_pathToLog, $"## END SNAPSHOT ##{Environment.NewLine}");

                yield return Timing.WaitForSeconds(5f);
            }
        }
    }
}