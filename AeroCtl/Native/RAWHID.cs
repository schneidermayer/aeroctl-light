using System;
using System.Runtime.InteropServices;

namespace AeroCtl.Native
{
	[StructLayout(LayoutKind.Sequential)]
	public struct RAWHID
	{
		public int dwSizeHid;
		public int dwCount;
		public IntPtr bRawData;
	}
}