using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Linq;
using System.Text;

namespace SerialHax
{
	class MainClass
	{
		static SerialPort _port;
		
		public static void Main (string[] args)
		{
			Thread updateUiThread = new Thread(UiThread);
			updateUiThread.Start();
			
			while (true)
			{
				var key = Console.ReadKey();
				
				if (key.Key == ConsoleKey.LeftArrow)
				{
					Console.WriteLine("LEFT!");
					wantToLeft = true;
				}
				else if (key.Key == ConsoleKey.RightArrow)
				{
					Console.WriteLine("RIGHT!");
					wantToRight  =true;
				}
				else if (key.Key == ConsoleKey.Escape)
				{
					Environment.Exit(0);
				}
				else if (key.Key == ConsoleKey.Spacebar)
				{
					wantToShoot = true;	
				}
				
				Console.WriteLine("Pressed " + key.Key);
			}
		}
		
		public static bool[] enemy = new bool[] { true, true, true, true };
		public static bool[] enemyJustDie = new bool[] { false, false, false, false };
		
		static bool wantToShoot = false, wantToRight = false, wantToLeft = false;
		
		static bool playerShotAlive =false;
		static int playerShotX, playerShotY;
		
		static bool enemyShotAlive =false;
		static int enemyShotX, enemyShotY;
		
		static Random random = new Random();
		
		static int playerX = 0;
		
		static bool playerWon = false;
		static bool playerLose = false;
		
		public static void RunSimulation()
		{
			//Actual sim here
			
			if ((playerWon || playerLose) && wantToShoot)
			{
				playerWon = false;
				playerLose = false;
				playerShotAlive = false;
				enemyShotAlive = false;
				wantToShoot = false;
				
				for (int i = 0; i < 4; i++){
					enemy[i] = true;
					enemyJustDie[i] = false;
				}
			}
			
			if (!enemy.Any(x => x))
			{
				playerWon = true;
			}
			
			for (int i =0; i < 4; i++)
				enemyJustDie[i] = false;
			
			if (playerShotAlive)
			{
				playerShotY--;
				if (playerShotY == 0 && enemy[playerShotX])
				{
					enemy[playerShotX] = false;
					enemyJustDie[playerShotX] = true;
					playerShotAlive = false;
				}
				else if (playerShotY < 0)
				{
					playerShotAlive = false;
				}
			}
			if (enemyShotAlive)
			{
				enemyShotY++;
				
				if (enemyShotY > 3)
					enemyShotAlive = false;
			}
			
			
			
			
			if (wantToShoot && !playerShotAlive)
			{
				playerShotAlive = true;
				playerShotX = playerX;
				playerShotY = 2;
			}
			if (wantToLeft)
				playerX = Math.Max(0, playerX - 1);
			if (wantToRight)
				playerX = Math.Min (3, playerX + 1);
			
					
			if (enemyShotAlive && enemyShotY == 3 && playerX == enemyShotX)
			{
				playerLose = true;
				return;
			}

			
			if (!enemyShotAlive)
			{
				enemyShotAlive = true;
				enemyShotX = random.Next (4);
				enemyShotY = 1;
			}

			
			wantToLeft = false;
			wantToRight = false;
			wantToShoot = false;
		}
		
		public static void UiThread()
		{
			while (true)
			{
				string[] buffers = new string[4];
				
				if (playerWon)
				{
					for (int i = 0; i < 4; i++)
						buffers[i] = "WIN!";
				}
				else if (playerLose)
				{
					for (int i = 0; i < 4; i++)
						buffers[i] = i % 2 == 0 ? "FAIL" : "LOSE";
				}
				else
				{
					//Enemies line
					for (int i = 0; i < 4; i++)
						buffers[0] += enemy[i] ? "@" : enemyJustDie[i] ? "X" : " ";
					
					//Shot lines
					buffers[1] = "    ";
					buffers[2] = "    ";
				
					
					
					///Player line
					for (int i = 0; i < playerX; i++)
						buffers[3] += " ";
					buffers[3] += "A";
					while (buffers[3].Length<4)
						buffers[3] += " ";
					
					//Add on shots
						if (playerShotAlive && playerShotY < 4 && playerShotY >= 0)
					{
						buffers[playerShotY] = buffers[playerShotY].Substring(0, playerShotX) + "!" + buffers[playerShotY].Substring(playerShotX + 1);
					}
					if (enemyShotAlive && enemyShotY >= 0 && enemyShotY < 4)
					{
						buffers[enemyShotY] = buffers[enemyShotY].Substring(0, enemyShotX) + "i" + buffers[enemyShotY].Substring(enemyShotX + 1);
					}
				}
				
				//Hack in calls to print all lines to your badges
				SendTextOnPort(4, buffers[0]);
				SendTextOnPort(5, buffers[1]);
				SendTextOnPort(6, buffers[2]);
				SendTextOnPort(7, buffers[3]);
				
				Console.Clear();
				for (int i = 0; i < 4; i++)
					Console.WriteLine(buffers[i]);
				Thread.Sleep (TimeSpan.FromSeconds(1));
				
				
				//Debug
				RunSimulation();
			}
		}
		
		public static void SendTextOnPort(int portNumber, string message)
		{
			//Checksum is (0x31 + filler + 64 bytes message) % 256
			byte[] test = new byte[] { 0x31, 0x06, 0, 0x35, 0x31, 0x42, 0x01, 0x61 };
			
			int s = test.Select(x => (int)x).Sum();
			int s2 = s % 256;
			
			
			using (var serial = new SerialPort("COM" + portNumber, 38400, Parity.None, 8, StopBits.One))
			{
				_port = serial;
				serial.Open();
				
				SendBytes(new byte[] { 0 }); //init byte
				
				SendText(message);
				SendBytes (new byte[] { 2, 0x33, 1 });
				return;
			}
		}
	
		public static void SendText(string msg)
		{
			SendBytes(new byte[] { 2 }); //send bytes message
			
			List<byte> toSend = new List<byte>();
			toSend.AddRange(new byte[] { 0x31, 0x06, 0x00 });
			toSend.Add (0x35); //Speed
			toSend.Add (0x31); //msg speed
			toSend.Add (0x41);//0x42); //scroll mode
			toSend.Add ((byte)msg.Length); //length
			
			var msgBytes = Encoding.ASCII.GetBytes(msg);
			
			toSend.AddRange(msgBytes);
			while (toSend.Count< 64 + 3)
				toSend.Add(0);
			
			int crc = toSend.Select(x => (int)x).Sum() % 256;
			
			SendBytes(toSend.ToArray());
			SendBytes(new byte[] { (byte)crc });
			
		}
		
		public static void SendBytes(byte[] toSend)
		{
			_port.Write(toSend, 0, toSend.Length);	
		}
	}
}
