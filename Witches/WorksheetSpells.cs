using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using NUnit.Framework;

namespace Witches
{
    public class WorksheetSpells
    {

        [Test]
        public void BugTest()
        {
            var allSpells = GetAllSpells().ToList();

            var baseSpell = ImmutableStack<Spell>.Instance
                .Push(allSpells[0])
                .Push(allSpells[1])
                .Push(allSpells[2])
                .Push(allSpells[3]);

            var tomeSpells = new List<Learn>
            {
                new Learn(244, 0, (-2, 0, -1, 2), 0),
                new Learn(245, 0, (-3, 0, 0, 1), 1),
                new Learn(246, 0, (-4, 0, 2, 0), 2),
                new Learn(247, 0, (3, -2, 1, 0), 3),
                new Learn(248, 0, (0, -3, 3, 0), 4),
                new Learn(249, 0, (0, 0, -3, 3), 5),
            };


            var orders = new List<Order>
            {
                OrderReceipts[22],
                OrderReceipts[29],
                OrderReceipts[10],
                OrderReceipts[15],
                OrderReceipts[3],
            };

            var spellOptimizer = new SpellOptimizer();
            var sw = Stopwatch.StartNew();
            var learnMap = spellOptimizer.GetSpellMapForLearn(orders, tomeSpells, baseSpell, sw);


            var spells = baseSpell;
            
           var learnRes = Player.TryGetLearnAction(learnMap, tomeSpells, spells, 3, new HashSet<int>());
           spells = spells.Push(new Spell(147, ((Learn) learnRes.action).Spell, true, 1));
           tomeSpells = tomeSpells.Where(x => x.Id != ((Learn) learnRes.action).Id).ToList();
           var learnRes2 = Player.TryGetLearnAction(learnMap, tomeSpells, spells, 3, new HashSet<int>());
           spells = spells.Push(new Spell(741, ((Learn)learnRes2.action).Spell, true, 1));
           tomeSpells = tomeSpells.Where(x => x.Id != ((Learn)learnRes2.action).Id).ToList();
           var learnRes3 = Player.TryGetLearnAction(learnMap, tomeSpells, spells, 3, new HashSet<int>());

        }

        [Test]
        public void SpellOptimizerTest()
        {
            var allSpells = GetAllSpells().ToList();

            var baseSpell = ImmutableStack<Spell>.Instance
                .Push(allSpells[0])
                .Push(allSpells[1])
                .Push(allSpells[2])
                .Push(allSpells[3]);

            var spellsForLearn = new List<Spell>()
            {
                allSpells[15],
                allSpells[27],
                allSpells[31],
                allSpells[9],
                allSpells[7],
                allSpells[11],
            };

            var tomeSpells = spellsForLearn.Select((x, i) => new Learn(x.Id, 0, x.Tiers, i))
                .ToList();

            var orders = new List<Order>
            {
                OrderReceipts[2],
                OrderReceipts[7],
                OrderReceipts[22],
                OrderReceipts[31],
                OrderReceipts[13],
            };

            var sw = Stopwatch.StartNew();
            var spellOptimizer = new SpellOptimizer();
            var mapForLearn = spellOptimizer.GetSpellMapForLearn(orders, tomeSpells, baseSpell, sw);

            foreach (var item in mapForLearn)
            {
                var sb = new StringBuilder();
                sb.Append($"points: {item.TotalRupies}, length: {item.TotalLength}, maxDeep: {item.MaxDeep}  | ");
                foreach (var i in item.LearnOrder)
                {
                    sb.Append($" -> {i}");
                }

                Console.WriteLine(sb);
            }

            Console.WriteLine("Optimal path for second set.");
            var t = mapForLearn[1];

            for (var i = 0; i < orders.Count; i++)
            {
                Console.WriteLine($"Recipes for order {i}");
                foreach (var recipes in t.OptimalRecipes[i])
                {
                    Console.WriteLine($"\t{recipes}");
                }
            }
        }


        [Test]
        public void Test1()
        {
            var allSpells = GetAllSpells().ToList();

            var startSpell = ImmutableStack<Spell>.Instance
                .Push(allSpells[0])
                .Push(allSpells[1])
                .Push(allSpells[2])
                .Push(allSpells[3]);

                //.Push(allSpells[15])
                //.Push(allSpells[27])
                //.Push(allSpells[31])
                //.Push(allSpells[9])
                //.Push(allSpells[7])
                //.Push(allSpells[11]);

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
            var orderPathFinder = new OrderPathFinder();
            var shortPaths = orderPathFinder.FindOrderPaths(orders, startSpell, sw);
            
            Console.WriteLine(sw.ElapsedMilliseconds);

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
                var sb = new StringBuilder($"{orders[i].Tiers}: |\r\n");
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
            (-1, 1, 0, 0),
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
            new Order(2, (-2, 0, 0, -2), 10),
            new Order(3, (-2, -3, 0, 0), 11),
            new Order(4, (-3, 0, 0, -2), 11),
            new Order(5, (0, -4, 0, 0), 12),
            new Order(6, (0, 0, -2, -2), 12),
            new Order(7, (0, 0, -3, -2), 14),
            new Order(8, (-2, 0, 0, -3), 14),
            new Order(9, (0, -5, 0, 0), 15),
            new Order(10, (0, 0, 0, -4), 16),
            new Order(11, (0, 0, -2, -3), 16),
            new Order(12, (0, -3, 0, -2), 17),
            new Order(13, (0, -2, -3, 0), 12),
            new Order(14, (0, -3, -2, 0), 13),
            new Order(15, (0, -2, 0, -3), 18),
            new Order(16, (0, -2, 0, -2), 14),
            new Order(17, (0, 0, 0, -5), 20),
            new Order(18, (-2, 0, -1, -1), 9),
            new Order(19, (0, -1, -2, -1), 12),
            new Order(20, (-1, -2, 0, -1), 12),
            new Order(21, (-2, -2, -2, 0), 13),
            new Order(22, (-2, 0, -2, -2), 15),
            new Order(23, (-2, -2, 0, -2), 17),
            new Order(24, (0, -2, -2, -2), 19),
            new Order(25, (-1, -1, -1, -1), 12),
            new Order(26, (-3, -1, -1, -1), 14),
            new Order(27, (-1, -1, -3, -1), 16),
            new Order(28, (-1, -1, -1, -3), 20),
            new Order(29, (-1, -3, -1, -1), 18),
            new Order(30, (-2, -2, 0, 0), 6),
            new Order(31, (-3, -2, 0, 0), 7),
            new Order(32, (0, -4, 0, 0), 8),
            new Order(33, (-2, 0, -2, 0), 8),
            new Order(34, (-2, -3, 0, 0), 8),
            new Order(35, (-3, 0, -2, 0), 9),
            new Order(36, (0, -2, -2, 0), 10),
        };
    }
}