using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGOverlay.Game
{
    public interface IGameInterface
    {
        // Existing events
        event Action<string> OnLog;
        event Action UIUpdate;
        event Action OnDisconnect;

        // Add the new events
        event Action<Rule, PlayerInfo> OnPunishmentTriggered;

        // Existing properties and methods
        PlayerInfo _localPlayer { get; set; }
        List<PlayerInfo> _players { get; set; }
        GameRules _gameRules { get; set; }
        UserData UserData { get; set; }

        // New property
        double _elapsedMinutes { get; set; }

        Task Start(int port, string ipAddress = null);
        void EditPlayer(string name, double drinkModifier, int drinkCount = 0);
        Task SetGameRules(string filepath);

        void TriggerRule(Rule rule, PlayerInfo player);
        void RequestUIUpdate();
        void Stop();
        void FinishDrink();
    }
}
