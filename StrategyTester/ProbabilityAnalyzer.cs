﻿using System;
using System.Collections.Generic;
using System.Linq;
using StrategyTester.Types;

namespace StrategyTester
{
	public class ProbabilityAnalyzer
	{
        public static Tuple<int, int> TesCandlesHightAlternation(List<Candle> candles, int continuationLength, int averageDaysLength)
        {
            var candlesHeights = new Queue<int>(candles.Take(averageDaysLength).Select(candle => candle.InnerHeigth));
            candles = candles.Skip(averageDaysLength).ToList();

            int currentHighCount = 0;
            int alternateCount = 0, continuationCount = 0;
            foreach (var candle in candles)
            {
                int currentHeight = candle.InnerHeigth;
                var averageHeight = candlesHeights.Average();

                if (currentHighCount >= continuationLength)
                {
                    if (currentHeight >= averageHeight)
                    {
                        continuationCount++;
                    }
                    else
                    {
                        alternateCount++;
                    }
                }

                if (currentHeight > averageHeight)
                {
                    currentHighCount++;
                }
                else
                {
                    currentHighCount = 0;
                }

                candlesHeights.Enqueue(currentHeight);
                candlesHeights.Dequeue();
            }

            return new Tuple<int, int>(alternateCount, continuationCount);
        }
		public static double TestTrendFromNCandle(List<Day> days, int candleNumber)
		{
			double successCount = 0;
			foreach (var day in days)
			{
				if (Math.Sign(day.FiveMins[candleNumber].Close - day.FiveMins[1].Open) == Math.Sign(day.Params.Close - day.FiveMins[candleNumber].Close))
				{
					++successCount;
				}
			}
			return successCount/days.Count;
		}

		public static Tuple<int, int> TestExtremumsContinuation(List<Candle> candles, int monotoneCount, bool isMinimums)
		{
			var finder = new ExtremumsFinder(0);
			var extremums = finder.FindFirstExtremums(candles, isMinimums);

			int successCount = 0, failCount = 0;

			int currentMonotoneCount = 0;
			for (int i = 1; i < extremums.Count; ++i)
			{
				//var currentSign = Math.Sign(extremums[i].Value - extremums[i - 1].Value);
				bool currentTrend = extremums[i].Value > extremums[i - 1].Value;
				if (currentTrend == isMinimums)
				{
					currentMonotoneCount++;
					if (currentMonotoneCount >= monotoneCount)
					{
						successCount++;
					}
				}
				else
				{
					if (currentMonotoneCount >= monotoneCount)
					{
						failCount++;
					}
					currentMonotoneCount = 0;
				}
			}
			return new Tuple<int, int>(successCount, failCount);
		}

		public static Tuple<int, int> TestExtremumsContinuationFromAngle(List<Candle> candles, int monotoneCount, double minAngle, bool isMinimums)
		{
			var finder = new ExtremumsFinder(0);
			var extremums = finder.FindFirstExtremums(candles, isMinimums);

			int successCount = 0, failCount = 0;

			int currentMonotoneCount = 0;
			for (int i = 1; i < extremums.Count; ++i)
			{
				//var currentSign = Math.Sign(extremums[i].Value - extremums[i - 1].Value);
				bool currentTrend = extremums[i].Value > extremums[i - 1].Value;
				if (currentTrend == isMinimums)
				{
					currentMonotoneCount++;
					if (currentMonotoneCount >= monotoneCount && Math.Abs(GetLineAngle(extremums, monotoneCount)) > minAngle)
					{
						successCount++;
					}
				}
				else
				{
					if (currentMonotoneCount >= monotoneCount && Math.Abs(GetLineAngle(extremums, monotoneCount)) > minAngle)
					{
						failCount++;
					}
					currentMonotoneCount = 0;
				}
			}
			return new Tuple<int, int>(successCount, failCount);
		}

		public static List<int> TestExtremumsContinuationLength(List<Candle> candles, int monotoneCount, bool isMinimums)
		{
			var finder = new ExtremumsFinder(0);
			var extremums = finder.FindFirstExtremums(candles, isMinimums);

			var lengths = new List<int>();

			int currentMonotoneCount = 0;
			for (int i = 1; i < extremums.Count; ++i)
			{
				bool currentTrend = extremums[i].Value > extremums[i - 1].Value;
				if (currentTrend == isMinimums)
				{
					currentMonotoneCount++;
				}
				else
				{
					currentMonotoneCount = 0;
				}

				if (currentMonotoneCount >= monotoneCount)
				{
					lengths.Add(extremums[i].Value - extremums[i-1].Value);
				}
			}
			return lengths;
		}

		private static double GetLineAngle(List<Extremum> extremums, int maxLineLength)
		{
			const int norm = 50;
			if (extremums.Count > maxLineLength)
			{
				extremums = extremums.Skip(extremums.Count - maxLineLength).ToList();
			}
			return Math.Atan(extremums.Average(ex => ex.Value - extremums.First().Value) / norm);
		}

	#region Old
		public static Tuple<int, int> TestTrendInvertion(List<Day> days, int length, int skippedCount)
		{
			int samelyCount = 0, invertedCount = 0;
			for (int i = 1; i < days.Count; ++i)
			{
				var current = TestTrendCandlesInvertion(length, days[i - 1].Params.IsLong, days[i].FiveMins.Skip(skippedCount));
				samelyCount += current.Item1;
				invertedCount += current.Item2;
			}

			return new Tuple<int, int>(samelyCount, invertedCount);
		}

		private static Tuple<int, int> TestTrendCandlesInvertion(int length, bool lastDayLong, IEnumerable<Candle> oneDayCandles)
		{
			int invertedCount = 0, samelyCount = 0;

			var candlesList = oneDayCandles as IList<Candle> ?? oneDayCandles.ToList();
			int startValue = candlesList.First().Open;

			int currentCount = 0;
			bool needLong = candlesList.First().IsLong;

			for (int i = 1; i < candlesList.Count; ++i)
			{
				var candle = candlesList[i];

				bool currentDayLong = candle.Open > startValue;
				//if (!lastDayLong && currentDayLong || lastDayLong && !currentDayLong)
				//	continue;

				if (candle.IsLong == needLong)
				{
					currentCount++;
				}
				else
				{
					currentCount = 1;
					needLong = !needLong;
				}

				if (candlesList[i - 1].IsLong != currentDayLong)
					continue;

				if (currentCount == length)
				{
					if (candle.IsLong == candlesList[i-1].IsLong)
					{
						samelyCount++;
						while (i < candlesList.Count && candlesList[i].IsLong == candlesList[i - 1].IsLong)
						{
							++i;
						}
					}
					else
					{
						invertedCount++;
					}

					currentCount = 0;
				}
			}

			return new Tuple<int, int>(samelyCount, invertedCount);
		}

		public static Tuple<int, int> TestCandlesInvertion(List<Day> days, int length)
		{
			int samelyCount = 0, invertedCount = 0;
			foreach (var day in days)
			{
				var current = TestCandlesInvertion2(length, day.FiveMins);
				samelyCount += current.Item1;
				invertedCount += current.Item2;
			}

			return new Tuple<int, int>(samelyCount, invertedCount);
		}

		private static Tuple<int, int> TestCandlesInvertion2(int countBeforeInvertion, List<Candle> dayCandles)
		{
			int invertedCount = 0, samelyCount = 0;
			int i = 1;

			while (i < dayCandles.Count)
			{
				int currentCount = 1;
				while (i < dayCandles.Count && dayCandles[i].IsLong == dayCandles[i - 1].IsLong)
				{
					++i;
					++currentCount;
				}
				++i;

				if (currentCount > countBeforeInvertion)
				{
					++samelyCount;
				}
				else if (currentCount == countBeforeInvertion)
				{
					++invertedCount;
				}
			}

			return new Tuple<int, int>(samelyCount, invertedCount);
		}

	#endregion
	}
}
