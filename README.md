# FBRepacker
The one stop tool for modifying game files for Gundam Extreme Vs. Full Boost on PS3.

Includes:
- Extract / Repack .PAC (FHM) file containers
- Parse and Serailize TBL files (.PAC regconizing TOC file)
- Converting 3D model NUD files alongside the skeleton structure VBN files into .DAE with the ability to be converted back.
- Parsing unit's variables binary into relevant JSON and serializing back.
- Parsing projectile, reload, and hit properties binary into relevant JSON and serializing back.
- Parsing voice line logic binary into relevant JSON and serializing back.
- Parsing text NTXB binary into relevant JSON and serializing back.
- Parsing unit and series list binary into relevant JSON and serializing back.
- Autolink script function for unit's MSC script
- Generate B4AC and additional info for MBON's unit MSC script
- Converting MBON's ALEO, LMB, unit's MSC script into FB compatible version.

## Build
1. Clone the repository
2. Go to Project -> Manage Nu Get Packages, there should be a restore packages option
3. On first build msclang.exe might be flagged as a virus due to pyinstaller bootloader issue, you can compile your own msclang.exe using python -m PyInstaller msclang.py --onefile, the source for msclang can be found here: https://github.com/descatal/msclang
