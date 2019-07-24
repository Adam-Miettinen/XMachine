using System;

namespace XMachine
{
	/// <summary>
	/// Represents an object that handles <see cref="Exception"/>s with a delegate method.
	/// </summary>
	public interface IExceptionHandler
	{
		/// <summary>
		/// Returns the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		Action<Exception> ExceptionHandler { get; }
	}
}
