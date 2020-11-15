using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Witches
{
    public class WorksheetSpells
    {
        [Test]
        public void Test1()
        {
            var allSpells = GetAllSpells().ToList();

            var startSpell = ImmutableStack<Spell>.Instance
                .Push(allSpells[0])
                .Push(allSpells[1])
                .Push(allSpells[2])
                .Push(allSpells[3])
                .Push(allSpells[15])
                .Push(allSpells[27])
                .Push(allSpells[31])
                .Push(allSpells[9])
                .Push(allSpells[7])
                .Push(allSpells[11]);

            //var startSpell = ImmutableStack<Spell>.Instance;

            //foreach (var spell in allSpells)
            //{
            //    var excluded = new int[0];
            //    //{
            //    // //   121, 120, 119, 118, 117, 109, 108, 107, 101, // образующие элементы. (8 / 42)
            //    //    //найти популярные продолжения (2) посчитать "потенциальную" стоимость префикса
            //    //    //как только ты заканчиваешь текщее зелье (если ты его варишь..)
            //    //    //забываешь в переборе про
            //    //}; 
            //    if (!excluded.Contains(spell.Id))
            //        startSpell = startSpell.Push(spell);
            //}

            //var spells = ImmutableStack<Spell>.Instance;
            //foreach (var spell in startSpell)
            //{
            //    spells = spells.Push(spell);
            //}

            //var orders = OrderReceipts;
            var orders = new List<Order>
            {
                OrderReceipts[2],
                OrderReceipts[7],
                OrderReceipts[22],
                OrderReceipts[31],
                OrderReceipts[13],
            };

            var sw = new Stopwatch();
            sw.Start();
            
            var respeller = new Respeller();
            respeller.Resolve(startSpell);
            
            Console.WriteLine(sw.ElapsedMilliseconds);

            var min = new byte[orders.Count];

            //for (var a = 0; a <= 10; a++)
            //{
            //    for (var b = 0; b <= 10 - a; b++)
            //    {
            //        for (var c = 0; c <= 10 - a - b; c++)
            //        {
            //            for (var d = 0; d <= 10 - a - b - c; d++)
            //            {
            //                if (respeller.Was[a, b, c, d].Cnt == 0)
            //                {
            //                    continue;
            //                }

            //                for(var i = 0; i < orders.Count; i++)
            //                {
            //                    var receipt = orders[i].Tiers;
            //                    if (
            //                        receipt[0] > a
            //                        || receipt[1] > b
            //                        || receipt[2] > c
            //                        || receipt[3] > d
            //                    )
            //                    {
            //                        continue;
            //                    }

            //                    if(min[i].Cnt == 0 || min[i].Cnt > respeller.Was[a, b, c, d].Cnt)
            //                    {
            //                        min[i] = respeller.Was[a, b, c, d];
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            for(var o = 0; o < orders.Count; o++)
            {
                var order = orders[o];

                for (var a = order.Tiers[0]; a <= 10; a++)
                {
                    for (var b = order.Tiers[1]; b <= 10 - a; b++)
                    {
                        for (var c = order.Tiers[2]; c <= 10 - a - b; c++)
                        {
                            for (var d = order.Tiers[3]; d <= 10 - a - b - c; d++)
                            {
                                if (respeller.Was[a, b, c, d].Cnt != 0 && (min[o] == 0 || min[o] > respeller.Was[a, b, c, d].Cnt))
                                {
                                    min[o] = respeller.Was[a, b, c, d].Cnt;
                                }
                            }
                        }
                    }
                }
            }


            var shortPaths = new List<Path>[orders.Count];

            for (var o = 0; o < orders.Count; o++)
            {
                var order = orders[o];

                for (var a = order.Tiers[0]; a <= 10; a++)
                {
                    for (var b = order.Tiers[1]; b <= 10 - a; b++)
                    {
                        for (var c = order.Tiers[2]; c <= 10 - a - b; c++)
                        {
                            for (var d = order.Tiers[3]; d <= 10 - a - b - c; d++)
                            {
                                if (respeller.Was[a, b, c, d].Cnt != 0 && respeller.Was[a, b, c, d].Cnt <= min[o] + 1)
                                {
                                    if (shortPaths[o] == null)
                                    {
                                        shortPaths[o] = new List<Path>();
                                    }

                                    foreach (var path in respeller.Was[a, b, c, d].Path)
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


            Console.WriteLine($"Total path: {shortPaths.SelectMany(x => x).Count()}");


            var cnt = shortPaths.SelectMany(x => x)
                .SelectMany(x => x.GetActions())
                .Where(x => x is Spell)
                .Select(x => ((Spell) x).Id)
                .Where(x => x > 104)
                .ToHashSet()
                .Count;

            Console.WriteLine($"Total new spells: {cnt}");

            for (var i = 0; i < orders.Count; i++)
            {
                var sb = new StringBuilder($"{orders[i].Tiers}: {min[i]} |\r\n");
                if (shortPaths[i].Count != 0)
                {
                    foreach (var path in shortPaths[i])
                    {
                        sb.AppendLine($"\t{path}");
                    }
                }

                Console.WriteLine(sb);
            }
        }

        [Test]
        public void PriceTest()
        {
            var a = 1;
            var b = 3;
            var c = 2;
            var d = 4;

            int AdjustPrice(Tiers tiers)
            {
                return tiers[0] * a + tiers[1] * b + tiers[2] * c + tiers[3] * d;
            }

            var cnt = 0;
            foreach (var order in OrderReceipts)
            {
                var adjustPrice = AdjustPrice(order.Tiers);
                if (adjustPrice == order.Price)
                {
                    cnt++;
                }

                if (adjustPrice < order.Price)
                {
                    Console.WriteLine($"Expensive order: {order.Id}. Price: {order.Price}. Adjust price: {adjustPrice}");
                }
                if (adjustPrice > order.Price)
                {
                    Console.WriteLine($"Bad order: {order.Id}. Price: {order.Price}. Adjust price: {adjustPrice}");
                }
            }

            Console.WriteLine($"Adjust order: {cnt}.");
        }

        private static IEnumerable<Spell> GetAllSpells()
        {
            var idx = 100;
            var res = new List<Spell>();
            foreach (var receipt in AllSpellsReceipts)
            {
                idx++;
                var repeatable = receipt[0] < 0 || receipt[1] < 0 || receipt[2] < 0 || receipt[3] < 0;
                repeatable = repeatable && idx > 104;
                res.Add(new Spell(idx, receipt, repeatable, 1));
            }

            return res;
        }
        //2 0 0 0
        //0 2 0 2
        //0 0 2 2
        //2 0 2 2
        //0 2 2 4
        //2 2 2 4
        //0 4 2 6
        private static readonly List<Tiers> AllSpellsReceipts = new List<Tiers>
        {
            (2, 0, 0, 0),
            (-1, 1, 0, 1),
            (0, -1, 1, 0),
            (0, 0, -1, 1),
            (-3, 0, 0, 1),
            (3, 0, -1, 0),
            (1, 0, 1, 0), //7
            (0, 1, 0, 0), //8
            (3, 0, 0, 0), //9 
            (2, -2, 3, 0),
            (2, -2, 1, 1),
            (3, 1, 0, -1),
            (3, 1, -2, 0),
            (2, 2, -3, 0),
            (2, 0, 2, -1),
            (-4, 2, 0, 0),
            (2, 0, 1, 0), //17
            (4, 0, 0, 0), //18
            (0, 0, 0, 1), //19
            (0, 0, 2, 0), //20
            (1, 1, 0, 0), //21
            (-2, 1, 0, 0),
            (-1, 0, -1, 1),
            (0, -1, 2, 0),
            (2, 0, -2, 1),
            (-3, 1, 1, 0),
            (0, -2, 2, 1),
            (1, 1, -3, 1),
            (0, 0, 3, -1),
            (0, 0, -3, 2),
            (1, 1, 1, -1),
            (1, -1, 2, 0),
            (4, -1, 1, 0),
            (-5, 0, 0, 2),
            (-4, 1, 0, 1),
            (0, 2, 3, -2),
            (1, 3, 1, -2),
            (-5, 3, 0, 0),
            (-2, -1, 0, 2),
            (0, -3, 0, 3),
            (0, 3, -3, 0),
            (-3, 0, 3, 0),
            (-2, 0, 2, 0),
            (0, -2, 0, 2),
            (0, 2, -2, 0),
            (0, 2, 0, -1),
        };

        private static readonly List<Order> OrderReceipts = new List<Order>
        {
            new Order(1, (0, 0, 5, 0), 10),
            new Order(2, (2, 0, 0, 2), 10),
            new Order(3, (2, 3, 0, 0), 11),
            new Order(4, (3, 0, 0, 2), 11),
            new Order(5, (0, 4, 0, 0), 12),
            new Order(6, (0, 0, 2, 2), 12),
            new Order(7, (0, 0, 3, 2), 14),
            new Order(8, (2, 0, 0, 3), 14),
            new Order(9, (0, 5, 0, 0), 15),
            new Order(10, (0, 0, 0, 4), 16),
            new Order(11, (0, 0, 2, 3), 16),
            new Order(12, (0, 3, 0, 2), 17),
            new Order(13, (0, 2, 3, 0), 12),
            new Order(14, (0, 3, 2, 0), 13),
            new Order(15, (0, 2, 0, 3), 18),
            new Order(16, (0, 2, 0, 2), 14),
            new Order(17, (0, 0, 0, 5), 20),
            new Order(18, (2, 0, 1, 1), 9),
            new Order(19, (0, 1, 2, 1), 12),
            new Order(20, (1, 2, 0, 1), 12),
            new Order(21, (2, 2, 2, 0), 13),
            new Order(22, (2, 0, 2, 2), 15),
            new Order(23, (2, 2, 0, 2), 17),
            new Order(24, (0, 2, 2, 2), 19),
            new Order(25, (1, 1, 1, 1), 12),
            new Order(26, (3, 1, 1, 1), 14),
            new Order(27, (1, 1, 3, 1), 16),
            new Order(28, (1, 1, 1, 3), 20),
            new Order(29, (1, 3, 1, 1), 18),
            new Order(30, (2, 2, 0, 0), 6),
            new Order(31, (3, 2, 0, 0), 7),
            new Order(32, (0, 4, 0, 0), 8),
            new Order(33, (2, 0, 2, 0), 8),
            new Order(34, (2, 3, 0, 0), 8),
            new Order(35, (3, 0, 2, 0), 9),
            new Order(36, (0, 2, 2, 0), 10),
        };
    }

    public class Respeller
    {
        public const int timeout = 5_000;
        public (byte Cnt, List<Path> Path)[,,,] Was { get; private set; } = new (byte Cnt, List<Path> Path)[11, 11, 11, 11];

        public void Resolve(ImmutableStack<Spell> spells)
        {
            var processed = 0;
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
                processed++;
                var cs = queue.Dequeue();

                if (maxD == 8)
                {
                    Console.WriteLine(maxD);
                    return;
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
                        ? GetRepeatableSpells(spell, cs)
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

                            if (Was[inv[0], inv[1], inv[2], inv[3]].Cnt == 0)
                            {
                                Was[inv[0], inv[1], inv[2], inv[3]].Cnt = (byte)currentD;
                                Was[inv[0], inv[1], inv[2], inv[3]].Path = new List<Path> {nextState.Actions};
                            }
                            else
                            {
                                if (Was[inv[0], inv[1], inv[2], inv[3]].Cnt + 1 >= currentD)
                                {
                                    Was[inv[0], inv[1], inv[2], inv[3]].Path.Add(nextState.Actions);
                                }
                            }

                            if (inv[0] != prevInv[0]
                                || inv[1] != prevInv[1]
                                || inv[2] != prevInv[2]
                                || inv[3] != prevInv[3])
                            {
                                if (Was[inv[0], inv[1], inv[2], inv[3]].Cnt != 0 && Was[inv[0], inv[1], inv[2], inv[3]].Cnt < currentD)
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
                Console.WriteLine($" {maxD} I'm BEST!");
            }
            else
            {
                Console.WriteLine($" {maxD} NOT BAD!");
            }
        }

        private static IEnumerable<GameState> GetRepeatableSpells(Spell spell, GameState gs)
        {
            yield return spell.TryGetNext(gs);

            if (spell.IsRepeatable())
            {
                var inventary = gs.StateRes.Inventary;
                var cnt = 2;
                var currentInv = inventary + (spell.Tiers * cnt);
                while (currentInv.IsValidInventary())
                {
                    yield return new Spell(spell.Id, spell.Tiers, false, cnt).TryGetNext(gs);
                    cnt++;
                    currentInv = inventary + spell.Tiers * cnt;
                }
            }
        }
    }
}