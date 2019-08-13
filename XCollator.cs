using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A small utility class for reading and writing <see cref="XElement"/>s to and from files. Each <see cref="XCollator"/>
	/// instance contains mutable instances of <see cref="XmlReaderSettings"/> and <see cref="XmlWriterSettings"/>.
	/// </summary>
	public class XCollator : IExceptionHandler
	{
		private Action<Exception> exceptionHandler;

		/// <summary>
		/// The <see cref="XmlReaderSettings"/> to be used when reading.
		/// </summary>
		public readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings
		{
			CloseInput = true,
			IgnoreComments = true,
			IgnoreWhitespace = false,
			Async = false
		};

		/// <summary>
		/// The <see cref="XmlWriterSettings"/> to be used when writing.
		/// </summary>
		public readonly XmlWriterSettings WriterSettings = new XmlWriterSettings
		{
			Encoding = Encoding.UTF8,
			Indent = true,
			IndentChars = "\t",
			Async = false,
			NamespaceHandling = NamespaceHandling.OmitDuplicates
		};

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public Action<Exception> ExceptionHandler
		{
			get => exceptionHandler ?? XmlTools.ThrowHandler;
			set => exceptionHandler = value;
		}

		/// <summary>
		/// Reads the given XML files.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the root elements of the files.</returns>
		public IEnumerable<XElement> ReadFiles(IEnumerable<string> files)
		{
			foreach (string file in files ??
				throw new ArgumentNullException("Null object cannot be read as enumeration of file paths"))
			{
				yield return ReadFile(file);
			}
		}

		/// <summary>
		/// Reads the given XML files.
		/// </summary>
		/// <param name="streams">Streams containing readable XML.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the root elements of the files.</returns>
		public IEnumerable<XElement> ReadFiles(IEnumerable<Stream> streams)
		{
			foreach (Stream stream in streams ??
				throw new ArgumentNullException("Null object cannot be read as enumeration of file paths"))
			{
				yield return ReadFile(stream);
			}
		}

		/// <summary>
		/// Reads the given XML file.
		/// </summary>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>The root <see cref="XElement"/> of the document.</returns>
		public XElement ReadFile(string file)
		{
			if (file == null)
			{
				throw new ArgumentNullException("File path is null");
			}

			using (Stream stream = File.OpenRead(file))
			{
				return ReadFile(stream);
			}
		}

		/// <summary>
		/// Reads XML from the given <see cref="Stream"/> and returns its root element.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <returns>The root <see cref="XElement"/> of the document.</returns>
		public XElement ReadFile(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("Null stream cannot be read as XML");
			}

			using (XmlReader xmlReader = XmlReader.Create(stream, ReaderSettings))
			{
				return ReadFile(xmlReader);
			}
		}

		/// <summary>
		/// Reads XML from the given <see cref="XmlReader"/> and returns the root element.
		/// </summary>
		/// <param name="xmlReader">The <see cref="XmlReader"/> to read from.</param>
		/// <returns>The root <see cref="XElement"/> of the document.</returns>
		public XElement ReadFile(XmlReader xmlReader)
		{
			if (xmlReader == null)
			{
				throw new ArgumentNullException("XML reader is null");
			}

			try
			{
				XElement file = XElement.Load(xmlReader);
				OnFileRead(file);
				return file;
			}
			catch (Exception e)
			{
				ExceptionHandler(new InvalidOperationException("Failed to read XML.", e));
				return null;
			}
		}

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given file.
		/// </summary>
		/// <param name="file">The path of the file to write to.</param>
		/// <param name="root">The root <see cref="XElement"/> of the XML tree to write.</param>
		public void WriteFile(string file, XElement root)
		{
			if (file == null)
			{
				throw new ArgumentNullException("File path is null");
			}

			using (Stream stream = File.Open(file, FileMode.Create, FileAccess.Write))
			{
				WriteFile(stream, root);
				stream.Flush();
			}
		}

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="root">The root <see cref="XElement"/> of the XML tree to write.</param>
		public void WriteFile(Stream stream, XElement root)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("Cannot write XML to null stream.");
			}

			using (XmlWriter xmlWriter = XmlWriter.Create(stream, WriterSettings))
			{
				WriteFile(xmlWriter, root);
				xmlWriter.Flush();
			}
		}

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given <see cref="XmlWriter"/>.
		/// </summary>
		/// <param name="xmlWriter">The <see cref="XmlWriter"/> to write to.</param>
		/// <param name="root">The root <see cref="XElement"/> of the XML tree to write.</param>
		public void WriteFile(XmlWriter xmlWriter, XElement root)
		{
			if (xmlWriter == null)
			{
				throw new ArgumentNullException("XML writer is null.");
			}

			try
			{
				OnFileWrite(root);
				new XDocument(root).Save(xmlWriter);
			}
			catch (Exception e)
			{
				ExceptionHandler(e);
			}
		}

		/// <summary>
		/// Called when a file has been read, but before its <see cref="XElement"/> value
		/// (<paramref name="root"/>) has been returned.
		/// </summary>
		/// <param name="root">The root <see cref="XElement"/> of the file that was read.</param>
		protected virtual void OnFileRead(XElement root)
		{

		}

		/// <summary>
		/// Called immediately before an <see cref="XElement"/> (<paramref name="root"/>) is
		/// written to file.
		/// </summary>
		/// <param name="root">The root <see cref="XElement"/> written to the file.</param>
		protected virtual void OnFileWrite(XElement root)
		{

		}
	}
}
