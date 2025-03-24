# OpenSimplex-noise-generator

Noise texture generation with octaves in Unity, simplexNoiseGenerator adapted from [jstanden](https://gist.github.com/jstanden/1489447). 

The script creates a noise texture that tiles seamlessly.

The new version grid.cs implements the texure generation in a compute shader. 
Simplex noise code from "Simplex noise demystified" by [Stefan Gustavson](https://muugumuugu.github.io/bOOkshelF/generative%20art/simplexnoise.pdf).
Hash function by [Bob Jenkins](https://burtleburtle.net/bob/hash/integer.html).

Here the scale input is an integer and represents the number of grid points along one edge. 
Example noise texture with scale three and five octaves.

<img src="https://github.com/user-attachments/assets/a5d92d2a-2e6b-428b-ad74-e53abaf9abb9" width=50% height=50%>

A 2x2 tiling of this texture. 

<img src="https://github.com/user-attachments/assets/a5549545-1e41-45b8-af3a-01bf7aa99acd" width=50% height=50%>
