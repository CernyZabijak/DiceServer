using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceServer
{
    public class GameSessionManager
    {
        private List<GameSession> sessions = new List<GameSession>();
        private Dictionary<string, int> turnIndices = new Dictionary<string, int>();

        public string CreateSession()
        {
            string sessionId = GenerateSessionId();
            var session = new GameSession
            {
                SessionId = sessionId,
                Players = new List<Player>(),
                State = GameState.Lobby
            };
            sessions.Add(session);
            return sessionId;
        }

        public bool JoinSession(string sessionId, Player player)
        {
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                session.Players.Add(player);
                return true;
            }
            return false;
        }

        public bool StartSession(string sessionId)
        {
            turnIndices[sessionId] = 0;
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null && session.Players.Count >= 1)
            {
                session.State = GameState.InProgress;
                return true;
            }
            return false;
        }

        public bool QuitSession(string sessionId, string playerId)
        {
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                var player = session.Players.FirstOrDefault(p => p.Id == playerId);
                if (player != null)
                {
                    session.Players.Remove(player);
                    return true;
                }
            }
            return false;
        }
        public bool IsFarkle(List<Dice> diceResults)
        {
            var possibleScoring = DiceLogic.Farkle(diceResults);
            return possibleScoring.Count == 0;
        }
        public string EndTurn(string sessionId)
        {
            if (!turnIndices.ContainsKey(sessionId))
            {
                throw new ArgumentException("Session ID does not exist.");
            }

            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session == null)
            {
                throw new ArgumentException("Session ID does not exist.");
            }

            int currentIndex = turnIndices[sessionId];
            currentIndex = (currentIndex + 1) % session.Players.Count;
            turnIndices[sessionId] = currentIndex;

            return session.Players[currentIndex].Id;
        }



        public List<Player> GetPlayersInSession(string sessionId)
        {
            GameSession session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            return session?.Players;
        }

        public string GetCurrentPlayerId(string sessionId)
        {
            if (!turnIndices.ContainsKey(sessionId))
            {
                throw new ArgumentException("Session ID does not exist.");
            }

            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session == null)
            {
                throw new ArgumentException("Session ID does not exist.");
            }

            int currentIndex = turnIndices[sessionId];
            return session.Players[currentIndex].Id;
        }

        public int UpdatePlayerScore(string sessionId, string playerId, int score)
        {
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                var player = session.Players.FirstOrDefault(p => p.Id == playerId);
                if (player != null)
                {
                    player.Score += score;
                    return player.Score;
                }
            }
            return 0;
        }
        public bool SessionExists(string sessionId)
        {
            return sessions.Any(s => s.SessionId == sessionId);
        }
        public GameSession GetSession(string sessionId)
        {
            return sessions.FirstOrDefault(s => s.SessionId == sessionId);
        }

        private string GenerateSessionId()
        {
            const string chars = "0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public bool HasAllPlayersHadEqualTurns(string sessionId)
        {
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                int turns = session.Players[0].Turns;
                return session.Players.All(p => p.Turns == turns);
            }
            return false;
        }

        public void IncrementPlayerTurn(string sessionId, string playerId)
        {
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                var player = session.Players.FirstOrDefault(p => p.Id == playerId);
                if (player != null)
                {
                    player.Turns++;
                }
            }
        }
        public bool HasPlayerReachedScore(string sessionId, int targetScore)
        {
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                return session.Players.Any(p => p.Score >= targetScore);
            }
            return false;
        }
        public List<Player> CompleteRoundAndDetermineWinner(string sessionId)
        {
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                return session.Players.OrderBy(p => p.Score).ToList();
            }
            return new List<Player>();
        }
        public void DeleteSession(string sessionId)
        {
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                sessions.Remove(session);
            }
        }
    }
}
