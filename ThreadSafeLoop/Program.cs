using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSafeLoop
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Looper!");

            var z = 0;
            var zz = Interlocked.CompareExchange(ref z, 1, 0);
            var maxTotal = 1_000_000;

            // naive
            Console.WriteLine("Naive");
            for (int i = 100; i < maxTotal; i*=10)
            {
                await Test(i, x=>x.GetNextNaive());
            }
            
            // interlocked
            Console.WriteLine("Interlocked");
            for (int i = 100; i < maxTotal; i*=10)
            {
                await Test(i, x=>x.GetNext());
            }
            
            //Console.ReadKey();
            Console.WriteLine("Finish");
        }

        private static async Task Test(int total,Func<Looper, int> func)
        {
            var sw = Stopwatch.StartNew();
            var zero = 0;
            var one = 0;
            var mistake = 0;

            var looper = new Looper();
            var range = Enumerable.Range(0, total);
            var tasks = range.Select(x => Task.Run(() =>
            {
                var i = func(looper);
                if (i == 0)
                    Interlocked.Increment(ref zero);
                else if (i == 1)
                    Interlocked.Increment(ref one);
                else
                    Interlocked.Increment(ref mistake);
            }));
            await Task.WhenAll(tasks);

            sw.Stop();
            Console.WriteLine($"[{sw.Elapsed}] Total was {total} and zeros={zero}({((decimal)zero / total):P1}) and ones={one}({((decimal)one / total):P1}) MISTAKES {mistake}");
        }
    }

    public class Looper
    {
        private int _max;
        private int _current;

        public Looper()
        {
            _max = 2;
            _current = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNext()
        {
            var i = Interlocked.Increment(ref _current);
            if (i < _max)
                return i;
            
            Interlocked.Exchange(ref _current, 0);
            return 0;
        }

        public int GetNextNaive()
        {
            if (_current >= _max)
                _current = 0;
            return _current++;
        }
        
    }
}