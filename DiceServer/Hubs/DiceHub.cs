using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DiceServer.Hubs
{
    public class DiceHub : Hub
    {
        private static readonly Random random = new Random();
        private readonly ILogger<DiceHub> _logger;
        private readonly GameSessionManager _sessionManager;
        private string currentPlayerId;

        public DiceHub(ILogger<DiceHub> logger, GameSessionManager sessionManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
        }

        public async Task SendMessage(int id, int value)
        {
            _logger.LogInformation($"Dice {id} Rolled {value}");
        }

        private async Task EndTurn(string sessionId)
        {
            string nextPlayerId = _sessionManager.EndTurn(sessionId);
            await NextTurn(sessionId, nextPlayerId);
        }

        private async Task NextTurn(string sessionId, string nextPlayerId)
        {
            var Dices = new List<Dice>();

            Enumerable.Range(1, 6).Select(id => new Dice { Id = id, Value = 7 }).ToList().ForEach(dice => Dices.Add(dice));

            await Clients.Group(sessionId).SendAsync("YourTurn", nextPlayerId, Dices);
        }
        public bool IsFarkle(List<Dice> diceResults)
        {
            var possibleScoring = DiceLogic.Farkle(diceResults);
            return possibleScoring.Count == 0;
        }

        private List<Dice> RollDice(List<Dice> Dices)
        {
            var results = new List<Dice>();
            for (int i = 0; i < 6; i++)
            {
                if (Dices[i].OnHold || Dices[i].Disabled)
                {
                    results.Add(Dices[i]);
                }
                else
                {
                    results.Add(new Dice { Id = i + 1, Value = random.Next(1, 7) });
                }
            }
            return results;
        }
        public async Task RollDice(List<Dice> Dices, string sessionId, int possibleScoreOnHost)
        {
            var currentPlayerId = _sessionManager.GetCurrentPlayerId(sessionId);
            if (currentPlayerId != Context.ConnectionId)
            {
                await Clients.Caller.SendAsync("NotYourTurn");
                return;
            }

            bool hasOnHoldNotDisabled = Dices.Any(dice => (dice.OnHold || dice.Value == 7) && !dice.Disabled);

            if (hasOnHoldNotDisabled)
            {
                await Clients.Caller.SendAsync("SelectDice", false);
            }
            else
            {
                await Clients.Caller.SendAsync("SelectDice", true);
                return;
            }

            var diceResults = RollDice(Dices);

            var scoreData = DiceLogic.CalculateFullScore(diceResults);

            var WronglyHeldDices = scoreData.Dices.Where(dice => dice.OnHold && !dice.Disabled).ToList();

            if (WronglyHeldDices.Count > 0)
            {
                var heldDiceValues = WronglyHeldDices.Select(dice => dice.Value).ToList();

                await Clients.Caller.SendAsync("WronglyHeldDices", heldDiceValues);

                return;
            }

            await Clients.Group(sessionId).SendAsync("DiceRolled", scoreData.Dices);


            var possibleScoring = DiceLogic.Farkle(scoreData.Dices);

            if (scoreData.Dices.All(dice => dice.Disabled))
            {
                scoreData.Dices.Clear();

                Enumerable.Range(1, 6).Select(id => new Dice { Id = id, Value = 7 }).ToList().ForEach(dice => scoreData.Dices.Add(dice));

                await Clients.Group(sessionId).SendAsync("SecondTurn", possibleScoreOnHost + scoreData.totalScore, scoreData.Dices);
            }
            else if (possibleScoring.Count == 0)
            {
                await Clients.Group(sessionId).SendAsync("BadLuck", scoreData.Dices);
                await Task.Delay(5000);
                _sessionManager.IncrementPlayerTurn(sessionId, currentPlayerId);
                if (_sessionManager.HasAllPlayersHadEqualTurns(sessionId))
                {
                    if (_sessionManager.HasPlayerReachedScore(sessionId, 10000))
                    {
                        var winner = _sessionManager.CompleteRoundAndDetermineWinner(sessionId);
                        await Clients.Group(sessionId).SendAsync("GameEnded", winner);
                        _sessionManager.DeleteSession(sessionId);
                        return;
                    }
                }
                await EndTurn(sessionId);
            }
            else
            {
                await Clients.Group(sessionId).SendAsync("UpdatePossibleScoring", possibleScoring, currentPlayerId, scoreData.totalScore, scoreData.Dices);
            }
        }
        public async Task Bank(List<Dice> Dices, string sessionId, int possibleScoreOnHost)
        {
            var currentPlayerId = _sessionManager.GetCurrentPlayerId(sessionId);
            if (currentPlayerId != Context.ConnectionId)
            {
                await Clients.Caller.SendAsync("NotYourTurn");
                return;
            }
            var scoreData = DiceLogic.CalculateFullScore(Dices);

            var WronglyHeldDices = scoreData.Dices.Where(dice => dice.OnHold && !dice.Disabled).ToList();

            if (WronglyHeldDices.Count > 0)
            {
                var heldDiceValues = WronglyHeldDices.Select(dice => dice.Value).ToList();

                await Clients.Caller.SendAsync("WronglyHeldDices", heldDiceValues);

                return;
            }

            int newScore = _sessionManager.UpdatePlayerScore(sessionId, currentPlayerId, scoreData.totalScore + possibleScoreOnHost);

            await Clients.Group(sessionId).SendAsync("UpdatePlayerScore", currentPlayerId, newScore, scoreData.Dices);

            _sessionManager.IncrementPlayerTurn(sessionId, currentPlayerId);

            if (_sessionManager.HasAllPlayersHadEqualTurns(sessionId))
            {
                if (_sessionManager.HasPlayerReachedScore(sessionId, 10000))
                {
                    var winner = _sessionManager.CompleteRoundAndDetermineWinner(sessionId);
                    await Clients.Group(sessionId).SendAsync("GameEnded", winner);
                    _sessionManager.DeleteSession(sessionId);
                    return;
                }
            }

            await EndTurn(sessionId);
        }

        public async Task UpdateDiceState(Dice dice, List<Dice> dices)
        {
            var possibleScore = DiceLogic.CalculateFullScore(dices);
            await Clients.All.SendAsync("DiceStateChanged", dice, possibleScore.totalScore);

        }


        public async Task CreateSession()
        {
            string sessionId = _sessionManager.CreateSession();
            await Clients.Caller.SendAsync("SessionCreated", sessionId);
            _logger.LogInformation($"Session {sessionId} Created");
        }

        public async Task JoinSession(string sessionId, string playerName)
        {
            if (_sessionManager.SessionExists(sessionId))
            {
                var session = _sessionManager.GetSession(sessionId);
                if (session.State != GameState.Lobby)
                {
                    await Clients.Caller.SendAsync("SessionInProgress");
                    return;
                }

                var player = new Player { Id = Context.ConnectionId };
                if (!string.IsNullOrEmpty(playerName))
                {
                    player.Name = playerName;
                }
                else
                {
                    player.Name = Context.ConnectionId;
                }

                if (_sessionManager.JoinSession(sessionId, player))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

                    await Clients.Group(sessionId).SendAsync("PlayersJoined", _sessionManager.GetPlayersInSession(sessionId));

                    await Clients.Caller.SendAsync("RecievePlayerId", Context.ConnectionId);

                    _logger.LogInformation($"Player {playerName} joined {sessionId}");
                }
                else
                {
                    await Clients.Caller.SendAsync("SessionNotFound");
                }
            }
            else
            {
                await Clients.Caller.SendAsync("SessionNotFound");
            }
        }


        public async Task StartSession(string sessionId)
        {

            if (_sessionManager.StartSession(sessionId))
            {
                currentPlayerId = _sessionManager.GetCurrentPlayerId(sessionId);
                await Clients.Group(sessionId).SendAsync("SessionStarted");

                var firstPlayerId = _sessionManager.GetPlayersInSession(sessionId).FirstOrDefault();

                if (firstPlayerId != null)
                {
                    List<Dice> Dices = new List<Dice>();

                    Enumerable.Range(1, 6).Select(id => new Dice { Id = id, Value = 7 }).ToList().ForEach(dice => Dices.Add(dice));

                    await Clients.Group(sessionId).SendAsync("YourTurn", firstPlayerId.Id, Dices);
                }

                _logger.LogInformation($"Session {sessionId} started");
            }
            else
            {
                await Clients.Group(sessionId).SendAsync("SessionNotStarted");
                _logger.LogInformation($"Session {sessionId} did not start");
            }
        }


        public async Task QuitSession(string sessionId, string playerId)
        {
            var success = _sessionManager.QuitSession(sessionId, playerId);
            if (success)
            {
                await Clients.Group(sessionId).SendAsync("PlayerQuit", (_sessionManager.GetPlayersInSession(sessionId)));
                _logger.LogInformation($"Player {playerId} quit {sessionId}");
            }
            else
            {
                await Clients.Caller.SendAsync("QuitSessionFailed", "Failed to quit the session.");
            }
        }

    }
}
