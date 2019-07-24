using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A small utility class for reading and writing <see cref="XElement"/> objects to and from files. Each instance
	/// encompasses an instance of <see cref="XmlReaderSettings"/> and an instance of <see cref="XmlWriterSettings"/>.
	/// <see cref="XCollator"/> has very little overhead, but for a default implementation, its methods are available
	/// statically in <see cref="XmlTools"/>.
	/// </summary>
	public class XCollator
	{
		/// <summary>
		/// The <see cref="XmlReaderSettings"/> to be used when reading.
		/// </summary>
		public readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings()
		{
			CloseInput = true,
			IgnoreComments = true,
			IgnoreWhitespace = false,
			Async = false
		};

		/// <summary>
		/// The <see cref="XmlWriterSettings"/> to be used when writing.
		/// </summary>
		public readonly XmlWriterSettings WriterSettings = new XmlWriterSettings()
		{
			Encoding = Encoding.UTF8,
			Indent = true,
			IndentChars = "\t",
			Async = false,
			NamespaceHandling = NamespaceHandling.OmitDuplicates
		};

		/// <summary>
		/// Reads each of the given XML files, producing an <see cref="IEnumerable{T}"/> over the root
		/// elements of the files.
		/// </summary>
		public IEnumerable<XElement> ReadFiles(IEnumerable<string> files)
		{
			foreach (string file in files ??
				throw new ArgumentNullException("Null object cannot be read as enumeration of file paths"))
			{
				yield return ReadFile(file);
			}
		}

		/// <summary>
		/// Reads each of the given XML files, producing an <see cref="IEnumerable{T}"/> over the root
		/// elements of the files.
		/// </summary>
		public IEnumerable<XElement> ReadFiles(IEnumerable<Stream> streams)
		{
			foreach (Stream stream in streams ??
				throw new ArgumentNullException("Null object cannot be read as enumeration of file paths"))
			{
				yield return ReadFile(stream);
			}
		}

		/// <summary>
		/// Reads the given XML file and returns its root element.
		/// </summary>
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
				throw new InvalidOperationException("Failed to read XML.", e);
			}
		}

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given file.
		/// </summary>
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
		public void WriteFile(XmlWriter xmlWriter, XElement root)
		{
			if (xmlWriter == null)
			{
				throw new ArgumentNullException("XML writer is null.");
			}

			OnFileWrite(root);
			new XDocument(root).Save(xmlWriter);
		}

		/// <summary>
		/// Called when a file has been read, but before its <see cref="XElement"/> value
		/// (<paramref name="root"/>) has been returned.
		/// </summary>
		protected virtual void OnFileRead(XElement root)
		{

		}

		/// <summary>
		/// Called immediately before an <see cref="XElement"/> (<paramref name="root"/>) is
		/// written to file.
		/// </summary>
		protected virtual void OnFileWrite(XElement root)
		{

		}
	}
}
