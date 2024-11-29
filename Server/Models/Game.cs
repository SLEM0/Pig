namespace Server.Models
{
    public class Game
    {
        public string GameId { get; }
        public List<string> Players { get; private set; }
        public Dictionary<string, int> Scores { get; private set; }
        public Dictionary<string, int> CurrentTurnScores { get; private set; }
        public string CurrentPlayer { get; private set; }
        private readonly Random random;
        public Dictionary<string, int> PlayerRolls { get; private set; }

        private const int MaxPlayers = 2; // Максимальное количество игроков
        public bool GameStarted { get; private set; } = false;

        public Game(string gameId)
        {
            GameId = gameId;
            Players = [];
            Scores = [];
            CurrentTurnScores = [];
            PlayerRolls = [];
            random = new Random();
        }

        public bool AddPlayer(string player)
        {
            if (Players.Count >= MaxPlayers) return false; // Ограничение на 2 игрока

            if (!Players.Contains(player))
            {
                Players.Add(player);
                Scores[player] = 0;
                CurrentTurnScores[player] = 0;
            }

            if (Players.Count == 1)
            {
                CurrentPlayer = player;
            }
            return true;
        }

        public int RollDice()
        {
            return random.Next(1, 7);
        }

        public bool NextTurn()
        {
            if (Players.Count < 2) return false;

            var currentIndex = Players.IndexOf(CurrentPlayer);
            CurrentPlayer = Players[(currentIndex + 1) % Players.Count];
            return true;
        }

        public void ResetCurrentTurnScore()
        {
            CurrentTurnScores[CurrentPlayer] = 0;
        }

        public void StartGame(string startingPlayer)
        {
            GameStarted = true;
            CurrentPlayer = startingPlayer;
        }
        public bool RemovePlayer(string player)
        {
            if (Players.Remove(player))
            {
                Scores.Remove(player);
                CurrentTurnScores.Remove(player);
                PlayerRolls.Remove(player);
                return true;
            }
            return false;
        }
    }
}