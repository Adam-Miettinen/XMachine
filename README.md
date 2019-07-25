# XMachine
> An open-source XML serialization library for .NET Standard 2.0.

XMachine offers an alternative to the default `System.Xml.Serialization` namespace for .NET developers who want to persist objects in XML format. Its design goal is to let you design your classes without having to think about how they will be serialized, and then offer enough flexibility and customization so you can serialize any object, no matter how it's defined.

## Getting started

Visit the [wiki](Wiki) to learn about the methods and classes available in XMachine. For most applications, you will only need to interact with a few of the methods in the static `XmlTools` class.

Download and build the source files, then add the .dll to your project dependencies to start programming with XMachine. XMachine itself has no external dependencies except for .NET Standard 2.0, which is implemented by .NET Core 2.0, .NET Framework 4.6.1 and Mono 5.4. (It might also work with older versions of .NET Standard; let me know how it goes if you try.)

The project is still in an alpha state, so please report any issues you find.

## Features

This project makes it easy to:
* Bootstrap your open source project properly
* Make sure everyone gets what you're trying to achieve with your project
* Follow simple instructions for a perfect `README.md`

## Contributing

This library was developed for one specific use case of mine, so you may find it lacking in other areas. Feel free to [open an issue](Issues) if you have any thoughts about how XMachine could be improved.




If you'd like to contribute, please fork the repository and make changes as
you'd like. Pull requests are warmly welcome.

If your vision of a perfect `README.md` differs greatly from mine, it might be
because your projects are for vastly different. In this case, you can create a
new file `README-yourplatform.md` and create the perfect boilerplate for that.

E.g. if you have a perfect `README.md` for a Grunt project, just name it as
`README-grunt.md`.

## Related projects

There are a few other .NET XML serializer projects of which I'm aware:

- [ExtendedXmlSerializer](https://github.com/wojtpl2/ExtendedXmlSerializer)
- [YAXLib](https://github.com/sinairv/YAXLib)

## Licensing

This project is licensed under the MIT licence. You are free to duplicate and modify the project for personal or commercial use as long as you include a copy of the licence in any distribution.
