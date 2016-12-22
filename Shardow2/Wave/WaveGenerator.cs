using System;
using System.Collections.Generic;

namespace ShadowingApp.Wave
{
	/// <summary>
	/// Wave波形生成
	/// </summary>
	public class WaveGenerator : List<int>
	{
		int SampleHz { get; set; }
		
		// 正弦波生成
		double Sin(int freq, int i)
		{
			// 2PI = 1Hz
			var period = SampleHz * freq;
			double iso = ((double)(i % period) / (double)period);
			if (0.00 == iso) return 0.0;
			if (0.25 * Math.PI == iso) return +1.0;
			if (0.50 * Math.PI == iso) return +0.0;
			if (0.75 * Math.PI == iso) return -1.0;
			var theta = (2 * Math.PI) * iso;
			var sin = Math.Sin(theta);
			return sin;
		}

		/// <summary>
		/// コンストラクタ処理
		/// </summary>
		/// <param name="sampleHz"></param>
		public WaveGenerator(int sampleHz)
		{
			SampleHz = sampleHz;
		}

		public MValue Generate(int size)
		{
			var result = new MValue(size);

			for (var i = 0; i < size; i++)
			{
				foreach (var hz in this)
				{
					result.Re[i] += Sin(hz, i);
				}
			}

			return result;
		}
	}
}
