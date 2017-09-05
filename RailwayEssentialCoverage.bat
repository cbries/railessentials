call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"

cd "C:\Users\ChristianRi\Source\Repos\railwayessential"

msbuild /t:Clean

cov-build --dir cov-int msbuild /t:Rebuild

"C:\Program Files\7-Zip\7z.exe" a RailwayEssentialCoverage.zip cov-int

pause
