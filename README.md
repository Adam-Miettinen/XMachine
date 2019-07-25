# XMachine
> An open-source XML serialization library for .NET Standard 2.0.

XMachine offers an alternative to the default `System.Xml.Serialization` namespace for .NET developers who want to persist objects in XML format. Its design goal is to let you design your classes without having to think about how they will be serialized, and then offer enough flexibility and customization so you can serialize any object, no matter how it's defined.

## Getting started

Visit the [wiki](Wiki) to learn about the methods and classes available in XMachine. For most applications, you will only need to interact with a few of the methods in the static `XmlTools` class.

To start programming with XMachine, download and build the source files, then add the built assembly to your project dependencies. XMachine itself has no external dependencies except for .NET Standard 2.0, which is implemented by .NET Core 2.0, .NET Framework 4.6.1 and Mono 5.4. (It might also work with older versions of .NET Standard; let me know how it goes if you try.)

The project is still in an alpha state, so please report any issues you find.

## Features

XMachine expands on the built-in serializer in several ways:

* Can serialize any .NET object's properties regardless of how the property type is declared (supports interfaces, generics, collections, arrays, etc.)
* Automatically supports serializing the items of the most commonly-used collections.
* Optionally, will recognize and use non-public properties and non-public or parameterized constructors.
* Can read and write duplicate objects as references, reducing XML file size.
* Lets serialized objects preserve references to contextual objects, objects that exist in the program but not in XML.
* Lets you specify how to handle exceptions thrown from malformed XML.

## Limitations

* XMachine does not use reflection emit or dynamic assemblies, so it must be run in a trust environment that has reflection permissions enabled.
* It does not support encryption/decryption.

Its functionality is extensible with [components](Wiki/Components), making it relatively easy to add features as needed.

## Contributing

XMachine was written by Adam Miettinen. It is my first _GitHub_ project.

The library was developed and tested around one specific use case of mine, so you may find it lacking in other areas. Feel free to [open an issue](Issues) if you have any thoughts about how XMachine could be improved. Feel free also to fork the repository and let me know how any changes work out.

## Related projects

There are a few other .NET XML serializer projects of which I'm aware:

- [ExtendedXmlSerializer](https://github.com/wojtpl2/ExtendedXmlSerializer)
- [YAXLib](https://github.com/sinairv/YAXLib)
- [XSerializer](https://github.com/QuickenLoans/XSerializer)

## Licensing

This project is licensed under the MIT licence. You are free to duplicate and modify the project for personal or commercial use as long as you include a copy of the licence in any distribution.