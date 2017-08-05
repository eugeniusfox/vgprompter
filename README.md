# VGPrompter

VGPrompter is a C# library which parses an extended subset of [Ren'Py](https://www.renpy.org/) script syntax and allows to iterate over the lines according to:
- flow control statements (conditions are C# parameterless methods referenced in the script);
- user input provided to interactive menus.

The compiled library is available as a [Unity3D managed plugin](https://www.assetstore.unity3d.com/en/#!/content/69665) in the Asset Store.

The project targets the .NET 3.5 framework and has no dependencies.

## Disclaimer
**It's still a work in progress!**

What it is **NOT**:
- a Ren'Py C# port;
- a visual novel engine.

Anything related to graphics and sound is purposefully excluded from this project.

Nothing in the script ever gets evaluated; logic can only be embedded by referencing C# methods via aliases.

## Documentation
See the [wiki](https://github.com/eugeniusfox/vgprompter/wiki) or jump straight to the [quickstart](https://github.com/eugeniusfox/vgprompter/wiki/Quickstart)!

## Development & Future plans
Check my blog: https://eugeniusfox.wordpress.com/
