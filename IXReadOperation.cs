﻿using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// Represents a deserialization routine in which XML is converted to objects. Exposes backend methods used
	/// by <see cref="XReaderComponent"/>s and <see cref="XTypeComponent{T}"/>s during reading.
	/// </summary>
	public interface IXReadOperation : IExceptionHandler
	{
		/// <summary>
		/// Add a task that will be attempted repeatedly in a loop with all other scheduled tasks registered with the
		/// <see cref="IXReadOperation"/>.
		/// </summary>
		/// <param name="source">An <see cref="object"/> to identify as the source of this task in any <see cref="Exception"/>s
		/// generated by <paramref name="task"/>.</param>
		/// <param name="task">A delegate task for <see cref="IXReadOperation"/> to perform. The task should return <c>true</c> 
		/// if it has completed and <c>false</c> if it should be attempted again. <see cref="IXReadOperation"/> will abort if
		/// all the remaining tasks in its queue return <c>false</c>.</param>
		void AddTask(object source, Func<bool> task);

		/// <summary>
		/// Reads the given <see cref="XElement"/> as an object assignable to the type <typeparamref name="T"/>,
		/// performing the given delegate once the object is successfully constructed.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to deserialize from.</param>
		/// <param name="assign">A delegate to perform on the deserialized <typeparamref name="T"/>. If it returns <c>false</c>,
		/// it will be added to the task queue and repeatedly attempted until it returns <c>true</c>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// read.</param>
		void Read<T>(XElement element, Func<T, bool> assign, XObjectArgs args = null);

		/// <summary>
		/// Reads the given <see cref="XElement"/> as an object, performing the given delegate once the object is 
		/// successfully constructed.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to deserialize from.</param>
		/// <param name="assign">A delegate to perform on the deserialized <see cref="object"/>. If it returns <c>false</c>,
		/// it will be added to the task queue and repeatedly attempted until it returns <c>true</c>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// read.</param>
		void Read(XElement element, Func<object, bool> assign, XObjectArgs args = null);

		/// <summary>
		/// Reads the given <see cref="XAttribute"/> as an object, performing the given delegate once the object is 
		/// successfully constructed.
		/// </summary>
		/// <param name="attribute">The <see cref="XAttribute"/> to deserialize from.</param>
		/// <param name="assign">A delegate to perform on the deserialized <typeparamref name="T"/>. If it returns <c>false</c>,
		/// it will be added to the task queue and repeatedly attempted until it returns <c>true</c>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="attribute"/> should be
		/// read.</param>
		void Read<T>(XAttribute attribute, Func<T, bool> assign, XObjectArgs args = null);

		/// <summary>
		/// Reads the given <see cref="XElement"/> as an object assignable to the type <paramref name="expectedType"/>,
		/// performing the given delegate once the object is successfully constructed.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to deserialize from.</param>
		/// <param name="expectedType">A <see cref="Type"/> to which the deserialized object is assignable. This should match
		/// the <see cref="Type"/> argument passed to <see cref="IXWriteOperation.WriteElement(object, Type, XObjectArgs)"/>.</param>
		/// <param name="assign">A delegate to perform on the deserialized <see cref="object"/>. If it returns <c>false</c>,
		/// it will be added to the task queue and repeatedly attempted until it returns <c>true</c>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// read.</param>
		void Read(XElement element, Type expectedType, Func<object, bool> assign, XObjectArgs args = null);

		/// <summary>
		/// Reads the given <see cref="XAttribute"/> as an object of type <paramref name="expectedType"/>,
		/// performing the given delegate once the object is successfully constructed.
		/// </summary>
		/// <param name="attribute">The <see cref="XAttribute"/> to deserialize from.</param>
		/// <param name="expectedType">The runtime <see cref="Type"/> of the serialized object.</param>
		/// <param name="assign">A delegate to perform on the deserialized <see cref="object"/>. If it returns <c>false</c>,
		/// it will be added to the task queue and repeatedly attempted until it returns <c>true</c>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="attribute"/> should be
		/// read.</param>
		void Read(XAttribute attribute, Type expectedType, Func<object, bool> assign, XObjectArgs args = null);
	}
}
