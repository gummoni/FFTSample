using System;
using System.Linq;

namespace ShadowingApp.Wave
{
	/// <summary>
	/// FFT用データ
	/// </summary>
	public class MValue
	{
		public int LogN { get; }
		public int Size { get; }
		public double[] Re { get; }
		public double[] Im { get; }

		/// <summary>
		/// コンストラクタ処理
		/// </summary>
		/// <param name="size"></param>
		public MValue(int size = 32, int logN = 5)
		{
			Size = size;
			LogN = logN;
			Re = new double[size];
			Im = new double[size];
			for (var i = 0; i < size; i++)
			{
				Re[i] = 0.0;
				Im[i] = 0.0;
			}
		}

		// 窓関数
		public void Windowing(WindowFunc windowFunc)
		{
			for (int i = 0; i < Size; i++)
			{
				double winValue = 0;
				// 各々の窓関数
				if (WindowFunc.Hamming == windowFunc)
				{
					winValue = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / Size);
				}
				else if (WindowFunc.Hanning == windowFunc)
				{
					winValue = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / Size);
				}
				else if (WindowFunc.Blackman == windowFunc)
				{
					winValue = 0.42 - 0.5 * Math.Cos(2 * Math.PI * i / Size)
									+ 0.08 * Math.Cos(4 * Math.PI * i / Size);
				}
				else if (WindowFunc.Rectangular == windowFunc)
				{
					winValue = 1.0;
				}
				else
				{
					winValue = 1.0;
				}
				// 窓関数を掛け算
				Re[i] = Re[i] * winValue;
			}
		}

		/// <summary>
		/// パワースペクトル値を取得
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public double GetValue(int index) => Math.Sqrt(Math.Pow(Re[index], 2) + Math.Pow(Im[index], 2));

		/// <summary>
		/// パワースペクトル取得
		/// </summary>
		/// <returns></returns>
		double[] GetValues()
		{
			var result = new double[Size];
			for (var i = 0; i < Size; i++)
			{
				result[i] = GetValue(i);
			}
			return result;
		}

		/// <summary>
		/// クローン作成
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public MValue Clone()
		{
			var result = new MValue(Size);
			Buffer.BlockCopy(Re, 0, result.Re, 0, Size);
			Buffer.BlockCopy(Im, 0, result.Im, 0, Size);
			return result;
		}

		/// <summary>
		/// ビット反転したデータ作成
		/// </summary>
		/// <returns></returns>
		public MValue BitScrollArray()
		{
			int[] reverseBitArray = _BitScrollArray();

			var result = new MValue(Size);
			for (int i = 0; i < Size; i++)
			{
				result.Re[i] = Re[reverseBitArray[i]];
				result.Im[i] = Im[reverseBitArray[i]];
			}
			return result;
		}

		// ビットを左右反転した配列を返す
		int[] _BitScrollArray()
		{
			int[] reBitArray = new int[Size];
			int arraySizeHarf = Size >> 1;

			reBitArray[0] = 0;
			for (int i = 1; i < Size; i <<= 1)
			{
				for (int j = 0; j < i; j++)
				{
					reBitArray[j + i] = reBitArray[j] + arraySizeHarf;
				}
				arraySizeHarf >>= 1;
			}
			return reBitArray;
		}


		/// <summary>
		/// 正規化(０～rangeの値で変化するものにする)
		/// </summary>
		/// <param name="range"></param>
		/// <returns></returns>
		public void Normalize(double offset = 0.0)
		{
			var len = Re.Length;
			var rmin = Re.Min();
			var rmax = Re.Max() - rmin;
			var imin = Im.Min();
			var imax = Im.Max() - imin;
			for (var idx = 0; idx < len; idx++)
			{
				if (0 < rmax)
				{
					var nor = ((Re[idx] - rmin) / rmax);
					var bai = nor * (1.0 - offset);
					var ans = bai + offset;
					Re[idx] = ans;
					//Re[idx] = ((Re[idx] - rmin) / rmax) * (1.0 - offset) + offset;
				}
				if (0 < imax)
				{
					var nor = ((Im[idx] - imin) / imax);
					var bai = nor * (1.0 - offset);
					var ans = bai + offset;
					//Im[idx] = ((Im[idx] - imin) / imax) * (1.0 - offset) + offset;
				}
			}
		}

		public int[] ToPowerValue()
		{
			//正規化
			Normalize();

			//パワースペクトル化
			var len = Re.Length;
			var ret = new double[len];
			var dat = new int[len];
			for (var idx = 0; idx < len; idx++)
			{
				var r = Re[idx];
				var i = Im[idx];
				var rval = (0 == r) ? 0 : Math.Pow(r, 2);
				var ival = (0 == i) ? 0 : Math.Pow(i, 2);

				ret[idx] = Math.Sqrt(rval + ival);
			}

			//正規化
			Normalize(ret);

			for (var idx = 0; idx < len; idx++)
			{
				dat[idx] = (int)(100.0 * ret[idx]);
			}

			//結果を返す
			return dat;
		}

		void Normalize(double[] self)
		{
			var len = self.Length;
			var min = self.Min();
			var max = self.Max() - min;
			if (0 == max) return;
			for (var idx = 0; idx < len; idx++)
			{
				self[idx] = (self[idx] - min) / max;
			}
		}

		public void DFT(int sampleHz, out MValue output)
		{
			var n = Re.Length;
			output = new MValue(n);
			var pi2 = Math.PI * 2.0;

			for (int i = 0; i < n; i++)
			{
				output.Re[i] = 0.0;
				output.Im[i] = 0.0;

				var bosu = (i + 1) * sampleHz;
				for (int j = 0; j < n; j++)
				{
					var value = Re[j];
					if (0.0 == value) continue;
					double iso = ((double)(j % bosu) / (double)bosu);
					var cos = 0.0;
					var sin = 1.0;
					if (0.0 == iso)
					{
						cos = 1.0;
						sin = 0.0;
					}
					else if (0.25 == iso)
					{
						cos = 0.0;
						sin = 1.0;
					}
					else if (0.5 == iso)
					{
						cos = -1.0;
						sin = 0.0;
					}
					else if (0.75 == iso)
					{
						cos = 0.0;
						sin = -1.0;
					}
					else
					{
						var theta = pi2 * iso;
						cos = Math.Cos(theta);
						sin = Math.Sin(theta);
					}
					output.Re[i] += value * cos;
					output.Im[i] -= value * sin;
				}
			}
		}

		/// <summary>
		/// 1次元FFT
		/// </summary>
		public void FFT(out MValue output)
		{
			int dataSize = 1 << LogN;
			int[] reverseBitArray = BitScrollArray(dataSize);



			output = new MValue(dataSize);

			// バタフライ演算のための置き換え
			for (int i = 0; i < dataSize; i++)
			{
				output.Re[i] = Re[reverseBitArray[i]];
				output.Im[i] = Im[reverseBitArray[i]];
			}



			// バタフライ演算
			for (int stage = 1; stage <= LogN; stage++)
			{
				int butterflyDistance = 1 << stage;
				int numType = butterflyDistance >> 1;
				int butterflySize = butterflyDistance >> 1;

				double wRe = 1.0;
				double wIm = 0.0;
				double uRe = +Math.Cos(Math.PI / butterflySize);
				double uIm = -Math.Sin(Math.PI / butterflySize);

				for (int type = 0; type < numType; type++)
				{
					for (int j = type; j < dataSize; j += butterflyDistance)
					{
						int jp = j + butterflySize;
						double tempRe = output.Re[jp] * wRe - output.Im[jp] * wIm;
						double tempIm = output.Re[jp] * wIm + output.Im[jp] * wRe;
						output.Re[jp] = output.Re[j] - tempRe;
						output.Im[jp] = output.Im[j] - tempIm;
						output.Re[j] += tempRe;
						output.Im[j] += tempIm;
					}
					double tempWRe = wRe * uRe - wIm * uIm;
					double tempWIm = wRe * uIm + wIm * uRe;
					wRe = tempWRe;
					wIm = tempWIm;
				}
			}
		}

		/// <summary>
		/// 1次元IFFT
		/// </summary>
		public void IFFT(out MValue output)
		{
			int dataSize = 1 << LogN;
			output = new MValue(dataSize);



			for (int i = 0; i < dataSize; i++)
			{
				Im[i] = -Im[i];
			}
			FFT(out output);
			for (int i = 0; i < dataSize; i++)
			{
				output.Re[i] /= (double)dataSize;
				output.Im[i] /= (double)(-dataSize);
			}
		}



		// ビットを左右反転した配列を返す
		static int[] BitScrollArray(int arraySize)
		{
			int[] reBitArray = new int[arraySize];
			int arraySizeHarf = arraySize >> 1;

			reBitArray[0] = 0;
			for (int i = 1; i < arraySize; i <<= 1)
			{
				for (int j = 0; j < i; j++)
					reBitArray[j + i] = reBitArray[j] + arraySizeHarf;
				arraySizeHarf >>= 1;
			}
			return reBitArray;
		}
	}
}


	/*
	public class data
	{
		static void Main(string[] args)
		{
			// 画像読み込み
			byte[,] loadImage = LoadByteImage("input.bmp");


			// 2次元フーリエ変換を行う
			byte[,] filterdata_2D = FrequencyFiltering(loadImage);
			// 画像出力r
			SaveByteImage(filterdata_2D, "output.bmp");

		}

		/// <summary>
		/// Bitmapをロードしbyte[,]配列で返す
		/// </summary>
		static byte[,] LoadByteImage(string filename)
		{
			try
			{
				Bitmap bitmap = new Bitmap(filename);
				byte[,] data = new byte[bitmap.Width, bitmap.Height];



				// bitmapクラスの画像ピクセル値を配列に挿入
				for (int i = 0; i < bitmap.Height; i++)
				{
					for (int j = 0; j < bitmap.Width; j++)
					{
						// ここではグレイスケールに変換して格納
						data[j, i] =
							(byte)(
								(bitmap.GetPixel(j, i).R +
								bitmap.GetPixel(j, i).B +
								bitmap.GetPixel(j, i).G) / 3
								);
					}
				}
				return data;
			}
			catch
			{
				Console.WriteLine("ファイルが読めません。");
				return null;
			}
		}

		/// <summary>
		/// 画像配列をbmpに書き出す
		/// </summary>
		static void SaveByteImage(byte[,] data, string filename)
		{
			try
			{
				// 縦横サイズを配列から読み取り
				int xsize = data.GetLength(0);
				int ysize = data.GetLength(1);

				Bitmap bitmap = new Bitmap(xsize, ysize);

				// bitmapクラスのSetPixelでbitmapオブジェクトに
				// ピクセル値をセット
				for (int i = 0; i < ysize; i++)
				{
					for (int j = 0; j < xsize; j++)
					{
						bitmap.SetPixel(
							j,
							i,
							Color.FromArgb(
								data[j, i],
								data[j, i],
								data[j, i])
							);
					}
				}



				// 画像の保存
				bitmap.Save(filename, ImageFormat.Bmp);
			}
			catch
			{
				Console.WriteLine("ファイルが書き込めません。");
			}
		}



		/// <summary>
		/// 周波数フィルタリング
		/// </summary>
		public static byte[,] FrequencyFiltering(byte[,] image)
		{
			try
			{
				// サイズ取得
				int xSize = image.GetLength(0);
				int ySize = image.GetLength(1);



				// double型配列にする
				double[,] data = ByteToDouble2D(image);



				double[,] re;
				double[,] im = new double[xSize, ySize];



				// 2次元フーリエ変換
				FFT2D(data, im, out re, out im, xSize, ySize);



				// 2次元逆フーリエ変換　周波数画像からもとの空間画像に戻す
				IFFT2D(re, im, out data, out im, xSize, ySize);



				// byte型配列に戻す
				image = DoubleToByte2D(re);



				return image;
			}
			catch
			{
				return null;
			}
		}



		/// <summary>
		/// byte型2次元配列からdouble型2次元配列に変換
		/// </summary>
		public static double[,] ByteToDouble2D(byte[,] data)
		{
			try
			{
				// サイズ取得
				int xSize = data.GetLength(0);
				int ySize = data.GetLength(1);



				double[,] convdata = new double[xSize, ySize];
				for (int i = 0; i < ySize; i++)
				{
					for (int j = 0; j < xSize; j++)
					{
						convdata[j, i] = (double)data[j, i];
					}
				}
				return convdata;
			}
			catch
			{
				return null;
			}
		}



		/// <summary>
		/// byte型2次元配列からdouble型2次元配列に変換
		/// </summary>
		public static byte[,] DoubleToByte2D(double[,] data)
		{
			try
			{
				// サイズ取得
				int xSize = data.GetLength(0);
				int ySize = data.GetLength(1);


				byte[,] convdata = new byte[xSize, ySize];
				for (int i = 0; i < ySize; i++)
				{
					for (int j = 0; j < xSize; j++)
					{
						if (data[j, i] >= 255)
						{
							convdata[j, i] = 255;
						}
						else if (data[j, i] < 0)
						{
							convdata[j, i] = 0;
						}
						else
						{
							convdata[j, i] = (byte)data[j, i];
						}
					}
				}
				return convdata;
			}
			catch
			{
				return null;
			}
		}



		/// <summary>
		/// 1次元FFT
		/// </summary>
		public static void FFT(
			double[] inputRe,
			double[] inputIm,
			out double[] outputRe,
			out double[] outputIm,
			int bitSize)
		{
			int dataSize = 1 << bitSize;
			int[] reverseBitArray = BitScrollArray(dataSize);



			outputRe = new double[dataSize];
			outputIm = new double[dataSize];



			// バタフライ演算のための置き換え
			for (int i = 0; i < dataSize; i++)
			{
				outputRe[i] = inputRe[reverseBitArray[i]];
				outputIm[i] = inputIm[reverseBitArray[i]];
			}



			// バタフライ演算
			for (int stage = 1; stage <= bitSize; stage++)
			{
				int butterflyDistance = 1 << stage;
				int numType = butterflyDistance >> 1;
				int butterflySize = butterflyDistance >> 1;



				double wRe = 1.0;
				double wIm = 0.0;
				double uRe =
					System.Math.Cos(System.Math.PI / butterflySize);
				double uIm =
					-System.Math.Sin(System.Math.PI / butterflySize);



				for (int type = 0; type < numType; type++)
				{
					for (int j = type; j < dataSize; j += butterflyDistance)
					{
						int jp = j + butterflySize;
						double tempRe =
							outputRe[jp] * wRe - outputIm[jp] * wIm;
						double tempIm =
							outputRe[jp] * wIm + outputIm[jp] * wRe;
						outputRe[jp] = outputRe[j] - tempRe;
						outputIm[jp] = outputIm[j] - tempIm;
						outputRe[j] += tempRe;
						outputIm[j] += tempIm;
					}
					double tempWRe = wRe * uRe - wIm * uIm;
					double tempWIm = wRe * uIm + wIm * uRe;
					wRe = tempWRe;
					wIm = tempWIm;
				}
			}
		}



		/// <summary>
		/// 1次元IFFT
		/// </summary>
		public static void IFFT(
			double[] inputRe,
			double[] inputIm,
			out double[] outputRe,
			out double[] outputIm,
			int bitSize)
		{
			int dataSize = 1 << bitSize;
			outputRe = new double[dataSize];
			outputIm = new double[dataSize];



			for (int i = 0; i < dataSize; i++)
			{
				inputIm[i] = -inputIm[i];
			}
			FFT(inputRe, inputIm, out outputRe, out outputIm, bitSize);
			for (int i = 0; i < dataSize; i++)
			{
				outputRe[i] /= (double)dataSize;
				outputIm[i] /= (double)(-dataSize);
			}
		}



		// ビットを左右反転した配列を返す
		private static int[] BitScrollArray(int arraySize)
		{
			int[] reBitArray = new int[arraySize];
			int arraySizeHarf = arraySize >> 1;



			reBitArray[0] = 0;
			for (int i = 1; i < arraySize; i <<= 1)
			{
				for (int j = 0; j < i; j++)
					reBitArray[j + i] = reBitArray[j] + arraySizeHarf;
				arraySizeHarf >>= 1;
			}
			return reBitArray;
		}



		/// <summary>
		/// 2次元FFT
		/// </summary>
		/// <param name="inDataR">実数入力部</param>
		/// <param name="inDataI">虚数入力部</param>
		/// <param name="outDataR">実数出力部</param>
		/// <param name="outDataI">虚数出力部</param>
		/// <param name="xSize">x方向サイズ</param>
		/// <param name="ySize">y方向サイズ</param>
		public static void FFT2D(double[,] inDataRe, double[,] inDataIm, out double[,] outDataRe, out double[,] outDataIm, int xSize, int ySize)
		{
			double[,] tempRe = new double[ySize, xSize];
			double[,] tempIm = new double[ySize, xSize];



			int xbit = GetBitNum(xSize);
			int ybit = GetBitNum(ySize);



			outDataRe = new double[xSize, ySize];
			outDataIm = new double[xSize, ySize];



			for (int j = 0; j < ySize; j++)
			{
				double[] re = new double[xSize];
				double[] im = new double[xSize];
				FFT(
					GetArray(inDataRe, j),
					GetArray(inDataIm, j),
					out re, out im, xbit);



				for (int i = 0; i < xSize; i++)
				{
					tempRe[j, i] = re[i];
					tempIm[j, i] = im[i];
				}
			}



			for (int i = 0; i < xSize; i++)
			{
				double[] re = new double[ySize];
				double[] im = new double[ySize];
				FFT(
					GetArray(tempRe, i),
					GetArray(tempIm, i),
					out re, out im, ybit);



				for (int j = 0; j < ySize; j++)
				{
					outDataRe[i, j] = re[j];
					outDataIm[i, j] = im[j];
				}
			}
		}



		// ビット数取得
		private static int GetBitNum(int num)
		{
			int bit = -1;
			while (num > 0)
			{
				num >>= 1;
				bit++;
			}
			return bit;
		}



		// 1次元配列取り出し
		private static double[] GetArray(double[,] data2D, int seg)
		{
			double[] reData = new double[data2D.GetLength(0)];
			for (int i = 0; i < data2D.GetLength(0); i++)
			{
				reData[i] = data2D[i, seg];
			}
			return reData;
		}



		/// <summary>
		/// 2次元IFFT
		/// </summary>
		/// <param name="inDataR">実数入力部</param>
		/// <param name="inDataI">虚数入力部</param>
		/// <param name="outDataR">実数出力部</param>
		/// <param name="outDataI">虚数出力部</param>
		/// <param name="xSize">x方向サイズ</param>
		/// <param name="ySize">y方向サイズ</param>
		public static void IFFT2D(double[,] inDataRe, double[,] inDataIm, out double[,] outDataRe,
			out double[,] outDataIm,
			int xSize,

			int ySize)
		{

			double[,] tempRe = new double[ySize, xSize];
			double[,] tempIm = new double[ySize, xSize];



			int xbit = GetBitNum(xSize);
			int ybit = GetBitNum(ySize);


			outDataRe = new double[xSize, ySize];
			outDataIm = new double[xSize, ySize];



			for (int j = 0; j < ySize; j++)
			{
				double[] re = new double[xSize];
				double[] im = new double[xSize];
				IFFT(
					GetArray(inDataRe, j),
					GetArray(inDataIm, j),
					out re, out im, xbit);

				for (int i = 0; i < xSize; i++)
				{
					tempRe[j, i] = re[i];
					tempIm[j, i] = im[i];
				}
			}



			for (int i = 0; i < xSize; i++)
			{
				double[] re = new double[ySize];
				double[] im = new double[ySize];
				IFFT(
					GetArray(tempRe, i),
					GetArray(tempIm, i),
					out re, out im, ybit);


				for (int j = 0; j < ySize; j++)
				{
					outDataRe[i, j] = re[j];
					outDataIm[i, j] = im[j];
				}
			}
		}
	}
	*/
