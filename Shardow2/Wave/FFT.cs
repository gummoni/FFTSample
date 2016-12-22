using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ShadowingApp.Wave
{
	//http://www.codeproject.com/Articles/9388/How-to-implement-the-FFT-algorithm
	//http://wildpie.hatenablog.com/entry/2014/09/24/000900
	//https://gerrybeauregard.wordpress.com/2011/04/01/an-fft-in-c/
	//http://morimori2008.web.fc2.com/contents/PCprograming/Csharp/fft.html
	public class FFT
	{
		static int bits = 6;
		static int size { get; } = (int)Math.Pow(2, bits);						// 波形サイズ

		/// <summary>
		/// 変換
		/// </summary>
		public FFT()
		{
		}

		/// <summary>
		/// スペクトル出力
		/// </summary>
		/// <returns></returns>
		public Bitmap GetSpectrum()
		{
			var height = 16;
			var result = new Bitmap(MAXRANGE, height);
			using (var g = Graphics.FromImage(result))
			{

				for (var v = 0; v < MAXRANGE; v++)
				{
					var col = GetColor(v);
					g.DrawLine(new Pen(col), v, 0, v, height);
				}

				return result;
			}
		}

		/// <summary>
		/// 解析処理
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public Bitmap Analyze(string filename)
		{
			Bitmap result = null;
			//var width = 256;

			//using (var wr = new WaveFileReader(filename))
			//{
			//	result = new Bitmap(width, size);
			//	var SRC = new MValue(size);

			//	var x = 0;
			//	var count = 0;
			//	float[] frames;
			//	while ((frames = wr.ReadNextSampleFrame()) != null)
			//	{
			//		//データ取り込み
			//		SRC.Re[count] = frames[0];
			//		SRC.Im[count] = 0;
			//		count++;
			//		if (size == count)
			//		{
			//			//FFT変換開始
			//			//SRC.Windowing(WindowFunc.Hamming);
			//			MValue DST;
			//			WaveAnalyzer.DFT(size, wr.WaveFormat.SampleRate, SRC.Re, out DST);
			//			//WaveAnalyzer.FFT(SRC, out DST, bits);
			//			DST.Windowing(WindowFunc.Hamming);
			//			var values = DST.Normalize(MAXRANGE);

			//			//出力
			//			for (var y = 0; y < size; y++)
			//			{
			//				var color = GetColor(values[y]);
			//				result.SetPixel(x, size - 1 - y, color);
			//			}

			//			//次へ
			//			count = 0;
			//			x++;
			//			if (width == x)
			//			{
			//				break;
			//			}
			//		}
			//	}
			//}
			return result;
		}

		/// <summary>
		/// 縮小
		/// </summary>
		/// <param name="src">入力：元画像</param>
		/// <param name="dst">出力：縮小画像</param>
		void Scaling(Bitmap src, Bitmap dst)
		{
			//ImageオブジェクトのGraphicsオブジェクトを作成する
			using (var g = Graphics.FromImage(dst))
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.DrawImage(src, 0, 0, dst.Width, dst.Height);
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			}
		}

		const int STAGE1 = 256;
		const int STAGE2 = STAGE1 + 255;
		const int STAGE3 = STAGE2 + 255;
		const int STAGE4 = STAGE3 + 255;
		const int STAGE5 = STAGE4 + 255;
		const int MAXVALUE = 255;
		const int MAXRANGE = STAGE5;	//1276
		Color GetColor(int value)
		{
			var r = 0;
			var g = 0;
			var b = 0;

			if (STAGE1 > value)
			{
				b = value;
			}
			else if (STAGE2 > value)
			{
				b = MAXVALUE;
				g = value - STAGE1;
			}
			else if (STAGE3 > value)
			{
				b = STAGE3 - value;
				g = MAXVALUE;
			}
			else if (STAGE4 > value)
			{
				g = MAXVALUE;
				r = value - STAGE3;
			}
			else if (STAGE5 > value)
			{
				g = STAGE5 - value;
				r = MAXVALUE;
			}
			else
			{
				r = MAXVALUE;
			}

			return Color.FromArgb(r, g, b);
		}
	}
}
