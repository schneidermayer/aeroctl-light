﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AeroCtl.Native;

namespace AeroCtl
{
	/// <summary>
	/// Implements logic to talk with the USB HID device that controls keyboard LEDs.
	/// The chip appears to be called "ITE8298" according to its firmware blob.
	/// </summary>
	public class Ite829XRgbController : IRgbController
	{
		private readonly HidDevice device;
		private const int defaultWait = 1;
		
		/// <summary>
		/// The default key matrix for US and UK keyboards.
		/// </summary>
		public static ReadOnlyMemory<byte> DefaultMatrix { get; } = new byte[]
		{
			0x0E, 0x00, 0x04, 0x00, 0x0E, 0x00, 0x03, 0x00, 0x0E, 0x00, 0x02, 0x00, 0x0E, 0x00, 0x01, 0x00,
			0x0E, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x00, 0x00, 0x00, 0xE1, 0x00,
			0x00, 0x00, 0x39, 0x00, 0x00, 0x00, 0x2B, 0x00, 0x00, 0x00, 0x35, 0x00, 0x00, 0x00, 0x29, 0x00,
			0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x14, 0x00,
			0x00, 0x00, 0x1E, 0x00, 0x00, 0x00, 0x3A, 0x00, 0x00, 0x00, 0xE3, 0x00, 0x00, 0x00, 0x1D, 0x00,
			0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x1A, 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00, 0x3B, 0x00,
			0x00, 0x00, 0xE2, 0x00, 0x00, 0x00, 0x1B, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x08, 0x00,
			0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00, 0x91, 0x00, 0x00, 0x00, 0x06, 0x00,
			0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x15, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00, 0x3D, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x17, 0x00,
			0x00, 0x00, 0x22, 0x00, 0x00, 0x00, 0x3E, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x05, 0x00,
			0x00, 0x00, 0x0B, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x23, 0x00, 0x00, 0x00, 0x3F, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x0D, 0x00, 0x00, 0x00, 0x18, 0x00,
			0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x90, 0x00, 0x00, 0x00, 0x10, 0x00,
			0x00, 0x00, 0x0E, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x25, 0x00, 0x00, 0x00, 0x41, 0x00,
			0x00, 0x00, 0xE6, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x12, 0x00,
			0x00, 0x00, 0x26, 0x00, 0x00, 0x00, 0x42, 0x00, 0x00, 0x00, 0x65, 0x00, 0x00, 0x00, 0x37, 0x00,
			0x00, 0x00, 0x33, 0x00, 0x00, 0x00, 0x13, 0x00, 0x00, 0x00, 0x27, 0x00, 0x00, 0x00, 0x43, 0x00,
			0x00, 0x00, 0xE4, 0x00, 0x00, 0x00, 0x38, 0x00, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00, 0x2F, 0x00,
			0x00, 0x00, 0x2D, 0x00, 0x00, 0x00, 0x44, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x30, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x00, 0x00, 0x45, 0x00,
			0x00, 0x00, 0x50, 0x00, 0x00, 0x00, 0xE5, 0x00, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0x31, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x51, 0x00, 0x00, 0x00, 0x52, 0x00,
			0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x4C, 0x00,
			0x00, 0x00, 0x4F, 0x00, 0x00, 0x00, 0x59, 0x00, 0x00, 0x00, 0x5C, 0x00, 0x00, 0x00, 0x5F, 0x00,
			0x00, 0x00, 0x53, 0x00, 0x00, 0x00, 0x4A, 0x00, 0x00, 0x00, 0x62, 0x00, 0x00, 0x00, 0x5A, 0x00,
			0x00, 0x00, 0x5D, 0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0x54, 0x00, 0x00, 0x00, 0x4B, 0x00,
			0x00, 0x00, 0x63, 0x00, 0x00, 0x00, 0x5B, 0x00, 0x00, 0x00, 0x5E, 0x00, 0x00, 0x00, 0x61, 0x00,
			0x00, 0x00, 0x55, 0x00, 0x00, 0x00, 0x4E, 0x00, 0x00, 0x00, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x57, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x56, 0x00, 0x00, 0x00, 0x4D, 0x00,
			0x00, 0x00, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x57, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x56, 0x00, 0x00, 0x00, 0x4D, 0x00, 0x00, 0x00, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00,
		};

		public Ite829XRgbController(HidDevice device)
		{
			this.device = device;
		}

		public async ValueTask<Version> GetFirmwareVersionAsync()
		{
			Packet res = await this.ExecAsync(new Packet {B1 = Command.GetFirmwareVersion});

			if (res.B3 >= 10)
				return new Version(res.B2, res.B3 / 10, res.B3 % 10);
			return new Version(res.B2, res.B3);
		}

		public async ValueTask SetEffectAsync(RgbEffect effect)
		{
			this.Set(new Packet
			{
				B1 = Command.SetEffect,
				B3 = (byte)effect.Type,
				B4 = (byte)effect.Speed,
				B5 = (byte)effect.Brightness,
				B6 = (byte)effect.Color,
				B7 = (byte)effect.Direction
			});
			await Task.Delay(defaultWait);
		}

		public async ValueTask<RgbEffect> GetEffectAsync()
		{
			Packet res = await this.ExecAsync(new Packet {B1 = Command.GetEffect});
			return new RgbEffect
			{
				Type = (RgbEffectType)res.B3,
				Speed = res.B4,
				Brightness = res.B5,
				Color = (RgbEffectColor)res.B6,
				Direction = res.B7
			};
		}

		/// <summary>
		/// Sets the key matrix which is responsible for mapping physical buttons to key codes (which are then translated into actual keys by Windows' keyboard layout configuration).
		/// </summary>
		/// <param name="keyMatrix"></param>
		/// <returns></returns>
		public ValueTask SetKeyMatrixAsync(ReadOnlyMemory<byte> keyMatrix)
		{
			this.Set(new Packet {B1 = Command.SetKeyMatrix, B4 = 8});

			byte[] temp = new byte[65];

			for (int i = 0; i < 8; ++i)
			{
				keyMatrix.Span.Slice(i * 64, 64).CopyTo(new Span<byte>(temp, 1, 64));
				this.device.Stream.Write(temp, 0, temp.Length);
				this.device.Stream.Flush();
			}

			return default;
		}

		/// <summary>
		/// Gets the current key matrix.
		/// </summary>
		/// <param name="keyMatrix"></param>
		/// <returns></returns>
		public async ValueTask GetKeyMatrixAsync(Memory<byte> keyMatrix)
		{
			await this.ExecAsync(new Packet {B1 = Command.GetKeyMatrix, B4 = 8});

			byte[] temp = new byte[65];

			for (int i = 0; i < 8; ++i)
			{
				this.device.Stream.Read(temp, 0, temp.Length);
				new Span<byte>(temp, 1, 64).CopyTo(keyMatrix.Span.Slice(i * 64, 64));
			}
		}

		public ValueTask SetImageAsync(int index, ReadOnlyMemory<byte> image)
		{
			this.Set(new Packet {B1 = Command.SetImage, B3 = (byte)index, B4 = 8});
			//await Task.Delay(defaultWait);

			byte[] temp = new byte[65];

			for (int i = 0; i < 8; ++i)
			{
				image.Span.Slice(i * 64, 64).CopyTo(new Span<byte>(temp, 1, 64));
				this.device.Stream.Write(temp, 0, temp.Length);
				this.device.Stream.Flush();
				//await Task.Delay(defaultWait);
			}

			return default;
		}

		public async ValueTask GetImageAsync(int index, Memory<byte> image)
		{
			await this.ExecAsync(new Packet {B1 = Command.GetImage, B3 = (byte)index, B4 = 8});

			byte[] temp = new byte[65];

			for (int i = 0; i < 8; ++i)
			{
				this.device.Stream.Read(temp, 0, temp.Length);
				new Span<byte>(temp, 1, 64).CopyTo(image.Span.Slice(i * 64, 64));
			}
		}

		public ValueTask<Packet> ExecAsync(Packet p)
		{
			this.Set(p);
			//await Task.Delay(defaultWait);
			return new ValueTask<Packet>(this.Get());
		}

		public void Set(Packet p)
		{
			Span<byte> buf = stackalloc byte[9];
			buf[0] = p.B0;
			buf[1] = (byte)p.B1;
			buf[2] = p.B2;
			buf[3] = p.B3;
			buf[4] = p.B4;
			buf[5] = p.B5;
			buf[6] = p.B6;
			buf[7] = p.B7;
			buf[8] = (byte) (0xFF - (p.B0 + p.B1 + p.B2 + p.B3 + p.B4 + p.B5 + p.B6 + p.B7));
			if (!Hid.HidD_SetFeature(this.device.Handle, ref buf[0], 9))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public Packet Get()
		{
			Span<byte> buf = stackalloc byte[9];
			if (!Hid.HidD_GetFeature(this.device.Handle, ref buf[0], 9))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			Packet p;
			p.B0 = buf[0];
			p.B1 = (Command)buf[1];
			p.B2 = buf[2];
			p.B3 = buf[3];
			p.B4 = buf[4];
			p.B5 = buf[5];
			p.B6 = buf[6];
			p.B7 = buf[7];
			return p;
		}

		public enum Command : byte
		{
			GetFirmwareVersion = 0x80,

			SetEffect = 0x08,
			GetEffect = 0x88,

			SetKeyboardMode = 0x09,
			GetKeyboardMode = 0x89,

			SetKeyMatrix = 0x0D,
			GetKeyMatrix = 0x8D,

			SetMacroContent = 0x11,
			GetMacroContent = 0x91,

			SetRecoveryData = 0x13,

			SetImage = 0x12,
			GetImage = 0x92,
		}

		public struct Packet
		{
			public byte B0; // Always 0 it seems.
			public Command B1;
			public byte B2;
			public byte B3;
			public byte B4;
			public byte B5;
			public byte B6;
			public byte B7;
		}
	}
}
