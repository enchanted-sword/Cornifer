using DiscordRPC.Logging;
using DiscordRPC;
using System;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Cornifer
{
    class RPC
    {
		private static readonly RichPresence defaultPresence = new() {
			Details = "Mapping Rain World",
			State = "Browsing Regions"
		};

		public static DiscordRpcClient Client;

		//Called when your application first starts.
		//For example, just before your main loop, on OnEnable for unity.
		public static void Initialize() {
			/*
			Create a Discord client
			NOTE:   If you are using Unity3D, you must use the full constructor and define
					 the pipe connection.
			*/
			Client = new("1365462705099509913");

			//Set the logger
			Client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

			//Subscribe to events
			Client.OnReady += (sender, e) =>
			{
				Console.WriteLine("Received Ready from user {0}", e.User.Username);
			};

			Client.OnPresenceUpdate += (sender, e) =>
			{
				Console.WriteLine("Received Update! {0}", e.Presence);
			};

			//Connect to the RPC
			Client.Initialize();

			//Set the rich presence
			//Call this as many times as you want and anywhere in your code.
			Client.SetPresence(defaultPresence);
		}

		public static void UpdateDescription(string details) {
			Client.UpdateDetails(details);
		}
		public static void UpdateState(string state) {
			Client.UpdateState(state);
		}

		public static void Deinitialize() {
			Client.Dispose();
		}
	}
}
