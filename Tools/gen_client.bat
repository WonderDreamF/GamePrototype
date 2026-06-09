set WORKSPACE=%~dp0..
set GEN_CLIENT=%~dp0Luban\Luban.dll
set CONF_ROOT=%WORKSPACE%\DataTables

dotnet %GEN_CLIENT% ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%WORKSPACE%\Assets\_Code\Gen ^
    -x outputDataDir=%WORKSPACE%\Assets\StreamingAssets\Tables

