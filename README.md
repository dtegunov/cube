![Cube screenshot](https://raw.githubusercontent.com/dtegunov/cube/master/github.png)

#Cube – making sub-tomogram picking suck slightly less.

## You will need
- [Precompiled binaries](https://github.com/dtegunov/cube/raw/master/Precompiled/Precompiled.zip) (compiling the code yourself will likely fail because I don't synchronize updates for all dependencies)
- Windows PC with a GPU that supports OpenGL 4.4, the latest drivers and [.NET Framework 4.6](https://www.microsoft.com/en-us/download/details.aspx?id=48130) installed.
- Tomographic volume in MRC format.
- Optionally, particle coordinates either as a tab-delimited text file (XYZ columns), or a STAR file using Relion's column names (i. e. rlnCoordinateX etc.).


## Controls
- **Pan** the view by clicking the mouse wheel and dragging.
- **Zoom in and out** by holding Shift and scrolling.
- **Go through slices** by scrolling.
- Click the left mouse button and drag to **synchronize all three planes to the mouse position**. During this operation, **scrolling through slices still works**, so you can easily explore all 3 dimensions without pressing anything extra.
- Release the left mouse button to **create a new particle**. If you don't want to create one, hold Shift while releasing the button.
- **Delete** the currently selected particle by pressing Delete.
- **Delete** any particle by right-clicking it.
- **Cycle through particles** by pressing the Left and Right Arrow keys.
- **Adjust particle positions** by clicking on them and dragging. While dragging, scroll the mouse wheel to **adjust the third coordinate on the fly**.
- Alternatively, you can **go to a specific position** or **move a particle** by adjusting the numeric sliders at the top left corner of each view. Either click the slider, enter a value and confirm with Enter; scroll the mouse wheel while hovering over one; or click the slider and drag the mouse vertically.


## Authorship

Cube is being developed by Dimitry Tegunov ([tegunov@gmail.com](mailto:tegunov@gmail.com)), currently in Patrick Cramer's lab at the Max Planck Institute for Biophysical Chemistry in Göttingen, Germany.