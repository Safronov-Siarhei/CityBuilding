Drop reference map images here (PNG/JPG), one file per map.

Convention:
- Logical map size is always 100x100 cells; a dark/black frame around the
  content is fine and gets auto-cropped by the importer.
- Colors: blue = water, light green = grass, dark green = forest, gray = stone.

To import: run the Unity Editor menu "CityBuilder/Import Maps From Source Folder"
(or run MapImporter.ImportAll via -executeMethod in batchmode). Each image
becomes a MapDefinition asset under Assets/_Project/Resources/Maps, named after
the source file, and is picked up automatically by the in-game map pool.
