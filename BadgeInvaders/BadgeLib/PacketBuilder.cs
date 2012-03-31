using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BadgeLib
{
	public class PacketBuilder
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="address0">0x31</param>
		/// <param name="messageIndex">0+</param>
		/// <param name="address1">0x00, 0x40, 0x80</param>
		/// <param name="speed"></param>
		/// <param name="unknown">0x31</param>
		/// <param name="scrollMode"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public static List<byte> GenerateTextPacket(byte address0, int messageIndex, byte address1, Speed speed, byte unknown, ScrollMode scrollMode, string text)
		{
			List<byte> buffer = new List<byte>();
			buffer.Add((byte)speed);
			buffer.Add(unknown);
			buffer.Add((byte)scrollMode);

			buffer.Add((byte)text.Length);
			buffer.AddRange(text.Select(c => (byte)c));

			return GenerateMultiplePackets(address0, messageIndex, buffer);
		}

		/// <summary>
		/// Packs the given bytes accross however many packets are required and returns them
		/// </summary>
		/// <param name="address0">0x31</param>
		/// <param name="messageIndex">0+</param>
		/// <param name="bytes"></param>
		/// <returns></returns>
		private static List<byte> GenerateMultiplePackets(byte address0, int messageIndex, List<byte> bytes)
		{
			const int DataBytesInPacket = 64;

			List<byte> result = new List<byte>();
			int packetsToBuild = 1 + (bytes.Count / DataBytesInPacket);

			for (int i = 0; i < packetsToBuild; i++)
			{
				List<byte> buffer = new List<byte>();
				buffer.Add(0x02); //Start of message
				buffer.Add(address0);
				buffer.Add((byte)(0x06 + messageIndex));
				buffer.Add((byte) (i * 0x40));
				for (int b = i * DataBytesInPacket; b < bytes.Count && b < (i + 1) * DataBytesInPacket; b++)
				{
					buffer.Add(bytes[b]);
				}
				while (buffer.Count < 4 + DataBytesInPacket)
					buffer.Add(0);
				buffer.Add(CalculateCrc(buffer));

				result.AddRange(buffer);
			}
			return result;
		}

		/// <summary>
		/// Returns the crc for the last 67 bytes in the buffer
		/// </summary>
		private static byte CalculateCrc(List<byte> bytes)
		{
			return (byte)((bytes.Skip(bytes.Count - 67).Select(b => (int)b).Sum()) & 0xff);
		}

		public enum Speed : byte
		{
			Unknown = 0,

			S1 = (byte)'1',
			S2 = (byte)'2',
			S3 = (byte)'3',
			S4 = (byte)'4',
			S5 = (byte)'5'
		}

		public enum ScrollMode : byte
		{
			Unknown = 0,

			Hold = (byte)'A',
			Rotate = (byte)'B',
			Snow = (byte)'C',
			Flash = (byte)'D',
			HoldFrame = (byte)'E',
		}
	}
}
