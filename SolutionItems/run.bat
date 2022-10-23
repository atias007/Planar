copy c:\temp\planar\*.* c:\temp\planar1\*.* /v /y
copy c:\temp\planar\*.* c:\temp\planar2\*.* /v /y
copy C:\Planar\Planar\bin\Debug\net6.0\Data\Jobs\BankOfIsraelCurrency\*.* C:\temp\Planar1\Data\Jobs\BankOfIsraelCurrency\*.* /v /y
copy C:\Planar\Planar\bin\Debug\net6.0\Data\Jobs\BankOfIsraelCurrency\*.* C:\temp\Planar2\Data\Jobs\BankOfIsraelCurrency\*.* /v /y

c:
cd\temp\planar1
start planar.exe
cd\temp\planar2
start planar.exe
cd\temp\cli
start planar-cli.exe service connect localhost 2306
start planar-cli.exe service connect localhost 2307

cd\temp
del *.xml
pause