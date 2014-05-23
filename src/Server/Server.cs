/*
   Copyright 2014 NETFX

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0
*/

namespace Hermes
{
	using System;

	public class Server
	{
		public Server(int port)
		{
			if (port != 1833)
				throw new ArgumentException("Must always be 1833");
		}
	}
}
