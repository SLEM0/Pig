using Microsoft.AspNetCore.SignalR.Client;

namespace Client.Services
{
    public class GameService
    {
        public event Action<string>? OnGameStarted;
        public event Action<string, int, int, Dictionary<string, int>>? OnDiceRolled;
        public event Action<string, Dictionary<string, int>>? OnRolledOne;
        public event Action<string, Dictionary<string, int>>? OnNextTurn;
        public event Action<string>? OnGameOver;
        public event Action? OnRollForTurn;
        public event Action<List<string>>? OnPlayerListUpdated;

        private HubConnection? _hubConnection;
        public string? CurrentPlayer { get; private set; }

        public async Task ConnectAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5162/gamehub")
                .Build();

            _hubConnection.On<string>("GameStarted", (startingPlayer) =>
            {
                OnGameStarted?.Invoke(startingPlayer);
            });

            _hubConnection.On<string, int, int, Dictionary<string, int>>("DiceRolled", (player, roll, currentTurnScore, totalScores) =>
            {
                OnDiceRolled?.Invoke(player, roll, currentTurnScore, totalScores);
            });

            _hubConnection.On<string, Dictionary<string, int>>("RolledOne", (nextPlayer, totalScores) =>
            {
                OnRolledOne?.Invoke(nextPlayer, totalScores);
            });

            _hubConnection.On<string, Dictionary<string, int>>("NextTurn", (nextPlayer, totalScores) =>
            {
                OnNextTurn?.Invoke(nextPlayer, totalScores);
            });

            _hubConnection.On<string>("GameOver", (winner) =>
            {
                OnGameOver?.Invoke(winner);
            });

            _hubConnection.On("RollForTurn", () =>
            {
                OnRollForTurn?.Invoke();
            });

            _hubConnection.On<List<string>>("UpdatePlayerList", (updatedPlayers) =>
            {
                OnPlayerListUpdated?.Invoke(updatedPlayers);
            });

            await _hubConnection.StartAsync();
        }

        public async Task JoinGameAsync(string gameId, string userName)
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("JoinGame", gameId, userName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error joining game: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Hub connection is not established.");
            }
        }

        public async Task RollForTurnAsync(string gameId)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.InvokeAsync("RollForTurn", gameId);
            }
        }

        public async Task RollDiceAsync(string gameId)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.InvokeAsync("RollDice", gameId);
            }
        }

        public async Task EndTurnAsync(string gameId)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.InvokeAsync("EndTurn", gameId);
            }
        }
    }
}
