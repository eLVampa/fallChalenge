using System;
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
                ImmutableStack<IAction>.Instance,
                needRest
            );

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
            if (turnNumber < 4)
            {
                var learn = learns.FirstOrDefault(l => l.TomeIndex == 0);
                if (learn != null)
                {
                    Console.WriteLine(learn.Print());
                    continue;
                }
            }
            var action = new Resolver(debug).Resolve(currentState);
            Console.WriteLine(action);
            // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
        }
    }


}

public class Resolver
{
    public Resolver(bool debug, int timeout = 40)
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
                // state.Orders.Cast<IAction>()
                // .Concat(state.Tome)
                ((IEnumerable<IAction>) state.Spells)
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
                    ? GetRepeatableSpells(spell, cs)
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
                        maxD = Math.Max(maxD, nextState.Actions.GetTop()?.Index ?? 0);

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

    private static IEnumerable<GameState> GetRepeatableSpells(Spell spell, GameState gs)
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

    private string GetBestAction(GameState best, List<GameState> bestCandidates, int processed, int maxD, bool timeIsOver, int queueCnt)
    {
        var theBest = best;

        foreach (var candidate in bestCandidates)
        {
            if (theBest.GetProfit() < candidate.GetProfit())
            {
                theBest = candidate;
            }

            if (theBest.GetProfit() == candidate.GetProfit() && theBest.Actions.GetTop().Index > candidate.Actions.GetTop().Index)
            {
                theBest = candidate;
            }
        }

        var bestAction = theBest?.Actions.GetDno()?.Item;
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
            sb.Append("\r\nMy Plan: \r\n");
            foreach (var action in theBest.Actions.Reverse())
            {
                sb.AppendLine(action.Print());
            }
        }

        return sb.ToString();
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
        ImmutableStack<IAction> actions,
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
    public ImmutableStack<IAction> Actions { get; }
    public bool NeedRest { get; }

    public decimal GetProfit()
    {
        return (StateRes.GetScore() * 1.0m) / (Actions.GetTop().Index + 1);
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
            gameState.Actions.Push(this),
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
            gameState.Actions.Push(this),
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
            gameState.Actions.Push(this),
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
            gameState.Actions.Push(this),
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

#endregion
