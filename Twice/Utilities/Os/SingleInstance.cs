﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Twice.Utilities.Os
{
	[ExcludeFromCodeCoverage]
	internal static class SingleInstance
	{
		static SingleInstance()
		{
			WM_SHOWFIRSTINSTANCE = NativeMethods.RegisterWindowMessage( $"WM_SHOWFIRSTINSTANCE|{AssemblyGuid}" );
		}

		public static void ShowFirstInstance()
		{
			NativeMethods.PostMessage( (IntPtr)NativeMethods.HWND_BROADCAST, WM_SHOWFIRSTINSTANCE, IntPtr.Zero, IntPtr.Zero );
		}

		public static bool Start()
		{
			string mutextName = $"Twice.{AssemblyGuid}".Replace( "-", "" );
			if( Constants.Debug )
			{
				mutextName += ".DEBUG";
			}

			bool onlyInstance;
			AppMutex = new Mutex( true, mutextName, out onlyInstance );
			ReleaseMutex = onlyInstance;
			return onlyInstance;
		}

		public static void Stop()
		{
			if( ReleaseMutex )
			{
				AppMutex?.ReleaseMutex();
				AppMutex?.Dispose();
			}
		}

		private static Mutex AppMutex;
		private static bool ReleaseMutex;

		// ReSharper disable once InconsistentNaming
		internal static readonly int WM_SHOWFIRSTINSTANCE;

		private static string AssemblyGuid
		{
			get
			{
				IEnumerable<GuidAttribute> attributes =
					Assembly.GetExecutingAssembly().GetCustomAttributes<GuidAttribute>().ToArray();
				return !attributes.Any()
					? string.Empty
					: attributes.First().Value;
			}
		}
	}
}