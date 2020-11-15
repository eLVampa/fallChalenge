using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Witches
{
    public class Workheet
    {

        [Test]
        public void StatesCntTest()
        {
            // вершин 10^3;

            // ребер 30

            //30к (!)

            var cnt = 0;
            for (var a = 0; a <= 10; a++)
            {
                for (var b = 0; b <= 10- a; b++)
                {
                    for (var c = 0; c <= 10 - a - b; c++)
                    {
                        for (var d = 0; d <= 10 - a - b -c; d++)
                        {
                            cnt++;
                        }
                    }
                }
            }

            Console.WriteLine(cnt);
        }

        [Test]
        public void DeepCheck()
        {
            var res = new StateRes((3, 0, 0, 0), 0);
            var spells = ImmutableStack<Spell>.Instance
                .Push(new Spell(1, (2, 0, 0, 0), false, 1))
                .Push(new Spell(2, (-1, 1, 0, 0), false, 1))
                .Push(new Spell(3, (0, -1, 1, 0), false, 1))
                .Push(new Spell(4, (0, 0, -1, 1), false, 1))
                .Push(new Spell(5, (3, -1, 0, 0), true, 1))
                .Push(new Spell(6, (0, 3, 0, -1), true, 1))
                .Push(new Spell(5, (2, 3, -2, 0), true, 1))
                .Push(new Spell(7, (0, -3, 0, 2), true, 1));

            var orders = new List<Order>
            {
                new Order(8, (-2, -2, -2, 0), 16),
                new Order(9, (-2, 0, 0, -2), 11),
                new Order(10, (0, -4, 0, 0), 8),
                new Order(11, (0, -3, 0, -2), 14),
                new Order(12, (-2, -3, 0, 0), 8),
            };

            var learns = new List<Learn>
            {
                new Learn(13, 0, (0, 0, -3, 3), 0),
                new Learn(14, 0, (0, 2, -2, 1), 1),
                new Learn(15, 0, (-5, 0, 0, 2), 2),
                new Learn(16, 0, (2, 2, 0, -1), 3),
                new Learn(17, 0, (0, -3, 3, 0), 4),
                new Learn(18, 0, (-1, 0, -1, 1), 5),
            };

            var currentState = new GameState
            (
                res,
                spells,
                orders,
                learns,
                new HashSet<int>(),
                new HashSet<int>(),
                new HashSet<int>(),
                ImmutableStack<IAction>.Instance,
                false
            );

            var action = new Resolver(true).Resolve(currentState);
            //action.Should().Contain("NOT BAD");
            Console.WriteLine(action);
        }
    }
}
