/*/
role classLibrary;
define IDE_LA,NO_GLOBAL,NO_DEFAULT_CHARSET_UNICODE;
noWarnings 1591,419,649;
preBuild ..\@Au.Editor\_prePostBuild.cs;
outputPath %folders.Workspace%\dll;
miscFlags 1;
noRef *\Au.dll;
pr ..\@Au\Au.cs;
resource Themes\Generic.xaml /embedded;
/*/
