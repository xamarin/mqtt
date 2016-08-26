#region BSD License
/* 
Copyright (c) 2010, NETFx
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Adds a key/value pair to the <see cref="IDictionary{TKey, TValue}"/> if the key does not already exist. 
/// </summary>
internal static partial class DictionaryGetOrAdd
{
	/// <summary>
	/// Adds a key/value pair to the <see cref="IDictionary{TKey, TValue}"/> if the key does not already exist. 
	/// No locking occurs, so the value may be calculated twice on concurrent scenarios. If you need 
	/// concurrency assurances, use a concurrent dictionary instead.
	/// </summary>
	/// <nuget id="netfx-System.Collections.Generic.DictionaryGetOrAdd" />
	/// <param name="dictionary" this="true">The dictionary where the key/value pair will be added</param>
	/// <param name="key">The key to be added to the dictionary</param>
	/// <param name="valueFactory">The value factory</param>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
	{
		var value = default(TValue);
		if (!dictionary.TryGetValue(key, out value))
		{
			value = valueFactory(key);
			dictionary[key] = value;
		}

		return value;
	}
}