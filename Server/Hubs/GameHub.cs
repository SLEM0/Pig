using Microsoft.AspNetCore.SignalR;
using Server.Models;

namespace Server.Hubs
{
    public class GameHub : Hub
    {
        private static readonly GameManager gameManager = new();

        public async Task JoinGame(string gameId, string userName)
        {
            Context.Items["UserName"] = userName;
            var game = gameManager.GetGame(gameId) ?? gameManager.CreateGame(gameId);

            if (!game.AddPlayer(userName))
            {
                await Clients.Caller.SendAsync("Error", "Комната уже заполнена двумя игроками.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

            // Отправка обновленного списка игроков всем в группе
            await Clients.Group(gameId).SendAsync("UpdatePlayerList", game.Players);

            if (game.Players.Count == 2)
            {
                // Случайный выбор первого игрока
                var random = new Random();
                var startingPlayer = game.Players[random.Next(game.Players.Count)];

                // Установка первого игрока и начало игры
                game.StartGame(startingPlayer);
                // Уведомление о начале игры
                await Clients.Group(gameId).SendAsync("GameStarted", game.CurrentPlayer);
            }
        }

        public async Task RollDice(string gameId)
        {
            var game = gameManager.GetGame(gameId);
            if (game == null || !game.GameStarted) return;

            var currentUserName = Context.Items["UserName"] as string;
            if (currentUserName != game.CurrentPlayer)
            {
                await Clients.Caller.SendAsync("Error", "Не ваш ход!");
                return;
            }

            int diceRoll = game.RollDice();
            if (diceRoll == 1)
            {
                game.ResetCurrentTurnScore();
                game.NextTurn();
                await Clients.Group(gameId).SendAsync("RolledOne", game.CurrentPlayer, game.Scores);
            }
            else
            {
                game.CurrentTurnScores[game.CurrentPlayer] += diceRoll;
                Console.WriteLine(diceRoll);
                await Clients.Group(gameId).SendAsync("DiceRolled", game.CurrentPlayer, diceRoll, game.CurrentTurnScores[game.CurrentPlayer], game.Scores);
            }
        }

        public async Task EndTurn(string gameId)
        {
            var game = gameManager.GetGame(gameId);
            if (game == null || !game.GameStarted) return;

            var currentUserName = Context.Items["UserName"] as string;
            if (currentUserName != game.CurrentPlayer)
            {
                await Clients.Caller.SendAsync("Error", "Не ваш ход!");
                return;
            }

            // Перенос текущей суммы в общую
            game.Scores[game.CurrentPlayer] += game.CurrentTurnScores[game.CurrentPlayer];
            game.ResetCurrentTurnScore();

            // Проверка на победу
            if (game.Scores[game.CurrentPlayer] >= 100)
            {
                await Clients.Group(gameId).SendAsync("GameOver", game.CurrentPlayer);
                gameManager.RemoveGame(gameId);
                return;
            }

            // Переключить на следующего игрока и отправить обновленные данные
            game.NextTurn();
            await Clients.Group(gameId).SendAsync("NextTurn", game.CurrentPlayer, game.Scores);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.Items["UserName"] is not string userName) return;

            foreach (var gameId in gameManager.GetActiveGameIds())
            {
                var game = gameManager.GetGame(gameId);
                if (game != null && game.RemovePlayer(userName))
                {
                    // Уведомить всех игроков о завершении игры
                    await Clients.Group(gameId).SendAsync("GameAborted");
                    // Удалить игру
                    gameManager.RemoveGame(gameId);
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

    }
}