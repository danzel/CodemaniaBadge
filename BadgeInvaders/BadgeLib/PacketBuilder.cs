using System;
using System.Collections.Generic;
using System.Drawing;
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
		/// <param name="speed"></param>
		/// <param name="unknown">0x31</param>
		/// <param name="scrollMode"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public static List<byte> GenerateTextPacket(byte address0, int messageIndex, Speed speed, byte unknown, ScrollMode scrollMode, string text)
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
		/// 
		/// </summary>
		/// <param name="address0">0x31</param>
		/// <param name="messageIndex">0-1</param>
		/// <param name="speed"></param>
		/// <param name="unknown">0x31</param>
		/// <param name="scrollMode"></param>
		/// <param name="bitmap">A 12 height monochrome image (currently only up to 12px wide are supported)</param>
		/// <returns></returns>
		public static List<byte> GenerateImagePacket(byte address0, int messageIndex, Speed speed, byte unknown, ScrollMode scrollMode, Bitmap bitmap)
		{
			List<byte> result = new List<byte>();


			List<byte> buffer = new List<byte>();
			//Scroll mode etc
			buffer.Add((byte)speed);
			buffer.Add(unknown);
			buffer.Add((byte)scrollMode);

			//Say how wide the image is in blocks of 12 pixels
			int bitmap12PxCount = 1 + (bitmap.Width - 1) / 12;

			buffer.Add((byte)bitmap12PxCount); //Length byte (how many image banks below will be mentioned

			for (int i = 0; i < bitmap12PxCount; i++)
			{
				buffer.Add(0x80); //Image
				buffer.Add((byte)i); //Index
			}


			//Pad the buffer out to be 4 packets long (Desktop programmer does this, but it doesn't appear to be required)
			//while (buffer.Count < 4 * 64)
			//	buffer.Add(0);

			result.AddRange(GenerateMultiplePackets(address0, messageIndex, buffer));


			//Next packets (image) are special, messageIndex 8!
			buffer.Clear();

			buffer.AddRange(PackBitmapToBytes(bitmap));

			result.AddRange(GenerateMultiplePackets(address0, 8, buffer));

			return result;
		}

		private static List<byte> PackBitmapToBytes(Bitmap bitmap)
		{
			List<byte> buffer = new List<byte>();

			//Pack 16 pixels from each row into 2 bytes
			for (int row = 0; row < 12; row++)
			{
				for (int c = 0; c < 2; c++)
				{
					byte b =
						(byte)(
							((c * 8 + 0 < bitmap.Width && (int)bitmap.GetPixel(c * 8 + 0, row).GetBrightness() == 0) ? 0x80 : 0x00) |
							((c * 8 + 1 < bitmap.Width && (int)bitmap.GetPixel(c * 8 + 1, row).GetBrightness() == 0) ? 0x40 : 0x00) |
							((c * 8 + 2 < bitmap.Width && (int)bitmap.GetPixel(c * 8 + 2, row).GetBrightness() == 0) ? 0x20 : 0x00) |
							((c * 8 + 3 < bitmap.Width && (int)bitmap.GetPixel(c * 8 + 3, row).GetBrightness() == 0) ? 0x10 : 0x00) |
							((c * 8 + 4 < bitmap.Width && (int)bitmap.GetPixel(c * 8 + 4, row).GetBrightness() == 0) ? 0x08 : 0x00) |
							((c * 8 + 5 < bitmap.Width && (int)bitmap.GetPixel(c * 8 + 5, row).GetBrightness() == 0) ? 0x04 : 0x00) |
							((c * 8 + 6 < bitmap.Width && (int)bitmap.GetPixel(c * 8 + 6, row).GetBrightness() == 0) ? 0x02 : 0x00) |
							((c * 8 + 7 < bitmap.Width && (int)bitmap.GetPixel(c * 8 + 7, row).GetBrightness() == 0) ? 0x01 : 0x00)
					);
					buffer.Add(b);
				}
			}

			//Now throw in 12 rows of hacked pixels, this lets us get in up to 16 pixels above. Not sure wtf these are meant to do...
			for (int row = 0; row < 12; row++)
			{
				int start = buffer.Count - 24;

				buffer.Add((byte) (buffer[start + 1] << 4));
				buffer.Add(0);
			}

			//Next lot of 12 pixels has something special
			//4 bits of unknown ??? (copy of last 4 bits of last byte)
			//Pack 16 pixels from each row into 2 bytes (offset by half a byte)


			return buffer;
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
			int packetsToBuild = 1 + (bytes.Count - 1) / DataBytesInPacket;

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
