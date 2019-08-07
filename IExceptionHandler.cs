using System;

namespace XMachine
{
	/// <summary>
	/// Represents an object that catches <see cref="Exception"/>s thrown by it or its 
	/// <see cref="IXComponent"/>s, then handles them with a delegate accessible via
	/// <see cref="ExceptionHandler"/>.
	/// </summary>
	public interface IExceptionHandler
	{
		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		Action<Exception> ExceptionHandler { get; set; }
	}
}
