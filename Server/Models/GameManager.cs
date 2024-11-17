using System.Collections.Concurrent;

namespace Server.Models
{
    public class GameManager
    {
        private ConcurrentDictionary<string, Game> games;

        public GameManager()
        {
            games = new ConcurrentDictionary<string, Game>();
        }

        public Game CreateGame(string gameId)
        {
            var game = new Game(gameId);
            games.TryAdd(gameId, game);
            return game;
        }

        public Game GetGame(string gameId)
        {
            games.TryGetValue(gameId, out var game);
            return game;
        }

        public void RemoveGame(string gameId)
        {
            games.TryRemove(gameId, out _);
        }
    }
}
