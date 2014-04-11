/*
   Copyright 2012 Clarius Consulting

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0
*/
namespace Hermes
{
	using System;

	public class Broker
	{
		public Broker(int port)
		{
			if (port < 1800 || port > 2000)
				throw new ArgumentException("Must always be between 1800 and 2000");
		}
	}
}