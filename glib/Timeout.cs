// GLib.Timeout.cs - Timeout class implementation
//
// Author: Mike Kestner <mkestner@speakeasy.net>
//
// Copyright (c) 2002 Mike Kestner
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the Lesser GNU General
// Public License as published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this program; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.


namespace GLib {

	using System;
	using System.Runtime.InteropServices;

	public delegate bool TimeoutHandler ();

	public class Timeout {

		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
		delegate bool TimeoutHandlerInternal (IntPtr ptr);

		internal class TimeoutProxy : SourceProxy {
			public TimeoutProxy (TimeoutHandler real)
			{
				real_handler = real;
				proxy_handler = new TimeoutHandlerInternal (Handler);
			}

			~TimeoutProxy ()
			{
				Dispose (false);
			}

			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}

			protected virtual void Dispose (bool disposing)
			{
				// Both branches remove our delegate from the
				// managed list of handlers, but only
				// Source.Remove will remove it from the
				// unmanaged list also.

				if (disposing)
					Remove ();
				else
					Source.Remove (ID);
			}

			static bool Handler (IntPtr data)
			{
				try {
					SourceProxy obj;
					lock (proxies)
						obj = proxies [(int)data];
					TimeoutHandler timeout_handler = (TimeoutHandler)obj.real_handler;

					bool cont = timeout_handler ();
					if (!cont)
						obj.Remove ();
					return cont;
				} catch (Exception e) {
					ExceptionManager.RaiseUnhandledException (e, false);
				}
				return false;
			}
		}

		private Timeout () {}

		[DllImport("libglib-2.0-0.dll", CallingConvention=CallingConvention.Cdecl)]
		static extern uint g_timeout_add (uint interval, TimeoutHandlerInternal d, IntPtr data);

		public static uint Add (uint interval, TimeoutHandler hndlr)
		{
			TimeoutProxy p = new TimeoutProxy (hndlr);

			p.ID = g_timeout_add (interval, (TimeoutHandlerInternal) p.proxy_handler, (IntPtr)p.proxyId);
			lock (Source.source_handlers)
				Source.source_handlers [p.ID] = p;

			return p.ID;
		}
	}
}
