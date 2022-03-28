This is a Unity based port for Smart Random Colors.

It is based off the following packages:

- https://www.npmjs.com/package/randomcolor
- https://github.com/nathanpjones/randomColorSharped

Getting a single color is a simple matter.

```
using RandomColorGenerator;
...
var color = RandomColor.GetColor(ColorScheme.Random, Luminosity.Bright);
```

Or you can generate multiple colors in a single go.

```
var colors = RandomColor.GetColors(ColorScheme.Red, Luminosity.Light, 25);
```
