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

            var maxTotal = 100000;

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
            
            // ideal
            Console.WriteLine("Ideal");
            for (int i = 100; i < maxTotal; i*=10)
            {
                await Test(i, x=>x.GetNextIdeal());
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
            Console.WriteLine($"[{sw.Elapsed}] Total={total} Zeros={zero}({((decimal)zero / total):P1}) Ones={one}({((decimal)one / total):P1}) MISTAKES {mistake}");
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
        
        private readonly int[] _pool = new int[] {0, 1};
        
        public int GetNextIdeal()
        {
            var i = Interlocked.Increment(ref _current);
            i %= _pool.Length;
            return _pool[Math.Abs(i)];
        }
    }
    
    public class Looper2
    {
        private int _max = Int32.MaxValue - 100000;
        private int _current = -1;
        private readonly int[] _pool = new int[] {0, 1};
        
        public Looper2()
        {
        }

        public int GetNext()
        {
            var i = Interlocked.Increment(ref _current);
            if (_current >= _max)
            {
                _current = 0;
            }
            
            i %= _pool.Length;
            return _pool[Math.Abs(i)];
        }

        public int GetNextNaive()
        {
            if (_current >= _max)
                _current = 0;
            return _current++;
        }
    }
}