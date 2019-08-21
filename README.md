![XMachineSocialCard](https://user-images.githubusercontent.com/51489385/63445850-210fa780-c407-11e9-9f47-e22cf763d7ce.png)

XMachine offers an alternative to the default `System.Xml.Serialization` namespace for .NET developers who want to persist objects in XML format. Its main design goals are flexibility, ease of use, and encapsulation: programmers should be able to code their classes without having to spare a thought for the limitations of the serializer.

## Getting started

Visit the [wiki](https://github.com/Adam-Miettinen/XMachine/wiki) to learn about the methods and classes available in XMachine. The code itself has fairly complete commenting as well. Start by looking at the static `XmlTools` class: for a simple application, you will only need to interact with a few of its methods.

To start programming with XMachine, download and build the source files, then add the built assembly to your project dependencies. XMachine itself has no external dependencies except for .NET Standard 2.0, which is implemented by .NET Core 2.0, .NET Framework 4.6.1 and Mono 5.4. (It will probably work with older versions of .NET Standard, too; let me know how it goes if you try.)

The project is still in an alpha state, so please [report any issues](https://github.com/Adam-Miettinen/XMachine/issues) you find.

## Features

XMachine expands on .NET's built-in serializer in several ways:

* It can serialize an object's properties regardless of how they are typed: you can serialize interface types, generics, arbitrary collections and arrays, etc.
* It can serialize most of the built-in .NET collections and any types that implement interfaces like `ICollection<T>` and `IDictionary<TKey, TValue>`.
* It automatically recognizes and uses parameterized constructors if the parameters can be matched to properties of the object.
* You can optionally choose to serialize non-public properties and to use non-public constructors.
* It is compatible with the standard XML custom attributes (XmlTypeAttribute, XmlAttributeAttribute, XmlElementAttribute).
* It can read and write objects as references, preserving references between serialized objects and reducing XML file sizes. References can be backward, forward or circular. You can even include references to "contextual objects," objects that don't exist in the XML document but are provided programmatically during serialization and deserialization.
* You can choose how to handle exceptions via simple `Action<Exception>` delegates. Catch statements are rarely necessary, and you can use a custom logger for debugging.
* You can control the format in which your XML will be written, such as the element names assigned to object types, which properties are written as attributes, and how collection entries are formatted, all without needing to change your classes or mess them up with custom attributes.

## Limitations

* XMachine does not use reflection emit or dynamic assemblies. It has to run in an application domain with sufficient reflection permissions.
* It does not support encryption/decryption.
* It cannot (automatically) serialize `ref readonly struct` types.

Most of XMachine's functionality is extensible with [components](https://github.com/Adam-Miettinen/XMachine/wiki/Components), making it relatively easy to add features as needed.

## Contributing

XMachine was written by Adam Miettinen. Hi.

The library was developed and tested around one specific use case of mine, so you may find it lacking in other areas. Feel free to [open an issue](https://github.com/Adam-Miettinen/XMachine/issues) if you have any thoughts about how XMachine could be improved. Feel free also to fork the repository and let me know how any changes work out.

## Licensing

This project is licensed under the MIT licence. You are free to duplicate and modify the project for personal or commercial use as long as you include a copy of the licence in any distribution.
