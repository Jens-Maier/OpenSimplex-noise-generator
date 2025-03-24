# OpenSimplex-noise-generator

Noise texture generation with octaves in Unity, simplexNoiseGenerator adapted from [jstanden](https://gist.github.com/jstanden/1489447). 

The script grid.cs creates a noise texture that tiles seamlessly.

The new version implements the texure generation in a compute shader. 

Simplex noise code from "Simplex noise demystified" by [Stefan Gustavson](https://muugumuugu.github.io/bOOkshelF/generative%20art/simplexnoise.pdf).
Hash function by [Bob Jenkins](https://burtleburtle.net/bob/hash/integer.html).
