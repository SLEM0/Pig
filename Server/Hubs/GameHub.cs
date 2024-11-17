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

            // Отправка уведомления о подключении нового игрока
            //await Clients.Group(gameId).SendAsync("PlayerJoined", userName);

            // Отправка обновленного списка игроков всем в группе
            await Clients.Group(gameId).SendAsync("UpdatePlayerList", game.Players);

            Console.WriteLine($"Player {userName} joined game {gameId}. Total players: {game.Players.Count}");

            if (game.Players.Count == 2)
            {
                await Clients.Group(gameId).SendAsync("RollForTurn");
            }
        }

        public async Task RollForTurn(string gameId)
        {
            var game = gameManager.GetGame(gameId);
            if (game == null || game.Players.Count < 2) return;

            var currentUserName = Context.Items["UserName"] as string;

            // Каждый игрок бросает кость
            int roll = game.RollDice();
            game.PlayerRolls[currentUserName] = roll;

            // Если оба игрока бросили кости, определяем первого игрока
            if (game.PlayerRolls.Count == 2)
            {
                var player1 = game.Players[0];
                var player2 = game.Players[1];
                int roll1 = game.PlayerRolls[player1];
                int roll2 = game.PlayerRolls[player2];

                if (roll1 > roll2)
                {
                    game.StartGame(player1);
                }
                else if (roll2 > roll1)
                {
                    game.StartGame(player2);
                }
                else
                {
                    // Повторить бросок в случае ничьей
                    game.PlayerRolls.Clear();
                    Console.WriteLine("ничья");
                    await Clients.Group(gameId).SendAsync("RollForTurn");
                    return;
                }
                Console.WriteLine("hub");
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
    }
}