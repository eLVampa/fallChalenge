﻿using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{

    public const bool debug = true;
    static void Main(string[] args)
    {
        string[] inputs;
        var turnNumber = -1;
        List<((int points, int length) score, int[] order)> learnMap = null;
        var iamReady = false;
        // game loop
        while (true)
        {
            turnNumber++;
            int actionCount = int.Parse(Console.ReadLine()); // the number of spells and recipes in play

            var orders = new List<Order>();
            var spells = ImmutableStack<Spell>.Instance;
            var learns = new List<Learn>();
            var usedSpells = new HashSet<int>();
            for (int i = 0; i < actionCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int actionId = int.Parse(inputs[0]); // the unique ID of this spell or recipe
                string actionType = inputs[1]; // in the first league: BREW; later: CAST, OPPONENT_CAST, LEARN, BREW
                int delta0 = int.Parse(inputs[2]); // tier-0 ingredient change
                int delta1 = int.Parse(inputs[3]); // tier-1 ingredient change
                int delta2 = int.Parse(inputs[4]); // tier-2 ingredient change
                int delta3 = int.Parse(inputs[5]); // tier-3 ingredient change
                byte price = byte.Parse(inputs[6]); // the price in rupees if this is a potion
                int tomeIndex = int.Parse(inputs[7]); // in the first two leagues: always 0; later: the index in the tome if this is a tome spell, equal to the read-ahead tax
                int taxCount = int.Parse(inputs[8]); // in the first two leagues: always 0; later: the amount of taxed tier-0 ingredients you gain from learning this spell
                bool castable = inputs[9] != "0"; // in the first league: always 0; later: 1 if this is a castable player spell
                bool repeatable = inputs[10] != "0"; // for the first two leagues: always 0; later: 1 if this is a repeatable player spell

                if (actionType == "BREW")
                {
                    orders.Add(new Order(actionId, (delta0, delta1, delta2, delta3), price));
                }

                if (actionType == "CAST")
                {
                    spells = spells.Push(new Spell(actionId, (delta0, delta1, delta2, delta3), repeatable, 1));
                    if (!castable)
                    {
                        usedSpells.Add(actionId);
                    }
                }

                if (actionType == "LEARN")
                {
                    learns.Add(new Learn(actionId, taxCount, (delta0, delta1, delta2, delta3), tomeIndex));
                }
            }

            //Console.Error.WriteLine($"Orders cnt: {orders.Count}");

            var needRest = usedSpells.Any();

            StateRes res = null;
            for (int i = 0; i < 2; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int inv0 = int.Parse(inputs[0]); // tier-0 ingredients in inventory
                int inv1 = int.Parse(inputs[1]);
                int inv2 = int.Parse(inputs[2]);
                int inv3 = int.Parse(inputs[3]);
                byte score = byte.Parse(inputs[4]); // amount of rupees

                if (i == 0)
                {
                    res = new StateRes((inv0, inv1, inv2, inv3), score);
                }
            }

            var currentState = new GameState
            (
                res,
                spells,
                orders,
                learns,
                new HashSet<int>(),
                usedSpells,
                new HashSet<int>(),
                Path.Instance,
                needRest
            );

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
            if (turnNumber == 0)
            {
                var spellOptimizer = new SpellOptimizer();

                var tomeSpells = learns.Select(
                    x => new Spell(x.Id, x.Spell, !x.Spell.IsPositiveOrZero(), 1)
                ).ToList();
                learnMap = spellOptimizer.GetSpellMapForLearn(orders, tomeSpells, spells);
            }

            if (!iamReady)
            {
                var (learnAction, finish) =
                    TryGetLearnAction(learnMap, learns, spells, currentState.StateRes.Inventary[0]);
                iamReady = iamReady || finish;
                if (learnAction != null)
                {
                    var q = new StringBuilder();
                    q.Append(learnAction.Print());
                    if (finish)
                    {
                        q.Append(" I AM READY!");
                    }

                    Console.WriteLine(q.ToString());
                    continue;
                }
            }

            var action = new Resolver(debug).Resolve(currentState);
            Console.WriteLine(action);
            // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
        }
    }

    private static (IAction action, bool finish) TryGetLearnAction(
        List<((int points, int length) score, int[] order)> learnMap,
        List<Learn> tomeSpells,
        ImmutableStack<Spell> knownSpells,
        int zero)
    {
        var knownSpellsIds = knownSpells.Select(x => x.Id).ToHashSet();
        foreach (var currentSet in learnMap)
        {
            var iamReady = true;

            var goodSet = currentSet.order.Length == 0
                          || currentSet.order.All(
                              x => knownSpellsIds.Contains(x)
                                   || (tomeSpells.FirstOrDefault(k => k.Id == x) != null)
                          );
            if (!goodSet)
            {
                continue;
            }

            foreach (var bestSpellId in currentSet.order)
            {
                if (knownSpellsIds.Contains(bestSpellId))
                {
                    continue;
                }

                for (var k = 0; k < tomeSpells.Count; k++)
                {
                    if (tomeSpells[k].Id == bestSpellId)
                    {
                        if (k <= zero)
                        {
                            return (tomeSpells[k], bestSpellId == currentSet.order[currentSet.order.Length - 1]);
                        }

                        return (knownSpells.First(s => s.Tiers[0] == 2), false);
                    }

                    iamReady = false;
                }
            }

            if (iamReady)
            {
                return (null, false);
            }
        }

        return (new Wait(), false);
    }


}

public class Resolver
{
    public Resolver(bool debug, int timeout = 35)
    {
        this.debug = debug;
        this.timeout = timeout;
    }

    private readonly bool debug;
    private readonly int timeout;
    private readonly byte[,,,] was = new byte[11, 11, 11, 11];

    public string Resolve(GameState state)
    {
        var processed = 0;
        var maxD = 0;
        GameState best = null;
        var bestCandidates = new List<GameState>();
        var timeIsOver = false;

        var queue = new Queue<GameState>();
        queue.Enqueue(state);

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeout && queue.Count != 0)
        {
            processed++;
            var cs = queue.Dequeue();
           
            var actions =
                state.Orders.Cast<IAction>()
                .Concat(state.Tome)
                .Concat(state.Spells)
                .Append(new Rest())
                .ToList();

            foreach (var action in actions)
            {
                if (sw.ElapsedMilliseconds > timeout)
                {
                    timeIsOver = true;
                    break;
                }

                var nextStates = action is Spell spell
                    ? spell.GetNexStatesForSpell(cs)
                    : new[] { action.TryGetNext(cs) };

                foreach (var nextState in nextStates)
                {
                    if (sw.ElapsedMilliseconds > timeout)
                    {
                        timeIsOver = true;
                        break;
                    }

                    if (nextState != null)
                    {
                        var prevInv = cs.StateRes.Inventary;
                        var inv = nextState.StateRes.Inventary;
                        if (inv[0] != prevInv[0]
                            || inv[1] != prevInv[1]
                            || inv[2] != prevInv[2]
                            || inv[3] != prevInv[3])
                        {
                            if (was[inv[0], inv[1], inv[2], inv[3]] > nextState.StateRes.Rupees + 1)
                            {
                                continue;
                            }
                        }

                        was[inv[0], inv[1], inv[2], inv[3]] = (byte)(nextState.StateRes.Rupees + 1);

                        //   if (!(action is Order))
                        //   {
                        queue.Enqueue(nextState);
                        maxD = Math.Max(maxD, nextState.Actions.GetLength());

                        //   }
                        //   else
                        //   {
                        //       bestCandidates.Add(nextState);
                        //   }

                        if (best == null)
                        {
                            best = nextState;
                        }

                        if (best.StateRes.GetScore() < nextState.StateRes.GetScore())
                        {
                            best = nextState;
                        }
                    }
                }
            }
        }

        if (sw.ElapsedMilliseconds > timeout)
        {
            timeIsOver = true;
        }

        return GetBestAction(best, bestCandidates, processed, maxD, timeIsOver, queue.Count);
    }

    private string GetBestAction(GameState best, List<GameState> bestCandidates, int processed, int maxD, bool timeIsOver, int queueCnt)
    {
        var theBest = best;

        foreach (var candidate in bestCandidates)
        {
            if (theBest.GetProfit() < candidate.GetProfit())
            {
                theBest = candidate;
            }

            if (theBest.GetProfit() == candidate.GetProfit() && theBest.Actions.GetLength() > candidate.Actions.GetLength())
            {
                theBest = candidate;
            }
        }

        var bestAction = theBest?.Actions.TryGetFirstAction();
        if (bestAction == null)
        {
            return "REST I`m getting stupid!";
        }

        var sb = new StringBuilder();
        sb.Append(bestAction.Print());

        if (debug)
        {
            sb.Append(queueCnt != 0 ? $" NOT BAD {queueCnt}" : $" I'M THE BEST {queueCnt}");
            sb.Append($" D {maxD}");
            //sb.Append($" KS {best.Spells.GetTop().Index + 1}");
            sb.Append($" V {processed}");
            //sb.Append("\r\nMy Plan: \r\n");
            //sb.Append(theBest.Actions);
        }

        return sb.ToString();
    }
}

public class SpellOptimizer
{
    public List<((int points, int length) score, int[] order)> GetSpellMapForLearn(List<Order> orders, List<Spell> tomSpell, ImmutableStack<Spell> baseSpell)
    {
        var pathFinder = new OrderPathFinder();
        var res = new List<((int points, int length) score, int[] order)>();
        for (var i0 = 0; i0 < 2; i0++)
            for (var i1 = 0; i1 < 2; i1++)
                for (var i2 = 0; i2 < 2; i2++)
                    for (var i3 = 0; i3 < 2; i3++)
                        for (var i4 = 0; i4 < 2; i4++)
                            for (var i5 = 0; i5 < 2; i5++)
                            {
                                var a = new[] { i0, i1, i2, 0, 0, 0 };
                                var spellForLearn = new List<Spell>();
                                for (var k = 0; k < a.Length; k++)
                                {
                                    if (a[k] != 0)
                                    {
                                        spellForLearn.Add(tomSpell[k]);
                                    }
                                }

                                var spellForSearch = baseSpell;
                                foreach (var s in spellForLearn)
                                {
                                    spellForSearch = spellForSearch.Push(s);
                                }

                                var paths = pathFinder.FindOrderPaths(orders, spellForSearch);

                                var (score, order) = GetScore(orders, paths, baseSpell.ToList(), spellForLearn);
                                res.Add((score, order));
                            }

        return res.OrderByDescending(x => x.score.points)
            .ThenBy(x => x.score.length)
            .ThenByDescending(x => x.order.Length)
            .ToList();
    }


    private static ((int points, int length) score, int[] order) GetScore(
        List<Order> orders,
        List<Path>[] paths,
        List<Spell> baseSpell,
        List<Spell> spellForLearn)
    {
        var baseSpellIds = baseSpell.Select(x => x.Id).ToHashSet();

        var score = 0;
        var totalLength = 0;
        var summs = spellForLearn.Select(x => (Sum: 0, Spell: x)).ToArray();
        for (var i = 0; i < orders.Count; i++)
        {
            var (length, spellsCnt) = GetBestLengthPathBySpells(i, paths, spellForLearn, baseSpellIds);
            for (var j = 0; j < spellsCnt.Length; j++)
            {
                summs[j].Sum += spellsCnt[j];
            }

            totalLength += length;
            if (length != 0)
            {
                score += orders[i].Price;
            }
        }

        var resIdxs = summs.OrderByDescending(x => x.Sum)
            .Select(x => x.Spell.Id)
            .ToArray();

        totalLength += GetActions(resIdxs);

        return ((score, totalLength), resIdxs);
    }

    private static (int length, int[] spellCnt) GetBestLengthPathBySpells(int orderIndex, List<Path>[] paths, List<Spell> spellsForLearn, HashSet<int> baseSpellIds)
    {
        var knownSpells = spellsForLearn.Select(x => x.Id).ToHashSet();

        var orderPaths = paths[orderIndex];
        var best = 0;
        var res = new int[spellsForLearn.Count];

        if (orderPaths != null)
        {
            foreach (var path in orderPaths)
            {
                var actions = path.GetActions();
                var canBrew = actions.All(a =>
                    !(a is Spell) || knownSpells.Contains(((Spell)a).Id) || baseSpellIds.Contains(((Spell)a).Id));
                if (canBrew && (best == 0 || best > path.GetLength()))
                {
                    for (var i = 0; i < spellsForLearn.Count; i++)
                    {
                        var spell = spellsForLearn[i];
                        var contains = actions.Any(x => x is Spell sp && sp.Id == spell.Id);
                        if (contains)
                        {
                            res[i]++;
                        }
                    }
                    best = path.GetLength();
                }
            }
        }

        return (best, res);
    }

    private static int GetActions(int[] tomeIndexes)
    {
        var actionsCnt = 0;
        var c = 3;
        foreach (var tomIndex in tomeIndexes)
        {
            var k = 0;
            switch (tomIndex)
            {
                case 5:
                    (k, c) = GetActions(5, c);
                    break;
                case 4:
                    (k, c) = GetActions(4, c);
                    break;
                case 3:
                    (k, c) = GetActions(3, c);
                    break;
                case 2:
                    (k, c) = GetActions(2, c);
                    break;
                case 1:
                    (k, c) = GetActions(1, c);
                    break;
                case 0:
                    (k, c) = GetActions(1, c);
                    break;
            }

            actionsCnt += k;
        }

        return actionsCnt;
    }

    private static (int a, int newC) GetActions(int index, int c)
    {
        if (index > c)
        {
            var t = index - c;
            var t2 = t / 2;
            var ost = (t2 * 2) != t ? 1 : 0;
            return (t2 + ost + 1, ost);
        }

        return (1, c - index);
    }
}

public class OrderPathFinder
{
    public OrderPathFinder(int timeout = 900, int depth = 8)
    {
        this.depth = depth;
        this.timeout = timeout;
    }

    private readonly int depth;
    private readonly int timeout;

    public List<Path>[] FindOrderPaths(List<Order> orders, ImmutableStack<Spell> spells)
    {
        var was = SpaceFill(spells);

        var min = new byte[orders.Count];


        for (var o = 0; o < orders.Count; o++)
        {
            var order = orders[o];
            var tiers = order.Tiers * -1;

            for (var a = tiers[0]; a <= 10; a++)
            {
                for (var b = tiers[1]; b <= 10 - a; b++)
                {
                    for (var c = tiers[2]; c <= 10 - a - b; c++)
                    {
                        for (var d = tiers[3]; d <= 10 - a - b - c; d++)
                        {
                            if (was[a, b, c, d].Cnt != 0 && (min[o] == 0 || min[o] > was[a, b, c, d].Cnt))
                            {
                                min[o] = was[a, b, c, d].Cnt;
                            }
                        }
                    }
                }
            }
        }

        var shortPaths = new List<Path>[orders.Count];
        for (var i = 0; i < shortPaths.Length; i++)
        {
            shortPaths[i] = new List<Path>();
        }

        for (var o = 0; o < orders.Count; o++)
        {
            var order = orders[o];
            var tiers = order.Tiers * -1;
            for (var a = tiers[0]; a <= 10; a++)
            {
                for (var b = tiers[1]; b <= 10 - a; b++)
                {
                    for (var c = tiers[2]; c <= 10 - a - b; c++)
                    {
                        for (var d = tiers[3]; d <= 10 - a - b - c; d++)
                        {
                            if (was[a, b, c, d].Cnt != 0 && was[a, b, c, d].Cnt <= min[o] + 1)
                            {
                                foreach (var path in was[a, b, c, d].Path)
                                {
                                    if (!shortPaths[o].Contains(path))
                                    {
                                        shortPaths[o].Add(path);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return shortPaths;
    }

    private (byte Cnt, List<Path> Path)[,,,] SpaceFill(ImmutableStack<Spell> spells)
    {
        var was = new (byte Cnt, List<Path> Path)[11, 11, 11, 11];

        var maxD = 0;

        var queue = new Queue<GameState>();

        var state = new GameState(
            new StateRes((0, 0, 0, 0), 0),
            spells,
            new List<Order>(),
            new List<Learn>(),
            new HashSet<int>(),
            new HashSet<int>(),
            new HashSet<int>(),
            Path.Instance,
            false);

        queue.Enqueue(state);

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeout && queue.Count != 0)
        {
            var cs = queue.Dequeue();

            if (maxD == depth)
            {
                //Console.Error.WriteLine(maxD);
                return was;
            }

            var actions =
                state.Orders.Cast<IAction>()
                    .Concat(state.Tome)
                    .Concat(state.Spells)
                    .Append(new Rest())
                    .ToList();

            foreach (var action in actions)
            {
                if (sw.ElapsedMilliseconds > timeout)
                {
                    break;
                }

                var nextStates = action is Spell spell
                    ? spell.GetNexStatesForSpell(cs)
                    : new[] { action.TryGetNext(cs) };

                foreach (var nextState in nextStates)
                {
                    if (sw.ElapsedMilliseconds > timeout)
                    {
                        break;
                    }

                    if (nextState != null)
                    {
                        var currentD = nextState.Actions.GetLength();

                        var prevInv = cs.StateRes.Inventary;
                        var inv = nextState.StateRes.Inventary;

                        if (was[inv[0], inv[1], inv[2], inv[3]].Cnt == 0)
                        {
                            was[inv[0], inv[1], inv[2], inv[3]].Cnt = (byte)currentD;
                            was[inv[0], inv[1], inv[2], inv[3]].Path = new List<Path> { nextState.Actions };
                        }
                        else
                        {
                            if (was[inv[0], inv[1], inv[2], inv[3]].Cnt + 1 >= currentD)
                            {
                                was[inv[0], inv[1], inv[2], inv[3]].Path.Add(nextState.Actions);
                            }
                        }

                        if (inv[0] != prevInv[0]
                            || inv[1] != prevInv[1]
                            || inv[2] != prevInv[2]
                            || inv[3] != prevInv[3])
                        {
                            if (was[inv[0], inv[1], inv[2], inv[3]].Cnt != 0 && was[inv[0], inv[1], inv[2], inv[3]].Cnt < currentD)
                            {
                                continue;
                            }
                        }

                        queue.Enqueue(nextState);
                        maxD = Math.Max(maxD, currentD);
                    }
                }
            }
        }


        if (queue.Count == 0)
        {
            //Console.Error.WriteLine($" {maxD} I'm BEST!");
        }
        else
        {
            //Console.Error.WriteLine($" {maxD} NOT BAD!");
        }

        return was;
    }
}


#region DataStructures

public class GameState
{
    public GameState(
        StateRes stateRes,
        ImmutableStack<Spell> spells,
        List<Order> orders,
        List<Learn> tome,
        HashSet<int> usedOrders,
        HashSet<int> usedSpells,
        HashSet<int> learnedByTomIndex,
        Path actions,
        bool needRest)
    {
        StateRes = stateRes;
        Spells = spells;
        Orders = orders;
        UsedOrders = usedOrders;
        UsedSpells = usedSpells;
        Tome = tome;
        LearnedByTomIndex = learnedByTomIndex;
        Actions = actions;
        NeedRest = needRest;
    }

    public StateRes StateRes { get; }
    public ImmutableStack<Spell> Spells { get; }
    public List<Order> Orders { get; }
    public List<Learn> Tome { get; }
    public HashSet<int> UsedOrders { get; }
    public HashSet<int> UsedSpells { get; }
    public HashSet<int> LearnedByTomIndex { get; }
    public Path Actions { get; }
    public bool NeedRest { get; }

    public decimal GetProfit()
    {
        return (StateRes.GetScore() * 1.0m) / Actions.GetLength();
    }
}

public class StateRes
{
    public StateRes(Tiers inventary, byte rupees)
    {
        Inventary = inventary;
        Rupees = rupees;
    }

    public byte Rupees { get; }
    public Tiers Inventary { get; }

    public int GetScore()
    {
        return Rupees + Inventary[1] + Inventary[2] + Inventary[3];
    }
}

public class Wait : IAction
{
    public GameState TryGetNext(GameState gameState)
    {
        return gameState;
    }

    public string Print()
    {
        return "WAIT";
    }
}

public class Rest : IAction
{
    public GameState TryGetNext(GameState gameState)
    {
        if (!gameState.NeedRest)
        {
            return null;
        }
        return new GameState(
            gameState.StateRes,
            gameState.Spells,
            gameState.Orders,
            gameState.Tome,
            gameState.UsedOrders,
            new HashSet<int>(),
            gameState.LearnedByTomIndex,
            gameState.Actions.Add(this),
            false
        );
    }

    public string Print()
    {
        return "REST";
    }
}

public class Spell : IAction
{
    private readonly int cnt;

    public Spell(int id, (int zero, int first, int second, int third) tiers, bool repeatable, int cnt)
    {
        this.cnt = cnt;
        Id = id;
        Tiers = tiers;
        Repeatable = repeatable;
    }

    public Spell(int id, Tiers tiers, bool repeatable, int cnt)
    {
        this.cnt = cnt;
        Id = id;
        Tiers = tiers;
        Repeatable = repeatable;
    }

    public bool IsRepeatable()
    {
        return Repeatable;
    }

    public int Id { get; }
    public Tiers Tiers { get; }
    public bool Repeatable { get; }

    public GameState TryGetNext(GameState gameState)
    {
        if (gameState.UsedSpells.Contains(Id))
        {
            return null;
        }
        //can spell
        var nextInventary = gameState.StateRes.Inventary + (Tiers * cnt);
        if (!nextInventary.IsValidInventary())
        {
            return null;
        }

        var nextStateRes = new StateRes(nextInventary, gameState.StateRes.Rupees);
        var usedSpells = new HashSet<int>(gameState.UsedSpells) { Id };

        return new GameState
        (
            nextStateRes,
            gameState.Spells,
            gameState.Orders,
            gameState.Tome,
            gameState.UsedOrders,
            usedSpells,
            gameState.LearnedByTomIndex,
            gameState.Actions.Add(this),
            true
        );
    }

    public string Print()
    {
        return $"CAST {Id} {cnt}";
    }
}

public class Order : IAction
{
    public Order(int id, (int zero, int first, int second, int third) tiers, byte price)
    {
        Id = id;
        Tiers = tiers;
        Price = price;
    }

    public int Id { get; }
    public Tiers Tiers { get; }
    public byte Price { get; }

    public GameState TryGetNext(GameState gameState)
    {
        if (gameState.UsedOrders.Contains(Id))
        {
            return null;
        }
        //can brew
        var nextInventary = gameState.StateRes.Inventary + Tiers;
        if (!nextInventary.IsValidInventary())
        {
            return null;
        }

        var nextStateRes = new StateRes(nextInventary, (byte)(gameState.StateRes.Rupees + Price));
        var usedOrders = new HashSet<int>(gameState.UsedOrders) { Id };

        return new GameState
        (
            nextStateRes,
            gameState.Spells,
            gameState.Orders,
            gameState.Tome,
            usedOrders,
            gameState.UsedSpells,
            gameState.LearnedByTomIndex,
            gameState.Actions.Add(this),
            gameState.NeedRest
        );
    }

    public string Print()
    {
        return $"BREW {Id}";
    }
}

public class Learn : IAction
{
    public Learn(int id, int tax, Tiers spell, int tomeIndex)
    {
        Id = id;
        Tax = tax;
        Spell = spell;
        TomeIndex = tomeIndex;
    }

    public int Id { get; }
    public int Tax { get; }
    public Tiers Spell { get; }
    public int TomeIndex { get; }

    public GameState TryGetNext(GameState gameState)
    {
        if (gameState.LearnedByTomIndex.Contains(TomeIndex))
        {
            return null;
        }
        //предыдущим не апдейтим Tax (?!)

        var currentInv = gameState.StateRes.Inventary;
        var forLearn = currentInv + (-TomeIndex, 0, 0, 0);
        if (!forLearn.IsValidInventary())
        {
            return null;
        }

        Tiers newInventary = (
            Math.Min(10, currentInv[0] - TomeIndex + Tax),
            currentInv[1],
            currentInv[2],
            currentInv[3]
        );

        var ns = new StateRes(newInventary, gameState.StateRes.Rupees);
        var repeatable = Spell[0] < 0 || Spell[1] < 0 || Spell[2] < 0 || Spell[3] < 0;
        var nSpells = gameState.Spells.Push(new Spell(Guid.NewGuid().GetHashCode(), Spell, repeatable, 1));
        var learnedByTomIndex = new HashSet<int>(gameState.LearnedByTomIndex) { TomeIndex };
        return new GameState(
            ns,
            nSpells,
            gameState.Orders,
            gameState.Tome,
            gameState.UsedOrders,
            gameState.UsedSpells,
            learnedByTomIndex,
            gameState.Actions.Add(this),
            gameState.NeedRest
        );
    }

    public string Print()
    {
        return $"LEARN {Id}";
    }
}

public interface IAction
{
    GameState TryGetNext(GameState gameState);
    string Print();
}

public class Tiers
{
    private (int zero, int first, int second, int third) val;

    public static implicit operator Tiers((int zero, int first, int second, int third) tiers)
    {
        return new Tiers(tiers.zero, tiers.first, tiers.second, tiers.third);
    }

    public bool IsValidInventary()
    {
        return IsPositiveOrZero()
               && (val.zero + val.first + val.second + val.third) <= 10;
    }

    private static bool IsValid(Tiers tiers, Func<int, bool> validation)
    {
        for (var i = 0; i < 4; i++)
        {
            if (!validation(tiers[i]))
            {
                return false;
            }
        }
        return true;
    }

    public bool IsPositiveOrZero()
    {
        return IsValid(this, t => t >= 0);
    }

    public static Tiers operator +(Tiers first, Tiers second)
    {
        return (
            first.val.zero + second.val.zero,
            first.val.first + second.val.first,
            first.val.second + second.val.second,
            first.val.third + second.val.third
            );
    }

    public static Tiers operator -(Tiers first, Tiers second)
    {
        return first + (second * -1);
    }

    public static Tiers operator *(int first, Tiers second)
    {
        return second * first;
    }

    public static Tiers operator *(Tiers first, int second)
    {
        return (
            first.val.zero * second,
            first.val.first * second,
            first.val.second * second,
            first.val.third * second
        );
    }

    public int this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                    return val.zero;
                case 1:
                    return val.first;
                case 2:
                    return val.second;
                case 3:
                    return val.third;
            };

            throw new Exception("invalid arg");
        }


        private set
        {
            switch (i)
            {
                case 0:
                    val.zero = value;
                    return;
                case 1:
                    val.first = value;
                    return;
                case 2:
                    val.second = value;
                    return;
                case 3:
                    val.third = value;
                    return;
            };

            throw new Exception("invalid arg");
        }
    }

    private Tiers(int zero, int first, int second, int third)
    {
        val = (zero, first, second, third);
    }

    public override string ToString()
    {
        return $"[{val.zero} {val.first} {val.second} {val.third}]";
    }
}

public class Path
{
    public override int GetHashCode()
    {
        return (chain != null ? chain.GetHashCode() : 0);
    }

    public static readonly Path Instance = new Path();

    private Path()
    {
        chain = ImmutableStack<IAction>.Instance;
    }

    private Path(ImmutableStack<IAction> stack)
    {
        chain = stack;
    }

    public Path Add(IAction item)
    {
        return new Path(chain.Push(item));
    }

    public ImmutableStack<IAction> GetActions()
    {
        return chain;
    }

    public int GetLength()
    {
        if (chain.GetTop() == null)
        {
            return 0;
        }
        return chain.GetTop().Index + 1;
    }

    public IAction TryGetFirstAction()
    {
        return chain.GetDno().Item;
    }

    public int GetUniqueSpellCnt()
    {
        if (uniqueSpellCnt == null)
        {
            FillFingerprint();
        }

        return uniqueSpellCnt.Value;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var action in chain.Reverse())
        {
            sb.Append($" -> {action.Print()}");
        }
        return sb.ToString();
    }

    protected bool Equals(Path other)
    {
        if (GetUniqueSpellCnt() != other.GetUniqueSpellCnt())
        {
            return false;
        }

        for (var i = 0; i < fingerprint.Length; i++)
        {
            if (fingerprint[i] != other.fingerprint[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Path) obj);
    }

    private void FillFingerprint()
    {
        var a = chain.Where(x => x is Spell)
            .Select(x => ((Spell) x).Id)
            .ToArray();

        Array.Sort(a);
        fingerprint = a;

        var cnt = 0;
        var val = -7;
        for (var i = 0; i < fingerprint.Length; i++)
        {
            if (val != i)
            {
                cnt++;
                val = i;
            }
        }

        uniqueSpellCnt = cnt;
    }

    private int? uniqueSpellCnt;
    private int[] fingerprint;

    private readonly ImmutableStack<IAction> chain;
}

public class ImmutableStack<T> : IEnumerable<T>
{
    public static readonly ImmutableStack<T> Instance = new ImmutableStack<T>(null, null);

    public ImmutableStack<T> Push(T item)
    {
        var newTop = new ImmutableNode<T>(item, top);
        return new ImmutableStack<T>(newTop, dno);
    }

    public ImmutableNode<T> GetTop()
    {
        return top;
    }

    public ImmutableNode<T> GetDno()
    {
        return dno;
    }

    private ImmutableStack(ImmutableNode<T> top, ImmutableNode<T> dno)
    {
        this.top = top;
        this.dno = dno ?? top;
    }

    private readonly ImmutableNode<T> top;
    private readonly ImmutableNode<T> dno;

    public IEnumerator<T> GetEnumerator()
    {
        var currentTop = top;
        while (currentTop != null)
        {
            yield return currentTop.Item;
            currentTop = currentTop.Prev;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class ImmutableNode<T>
{
    public ImmutableNode(T item, ImmutableNode<T> prev)
    {
        Item = item;
        Prev = prev;
        if (prev == null)
        {
            Index = 0;
        }
        else
        {
            Index = Prev.Index + 1;
        }
    }

    public T Item { get; }

    public ImmutableNode<T> Prev { get; }

    public int Index { get; }
}

public static class SpellHelper
{
    public static IEnumerable<GameState> GetNexStatesForSpell(this Spell spell, GameState gs)
    {
        yield return spell.TryGetNext(gs);

        if (spell.IsRepeatable())
        {
            var inventary = gs.StateRes.Inventary;
            var cnt = 2;
            var currentInv = inventary + spell.Tiers * cnt;
            while (currentInv.IsValidInventary())
            {
                yield return new Spell(spell.Id, spell.Tiers, false, cnt).TryGetNext(gs);
                cnt++;
                currentInv = inventary + spell.Tiers * cnt;
            }
        }
    }
}

#endregion
