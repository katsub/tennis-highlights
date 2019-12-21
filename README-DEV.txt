1.Publishing a self-contained release:
-Generate -> Publish... (config Release / x64 / Self-contained)
-An internet connection might be needed because of NUGET, check the Visual Studio output if you have publishing errors even though the build compiles
-Delete the DLL folder before publishing, for some reason it contains repeated versions of the dlls in the root folder
-Zip the release for about 40% final size compression