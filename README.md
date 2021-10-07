# FBRepacker
Repacker tool for FB

## Build
1. Clone the repository
2. Go to Project -> Manage Nu Get Packages, there should be a restore packages option
3. On first build msclang.exe might be flagged as a virus due to pyinstaller bootloader issue, you can compile your own msclang.exe using python -m PyInstaller msclang.py --onefile, the source for msclang can be found here: https://github.com/descatal/msclang
