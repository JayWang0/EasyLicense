using System;
using System.Collections.Generic;

namespace EasyLicense.Lib
{
	public class CountManager
	{
		private static Dictionary<string, int> _countLimits = new Dictionary<string, int>();
		private Dictionary<string, int> _counts = new Dictionary<string, int>();
		
		public CountManager()
		{
			_counts = new Dictionary<string, int>();
		}

		public event Action<string> ExceedLimitation = str => { };

		public void Initialize(Dictionary<string, int> limits)
		{
			_countLimits = limits;
		}

		/// <summary>
		///     Checks the count.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>If count valid return true, otherwise false.</returns>
		public bool CheckCount(string name)
		{
			if (_counts.ContainsKey(name) && _countLimits.ContainsKey(name))
				if (_counts[name] >= _countLimits[name])
					return false;

			return true;
		}

		public void DecreaseCount(string name)
		{
			if (_counts.ContainsKey(name))
				if (_counts[name] > 0)
					_counts[name] -= 1;
		}

		public void IncreaseAndValidateCount(string name)
		{
			IncreaseCount(name);

			if (CheckCount(name) == false)
			{
				TriggerExceedLimitationEvent(name);

				ResetCount(name);
			}
		}

		public void IncreaseCount(string name)
		{
			if (_counts.ContainsKey(name))
				_counts[name] += 1;
			else
				ResetCount(name);
		}

		public void ResetCount(string name)
		{
			_counts[name] = 0;
		}

		public void TriggerExceedLimitationEvent(string name)
		{
			ExceedLimitation(name);
		}

		public void UpdateCount(string name, int count)
		{
			_counts[name] = count;
		}
	}
}