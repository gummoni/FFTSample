using ShadowingApp.Wave;
using System;
using System.Linq;

namespace ConsoleApplication1
{
    class Program
	{
		static void Main(string[] args)
		{
            //有効周波数はサンプリング周波数の２倍
            var len = 32;
            var sampleHz = 1;
            var gene = new WaveGenerator(sampleHz);
            gene.Add(7);
            var data = gene.Generate(len);
            data.Normalize();

            MValue ret2;
            data.DFT(sampleHz, out ret2);
            var t2 = ret2.ToPowerValue();
            var ans1 = bunsan(t2);

            Console.WriteLine(ans1);
            Console.ReadLine();
		}


		static double bunsan(int[] data)
		{
			var result = 0.0;

			var max = data.Max();
			var sum = data.Sum();
			var len = data.Length;
			var mid = (double)sum / (double)len;
			foreach (var dat in data)
			{
				result += Math.Pow(dat - mid, 2);
			}
			result /= len;

			result = Math.Sqrt(result); //σ1

			return result;


		}
	}

}

