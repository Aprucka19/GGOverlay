using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGOverlay.Game
{
    public interface IGameInterface
    {
        // Events for logging, updating UI, and handling disconnection
        event Action<string> OnLog;
        event Action UIUpdate;
        event Action OnDisconnect;

        // Local player information
        PlayerInfo _localPlayer { get; set; }

        List<PlayerInfo> _players { get; set; }

        GameRules _gameRules { get; set; }

        // UserData property
        UserData UserData { get; set; }

        // Method to start the game (Host or Join)
        Task Start(int port, string ipAddress = null);

        // Method to edit player details
        void EditPlayer(string name, double drinkModifier);

        Task SetGameRules(string filepath);

        void TriggerGroupRule(Rule rule);

        void TriggerIndividualRule(Rule rule, PlayerInfo player);

        void RequestUIUpdate();

        // Method to stop the server or disconnect
        void Stop();
    }
}
