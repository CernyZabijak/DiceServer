using System.Collections.Generic;
using System.Linq;

namespace DiceServer
{
    public class DiceLogic
    {
        public static (int totalScore, List<Dice> Dices) CalculateFullScore(List<Dice> Dices)
        {
            int totalScore = 0;

            while (true)
            {
                var scoresData = CalculateHighestScore(Dices.ToArray());
                int score = scoresData.score;

                if (score == 0)
                    break;

                totalScore += score;
                Dices = scoresData.updatedDice.ToList();
            }

            return (totalScore, Dices);
        }
        public static (int score, Dice[] updatedDice) CalculateHighestScore(Dice[] dice)
        {
            var scoresAndDices = new List<(int score, Dice[] updatedDice)>
            {
                ScoreOnes(dice),
                ScoreFives(dice),
                ScoreThreeOfAKind(dice),
                ScoreFourOfAKind(dice),
                ScoreFiveOfAKind(dice),
                ScoreSixOfAKind(dice),
                ScoreThreePairs(dice),
                ScoreRun(dice)
            };

            scoresAndDices.Sort((x, y) => y.score.CompareTo(x.score));

            return scoresAndDices.FirstOrDefault();
        }
        public static List<string> Farkle(List<Dice> dice)
        {
            var possibleScoring = new List<string>();


            Dice[] diceToScore = dice.Select(d => new Dice { Id = d.Id, Value = d.Value, OnHold = true, Disabled = d.Disabled }).ToArray();

            //var counts = dice.GroupBy(x => x.Value).ToDictionary(g => g.Key, g => g.Count());

            //var diceArray = counts.SelectMany(kv => Enumerable.Repeat(kv.Key, kv.Value)).ToArray();
            //var diceObjects = diceArray.Select(value => new Dice{ Value = value, OnHold = false, Disabled = dice.FirstOrDefault(d => d.Value == value)?.Disabled ?? false }).ToArray();
            //var diceToScore = new Dice[diceObjects.Length];
            //for (int i = 0; i < diceObjects.Length; i++)
            //{
            //    diceToScore[i] = new Dice { Value = diceObjects[i].Value, OnHold = true, Disabled = diceObjects[i].Disabled };
            //}

            var onesScore = ScoreOnes(diceToScore);
            if (onesScore.score > 0)
                possibleScoring.Add("Ones");

            var fivesScore = ScoreFives(diceToScore);
            if (fivesScore.score > 0)
                possibleScoring.Add("Fives");

            var threeOfAKindScore = ScoreThreeOfAKind(diceToScore);
            if (threeOfAKindScore.score > 0)
                possibleScoring.Add("ThreeOfAKind");

            var fourOfAKindScore = ScoreFourOfAKind(diceToScore);
            if (fourOfAKindScore.score > 0)
                possibleScoring.Add("FourOfAKind");

            var fiveOfAKindScore = ScoreFiveOfAKind(diceToScore);
            if (fiveOfAKindScore.score > 0)
                possibleScoring.Add("FiveOfAKind");

            var sixOfAKindScore = ScoreSixOfAKind(diceToScore);
            if (sixOfAKindScore.score > 0)
                possibleScoring.Add("SixOfAKind");

            var threePairsScore = ScoreThreePairs(diceToScore);
            if (threePairsScore.score > 0)
                possibleScoring.Add("ThreePairs");

            var runScore = ScoreRun(diceToScore);
            if (runScore.score > 0)
                possibleScoring.Add("Run");

            return possibleScoring;
        }


        private static (int score, Dice[] updatedDice) ScoreOnes(Dice[] dice)
        {
            var counts = new int[7];

            foreach (var die in dice)
            {
                if (die.OnHold && !die.Disabled)
                {
                    counts[die.Value]++;
                }
            }

            int score = counts[1] * 100;

            var updatedDice = dice.Select(d =>
            {
                var updatedDie = new Dice { Id = d.Id, Value = d.Value, OnHold = d.OnHold, Disabled = d.Disabled };
                updatedDie.Disabled = d.Disabled || (d.OnHold && !d.Disabled && d.Value == 1);
                return updatedDie;
            }).ToArray();

            return (score, updatedDice);
        }

        private static (int score, Dice[] updatedDice) ScoreFives(Dice[] dice)
        {
            var counts = new int[7];

            foreach (var die in dice)
            {
                if (die.OnHold && !die.Disabled)
                {
                    counts[die.Value]++;
                }
            }

            int score = counts[5] * 50;

            var updatedDice = dice.Select(d =>
            {
                var updatedDie = new Dice { Id = d.Id, Value = d.Value, OnHold = d.OnHold, Disabled = d.Disabled };
                updatedDie.Disabled = d.Disabled || (d.OnHold && !d.Disabled && d.Value == 5);
                return updatedDie;
            }).ToArray();

            return (score, updatedDice);
        }
        private static (int score, Dice[] updatedDice) ScoreThreeOfAKind(Dice[] dice)
        {
            var counts = new int[7];

            foreach (var die in dice)
            {
                if (die.OnHold && !die.Disabled)
                {
                    counts[die.Value]++;
                }
            }

            for (int i = 1; i <= 6; i++)
            {
                if (counts[i] >= 3)
                {
                    int score = 0;
                    switch (i)
                    {
                        case 1: score = 1000; break;
                        case 2: score = 200; break;
                        case 3: score = 300; break;
                        case 4: score = 400; break;
                        case 5: score = 500; break;
                        case 6: score = 600; break;
                    }

                    var updatedDice = dice.Select(d =>
                    {
                        var updatedDie = new Dice { Id = d.Id, Value = d.Value, OnHold = d.OnHold, Disabled = d.Disabled };
                        updatedDie.Disabled = d.Disabled || (d.OnHold && !d.Disabled && d.Value == i);
                        return updatedDie;
                    }).ToArray();

                    return (score, updatedDice);
                }
            }

            return (0, dice);
        }

        private static (int score, Dice[] updatedDice) ScoreFourOfAKind(Dice[] dice)
        {
            var counts = new int[7];

            foreach (var die in dice)
            {
                if (die.OnHold && !die.Disabled)
                {
                    counts[die.Value]++;
                }
            }

            for (int i = 1; i <= 6; i++)
            {
                if (counts[i] >= 4)
                {
                    int score = 0;
                    switch (i)
                    {
                        case 1: score = 2000; break;
                        case 2: score = 400; break;
                        case 3: score = 600; break;
                        case 4: score = 800; break;
                        case 5: score = 1000; break;
                        case 6: score = 1200; break;
                    }

                    var updatedDice = dice.Select(d =>
                    {
                        var updatedDie = new Dice { Id = d.Id, Value = d.Value, OnHold = d.OnHold, Disabled = d.Disabled };
                        updatedDie.Disabled = d.Disabled || (d.OnHold && !d.Disabled && d.Value == i);
                        return updatedDie;
                    }).ToArray();

                    return (score, updatedDice);
                }
            }

            return (0, dice);
        }
        private static (int score, Dice[] updatedDice) ScoreFiveOfAKind(Dice[] dice)
        {
            var counts = new int[7];

            foreach (var die in dice)
            {
                if (die.OnHold && !die.Disabled)
                {
                    counts[die.Value]++;
                }
            }

            for (int i = 1; i <= 6; i++)
            {
                if (counts[i] == 5)
                {
                    int score = 0;
                    switch (i)
                    {
                        case 1: score = 4000; break;
                        case 2: score = 800; break;
                        case 3: score = 1200; break;
                        case 4: score = 1600; break;
                        case 5: score = 2000; break;
                        case 6: score = 2400; break;
                    }

                    var updatedDice = dice.Select(d =>
                    {
                        var updatedDie = new Dice { Id = d.Id, Value = d.Value, OnHold = d.OnHold, Disabled = d.Disabled };
                        updatedDie.Disabled = d.Disabled || (d.OnHold && !d.Disabled && d.Value == i);
                        return updatedDie;
                    }).ToArray();

                    return (score, updatedDice);
                }
            }

            return (0, dice);
        }
        private static (int score, Dice[] updatedDice) ScoreSixOfAKind(Dice[] dice)
        {
            var counts = new int[7];

            foreach (var die in dice)
            {
                if (die.OnHold && !die.Disabled)
                {
                    counts[die.Value]++;
                }
            }

            for (int i = 1; i <= 6; i++)
            {
                if (counts[i] == 6)
                {
                    int score = 0;
                    switch (i)
                    {
                        case 1: score = 8000; break;
                        case 2: score = 1600; break;
                        case 3: score = 2400; break;
                        case 4: score = 3200; break;
                        case 5: score = 4000; break;
                        case 6: score = 4800; break;
                    }

                    var updatedDice = dice.Select(d =>
                    {
                        var updatedDie = new Dice { Id = d.Id, Value = d.Value, OnHold = d.OnHold, Disabled = d.Disabled };
                        updatedDie.Disabled = d.Disabled || (d.OnHold && !d.Disabled && d.Value == i);
                        return updatedDie;
                    }).ToArray();

                    return (score, updatedDice);
                }
            }

            return (0, dice);
        }

        private static (int score, Dice[] updatedDice) ScoreThreePairs(Dice[] dice)
        {
            var counts = new int[7];

            foreach (var die in dice)
            {
                if (die.OnHold && !die.Disabled)
                {
                    counts[die.Value]++;
                }
            }

            int pairCount = counts.Count(c => c >= 2);

            if (pairCount == 3)
            {
                int score = 1000;

                var updatedDice = dice.Select(d =>
                {
                    var updatedDie = new Dice { Id = d.Id, Value = d.Value, OnHold = d.OnHold, Disabled = d.Disabled };
                    updatedDie.Disabled = d.Disabled || (d.OnHold && !d.Disabled && counts[d.Value] >= 2);
                    return updatedDie;
                }).ToArray();

                return (score, updatedDice);
            }

            return (0, dice);
        }

        private static (int score, Dice[] updatedDice) ScoreRun(Dice[] dice)
        {
            var distinctDice = dice.Where(d => d.OnHold && !d.Disabled)
                                   .Select(d => d.Value)
                                   .Distinct()
                                   .OrderBy(x => x)
                                   .ToList();

            if (distinctDice.Count == 6 && distinctDice.Last() - distinctDice.First() == 5)
            {
                var updatedDice = dice.Select(d =>
                {
                    var updatedDie = new Dice { Id = d.Id, Value = d.Value, OnHold = d.OnHold, Disabled = d.Disabled };
                    updatedDie.Disabled = d.Disabled || (d.OnHold && !d.Disabled && distinctDice.Contains(d.Value));
                    return updatedDie;
                }).ToArray();

                return (1500, updatedDice);
            }

            return (0, dice);
        }

    }
}
