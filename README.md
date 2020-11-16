# PackLegion

This tool creates a new DAT/FAT file pair containing the files of the specified input folder.

## Usage

### Normal Mode

`PackLegion.exe [inputFolder] [outputFat]`  
`PackLegion.exe [inputFolder] [outputFat] [inputFat]`

Variant 1: Specify the input folder and the output FAT file.
Variant 2: Specify the input folder, the output FAT file and a FAT archive to use as a base archive.

### Combine Mode

Automatically combines the library files (\*.lib) contained in the input folder with the existing ones from the specified **patch** or **common** FAT archives, replacing the existing library objects and appending the new ones.

`PackLegion.exe -c [inputFolder] [outputFat] [patchFat] [commonFat]`
`PackLegion.exe -c [inputFolder] [outputFat] [commonFat]`

Variant 1: Specify the input folder, the output FAT file and the **common** FAT archive.
Variant 2: Specify the input folder, the output FAT file, a FAT archive to use as a base archive (patch) and the **common** FAT archive.