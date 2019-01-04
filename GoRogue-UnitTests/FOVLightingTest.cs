﻿using GoRogue;
using GoRogue.MapViews;
using GoRogue.MapGeneration;
using GoRogue.SenseMapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;

namespace GoRogue_UnitTests
{
	[TestClass]
	public class FOVLightingTest
	{
		[TestMethod]
		public void CircleRadius()
		{
			var testResMap = new EmptyResMap(17, 17);
			var myFov = new FOV(testResMap);
			var myLighting = new SenseMap(testResMap);
			// Circle at 8, 8; radius 7

			myLighting.AddSenseSource(new SenseSource(SourceType.SHADOW, Coord.Get(8, 8), 7, Radius.CIRCLE));
			myFov.Calculate(8, 8, 7, Radius.CIRCLE);
			myLighting.Calculate();
			for (int x = 0; x < testResMap.Width; x++)
			{
				for (int y = 0; y < testResMap.Height; y++)
				{
					Console.Write(myLighting[x, y].ToString("0.00") + " ");
					Assert.AreEqual(myFov[x, y], myLighting[x, y]); // Both got the same results
				}
				Console.WriteLine();
			}
		}

		[TestMethod]
		public void EqualLargeMap()
		{
			var testResMap = new TestResMap(17, 17);
			testResMap[8, 8] = 0.0; // Make sure start is free
			var myFov = new FOV(testResMap);
			var myLighting = new SenseMap(testResMap);
			// Circle at 8, 8; radius 7
			myLighting.AddSenseSource(new SenseSource(SourceType.SHADOW, Coord.Get(8, 8), 7, Radius.CIRCLE));
			myFov.Calculate(8, 8, 7, Radius.CIRCLE);
			myLighting.Calculate();
			Console.WriteLine("LOS: ");
			for (int x = 0; x < testResMap.Width; x++)
			{
				for (int y = 0; y < testResMap.Height; y++)
				{
					Console.Write($"{myFov[x, y].ToString("N2")}\t");
				}
				Console.WriteLine();
			}
			Console.WriteLine("\nLighting:");
			for (int x = 0; x < testResMap.Width; x++)
			{
				for (int y = 0; y < testResMap.Height; y++)
				{
					Console.Write($"{myLighting[x, y].ToString("N2")}\t");
				}
				Console.WriteLine();
			}
			for (int x = 0; x < testResMap.Width; x++)
				for (int y = 0; y < testResMap.Height; y++)
				{
					System.Console.WriteLine($"We have ({x},{y}) fov {myFov[x, y]}, lighting {myLighting[Coord.Get(x, y)]}");
					Assert.AreEqual(myFov[x, y], myLighting[x, y]); // Both got the same results
				}
		}

		[TestMethod]
		public void FOVCurrentHash()
		{
			var map = new BoxResMap(50, 50);
			var fov = new FOV(map);

			fov.Calculate(20, 20, 10);

			// Inefficient copy but fine for testing
			HashSet<Coord> currentFov = new HashSet<Coord>(fov.CurrentFOV);

			for (int x = 0; x < map.Width; x++)
				for (int y = 0; y < map.Height; y++)
				{
					if (fov[x, y] > 0.0)
						Assert.AreEqual(true, currentFov.Contains(Coord.Get(x, y)));
					else
						Assert.AreEqual(false, currentFov.Contains(Coord.Get(x, y)));
				}
		}

		[TestMethod]
		public void FOVNewlySeenUnseen()
		{
			var map = new BoxResMap(50, 50);
			var fov = new FOV(map);

			fov.Calculate(20, 20, 10, Radius.SQUARE);
			var prevFov = new HashSet<Coord>(fov.CurrentFOV);

			fov.Calculate(19, 19, 10, Radius.SQUARE);
			var curFov = new HashSet<Coord>(fov.CurrentFOV);
			var newlySeen = new HashSet<Coord>(fov.NewlySeen);
			var newlyUnseen = new HashSet<Coord>(fov.NewlyUnseen);

			foreach (var pos in prevFov)
			{
				if (!curFov.Contains(pos))
					Assert.AreEqual(true, newlyUnseen.Contains(pos));
				else
					Assert.AreEqual(false, newlyUnseen.Contains(pos));
			}

			foreach (var pos in curFov)
			{
				if (!prevFov.Contains(pos))
					Assert.AreEqual(true, newlySeen.Contains(pos));
				else
					Assert.AreEqual(false, newlySeen.Contains(pos));
			}
		}

		[TestMethod]
		public void RippleCircleValue()
		{
			int MAP_SIZE = 30;
			int RADIUS = 10;
			Radius RAD_TYPE = Radius.CIRCLE;
			Coord SOURCE_POS = Coord.Get(15, 15);

			BoxResMap resMap = new BoxResMap(MAP_SIZE, MAP_SIZE);
			SenseMap senseMap = new SenseMap(resMap);

			var source = new SenseSource(SourceType.RIPPLE, SOURCE_POS, RADIUS, RAD_TYPE);
			senseMap.AddSenseSource(source);

			senseMap.Calculate();

			Console.WriteLine("Map on 10x10, light source at (2, 3), 3 circle radius, using ripple is: ");
			for (int x = 0; x < MAP_SIZE; x++)
			{
				for (int y = 0; y < MAP_SIZE; y++)
				{
					Console.Write($"{senseMap[x, y].ToString("N4")} ");
				}
				Console.WriteLine();
			}
		}

		[TestMethod]
		public void SenseMapCurrentHash()
		{
			var map = new BoxResMap(50, 50);
			var senseMap = new SenseMap(map);
			senseMap.AddSenseSource(new SenseSource(SourceType.RIPPLE, Coord.Get(20, 20), 10, Radius.CIRCLE));

			senseMap.Calculate();

			// Inefficient copy but fine for testing
			HashSet<Coord> currentSenseMap = new HashSet<Coord>(senseMap.CurrentSenseMap);

			for (int x = 0; x < map.Width; x++)
				for (int y = 0; y < map.Height; y++)
				{
					if (senseMap[x, y] > 0.0)
						Assert.AreEqual(true, currentSenseMap.Contains(Coord.Get(x, y)));
					else
						Assert.AreEqual(false, currentSenseMap.Contains(Coord.Get(x, y)));
				}

		}

		[TestMethod]
		public void FOVSenseMapEquivalency()
		{
			ArrayMap<bool> map = new ArrayMap<bool>(100, 100);
			QuickGenerators.GenerateRectangleMap(map);

			var positions = Enumerable.Range(0, 100).Select(x => map.RandomPosition(true)).ToList();

			// Make 2-layer thick walls to verify wall-lighting is working properly
			foreach (var pos in map.Positions())
			{
				if (pos.X == 1 || pos.Y == 1 || pos.X == map.Width - 2 || pos.Y == map.Height - 2)
					map[pos] = false;
			}

			var fov = new FOV(map);
			var senseMap = new SenseMap(new LambdaTranslationMap<bool, double>(map, x => x ? 0.0 : 1.0));
			var senseSource = new SenseSource(SourceType.SHADOW, map.RandomPosition(true), 5, Distance.EUCLIDEAN);
			senseMap.AddSenseSource(senseSource);


			foreach (var curPos in positions)
			{
				if (!map[curPos])
					continue;

				senseSource.Position = curPos;
				fov.Calculate(senseSource.Position, senseSource.Radius, senseSource.DistanceCalc);
				senseMap.Calculate();

				foreach (var pos in map.Positions())
				{
					bool success = fov.BooleanFOV[pos] == (senseMap[pos] > 0.0 ? true : false);

					if (!success)
					{
						Console.WriteLine($"Failed on pos {pos} with source at {senseSource.Position}.");
						Console.WriteLine($"FOV: {fov[pos]}, SenseMap: {senseMap[pos]}");
						Console.WriteLine($"Distance between source and fail point: {Distance.EUCLIDEAN.Calculate(senseSource.Position, pos)}, source radius: {senseSource.Radius}");
					}

					Assert.AreEqual(true, success);
				}
			}


			var degreesList = Enumerable.Range(0, 360).ToList();
			degreesList.FisherYatesShuffle();
			var spanList = Enumerable.Range(1, 359).ToList();
			spanList.FisherYatesShuffle();

			var degrees = degreesList.Take(30).ToList();
			var spans = spanList.Take(30).ToList();

			senseSource.IsAngleRestricted = true;
			// Test angle-based shadowcasting
			foreach (var curPos in positions.Take(1))
			{
				if (!map[curPos])
					continue;

				foreach (var degree in degrees)
				{
					foreach (var span in spans)
					{
						senseSource.Angle = degree;
						senseSource.Span = span;

						senseSource.Position = curPos;
						fov.Calculate(senseSource.Position, senseSource.Radius, senseSource.DistanceCalc, senseSource.Angle, senseSource.Span);
						senseMap.Calculate();

						foreach (var pos in map.Positions())
						{
							bool success = fov.BooleanFOV[pos] == (senseMap[pos] > 0.0 ? true : false);

							if (!success)
							{
								Console.WriteLine($"Failed on pos {pos} with source at {senseSource.Position}, angle: {senseSource.Angle}, span: {senseSource.Span}.");
								Console.WriteLine($"FOV: {fov[pos]}, SenseMap: {senseMap[pos]}");
								Console.WriteLine($"Distance between source and fail point: {Distance.EUCLIDEAN.Calculate(senseSource.Position, pos)}, source radius: {senseSource.Radius}");
							}

							Assert.AreEqual(true, success);
						}
					}
				}
			}
		}

		[TestMethod]
		public void FOVBooleanInput()
		{
			var map = new ArrayMap<bool>(10, 10);
			QuickGenerators.GenerateRectangleMap(map);

			var identicalResMap = new BoxResMap(10, 10);

			var fov = new FOV(map);
			var fovDouble = new FOV(identicalResMap);

			fov.Calculate(5, 5, 3);
			fovDouble.Calculate(5, 5, 3);

			foreach (var pos in fov.Positions())
				Assert.AreEqual(fov[pos], fovDouble[pos]);
		}

		[TestMethod]
		public void FOVBooleanOutput()
		{
			var map = new ArrayMap<bool>(10, 10);
			QuickGenerators.GenerateRectangleMap(map);

			var fov = new FOV(map);
			fov.Calculate(5, 5, 3);

			Console.WriteLine("FOV for reference:");
			Console.WriteLine(fov.ToString(2));

			foreach (var pos in fov.Positions())
			{
				bool inFOV = fov[pos] != 0.0;
				Assert.AreEqual(inFOV, fov.BooleanFOV[pos]);
			}
		}
	}

	internal class BoxResMap : ArrayMap<double>
	{
		public BoxResMap(int width, int height)
			: base(width, height)
		{
			for (int x = 0; x < width; x++)
				for (int y = 0; y < height; y++)
				{
					if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
						this[x, y] = 1.0;
					else
						this[x, y] = 0.0;
				}
		}
	}

	internal class EmptyResMap : IMapView<double>
	{
		public EmptyResMap(int width, int height)
		{
			Width = width;
			Height = height;
		}

		public int Height { get; private set; }
		public int Width { get; private set; }
		public double this[int x, int y] { get => 0.0; }
		public double this[Coord c] { get => 0.0; }
	}

	internal class TestResMap : ArrayMap<double>
	{
		public TestResMap(int width, int height)
			: base(width, height)
		{
			Random rng = new Random();
			for (int x = 0; x < width; x++)
				for (int y = 0; y < height; y++)
				{
					this[x, y] = rng.NextDouble();
				}
		}
	}
}